using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class PhienDangNhap
    {
        public int TokenId { get; set; }
        public int TaiKhoanId { get; set; }
        public string TokenChuoi { get; set; } = null!;
        public DateTime NgayTao { get; set; }
        public DateTime NgayHetHan { get; set; }
        public string? ThietBi { get; set; }
    }
}
