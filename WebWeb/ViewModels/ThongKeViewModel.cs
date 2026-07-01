using System.Collections.Generic;

namespace WebWeb.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        // Các số liệu thẻ tổng quan (Top Cards)
        public decimal TongDoanhThu { get; set; }
        public int SoDonHangMoi { get; set; }
        public int SoKhachHang { get; set; }
        public decimal TongLoiNhuan { get; set; }

        // Dữ liệu cho biểu đồ cột: Doanh thu theo tháng (12 tháng)
        public List<string> LabelThangs { get; set; } = new List<string>();
        public List<decimal> DataDoanhThuThang { get; set; } = new List<decimal>();

        // Dữ liệu cho biểu đồ tròn: Top 5 nông sản bán chạy
        public List<string> TopNongSanNames { get; set; } = new List<string>();
        public List<int> TopNongSanQuantities { get; set; } = new List<int>();
    }

    public class ThongKeViewModel
    {
        // --- Bộ lọc (Filters) ---
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? LoaiGiaoDich { get; set; } // "TatCa", "DonHangLe", "GoiDinhKy"
        public string? TrangThai { get; set; }

        // --- Dữ liệu thống kê tổng hợp (Cards) ---
        public decimal TongDoanhThu { get; set; }
        public int TongDonHangLe { get; set; }
        public int TongGoiDinhKy { get; set; }

        // --- Danh sách kết quả hiển thị sau khi lọc ---
        public List<KetQuaThongKeItem> DanhSachKetQua { get; set; } = new List<KetQuaThongKeItem>();
    }

    public class KetQuaThongKeItem
    {
        public string MaGiaoDich { get; set; } = null!;
        public string Loai { get; set; } = null!; // "Đơn hàng lẻ" hoặc "Gói định kỳ"
        public DateTime NgayGiaoDich { get; set; }
        public string KhachHang { get; set; } = null!;
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; } = null!;
        public string TrangThai { get; set; } = null!;
    }
}