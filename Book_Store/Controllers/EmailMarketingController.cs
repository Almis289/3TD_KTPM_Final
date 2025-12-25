using Book_Store.Data;
using Book_Store.Models;
using Book_Store.Services;
using Book_Store.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                Products = await _context.Products
            .Include(p => p.ProductDetail)
            .ToListAsync(),

                Users = await _context.Users
            .Where(u => u.Role == "Customer")
            .ToListAsync(),

                // 👇 THÊM DÒNG NÀY
                SubscribeEmails = await _context.SubscribeEmails
            .Select(x => x.Email)
            .ToListAsync()
            };

            return View("~/Views/Admin/EmailMarketing/SendEmailForm.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailForm(EmailMarketingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";

                model.Products = await _context.Products
                    .Include(p => p.ProductDetail)
                    .ToListAsync();

                model.Users = await _context.Users
                    .Where(u => u.Role == "Customer")
                    .ToListAsync();

                model.SubscribeEmails = await _context.SubscribeEmails
                    .Select(x => x.Email)
                    .ToListAsync();

                return View("~/Views/Admin/EmailMarketing/SendEmailForm.cshtml", model);
            }

            // Lấy toàn bộ email
            List<string> emails = new();

            if (model.SelectedEmails?.Any() ?? false)
                emails.AddRange(model.SelectedEmails);

            if (!string.IsNullOrWhiteSpace(model.ExtraEmails))
            {
                var manual = model.ExtraEmails.Split(',')
                    .Select(e => e.Trim())
                    .Where(e => e.Contains("@"));
                emails.AddRange(manual);
            }

            var subscribers = await _context.SubscribeEmails.Select(s => s.Email).ToListAsync();
            

            emails = emails.Distinct().ToList();

            // Lấy danh sách sản phẩm được chọn
            var products = new List<Product>();

            if (model.SelectedProductIds != null && model.SelectedProductIds.Any())
            {
                products = await _context.Products
                    .Include(p => p.ProductDetail)
                    .Where(p => model.SelectedProductIds.Contains(p.ProductId))
                    .ToListAsync();
            }

            // Build HTML template
            string htmlTemplate = _emailService.BuildProductEmailTemplate(
                model.Subject,
                model.Content,
                products
            );

            // Gửi email
            await _emailService.SendEmailToAllUsersAsync(emails, model.Subject, htmlTemplate, products);

            TempData["Message"] = "Gửi email marketing thành công!";
            return RedirectToAction("SendEmailForm");
        }
    }
}
