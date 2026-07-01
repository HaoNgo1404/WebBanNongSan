using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class KhuyenMai
    {
        public int KhuyenMaiId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public string TenChuongTrinh { get; set; } = null!;
        public int LoaiGiamGia { get; set; }
        public decimal MucGiam { get; set; }
        public decimal GiaTriDonToiThieu { get; set; }
        public decimal SoTienGiamToiDa { get; set; }
        public int SoLuotPhatHanh { get; set; }
        public int SoLuotDaDung { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public bool? TrangThai { get; set; }
    }
}
