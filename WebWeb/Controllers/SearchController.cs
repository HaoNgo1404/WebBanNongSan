using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models;
using WebWeb.Services; // 🟢 THÊM NAMESPACE SERVICE AI

namespace WebWeb.Controllers
{
    public class SearchController : Controller
    {
        private readonly ECommerceDBContext _context;
        private readonly AISearchService _aiSearchService; // 🟢 KHAI BÁO AI SERVICE

        public SearchController(ECommerceDBContext context, AISearchService aiSearchService) // 🟢 INJECT SERVICE
        {
            _context = context;
            _aiSearchService = aiSearchService;
        }

        // TRANG KẾT QUẢ TÌM KIẾM CHÍNH (CÓ THÊM BỘ LỌC + AI)
        public async Task<IActionResult> Index(string keyword, int? danhMucId, string khoangGia)
        {
            ViewBag.Keyword = keyword;
            ViewBag.SelectedDanhMuc = danhMucId;
            ViewBag.SelectedKhoangGia = khoangGia;
            ViewBag.DanhMucList = await _context.DanhMucs.ToListAsync();
            ViewBag.IsAISearch = false;

            var query = _context.NongSans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // 🟢 1. THỬ TÌM KIẾM BẰNG AI PHÂN TÍCH Ý ĐỊNH
                List<int> aiProductIds = await _aiSearchService.LayDanhSachIdGoiYAsync(keyword);

                if (aiProductIds.Any())
                {
                    // Nếu AI trả về ID gợi ý phù hợp -> Ưu tiên lấy sản phẩm theo AI
                    query = query.Where(n => aiProductIds.Contains(n.NongSanId));
                    ViewBag.IsAISearch = true; // Cờ đánh dấu để hiển thị Badge AI trên View nếu muốn
                }
                else
                {
                    // 🔴 FALLBACK: Lọc SQL Contains truyền thống nếu AI không trả về kết quả
                    string searchKey = keyword.Trim().ToLower();
                    query = query.Where(n => n.TenNongSan.ToLower().Contains(searchKey) 
                                          || (n.MoTa != null && n.MoTa.ToLower().Contains(searchKey)));
                }
            }

            // 2. Lọc theo Danh mục sản phẩm (kết hợp chung với kết quả AI)
            if (danhMucId.HasValue)
            {
                query = query.Where(n => n.DanhMucId == danhMucId.Value);
            }

            // 3. Lọc theo Khoảng giá bán
            if (!string.IsNullOrWhiteSpace(khoangGia))
            {
                switch (khoangGia)
                {
                    case "under100":
                        query = query.Where(n => n.GiaBanNiemYet < 100000);
                        break;
                    case "100to300":
                        query = query.Where(n => n.GiaBanNiemYet >= 100000 && n.GiaBanNiemYet <= 300000);
                        break;
                    case "over300":
                        query = query.Where(n => n.GiaBanNiemYet > 300000);
                        break;
                }
            }

            var ketQua = await query.ToListAsync();
            return View(ketQua);
        }

        // Action Suggest hiển thị gợi ý nhanh khi đang gõ
        public async Task<IActionResult> Suggest(string keyword)
        {
            IEnumerable<NongSan> items;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                // Mặc định lấy 5 sản phẩm
                items = await _context.NongSans.Take(5).ToListAsync();
            }
            else
            {
                // 🟢 THỬ GỢI Ý BẰNG AI KHI ĐANG GÕ
                List<int> aiProductIds = await _aiSearchService.LayDanhSachIdGoiYAsync(keyword);

                if (aiProductIds.Any())
                {
                    items = await _context.NongSans
                                          .Where(x => aiProductIds.Contains(x.NongSanId))
                                          .Take(5)
                                          .ToListAsync();
                }
                else
                {
                    // Fallback lọc SQL
                    items = await _context.NongSans
                                          .Where(x => x.TenNongSan.Contains(keyword))
                                          .Take(5)
                                          .ToListAsync();
                }
            }

            return PartialView("_SearchSuggestion", items);
        }
    }
}