using Book_Store.Helpers;
using Book_Store.Models;

namespace Book_Store.Services.Implement
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model)
        {
            var vnpay = new VnpayLibrary();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _config["Vnpay:TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {model.OrderId}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["Vnpay:ReturnUrl"]);
            vnpay.AddRequestData("vnp_TxnRef", model.OrderId);

            return vnpay.CreateRequestUrl(
                _config["Vnpay:BaseUrl"],
                _config["Vnpay:HashSecret"]
            );
        }


        public VnPaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            return new VnPaymentResponseModel
            {
                Success = collections["vnp_ResponseCode"] == "00",
                PaymentMethod = "VNPAY",
                TransactionId = collections["vnp_TransactionNo"],
                VnPayResponseCode = collections["vnp_ResponseCode"]
            };
        }
    }
}
