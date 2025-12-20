// Book_Store/Services/Vnpay/VnPayService.cs
using Book_Store.Libraries;
using Book_Store.Models.Vnpay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Book_Store.Services.Vnpay
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;
        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var timeZoneId = _configuration["TimeZoneId"] ?? "SE Asia Standard Time";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            var vnp_Params = new SortedDictionary<string, string>
            {
                { "vnp_Version", _configuration["Vnpay:Version"] ?? "2.1.0" },
                { "vnp_Command", _configuration["Vnpay:Command"] ?? "pay" },
                { "vnp_TmnCode", _configuration["Vnpay:TmnCode"] },
                { "vnp_Amount", ((long)(model.Amount * 100)).ToString() },   // quan trọng: long, *100
                { "vnp_CreateDate", now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", _configuration["Vnpay:CurrCode"] ?? "VND" },
                { "vnp_IpAddr", GetIpAddress(context) },
                { "vnp_Locale", _configuration["Vnpay:Locale"] ?? "vn" },
                { "vnp_OrderInfo", $"{model.Name} {model.OrderDescription} {model.Amount}" },
                { "vnp_OrderType", model.OrderType ?? "other" },
                { "vnp_ReturnUrl", _configuration["Vnpay:PaymentBackReturnUrl"] },
                { "vnp_TxnRef", DateTime.Now.Ticks.ToString() },
                { "vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss") } // bắt buộc 2024-2025
            };

            // Nếu có chọn ngân hàng
            if (!string.IsNullOrEmpty(model.BankCode))
                vnp_Params["vnp_BankCode"] = model.BankCode;

            // Tạo query string đã encode đúng chuẩn
            string queryString = string.Join("&", vnp_Params
                .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

            // Tính hash
            string rawHashData = queryString;
            string vnp_SecureHash = HmacSHA512(_configuration["Vnpay:HashSecret"], rawHashData);

            // URL cuối cùng
            string paymentUrl = _configuration["Vnpay:BaseUrl"] + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
            return paymentUrl;
        }

        // Hàm hash chuẩn VNPAY yêu cầu
        private static string HmacSHA512(string key, string input)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // Lấy IP (VNPAY chỉ chấp nhận max 15 ký tự)
        private static string GetIpAddress(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
                ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ip;

            if (string.IsNullOrEmpty(ip) || ip == "::1") ip = "127.0.0.1";

            return ip.Length > 15 ? "127.0.0.1" : ip;
        }

        // Phần trả về kết quả (giữ nguyên hoặc cải thiện)
        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            // Bạn có thể vẫn dùng VnpayLibrary ở đây nếu muốn, hoặc tự parse cũng được
            var pay = new VnpayLibrary();
            return pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);
        }
    }
}