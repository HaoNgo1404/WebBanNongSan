using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class YeuThich
    {
        public int KhachHangId { get; set; }
        public int NongSanId { get; set; }
        public DateTime? NgayThem { get; set; }

        public virtual KhachHang KhachHang { get; set; } = null!;
        public virtual NongSan NongSan { get; set; } = null!;
    }
}
