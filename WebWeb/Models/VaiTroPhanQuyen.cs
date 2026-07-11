using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class VaiTroPhanQuyen
    {
        public VaiTroPhanQuyen()
        {
            NhanViens = new HashSet<NhanVien>();
        }

        public int VaiTroId { get; set; }
        public string TenVaiTro { get; set; } = null!;
        public string? MoTa { get; set; }

        public virtual ICollection<NhanVien>? NhanViens { get; set; }
    }
}
