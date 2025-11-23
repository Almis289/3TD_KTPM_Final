using Book_Store.Data;
using Book_Store.Models;
using Book_Store.Services;
using Book_Store.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Book_Store.Controllers
{
    [Authorize(Roles = "Manager")]
    public class EmailMarketingController : Controller
    {
        private readonly EmailService _emailService;
        private readonly BookStoreDbContext _context;

        public EmailMarketingController(EmailService emailService, BookStoreDbContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SendEmailForm()
        {
            var model = new EmailMarketingViewModel
            {
                Products = await _context.Products.Include(p => p.ProductDetail).ToListAsync(),
                Users = await _context.Users.Where(u => u.Role == "Customer").ToListAsync()
            };

            return View("~/Views/Admin/EmailMarketing/SendEmailForm.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailForm(EmailMarketingViewModel model)
        {
            // Danh sách email người nhận
            var emails = model.SelectedEmails.Any()
                ? model.SelectedEmails
                : await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync();

            Product? product = null;
            string? productImagePath = null;

            if (model.ProductId.HasValue)
            {
                product = await _context.Products
                    .Include(p => p.ProductDetail)
                    .FirstOrDefaultAsync(p => p.ProductId == model.ProductId.Value);

                if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
                {
                    productImagePath = System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(),
                        "wwwroot",
                        product.ImageUrl.TrimStart('/'));

                    if (!System.IO.File.Exists(productImagePath))
                    {
                        productImagePath = null;
                    }
                }
            }

            // Tạo template email
            string htmlTemplate = _emailService.BuildProductEmailTemplate(
                model.Subject,
                model.Content,
                product
            );

            // Gửi email với ảnh inline nếu có
            await _emailService.SendEmailToAllUsersAsync(emails, model.Subject, htmlTemplate, productImagePath);

            TempData["Message"] = "Gửi email marketing thành công!";
            return View("~/Views/Admin/EmailMarketing/SendEmailForm.cshtml", model);
        }
    }
}
