using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly ECommerceDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public BotController(ECommerceDBContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("chat")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { response = "Tin nhắn không được để trống." });
            }

            try
            {
                string rawQuery = request.Message.Trim();
                string normalizedQuery = rawQuery.ToLower();

                // 1. KIỂM TRA TRONG DATABASE CACHE TRƯỚC (Hỗ trợ tìm kiếm tương đồng >= 80% an toàn)
                var allCaches = await _context.BotCaches.ToListAsync();

                BotCache? bestMatchedCache = null;
                double highestSimilarity = 0.0;

                foreach (var cache in allCaches)
                {
                    double similarity = CalculateSimilarity(normalizedQuery, cache.UserQuery);
                    if (similarity > highestSimilarity)
                    {
                        highestSimilarity = similarity;
                        bestMatchedCache = cache;
                    }
                }

                // Chặn việc kích hoạt nhầm cache khi độ tương đồng thực tế không đạt yêu cầu
                if (bestMatchedCache != null && highestSimilarity >= 0.8)
                {
                    bestMatchedCache.HitCount += 1;
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { 
                        response = bestMatchedCache.BotResponse, 
                        source = $"database_cache (Độ tương đồng: {Math.Round(highestSimilarity * 100, 1)}%)" 
                    });
                }

                // 2. TRUY VẤN TOÀN BỘ DỮ LIỆU NGHIỆP VỤ QUAN TRỌNG TỪ ECOMMERCEDB
                var dbProducts = await _context.NongSans
                    .Where(n => n.LoHangs.Any(lh => lh.SoLuongTon > 0))
                    .Select(n => new 
                    { 
                        n.TenNongSan, 
                        GiaBan = n.GiaBanNiemYet,
                        DonVi = n.DonViTinh,
                        DanhMuc = n.DanhMuc.TenDanhMuc,
                        NhaVuon = n.NhaVuon.TenNhaVuon,
                        MoTa = n.MoTa ?? "Đang cập nhật"
                    })
                    .ToListAsync();

                var activeVouchers = await _context.KhuyenMais
                    .Where(k => k.TrangThai == true && k.NgayKetThuc > DateTime.Now && k.SoLuotDaDung < k.SoLuotPhatHanh)
                    .Select(k => new 
                    {
                        k.VoucherCode,
                        k.TenChuongTrinh,
                        k.MucGiam,
                        k.LoaiGiamGia,
                        k.GiaTriDonToiThieu
                    })
                    .ToListAsync();

                var topReviews = await _context.DanhGiaSanPhams
                    .Where(d => d.SoSao >= 4)
                    .OrderByDescending(d => d.NgayDanhGia)
                    .Take(5)
                    .Select(d => new 
                    {
                        TenSanPham = d.NongSan.TenNongSan,
                        KhachHang = d.KhachHang.HoTen,
                        d.SoSao,
                        d.BinhLuan
                    })
                    .ToListAsync();

                var storeContext = new
                {
                    ThongTinCuaHang = "Green Fresh - Cửa hàng cung cấp nông sản sạch, an toàn từ các nhà vườn uy tín.",
                    ThoiGianCapNhat = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    NongSanDangCoSan = dbProducts,
                    KhuyenMaiHoatDong = activeVouchers,
                    DanhGiaKhachHang = topReviews
                };

                string fullContextJson = JsonSerializer.Serialize(storeContext, new JsonSerializerOptions { WriteIndented = true });

                // 3. CACHE MISS -> GỌI GOOGLE GEMINI API
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new { response = "Hệ thống chưa cấu hình API Key!" });
                }

                var client = _httpClientFactory.CreateClient();

                // Phân tách vai trò hệ thống rõ ràng
                var systemInstruction = "Bạn là trợ lý ảo chăm sóc khách hàng siêu dễ thương tên là 'Green Fresh' của cửa hàng nông sản sạch Green Fresh.\n" +
                        "Nhiệm vụ của bạn là dựa vào DỮ LIỆU THỰC TẾ dưới dạng JSON được cung cấp để tư vấn chính xác cho khách hàng.\n\n" +
                        "QUY TẮC ỨNG XỬ:\n" +
                        "1. Chỉ tư vấn các sản phẩm, giá cả, và chương trình khuyến mãi thực tế nằm trong JSON.\n" +
                        "2. Tuyệt đối không tự bịa ra sản phẩm, giá bán, hay mã voucher không tồn tại.\n" +
                        "3. Nếu khách hỏi sản phẩm không có trong danh sách, hãy khéo léo phản hồi: 'Dạ sản phẩm này hiện tại Green Fresh em chưa kinh doanh hoặc đang tạm hết hàng ạ.'\n" +
                        "4. Tận dụng thông tin Đánh giá khách hàng và Nhà vườn để tăng độ tin cậy khi tư vấn.\n" +
                        "5. Xưng hô 'Green Fresh em' và gọi khách là 'Anh/Chị' lịch sự, thân thiện.\n" +
                        "6. Chỉ tập trung trả lời đúng trọng tâm câu hỏi của khách. Tránh dông dài lặp lại các thông tin khuyến mãi hay giới thiệu không liên quan nếu khách không hỏi.\n\n" +
                        "=== DỮ LIỆU THỰC TẾ HỆ THỐNG (JSON) ===\n" +
                        fullContextJson;

                // Cấu trúc Payload chuẩn hóa tách biệt systemInstruction theo định dạng của Google Gemini API
                var payload = new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = systemInstruction } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = rawQuery }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2 // Giảm temperature xuống 0.2 để bot trả lời chính xác, tránh tự sáng tạo / lặp thông tin
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                string geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";
                
                var response = await client.PostAsync(geminiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetail = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { response = $"Lỗi từ Google API (Mã {response.StatusCode}): {errorDetail}" });
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                
                var aiResponse = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrEmpty(aiResponse))
                {
                    var newCache = new BotCache
                    {
                        UserQuery = normalizedQuery,
                        BotResponse = aiResponse,
                        CreatedAt = DateTime.Now,
                        HitCount = 1
                    };

                    _context.BotCaches.Add(newCache);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { response = aiResponse, source = "gemini_api" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { response = "Lỗi kết nối chatbot: " + ex.Message });
            }
        }

        // CẢI TIẾN THUẬT TOÁN: Kết hợp Levenshtein và Jaccard Word-Level Similarity
        private double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;
            if (source == target) return 1.0;

            // 1. Phân tích cấp độ từ (Tránh trùng lặp giả do độ dài chuỗi ngắn gần bằng nhau)
            var sourceWords = source.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            var targetWords = target.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);

            var intersect = sourceWords.Intersect(targetWords).Count();
            var union = sourceWords.Union(targetWords).Count();
            
            double jaccardSimilarity = (double)intersect / union;

            // Nếu các từ khóa chính dùng để hỏi khác nhau hoàn toàn (dưới 40% số từ trùng nhau), loại luôn không so khớp
            if (jaccardSimilarity < 0.4) return 0.0;

            // 2. Nếu vượt qua bộ lọc từ khóa, tiến hành tính khoảng cách Levenshtein chi tiết
            int sourceLength = source.Length;
            int targetLength = target.Length;

            int[,] distance = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; distance[i, 0] = i++) { }
            for (int j = 0; j <= targetLength; distance[0, j] = j++) { }

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            int maxLength = Math.Max(sourceLength, targetLength);
            double levenshteinSimilarity = 1.0 - ((double)distance[sourceLength, targetLength] / maxLength);

            // Trả về trung bình trọng số (Ưu tiên độ tương đồng từ khóa Jaccard)
            return (jaccardSimilarity * 0.4) + (levenshteinSimilarity * 0.6);
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}