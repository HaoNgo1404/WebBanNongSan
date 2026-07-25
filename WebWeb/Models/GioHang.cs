namespace WebWeb.Models
{
    public class GioHang
    {
        public int NongSanId { get; set; }
        public string TenNongSan { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty;
        public decimal Gia { get; set; }
        public decimal GiaThucTe { get; set; }
        public string DonViTinh { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        
        // Thành tiền tự động tính dựa trên Số lượng x Giá (hoặc Giá thực tế)
        public decimal GiaThucSu => (GiaThucTe > 0 && GiaThucTe < Gia) ? GiaThucTe : Gia;

        public decimal ThanhTien => SoLuong * GiaThucSu;
    }
}