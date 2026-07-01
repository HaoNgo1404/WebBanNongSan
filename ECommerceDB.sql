USE master;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ECommerceDB')
BEGIN
    ALTER DATABASE ECommerceDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ECommerceDB;
END
GO

-- KHỞI TẠO DATABASE
CREATE DATABASE ECommerceDB;
GO
USE ECommerceDB;
GO

-- 1. Bảng VaiTroPhanQuyen
CREATE TABLE VaiTroPhanQuyen (
    vaiTroID INT IDENTITY(1,1) PRIMARY KEY,
    tenVaiTro NVARCHAR(50) NOT NULL UNIQUE,
    moTa NVARCHAR(255) NULL
);

-- 2. Bảng NhanVien
CREATE TABLE NhanVien (
    nhanVienID INT IDENTITY(1,1) PRIMARY KEY,
    hoTen NVARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    matKhau VARCHAR(250) NOT NULL,
    vaiTroID INT NOT NULL,
    trangThai BIT DEFAULT 1, -- 1: Đang hoạt động, 0: Đã khóa
    CONSTRAINT FK_NhanVien_VaiTro FOREIGN KEY (vaiTroID) REFERENCES VaiTroPhanQuyen(vaiTroID)
);

-- 3. Bảng NhaVuon
CREATE TABLE NhaVuon (
    nhaVuonID INT IDENTITY(1,1) PRIMARY KEY,
    tenNhaVuon NVARCHAR(150) NOT NULL,
    diaChi NVARCHAR(250) NULL,
    soDienThoai VARCHAR(15) NULL UNIQUE,
    chungNhanAnToan NVARCHAR(100) NULL,
    cauChuyenNhaVuon NVARCHAR(MAX) NULL
);

-- 4. Bảng DanhMuc
CREATE TABLE DanhMuc (
    danhMucID INT IDENTITY(1,1) PRIMARY KEY,
    tenDanhMuc NVARCHAR(100) NOT NULL,
    moTa NVARCHAR(250) NULL,
    loaiHang NVARCHAR(50) NOT NULL CHECK (loaiHang IN (N'Hàng tươi', N'Hàng khô'))
);

-- 5. Bảng NongSan
CREATE TABLE NongSan (
    nongSanID INT IDENTITY(1,1) PRIMARY KEY,
    tenNongSan NVARCHAR(150) NOT NULL,
    moTa NVARCHAR(MAX) NULL,
    hinhAnh VARCHAR(250) NULL,
    giaBanNiemYet DECIMAL(18,2) NOT NULL CHECK (giaBanNiemYet >= 0),
    donViTinh NVARCHAR(50) NOT NULL,
    saiSoChoPhep INT NOT NULL DEFAULT 10 CHECK (saiSoChoPhep BETWEEN 0 AND 100),
    danhMucID INT NOT NULL,
    nhaVuonID INT NOT NULL,
    CONSTRAINT FK_NongSan_DanhMuc FOREIGN KEY (danhMucID) REFERENCES DanhMuc(danhMucID),
    CONSTRAINT FK_NongSan_NhaVuon FOREIGN KEY (nhaVuonID) REFERENCES NhaVuon(nhaVuonID)
);

-- 18. Bảng PhieuNhapKho
CREATE TABLE PhieuNhapKho (
    phieuNhapID INT IDENTITY(1,1) PRIMARY KEY,
    nhaVuonID INT NOT NULL,
    nhanVienID INT NOT NULL,
    ngayLapPhieu DATETIME NOT NULL DEFAULT GETDATE(),
    tongTienNhap DECIMAL(18,2) NOT NULL CHECK (tongTienNhap >= 0),
    CONSTRAINT FK_PhieuNhap_NhaVuon FOREIGN KEY (nhaVuonID) REFERENCES NhaVuon(nhaVuonID),
    CONSTRAINT FK_PhieuNhap_NhanVien FOREIGN KEY (nhanVienID) REFERENCES NhanVien(nhanVienID)
);

