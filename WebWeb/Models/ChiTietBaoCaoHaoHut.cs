using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class ChiTietBaoCaoHaoHut
    {
        public int BaoCaoId { get; set; }
        public int LoHangId { get; set; }
        public decimal SoLuongHaoHut { get; set; }
        public decimal DonGiaHaoHut { get; set; }

        public virtual BaoCaoHaoHut BaoCao { get; set; } = null!;
        public virtual LoHang LoHang { get; set; } = null!;
    }
}
