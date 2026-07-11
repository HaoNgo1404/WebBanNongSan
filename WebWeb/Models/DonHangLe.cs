using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class DonHangLe
    {
        public DonHangLe()
        {
            ChiTietDonHangLes = new HashSet<ChiTietDonHangLe>();
            DanhGiaSanPhams = new HashSet<DanhGiaSanPham>();
            GiaoDichThanhToans = new HashSet<GiaoDichThanhToan>();
            KhieuNais = new HashSet<KhieuNai>();
        }

        public int DonHangLeId { get; set; }
        public int? KhachHangId { get; set; }
        public int? DiaChiId { get; set; }
        public int? NhanVienId { get; set; }
        public int? KhuyenMaiId { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TongTienTamTinh { get; set; }
        public decimal? TongTienThucTe { get; set; }
        public decimal? TienChenhLech { get; set; }
        public string TrangThaiThanhToan { get; set; } = null!;
        public string PhuongThucThanhToan { get; set; } = null!;
        public string KhungGioGiaoHang { get; set; } = null!;
        public string TrangThaiDonHang { get; set; } = null!;
        public string? PhoneNonAccount { get; set; }
        public string? NameCusNonAccount { get; set; }
        public string? AddressNonAccount { get; set; }

        public virtual SoDiaChi? DiaChi { get; set; }
        public virtual KhachHang? KhachHang { get; set; }
        public virtual NhanVien? NhanVien { get; set; }
        public virtual KhuyenMai? KhuyenMai { get; set; }
        public virtual ICollection<ChiTietDonHangLe>? ChiTietDonHangLes { get; set; }
        public virtual ICollection<DanhGiaSanPham>? DanhGiaSanPhams { get; set; }
        public virtual ICollection<GiaoDichThanhToan>? GiaoDichThanhToans { get; set; }
        public virtual ICollection<KhieuNai>? KhieuNais { get; set; }
    }
}