-- 6. Bảng LoHang
CREATE TABLE LoHang (
    LoHangID INT IDENTITY(1,1) PRIMARY KEY,
    PhieuNhapID INT NOT NULL,              
    NongSanID INT NOT NULL,                
    DonGiaNhap DECIMAL(18,2) NOT NULL,     
    SoLuongNhap DECIMAL(18,2) NOT NULL,    
    SoLuongTon DECIMAL(18,2) NOT NULL,     
    NgayThuHoach DATETIME NULL,
    NgayNhapKho DATETIME NOT NULL DEFAULT GETDATE(),
    HanSuDung DATETIME NOT NULL,
    TrangThaiHsd NVARCHAR(50) NOT NULL DEFAULT N'Còn hạn',
    CONSTRAINT FK_LoHang_PhieuNhap FOREIGN KEY (PhieuNhapID) REFERENCES PhieuNhapKho(PhieuNhapID) ON DELETE CASCADE,
    CONSTRAINT FK_LoHang_NongSan FOREIGN KEY (NongSanID) REFERENCES NongSan(nongSanID)
);

-- 7. Bảng KhachHang
CREATE TABLE KhachHang (
    khachHangID INT IDENTITY(1,1) PRIMARY KEY,
    hoTen NVARCHAR(100) NOT NULL,
    soDienThoai VARCHAR(15) NOT NULL UNIQUE,
    email VARCHAR(100) NULL UNIQUE,
    matKhauMaHoa VARCHAR(250) NOT NULL,
    ngayDangKy DATETIME NOT NULL DEFAULT GETDATE(),
    diemTichLuy INT NOT NULL DEFAULT 0 CHECK (diemTichLuy >= 0)
);

-- 8. Bảng SoDiaChi
CREATE TABLE SoDiaChi (
    diaChiID INT IDENTITY(1,1) PRIMARY KEY,
    khachHangID INT NOT NULL,
    tenNguoiNhan NVARCHAR(100) NOT NULL,
    soDienThoaiNhan VARCHAR(15) NOT NULL,
    diaChiGiao NVARCHAR(250) NOT NULL,
    loaiDiaChi NVARCHAR(50) NULL DEFAULT N'Nhà riêng',
    isDefault BIT NOT NULL DEFAULT 0, -- Thêm để bổ sung cho BR02 của UC09
    CONSTRAINT FK_SoDiaChi_KhachHang FOREIGN KEY (khachHangID) REFERENCES KhachHang(khachHangID)
);

-- 9. Bảng DonHangLe
CREATE TABLE DonHangLe (
    donHangLeID INT IDENTITY(1,1) PRIMARY KEY,
    khachHangID INT NULL, -- NULL nếu là khách vãng lai
    diaChiID INT NOT NULL,
    nhanVienID INT NULL, -- Nhân viên/Shipper phụ trách
    ngayDat DATETIME NOT NULL DEFAULT GETDATE(),
    tongTienTamTinh DECIMAL(18,2) NOT NULL CHECK (tongTienTamTinh >= 0),
    tongTienThucTe DECIMAL(18,2) NULL CHECK (tongTienThucTe >= 0),
    tienChenhLech DECIMAL(18,2) NULL DEFAULT 0,
    trangThaiThanhToan NVARCHAR(50) NOT NULL DEFAULT N'Chưa' CHECK (trangThaiThanhToan IN (N'Chưa', N'Đã thanh toán', N'Đang xử lý')),
    phuongThucThanhToan NVARCHAR(50) NOT NULL DEFAULT N'COD',
    khungGioGiaoHang NVARCHAR(100) NOT NULL,
    trangThaiDonHang NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
    PhoneNonAccount varchar(15),
    NameCusNonAccount nvarchar(100),
    AddressCusNonAccount nvarchar(250),
    EmailCusNonAcocunt varchar(100),
    CONSTRAINT FK_DonHangLe_KhachHang FOREIGN KEY (khachHangID) REFERENCES KhachHang(khachHangID),
    CONSTRAINT FK_DonHangLe_SoDiaChi FOREIGN KEY (diaChiID) REFERENCES SoDiaChi(diaChiID),
    CONSTRAINT FK_DonHangLe_NhanVien FOREIGN KEY (nhanVienID) REFERENCES NhanVien(nhanVienID)
);

