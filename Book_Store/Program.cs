using Book_Store.Data;
using Book_Store.Hubs;
using Book_Store.Models;
using Book_Store.Services;
using Book_Store.Services.Vnpay;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);




// Cấu hình Email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);
builder.Services.AddScoped<EmailService>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddSignalR();

//Connect VNPay API
builder.Services.AddScoped<IVnPayService, VnPayService>();

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

// ✅ Route mặc định trỏ đến CustomerController → ProductList
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Customer}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");
app.Run();
