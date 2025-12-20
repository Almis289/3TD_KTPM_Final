using Book_Store.Data;
using Book_Store.Models;
using Microsoft.AspNetCore.Mvc;

namespace Book_Store.Controllers
{
    public class SubscribeController : Controller
    {
        private readonly BookStoreDbContext _context;

        public SubscribeController(BookStoreDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập email!" });
            }

            bool exists = _context.Subscribers.Any(s => s.Email == email);
            if (exists)
            {
                return Json(new { success = false, message = "Email này đã đăng ký trước đó!" });
            }

            _context.Subscribers.Add(new Subscriber { Email = email });
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đăng ký nhận tin thành công!" });
        }

    }
}