-- 10. Bảng ChiTietDonHangLe
CREATE TABLE ChiTietDonHangLe (
    donHangLeID INT NOT NULL,
    nongSanID INT NOT NULL,
    soLuongDat INT NOT NULL CHECK (soLuongDat > 0),
    trongLuongThucTe DECIMAL(10,2) NULL CHECK (trongLuongThucTe >= 0),
    donGiaThoiDiem DECIMAL(18,2) NOT NULL CHECK (donGiaThoiDiem >= 0),
    thanhTienThucTe DECIMAL(18,2) NULL CHECK (thanhTienThucTe >= 0),
    PRIMARY KEY (donHangLeID, nongSanID),
    CONSTRAINT FK_CTDHL_DonHang FOREIGN KEY (donHangLeID) REFERENCES DonHangLe(donHangLeID),
    CONSTRAINT FK_CTDHL_NongSan FOREIGN KEY (nongSanID) REFERENCES NongSan(nongSanID)
);

-- 11. Bảng GoiDangKyDinhKy
CREATE TABLE GoiDangKyDinhKy (
    goiID INT IDENTITY(1,1) PRIMARY KEY,
    khachHangID INT NOT NULL,
    diaChiID INT NOT NULL,
    ngayBatDau DATETIME NOT NULL,
    ngayKetThuc DATETIME NOT NULL,
    tanSuatGiao NVARCHAR(50) NOT NULL,
    thuTrongTuan VARCHAR(50) NOT NULL,
    tongTienGoi DECIMAL(18,2) NOT NULL CHECK (tongTienGoi > 0),
    trangThaiGoi NVARCHAR(50) NOT NULL DEFAULT N'Tạm dừng',
    CONSTRAINT FK_GoiDinhKy_KhachHang FOREIGN KEY (khachHangID) REFERENCES KhachHang(khachHangID),
    CONSTRAINT FK_GoiDinhKy_SoDiaChi FOREIGN KEY (diaChiID) REFERENCES SoDiaChi(diaChiID),
    CONSTRAINT CK_GoiDinhKy_Dates CHECK (ngayKetThuc > ngayBatDau)
);

-- 12. Bảng ChiTietGoiDinhKy
CREATE TABLE ChiTietGoiDinhKy (
    goiID INT NOT NULL,
    nongSanID INT NOT NULL,
    soLuongMoiDot DECIMAL(10,2) NOT NULL CHECK (soLuongMoiDot > 0),
    PRIMARY KEY (goiID, nongSanID),
    CONSTRAINT FK_CTGDK_GoiID FOREIGN KEY (goiID) REFERENCES GoiDangKyDinhKy(goiID),
    CONSTRAINT FK_CTGDK_NongSan FOREIGN KEY (nongSanID) REFERENCES NongSan(nongSanID)
);

-- 13. Bảng DotGiaoDinhKy
CREATE TABLE DotGiaoDinhKy (
    dotGiaoID INT IDENTITY(1,1) PRIMARY KEY,
    goiID INT NOT NULL,
    nhanVienID INT NULL,
    ngayGiaoThucTe DATETIME NOT NULL,
    trongLuongThucTeDot DECIMAL(10,2) NULL CHECK (trongLuongThucTeDot >= 0),
    trangThaiGiao NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý',
    CONSTRAINT FK_DotGiao_GoiID FOREIGN KEY (goiID) REFERENCES GoiDangKyDinhKy(goiID),
    CONSTRAINT FK_DotGiao_NhanVien FOREIGN KEY (nhanVienID) REFERENCES NhanVien(nhanVienID)
);

-- 14. Bảng BaoCaoHaoHut
CREATE TABLE BaoCaoHaoHut (
    baoCaoID INT IDENTITY(1,1) PRIMARY KEY,
    nhanVienID INT NOT NULL,
    ngayLap DATETIME NOT NULL DEFAULT GETDATE(),
    lyDoHaoHut NVARCHAR(250) NOT NULL,
    tongGiaTriThietHai DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (tongGiaTriThietHai >= 0),
    CONSTRAINT FK_BaoCaoHH_NhanVien FOREIGN KEY (nhanVienID) REFERENCES NhanVien(nhanVienID)
);

