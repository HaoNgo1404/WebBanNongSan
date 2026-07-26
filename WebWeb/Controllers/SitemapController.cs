using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml;
using WebWeb.Models;

namespace WebWeb.Controllers;

public class SitemapController : Controller
{
    private readonly ECommerceDBContext _context;

    public SitemapController(ECommerceDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("sitemap")] // Cấu hình đường dẫn cố định domain.com/sitemap.xml
    public async Task<IActionResult> Index()
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}";

        // 1. Lấy danh sách sản phẩm từ CSDL
        var products = await _context.NongSans
            .Select(p => new { p.NongSanId, p.TenNongSan })
            .ToListAsync();

        // 2. Lấy danh sách danh mục sản phẩm từ CSDL
        var categories = await _context.DanhMucs
            .Select(c => new { c.DanhMucId })
            .ToListAsync();

        // 3. Khởi tạo cấu trúc XML chuẩn Sitemap
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            Async = true
        };

        using var stringWriter = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            await xmlWriter.WriteStartDocumentAsync();
            xmlWriter.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // ----------------------------------------------------
            // A. THÊM CÁC TRANG CỐ ĐỊNH (Trang chủ, Giới thiệu...)
            // ----------------------------------------------------
            AddUrlToSitemap(xmlWriter, baseUrl, "/", "1.0", "daily");
            AddUrlToSitemap(xmlWriter, baseUrl, "/About", "0.5", "monthly");

            // ----------------------------------------------------
            // B. THÊM CÁC TRANG DANH MỤC SẢN PHẨM
            // ----------------------------------------------------
            foreach (var cat in categories)
            {
                AddUrlToSitemap(xmlWriter, baseUrl, $"/Product/DanhMuc/{cat.DanhMucId}", "0.8", "weekly");
            }

            // ----------------------------------------------------
            // C. THÊM TẤT CẢ TRANG CHI TIẾT SẢN PHẨM
            // ----------------------------------------------------
            foreach (var prod in products)
            {
                AddUrlToSitemap(xmlWriter, baseUrl, $"/Product/Detail/{prod.NongSanId}", "0.9", "daily");
            }

            await xmlWriter.WriteEndElementAsync(); // </urlset>
            await xmlWriter.WriteEndDocumentAsync();
        }

        // 4. Trả về định dạng application/xml
        return Content(stringWriter.ToString(), "application/xml", Encoding.UTF8);
    }

    // Hàm phụ trợ ghi từng thẻ <url>
    private void AddUrlToSitemap(XmlWriter writer, string baseUrl, string relativePath, string priority, string changeFreq)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", $"{baseUrl}{relativePath}");
        writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        writer.WriteElementString("changefreq", changeFreq);
        writer.WriteElementString("priority", priority);
        writer.WriteEndElement();
    }
}