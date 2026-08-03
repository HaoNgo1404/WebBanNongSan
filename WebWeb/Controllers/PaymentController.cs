using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebWeb.Helpers;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ECommerceDBContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(ECommerceDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =================================================================
        // 1. VNPAY: CHUẨN THEO PROGCODER & THAY ĐỔI ORDER_TYPE
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RedirectToVnPay(int orderId, string type = "le")
        {
            decimal? tongTien = 0;
            string orderInfo = $"Thanh toan don hang {type} ma {orderId}"; 

            if (type == "dinhky")
            {
                var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                if (goiKy == null) return NotFound();
                tongTien = goiKy.TongTienGoi;
            }
            else
            {
                var donHang = await _context.DonHangLes.FindAsync(orderId);
                if (donHang == null) return NotFound();
                tongTien = donHang.TongTienThucTe;
            }

            string tmnCode = _configuration["PaymentSettings:Vnpay:TmnCode"]?.Trim();
            string hashSecret = _configuration["PaymentSettings:Vnpay:HashSecret"]?.Trim();
            string baseUrl = _configuration["PaymentSettings:Vnpay:BaseUrl"]?.Trim();
            string returnUrl = _configuration["PaymentSettings:Vnpay:ReturnUrl"]?.Trim();

            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            string txtCreateDate = timeNow.ToString("yyyyMMddHHmmss");
            string txtExpireDate = timeNow.AddMinutes(15).ToString("yyyyMMddHHmmss");
            
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1") ipAddress = "127.0.0.1";

            string vnp_TxnRef = $"{type}_{orderId}_{DateTime.Now.Ticks}";
            long amountInCents = (long)(tongTien * 100);

            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.1");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);
            vnpay.AddRequestData("vnp_Amount", amountInCents.ToString());
            vnpay.AddRequestData("vnp_CreateDate", txtCreateDate);
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            // SỬA QUAN TRỌNG: Đổi từ "other" sang mã loại hình dịch vụ chuẩn số "250000"
            vnpay.AddRequestData("vnp_OrderType", "250000"); 
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);
            vnpay.AddRequestData("vnp_ExpireDate", txtExpireDate);

            string paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);
            Console.WriteLine(paymentUrl);
            return Redirect(paymentUrl);
        }

        // =================================================================
        // 2. MOMO: SỬA ORDERID CHỈ CHỨA SỐ VÀ ĐÓNG GÓI DỮ LIỆU VÀO EXTRADATA
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RedirectToMoMo(int orderId, string type = "le")
        {
            decimal? tongTien = 0;
            string orderInfo = $"ThanhToanDonHang{type}Ma{orderId}";

            if (type == "dinhky")
            {
                var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                if (goiKy == null) return NotFound();
                tongTien = goiKy.TongTienGoi;
            }
            else
            {
                var donHang = await _context.DonHangLes.FindAsync(orderId);
                if (donHang == null) return NotFound();
                tongTien = donHang.TongTienThucTe;
            }

            string partnerCode = _configuration["PaymentSettings:Momo:PartnerCode"];
            string accessKey = _configuration["PaymentSettings:Momo:AccessKey"];
            string secretKey = _configuration["PaymentSettings:Momo:SecretKey"];
            string endpoint = _configuration["PaymentSettings:Momo:Endpoint"];

            string configReturnUrl = _configuration["PaymentSettings:Momo:ReturnUrl"];
            string redirectUrl = !string.IsNullOrEmpty(configReturnUrl) 
                ? configReturnUrl 
                : "http://localhost:5000/Payment/MomoReturn";
            
            string configIpnUrl = _configuration["PaymentSettings:Momo:IpnUrl"];
            string ipnUrl = !string.IsNullOrEmpty(configIpnUrl) 
                ? configIpnUrl 
                : redirectUrl;

            // FIX NAPAS: orderId CHỈ CHỨA SỐ
            string orderIdMomo = DateTime.Now.Ticks.ToString();
            string requestId = orderIdMomo;
            
            // Đóng gói thông tin type và id gốc vào extraData (Mã hóa Base64)
            string extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{type}_{orderId}"));
            long amountLong = (long)(tongTien ?? 0);
            string requestType = "payWithMethod";

            // Chuỗi thô (Raw Hash) sắp xếp Alphabet bắt buộc khớp từng thuộc tính theo tài liệu MoMo
            string rawHash = $"accessKey={accessKey}" +
                             $"&amount={amountLong}" +
                             $"&extraData={extraData}" +
                             $"&ipnUrl={ipnUrl}" + 
                             $"&orderId={orderIdMomo}" +
                             $"&orderInfo={orderInfo}" +
                             $"&partnerCode={partnerCode}" +
                             $"&redirectUrl={redirectUrl}" + 
                             $"&requestId={requestId}" +
                             $"&requestType={requestType}";
            
            string signature = "";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawHash));
                signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            System.Diagnostics.Debug.WriteLine("RAW HASH: " + rawHash);
            System.Diagnostics.Debug.WriteLine("SIGNATURE: " + signature);

            var requestData = new
            {
                partnerCode = partnerCode,
                requestId = requestId,
                orderId = orderIdMomo,
                amount = amountLong, 
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                requestType = requestType,
                extraData = extraData,
                signature = signature,
                lang = "vi"
            };

            using (var client = new HttpClient())
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var momoResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    if (momoResponse != null && momoResponse.ContainsKey("payUrl"))
                    {
                        return Redirect(momoResponse["payUrl"].ToString());
                    }
                }
                return BadRequest("Không thể kết nối API MoMo Sandbox: " + responseContent);
            }
        }

        // =================================================================
        // 3. ĐÓN KẾT QUẢ VNPAY TRẢ VỀ
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            var vnpay = new VnPayLibrary();
            foreach (var key in Request.Query.Keys)
            {
                vnpay.AddResponseData(key, Request.Query[key]);
            }

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_TransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
            string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
            string vnp_AmountStr = vnpay.GetResponseData("vnp_Amount");

            string hashSecret = _configuration["PaymentSettings:Vnpay:HashSecret"]?.Trim();
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, hashSecret);

            if (checkSignature && vnp_ResponseCode == "00")
            {
                var parts = vnp_TxnRef.Split('_');
                string type = parts[0];
                int orderId = int.Parse(parts[1]);
                decimal amount = decimal.Parse(vnp_AmountStr) / 100;

                if (type == "dinhky")
                {
                    var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                    if (goiKy != null)
                    {
                        goiKy.TrangThaiGoi = OrderStatuses.HoatDong;
                        _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                        {
                            MaGiaoDichCong = "VNPAY-" + vnp_TransactionNo,
                            GoiDangKyId = goiKy.GoiId,
                            SoTien = amount,
                            PhuongThuc = "VNPAY",
                            TrangThai = 1,
                            NgayGiaoDich = DateTime.Now
                        });
                        try
                        {
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            var message = ex.InnerException?.Message ?? ex.Message;
                            return Content(message);
                        }
                        return RedirectToAction("OrderPackageSuccess", "Notification", new { orderId = goiKy.GoiId, platform = "VNPAY", amount = amount, type = "dinhky" });
                    }
                }
                else
                {
                    var donHang = await _context.DonHangLes.FindAsync(orderId);
                    if (donHang != null)
                    {
                        donHang.TrangThaiThanhToan = OrderStatuses.DaThanhToan;
                        _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                        {
                            MaGiaoDichCong = "VNPAY-" + vnp_TransactionNo,
                            DonHangLeId = donHang.DonHangLeId,
                            SoTien = amount,
                            PhuongThuc = "VNPAY",
                            TrangThai = 1,
                            NgayGiaoDich = DateTime.Now
                        });
                        try
                        {
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            return Content(ex.InnerException?.Message ?? ex.Message);
                        }
                        return RedirectToAction("OrderSuccess", "Notification", new { orderId = donHang.DonHangLeId, platform = "VNPAY", amount = amount, type = "le" });
                    }
                }
            }
            TempData["Error"] = "Thanh toán VNPay thất bại hoặc đã bị hủy.";
            return RedirectToAction("OrderFailed", "Notification");
        }

        // =================================================================
        // 4. ĐÓN KẾT QUẢ MOMO TRẢ VỀ (REDIRECT VIEW)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> MomoReturn()
        {
            try
            {
                string resultCode = Request.Query["resultCode"];
                string extraData = Request.Query["extraData"];
                string transId = Request.Query["transId"];

                if (string.IsNullOrEmpty(extraData)) 
                    return RedirectToAction("OrderFailed", "Notification");

                // Giải mã Base64 từ extraData để lấy lại type và orderId
                string decodedExtra = Encoding.UTF8.GetString(Convert.FromBase64String(extraData));
                var parts = decodedExtra.Split('_');
                string type = parts[0];
                int orderId = int.Parse(parts[1]);

                if (resultCode == "0")
                {
                    if (type == "dinhky")
                    {
                        var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                        if (goiKy != null)
                        {
                            if (goiKy.TrangThaiGoi != OrderStatuses.HoatDong)
                            {
                                goiKy.TrangThaiGoi = OrderStatuses.HoatDong;
                                _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                                {
                                    MaGiaoDichCong = "MOMO-" + transId,
                                    GoiDangKyId = goiKy.GoiId,
                                    SoTien = goiKy.TongTienGoi,
                                    PhuongThuc = "MOMO",
                                    TrangThai = 1,
                                    NgayGiaoDich = DateTime.Now
                                });
                                await _context.SaveChangesAsync();
                            }
                            return RedirectToAction("OrderPackageSuccess", "Notification", new { orderId = goiKy.GoiId, platform = "MOMO", amount = goiKy.TongTienGoi, type = "dinhky" });
                        }
                    }
                    else
                    {
                        var donHang = await _context.DonHangLes.FindAsync(orderId);
                        if (donHang != null)
                        {
                            if (donHang.TrangThaiThanhToan != OrderStatuses.DaThanhToan)
                            {
                                donHang.TrangThaiThanhToan = OrderStatuses.DaThanhToan;
                                _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                                {
                                    MaGiaoDichCong = "MOMO-" + transId,
                                    DonHangLeId = donHang.DonHangLeId,
                                    SoTien = donHang.TongTienThucTe,
                                    PhuongThuc = "MOMO",
                                    TrangThai = 1,
                                    NgayGiaoDich = DateTime.Now
                                });
                                await _context.SaveChangesAsync();
                            }
                            return RedirectToAction("OrderSuccess", "Notification", new { orderId = donHang.DonHangLeId, platform = "MOMO", amount = donHang.TongTienThucTe, type = "le" });
                        }
                    }
                }
                TempData["Error"] = "Thanh toán MoMo thất bại hoặc đã bị hủy.";
                return RedirectToAction("OrderFailed", "Notification");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LỖI MOMO RETURN: " + ex.ToString());
                return Content("Lỗi xử lý Return: " + ex.Message); 
            } 
        }

        // =================================================================
        // 5. IPN MOMO (SERVER-TO-SERVER WEBHOOK TRẢ VỀ NGẦM)
        // =================================================================
        [HttpPost]
        [Route("Payment/MomoIPN")]
        public async Task<IActionResult> MomoIPN([FromBody] JsonElement body)
        {
            try
            {
                string resultCode = body.GetProperty("resultCode").ToString();
                string extraData = body.GetProperty("extraData").ToString();
                string transId = body.GetProperty("transId").ToString();

                if (resultCode == "0" && !string.IsNullOrEmpty(extraData))
                {
                    string decodedExtra = Encoding.UTF8.GetString(Convert.FromBase64String(extraData));
                    var parts = decodedExtra.Split('_');
                    string type = parts[0];
                    int orderId = int.Parse(parts[1]);

                    if (type == "dinhky")
                    {
                        var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                        if (goiKy != null && goiKy.TrangThaiGoi != OrderStatuses.HoatDong)
                        {
                            goiKy.TrangThaiGoi = OrderStatuses.HoatDong;
                            _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                            {
                                MaGiaoDichCong = "MOMO-" + transId,
                                GoiDangKyId = goiKy.GoiId,
                                SoTien = goiKy.TongTienGoi,
                                PhuongThuc = "MOMO",
                                TrangThai = 1,
                                NgayGiaoDich = DateTime.Now
                            });
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        var donHang = await _context.DonHangLes.FindAsync(orderId);
                        if (donHang != null && donHang.TrangThaiThanhToan != OrderStatuses.DaThanhToan)
                        {
                            donHang.TrangThaiThanhToan = OrderStatuses.DaThanhToan;
                            _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                            {
                                MaGiaoDichCong = "MOMO-" + transId,
                                DonHangLeId = donHang.DonHangLeId,
                                SoTien = donHang.TongTienThucTe,
                                PhuongThuc = "MOMO",
                                TrangThai = 1,
                                NgayGiaoDich = DateTime.Now
                            });
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                return NoContent(); // HTTP 204
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MomoSandboxSuccess(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
                return BadRequest();

            var parts = orderId.Split('_');
            string type = parts[0];
            int id = int.Parse(parts[1]);

            if (type == "dinhky")
            {
                var goiKy = await _context.GoiDangKyDinhKies.FindAsync(id);
                if (goiKy == null)
                    return NotFound();

                goiKy.TrangThaiGoi = OrderStatuses.HoatDong;

                _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                {
                    MaGiaoDichCong = "MOMO-" + Guid.NewGuid().ToString("N")[..8],
                    GoiDangKyId = goiKy.GoiId,
                    SoTien = goiKy.TongTienGoi,
                    PhuongThuc = "MOMO",
                    TrangThai = 1,
                    NgayGiaoDich = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return RedirectToAction("OrderPackageSuccess", "Notification", new { orderId = goiKy.GoiId });
            }

            var donHang = await _context.DonHangLes.FindAsync(id);

            if (donHang == null)
                return NotFound();

            donHang.TrangThaiThanhToan = OrderStatuses.DaThanhToan;

            _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
            {
                MaGiaoDichCong = "MOMO-" + Guid.NewGuid().ToString("N")[..8],
                DonHangLeId = donHang.DonHangLeId,
                SoTien = donHang.TongTienThucTe,
                PhuongThuc = "MOMO",
                TrangThai = 1,
                NgayGiaoDich = DateTime.Now
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Content(ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("OrderSuccess", "Notification", new { orderId = donHang.DonHangLeId });
        }
    }
}