-- 15. Bảng ChiTietBaoCaoHaoHut
CREATE TABLE ChiTietBaoCaoHaoHut (
    baoCaoID INT NOT NULL,
    loHangID INT NOT NULL,
    soLuongHaoHut DECIMAL(10,2) NOT NULL CHECK (soLuongHaoHut > 0),
    donGiaHaoHut DECIMAL(18,2) NOT NULL CHECK (donGiaHaoHut >= 0),
    PRIMARY KEY (baoCaoID, loHangID),
    CONSTRAINT FK_CTBCHH_BaoCao FOREIGN KEY (baoCaoID) REFERENCES BaoCaoHaoHut(baoCaoID),
    CONSTRAINT FK_CTBCHH_LoHang FOREIGN KEY (loHangID) REFERENCES LoHang(loHangID)
);

-- 16. Bảng KhuyenMai
CREATE TABLE KhuyenMai (
    khuyenMaiID INT IDENTITY(1,1) PRIMARY KEY,
    voucherCode VARCHAR(50) NOT NULL UNIQUE,
    tenChuongTrinh NVARCHAR(100) NOT NULL,
    loaiGiamGia INT NOT NULL CHECK (loaiGiamGia IN (1, 2)),
    mucGiam DECIMAL(18,2) NOT NULL CHECK (mucGiam > 0),
    giaTriDonToiThieu DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (giaTriDonToiThieu >= 0),
    soTienGiamToiDa DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (soTienGiamToiDa >= 0),
    soLuotPhatHanh INT NOT NULL CHECK (soLuotPhatHanh > 0),
    soLuotDaDung INT NOT NULL DEFAULT 0,
    ngayBatDau DATETIME NOT NULL,
    ngayKetThuc DATETIME NOT NULL,
    trangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_KhuyenMai_Dates CHECK (ngayKetThuc > ngayBatDau)
);

-- 17. Bảng KhieuNai
CREATE TABLE KhieuNai (
    khieuNaiID INT IDENTITY(1,1) PRIMARY KEY,
    khachHangID INT NOT NULL,
    donHangLeID INT NULL,
    dotGiaoID INT NULL,
    nhanVienID INT NULL,
    noiDung NVARCHAR(MAX) NOT NULL,
    hinhAnhMinhChung VARCHAR(255) NOT NULL,
    ngayGui DATETIME NOT NULL DEFAULT GETDATE(),
    phuongAnXuLy NVARCHAR(500) NULL,
    soTienHoan DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (soTienHoan >= 0),
    trangThai INT NOT NULL DEFAULT 0 CHECK (trangThai IN (0, 1, 2)),
    CONSTRAINT FK_KhieuNai_KhachHang FOREIGN KEY (khachHangID) REFERENCES KhachHang(khachHangID),
    CONSTRAINT FK_KhieuNai_DonHang FOREIGN KEY (donHangLeID) REFERENCES DonHangLe(donHangLeID),
    CONSTRAINT FK_KhieuNai_DotGiao FOREIGN KEY (dotGiaoID) REFERENCES DotGiaoDinhKy(dotGiaoID),
    CONSTRAINT FK_KhieuNai_NhanVien FOREIGN KEY (nhanVienID) REFERENCES NhanVien(nhanVienID)
);

-- 19. Bảng PhieuChiCongNo
CREATE TABLE PhieuChiCongNo (
    phieuChiID INT IDENTITY(1,1) PRIMARY KEY,
    nhaVuonID INT NOT NULL,
    nhanVienID INT NOT NULL,
    ngayLap DATETIME NOT NULL DEFAULT GETDATE(),
    soTienThucChi DECIMAL(18,2) NOT NULL CHECK (soTienThucChi > 0),
    phuongThuc INT NOT NULL CHECK (phuongThuc IN (1, 2)),
    maGiaoDich VARCHAR(100) NULL,
    CONSTRAINT FK_PhieuChi_NhaVuon FOREIGN KEY (nhaVuonID) REFERENCES NhaVuon(nhaVuonID),
    CONSTRAINT FK_PhieuChi_NhanVien FOREIGN KEY (nhanVienID) REFERENCES NhanVien(nhanVienID)
);

