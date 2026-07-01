using System;
using System.Collections.Generic;

namespace WebWeb.Models.ViewModels
{
    // Dùng chung cho cả Đơn Hàng Lẻ và Đăng Ký Gói
    public class CheckoutViewModel
    {
        // Thông tin địa chỉ nhận hàng
        public int DiaChiId { get; set; }
        public string PhuongThucThanhToan { get; set; } = "COD"; // Hoặc VNPAY
        
        // Dành riêng cho UC01: Đơn hàng lẻ
        public string KhungGioGiaoHang { get; set; } = null!;
        // Nếu là khách vãng lai không có tài khoản
        public string? PhoneNonAccount { get; set; }
        public string? NameCusNonAccount { get; set; }
        public string? AddressNonAccount { get; set; }
        public string? EmailCusNonAccount { get; set; } 

        // Dành riêng cho UC02: Gói định kỳ
        public string? TanSuatGiao { get; set; } // Ví dụ: "HangTuan", "CachTuan"
        public string? ThuTrongTuan { get; set; } // Ví dụ: "Thu2-Thu5", "Thu7"
        public int? SoThangDangKy { get; set; }   // Để tính ngày kết thúc

        // Danh sách nông sản trong giỏ hàng được chọn để thanh toán
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
    }

    public class CartItemViewModel
    {
        public int NongSanId { get; set; }
        public int SoLuongDat { get; set; }
        public decimal DonGiaThoiDiem { get; set; }
    }
}