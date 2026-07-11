using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class ChiTietGoiDinhKy
    {
        public int GoiId { get; set; }
        public int NongSanId { get; set; }
        public decimal SoLuongMoiDot { get; set; }

        public virtual GoiDangKyDinhKy? Goi { get; set; }
        public virtual NongSan? NongSan { get; set; }
    }
}
