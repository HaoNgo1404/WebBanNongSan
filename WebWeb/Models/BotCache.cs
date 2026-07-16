using System;
using System.ComponentModel.DataAnnotations;

namespace WebWeb.Models
{
    public class BotCache
    {
        [Key]
        public int BotId { get; set; }

        [Required]
        // Lưu câu hỏi của khách (nên lưu dạng viết thường, xóa khoảng trắng thừa để đối chiếu chính xác)
        public string UserQuery { get; set; } = string.Empty;

        [Required]
        // Lưu câu trả lời tương ứng của AI
        public string BotResponse { get; set; } = string.Empty;

        // Ngày tạo bản ghi để quản lý
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Số lần câu hỏi này được tái sử dụng (giúp Hào làm thống kê câu hỏi thường gặp cực tốt!)
        public int HitCount { get; set; } = 1;
    }
}