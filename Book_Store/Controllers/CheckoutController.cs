using Microsoft.AspNetCore.Mvc;
using Book_Store.Services.Vnpay;        // để nhận IVnPayService
using Book_Store.Models;               // vì PaymentResponseModel nằm đây

namespace Book_Store.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IVnPayService _vnPayService;

        public CheckoutController(IVnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        [HttpGet]
        [Route("Checkout/PaymentCallbackVnpay")]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            // Kiểm tra thành công (mã "00")
            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Lỗi thanh toán VNPAY: {response?.VnPayResponseCode ?? "Không xác định"}";
                return RedirectToAction("PaymentFail");
            }

            // Thành công → lấy đúng property của PaymentResponseModel
            TempData["Message"] = $"Thanh toán thành công đơn hàng #{response.OrderId} • Số tiền: {response.OrderDescription} ";

            // Nếu muốn cập nhật trạng thái đơn hàng thì mở comment
            // _orderService.ConfirmPayment(response.OrderId, response.TransactionNo);

            return RedirectToAction("PaymentSuccess");
        }

        public IActionResult PaymentSuccess()
        {
            ViewBag.Message = TempData["Message"];
            return View();
        }

        public IActionResult PaymentFail()
        {
            ViewBag.Message = TempData["Message"];
            return View();
        }
    }
}