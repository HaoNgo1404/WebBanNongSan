using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebWeb.Models
{
    [Table("SupportTicket")]
    public class SupportTicket
    {
        [Key]
        [Column("ticketID")]
        public int TicketID { get; set; }

        [Column("nhanVienID")]
        public int? NhanVienID { get; set; }

        [Column("khachHangID")]
        public int? KhachHangID { get; set; }

        [Required]
        [Column("tenKhachHang")]
        [StringLength(100)]
        public string TenKhachHang { get; set; } = "Khách vãng lai";

        [Column("emailLienHe")]
        [StringLength(250)]
        public string? EmailLienHe { get; set; }

        [Required]
        [Column("cauHoi")]
        [StringLength(450)]
        public string CauHoi { get; set; } = string.Empty;

        [Column("adminTraLoi")]
        public string? AdminTraLoi { get; set; }

        [Required]
        [Column("trangThai")]
        [StringLength(50)]
        public string TrangThai { get; set; } = OrderStatuses.ChoXuLy;

        [Column("ngayTao")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Column("ngayPhanHoi")]
        public DateTime? NgayPhanHoi { get; set; }

        // Navigation Properties (Quan hệ bảng)
        [ForeignKey("KhachHangID")]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("NhanVienID")]
        public virtual NhanVien? NhanVien { get; set; }
    }
}