-- 20. Bảng DanhGiaSanPham
CREATE TABLE DanhGiaSanPham (
    danhGiaID INT IDENTITY(1,1) PRIMARY KEY,
    khachHangID INT NOT NULL,
    nongSanID INT NOT NULL,
    donHangLeID INT NOT NULL,
    soSao INT NOT NULL CHECK (soSao BETWEEN 1 AND 5),
    binhLuan NVARCHAR(500) NULL,
    hinhAnhThucTe VARCHAR(255) NULL,
    ngayDanhGia DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_DanhGia_DonHang FOREIGN KEY (donHangLeID) REFERENCES DonHangLe(donHangLeID),
    CONSTRAINT FK_DanhGia_KhachHang FOREIGN KEY (khachHangID) REFERENCES KhachHang(khachHangID),
    CONSTRAINT FK_DanhGia_NongSan FOREIGN KEY (nongSanID) REFERENCES NongSan(nongSanID)
);

-- 21. Bảng PhienDangNhap
CREATE TABLE PhienDangNhap (
    tokenID INT IDENTITY(1,1) PRIMARY KEY,
    taiKhoanID INT NOT NULL, -- Đồng bộ chung ID người dùng/nhân viên
    tokenChuoi VARCHAR(500) NOT NULL UNIQUE,
    ngayTao DATETIME NOT NULL DEFAULT GETDATE(),
    ngayHetHan DATETIME NOT NULL,
    thietBi NVARCHAR(150) NULL,
    CONSTRAINT CK_PhienDN_Dates CHECK (ngayHetHan > ngayTao)
);

-- 22. Bảng ChiTietGioHang
CREATE TABLE ChiTietGioHang (
    khachHangID INT NOT NULL,
    nongSanID INT NOT NULL,
    soLuong DECIMAL(10,2) NOT NULL CHECK (soLuong > 0),
    ngayCapNhat DATETIME NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (khachHangID, nongSanID),
    CONSTRAINT FK_GioHang_KhachHang FOREIGN KEY (khachHangID) REFERENCES KhachHang(khachHangID),
    CONSTRAINT FK_GioHang_NongSan FOREIGN KEY (nongSanID) REFERENCES NongSan(nongSanID)
);

-- 23. Bảng GiaoDichThanhToan
CREATE TABLE GiaoDichThanhToan (
    giaoDichID INT IDENTITY(1,1) PRIMARY KEY,
    donHangLeID INT NULL,
    goiDangKyID INT NULL,
    maGiaoDichCong VARCHAR(100) NOT NULL UNIQUE,
    soTien DECIMAL(18,2) NOT NULL CHECK (soTien > 0),
    phuongThuc NVARCHAR(50) NOT NULL,
    ngayGiaoDich DATETIME NOT NULL DEFAULT GETDATE(),
    trangThai INT NOT NULL CHECK (trangThai IN (0, 1, 2)), -- 0: Thất bại, 1: Thành công, 2: Chờ
    CONSTRAINT FK_GiaoDich_DonHang FOREIGN KEY (donHangLeID) REFERENCES DonHangLe(donHangLeID),
    CONSTRAINT FK_GiaoDich_GoiDinhKy FOREIGN KEY (goiDangKyID) REFERENCES GoiDangKyDinhKy(goiID)
);

-- 24. Bảng ThamSo (Bảng lưu trữ thông số cấu hình hệ thống)
CREATE TABLE ThamSo (
    maThamSo VARCHAR(20) PRIMARY KEY,
    giaTri DECIMAL(18,2) NOT NULL,
    ghiChu NVARCHAR(250) NULL
);
GO

-- Chèn dữ liệu mặc định ban đầu cho bảng Tham So
INSERT INTO ThamSo (maThamSo, giaTri, ghiChu) VALUES 
('TS1', 15.00, N'Phần trăm sai số trọng lượng tối đa cho phép khi bốc xếp hàng tươi sống'),
('TS2', 3.00, N'Số ngày tối đa hệ thống cảnh báo trước khi hàng tươi sống hết hạn sử dụng'),
('TS3', 50000.00, N'Số điểm tích lũy tối thiểu của Khách hàng thân thiết để đổi voucher'),
('TS4', 200000.00, N'Số tiền đơn hàng tối thiểu để được áp dụng chính sách miễn phí vận chuyển');
GO

