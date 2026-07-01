using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class LoHang
    {
        public LoHang()
        {
            ChiTietBaoCaoHaoHuts = new HashSet<ChiTietBaoCaoHaoHut>();
        }

        public int LoHangId { get; set; }
        public int PhieuNhapId { get; set; }
        public int NongSanId { get; set; }
        public decimal DonGiaNhap { get; set; }
        public decimal SoLuongNhap { get; set; }
        public decimal SoLuongTon { get; set; }
        public DateTime? NgayThuHoach { get; set; }
        public DateTime NgayNhapKho { get; set; }
        public DateTime HanSuDung { get; set; }
        public string TrangThaiHsd { get; set; } = null!;

        public virtual NongSan NongSan { get; set; } = null!;
        public virtual PhieuNhapKho PhieuNhapKho { get; set; } = null!;
        public virtual ICollection<ChiTietBaoCaoHaoHut> ChiTietBaoCaoHaoHuts { get; set; }
    }
}
