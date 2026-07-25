using System.Text;
using System.Text.Json;

namespace WebWeb.Services
{
    public class AISentimentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey; // Thay API Key của bạn vào đây

        public AISentimentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        /// <summary>
        /// Hàm phân tích cảm xúc đoạn bình luận
        /// Trả về: "Tot", "Xau", hoặc "TrungTinh"
        /// </summary>
        public async Task<string> PhanTichCamXucAsync(string binhLuan)
        {
            if (string.IsNullOrWhiteSpace(binhLuan)) 
                return "TrungTinh";

            try
            {
                // Cấu hình prompt ngắn gọn ép AI chỉ trả về 1 trong 3 nhãn
                var prompt = $@"Bạn là hệ thống phân tích cảm xúc đánh giá sản phẩm.
                Nhiệm vụ: Phân tích bình luận sau: '{binhLuan}'

                Quy tắc phân loại:
                - Tot: Khen ngợi, hài lòng, thể hiện cảm xúc tích cực (Ví dụ: ngon, tươi, giao nhanh, chất lượng).
                - Xau: Chê bai, phàn nàn, không hài lòng (Ví dụ: héo, thối, dở, giao chậm, đắt).
                - TrungTinh: Nhận xét bình thường, mô tả thực tế không khen không chê hoặc vừa khen vừa chê, câu hỏi, hoặc bình luận hòa hoãn/bình thường (Ví dụ: 'Mới nhận được hàng', 'Sản phẩm đóng gói bằng hộp giấy', 'Trái to vừa phải', 'Cần cải thiện thêm chút', 'Sản phẩm hơi già nhưng bảo quản tốt').

                YÊU CẦU DUY NHẤT: Chỉ trả về 1 từ: Tot, Xau, hoặc TrungTinh.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                
                // Gọi API (Ví dụ dùng endpoint Gemini 1.5 Flash)
                var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonResponse);
                    
                    string aiText = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString()?.Trim() ?? "TrungTinh";

                    if (aiText.Contains("Tot")) return "Tot";
                    if (aiText.Contains("Xau")) return "Xau";
                    return "TrungTinh";
                }
            }
            catch
            {
                // Nếu gọi API lỗi hoặc hết hạn ngạch, tự động fallback phân tích từ khóa cơ bản
                return PhanTichCơBanLocal(binhLuan);
            }

            return "TrungTinh";
        }

        // Fallback nhẹ không lo sập ứng dụng khi mất mạng/lỗi API
        private string PhanTichCơBanLocal(string text)
        {
            text = text.ToLower();
            if (text.Contains("tươi") || text.Contains("ngon") || text.Contains("tốt") || text.Contains("sạch") || text.Contains("hài lòng"))
                return "Tot";
            if (text.Contains("hư") || text.Contains("thối") || text.Contains("dở") || text.Contains("chậm") || text.Contains("tệ") || text.Contains("kém"))
                return "Xau";
            return "TrungTinh";
        }
    }
}