using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class NhaVuon
    {
        public NhaVuon()
        {
            NongSans = new HashSet<NongSan>();
            PhieuChiCongNos = new HashSet<PhieuChiCongNo>();
            PhieuNhapKhos = new HashSet<PhieuNhapKho>();
        }

        public int NhaVuonId { get; set; }
        public string TenNhaVuon { get; set; } = null!;
        public string? DiaChi { get; set; }
        public string? SoDienThoai { get; set; }
        public string? ChungNhanAnToan { get; set; }
        public string? CauChuyenNhaVuon { get; set; }

        public virtual ICollection<NongSan> NongSans { get; set; }
        public virtual ICollection<PhieuChiCongNo> PhieuChiCongNos { get; set; }
        public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; }
    }
}
