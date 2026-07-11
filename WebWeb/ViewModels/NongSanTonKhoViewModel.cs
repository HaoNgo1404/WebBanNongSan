namespace WebWeb.ViewModels
{
    // Định nghĩa một Class ViewModel nhỏ ngay trong file Controller (hoặc file riêng tùy Hào nhé)
    public class NongSanTonKhoViewModel
    {
        public int NongSanId { get; set; }
        public string TenNongSan { get; set; } = null!;
        public decimal TongSoLuongTon { get; set; }
        public int SoLuongLoHangActive { get; set; } // Số lượng lô hiện vẫn đang còn hàng
    }
}