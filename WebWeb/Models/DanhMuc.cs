using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class DanhMuc
    {
        public DanhMuc()
        {
            NongSans = new HashSet<NongSan>();
            KhuyenMais = new HashSet<KhuyenMai>();
        }

        public int DanhMucId { get; set; }
        public string TenDanhMuc { get; set; } = null!;
        public string? MoTa { get; set; }
        public string LoaiHang { get; set; } = null!;

        public virtual ICollection<NongSan>? NongSans { get; set; }
        public virtual ICollection<KhuyenMai>? KhuyenMais { get; set; }
    }
}
