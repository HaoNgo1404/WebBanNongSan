using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class PhieuNhapKho
    {
        public PhieuNhapKho()
        {
            LoHangs = new HashSet<LoHang>();
        }

        public int PhieuNhapId { get; set; }
        public int NhaVuonId { get; set; }
        public int NhanVienId { get; set; }
        public DateTime NgayLapPhieu { get; set; }
        public decimal TongTienNhap { get; set; }

        public virtual NhaVuon NhaVuon { get; set; } = null!;
        public virtual NhanVien NhanVien { get; set; } = null!;
        public virtual ICollection<LoHang> LoHangs { get; set; }
    }
}
