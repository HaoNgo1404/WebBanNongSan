using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class ThamSo
    {
        public string MaThamSo { get; set; } = null!;
        public decimal GiaTri { get; set; }
        public string? GhiChu { get; set; }
    }
}
