using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class ChiTietDonHangLe
    {
        public int DonHangLeId { get; set; }
        public int NongSanId { get; set; }
        public int SoLuongDat { get; set; }
        public decimal? TrongLuongThucTe { get; set; }
        public decimal DonGiaThoiDiem { get; set; }
        public decimal? ThanhTienThucTe { get; set; }

        public virtual DonHangLe? DonHangLe { get; set; }
        public virtual NongSan? NongSan { get; set; }
    }
}
