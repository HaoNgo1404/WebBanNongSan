using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class ChiTietGioHang
    {
        public int KhachHangId { get; set; }
        public int NongSanId { get; set; }
        public decimal SoLuong { get; set; }
        public DateTime NgayCapNhat { get; set; }

        public virtual KhachHang KhachHang { get; set; } = null!;
        public virtual NongSan NongSan { get; set; } = null!;
    }
}
