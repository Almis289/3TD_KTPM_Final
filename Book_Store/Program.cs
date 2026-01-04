using Book_Store.Data;
using Book_Store.Hubs;
using Book_Store.Models;
using Book_Store.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Book_Store.Services.Implement;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<IVnPayService, VnPayService>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddSignalR();

// ✅ Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

// ✅ Kích hoạt session
builder.Services.AddSession();

// ✅ Cấu hình xác thực bằng Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";               // Trang đăng nhập
        options.AccessDeniedPath = "/Account/AccessDenied"; // Trang từ chối truy cập
    });

builder.Services.AddAuthorization();

// ✅ Cấu hình DbContext
builder.Services.AddDbContext<BookStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ✅ Cấu hình middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();           // Kích hoạt session
app.UseAuthentication();    // Kích hoạt xác thực
app.UseAuthorization();     // Kích hoạt phân quyền

// URL friendly cho sản phẩm
app.MapControllerRoute(
    name: "customer-product-detail",
    pattern: "sach/{slug}",
    defaults: new { controller = "Customer", action = "ProductDetail" }
);

// Route mặc định trỏ đến CustomerController → ProductList
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Customer}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");
app.Run();
