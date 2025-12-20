using Book_Store.Data; // namespace chứa ApplicationDbContext
using Book_Store.Models; // namespace chứa Order, OrderDetail, Product
using Book_Store.Services.Vnpay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Book_Store.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly BookStoreDbContext _context;
        private readonly IVnPayService _vnPayService;

        public OrderController(BookStoreDbContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == int.Parse(userId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders); // sẽ tìm view tại Views/Order/History.cshtml
        }

        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            return Json(response);
        }
    }
}
