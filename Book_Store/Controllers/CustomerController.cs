using Book_Store.Data;
using Book_Store.Models;
using Book_Store.Services;
using Book_Store.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Book_Store.Controllers
{
    public class CustomerController : Controller
    {
        private readonly BookStoreDbContext _context;
        private readonly IVnPayService _vnPayService;

        public CustomerController(BookStoreDbContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        // ===================================================================
        // Helper: Lấy UserId từ Claims (dùng chung cho toàn controller)
        // ===================================================================
        private async Task<int?> GetCurrentUserIdAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return null;

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            return user?.UserId;
        }

        private async Task<int> GetCartItemCount()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return 0;

            return await _context.CartItems
                .Where(c => c.UserId == userId.Value)
                .SumAsync(c => (int?)c.Quantity) ?? 0;
        }

        private async Task SetCartCountAsync()
        {
            ViewData["CartCount"] = await GetCartItemCount();
        }

        // ===================================================================
        // Các action trang chủ, sản phẩm, giỏ hàng...
        // ===================================================================
        public async Task<IActionResult> Index()
        {
            await SetCartCountAsync();
            var featuredBooks = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View(featuredBooks);
        }

        public async Task<IActionResult> ProductList(string search, int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(search) ||
                    p.Author.AuthorName.ToLower().Contains(search) ||
                    p.Category.CategoryName.ToLower().Contains(search)
                );
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            await SetCartCountAsync();
            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.ProductDetail)
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null)
                return NotFound();

            await SetCartCountAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                var returnUrl = Url.Action("ProductDetail", "Customer", new { id = productId });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity
                };
                await _context.CartItems.AddAsync(cartItem);
            }

            await _context.SaveChangesAsync();

            var referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("ProductDetail", new { id = productId });
        }

        public async Task<IActionResult> Cart()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            await SetCartCountAsync();
            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var items = await _context.CartItems.Where(c => c.UserId == userId.Value).ToListAsync();
            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decrement(int productId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);

            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                }
                else
                {
                    _context.CartItems.Remove(item);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Cart));
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            await SetCartCountAsync();
            return View(cartItems);
        }

        // ===================================================================
        // Xử lý thanh toán COD, Chuyển khoản, PayPal (tạo đơn ngay lập tức)
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string address, string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod) || paymentMethod == "VNPAY")
                return RedirectToAction("Cart"); // Không xử lý VNPAY ở đây

            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            decimal totalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity);

            var order = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.DangXuLy,
                TotalAmount = totalAmount,
                ShippingAddress = address
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }

            var payment = new Payment
            {
                OrderId = order.OrderId,
                UserId = userId.Value,
                Amount = totalAmount,
                PaymentMethod = paymentMethod,
                PaymentDate = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var paymentHistory = new PaymentHistory
            {
                PaymentId = payment.PaymentId,
                TransactionDate = DateTime.UtcNow,
                Status = "Success",
                Notes = $"Thanh toán đơn hàng #{order.OrderId} thành công ({paymentMethod})."
            };
            _context.PaymentHistories.Add(paymentHistory);

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt hàng thành công!";
            return RedirectToAction("OrderHistory");
        }

        // ===================================================================
        // Tạo URL thanh toán VNPAY (chuyển hướng sang cổng VNPAY)
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePaymentVnpay(PaymentInformationModel model, string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                TempData["Error"] = "Vui lòng nhập địa chỉ giao hàng.";
                return RedirectToAction("Checkout");
            }

            // Lưu tạm địa chỉ vào Session để dùng khi return
            HttpContext.Session.SetString("PendingOrderAddress", address);

            model.OrderDescription = "Thanh toan don hang tai Book Store";
            model.Name = "Khách hàng";

            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Redirect(url);
        }

        // ===================================================================
        // Xử lý return từ VNPAY (chỉ tạo đơn khi thanh toán thành công)
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> VnpayReturn()
        {
            var collections = Request.Query;
            var response = _vnPayService.PaymentExecute(collections);

            if (response.Success && response.VnPayResponseCode == "00")
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId.Value)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống hoặc đã được xử lý.";
                    return RedirectToAction("Cart");
                }

                string address = HttpContext.Session.GetString("PendingOrderAddress") ?? "Chưa cung cấp địa chỉ";
                decimal totalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);

                // Tạo đơn hàng
                var order = new Order
                {
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.DangXuLy,
                    ShippingAddress = address,
                    UserId = userId.Value
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price
                    });
                }

                // Ghi lịch sử thanh toán VNPAY
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    UserId = userId.Value,
                    Amount = totalAmount,
                    PaymentMethod = "VNPAY",
                    PaymentDate = DateTime.UtcNow,
                    TransactionId = response.TransactionId,
                    PaypalOrderId = null
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                var paymentHistory = new PaymentHistory
                {
                    PaymentId = payment.PaymentId,
                    TransactionDate = DateTime.UtcNow,
                    Status = "Success",
                    Notes = $"Thanh toán VNPAY thành công. Mã giao dịch: {response.TransactionId}"
                };
                _context.PaymentHistories.Add(paymentHistory);

                // Xóa giỏ hàng và session tạm
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("PendingOrderAddress");

                TempData["Success"] = $"Thanh toán VNPAY thành công! Mã giao dịch: {response.TransactionId}";
                return RedirectToAction("OrderHistory");
            }
            else
            {
                TempData["Error"] = $"Thanh toán VNPAY thất bại: {response.Message}";
                return RedirectToAction("Cart");
            }
        }

        // ===================================================================
        // Lịch sử đơn hàng, tìm kiếm, profile...
        // ===================================================================
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            await SetCartCountAsync();
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            var query = _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(keyword) ||
                    p.Author.AuthorName.ToLower().Contains(keyword) ||
                    p.Category.CategoryName.ToLower().Contains(keyword)
                );
            }

            await SetCartCountAsync();
            return View("ProductList", await query.ToListAsync());
        }

        public async Task<IActionResult> SearchProducts(string search)
        {
            var query = _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(search) ||
                    p.Author.AuthorName.ToLower().Contains(search) ||
                    p.Category.CategoryName.ToLower().Contains(search)
                );
            }

            var products = await query.ToListAsync();
            return PartialView("_ProductListPartial", products);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return NotFound();

            var model = new EditProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address,
                Phone = user.Phone
            };

            await SetCartCountAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await SetCartCountAsync();
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == model.UserId);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Address = model.Address;
            user.Phone = model.Phone;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile"); // hoặc trang profile nếu có
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { results = new object[0] });

            query = query.ToLower();

            var results = await _context.Products
                .Where(p => p.IsActive && p.ProductName.ToLower().Contains(query))
                .OrderBy(p => p.ProductName)
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.ProductName,
                    image = p.ImageUrl
                })
                .Take(5)
                .ToListAsync();

            return Json(new { results });
        }
    }
}