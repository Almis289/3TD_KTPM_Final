using Book_Store.Data;
using Book_Store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Controllers
{
    public class ProductController : Controller
    {
        private readonly BookStoreDbContext _context;

        public ProductController(BookStoreDbContext context)
        {
            _context = context;
        }

        // ✅ Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .ToListAsync();

            return View(products);
        }

        // ✅ Hiển thị chi tiết sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.ProductDetail)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // ✅ Thêm sản phẩm vào giỏ hàng (chỉ khi đã đăng nhập)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId.Value);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    ProductId = productId,
                    UserId = userId.Value,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Cart");
        }

        // ✅ Hiển thị giỏ hàng (chỉ khi đã đăng nhập)
        public async Task<IActionResult> Cart()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId.Value)
                .Include(c => c.Product)
                .ToListAsync();

            return View(cartItems);
        }

        // ✅ Xóa sản phẩm khỏi giỏ hàng (chỉ khi là chủ sở hữu)
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null && cartItem.UserId == userId.Value)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Cart");
        }

        // ✅ Lấy ID người dùng hiện tại một cách an toàn
        private int? GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }

            return null;
        }

        [HttpGet("Product/Detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductDetail)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View("ProductDetail", product); // ← tên file View ở đây
        }
    }
}