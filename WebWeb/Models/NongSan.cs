using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class NongSan
    {
        public NongSan()
        {
            ChiTietDonHangLes = new HashSet<ChiTietDonHangLe>();
            ChiTietGioHangs = new HashSet<ChiTietGioHang>();
            ChiTietGoiDinhKies = new HashSet<ChiTietGoiDinhKy>();
            DanhGiaSanPhams = new HashSet<DanhGiaSanPham>();
            LoHangs = new HashSet<LoHang>();
            YeuThiches = new HashSet<YeuThich>();
            KhuyenMais = new HashSet<KhuyenMai>();
        }

        public int NongSanId { get; set; }
        public string TenNongSan { get; set; } = null!;
        public string? MoTa { get; set; }
        public string? HinhAnh { get; set; }
        public decimal GiaBanNiemYet { get; set; }
        public string DonViTinh { get; set; } = null!;
        public int SaiSoChoPhep { get; set; }
        public int DanhMucId { get; set; }
        public int NhaVuonId { get; set; }

        public virtual DanhMuc? DanhMuc { get; set; }
        public virtual NhaVuon? NhaVuon { get; set; }
        public virtual ICollection<KhuyenMai>? KhuyenMais { get; set; }
        public virtual ICollection<ChiTietDonHangLe>? ChiTietDonHangLes { get; set; }
        public virtual ICollection<ChiTietGioHang>? ChiTietGioHangs { get; set; }
        public virtual ICollection<ChiTietGoiDinhKy>? ChiTietGoiDinhKies { get; set; }
        public virtual ICollection<DanhGiaSanPham>? DanhGiaSanPhams { get; set; }
        public virtual ICollection<LoHang>? LoHangs { get; set; }
        public virtual ICollection<YeuThich>? YeuThiches { get; set; }
    }
}
