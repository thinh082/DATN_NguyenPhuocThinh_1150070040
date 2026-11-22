using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Server.Entities;

namespace Server.Data;

public partial class SmartBusDbContext : DbContext
{
    public SmartBusDbContext(DbContextOptions<SmartBusDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CanhBaoGianLan> CanhBaoGianLans { get; set; }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<ChuyenXe> ChuyenXes { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<GiaVe> GiaVes { get; set; }
    public virtual DbSet<GoiSoatVe> GoiSoatVes { get; set; }
    public virtual DbSet<XuLySuKienOffline> XuLySuKienOfflines { get; set; }

    public virtual DbSet<HoSoUuTien> HoSoUuTiens { get; set; }

    public virtual DbSet<KhoaKySo> KhoaKySos { get; set; }

    public virtual DbSet<LoDongBo> LoDongBos { get; set; }

    public virtual DbSet<LoTrinh> LoTrinhs { get; set; }

    public virtual DbSet<LoTrinhTramDung> LoTrinhTramDungs { get; set; }

    public virtual DbSet<LoaiVe> LoaiVes { get; set; }

    public virtual DbSet<LuotDi> LuotDis { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NhatKyQuetVe> NhatKyQuetVes { get; set; }

    public virtual DbSet<NhatKyThuHoiVe> NhatKyThuHoiVes { get; set; }

    public virtual DbSet<NhomDoiTuong> NhomDoiTuongs { get; set; }

    public virtual DbSet<TinhThanh> TinhThanhs { get; set; }

    public virtual DbSet<ChinhSachMienPhi> ChinhSachMienPhis { get; set; }

    public virtual DbSet<PhuongXa> PhuongXas { get; set; }

    public virtual DbSet<PhienSoatVe> PhienSoatVes { get; set; }

    public virtual DbSet<SuKienThanhToan> SuKienThanhToans { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThietBiSoatVe> ThietBiSoatVes { get; set; }

    public virtual DbSet<TramDung> TramDungs { get; set; }

    public virtual DbSet<TepMinhChung> TepMinhChungs { get; set; }

    public virtual DbSet<TuyenXe> TuyenXes { get; set; }

    public virtual DbSet<Ve> Ves { get; set; }

    public virtual DbSet<XeBuyt> XeBuyts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoiSoatVe>(entity => { entity.ToTable("GoiSoatVe"); entity.HasKey(x => x.id); });
        modelBuilder.Entity<XuLySuKienOffline>(entity => { entity.ToTable("XuLySuKienOffline"); entity.HasKey(x => x.id); });
        modelBuilder.Entity<TepMinhChung>(entity =>
        {
            entity.ToTable("TepMinhChung");
            entity.HasKey(x => x.id);
            entity.HasIndex(x => x.maTep).IsUnique();
            entity.Property(x => x.tenFileGoc).HasMaxLength(255);
            entity.Property(x => x.tenFileLuu).HasMaxLength(255);
            entity.Property(x => x.loaiFile).HasMaxLength(100).IsUnicode(false);
            entity.HasOne(x => x.nguoiDung)
                  .WithMany()
                  .HasForeignKey(x => x.nguoiDungId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CanhBaoGianLan>(entity =>
        {
            entity.ToTable("CanhBaoGianLan");

            entity.HasIndex(e => new { e.trangThai, e.ngayTao }, "IX_CanhBaoGianLan_xuLy");

            entity.HasIndex(e => new { e.nhatKyQuetVeId, e.maCanhBao }, "UQ_CanhBaoGianLan_loai").IsUnique();

            entity.Property(e => e.ketLuan).HasMaxLength(1000);
            entity.Property(e => e.maCanhBao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ngayTao)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.noiDung).HasMaxLength(1000);
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Moi");
            entity.Property(e => e.xuLyLuc).HasPrecision(3);

            entity.HasOne(d => d.nguoiXuLy).WithMany(p => p.CanhBaoGianLans)
                .HasForeignKey(d => d.nguoiXuLyId)
                .HasConstraintName("FK__CanhBaoGi__nguoi__65370702");

            entity.HasOne(d => d.nhatKyQuetVe).WithMany(p => p.CanhBaoGianLans)
                .HasForeignKey(d => d.nhatKyQuetVeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CanhBaoGi__nhatK__625A9A57");
        });

        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.ToTable("ChiTietDonHang");

            entity.HasIndex(e => e.donHangId, "IX_ChiTietDonHang_don");

            entity.Property(e => e.donGiaGoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.mienGiamMoiVe).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.soLuong).HasDefaultValue((short)1);
            entity.Property(e => e.thanhTien)
                .HasComputedColumnSql("(CONVERT([decimal](18,2),[soLuong]*([donGiaGoc]-[mienGiamMoiVe])))", true)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.donHang).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.donHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietDo__donHa__06CD04F7");

            entity.HasOne(d => d.giaVe).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.giaVeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietDo__giaVe__07C12930");
        });

        modelBuilder.Entity<ChuyenXe>(entity =>
        {
            entity.ToTable("ChuyenXe");

            entity.HasIndex(e => new { e.loTrinhId, e.gioKhoiHanhDuKien }, "IX_ChuyenXe_lichTrinh");

            entity.HasIndex(e => new { e.xeBuytId, e.gioKhoiHanhDuKien }, "IX_ChuyenXe_xe");

            entity.Property(e => e.gioKetThucDuKien).HasPrecision(3);
            entity.Property(e => e.gioKetThucThucTe).HasPrecision(3);
            entity.Property(e => e.gioKhoiHanhDuKien).HasPrecision(3);
            entity.Property(e => e.gioKhoiHanhThucTe).HasPrecision(3);
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DuKien");

            entity.HasOne(d => d.loTrinh).WithMany(p => p.ChuyenXes)
                .HasForeignKey(d => d.loTrinhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChuyenXe__loTrin__5AEE82B9");

            entity.HasOne(d => d.xeBuyt).WithMany(p => p.ChuyenXes)
                .HasForeignKey(d => d.xeBuytId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChuyenXe__xeBuyt__5BE2A6F2");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.ToTable("DonHang");

            entity.HasIndex(e => new { e.nguoiDungId, e.ngayTao }, "IX_DonHang_nguoiDung").IsDescending(false, true);

            entity.HasIndex(e => new { e.nguoiDungId, e.maYeuCau }, "UQ_DonHang_chongLap").IsUnique();

            entity.HasIndex(e => e.maDonHang, "UQ_DonHang_ma").IsUnique();

            entity.Property(e => e.hetHanLuc).HasPrecision(3);
            entity.Property(e => e.maDonHang).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ngayTao)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.phienBan)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.tongGiaGoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.tongMienGiam).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.tongThanhToan)
                .HasComputedColumnSql("([tongGiaGoc]-[tongMienGiam])", true)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ChoThanhToan");

            entity.HasOne(d => d.nguoiDung).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.nguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DonHang__nguoiDu__7D439ABD");
        });

        modelBuilder.Entity<GiaVe>(entity =>
        {
            entity.ToTable("GiaVe");

            entity.HasIndex(e => new { e.maLoaiVe, e.nhomDoiTuongId, e.tuyenXeId, e.hieuLucTu }, "IX_GiaVe_traCuu");

            entity.Property(e => e.canCuChinhSach).HasMaxLength(500);
            entity.Property(e => e.giaGoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.hieuLucDen).HasPrecision(3);
            entity.Property(e => e.hieuLucTu).HasPrecision(3);
            entity.Property(e => e.maLoaiVe)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.soTienMienGiam).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.trangThai).HasDefaultValue(true);

            entity.HasOne(d => d.maLoaiVeNavigation).WithMany(p => p.GiaVes)
                .HasForeignKey(d => d.maLoaiVe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GiaVe__maLoaiVe__71D1E811");

            entity.HasOne(d => d.nhomDoiTuong).WithMany(p => p.GiaVes)
                .HasForeignKey(d => d.nhomDoiTuongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GiaVe__nhomDoiTu__72C60C4A");

            entity.HasOne(d => d.tuyenXe).WithMany(p => p.GiaVes)
                .HasForeignKey(d => d.tuyenXeId)
                .HasConstraintName("FK__GiaVe__tuyenXeId__73BA3083");
        });

        modelBuilder.Entity<HoSoUuTien>(entity =>
        {
            entity.ToTable("HoSoUuTien");

            entity.HasIndex(e => new { e.nguoiDungId, e.trangThai, e.hieuLucDen }, "IX_HoSoUuTien_nguoiDung");

            entity.Property(e => e.duongDanMinhChung).HasMaxLength(500);
            entity.Property(e => e.lyDo).HasMaxLength(500);
            entity.Property(e => e.ngayDuyet).HasPrecision(3);
            entity.Property(e => e.ngayTao)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ChoDuyet");

            entity.HasOne(d => d.nguoiDung).WithMany(p => p.HoSoUuTiennguoiDungs)
                .HasForeignKey(d => d.nguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HoSoUuTie__nguoi__33D4B598");

            entity.HasOne(d => d.nguoiDuyet).WithMany(p => p.HoSoUuTiennguoiDuyets)
                .HasForeignKey(d => d.nguoiDuyetId)
                .HasConstraintName("FK__HoSoUuTie__nguoi__36B12243");

            entity.HasOne(d => d.nhomDoiTuong).WithMany(p => p.HoSoUuTiens)
                .HasForeignKey(d => d.nhomDoiTuongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HoSoUuTie__nhomD__34C8D9D1");
        });

        modelBuilder.Entity<KhoaKySo>(entity =>
        {
            entity.ToTable("KhoaKySo");

            entity.HasIndex(e => e.maKhoa, "UQ_KhoaKySo_ma").IsUnique();

            entity.Property(e => e.hetHanLuc).HasPrecision(3);
            entity.Property(e => e.hieuLucTu).HasPrecision(3);
            entity.Property(e => e.khoaCongKhaiPem).HasMaxLength(2000);
            entity.Property(e => e.maKhoa)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.thamChieuKhoaBiMat).HasMaxLength(300);
            entity.Property(e => e.thuatToan)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("ECDSA_P256_SHA256");
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DangDung");
        });

        modelBuilder.Entity<LoDongBo>(entity =>
        {
            entity.ToTable("LoDongBo");

            entity.HasIndex(e => new { e.id, e.thietBiSoatVeId }, "UQ_LoDongBo_thietBi").IsUnique();

            entity.Property(e => e.id).ValueGeneratedNever();
            entity.Property(e => e.hoanTatLuc).HasPrecision(3);
            entity.Property(e => e.nhanLuc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DangXuLy");

            entity.HasOne(d => d.thietBiSoatVe).WithMany(p => p.LoDongBos)
                .HasForeignKey(d => d.thietBiSoatVeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LoDongBo__thietB__41EDCAC5");
        });

        modelBuilder.Entity<LoTrinh>(entity =>
        {
            entity.ToTable("LoTrinh");

            entity.HasIndex(e => new { e.tuyenXeId, e.chieu, e.phienBan }, "UQ_LoTrinh_phienBan").IsUnique();

            entity.Property(e => e.chieu)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.hieuLucDen).HasPrecision(3);
            entity.Property(e => e.hieuLucTu).HasPrecision(3);
            entity.Property(e => e.phienBan).HasDefaultValue((short)1);
            entity.Property(e => e.tenLoTrinh).HasMaxLength(200);

            entity.HasOne(d => d.tuyenXe).WithMany(p => p.LoTrinhs)
                .HasForeignKey(d => d.tuyenXeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LoTrinh__tuyenXe__47DBAE45");
        });

        modelBuilder.Entity<LoTrinhTramDung>(entity =>
        {
            entity.ToTable("LoTrinhTramDung");

            entity.HasIndex(e => new { e.tramDungId, e.loTrinhId }, "IX_LoTrinhTramDung_tram");

            entity.HasIndex(e => new { e.loTrinhId, e.thuTu }, "UQ_LoTrinhTramDung_thuTu").IsUnique();

            entity.Property(e => e.khoangCachTuDauKm).HasColumnType("decimal(8, 3)");

            entity.HasOne(d => d.loTrinh).WithMany(p => p.LoTrinhTramDungs)
                .HasForeignKey(d => d.loTrinhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LoTrinhTr__loTri__4F7CD00D");

            entity.HasOne(d => d.tramDung).WithMany(p => p.LoTrinhTramDungs)
                .HasForeignKey(d => d.tramDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LoTrinhTr__tramD__5070F446");
        });

        modelBuilder.Entity<LoaiVe>(entity =>
        {
            entity.HasKey(e => e.maLoai);

            entity.ToTable("LoaiVe");

            entity.Property(e => e.maLoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.tenLoai).HasMaxLength(100);
            entity.Property(e => e.trangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<LuotDi>(entity =>
        {
            entity.ToTable("LuotDi");

            entity.HasIndex(e => new { e.trangThai, e.congNhanLuc }, "IX_LuotDi_baoCao");

            entity.HasIndex(e => e.nhatKyQuetVeId, "UQ_LuotDi_nhatKy").IsUnique();

            entity.HasIndex(e => new { e.veId, e.buocThoiGian }, "UX_LuotDi_totp")
                .IsUnique()
                .HasFilter("([buocThoiGian] IS NOT NULL AND [trangThai]='HopLe')");

            entity.HasIndex(e => e.veId, "UX_LuotDi_veLuot")
                .IsUnique()
                .HasFilter("([maLoaiVe]='VeLuot' AND [trangThai]='HopLe')");

            entity.Property(e => e.congNhanLuc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.huyLuc).HasPrecision(3);
            entity.Property(e => e.lyDoHuy).HasMaxLength(500);
            entity.Property(e => e.maLoaiVe)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.trangThai)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("HopLe");

            entity.HasOne(d => d.NhatKyQuetVe).WithMany(p => p.LuotDis)
                .HasPrincipalKey(p => new { p.id, p.veId })
                .HasForeignKey(d => new { d.nhatKyQuetVeId, d.veId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LuotDi_nhatKy");

            entity.HasOne(d => d.Ve).WithMany(p => p.LuotDis)
                .HasPrincipalKey(p => new { p.id, p.maLoaiVe })
                .HasForeignKey(d => new { d.veId, d.maLoaiVe })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LuotDi_ve");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.cccd, "UX_NguoiDung_cccd")
                .IsUnique()
                .HasFilter("([cccd] IS NOT NULL)");

            entity.HasIndex(e => e.email, "UX_NguoiDung_email")
                .IsUnique()
                .HasFilter("([email] IS NOT NULL)");

            entity.HasIndex(e => e.soDienThoai, "UX_NguoiDung_soDienThoai")
                .IsUnique()
                .HasFilter("([soDienThoai] IS NOT NULL)");

            entity.Property(e => e.cccd)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.diaChi).HasMaxLength(1000);
            entity.Property(e => e.phuongThucXacThuc)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ngayXacThuc).HasPrecision(3);
            entity.Property(e => e.minhChungXacThuc).HasMaxLength(500);
            entity.HasOne(d => d.tinhThanh).WithMany()
                .HasForeignKey(d => d.maTinhThanh)
                .HasConstraintName("FK_NguoiDung_TinhThanh");
            entity.HasOne(d => d.phuongXa).WithMany()
                .HasForeignKey(d => d.maPhuongXa)
                .HasConstraintName("FK_NguoiDung_PhuongXa");
            entity.Property(e => e.email)
                .HasMaxLength(254)
                .IsUnicode(false);
            entity.Property(e => e.hoTen).HasMaxLength(150);
            entity.Property(e => e.matKhauBam).HasMaxLength(512);
            entity.Property(e => e.ngayCapNhat)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ngayTao)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.phienBan)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.soDienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.trangThai).HasDefaultValue(true);
            entity.Property(e => e.vaiTro)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("HanhKhach");
        });

        modelBuilder.Entity<NhatKyQuetVe>(entity =>
        {
            entity.ToTable("NhatKyQuetVe");

            entity.HasIndex(e => e.loDongBoId, "IX_NhatKyQuetVe_lo").HasFilter("([loDongBoId] IS NOT NULL)");

            entity.HasIndex(e => new { e.phienSoatVeId, e.thoiDiemQuet }, "IX_NhatKyQuetVe_phien");

            entity.HasIndex(e => new { e.veId, e.thoiDiemQuet }, "IX_NhatKyQuetVe_ve");

            entity.HasIndex(e => e.maSuKien, "UQ_NhatKyQuetVe_suKien").IsUnique();

            entity.HasIndex(e => new { e.thietBiSoatVeId, e.soThuTuThietBi }, "UQ_NhatKyQuetVe_thuTu").IsUnique();

            entity.HasIndex(e => new { e.id, e.veId }, "UQ_NhatKyQuetVe_ve").IsUnique();

            entity.Property(e => e.bamDuLieuQr)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.cheDo)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ketQuaTaiThietBi)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.maLyDo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.maVeDocDuoc).HasMaxLength(100);
            entity.Property(e => e.phuongThuc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Qr");
            entity.Property(e => e.thoiDiemNhan)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.thoiDiemQuet).HasPrecision(3);
            entity.Property(e => e.veId).IsRequired();

            entity.HasOne(d => d.thietBiSoatVe).WithMany(p => p.NhatKyQuetVes)
                .HasForeignKey(d => d.thietBiSoatVeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhatKyQue__thiet__4C6B5938");

            entity.HasOne(d => d.ve).WithMany(p => p.NhatKyQuetVes)
                .HasForeignKey(d => d.veId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhatKyQuet__veId__4D5F7D71");

            entity.HasOne(d => d.LoDongBo).WithMany(p => p.NhatKyQuetVes)
                .HasPrincipalKey(p => new { p.id, p.thietBiSoatVeId })
                .HasForeignKey(d => new { d.loDongBoId, d.thietBiSoatVeId })
                .HasConstraintName("FK_NhatKyQuetVe_lo");

            entity.HasOne(d => d.PhienSoatVe).WithMany(p => p.NhatKyQuetVes)
                .HasPrincipalKey(p => new { p.id, p.thietBiSoatVeId })
                .HasForeignKey(d => new { d.phienSoatVeId, d.thietBiSoatVeId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NhatKyQuetVe_phien");
        });

        modelBuilder.Entity<NhatKyThuHoiVe>(entity =>
        {
            entity.ToTable("NhatKyThuHoiVe");

            entity.HasIndex(e => new { e.veId, e.id }, "IX_NhatKyThuHoiVe_ve");

            entity.Property(e => e.hanhDong)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.lyDo).HasMaxLength(500);
            entity.Property(e => e.ngayTao)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.nguoiThucHien).WithMany(p => p.NhatKyThuHoiVes)
                .HasForeignKey(d => d.nguoiThucHienId)
                .HasConstraintName("FK__NhatKyThu__nguoi__3C34F16F");

            entity.HasOne(d => d.ve).WithMany(p => p.NhatKyThuHoiVes)
                .HasForeignKey(d => d.veId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhatKyThuH__veId__3B40CD36");
        });

        modelBuilder.Entity<NhomDoiTuong>(entity =>
        {
            entity.ToTable("NhomDoiTuong");

            entity.HasIndex(e => e.maNhom, "UQ_NhomDoiTuong_maNhom").IsUnique();

            entity.Property(e => e.maNhom)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.tenNhom).HasMaxLength(100);
            entity.Property(e => e.trangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<TinhThanh>(entity =>
        {
            entity.ToTable("TinhThanh");
            entity.HasKey(e => e.ma);
            entity.Property(e => e.ma).ValueGeneratedNever();
            entity.Property(e => e.ten).HasMaxLength(100);
            entity.Property(e => e.loai).HasMaxLength(50);
        });

        modelBuilder.Entity<PhuongXa>(entity =>
        {
            entity.ToTable("PhuongXa");
            entity.HasKey(e => e.ma);
            entity.Property(e => e.ma).ValueGeneratedNever();
            entity.Property(e => e.ten).HasMaxLength(100);
            entity.Property(e => e.loai).HasMaxLength(30);
            entity.HasIndex(e => new { e.maTinhThanh, e.ten }, "IX_PhuongXa_maTinhThanh");
            entity.HasOne(d => d.tinhThanh).WithMany(p => p.PhuongXas)
                .HasForeignKey(d => d.maTinhThanh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhuongXa_TinhThanh");
        });

        modelBuilder.Entity<PhienSoatVe>(entity =>
        {
            entity.ToTable("PhienSoatVe");

            entity.HasIndex(e => e.chuyenXeId, "IX_PhienSoatVe_chuyen");

            entity.HasIndex(e => new { e.id, e.thietBiSoatVeId }, "UQ_PhienSoatVe_thietBi").IsUnique();

            entity.HasIndex(e => e.thietBiSoatVeId, "UX_PhienSoatVe_dangMo")
                .IsUnique()
                .HasFilter("([ketThucLuc] IS NULL)");

            entity.Property(e => e.id).ValueGeneratedNever();
            entity.Property(e => e.batDauLuc).HasPrecision(3);
            entity.Property(e => e.ketThucLuc).HasPrecision(3);

            entity.HasOne(d => d.chuyenXe).WithMany(p => p.PhienSoatVes)
                .HasForeignKey(d => d.chuyenXeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PhienSoat__chuye__68487DD7");

            entity.HasOne(d => d.nhanVien).WithMany(p => p.PhienSoatVes)
                .HasForeignKey(d => d.nhanVienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PhienSoat__nhanV__6A30C649");

            entity.HasOne(d => d.thietBiSoatVe).WithOne(p => p.PhienSoatVe)
                .HasForeignKey<PhienSoatVe>(d => d.thietBiSoatVeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PhienSoat__thiet__693CA210");
        });

        modelBuilder.Entity<SuKienThanhToan>(entity =>
        {
            entity.ToTable("SuKienThanhToan");

            entity.HasIndex(e => new { e.congThanhToan, e.maSuKienCong }, "UQ_SuKienThanhToan_ma").IsUnique();

            entity.Property(e => e.congThanhToan)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.loiXuLy).HasMaxLength(1000);
            entity.Property(e => e.maSuKienCong)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.nhanLuc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.trangThaiXuLy)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ChoXuLy");
            entity.Property(e => e.xuLyLuc).HasPrecision(3);

            entity.HasOne(d => d.thanhToan).WithMany(p => p.SuKienThanhToans)
                .HasForeignKey(d => d.thanhToanId)
                .HasConstraintName("FK__SuKienTha__thanh__17F790F9");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.ToTable("ThanhToan");

            entity.HasIndex(e => new { e.donHangId, e.ngayTao }, "IX_ThanhToan_don");

            entity.HasIndex(e => e.maYeuCau, "UQ_ThanhToan_yeuCau").IsUnique();

            entity.HasIndex(e => new { e.congThanhToan, e.maGiaoDichCong }, "UX_ThanhToan_giaoDich")
                .IsUnique()
                .HasFilter("([maGiaoDichCong] IS NOT NULL)");

            entity.HasIndex(e => e.donHangId, "UX_ThanhToan_motLanThanhCong")
                .IsUnique()
                .HasFilter("([trangThai]='ThanhCong')");

            entity.Property(e => e.congThanhToan)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.donViTien)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("VND")
                .IsFixedLength();
            entity.Property(e => e.maGiaoDichCong)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ngayTao)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.soTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.thanhCongLuc).HasPrecision(3);
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("KhoiTao");

            entity.HasOne(d => d.donHang).WithOne(p => p.ThanhToan)
                .HasForeignKey<ThanhToan>(d => d.donHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ThanhToan__donHa__0E6E26BF");
        });

        modelBuilder.Entity<ThietBiSoatVe>(entity =>
        {
            entity.ToTable("ThietBiSoatVe");

            entity.HasIndex(e => e.maThietBi, "UQ_ThietBiSoatVe_ma").IsUnique();

            entity.Property(e => e.khoaCongKhaiThietBi).HasMaxLength(2000);
            entity.Property(e => e.lanDongBoCuoi).HasPrecision(3);
            entity.Property(e => e.ngayDangKy)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.tenThietBi).HasMaxLength(100);
            entity.Property(e => e.trangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<TramDung>(entity =>
        {
            entity.ToTable("TramDung");

            entity.HasIndex(e => e.maTram, "UQ_TramDung_maTram").IsUnique();

            entity.Property(e => e.diaChi).HasMaxLength(300);
            entity.Property(e => e.kinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.maTram)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.tenTram).HasMaxLength(200);
            entity.Property(e => e.trangThai).HasDefaultValue(true);
            entity.Property(e => e.viDo).HasColumnType("decimal(9, 6)");
        });

        modelBuilder.Entity<TuyenXe>(entity =>
        {
            entity.ToTable("TuyenXe");

            entity.HasIndex(e => e.maTuyen, "UQ_TuyenXe_maTuyen").IsUnique();

            entity.Property(e => e.gioBatDau).HasPrecision(0);
            entity.Property(e => e.gioKetThuc).HasPrecision(0);
            entity.Property(e => e.maTuyen)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.tenTuyen).HasMaxLength(200);
            entity.Property(e => e.trangThai).HasDefaultValue(true);
            entity.Property(e => e.cuLyKm).HasColumnType("decimal(6, 2)");
            entity.HasOne(d => d.chinhSachMienPhi).WithMany(p => p.TuyenXes)
                .HasForeignKey(d => d.chinhSachMienPhiId)
                .HasConstraintName("FK_TuyenXe_ChinhSachMienPhi");
        });

        modelBuilder.Entity<ChinhSachMienPhi>(entity =>
        {
            entity.ToTable("ChinhSachMienPhi");
            entity.HasIndex(e => e.ma, "UQ_ChinhSachMienPhi_ma").IsUnique();
            entity.Property(e => e.ma).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.ten).HasMaxLength(200);
            entity.Property(e => e.canCu).HasMaxLength(500);
            entity.Property(e => e.yeuCauXacThuc).HasDefaultValue(true);
            entity.Property(e => e.trangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<Ve>(entity =>
        {
            entity.ToTable("Ve");

            entity.HasIndex(e => new { e.nguoiDungId, e.trangThai, e.hetHanLuc }, "IX_Ve_nguoiDung");

            entity.HasIndex(e => new { e.chiTietDonHangId, e.soThuTu }, "UQ_Ve_chiTiet").IsUnique();

            entity.HasIndex(e => new { e.id, e.maLoaiVe }, "UQ_Ve_loai").IsUnique();

            entity.HasIndex(e => e.maVe, "UQ_Ve_ma").IsUnique();

            entity.Property(e => e.biMatTotpMaHoa).HasMaxLength(512);
            entity.Property(e => e.chuKy).HasMaxLength(64);
            entity.Property(e => e.chuKyTotpGiay).HasDefaultValue((short)30);
            entity.Property(e => e.duLieuDaKy).HasMaxLength(4000);
            entity.Property(e => e.hetHanLuc).HasPrecision(3);
            entity.Property(e => e.hieuLucTu).HasPrecision(3);
            entity.Property(e => e.maLoaiVe)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.maVe).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ngayPhatHanh)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.phienBan)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.phienBanDuLieu).HasDefaultValue((short)1);
            entity.Property(e => e.phienBanKhoaBaoVe)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.soChuSoTotp).HasDefaultValue((byte)8);
            entity.Property(e => e.soThuTu).HasDefaultValue((short)1);
            entity.Property(e => e.trangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("HoatDong");

            entity.HasOne(d => d.chiTietDonHang).WithMany(p => p.Ves)
                .HasForeignKey(d => d.chiTietDonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ve__chiTietDonHa__29221CFB");

            entity.HasOne(d => d.khoaKySo).WithMany(p => p.Ves)
                .HasForeignKey(d => d.khoaKySoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ve__khoaKySoId__2EDAF651");

            entity.HasOne(d => d.maLoaiVeNavigation).WithMany(p => p.Ves)
                .HasForeignKey(d => d.maLoaiVe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ve__maLoaiVe__2BFE89A6");

            entity.HasOne(d => d.nguoiDung).WithMany(p => p.Ves)
                .HasForeignKey(d => d.nguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ve__nguoiDungId__2B0A656D");

            entity.HasOne(d => d.nhomDoiTuong).WithMany(p => p.Ves)
                .HasForeignKey(d => d.nhomDoiTuongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ve__nhomDoiTuong__2CF2ADDF");

            entity.HasOne(d => d.tuyenXe).WithMany(p => p.Ves)
                .HasForeignKey(d => d.tuyenXeId)
                .HasConstraintName("FK__Ve__tuyenXeId__2DE6D218");
        });

        modelBuilder.Entity<XeBuyt>(entity =>
        {
            entity.ToTable("XeBuyt");

            entity.HasIndex(e => e.bienSo, "UQ_XeBuyt_bienSo").IsUnique();

            entity.HasIndex(e => e.maQrTinh, "UQ_XeBuyt_maQrTinh").IsUnique();

            entity.Property(e => e.bienSo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.maQrTinh).HasDefaultValueSql("(newid())");
            entity.Property(e => e.trangThai).HasDefaultValue(true);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
