using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class BaoCaoHaoHut
    {
        public BaoCaoHaoHut()
        {
            ChiTietBaoCaoHaoHuts = new HashSet<ChiTietBaoCaoHaoHut>();
        }

        public int BaoCaoId { get; set; }
        public int NhanVienId { get; set; }
        public DateTime NgayLap { get; set; }
        public string LyDoHaoHut { get; set; } = null!;
        public decimal TongGiaTriThietHai { get; set; }

        public virtual NhanVien NhanVien { get; set; } = null!;
        public virtual ICollection<ChiTietBaoCaoHaoHut> ChiTietBaoCaoHaoHuts { get; set; }
    }
}