-- Trigger tự động cập nhật tổng tiền thực tế của Đơn Hàng Lẻ khi nhập cân nặng thực tế[cite: 7]
CREATE TRIGGER trg_UpdateTongTienThucTe
ON ChiTietDonHangLe
AFTER UPDATE
AS
BEGIN
    IF UPDATE(trongLuongThucTe)
    BEGIN
        -- Cập nhật thành tiền thực tế của từng dòng chi tiết trước
        UPDATE ChiTietDonHangLe
        SET thanhTienThucTe = i.trongLuongThucTe * ChiTietDonHangLe.donGiaThoiDiem
        FROM ChiTietDonHangLe
        JOIN inserted i ON ChiTietDonHangLe.donHangLeID = i.donHangLeID AND ChiTietDonHangLe.nongSanID = i.nongSanID;

        -- Tính toán lại tổng tiền thực tế trên bảng DonHangLe[cite: 7]
        UPDATE DonHangLe
        SET tongTienThucTe = (SELECT SUM(thanhTienThucTe) FROM ChiTietDonHangLe WHERE ChiTietDonHangLe.donHangLeID = DonHangLe.donHangLeID)
        WHERE donHangLeID IN (SELECT DISTINCT donHangLeID FROM inserted);
    END
END;
GO

alter table DonHangLe
add PhoneNonAccount varchar(15),
    NameCusNonAccount nvarchar(100);

alter table DanhGiaSanPham
add donHangLeID int,
    CONSTRAINT FK_DanhGia_DonHang FOREIGN KEY (donHangLeID) REFERENCES DonHangLe(donHangLeID);

USE ECommerceDB;
GO

-- 1. CHÈN VAI TRÒ & NHÂN VIÊN (Để làm Admin quản lý người dùng / Đăng nhập)
INSERT INTO VaiTroPhanQuyen (tenVaiTro, moTa) VALUES 
(N'Admin', N'Quản trị viên toàn quyền hệ thống'),
(N'ThuKho', N'Nhân viên quản lý nhập xuất kho nông sản'),
(N'KeToan', N'Nhân viên quản lý tài chính và công nợ nhà vườn'),
(N'Shipper', N'Nhân viên giao hàng');

INSERT INTO NhanVien (hoTen, email, matKhau, vaiTroID, trangThai) VALUES 
(N'Ngô Quang Hào', 'haonq@foodmap.vn', 'AdminPass123', 1, 1),
(N'David Nguyen', 'david@foodmap.vn', 'StaffPass123', 2, 1),
(N'Ruby Tran', 'ruby@foodmap.vn', 'StaffPass123', 3, 1);

-- 2. CHÈN DANH MỤC & NHÀ VƯỜN (Để lên bộ lọc và thông tin nguồn gốc ở trang chủ)
INSERT INTO DanhMuc (tenDanhMuc, moTa, loaiHang) VALUES 
(N'Rau Củ Hữu Cơ', N'Các loại rau củ đạt chuẩn hữu cơ, VietGAP', N'Hàng tươi'),
(N'Trái Cây Nhiệt Đới', N'Trái cây tươi ngon thu hoạch từ các vườn miền Tây', N'Hàng tươi'),
(N'Nông Sản Khô & Hạt', N'Các loại hạt dinh dưỡng, đặc sản sấy khô', N'Hàng khô');

INSERT INTO NhaVuon (tenNhaVuon, diaChi, soDienThoai, chungNhanAnToan, cauChuyenNhaVuon) VALUES 
(N'Hợp Tác Xã Nông Nghiệp Xanh Đà Lạt', N'Lạc Dương, Lâm Đồng', '0912345678', 'GlobalGAP', N'Nơi khởi nguồn của những cây rau xứ lạnh được chăm sóc bằng công nghệ tưới nhỏ giọt tự động.'),
(N'Miệt Vườn Cửu Long', N'Cái Bè, Tiền Giang', '0987654321', 'VietGAP', N'Chuyên cung cấp các loại trái cây đặc sản miền Tây sông nước chín cây tự nhiên.');

