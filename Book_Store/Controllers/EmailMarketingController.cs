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
            var customers = await _context.Users
                .Where(u => u.Role == "Customer")
                .ToListAsync();

            var subs = await _context.Subscribers.ToListAsync();

            var model = new EmailMarketingViewModel
            {
                Products = await _context.Products.Include(p => p.ProductDetail).ToListAsync(),

                AllEmails = customers.Select(c => new UnifiedEmailVM
                {
                    Email = c.Email,
                    Name = c.FullName,
                    Type = "Customer"
                })
                .Concat(
                    subs.Select(s => new UnifiedEmailVM
                    {
                        Email = s.Email,
                        Name = null,
                        Type = "Subscriber"
                    })
                )
                .ToList()
            };

            return View("~/Views/Admin/EmailMarketing/SendEmailForm.cshtml", model);
        }


        [HttpPost]
        public async Task<IActionResult> SendEmailForm(EmailMarketingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Products = await _context.Products.Include(p => p.ProductDetail).ToListAsync();
                model.Users = await _context.Users.Where(u => u.Role == "Customer").ToListAsync();
                model.Subscribers = await _context.Subscribers.ToListAsync();   // ⭐ THÊM
                return View("~/Views/Admin/EmailMarketing/SendEmailForm.cshtml", model);
            }



            // ======== BẮT ĐẦU TẠO DANH SÁCH EMAIL ========

            List<string> recipientEmails = new List<string>();

            // 1) Email nhập thủ công
            if (!string.IsNullOrWhiteSpace(model.ManualEmails))
            {
                var rawEmails = model.ManualEmails
                    .Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var email in rawEmails)
                {
                    var trimmed = email.Trim();
                    if (IsValidEmail(trimmed))
                        recipientEmails.Add(trimmed);
                }
            }

            // ⭐⭐ === 2) Email từ box duy nhất === ⭐⭐
            if (model.SelectedAllEmails != null && model.SelectedAllEmails.Any())
            {
                recipientEmails.AddRange(model.SelectedAllEmails);
            }

            // 3) Nếu vẫn không có gì → gửi tất cả
            if (!recipientEmails.Any())
            {
                var customerEmails = await _context.Users
                    .Where(u => u.Role == "Customer" && !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync();

                var subscriberEmails = await _context.Subscribers
                    .Where(s => !string.IsNullOrEmpty(s.Email))
                    .Select(s => s.Email)
                    .ToListAsync();

                recipientEmails = customerEmails
                    .Concat(subscriberEmails)
                    .Distinct()
                    .ToList();
            }
            else
            {
                // loại trùng lặp
                recipientEmails = recipientEmails.Distinct().ToList();
            }
            // ======== HẾT PHẦN EMAIL ========

            // Lấy thông tin sản phẩm (nếu có)
            List<Product> selectedProducts = new List<Product>();

            if (model.SelectedProductIds != null && model.SelectedProductIds.Any())
            {
                selectedProducts = await _context.Products
                    .Include(p => p.ProductDetail)
                    .Where(p => model.SelectedProductIds.Contains(p.ProductId))
                    .Take(10) // tăng lên 10 nếu muốn gửi nhiều hơn
                    .ToListAsync();
            }

            // Tạo template
            string htmlTemplate = _emailService.BuildProductEmailTemplate(
                model.Subject,
                model.Content,
                selectedProducts.Any() ? selectedProducts : null
            );

            // GỬI EMAIL VỚI ẢNH CID CHO TẤT CẢ SẢN PHẨM
            await _emailService.SendEmailWithInlineImagesAsync(
                recipientEmails,
                model.Subject,
                htmlTemplate,
                selectedProducts
            );

            TempData["Message"] = $"Gửi email marketing thành công đến {recipientEmails.Count} người nhận!";
            return RedirectToAction("SendEmailForm");
        }

        // Hàm kiểm tra email hợp lệ
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        


    }
}
