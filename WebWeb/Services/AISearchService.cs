using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Services
{
    public class AISearchService
    {
        private readonly HttpClient _httpClient;
        private readonly ECommerceDBContext _context;
        private readonly string _apiKey; // Thay API Key của bạn vào đây

        public AISearchService(HttpClient httpClient, ECommerceDBContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        /// <summary>
        /// Phân tích nhu cầu tìm kiếm tự nhiên của người dùng và trả về danh sách ID nông sản phù hợp
        /// </summary>
        public async Task<List<int>> LayDanhSachIdGoiYAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<int>();

            try
            {
                // 1. Lấy ngữ cảnh toàn bộ sản phẩm hiện có trong DB
                var allProducts = await _context.NongSans
                    .Select(n => new { n.NongSanId, n.TenNongSan, n.MoTa })
                    .ToListAsync();

                if (!allProducts.Any()) return new List<int>();

                string contextData = string.Join(", ", allProducts.Select(p => $"[ID:{p.NongSanId}] {p.TenNongSan} ({p.MoTa})"));

                // 2. Tạo Prompt yêu cầu AI chọn ID phù hợp
                var prompt = $"Bạn là chuyên gia tư vấn nông sản. Danh sách sản phẩm trong kho: {contextData}.\n" +
                             $"Người dùng nhập từ khóa tìm kiếm: '{keyword}'.\n" +
                             $"Hãy chọn tối đa 6 ID sản phẩm phù hợp nhất với ý định của người dùng (Ví dụ: 'ngọt' -> dưa hấu, xoài; 'giải nhiệt' -> rau má, dưa hấu).\n" +
                             $"YÊU CẦU DUY NHẤT: Trả về chuỗi các ID cách nhau bằng dấu phẩy (Ví dụ: 1,3,5). Không thêm bất kỳ chữ nào khác.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonResponse);

                    string aiText = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString()?.Trim() ?? "";

                    // Trích xuất các ID
                    var productIds = aiText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(id => id.Trim())
                                           .Where(id => int.TryParse(id, out _))
                                           .Select(int.Parse)
                                           .ToList();

                    return productIds;
                }
            }
            catch
            {
                // Nếu lỗi kết nối API thì trả về danh sách rỗng để Controller dùng lọc SQL truyền thống
                return new List<int>();
            }

            return new List<int>();
        }
    }
}