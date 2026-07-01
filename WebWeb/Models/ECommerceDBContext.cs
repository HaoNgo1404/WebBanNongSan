using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WebWeb.Models
{
    public partial class ECommerceDBContext : DbContext
    {
        public ECommerceDBContext()
        {
        }

        public ECommerceDBContext(DbContextOptions<ECommerceDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<BaoCaoHaoHut> BaoCaoHaoHuts { get; set; } = null!;
        public virtual DbSet<ChiTietBaoCaoHaoHut> ChiTietBaoCaoHaoHuts { get; set; } = null!;
        public virtual DbSet<ChiTietDonHangLe> ChiTietDonHangLes { get; set; } = null!;
        public virtual DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; } = null!;
        public virtual DbSet<ChiTietGoiDinhKy> ChiTietGoiDinhKies { get; set; } = null!;
        public virtual DbSet<DanhGiaSanPham> DanhGiaSanPhams { get; set; } = null!;
        public virtual DbSet<DanhMuc> DanhMucs { get; set; } = null!;
        public virtual DbSet<DonHangLe> DonHangLes { get; set; } = null!;
        public virtual DbSet<DotGiaoDinhKy> DotGiaoDinhKies { get; set; } = null!;
        public virtual DbSet<GiaoDichThanhToan> GiaoDichThanhToans { get; set; } = null!;
        public virtual DbSet<GoiDangKyDinhKy> GoiDangKyDinhKies { get; set; } = null!;
        public virtual DbSet<KhachHang> KhachHangs { get; set; } = null!;
        public virtual DbSet<KhieuNai> KhieuNais { get; set; } = null!;
        public virtual DbSet<KhuyenMai> KhuyenMais { get; set; } = null!;
        public virtual DbSet<LoHang> LoHangs { get; set; } = null!;
        public virtual DbSet<NhaVuon> NhaVuons { get; set; } = null!;
        public virtual DbSet<NhanVien> NhanViens { get; set; } = null!;
        public virtual DbSet<NongSan> NongSans { get; set; } = null!;
        public virtual DbSet<PhienDangNhap> PhienDangNhaps { get; set; } = null!;
        public virtual DbSet<PhieuChiCongNo> PhieuChiCongNos { get; set; } = null!;
        public virtual DbSet<PhieuNhapKho> PhieuNhapKhos { get; set; } = null!;
        public virtual DbSet<SoDiaChi> SoDiaChis { get; set; } = null!;
        public virtual DbSet<ThamSo> ThamSos { get; set; } = null!;
        public virtual DbSet<VaiTroPhanQuyen> VaiTroPhanQuyens { get; set; } = null!;
        public virtual DbSet<YeuThich> YeuThiches { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=.;Database=ECommerceDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BaoCaoHaoHut>(entity =>
            {
                entity.HasKey(e => e.BaoCaoId)
                    .HasName("PK__BaoCaoHa__7CDB328FC3A04AE7");

                entity.ToTable("BaoCaoHaoHut");

                entity.Property(e => e.BaoCaoId).HasColumnName("baoCaoID");

                entity.Property(e => e.LyDoHaoHut)
                    .HasMaxLength(250)
                    .HasColumnName("lyDoHaoHut");

                entity.Property(e => e.NgayLap)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayLap")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NhanVienId).HasColumnName("nhanVienID");

                entity.Property(e => e.TongGiaTriThietHai)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("tongGiaTriThietHai");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.BaoCaoHaoHuts)
                    .HasForeignKey(d => d.NhanVienId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BaoCaoHH_NhanVien");
            });

            modelBuilder.Entity<ChiTietBaoCaoHaoHut>(entity =>
            {
                entity.HasKey(e => new { e.BaoCaoId, e.LoHangId })
                    .HasName("PK__ChiTietB__FC33BA7D362DD90F");

                entity.ToTable("ChiTietBaoCaoHaoHut");

                entity.Property(e => e.BaoCaoId).HasColumnName("baoCaoID");

                entity.Property(e => e.LoHangId).HasColumnName("loHangID");

                entity.Property(e => e.DonGiaHaoHut)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("donGiaHaoHut");

                entity.Property(e => e.SoLuongHaoHut)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("soLuongHaoHut");

                entity.HasOne(d => d.BaoCao)
                    .WithMany(p => p.ChiTietBaoCaoHaoHuts)
                    .HasForeignKey(d => d.BaoCaoId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CTBCHH_BaoCao");

                entity.HasOne(d => d.LoHang)
                    .WithMany(p => p.ChiTietBaoCaoHaoHuts)
                    .HasForeignKey(d => d.LoHangId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CTBCHH_LoHang");
            });

            modelBuilder.Entity<ChiTietDonHangLe>(entity =>
            {
                entity.HasKey(e => new { e.DonHangLeId, e.NongSanId })
                    .HasName("PK__ChiTietD__BA5491948D183E0D");

                entity.ToTable("ChiTietDonHangLe");

                entity.Property(e => e.DonHangLeId).HasColumnName("donHangLeID");

                entity.Property(e => e.NongSanId).HasColumnName("nongSanID");

                entity.Property(e => e.DonGiaThoiDiem)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("donGiaThoiDiem");

                entity.Property(e => e.SoLuongDat).HasColumnName("soLuongDat");

                entity.Property(e => e.ThanhTienThucTe)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("thanhTienThucTe");

                entity.Property(e => e.TrongLuongThucTe)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("trongLuongThucTe");

                entity.HasOne(d => d.DonHangLe)
                    .WithMany(p => p.ChiTietDonHangLes)
                    .HasForeignKey(d => d.DonHangLeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CTDHL_DonHang");

                entity.HasOne(d => d.NongSan)
                    .WithMany(p => p.ChiTietDonHangLes)
                    .HasForeignKey(d => d.NongSanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CTDHL_NongSan");
            });

            modelBuilder.Entity<ChiTietGioHang>(entity =>
            {
                entity.HasKey(e => new { e.KhachHangId, e.NongSanId })
                    .HasName("PK__ChiTietG__B1DDBF25122460B2");

                entity.ToTable("ChiTietGioHang");

                entity.Property(e => e.KhachHangId).HasColumnName("khachHangID");

                entity.Property(e => e.NongSanId).HasColumnName("nongSanID");

                entity.Property(e => e.NgayCapNhat)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayCapNhat")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.SoLuong)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("soLuong");

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.ChiTietGioHangs)
                    .HasForeignKey(d => d.KhachHangId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GioHang_KhachHang");

                entity.HasOne(d => d.NongSan)
                    .WithMany(p => p.ChiTietGioHangs)
                    .HasForeignKey(d => d.NongSanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GioHang_NongSan");
            });

            modelBuilder.Entity<ChiTietGoiDinhKy>(entity =>
            {
                entity.HasKey(e => new { e.GoiId, e.NongSanId })
                    .HasName("PK__ChiTietG__D1F831676F95410C");

                entity.ToTable("ChiTietGoiDinhKy");

                entity.Property(e => e.GoiId).HasColumnName("goiID");

                entity.Property(e => e.NongSanId).HasColumnName("nongSanID");

                entity.Property(e => e.SoLuongMoiDot)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("soLuongMoiDot");

                entity.HasOne(d => d.Goi)
                    .WithMany(p => p.ChiTietGoiDinhKies)
                    .HasForeignKey(d => d.GoiId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CTGDK_GoiID");

                entity.HasOne(d => d.NongSan)
                    .WithMany(p => p.ChiTietGoiDinhKies)
                    .HasForeignKey(d => d.NongSanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CTGDK_NongSan");
            });

            modelBuilder.Entity<DanhGiaSanPham>(entity =>
            {
                entity.HasKey(e => e.DanhGiaId)
                    .HasName("PK__DanhGiaS__3051F46E5670FA3B");

                entity.ToTable("DanhGiaSanPham");

                entity.Property(e => e.DanhGiaId).HasColumnName("danhGiaID");

                entity.Property(e => e.BinhLuan)
                    .HasMaxLength(500)
                    .HasColumnName("binhLuan");

                entity.Property(e => e.DonHangLeId).HasColumnName("donHangLeID");

                entity.Property(e => e.HinhAnhThucTe)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("hinhAnhThucTe");

                entity.Property(e => e.KhachHangId).HasColumnName("khachHangID");

                entity.Property(e => e.NgayDanhGia)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayDanhGia")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NongSanId).HasColumnName("nongSanID");

                entity.Property(e => e.SoSao).HasColumnName("soSao");

                entity.HasOne(d => d.DonHangLe)
                    .WithMany(p => p.DanhGiaSanPhams)
                    .HasForeignKey(d => d.DonHangLeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DanhGia_DonHang");

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.DanhGiaSanPhams)
                    .HasForeignKey(d => d.KhachHangId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DanhGia_KhachHang");

                entity.HasOne(d => d.NongSan)
                    .WithMany(p => p.DanhGiaSanPhams)
                    .HasForeignKey(d => d.NongSanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DanhGia_NongSan");
            });

            modelBuilder.Entity<DanhMuc>(entity =>
            {
                entity.ToTable("DanhMuc");

                entity.Property(e => e.DanhMucId).HasColumnName("danhMucID");

                entity.Property(e => e.LoaiHang)
                    .HasMaxLength(50)
                    .HasColumnName("loaiHang");

                entity.Property(e => e.MoTa)
                    .HasMaxLength(250)
                    .HasColumnName("moTa");

                entity.Property(e => e.TenDanhMuc)
                    .HasMaxLength(100)
                    .HasColumnName("tenDanhMuc");
            });

            modelBuilder.Entity<DonHangLe>(entity =>
            {
                entity.ToTable("DonHangLe");

                entity.Property(e => e.DonHangLeId).HasColumnName("donHangLeID");

                entity.Property(e => e.AddressNonAccount).HasMaxLength(500);

                entity.Property(e => e.DiaChiId).HasColumnName("diaChiID");

                entity.Property(e => e.EmailCusNonAccount)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.KhachHangId).HasColumnName("khachHangID");

                entity.Property(e => e.KhungGioGiaoHang)
                    .HasMaxLength(100)
                    .HasColumnName("khungGioGiaoHang");

                entity.Property(e => e.NameCusNonAccount).HasMaxLength(100);

                entity.Property(e => e.NgayDat)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayDat")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NhanVienId).HasColumnName("nhanVienID");

                entity.Property(e => e.PhoneNonAccount)
                    .HasMaxLength(15)
                    .IsUnicode(false);

                entity.Property(e => e.PhuongThucThanhToan)
                    .HasMaxLength(50)
                    .HasColumnName("phuongThucThanhToan")
                    .HasDefaultValueSql("(N'COD')");

                entity.Property(e => e.TienChenhLech)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("tienChenhLech")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TongTienTamTinh)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("tongTienTamTinh");

                entity.Property(e => e.TongTienThucTe)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("tongTienThucTe");

                entity.Property(e => e.TrangThaiDonHang)
                    .HasMaxLength(50)
                    .HasColumnName("trangThaiDonHang")
                    .HasDefaultValueSql("(N'Chờ duyệt')");

                entity.Property(e => e.TrangThaiThanhToan)
                    .HasMaxLength(50)
                    .HasColumnName("trangThaiThanhToan")
                    .HasDefaultValueSql("(N'Chưa')");

                entity.HasOne(d => d.DiaChi)
                    .WithMany(p => p.DonHangLes)
                    .HasForeignKey(d => d.DiaChiId)
                    .HasConstraintName("FK_DonHangLe_SoDiaChi");

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.DonHangLes)
                    .HasForeignKey(d => d.KhachHangId)
                    .HasConstraintName("FK_DonHangLe_KhachHang");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.DonHangLes)
                    .HasForeignKey(d => d.NhanVienId)
                    .HasConstraintName("FK_DonHangLe_NhanVien");
            });

            modelBuilder.Entity<DotGiaoDinhKy>(entity =>
            {
                entity.HasKey(e => e.DotGiaoId)
                    .HasName("PK__DotGiaoD__5416AF46A6EDD1E6");

                entity.ToTable("DotGiaoDinhKy");

                entity.Property(e => e.DotGiaoId).HasColumnName("dotGiaoID");

                entity.Property(e => e.GoiId).HasColumnName("goiID");

                entity.Property(e => e.NgayGiaoThucTe)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayGiaoThucTe");

                entity.Property(e => e.NhanVienId).HasColumnName("nhanVienID");

                entity.Property(e => e.TrangThaiGiao)
                    .HasMaxLength(50)
                    .HasColumnName("trangThaiGiao")
                    .HasDefaultValueSql("(N'Chờ xử lý')");

                entity.Property(e => e.TrongLuongThucTeDot)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("trongLuongThucTeDot");

                entity.HasOne(d => d.Goi)
                    .WithMany(p => p.DotGiaoDinhKies)
                    .HasForeignKey(d => d.GoiId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DotGiao_GoiID");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.DotGiaoDinhKies)
                    .HasForeignKey(d => d.NhanVienId)
                    .HasConstraintName("FK_DotGiao_NhanVien");
            });

            modelBuilder.Entity<GiaoDichThanhToan>(entity =>
            {
                entity.HasKey(e => e.GiaoDichId)
                    .HasName("PK__GiaoDich__FDDC6977A1611D7B");

                entity.ToTable("GiaoDichThanhToan");

                entity.HasIndex(e => e.MaGiaoDichCong, "UQ__GiaoDich__F5F9964028B59B9E")
                    .IsUnique();

                entity.Property(e => e.GiaoDichId).HasColumnName("giaoDichID");

                entity.Property(e => e.DonHangLeId).HasColumnName("donHangLeID");

                entity.Property(e => e.GoiDangKyId).HasColumnName("goiDangKyID");

                entity.Property(e => e.MaGiaoDichCong)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("maGiaoDichCong");

                entity.Property(e => e.NgayGiaoDich)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayGiaoDich")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PhuongThuc)
                    .HasMaxLength(50)
                    .HasColumnName("phuongThuc");

                entity.Property(e => e.SoTien)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("soTien");

                entity.Property(e => e.TrangThai).HasColumnName("trangThai");

                entity.HasOne(d => d.DonHangLe)
                    .WithMany(p => p.GiaoDichThanhToans)
                    .HasForeignKey(d => d.DonHangLeId)
                    .HasConstraintName("FK_GiaoDich_DonHang");

                entity.HasOne(d => d.GoiDangKy)
                    .WithMany(p => p.GiaoDichThanhToans)
                    .HasForeignKey(d => d.GoiDangKyId)
                    .HasConstraintName("FK_GiaoDich_GoiDinhKy");
            });

            modelBuilder.Entity<GoiDangKyDinhKy>(entity =>
            {
                entity.HasKey(e => e.GoiId)
                    .HasName("PK__GoiDangK__E020EE5073BD782E");

                entity.ToTable("GoiDangKyDinhKy");

                entity.Property(e => e.GoiId).HasColumnName("goiID");

                entity.Property(e => e.DiaChiId).HasColumnName("diaChiID");

                entity.Property(e => e.KhachHangId).HasColumnName("khachHangID");

                entity.Property(e => e.NgayBatDau)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayBatDau");

                entity.Property(e => e.NgayKetThuc)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayKetThuc");

                entity.Property(e => e.TanSuatGiao)
                    .HasMaxLength(50)
                    .HasColumnName("tanSuatGiao");

                entity.Property(e => e.ThuTrongTuan)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("thuTrongTuan");

                entity.Property(e => e.TongTienGoi)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("tongTienGoi");

                entity.Property(e => e.TrangThaiGoi)
                    .HasMaxLength(50)
                    .HasColumnName("trangThaiGoi")
                    .HasDefaultValueSql("(N'Tạm dừng')");

                entity.HasOne(d => d.DiaChi)
                    .WithMany(p => p.GoiDangKyDinhKies)
                    .HasForeignKey(d => d.DiaChiId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GoiDinhKy_SoDiaChi");

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.GoiDangKyDinhKies)
                    .HasForeignKey(d => d.KhachHangId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GoiDinhKy_KhachHang");
            });

            modelBuilder.Entity<KhachHang>(entity =>
            {
                entity.ToTable("KhachHang");

                entity.HasIndex(e => e.SoDienThoai, "UQ__KhachHan__06ACB9A27AB82B97")
                    .IsUnique();

                entity.HasIndex(e => e.Email, "UQ__KhachHan__AB6E6164233E5467")
                    .IsUnique();

                entity.Property(e => e.KhachHangId).HasColumnName("khachHangID");

                entity.Property(e => e.DiemTichLuy).HasColumnName("diemTichLuy");

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("email");

                entity.Property(e => e.HoTen)
                    .HasMaxLength(100)
                    .HasColumnName("hoTen");

                entity.Property(e => e.MatKhauMaHoa)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("matKhauMaHoa");

                entity.Property(e => e.NgayDangKy)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayDangKy")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.SoDienThoai)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("soDienThoai");
            });

            modelBuilder.Entity<KhieuNai>(entity =>
            {
                entity.ToTable("KhieuNai");

                entity.Property(e => e.KhieuNaiId).HasColumnName("khieuNaiID");

                entity.Property(e => e.DonHangLeId).HasColumnName("donHangLeID");

                entity.Property(e => e.DotGiaoId).HasColumnName("dotGiaoID");

                entity.Property(e => e.HinhAnhMinhChung)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("hinhAnhMinhChung");

                entity.Property(e => e.KhachHangId).HasColumnName("khachHangID");

                entity.Property(e => e.NgayGui)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayGui")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NhanVienId).HasColumnName("nhanVienID");

                entity.Property(e => e.NoiDung).HasColumnName("noiDung");

                entity.Property(e => e.PhuongAnXuLy)
                    .HasMaxLength(500)
                    .HasColumnName("phuongAnXuLy");

                entity.Property(e => e.SoTienHoan)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("soTienHoan");

                entity.Property(e => e.TrangThai).HasColumnName("trangThai");

                entity.HasOne(d => d.DonHangLe)
                    .WithMany(p => p.KhieuNais)
                    .HasForeignKey(d => d.DonHangLeId)
                    .HasConstraintName("FK_KhieuNai_DonHang");

                entity.HasOne(d => d.DotGiao)
                    .WithMany(p => p.KhieuNais)
                    .HasForeignKey(d => d.DotGiaoId)
                    .HasConstraintName("FK_KhieuNai_DotGiao");

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.KhieuNais)
                    .HasForeignKey(d => d.KhachHangId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_KhieuNai_KhachHang");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.KhieuNais)
                    .HasForeignKey(d => d.NhanVienId)
                    .HasConstraintName("FK_KhieuNai_NhanVien");
            });

            modelBuilder.Entity<KhuyenMai>(entity =>
            {
                entity.ToTable("KhuyenMai");

                entity.HasIndex(e => e.VoucherCode, "UQ__KhuyenMa__09FEFFB04DFE65AF")
                    .IsUnique();

                entity.Property(e => e.KhuyenMaiId).HasColumnName("khuyenMaiID");

                entity.Property(e => e.GiaTriDonToiThieu)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("giaTriDonToiThieu");

                entity.Property(e => e.LoaiGiamGia).HasColumnName("loaiGiamGia");

                entity.Property(e => e.MucGiam)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("mucGiam");

                entity.Property(e => e.NgayBatDau)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayBatDau");

                entity.Property(e => e.NgayKetThuc)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayKetThuc");

                entity.Property(e => e.SoLuotDaDung).HasColumnName("soLuotDaDung");

                entity.Property(e => e.SoLuotPhatHanh).HasColumnName("soLuotPhatHanh");

                entity.Property(e => e.SoTienGiamToiDa)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("soTienGiamToiDa");

                entity.Property(e => e.TenChuongTrinh)
                    .HasMaxLength(100)
                    .HasColumnName("tenChuongTrinh");

                entity.Property(e => e.TrangThai)
                    .IsRequired()
                    .HasColumnName("trangThai")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.VoucherCode)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("voucherCode");
            });

            modelBuilder.Entity<LoHang>(entity =>
            {
                entity.ToTable("LoHang");

                entity.Property(e => e.LoHangId).HasColumnName("LoHangID");

                entity.Property(e => e.DonGiaNhap).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.HanSuDung).HasColumnType("datetime");

                entity.Property(e => e.NgayNhapKho)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NgayThuHoach).HasColumnType("datetime");

                entity.Property(e => e.NongSanId).HasColumnName("NongSanID");

                entity.Property(e => e.PhieuNhapId).HasColumnName("PhieuNhapID");

                entity.Property(e => e.SoLuongNhap).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.SoLuongTon).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TrangThaiHsd)
                    .HasMaxLength(50)
                    .HasDefaultValueSql("(N'Còn hạn')");

                entity.HasOne(d => d.NongSan)
                    .WithMany(p => p.LoHangs)
                    .HasForeignKey(d => d.NongSanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoHang_NongSan");

                entity.HasOne(d => d.PhieuNhapKho)
                    .WithMany(p => p.LoHangs)
                    .HasForeignKey(d => d.PhieuNhapId)
                    .HasConstraintName("FK_LoHang_PhieuNhap");
            });

            modelBuilder.Entity<NhaVuon>(entity =>
            {
                entity.ToTable("NhaVuon");

                entity.HasIndex(e => e.SoDienThoai, "UQ__NhaVuon__06ACB9A22D6DF864")
                    .IsUnique();

                entity.Property(e => e.NhaVuonId).HasColumnName("nhaVuonID");

                entity.Property(e => e.CauChuyenNhaVuon).HasColumnName("cauChuyenNhaVuon");

                entity.Property(e => e.ChungNhanAnToan)
                    .HasMaxLength(100)
                    .HasColumnName("chungNhanAnToan");

                entity.Property(e => e.DiaChi)
                    .HasMaxLength(250)
                    .HasColumnName("diaChi");

                entity.Property(e => e.SoDienThoai)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("soDienThoai");

                entity.Property(e => e.TenNhaVuon)
                    .HasMaxLength(150)
                    .HasColumnName("tenNhaVuon");
            });

            modelBuilder.Entity<NhanVien>(entity =>
            {
                entity.ToTable("NhanVien");

                entity.HasIndex(e => e.Email, "UQ__NhanVien__AB6E61646024E2CB")
                    .IsUnique();

                entity.Property(e => e.NhanVienId).HasColumnName("nhanVienID");

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("email");

                entity.Property(e => e.HoTen)
                    .HasMaxLength(100)
                    .HasColumnName("hoTen");

                entity.Property(e => e.MatKhau)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("matKhau");

                entity.Property(e => e.TrangThai)
                    .HasColumnName("trangThai")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.VaiTroId).HasColumnName("vaiTroID");

                entity.HasOne(d => d.VaiTro)
                    .WithMany(p => p.NhanViens)
                    .HasForeignKey(d => d.VaiTroId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_NhanVien_VaiTro");
            });

            modelBuilder.Entity<NongSan>(entity =>
            {
                entity.ToTable("NongSan");

                entity.Property(e => e.NongSanId).HasColumnName("nongSanID");

                entity.Property(e => e.DanhMucId).HasColumnName("danhMucID");

                entity.Property(e => e.DonViTinh)
                    .HasMaxLength(50)
                    .HasColumnName("donViTinh");

                entity.Property(e => e.GiaBanNiemYet)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("giaBanNiemYet");

                entity.Property(e => e.HinhAnh)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("hinhAnh");

                entity.Property(e => e.MoTa).HasColumnName("moTa");

                entity.Property(e => e.NhaVuonId).HasColumnName("nhaVuonID");

                entity.Property(e => e.SaiSoChoPhep)
                    .HasColumnName("saiSoChoPhep")
                    .HasDefaultValueSql("((10))");

                entity.Property(e => e.TenNongSan)
                    .HasMaxLength(150)
                    .HasColumnName("tenNongSan");

                entity.HasOne(d => d.DanhMuc)
                    .WithMany(p => p.NongSans)
                    .HasForeignKey(d => d.DanhMucId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_NongSan_DanhMuc");

                entity.HasOne(d => d.NhaVuon)
                    .WithMany(p => p.NongSans)
                    .HasForeignKey(d => d.NhaVuonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_NongSan_NhaVuon");
            });

            modelBuilder.Entity<PhienDangNhap>(entity =>
            {
                entity.HasKey(e => e.TokenId)
                    .HasName("PK__PhienDan__AC16DAA7788BEE16");

                entity.ToTable("PhienDangNhap");

                entity.HasIndex(e => e.TokenChuoi, "UQ__PhienDan__926C6CDB2B4E9CD6")
                    .IsUnique();

                entity.Property(e => e.TokenId).HasColumnName("tokenID");

                entity.Property(e => e.NgayHetHan)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayHetHan");

                entity.Property(e => e.NgayTao)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayTao")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.TaiKhoanId).HasColumnName("taiKhoanID");

                entity.Property(e => e.ThietBi)
                    .HasMaxLength(150)
                    .HasColumnName("thietBi");

                entity.Property(e => e.TokenChuoi)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("tokenChuoi");
            });

            modelBuilder.Entity<PhieuChiCongNo>(entity =>
            {
                entity.HasKey(e => e.PhieuChiId)
                    .HasName("PK__PhieuChi__9DE1652100A8FC78");

                entity.ToTable("PhieuChiCongNo");

                entity.Property(e => e.PhieuChiId).HasColumnName("phieuChiID");

                entity.Property(e => e.MaGiaoDich)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("maGiaoDich");

                entity.Property(e => e.NgayLap)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayLap")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NhaVuonId).HasColumnName("nhaVuonID");

                entity.Property(e => e.NhanVienId).HasColumnName("nhanVienID");

                entity.Property(e => e.PhuongThuc).HasColumnName("phuongThuc");

                entity.Property(e => e.SoTienThucChi)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("soTienThucChi");

                entity.HasOne(d => d.NhaVuon)
                    .WithMany(p => p.PhieuChiCongNos)
                    .HasForeignKey(d => d.NhaVuonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PhieuChi_NhaVuon");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.PhieuChiCongNos)
                    .HasForeignKey(d => d.NhanVienId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PhieuChi_NhanVien");
            });

            modelBuilder.Entity<PhieuNhapKho>(entity =>
            {
                entity.HasKey(e => e.PhieuNhapId)
                    .HasName("PK__PhieuNha__F80BA12894B49095");

                entity.ToTable("PhieuNhapKho");

                entity.Property(e => e.PhieuNhapId).HasColumnName("phieuNhapID");

                entity.Property(e => e.NgayLapPhieu)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayLapPhieu")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NhaVuonId).HasColumnName("nhaVuonID");

                entity.Property(e => e.NhanVienId).HasColumnName("nhanVienID");

                entity.Property(e => e.TongTienNhap)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("tongTienNhap");

                entity.HasOne(d => d.NhaVuon)
                    .WithMany(p => p.PhieuNhapKhos)
                    .HasForeignKey(d => d.NhaVuonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PhieuNhap_NhaVuon");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.PhieuNhapKhos)
                    .HasForeignKey(d => d.NhanVienId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PhieuNhap_NhanVien");
            });

            modelBuilder.Entity<SoDiaChi>(entity =>
            {
                entity.HasKey(e => e.DiaChiId)
                    .HasName("PK__SoDiaChi__31BFEC317208405E");

                entity.ToTable("SoDiaChi");

                entity.Property(e => e.DiaChiId).HasColumnName("diaChiID");

                entity.Property(e => e.DiaChiGiao)
                    .HasMaxLength(250)
                    .HasColumnName("diaChiGiao");

                entity.Property(e => e.IsDefault).HasColumnName("isDefault");

                entity.Property(e => e.KhachHangId).HasColumnName("khachHangID");

                entity.Property(e => e.LoaiDiaChi)
                    .HasMaxLength(50)
                    .HasColumnName("loaiDiaChi")
                    .HasDefaultValueSql("(N'Nhà riêng')");

                entity.Property(e => e.SoDienThoaiNhan)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("soDienThoaiNhan");

                entity.Property(e => e.TenNguoiNhan)
                    .HasMaxLength(100)
                    .HasColumnName("tenNguoiNhan");

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.SoDiaChis)
                    .HasForeignKey(d => d.KhachHangId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SoDiaChi_KhachHang");
            });

            modelBuilder.Entity<ThamSo>(entity =>
            {
                entity.HasKey(e => e.MaThamSo)
                    .HasName("PK__ThamSo__06CFCFB926FD4AFD");

                entity.ToTable("ThamSo");

                entity.Property(e => e.MaThamSo)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("maThamSo");

                entity.Property(e => e.GhiChu)
                    .HasMaxLength(250)
                    .HasColumnName("ghiChu");

                entity.Property(e => e.GiaTri)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("giaTri");
            });

            modelBuilder.Entity<VaiTroPhanQuyen>(entity =>
            {
                entity.HasKey(e => e.VaiTroId)
                    .HasName("PK__VaiTroPh__846C744F132BEA1F");

                entity.ToTable("VaiTroPhanQuyen");

                entity.HasIndex(e => e.TenVaiTro, "UQ__VaiTroPh__BDF7AFDF9F95903F")
                    .IsUnique();

                entity.Property(e => e.VaiTroId).HasColumnName("vaiTroID");

                entity.Property(e => e.MoTa)
                    .HasMaxLength(255)
                    .HasColumnName("moTa");

                entity.Property(e => e.TenVaiTro)
                    .HasMaxLength(50)
                    .HasColumnName("tenVaiTro");
            });

            modelBuilder.Entity<YeuThich>(entity =>
            {
                entity.HasKey(e => new { e.KhachHangId, e.NongSanId })
                    .HasName("PK__YeuThich__BD733A23C3C65D0D");

                entity.ToTable("YeuThich");

                entity.Property(e => e.NgayThem)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.YeuThiches)
                    .HasForeignKey(d => d.KhachHangId)
                    .HasConstraintName("FK__YeuThich__KhachH__72910220");

                entity.HasOne(d => d.NongSan)
                    .WithMany(p => p.YeuThiches)
                    .HasForeignKey(d => d.NongSanId)
                    .HasConstraintName("FK__YeuThich__NongSa__73852659");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
