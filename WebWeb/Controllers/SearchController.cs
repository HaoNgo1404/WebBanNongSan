using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    public class SearchController : Controller
    {
        private readonly ECommerceDBContext _context;

        public SearchController(ECommerceDBContext context)
        {
            _context = context;
        }

        // TRANG KẾT QUẢ TÌM KIẾM CHÍNH (CÓ THÊM BỘ LỌC)
        public async Task<IActionResult> Index(string keyword, int? danhMucId, string khoangGia)
        {
            // Bỏ dữ liệu ngược lại ViewBag để giữ trạng thái đã chọn trên giao diện
            ViewBag.Keyword = keyword;
            ViewBag.SelectedDanhMuc = danhMucId;
            ViewBag.SelectedKhoangGia = khoangGia;

            // Lấy danh sách danh mục để hiển thị lên thẻ select ở bộ lọc
            // Hào lưu ý kiểm tra xem bảng danh mục của bạn tên là LoaiNongSans hay DanhMucs nhé
            ViewBag.DanhMucList = await _context.DanhMucs.ToListAsync(); 

            var query = _context.NongSans.AsQueryable();

            // 1. Lọc theo từ khóa tìm kiếm (Tên hoặc mô tả)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string searchKey = keyword.Trim().ToLower();
                query = query.Where(n => n.TenNongSan.ToLower().Contains(searchKey) 
                                      || (n.MoTa != null && n.MoTa.ToLower().Contains(searchKey)));
            }

            // 2. Lọc theo Danh mục sản phẩm (nếu khách chọn)
            if (danhMucId.HasValue)
            {
                query = query.Where(n => n.DanhMucId == danhMucId.Value); // Hào kiểm tra lại tên trường khóa ngoại nhé (LoaiId hoặc LoaiNongSanId)
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

        // Action Suggest giữ nguyên...
        public async Task<IActionResult> Suggest(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return Content("");
            string searchKey = keyword.Trim().ToLower();
            var goiY = await _context.NongSans
                .Where(n => n.TenNongSan.ToLower().Contains(searchKey))
                .Take(5)
                .ToListAsync();
            return PartialView("_SearchSuggestion", goiY);
        }
    }
}