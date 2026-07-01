using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class NhanVien
    {
        public NhanVien()
        {
            BaoCaoHaoHuts = new HashSet<BaoCaoHaoHut>();
            DonHangLes = new HashSet<DonHangLe>();
            DotGiaoDinhKies = new HashSet<DotGiaoDinhKy>();
            KhieuNais = new HashSet<KhieuNai>();
            PhieuChiCongNos = new HashSet<PhieuChiCongNo>();
            PhieuNhapKhos = new HashSet<PhieuNhapKho>();
        }

        public int NhanVienId { get; set; }
        public string HoTen { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MatKhau { get; set; } = null!;
        public int VaiTroId { get; set; }
        public bool? TrangThai { get; set; }

        public virtual VaiTroPhanQuyen VaiTro { get; set; } = null!;
        public virtual ICollection<BaoCaoHaoHut> BaoCaoHaoHuts { get; set; }
        public virtual ICollection<DonHangLe> DonHangLes { get; set; }
        public virtual ICollection<DotGiaoDinhKy> DotGiaoDinhKies { get; set; }
        public virtual ICollection<KhieuNai> KhieuNais { get; set; }
        public virtual ICollection<PhieuChiCongNo> PhieuChiCongNos { get; set; }
        public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; }
    }
}
