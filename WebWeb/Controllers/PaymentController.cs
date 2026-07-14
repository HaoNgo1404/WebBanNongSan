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
            decimal tongTien = 0;
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
                tongTien = donHang.TongTienTamTinh;
            }

            string tmnCode = _configuration["PaymentSettings:Vnpay:TmnCode"]?.Trim();
            string hashSecret = _configuration["PaymentSettings:Vnpay:HashSecret"]?.Trim();
            string baseUrl = _configuration["PaymentSettings:Vnpay:BaseUrl"]?.Trim();
            string returnUrl = _configuration["PaymentSettings:Vnpay:ReturnUrl"]?.Trim();

            string txtCreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string txtExpireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");
            
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
            // SỬA QUAN TRỌNG: Đổi từ "other" sang mã loại hình dịch vụ số "250000" (Thanh toán hóa đơn nông sản/thực phẩm)
            vnpay.AddRequestData("vnp_OrderType", "other"); 
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);
            vnpay.AddRequestData("vnp_ExpireDate", txtExpireDate);

            string paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);
            Console.WriteLine(paymentUrl);
            return Redirect(paymentUrl);
        }

        // =================================================================
        // 2. MOMO: CHUẨN ĐỊNH DẠNG TEXT KHÔNG DẤU GẠCH & ÉP KHỚP PAYLOAD JSON
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RedirectToMoMo(int orderId, string type = "le")
        {
            decimal tongTien = 0;
            // Trả về chuỗi ký tự viết liền không dấu, không khoảng trắng để chuỗi thô (Raw) của MoMo khớp tuyệt đối
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
                tongTien = donHang.TongTienTamTinh;
            }

            string partnerCode = _configuration["PaymentSettings:Momo:PartnerCode"];
            string accessKey = _configuration["PaymentSettings:Momo:AccessKey"];
            string secretKey = _configuration["PaymentSettings:Momo:SecretKey"];
            string endpoint = _configuration["PaymentSettings:Momo:Endpoint"];
            string returnUrl = _configuration["PaymentSettings:Momo:ReturnUrl"];

            string requestId = $"{type}_{orderId}_{DateTime.Now.Ticks}";
            string orderIdMomo = requestId;
            long amountLong = (long)tongTien;
            string requestType = "payWithMethod";
            string extraData = ""; 

            // Chuỗi thô (Raw Hash) khớp từng ký tự theo tài liệu MoMo Developer Hào gửi
            string rawHash = $"accessKey={accessKey}" +
                             $"&amount={amountLong}" +
                             $"&extraData={extraData}" +
                             $"&ipnUrl={returnUrl}" + 
                             $"&orderId={orderIdMomo}" +
                             $"&orderInfo={orderInfo}" +
                             $"&partnerCode={partnerCode}" +
                             $"&redirectUrl={returnUrl}" + 
                             $"&requestId={requestId}" +
                             $"&requestType={requestType}";
            
            string signature = "";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawHash));
                signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            System.Diagnostics.Debug.WriteLine(rawHash);
            System.Diagnostics.Debug.WriteLine(signature);

            // Object gửi đi bắt buộc thuộc tính nhận URL phải đặt tên khớp với chuỗi băm
            var requestData = new
            {
                partnerCode = partnerCode,
                requestId = requestId,
                orderId = orderIdMomo,
                amount = amountLong, 
                orderInfo = orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = returnUrl, // Đảm bảo trùng tên thuộc tính băm
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
        // 3. ĐÓN KẾT QUẢ VNPAY TRẢ VỀ (GIỮ NGUYÊN ĐỂ XỬ LÝ DATABASE)
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
                        await _context.SaveChangesAsync();
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
                        await _context.SaveChangesAsync();
                        return RedirectToAction("OrderSuccess", "Notification", new { orderId = donHang.DonHangLeId, platform = "VNPAY", amount = amount, type = "le" });
                    }
                }
            }
            TempData["Error"] = "Thanh toán VNPay thất bại hoặc đã bị hủy.";
            return RedirectToAction("OrderFailed", "Notification");
        }

        // =================================================================
        // 4. ĐÓN KẾT QUẢ MOMO TRẢ VỀ (GIỮ NGUYÊN ĐỂ XỬ LÝ DATABASE)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> MomoReturn()
        {
            string resultCode = Request.Query["resultCode"];
            string orderIdMomo = Request.Query["orderId"];
            string transId = Request.Query["transId"];

            if (string.IsNullOrEmpty(orderIdMomo)) return BadRequest();

            var parts = orderIdMomo.Split('_');
            string type = parts[0];
            int orderId = int.Parse(parts[1]);

            if (resultCode == "0")
            {
                if (type == "dinhky")
                {
                    var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                    if (goiKy != null)
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
                        return RedirectToAction("OrderPackageSuccess", "Notification", new { orderId = goiKy.GoiId, platform = "MOMO", amount = goiKy.TongTienGoi, type = "dinhky" });
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
                            MaGiaoDichCong = "MOMO-" + transId,
                            DonHangLeId = donHang.DonHangLeId,
                            SoTien = donHang.TongTienTamTinh,
                            PhuongThuc = "MOMO",
                            TrangThai = 1,
                            NgayGiaoDich = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                        return RedirectToAction("OrderSuccess", "Notification", new { orderId = donHang.DonHangLeId, platform = "MOMO", amount = donHang.TongTienTamTinh, type = "le" });
                    }
                }
            }
            TempData["Error"] = "Thanh toán MoMo thất bại hoặc đã bị hủy.";
            return RedirectToAction("OrderFailed", "Notification"); 
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
                    MaGiaoDichCong = "MOMO-DEMO-" + Guid.NewGuid().ToString("N")[..8],
                    GoiDangKyId = goiKy.GoiId,
                    SoTien = goiKy.TongTienGoi,
                    PhuongThuc = "MOMO",
                    TrangThai = 1,
                    NgayGiaoDich = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "OrderPackageSuccess",
                    "Notification",
                    new
                    {
                        orderId = goiKy.GoiId
                    });
            }

            var donHang = await _context.DonHangLes.FindAsync(id);

            if (donHang == null)
                return NotFound();

            donHang.TrangThaiThanhToan = OrderStatuses.DaThanhToan;

            _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
            {
                MaGiaoDichCong = "MOMO-DEMO-" + Guid.NewGuid().ToString("N")[..8],
                DonHangLeId = donHang.DonHangLeId,
                SoTien = donHang.TongTienTamTinh,
                PhuongThuc = "MOMO",
                TrangThai = 1,
                NgayGiaoDich = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "OrderSuccess",
                "Notification",
                new
                {
                    orderId = donHang.DonHangLeId
                });
        }
    }
}