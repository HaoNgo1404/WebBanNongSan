using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebWeb.Models;
using WebWeb.Services;
using System.Text;

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

builder.Services.AddScoped<KhuyenMaiService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<AISentimentService>();
builder.Services.AddScoped<AISentimentService>();
builder.Services.AddHttpClient<AISearchService>();
builder.Services.AddScoped<AISearchService>();

builder.Services.AddHttpClient();

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

app.MapControllers();

app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 🟢 ROUTE SITEMAP CHUẨN ĐỊNH DẠNG XML CHO GOOGLE SEARCH CONSOLE
app.MapGet("/sitemap", async (HttpContext context, ECommerceDBContext db) =>
{
    string baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

    var products = await db.NongSans.Select(p => p.NongSanId).ToListAsync();
    var categories = await db.DanhMucs.Select(c => c.DanhMucId).ToListAsync();

    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

    // 1. Trang cố định
    sb.AppendLine($"  <url>");
    sb.AppendLine($"    <loc>{baseUrl}/</loc>");
    sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
    sb.AppendLine($"    <changefreq>daily</changefreq>");
    sb.AppendLine($"    <priority>1.0</priority>");
    sb.AppendLine($"  </url>");

    sb.AppendLine($"  <url>");
    sb.AppendLine($"    <loc>{baseUrl}/About</loc>");
    sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
    sb.AppendLine($"    <changefreq>monthly</changefreq>");
    sb.AppendLine($"    <priority>0.5</priority>");
    sb.AppendLine($"  </url>");

    // 2. Trang Danh mục
    foreach (var catId in categories)
    {
        sb.AppendLine($"  <url>");
        sb.AppendLine($"    <loc>{baseUrl}/Product/DanhMuc/{catId}</loc>");
        sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
        sb.AppendLine($"    <changefreq>weekly</changefreq>");
        sb.AppendLine($"    <priority>0.8</priority>");
        sb.AppendLine($"  </url>");
    }

    // 3. Trang Sản phẩm
    foreach (var prodId in products)
    {
        sb.AppendLine($"  <url>");
        sb.AppendLine($"    <loc>{baseUrl}/Product/Detail/{prodId}</loc>");
        sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
        sb.AppendLine($"    <changefreq>daily</changefreq>");
        sb.AppendLine($"    <priority>0.9</priority>");
        sb.AppendLine($"  </url>");
    }

    sb.AppendLine("</urlset>");

    // Trả về chuẩn Content-Type là application/xml để Google nhận diện ngay lập tức
    return Results.Text(sb.ToString(), "application/xml", Encoding.UTF8);
});

app.Run();
