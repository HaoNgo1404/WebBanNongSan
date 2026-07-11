using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class PhieuChiCongNo
    {
        public int PhieuChiId { get; set; }
        public int NhaVuonId { get; set; }
        public int NhanVienId { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal SoTienThucChi { get; set; }
        public int PhuongThuc { get; set; }
        public string? MaGiaoDich { get; set; }

        public virtual NhaVuon? NhaVuon { get; set; }
        public virtual NhanVien? NhanVien { get; set; }
    }
}
