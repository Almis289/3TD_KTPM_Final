using Book_Store.Data;
using Book_Store.Models;
using Book_Store.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Book_Store.Controllers
{
    public class AccountController : Controller
    {
        private readonly BookStoreDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(BookStoreDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra email tồn tại
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại");
                return View(model);
            }

            // Tạo user mới (chỉ gán các trường cần thiết)
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Hash mật khẩu bằng PasswordHasher của Identity
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // truyền returnUrl xuống view để đặt hidden input
            ViewData["ReturnUrl"] = returnUrl; // Lưu returnUrl
            return View();
        }


        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng";
                return View();
            }

            // Verify bằng PasswordHasher
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng";
                return View();
            }

            // Nếu verify trả về SuccessRehashNeeded, hash lại bằng hasher hiện tại và lưu
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.UserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            /// Nếu returnUrl hợp lệ thì redirect về đó
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.Role == "Manager" ? RedirectToAction("Index", "Admin")
                                           : RedirectToAction("Index", "Customer");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // ✅ Hồ sơ người dùng
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                return RedirectToAction("Login");

            var cartCount = await _context.CartItems
                .Where(c => c.UserId == user.UserId)
                .SumAsync(c => (int?)c.Quantity) ?? 0;

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.PaymentHistories)
                .Where(o => o.UserId == user.UserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var model = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                CartCount = cartCount,
                AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl)
                    ? "https://cdn-icons-png.flaticon.com/512/149/149071.png"
                    : user.AvatarUrl,
                Orders = orders
            };

            return View(model);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                return RedirectToAction("Login");

            var model = new EditProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
                return RedirectToAction("Login");

            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.Address = model.Address;
            // Nếu có file ảnh mới
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                // cấu hình dung lượng tối đa (MB) - đổi số ở đây nếu muốn
                int maxMb = 10;
                var maxBytes = (long)maxMb * 1024 * 1024;

                // Validate cơ bản (kiểm tra ContentType đúng)
                if (!model.AvatarFile.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận file ảnh.");
                    return View(model);
                }
                if (model.AvatarFile.Length > maxBytes)
                {
                    ModelState.AddModelError("AvatarFile", $"Ảnh tối đa {maxMb}MB.");
                    return View(model);
                }

                // Lấy IWebHostEnvironment từ DI runtime (không cần inject trong constructor)
                var env = HttpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
                if (env == null)
                {
                    ModelState.AddModelError("", "Không thể xác định thư mục upload trên server.");
                    return View(model);
                }

                // Thư mục lưu ảnh trong wwwroot/images
                var uploadsFolder = Path.Combine(env.WebRootPath ?? ".", "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Tạo tên file duy nhất, an toàn
                var ext = Path.GetExtension(model.AvatarFile.FileName);
                var safeExt = string.IsNullOrWhiteSpace(ext) ? ".jpg" : ext;
                var fileName = $"avatar_{user.UserId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{safeExt}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    // Lưu file
                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await model.AvatarFile.CopyToAsync(stream);
                    }

                    // Xóa file cũ (nếu đường dẫn cũ nằm trong /images)
                    if (!string.IsNullOrWhiteSpace(user.AvatarUrl) &&
                        user.AvatarUrl.StartsWith("/images", StringComparison.OrdinalIgnoreCase))
                    {
                        var oldRelative = user.AvatarUrl.TrimStart('/');
                        var oldPath = Path.Combine(env.WebRootPath ?? ".", oldRelative.Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(oldPath))
                        {
                            try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                        }
                    }

                    // Lưu đường dẫn public (relative)
                    user.AvatarUrl = $"/images/{fileName}";
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Lưu ảnh thất bại. Vui lòng thử lại.");
                    return View(model);
                }
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp");
                return View(model);
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                return RedirectToAction("Login");

            // Verify current password bằng PasswordHasher
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng");
                return View(model);
            }

            // Nếu verify trả về SuccessRehashNeeded, ta vẫn coi là hợp lệ — có thể rehash lại
            if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                // optional: nothing needed now (we will rehash new password below)
            }

            // Hash mật khẩu mới bằng PasswordHasher và lưu
            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

    }
}
