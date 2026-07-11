using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class DonHangController : Controller
    {
        private readonly ECommerceDBContext _context;

        public DonHangController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH ĐƠN HÀNG LẺ
        public async Task<IActionResult> Index()
        {
            var danhSachDonHang = await _context.DonHangLes
                .Include(d => d.KhachHang)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            return View(danhSachDonHang);
        }

        // 2. CHI TIẾT ĐƠN HÀNG LẺ
        public async Task<IActionResult> Details(int id)
        {
            var donHang = await _context.DonHangLes
                .Include(d => d.KhachHang)
                .Include(d => d.DiaChi)
                .Include(d => d.ChiTietDonHangLes).ThenInclude(ct => ct.NongSan)
                .FirstOrDefaultAsync(d => d.DonHangLeId == id);

            if (donHang == null) return NotFound();

            return View(donHang);
        }

        // =================================================================
        // XỬ LÝ DUYỆT ĐƠN HÀNG LẺ (Chuyển từ "Chờ duyệt" sang "Chờ xử lý")
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetDonHang(int id)
        {
            // 1. Tìm đơn hàng lẻ kèm theo chi tiết nông sản để kiểm tra tính hợp lệ
            var donHang = await _context.DonHangLes
                .Include(d => d.ChiTietDonHangLes)
                    .ThenInclude(ct => ct.NongSan)
                .FirstOrDefaultAsync(d => d.DonHangLeId == id);

            if (donHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng cần duyệt trên hệ thống!" });
            }

            // 2. KIỂM TRA HỢP LỆ: Đơn hàng phải ở trạng thái "Chờ duyệt" mới được xử lý
            if (donHang.TrangThaiDonHang != OrderStatuses.ChoDuyet)
            {
                return Json(new { success = false, message = $"Đơn hàng này không cần duyệt do đang có trạng thái là: {donHang.TrangThaiDonHang}" });
            }

            // Kiểm tra xem đơn hàng có sản phẩm nào không
            if (donHang.ChiTietDonHangLes == null || !donHang.ChiTietDonHangLes.Any())
            {
                return Json(new { success = false, message = "Đơn hàng không hợp lệ vì không chứa bất kỳ nông sản nào!" });
            }

            // 3. SỬ DỤNG TRANSACTION ĐỂ ĐẢM BẢO AN TOÀN KHI TRỪ KHO
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Duyệt qua từng sản phẩm (nông sản) khách đặt trong đơn hàng
                    foreach (var chiTiet in donHang.ChiTietDonHangLes)
                    {
                        decimal soLuongCanTru = chiTiet.SoLuongDat; // Giả định trường số lượng đặt là chiTiet.SoLuong hoặc chiTiet.SoLuongDat
                        int missingNongSanId = chiTiet.NongSanId;
                        string tenNongSan = chiTiet.NongSan?.TenNongSan ?? "Nông sản";

                        // Lấy các lô hàng còn tồn kho của nông sản này, ưu tiên lô nhập trước (FIFO) và còn hạn
                        var danhSachLoHang = await _context.LoHangs
                            .Where(l => l.NongSanId == missingNongSanId && l.SoLuongTon > 0 && l.HanSuDung >= DateTime.Now)
                            .OrderBy(l => l.NgayNhapKho) // FIFO: Lô nào nhập trước trừ trước
                            .ToListAsync();

                        // Tính tổng lượng tồn hiện tại của tất cả các lô để kiểm tra xem có đủ hàng không
                        decimal tongTonHienTai = danhSachLoHang.Sum(l => l.SoLuongTon);
                        if (tongTonHienTai < soLuongCanTru)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Không thể duyệt đơn! Nông sản [{tenNongSan}] trong kho chỉ còn {tongTonHienTai}, không đủ cung cấp cho số lượng đặt là {soLuongCanTru}." });
                        }

                        // Tiến hành trừ lượng tồn kho lần lượt qua từng lô hàng
                        foreach (var loHang in danhSachLoHang)
                        {
                            if (soLuongCanTru <= 0) break;

                            if (loHang.SoLuongTon >= soLuongCanTru)
                            {
                                // Lô hàng này đủ cân hết số lượng còn thiếu
                                loHang.SoLuongTon -= soLuongCanTru;
                                soLuongCanTru = 0;
                            }
                            else
                            {
                                // Lô hàng này không đủ, trừ hết sạch lô này và chuyển lượng thiếu sang lô tiếp theo
                                soLuongCanTru -= loHang.SoLuongTon;
                                loHang.SoLuongTon = 0;
                            }

                            // Cập nhật trạng thái HSD dựa trên lượng tồn kho mới giống như logic bên KhoHangController
                            loHang.TrangThaiHsd = loHang.SoLuongTon == 0 ? "Hết hàng" : (loHang.HanSuDung < DateTime.Now ? "Hết hạn" : "Còn hạn");
                            _context.Update(loHang);
                        }
                    }

                    // 4. CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG SANG "Chờ xử lý"
                    donHang.TrangThaiDonHang = OrderStatuses.ChoXuLy; 
                    _context.Update(donHang);

                    // Lưu toàn bộ thay đổi vào Database và commit transaction
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = $"Đã duyệt thành công đơn hàng #{id}! Hệ thống đã tự động khấu trừ số lượng tồn kho theo từng lô hàng." });
                }
                catch (Exception ex)
                {
                    // Hoàn tác dữ liệu nếu phát sinh bất kỳ lỗi hệ thống nào ngoài ý muốn
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi hệ thống khi khấu trừ tồn kho: " + ex.Message });
                }
            }
        }

        // 3. DANH SÁCH ĐƠN HÀNG ĐỊNH KỲ
        public async Task<IActionResult> IndexDinhKy()
        {
            var danhSachGoi = await _context.GoiDangKyDinhKies
                .Include(g => g.KhachHang)
                .Include(g => g.DiaChi)
                .ToListAsync();
            return View(danhSachGoi);
        }

        // 4. CHI TIẾT ĐƠN HÀNG ĐỊNH KỲ
        public async Task<IActionResult> DetailsDinhKy(int id)
        {
            var goiDangKy = await _context.GoiDangKyDinhKies
                .Include(g => g.KhachHang)
                .Include(g => g.DiaChi)
                .Include(g => g.DotGiaoDinhKies)
                .Include(g => g.ChiTietGoiDinhKies).ThenInclude(ct => ct.NongSan)
                .Include(g => g.DotGiaoDinhKies)
                .FirstOrDefaultAsync(g => g.GoiId == id);

            if (goiDangKy == null) return NotFound();

            return View(goiDangKy);
        }

        // =================================================================
        // XỬ LÝ DUYỆT ĐỢT GIAO ĐỊNH KỲ - TIẾN HÀNH TRỪ KHO THEO ĐỢT
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetDotGiaoDinhKy(int dotGiaoId, decimal? trongLuongThucTe)
        {
            // 1. Tìm đợt giao hàng định kỳ
            var dotGiao = await _context.DotGiaoDinhKies.FindAsync(dotGiaoId);
            if (dotGiao == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đợt giao hàng này!" });
            }

            if (dotGiao.TrangThaiGiao != OrderStatuses.ChoXuLy)
            {
                return Json(new { success = false, message = "Đợt giao này đã được xử lý hoặc đã hủy từ trước!" });
            }

            // 2. Lấy danh sách cấu hình nông sản cần giao của gói này
            var chiTiets = await _context.ChiTietGoiDinhKies
                .Where(ct => ct.GoiId == dotGiao.GoiId)
                .ToListAsync();

            if (chiTiets == null || !chiTiets.Any())
            {
                return Json(new { success = false, message = "Gói định kỳ này không chứa nông sản nào trong cấu hình!" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    decimal tongTrongLuongLyThuyet = 0;

                    // 3. Vòng lặp duyệt qua từng nông sản để trừ số lượng tồn theo LÔ HÀNG (FIFO - Thuần decimal)
                    foreach (var item in chiTiets)
                    {
                        tongTrongLuongLyThuyet += item.SoLuongMoiDot;
                        
                        decimal soLuongCanGiao = item.SoLuongMoiDot; // Giữ nguyên kiểu decimal không cần làm tròn
                        decimal soLuongConThieu = soLuongCanGiao;

                        // Lấy các lô hàng còn tồn, còn hạn, ưu tiên lô cũ nhập trước
                        var danhSachLoHang = await _context.LoHangs
                            .Where(l => l.NongSanId == item.NongSanId && l.SoLuongTon > 0 && l.HanSuDung >= DateTime.Now)
                            .OrderBy(l => l.NgayNhapKho) 
                            .ToListAsync();

                        // Tính tổng tồn khả dụng kiểu decimal từ các lô hàng
                        decimal tongTonCacLo = danhSachLoHang.Sum(l => l.SoLuongTon);

                        if (tongTonCacLo < soLuongCanGiao)
                        {
                            var nongSan = await _context.NongSans.FindAsync(item.NongSanId);
                            return Json(new { 
                                success = false, 
                                message = $"Nông sản '{nongSan?.TenNongSan}' không đủ số lượng tồn trong các lô! (Hiện còn: {tongTonCacLo.ToString("N2")} kg, cần: {soLuongCanGiao.ToString("N2")} kg)!" 
                            });
                        }

                        // Tiến hành khấu trừ số lượng qua từng lô hàng
                        foreach (var loHang in danhSachLoHang)
                        {
                            if (soLuongConThieu <= 0) break;

                            if (loHang.SoLuongTon >= soLuongConThieu)
                            {
                                // Lô này đủ hoặc thừa để đáp ứng
                                loHang.SoLuongTon -= soLuongConThieu;
                                soLuongConThieu = 0;
                            }
                            else
                            {
                                // Lô này không đủ, lấy hết số lượng tồn của lô này và chuyển sang lô sau
                                soLuongConThieu -= loHang.SoLuongTon;
                                loHang.SoLuongTon = 0m;
                            }
                        }
                    }

                    // 4. Cập nhật thông tin đợt giao định kỳ
                    // Ghi nhận trọng lượng thực tế khi thủ kho cân, nếu để trống tự động lấy tổng lý thuyết
                    dotGiao.TrongLuongThucTeDot = trongLuongThucTe ?? tongTrongLuongLyThuyet;
                    dotGiao.TrangThaiGiao = OrderStatuses.DangGiao;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { 
                        success = true, 
                        message = $"Duyệt đợt giao thành công! Hàng đã được khấu trừ tự động chính xác theo các lô hàng và chuyển sang trạng thái '{OrderStatuses.DangGiao}'." 
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống khi duyệt trừ lô hàng: " + ex.Message });
                }
            }
        }

        // =================================================================
        // XÓA ĐƠN HÀNG LẺ (GET - Hiển thị trang xác nhận)
        // =================================================================
        public async Task<IActionResult> Delete(int id)
        {
            // Nạp thêm thông tin khách hàng và địa chỉ để hiển thị trên trang xác nhận
            var donHang = await _context.DonHangLes
                .Include(d => d.KhachHang)
                .Include(d => d.DiaChi)
                .FirstOrDefaultAsync(m => m.DonHangLeId == id);

            if (donHang == null)
            {
                return NotFound();
            }

            return View(donHang);
        }

        // =================================================================
        // XÓA ĐƠN HÀNG LẺ (POST - Thực hiện xóa chính thức)
        // =================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donHang = await _context.DonHangLes.FindAsync(id);
            if (donHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần xóa!";
                return RedirectToAction(nameof(Index));
            }

            // Sử dụng Transaction để đảm bảo an toàn dữ liệu khi xóa nhiều bảng cùng lúc
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    // 1. Xóa tất cả Nhật ký giao dịch liên quan đến đơn hàng này trong bảng GiaoDichThanhToan
                    var giaoDichs = await _context.GiaoDichThanhToans
                        .Where(g => g.DonHangLeId == id)
                        .ToListAsync();
                    if (giaoDichs.Any())
                    {
                        _context.GiaoDichThanhToans.RemoveRange(giaoDichs);
                        await _context.SaveChangesAsync();
                    }

                    // 2. Xóa tất cả Chi tiết đơn hàng thuộc đơn hàng này
                    var chiTiets = await _context.ChiTietDonHangLes
                        .Where(ct => ct.DonHangLeId == id)
                        .ToListAsync();
                        
                    if (chiTiets.Any())
                    {
                        _context.ChiTietDonHangLes.RemoveRange(chiTiets);
                        await _context.SaveChangesAsync();
                    }

                    // 3. Xóa bản ghi đơn hàng chính
                    _context.DonHangLes.Remove(donHang);
                    await _context.SaveChangesAsync();
                    
                    // Hoàn tất toàn bộ chuỗi tiến trình hành động an toàn
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Xóa thành công đơn hàng #{id} và thu hồi điểm tích lũy liên quan!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống khi thực hiện xóa đơn hàng: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }
        // =================================================================
        // XÓA ĐƠN HÀNG ĐỊNH KỲ (GET - Hiển thị trang xác nhận)
        // =================================================================
        public async Task<IActionResult> DeleteDinhKy(int id)
        {
            // Nạp thêm thông tin khách hàng và địa chỉ để hiển thị trên trang xác nhận
            var goiky = await _context.GoiDangKyDinhKies
                .Include(d => d.KhachHang)
                .Include(d => d.DiaChi)
                .FirstOrDefaultAsync(m => m.GoiId == id);

            if (goiky == null)
            {
                return NotFound();
            }

            return View(goiky);
        }

        // =================================================================
        // XÓA ĐƠN HÀNG LẺ (POST - Thực hiện xóa chính thức)
        // =================================================================
        [HttpPost, ActionName("DeleteDinhKy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedDinhKy(int id)
        {
            var goiky = await _context.GoiDangKyDinhKies.FindAsync(id);
            if (goiky == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần xóa!";
                return RedirectToAction(nameof(Index));
            }

            // Sử dụng Transaction để đảm bảo an toàn dữ liệu khi xóa nhiều bảng cùng lúc
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    // 1. Xóa tất cả Nhật ký giao dịch liên quan đến đơn hàng này trong bảng GiaoDichThanhToan
                    var giaoDichs = await _context.GiaoDichThanhToans
                        .Where(g => g.GoiDangKyId == id)
                        .ToListAsync();
                    if (giaoDichs.Any())
                    {
                        _context.GiaoDichThanhToans.RemoveRange(giaoDichs);
                        await _context.SaveChangesAsync();
                    }

                    // 2. Xóa tất cả Chi tiết đơn hàng thuộc đơn hàng này
                    var chiTiets = await _context.ChiTietGoiDinhKies
                        .Where(ct => ct.GoiId == id)
                        .ToListAsync();
                        
                    if (chiTiets.Any())
                    {
                        _context.ChiTietGoiDinhKies.RemoveRange(chiTiets);
                        await _context.SaveChangesAsync();
                    }
                    var dotGiaos = await _context.DotGiaoDinhKies
                        .Where(m => m.GoiId == id)
                        .ToListAsync();

                    // 3. Xóa tất cả đợt giao liên quan đến đơn hàng này
                    if (dotGiaos.Any())
                    {
                        _context.DotGiaoDinhKies.RemoveRange(dotGiaos);
                        await _context.SaveChangesAsync();
                    }

                    // 3. Xóa bản ghi đơn hàng chính
                    _context.GoiDangKyDinhKies.Remove(goiky);
                    await _context.SaveChangesAsync();
                    
                    // Hoàn tất toàn bộ chuỗi tiến trình hành động an toàn
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Xóa thành công gói #{id} ";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống khi thực hiện xóa gói: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(IndexDinhKy));
        }
    }
}