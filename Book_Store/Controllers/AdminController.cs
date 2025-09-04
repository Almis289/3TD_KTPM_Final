using Book_Store.Data;
using Microsoft.AspNetCore.Identity;
using Book_Store.Models;
using Book_Store.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BookStore.Controllers
{
    [Authorize(Roles = "Manager")]
    public class AdminController : Controller
    {
        private readonly BookStoreDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AdminController(BookStoreDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }


        // ------------------ DASHBOARD ------------------

        public IActionResult Index()
        {
            ViewBag.TotalUsers = _context.Users.Count(u => u.Role == "Customer");
            ViewBag.PendingOrders = _context.Orders.Count(o => o.Status == OrderStatus.DangXuLy); // ví dụ chờ xử lý

            var today = DateTime.Today;
            ViewBag.TodayRevenue = _context.Payments
                .Where(p => p.PaymentDate.Date == today)
                .Sum(p => (decimal?)p.Amount) ?? 0;

            return View();
        }

        // ------------------ SẢN PHẨM ------------------
        public IActionResult ProductManagement(string keyword)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Author)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p =>
                    p.ProductId.ToString().Contains(keyword) ||
                    p.ProductName.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(query.ToList());
        }
        [HttpGet]
        public IActionResult CreateProduct()
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Authors = _context.Authors.ToList();

            var model = new ProductViewModel();
            return View("~/Views/Admin/ProductManagement/CreateProduct.cshtml", model);
        }

        [HttpPost]
        public IActionResult CreateProduct(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imagePath = null;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(model.ImageFile.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        model.ImageFile.CopyTo(stream);
                    }

                    imagePath = "/images/" + fileName;
                }

                var author = _context.Authors.FirstOrDefault(a => a.AuthorName == model.AuthorName);
                if (author == null)
                {
                    author = new Author { AuthorName = model.AuthorName };
                    _context.Authors.Add(author);
                    _context.SaveChanges();
                }

                var category = _context.Categories.FirstOrDefault(c => c.CategoryName == model.CategoryName);
                if (category == null)
                {
                    category = new Category { CategoryName = model.CategoryName };
                    _context.Categories.Add(category);
                    _context.SaveChanges();
                }

                var product = new Product
                {
                    ProductName = model.ProductName,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    ImageUrl = imagePath,
                    AuthorId = author.AuthorId,
                    CategoryId = category.CategoryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                _context.SaveChanges();

                var productDetail = new ProductDetail
                {
                    Description = model.Description,
                    Publisher = model.Publisher,
                    PageCount = model.PageCount,
                    Language = model.Language,
                    ProductId = product.ProductId
                };

                _context.ProductDetails.Add(productDetail);
                _context.SaveChanges();

                return RedirectToAction("ProductManagement");
            }

            return View("~/Views/Admin/ProductManagement/CreateProduct.cshtml", model);
        }
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductDetail)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                AuthorName = product.Author?.AuthorName ?? "",
                CategoryName = product.Category?.CategoryName ?? "",
                Description = product.ProductDetail?.Description,
                Publisher = product.ProductDetail?.Publisher,
                PageCount = product.ProductDetail?.PageCount ?? 0,
                Language = product.ProductDetail?.Language
            };

            return View("~/Views/Admin/ProductManagement/CreateProduct.cshtml", model);
        }

        [HttpPost]
        public IActionResult EditProduct(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = _context.Products
                    .Include(p => p.ProductDetail)
                    .FirstOrDefault(p => p.ProductId == model.ProductId);

                if (product == null)
                    return NotFound();

                var author = _context.Authors.FirstOrDefault(a => a.AuthorName == model.AuthorName);
                if (author == null)
                {
                    author = new Author { AuthorName = model.AuthorName };
                    _context.Authors.Add(author);
                    _context.SaveChanges();
                }

                var category = _context.Categories.FirstOrDefault(c => c.CategoryName == model.CategoryName);
                if (category == null)
                {
                    category = new Category { CategoryName = model.CategoryName };
                    _context.Categories.Add(category);
                    _context.SaveChanges();
                }

                product.ProductName = model.ProductName;
                product.Price = model.Price;
                product.StockQuantity = model.StockQuantity;
                product.AuthorId = author.AuthorId;
                product.CategoryId = category.CategoryId;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(model.ImageFile.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        model.ImageFile.CopyTo(stream);
                    }

                    product.ImageUrl = "/images/" + fileName;
                }
                if (product.ProductDetail == null)
                {
                    product.ProductDetail = new ProductDetail
                    {
                        ProductId = product.ProductId
                    };
                }

                product.ProductDetail.Description = model.Description;
                product.ProductDetail.Publisher = model.Publisher;
                product.ProductDetail.PageCount = model.PageCount;
                product.ProductDetail.Language = model.Language;

                _context.Products.Update(product);
                _context.SaveChanges();

                return RedirectToAction("ProductManagement");
            }

            return View("~/Views/Admin/ProductManagement/CreateProduct.cshtml", model);
        }

        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductDetail)
                .FirstOrDefault(p => p.ProductId == id);

            if (product != null)
            {
                if (product.ProductDetail != null)
                {
                    _context.ProductDetails.Remove(product.ProductDetail);
                }

                _context.Products.Remove(product);
                _context.SaveChanges();
            }

            return RedirectToAction("ProductManagement");
        }

        public IActionResult ToggleProductStatus(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product != null)
            {
                product.IsActive = !product.IsActive; // ✅ đảo trạng thái
                _context.SaveChanges();
            }

            return RedirectToAction("ProductManagement");
        }

        // ------------------ ĐƠN HÀNG ------------------

        public IActionResult OrderManagement()
        {
            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ToList();

            return View(orders);
        }

        public IActionResult UpdateOrderStatus(int id, string status)
        {
            var order = _context.Orders.Find(id);
            if (order != null)
            {
                // Chuyển string sang enum
                if (Enum.TryParse<OrderStatus>(status, out var parsedStatus))
                {
                    order.Status = parsedStatus;
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("OrderManagement");
        }
        [HttpGet]
        public IActionResult OrderDetails(int id)
        {
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Payments) // nếu cần hiển thị thanh toán
                    .ThenInclude(p => p.PaymentHistories)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order); // Views/Admin/OrderDetails.cshtml
        }

        // ------------------ KHÁCH HÀNG ------------------

        public IActionResult CustomerManagement(string search)
        {
            var query = _context.Users.Where(u => u.Role == "Customer");

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Email.Contains(search));
            }

            var customers = query.ToList();
            return View(customers);
        }

        public IActionResult LockCustomer(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = false;
                _context.SaveChanges();
            }

            return RedirectToAction("CustomerManagement");
        }

        public IActionResult UnlockCustomer(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = true;
                _context.SaveChanges();
            }

            return RedirectToAction("CustomerManagement");
        }

        [HttpGet]
        public async Task<IActionResult> CustomerDetails(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.Payments)
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var vm = new CustomerDetailViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Orders = orders
            };

            return View("~/Views/Admin/CustomerDetails.cshtml", vm);
        }

        // -------------------- Reset password (GET: form) --------------------
        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new ResetPasswordViewModel
            {
                UserId = user.UserId,
                Email = user.Email
            };

            return View("~/Views/Admin/ResetPassword.cshtml", vm);
        }

        // -------------------- Reset password (POST) --------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Admin/ResetPassword.cshtml", model);

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null) return NotFound();

            // Nếu admin nhập mật khẩu mới và xác nhận khớp -> dùng mật khẩu đó
            string newPlain;
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                    return View("~/Views/Admin/ResetPassword.cshtml", model);
                }
                newPlain = model.NewPassword!;
            }
            else
            {
                // Nếu để trống -> sinh mật khẩu ngẫu nhiên
                newPlain = GenerateRandomPassword(10);
            }

            // Hash bằng PasswordHasher và lưu
            user.PasswordHash = _passwordHasher.HashPassword(user, newPlain);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Thông báo cho admin: bạn có thể hiển thị 1 lần (TempData) để copy hoặc gửi email
            TempData["Success"] = "Reset mật khẩu thành công.";
            TempData["NewPassword"] = newPlain; // **Chỉ dùng tạm thời**, KHÔNG in log trong production

            // Quay lại trang chi tiết khách
            return RedirectToAction(nameof(CustomerDetails), new { id = user.UserId });
        }

        // --------- Helper: sinh mật khẩu ngẫu nhiên (private) ----------
        private static string GenerateRandomPassword(int length = 10)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz123456789!@#$%^&*";
            var data = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);
            var sb = new StringBuilder(length);
            foreach (var b in data)
                sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }

        //-------------------Xóa người dùng---------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Không cho xóa chính admin đang đăng nhập
            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(currentUserIdClaim) && int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                if (currentUserId == id)
                {
                    TempData["Error"] = "Bạn không thể xóa chính mình.";
                    return RedirectToAction("CustomerManagement");
                }
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Người dùng không tồn tại.";
                return RedirectToAction("CustomerManagement");
            }

            // Không cho xóa user có role Manager (nếu bạn muốn)
            if (string.Equals(user.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Không thể xóa tài khoản quản trị (Manager).";
                return RedirectToAction("CustomerManagement");
            }

            // Xóa dữ liệu liên quan (tùy cấu trúc DB của bạn).
            // CartItems
            var carts = _context.CartItems.Where(c => c.UserId == id);
            _context.CartItems.RemoveRange(carts);

            // Payments và PaymentHistories
            var payments = await _context.Payments.Where(p => p.UserId == id).ToListAsync();
            if (payments.Any())
            {
                var paymentIds = payments.Select(p => p.PaymentId).ToList();
                var histories = _context.PaymentHistories.Where(ph => paymentIds.Contains(ph.PaymentId));
                _context.PaymentHistories.RemoveRange(histories);
                _context.Payments.RemoveRange(payments);
            }

            // Orders và OrderDetails
            var orders = await _context.Orders.Where(o => o.UserId == id).ToListAsync();
            if (orders.Any())
            {
                var orderIds = orders.Select(o => o.OrderId).ToList();
                var orderDetails = _context.OrderDetails.Where(od => orderIds.Contains(od.OrderId));
                _context.OrderDetails.RemoveRange(orderDetails);
                _context.Orders.RemoveRange(orders);
            }

            // Cuối cùng xóa user
            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa người dùng thành công.";
            }
            catch (Exception ex)
            {
                // Log ex nếu bạn có logging
                TempData["Error"] = "Xóa thất bại. Có thể có ràng buộc dữ liệu. " + ex.Message;
            }

            return RedirectToAction("CustomerManagement");
        }

        // ------------------ DOANH THU ------------------
        public async Task<IActionResult> RevenueManagement(int? year, int? month)
        {
            // 1) Lấy payments kèm order (materialize - in memory)
            var allPayments = await _context.Payments
                .Include(p => p.Order) // nếu có navigation
                .ToListAsync();

            // 2) Lọc: chỉ payments có Order != null, Order.Status == DaGiao, và có PaymentDate thực (dù PaymentDate là DateTime hay DateTime?)
            var deliveredPayments = allPayments
                .Where(p =>
                {
                    // kiểm tra order và trạng thái
                    if (p.Order == null) return false;
                    if (p.Order.Status != OrderStatus.DaGiao) return false;

                    // pattern matching: nếu PaymentDate chứa giá trị DateTime thì lấy dt
                    if (p.PaymentDate is DateTime dt)
                        return true; // có ngày -> giữ lại
                    return false; // null hoặc không phải DateTime
                })
                .ToList();

            // Nếu không có payments phù hợp thì trả view rỗng nhưng vẫn an toàn
            if (!deliveredPayments.Any())
            {
                ViewBag.TotalRevenue = 0m;
                ViewBag.AvailableYears = Enumerable.Empty<int>();
                ViewBag.SelectedYear = year ?? DateTime.Today.Year;
                ViewBag.SelectedMonth = month;
                ViewBag.YearlyRevenue = Enumerable.Empty<object>();
                ViewBag.MonthlyRevenue = Enumerable.Range(1, 12).Select(m => new { Year = DateTime.Today.Year, Month = m, Total = 0m }).ToList();
                ViewBag.DailyRevenue = Enumerable.Empty<object>();
                return View(new List<Payment>());
            }

            // 3) Tổng doanh thu (chỉ deliveredPayments)
            decimal totalRevenue = deliveredPayments.Sum(p => p.Amount);

            // 4) Danh sách năm có dữ liệu (sử dụng pattern matching an toàn)
            var availableYears = deliveredPayments
                .Select(p => ((DateTime)p.PaymentDate).Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            int selectedYear = year ?? (availableYears.Any() ? availableYears.First() : DateTime.Today.Year);
            int? selectedMonth = month; // null => tất cả

            // 5) Doanh thu theo năm (tổng mỗi năm)
            var yearlyRevenue = deliveredPayments
                .GroupBy(p => ((DateTime)p.PaymentDate).Year)
                .Select(g => new { Year = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderBy(g => g.Year)
                .ToList();

            // 6) Doanh thu theo tháng cho selectedYear (đảm bảo 12 tháng)
            var monthlyGroups = deliveredPayments
                .Where(p => ((DateTime)p.PaymentDate).Year == selectedYear)
                .GroupBy(p => ((DateTime)p.PaymentDate).Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(x => x.Amount) })
                .ToDictionary(x => x.Month, x => x.Total);

            var monthlyRevenue = Enumerable.Range(1, 12)
                .Select(m => new
                {
                    Year = selectedYear,
                    Month = m,
                    Total = monthlyGroups.ContainsKey(m) ? monthlyGroups[m] : 0m
                })
                .ToList();

            // 7) Nếu chọn month -> tính dailyRevenue và lấy payments cho tháng đó
            List<object> dailyRevenue;
            List<Payment> paymentsForSelectedPeriod;
            if (selectedMonth.HasValue && selectedMonth.Value >= 1 && selectedMonth.Value <= 12)
            {
                int sm = selectedMonth.Value;
                var dailyGroups = deliveredPayments
                    .Where(p => ((DateTime)p.PaymentDate).Year == selectedYear && ((DateTime)p.PaymentDate).Month == sm)
                    .GroupBy(p => ((DateTime)p.PaymentDate).Day)
                    .Select(g => new { Day = g.Key, Total = g.Sum(x => x.Amount) })
                    .ToDictionary(x => x.Day, x => x.Total);

                int daysInMonth = DateTime.DaysInMonth(selectedYear, sm);
                dailyRevenue = Enumerable.Range(1, daysInMonth)
                    .Select(d => (object)new { Day = d, Total = dailyGroups.ContainsKey(d) ? dailyGroups[d] : 0m })
                    .ToList();

                paymentsForSelectedPeriod = deliveredPayments
                    .Where(p => ((DateTime)p.PaymentDate).Year == selectedYear && ((DateTime)p.PaymentDate).Month == sm)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();
            }
            else
            {
                // Không chọn tháng -> trả về tất cả payments trong selectedYear
                dailyRevenue = new List<object>();
                paymentsForSelectedPeriod = deliveredPayments
                    .Where(p => ((DateTime)p.PaymentDate).Year == selectedYear)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();
            }

            // 8) Gán ViewBag và trả view
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.YearlyRevenue = yearlyRevenue;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.DailyRevenue = dailyRevenue;

            return View(paymentsForSelectedPeriod);
        }

    }

}

