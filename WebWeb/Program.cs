using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebWeb.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session tồn tại trong 30 phút rảnh
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Nếu cố tình vào trang yêu cầu đăng nhập của Customer mà chưa đăng nhập, sẽ đá về đây:
        options.LoginPath = "/Account/Login"; 
        options.AccessDeniedPath = "/Account/Login";
    })
    .AddCookie("AdminScheme", options =>
    {
        // SỬA Ở ĐÂY: Trỏ đúng vào Area Admin, Controller AdminAccount và Action Login
        options.LoginPath = "/Admin/AdminAccount/Login";
        options.AccessDeniedPath = "/Admin/AdminAccount/Login";
    })
    .AddCookie("ShipperScheme", options =>
    {
        // Đường dẫn đến trang đăng nhập của Shipper
        options.LoginPath = "/Shipper/ShipperAccount/Login";
        options.AccessDeniedPath = "/Shipper/ShipperAccount/Login";
    });

builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ECommerceDBContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
