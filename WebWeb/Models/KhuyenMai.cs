using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class KhuyenMai
    {
        public KhuyenMai()
        {
            DonHangLes = new HashSet<DonHangLe>();
            GoiDangKyDinhKies = new HashSet<GoiDangKyDinhKy>();
        }
        public int KhuyenMaiId { get; set; }
        public int? NongSanId { get; set; }
        public int? DanhMucId { get; set; }
        public string? VoucherCode { get; set; }
        public string TenChuongTrinh { get; set; } = null!;
        public int LoaiGiamGia { get; set; }
        public decimal MucGiam { get; set; }
        public decimal GiaTriDonToiThieu { get; set; }
        public decimal SoTienGiamToiDa { get; set; }
        public int SoLuotPhatHanh { get; set; }
        public int SoLuotDaDung { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public bool TrangThai { get; set; }

        public virtual NongSan? NongSan { get; set; }
        public virtual DanhMuc? DanhMuc { get; set; }
        public virtual ICollection<DonHangLe> DonHangLes { get; set; }
        public virtual ICollection<GoiDangKyDinhKy> GoiDangKyDinhKies { get; set; }
    }
}
