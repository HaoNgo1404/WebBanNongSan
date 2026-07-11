using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class KhachHang
    {
        public KhachHang()
        {
            ChiTietGioHangs = new HashSet<ChiTietGioHang>();
            DanhGiaSanPhams = new HashSet<DanhGiaSanPham>();
            DonHangLes = new HashSet<DonHangLe>();
            GoiDangKyDinhKies = new HashSet<GoiDangKyDinhKy>();
            KhieuNais = new HashSet<KhieuNai>();
            SoDiaChis = new HashSet<SoDiaChi>();
            YeuThiches = new HashSet<YeuThich>();
        }

        public int KhachHangId { get; set; }
        public string HoTen { get; set; } = null!;
        public string SoDienThoai { get; set; } = null!;
        public string? Email { get; set; }
        public string MatKhauMaHoa { get; set; } = null!;
        public DateTime NgayDangKy { get; set; }
        public int DiemTichLuy { get; set; }

        public virtual ICollection<ChiTietGioHang>? ChiTietGioHangs { get; set; }
        public virtual ICollection<DanhGiaSanPham>? DanhGiaSanPhams { get; set; }
        public virtual ICollection<DonHangLe>? DonHangLes { get; set; }
        public virtual ICollection<GoiDangKyDinhKy>? GoiDangKyDinhKies { get; set; }
        public virtual ICollection<KhieuNai>? KhieuNais { get; set; }
        public virtual ICollection<SoDiaChi>? SoDiaChis { get; set; }
        public virtual ICollection<YeuThich>? YeuThiches { get; set; }
    }
}
