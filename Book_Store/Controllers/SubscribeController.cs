using Book_Store.Data;
using Book_Store.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Register(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index", "Home");

            if (!_context.SubscribeEmails.Any(x => x.Email == email))
            {
                _context.SubscribeEmails.Add(new SubscribeEmail
                {
                    Email = email
                });
                await _context.SaveChangesAsync();
            }

            TempData["SubscribeSuccess"] = "Đăng ký nhận bản tin thành công!";
            return RedirectToAction("Index", "Customer");
        }
    }
}
