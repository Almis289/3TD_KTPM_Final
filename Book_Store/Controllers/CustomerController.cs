﻿using Book_Store.Data;
using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Book_Store.Controllers
{
    public class CustomerController : Controller
    {
        private readonly BookStoreDbContext _context;

        public CustomerController(BookStoreDbContext context)
        {
            _context = context;
        }

        // 🔹 Lấy userId từ Claims
        private async Task<int?> GetCurrentUserIdAsync()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
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

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(search) ||
                    p.Author.AuthorName.ToLower().Contains(search) ||
                    p.Category.CategoryName.ToLower().Contains(search)
                );
            }

            // Lọc theo thể loại
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Đẩy danh sách thể loại sang View để tạo dropdown
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
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                // Nếu chưa login -> redirect tới Login kèm returnUrl quay lại ProductDetail
                var returnUrl = Url.Action("ProductDetail", "Customer", new { id = productId });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                await _context.CartItems.AddAsync(new CartItem
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);

            return RedirectToAction("ProductDetail", new { id = productId });
        }

        public async Task<IActionResult> Cart()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login");

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
            if (userId == null) return RedirectToAction("Login");

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
            if (userId == null) return RedirectToAction("Login");

            var items = await _context.CartItems
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

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
            if (userId == null) return RedirectToAction("Login");

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);

            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                    _context.CartItems.Update(item);
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
            if (userId == null) return RedirectToAction("Login");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            await SetCartCountAsync();
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string address, string paymentMethod)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            decimal totalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity);
            
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                paymentMethod = "COD";
            }

            var order = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.DangXuLy,
                TotalAmount = totalAmount,
                ShippingAddress = address // <- lưu địa chỉ
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                _context.OrderDetails.Add(orderDetail);
            }

            var payment = new Payment
            {
                OrderId = order.OrderId,
                UserId = userId.Value,
                Amount = totalAmount,
                PaymentMethod = paymentMethod, // <- từ form
                PaymentDate = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var paymentHistory = new PaymentHistory
            {
                PaymentId = payment.PaymentId,
                TransactionDate = DateTime.UtcNow,
                Status = "Success",
                Notes = $"Thanh toán đơn hàng #{order.OrderId} thành công."
            };
            _context.PaymentHistories.Add(paymentHistory);

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderHistory");
        }
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return RedirectToAction("Login");

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
            if (userId == null) return RedirectToAction("Login");

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

            return RedirectToAction("Profile");
        }

    }
}