-- 3. CHÈN NÔNG SẢN MẪU (Dữ liệu quan trọng nhất để hiển thị Trang chủ & Chi tiết)
-- Lưu ý: Link hình ảnh bạn có thể thay đổi tùy theo thư mục lưu ảnh của project web sau này
INSERT INTO NongSan (tenNongSan, moTa, hinhAnh, giaBanNiemYet, donViTinh, saiSoChoPhep, danhMucID, nhaVuonID) VALUES 
(N'Xà Lách Mỹ Đà Lạt', N'Rau xà lách tươi ngon, giòn ngọt, thích hợp làm salad. Trồng theo chuẩn hữu cơ không thuốc trừ sâu.', '/images/products/xa-lach.jpg', 35000.00, N'Bó (500g)', 10, 1, 1),
(N'Cà Chua Beef Premium', N'Cà chua trái lớn, mọng nước, nhiều bột. Giàu vitamin A và C tốt cho sức khỏe.', '/images/products/ca-chua.jpg', 42000.00, N'Kg', 5, 1, 1),
(N'Xoài Cát Hòa Lộc Chín Cây', N'Xoài cát loại 1, vị ngọt đậm đà, hương thơm đặc trưng, thịt quả dày không xơ.', '/images/products/xoai-cat.jpg', 85000.00, N'Kg', 8, 2, 2),
(N'Hạt Điều Rang Muối Vỏ Lụa', N'Hạt điều Bình Phước size lớn, rang củi thủ công giữ trọn vị béo bùi, giòn rụm.', '/images/products/hat-dieu.jpg', 150000.00, N'Hộp (500g)', 2, 3, 2);

-- 5. CHÈN KHÁCH HÀNG & SỔ ĐỊA CHỈ (Để chạy thử luồng Đặt hàng)
INSERT INTO KhachHang (hoTen, soDienThoai, email, matKhauMaHoa, diemTichLuy) VALUES 
(N'Nguyễn Văn A', '0901112222', 'khachhangA@gmail.com', 'CustPass123', 50);

INSERT INTO SoDiaChi (khachHangID, tenNguoiNhan, soDienThoaiNhan, diaChiGiao, loaiDiaChi, isDefault) VALUES 
(1, N'Nguyễn Văn A', '0901112222', N'123 Nguyễn Tri Phương, Quận 10, TP.HCM', N'Nhà riêng', 1);

-- 6. CHÈN ĐƠN HÀNG LẺ (Bao gồm cả Đơn của Khách thành viên và Đơn của Khách vãng lai theo ý cô Thu)
-- Đơn hàng 1: Khách thành viên đã đăng nhập
INSERT INTO DonHangLe (khachHangID, diaChiID, nhanVienID, ngayDat, tongTienTamTinh, tongTienThucTe, tienChenhLech, trangThaiThanhToan, phuongThucThanhToan, khungGioGiaoHang, trangThaiDonHang) VALUES 
(1, 1, NULL, GETDATE(), 77000.00, NULL, 0, N'Chưa', N'COD', N'08:00 - 12:00', N'Chờ duyệt');

INSERT INTO ChiTietDonHangLe (donHangLeID, nongSanID, soLuongDat, trongLuongThucTe, donGiaThoiDiem, thanhTienThucTe) VALUES 
(1, 1, 1, NULL, 35000.00, NULL),
(1, 2, 1, NULL, 42000.00, NULL);

-- Đơn hàng 2: Khách vãng lai mua nhanh không cần tài khoản (Có điền thông tin Phone và Name NonAccount)
INSERT INTO DonHangLe (khachHangID, diaChiID, nhanVienID, ngayDat, tongTienTamTinh, tongTienThucTe, tienChenhLech, trangThaiThanhToan, phuongThucThanhToan, khungGioGiaoHang, trangThaiDonHang, PhoneNonAccount, NameCusNonAccount) VALUES 
(NULL, 1, NULL, GETDATE(), 85000.00, NULL, 0, N'Đã thanh toán', N'VNPAY', N'14:00 - 18:00', N'Chờ duyệt', '0933445566', N'Trần Khách Vãng Lai');

INSERT INTO ChiTietDonHangLe (donHangLeID, nongSanID, soLuongDat, trongLuongThucTe, donGiaThoiDiem, thanhTienThucTe) VALUES 
(2, 3, 1, NULL, 85000.00, NULL);

-- 7. CHÈN BÌNH LUẬN ĐÁNH GIÁ MẪU (Để hiển thị ở Trang Chi tiết sản phẩm như cô Thu lưu ý)
INSERT INTO DanhGiaSanPham (khachHangID, nongSanID, soSao, binhLuan, hinhAnhThucTe, donHangLeID) VALUES 
(1, 1, 5, N'Rau rất tươi, giao hàng nhanh, bọc gói kỹ càng đúng chuẩn Foodmap!', NULL, 1);
GO

select * from NongSan



