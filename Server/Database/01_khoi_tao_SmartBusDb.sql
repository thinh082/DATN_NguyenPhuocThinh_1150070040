/*
  SQL Server 2019+ | Chay toan bo file trong SSMS (Execute/F5).
  Ten DB: SmartBusDb, khop voi appsettings.json. Doi ten trong CREATE/USE neu can.
  Chi khoi tao schema rong; khong xoa/ghi de bang da co.
  Thoi gian luu UTC; tien VND dung decimal(18,2).
  Khong tao tai khoan, mat khau, khoa mat ma hoac ve gia.
*/
USE [master];
GO
IF DB_ID(N'SmartBusDb') IS NULL
    EXEC(N'CREATE DATABASE [SmartBusDb]');
GO
USE [SmartBusDb];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo'))
    THROW 50001, N'DB da co bang dbo. Script nay chi danh cho DB rong; khong tu dong ghi de.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Nguoi dung va xac minh doi tuong uu tien
    CREATE TABLE dbo.NhomDoiTuong (
        id int IDENTITY(1,1) CONSTRAINT PK_NhomDoiTuong PRIMARY KEY,
        maNhom varchar(30) NOT NULL CONSTRAINT UQ_NhomDoiTuong_maNhom UNIQUE,
        tenNhom nvarchar(100) NOT NULL,
        canXacMinh bit NOT NULL DEFAULT 0,
        trangThai bit NOT NULL DEFAULT 1
    );

    CREATE TABLE dbo.NguoiDung (
        id int IDENTITY(1,1) CONSTRAINT PK_NguoiDung PRIMARY KEY,
        hoTen nvarchar(150) NOT NULL,
        soDienThoai varchar(20) NULL,
        email varchar(254) NULL,
        cccd varchar(12) NULL,
        matKhauBam nvarchar(512) NOT NULL,
        vaiTro varchar(30) NOT NULL DEFAULT 'HanhKhach',
        trangThai bit NOT NULL DEFAULT 1,
        ngayTao datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        ngayCapNhat datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        phienBan rowversion NOT NULL,
        CONSTRAINT CK_NguoiDung_vaiTro CHECK (vaiTro IN ('HanhKhach','NhanVienSoatVe','QuanTriVien')),
        CONSTRAINT CK_NguoiDung_lienHe CHECK (soDienThoai IS NOT NULL OR email IS NOT NULL),
        CONSTRAINT CK_NguoiDung_soDienThoai CHECK (soDienThoai IS NULL OR LEN(LTRIM(RTRIM(soDienThoai))) > 0),
        CONSTRAINT CK_NguoiDung_email CHECK (email IS NULL OR LEN(LTRIM(RTRIM(email))) > 0),
        CONSTRAINT CK_NguoiDung_cccd CHECK (cccd IS NULL OR (DATALENGTH(cccd) = 12 AND cccd NOT LIKE '%[^0-9]%'))
    );
    CREATE UNIQUE INDEX UX_NguoiDung_soDienThoai ON dbo.NguoiDung(soDienThoai) WHERE soDienThoai IS NOT NULL;
    CREATE UNIQUE INDEX UX_NguoiDung_email ON dbo.NguoiDung(email) WHERE email IS NOT NULL;
    CREATE UNIQUE INDEX UX_NguoiDung_cccd ON dbo.NguoiDung(cccd) WHERE cccd IS NOT NULL;

    CREATE TABLE dbo.HoSoUuTien (
        id int IDENTITY(1,1) CONSTRAINT PK_HoSoUuTien PRIMARY KEY,
        nguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(id),
        nhomDoiTuongId int NOT NULL REFERENCES dbo.NhomDoiTuong(id),
        duongDanMinhChung nvarchar(500) NOT NULL,
        trangThai varchar(20) NOT NULL DEFAULT 'ChoDuyet',
        hieuLucTu date NOT NULL,
        hieuLucDen date NOT NULL,
        nguoiDuyetId int NULL REFERENCES dbo.NguoiDung(id),
        ngayDuyet datetime2(3) NULL,
        lyDo nvarchar(500) NULL,
        ngayTao datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_HoSoUuTien_thoiHan CHECK (hieuLucDen >= hieuLucTu),
        CONSTRAINT CK_HoSoUuTien_trangThai CHECK (trangThai IN ('ChoDuyet','DaDuyet','TuChoi','ThuHoi')),
        CONSTRAINT CK_HoSoUuTien_duyet CHECK (trangThai <> 'DaDuyet' OR (nguoiDuyetId IS NOT NULL AND ngayDuyet IS NOT NULL))
    );
    CREATE INDEX IX_HoSoUuTien_nguoiDung ON dbo.HoSoUuTien(nguoiDungId, trangThai, hieuLucDen);

    -- 2. Tuyen, lo trinh co phien ban, tram, xe va chuyen van hanh
    CREATE TABLE dbo.TuyenXe (
        id int IDENTITY(1,1) CONSTRAINT PK_TuyenXe PRIMARY KEY,
        maTuyen varchar(20) NOT NULL CONSTRAINT UQ_TuyenXe_maTuyen UNIQUE,
        tenTuyen nvarchar(200) NOT NULL,
        gioBatDau time(0) NOT NULL,
        gioKetThuc time(0) NOT NULL,
        gianCachPhut smallint NULL,
        trangThai bit NOT NULL DEFAULT 1,
        CONSTRAINT CK_TuyenXe_gianCach CHECK (gianCachPhut IS NULL OR gianCachPhut > 0)
    );

    CREATE TABLE dbo.TramDung (
        id int IDENTITY(1,1) CONSTRAINT PK_TramDung PRIMARY KEY,
        maTram varchar(30) NOT NULL CONSTRAINT UQ_TramDung_maTram UNIQUE,
        tenTram nvarchar(200) NOT NULL,
        diaChi nvarchar(300) NULL,
        viDo decimal(9,6) NOT NULL,
        kinhDo decimal(9,6) NOT NULL,
        trangThai bit NOT NULL DEFAULT 1,
        CONSTRAINT CK_TramDung_toaDo CHECK (viDo BETWEEN -90 AND 90 AND kinhDo BETWEEN -180 AND 180)
    );

    CREATE TABLE dbo.LoTrinh (
        id int IDENTITY(1,1) CONSTRAINT PK_LoTrinh PRIMARY KEY,
        tuyenXeId int NOT NULL REFERENCES dbo.TuyenXe(id),
        chieu varchar(5) NOT NULL,
        phienBan smallint NOT NULL DEFAULT 1,
        tenLoTrinh nvarchar(200) NOT NULL,
        hieuLucTu datetime2(3) NOT NULL,
        hieuLucDen datetime2(3) NULL,
        CONSTRAINT UQ_LoTrinh_phienBan UNIQUE (tuyenXeId, chieu, phienBan),
        CONSTRAINT CK_LoTrinh_chieu CHECK (chieu IN ('Di','Ve')),
        CONSTRAINT CK_LoTrinh_phienBan CHECK (phienBan > 0),
        CONSTRAINT CK_LoTrinh_hieuLuc CHECK (hieuLucDen IS NULL OR hieuLucDen > hieuLucTu)
    );

    CREATE TABLE dbo.LoTrinhTramDung (
        id int IDENTITY(1,1) CONSTRAINT PK_LoTrinhTramDung PRIMARY KEY,
        loTrinhId int NOT NULL REFERENCES dbo.LoTrinh(id),
        tramDungId int NOT NULL REFERENCES dbo.TramDung(id),
        thuTu smallint NOT NULL,
        khoangCachTuDauKm decimal(8,3) NULL,
        phutTuDau smallint NULL,
        CONSTRAINT UQ_LoTrinhTramDung_thuTu UNIQUE (loTrinhId, thuTu),
        CONSTRAINT CK_LoTrinhTramDung_giaTri CHECK (thuTu > 0 AND (khoangCachTuDauKm IS NULL OR khoangCachTuDauKm >= 0) AND (phutTuDau IS NULL OR phutTuDau >= 0))
    );
    CREATE INDEX IX_LoTrinhTramDung_tram ON dbo.LoTrinhTramDung(tramDungId, loTrinhId);

    CREATE TABLE dbo.XeBuyt (
        id int IDENTITY(1,1) CONSTRAINT PK_XeBuyt PRIMARY KEY,
        bienSo varchar(20) NOT NULL CONSTRAINT UQ_XeBuyt_bienSo UNIQUE,
        maQrTinh uniqueidentifier NOT NULL DEFAULT NEWID() CONSTRAINT UQ_XeBuyt_maQrTinh UNIQUE,
        sucChua smallint NOT NULL,
        trangThai bit NOT NULL DEFAULT 1,
        CONSTRAINT CK_XeBuyt_sucChua CHECK (sucChua > 0)
    );

    CREATE TABLE dbo.ChuyenXe (
        id bigint IDENTITY(1,1) CONSTRAINT PK_ChuyenXe PRIMARY KEY,
        loTrinhId int NOT NULL REFERENCES dbo.LoTrinh(id),
        xeBuytId int NOT NULL REFERENCES dbo.XeBuyt(id),
        gioKhoiHanhDuKien datetime2(3) NOT NULL,
        gioKetThucDuKien datetime2(3) NOT NULL,
        gioKhoiHanhThucTe datetime2(3) NULL,
        gioKetThucThucTe datetime2(3) NULL,
        trangThai varchar(20) NOT NULL DEFAULT 'DuKien',
        CONSTRAINT CK_ChuyenXe_trangThai CHECK (trangThai IN ('DuKien','DangChay','HoanThanh','Huy')),
        CONSTRAINT CK_ChuyenXe_thoiGian CHECK (gioKetThucDuKien > gioKhoiHanhDuKien AND (gioKetThucThucTe IS NULL OR (gioKhoiHanhThucTe IS NOT NULL AND gioKetThucThucTe >= gioKhoiHanhThucTe)))
    );
    CREATE INDEX IX_ChuyenXe_lichTrinh ON dbo.ChuyenXe(loTrinhId, gioKhoiHanhDuKien);
    CREATE INDEX IX_ChuyenXe_xe ON dbo.ChuyenXe(xeBuytId, gioKhoiHanhDuKien);

    CREATE TABLE dbo.ThietBiSoatVe (
        id int IDENTITY(1,1) CONSTRAINT PK_ThietBiSoatVe PRIMARY KEY,
        maThietBi uniqueidentifier NOT NULL CONSTRAINT UQ_ThietBiSoatVe_ma UNIQUE,
        tenThietBi nvarchar(100) NOT NULL,
        khoaCongKhaiThietBi nvarchar(2000) NULL,
        lanDongBoCuoi datetime2(3) NULL,
        mocThuHoiDaNhan bigint NOT NULL DEFAULT 0,
        trangThai bit NOT NULL DEFAULT 1,
        ngayDangKy datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE TABLE dbo.PhienSoatVe (
        id uniqueidentifier CONSTRAINT PK_PhienSoatVe PRIMARY KEY,
        chuyenXeId bigint NOT NULL REFERENCES dbo.ChuyenXe(id),
        thietBiSoatVeId int NOT NULL REFERENCES dbo.ThietBiSoatVe(id),
        nhanVienId int NOT NULL REFERENCES dbo.NguoiDung(id),
        batDauLuc datetime2(3) NOT NULL,
        ketThucLuc datetime2(3) NULL,
        CONSTRAINT UQ_PhienSoatVe_thietBi UNIQUE (id, thietBiSoatVeId),
        CONSTRAINT CK_PhienSoatVe_thoiGian CHECK (ketThucLuc IS NULL OR ketThucLuc >= batDauLuc)
    );
    CREATE UNIQUE INDEX UX_PhienSoatVe_dangMo ON dbo.PhienSoatVe(thietBiSoatVeId) WHERE ketThucLuc IS NULL;
    CREATE INDEX IX_PhienSoatVe_chuyen ON dbo.PhienSoatVe(chuyenXeId);

    -- 3. Danh muc ve va bieu gia; NULL tuyenXeId = ap dung toan mang luoi
    CREATE TABLE dbo.LoaiVe (
        maLoai varchar(20) CONSTRAINT PK_LoaiVe PRIMARY KEY,
        tenLoai nvarchar(100) NOT NULL,
        trangThai bit NOT NULL DEFAULT 1,
        CONSTRAINT CK_LoaiVe_ma CHECK (maLoai IN ('VeLuot','VeThang','TheMienPhi'))
    );

    CREATE TABLE dbo.GiaVe (
        id int IDENTITY(1,1) CONSTRAINT PK_GiaVe PRIMARY KEY,
        maLoaiVe varchar(20) NOT NULL REFERENCES dbo.LoaiVe(maLoai),
        nhomDoiTuongId int NOT NULL REFERENCES dbo.NhomDoiTuong(id),
        tuyenXeId int NULL REFERENCES dbo.TuyenXe(id),
        giaGoc decimal(18,2) NOT NULL,
        soTienMienGiam decimal(18,2) NOT NULL DEFAULT 0,
        hieuLucTu datetime2(3) NOT NULL,
        hieuLucDen datetime2(3) NULL,
        canCuChinhSach nvarchar(500) NULL,
        trangThai bit NOT NULL DEFAULT 1,
        CONSTRAINT CK_GiaVe_tien CHECK (giaGoc >= 0 AND soTienMienGiam >= 0 AND soTienMienGiam <= giaGoc),
        CONSTRAINT CK_GiaVe_thoiHan CHECK (hieuLucDen IS NULL OR hieuLucDen > hieuLucTu)
    );
    CREATE INDEX IX_GiaVe_traCuu ON dbo.GiaVe(maLoaiVe, nhomDoiTuongId, tuyenXeId, hieuLucTu);

    -- 4. Don hang va thanh toan (bao gom don 0 dong)
    CREATE TABLE dbo.DonHang (
        id bigint IDENTITY(1,1) CONSTRAINT PK_DonHang PRIMARY KEY,
        maDonHang uniqueidentifier NOT NULL DEFAULT NEWID() CONSTRAINT UQ_DonHang_ma UNIQUE,
        nguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(id),
        maYeuCau uniqueidentifier NOT NULL,
        tongGiaGoc decimal(18,2) NOT NULL,
        tongMienGiam decimal(18,2) NOT NULL DEFAULT 0,
        tongThanhToan AS (tongGiaGoc - tongMienGiam) PERSISTED,
        trangThai varchar(20) NOT NULL DEFAULT 'ChoThanhToan',
        ngayTao datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        hetHanLuc datetime2(3) NOT NULL,
        phienBan rowversion NOT NULL,
        CONSTRAINT UQ_DonHang_chongLap UNIQUE (nguoiDungId, maYeuCau),
        CONSTRAINT CK_DonHang_tien CHECK (tongGiaGoc >= 0 AND tongMienGiam >= 0 AND tongMienGiam <= tongGiaGoc),
        CONSTRAINT CK_DonHang_trangThai CHECK (trangThai IN ('ChoThanhToan','DaThanhToan','MienPhi','Huy','HetHan')),
        CONSTRAINT CK_DonHang_mienPhi CHECK (trangThai <> 'MienPhi' OR tongGiaGoc = tongMienGiam),
        CONSTRAINT CK_DonHang_thoiHan CHECK (hetHanLuc > ngayTao)
    );
    CREATE INDEX IX_DonHang_nguoiDung ON dbo.DonHang(nguoiDungId, ngayTao DESC);

    CREATE TABLE dbo.ChiTietDonHang (
        id bigint IDENTITY(1,1) CONSTRAINT PK_ChiTietDonHang PRIMARY KEY,
        donHangId bigint NOT NULL REFERENCES dbo.DonHang(id),
        giaVeId int NOT NULL REFERENCES dbo.GiaVe(id),
        soLuong smallint NOT NULL DEFAULT 1,
        donGiaGoc decimal(18,2) NOT NULL,
        mienGiamMoiVe decimal(18,2) NOT NULL DEFAULT 0,
        thanhTien AS (CONVERT(decimal(18,2), soLuong * (donGiaGoc - mienGiamMoiVe))) PERSISTED,
        CONSTRAINT CK_ChiTietDonHang_giaTri CHECK (soLuong > 0 AND donGiaGoc >= 0 AND mienGiamMoiVe >= 0 AND mienGiamMoiVe <= donGiaGoc)
    );
    CREATE INDEX IX_ChiTietDonHang_don ON dbo.ChiTietDonHang(donHangId);

    CREATE TABLE dbo.ThanhToan (
        id bigint IDENTITY(1,1) CONSTRAINT PK_ThanhToan PRIMARY KEY,
        donHangId bigint NOT NULL REFERENCES dbo.DonHang(id),
        maYeuCau uniqueidentifier NOT NULL CONSTRAINT UQ_ThanhToan_yeuCau UNIQUE,
        congThanhToan varchar(30) NOT NULL,
        maGiaoDichCong varchar(150) NULL,
        soTien decimal(18,2) NOT NULL,
        donViTien char(3) NOT NULL DEFAULT 'VND',
        trangThai varchar(20) NOT NULL DEFAULT 'KhoiTao',
        ngayTao datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        thanhCongLuc datetime2(3) NULL,
        CONSTRAINT CK_ThanhToan_soTien CHECK (soTien > 0 AND donViTien = 'VND'),
        CONSTRAINT CK_ThanhToan_trangThai CHECK (trangThai IN ('KhoiTao','DangXuLy','ThanhCong','ThatBai')),
        CONSTRAINT CK_ThanhToan_thanhCong CHECK (trangThai <> 'ThanhCong' OR (thanhCongLuc IS NOT NULL AND maGiaoDichCong IS NOT NULL))
    );
    CREATE UNIQUE INDEX UX_ThanhToan_giaoDich ON dbo.ThanhToan(congThanhToan, maGiaoDichCong) WHERE maGiaoDichCong IS NOT NULL;
    CREATE UNIQUE INDEX UX_ThanhToan_motLanThanhCong ON dbo.ThanhToan(donHangId) WHERE trangThai = 'ThanhCong';
    CREATE INDEX IX_ThanhToan_don ON dbo.ThanhToan(donHangId, ngayTao);

    CREATE TABLE dbo.SuKienThanhToan (
        id bigint IDENTITY(1,1) CONSTRAINT PK_SuKienThanhToan PRIMARY KEY,
        congThanhToan varchar(30) NOT NULL,
        maSuKienCong varchar(200) NOT NULL,
        thanhToanId bigint NULL REFERENCES dbo.ThanhToan(id),
        noiDung nvarchar(max) NOT NULL,
        chuKyHopLe bit NOT NULL,
        trangThaiXuLy varchar(20) NOT NULL DEFAULT 'ChoXuLy',
        nhanLuc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        xuLyLuc datetime2(3) NULL,
        loiXuLy nvarchar(1000) NULL,
        CONSTRAINT UQ_SuKienThanhToan_ma UNIQUE (congThanhToan, maSuKienCong),
        CONSTRAINT CK_SuKienThanhToan_trangThai CHECK (trangThaiXuLy IN ('ChoXuLy','DaXuLy','BoQua','Loi'))
    );

    -- 5. Khoa ky so va ve; KHONG luu private key ECDSA dang ro trong DB
    CREATE TABLE dbo.KhoaKySo (
        id int IDENTITY(1,1) CONSTRAINT PK_KhoaKySo PRIMARY KEY,
        maKhoa varchar(64) NOT NULL CONSTRAINT UQ_KhoaKySo_ma UNIQUE,
        thuatToan varchar(30) NOT NULL DEFAULT 'ECDSA_P256_SHA256',
        khoaCongKhaiPem nvarchar(2000) NOT NULL,
        thamChieuKhoaBiMat nvarchar(300) NOT NULL,
        hieuLucTu datetime2(3) NOT NULL,
        hetHanLuc datetime2(3) NOT NULL,
        trangThai varchar(20) NOT NULL DEFAULT 'DangDung',
        CONSTRAINT CK_KhoaKySo_thuatToan CHECK (thuatToan = 'ECDSA_P256_SHA256'),
        CONSTRAINT CK_KhoaKySo_trangThai CHECK (trangThai IN ('DangDung','NgungKy','ThuHoi')),
        CONSTRAINT CK_KhoaKySo_thoiHan CHECK (hetHanLuc > hieuLucTu)
    );

    CREATE TABLE dbo.Ve (
        id bigint IDENTITY(1,1) CONSTRAINT PK_Ve PRIMARY KEY,
        maVe uniqueidentifier NOT NULL DEFAULT NEWID() CONSTRAINT UQ_Ve_ma UNIQUE,
        chiTietDonHangId bigint NOT NULL REFERENCES dbo.ChiTietDonHang(id),
        soThuTu smallint NOT NULL DEFAULT 1,
        nguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(id),
        maLoaiVe varchar(20) NOT NULL REFERENCES dbo.LoaiVe(maLoai),
        nhomDoiTuongId int NOT NULL REFERENCES dbo.NhomDoiTuong(id),
        tuyenXeId int NULL REFERENCES dbo.TuyenXe(id),
        khoaKySoId int NOT NULL REFERENCES dbo.KhoaKySo(id),
        phienBanDuLieu smallint NOT NULL DEFAULT 1,
        duLieuDaKy varbinary(4000) NOT NULL,
        chuKy varbinary(64) NOT NULL,
        biMatTotpMaHoa varbinary(512) NOT NULL,
        phienBanKhoaBaoVe varchar(64) NOT NULL,
        chuKyTotpGiay smallint NOT NULL DEFAULT 30,
        soChuSoTotp tinyint NOT NULL DEFAULT 8,
        hieuLucTu datetime2(3) NOT NULL,
        hetHanLuc datetime2(3) NOT NULL,
        trangThai varchar(20) NOT NULL DEFAULT 'HoatDong',
        ngayPhatHanh datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        phienBan rowversion NOT NULL,
        CONSTRAINT UQ_Ve_chiTiet UNIQUE (chiTietDonHangId, soThuTu),
        CONSTRAINT UQ_Ve_loai UNIQUE (id, maLoaiVe),
        CONSTRAINT CK_Ve_soThuTu CHECK (soThuTu > 0 AND phienBanDuLieu > 0),
        CONSTRAINT CK_Ve_thoiHan CHECK (hetHanLuc > hieuLucTu),
        CONSTRAINT CK_Ve_chuKy CHECK (DATALENGTH(chuKy) = 64),
        CONSTRAINT CK_Ve_totp CHECK (chuKyTotpGiay = 30 AND soChuSoTotp = 8),
        CONSTRAINT CK_Ve_trangThai CHECK (trangThai IN ('HoatDong','TamKhoa','ThuHoi'))
    );
    CREATE INDEX IX_Ve_nguoiDung ON dbo.Ve(nguoiDungId, trangThai, hetHanLuc);

    CREATE TABLE dbo.NhatKyThuHoiVe (
        id bigint IDENTITY(1,1) CONSTRAINT PK_NhatKyThuHoiVe PRIMARY KEY,
        veId bigint NOT NULL REFERENCES dbo.Ve(id),
        hanhDong varchar(20) NOT NULL,
        lyDo nvarchar(500) NOT NULL,
        nguoiThucHienId int NULL REFERENCES dbo.NguoiDung(id),
        ngayTao datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_NhatKyThuHoiVe_hanhDong CHECK (hanhDong IN ('TamKhoa','MoKhoa','ThuHoi'))
    );
    CREATE INDEX IX_NhatKyThuHoiVe_ve ON dbo.NhatKyThuHoiVe(veId, id);

    -- 6. Dong bo: nhat ky goc tach khoi luot di duoc server cong nhan
    CREATE TABLE dbo.LoDongBo (
        id uniqueidentifier CONSTRAINT PK_LoDongBo PRIMARY KEY,
        thietBiSoatVeId int NOT NULL REFERENCES dbo.ThietBiSoatVe(id),
        soSuKienKhaiBao int NOT NULL,
        soSuKienDaXuLy int NOT NULL DEFAULT 0,
        trangThai varchar(20) NOT NULL DEFAULT 'DangXuLy',
        nhanLuc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        hoanTatLuc datetime2(3) NULL,
        CONSTRAINT UQ_LoDongBo_thietBi UNIQUE (id, thietBiSoatVeId),
        CONSTRAINT CK_LoDongBo_soLuong CHECK (soSuKienKhaiBao >= 0 AND soSuKienDaXuLy BETWEEN 0 AND soSuKienKhaiBao),
        CONSTRAINT CK_LoDongBo_trangThai CHECK (trangThai IN ('DangXuLy','HoanTat','Loi'))
    );

    CREATE TABLE dbo.NhatKyQuetVe (
        id bigint IDENTITY(1,1) CONSTRAINT PK_NhatKyQuetVe PRIMARY KEY,
        maSuKien uniqueidentifier NOT NULL CONSTRAINT UQ_NhatKyQuetVe_suKien UNIQUE,
        thietBiSoatVeId int NOT NULL REFERENCES dbo.ThietBiSoatVe(id),
        phienSoatVeId uniqueidentifier NOT NULL,
        loDongBoId uniqueidentifier NULL,
        soThuTuThietBi bigint NOT NULL,
        veId bigint NULL REFERENCES dbo.Ve(id),
        maVeDocDuoc nvarchar(100) NULL,
        bamDuLieuQr binary(32) NULL,
        buocThoiGian bigint NULL,
        phuongThuc varchar(20) NOT NULL DEFAULT 'Qr',
        cheDo varchar(10) NOT NULL,
        ketQuaTaiThietBi varchar(20) NOT NULL,
        maLyDo varchar(50) NULL,
        thoiDiemQuet datetime2(3) NOT NULL,
        thoiDiemNhan datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        doLechDongHoGiay int NULL,
        CONSTRAINT UQ_NhatKyQuetVe_thuTu UNIQUE (thietBiSoatVeId, soThuTuThietBi),
        CONSTRAINT UQ_NhatKyQuetVe_ve UNIQUE (id, veId),
        CONSTRAINT FK_NhatKyQuetVe_phien FOREIGN KEY (phienSoatVeId, thietBiSoatVeId) REFERENCES dbo.PhienSoatVe(id, thietBiSoatVeId),
        CONSTRAINT FK_NhatKyQuetVe_lo FOREIGN KEY (loDongBoId, thietBiSoatVeId) REFERENCES dbo.LoDongBo(id, thietBiSoatVeId),
        CONSTRAINT CK_NhatKyQuetVe_thuTu CHECK (soThuTuThietBi > 0 AND (buocThoiGian IS NULL OR buocThoiGian >= 0)),
        CONSTRAINT CK_NhatKyQuetVe_phuongThuc CHECK (phuongThuc IN ('Qr','TraCuuThuCong')),
        CONSTRAINT CK_NhatKyQuetVe_cheDo CHECK (cheDo IN ('Online','Offline')),
        CONSTRAINT CK_NhatKyQuetVe_ketQua CHECK (ketQuaTaiThietBi IN ('ChapNhan','TuChoi','NghiNgo'))
    );
    CREATE INDEX IX_NhatKyQuetVe_ve ON dbo.NhatKyQuetVe(veId, thoiDiemQuet);
    CREATE INDEX IX_NhatKyQuetVe_phien ON dbo.NhatKyQuetVe(phienSoatVeId, thoiDiemQuet);
    CREATE INDEX IX_NhatKyQuetVe_lo ON dbo.NhatKyQuetVe(loDongBoId) WHERE loDongBoId IS NOT NULL;

    CREATE TABLE dbo.LuotDi (
        id bigint IDENTITY(1,1) CONSTRAINT PK_LuotDi PRIMARY KEY,
        nhatKyQuetVeId bigint NOT NULL CONSTRAINT UQ_LuotDi_nhatKy UNIQUE,
        veId bigint NOT NULL,
        maLoaiVe varchar(20) NOT NULL,
        buocThoiGian bigint NULL,
        trangThai varchar(10) NOT NULL DEFAULT 'HopLe',
        congNhanLuc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        huyLuc datetime2(3) NULL,
        lyDoHuy nvarchar(500) NULL,
        CONSTRAINT FK_LuotDi_nhatKy FOREIGN KEY (nhatKyQuetVeId, veId) REFERENCES dbo.NhatKyQuetVe(id, veId),
        CONSTRAINT FK_LuotDi_ve FOREIGN KEY (veId, maLoaiVe) REFERENCES dbo.Ve(id, maLoaiVe),
        CONSTRAINT CK_LuotDi_buoc CHECK (buocThoiGian IS NULL OR buocThoiGian >= 0),
        CONSTRAINT CK_LuotDi_trangThai CHECK (trangThai IN ('HopLe','Huy')),
        CONSTRAINT CK_LuotDi_huy CHECK (trangThai <> 'Huy' OR (huyLuc IS NOT NULL AND lyDoHuy IS NOT NULL))
    );
    -- Ve luot chi co 1 luot duoc cong nhan, ke ca khi 2 may cung dong bo.
    CREATE UNIQUE INDEX UX_LuotDi_veLuot ON dbo.LuotDi(veId) WHERE maLoaiVe = 'VeLuot' AND trangThai = 'HopLe';
    -- QR cua 1 ve trong 1 buoc TOTP chi duoc cong nhan 1 lan.
    CREATE UNIQUE INDEX UX_LuotDi_totp ON dbo.LuotDi(veId, buocThoiGian) WHERE buocThoiGian IS NOT NULL AND trangThai = 'HopLe';
    CREATE INDEX IX_LuotDi_baoCao ON dbo.LuotDi(trangThai, congNhanLuc) INCLUDE (veId, nhatKyQuetVeId);

    CREATE TABLE dbo.CanhBaoGianLan (
        id bigint IDENTITY(1,1) CONSTRAINT PK_CanhBaoGianLan PRIMARY KEY,
        nhatKyQuetVeId bigint NOT NULL REFERENCES dbo.NhatKyQuetVe(id),
        maCanhBao varchar(50) NOT NULL,
        noiDung nvarchar(1000) NOT NULL,
        trangThai varchar(20) NOT NULL DEFAULT 'Moi',
        ngayTao datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        nguoiXuLyId int NULL REFERENCES dbo.NguoiDung(id),
        xuLyLuc datetime2(3) NULL,
        ketLuan nvarchar(1000) NULL,
        CONSTRAINT UQ_CanhBaoGianLan_loai UNIQUE (nhatKyQuetVeId, maCanhBao),
        CONSTRAINT CK_CanhBaoGianLan_trangThai CHECK (trangThai IN ('Moi','DangXuLy','DaXuLy','BoQua'))
    );
    CREATE INDEX IX_CanhBaoGianLan_xuLy ON dbo.CanhBaoGianLan(trangThai, ngayTao);

    INSERT dbo.NhomDoiTuong(maNhom, tenNhom, canXacMinh)
    VALUES ('PhoThong', N'Phổ thông', 0),
           ('HocSinhSinhVien', N'Học sinh, sinh viên', 1),
           ('NguoiCaoTuoi', N'Người cao tuổi', 1);

    INSERT dbo.LoaiVe(maLoai, tenLoai)
    VALUES ('VeLuot', N'Vé lượt'), ('VeThang', N'Vé tháng');

    COMMIT TRANSACTION;
    PRINT N'Khoi tao SmartBusDb thanh cong: 24 bang va du lieu danh muc co ban.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
