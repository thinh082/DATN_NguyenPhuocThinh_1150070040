-- 08. Chinh sach mien phi theo tuyen (NQ 09/2026 Dieu 3, Dieu 4), the di xe mien phi, bo sung thong tin 134 tuyen
-- va nhap 24 tuyen tinh phi tu file word/xe_buyt_tphcm_2025.xlsx (nguon vietnammoi.vn, 05/2025).
-- Idempotent, chi them/cap nhat. Chay: sqlcmd -S localhost -E -C -b -I -f i:65001 -d SmartBusDb -i "Server/Database/08_chinh_sach_mien_phi_tuyen.sql"
-- Sau script nay chay lai 05_seed_chuyen_xe_134_tuyen.sql de tao lo trinh/xe/chuyen dang chay cho cac tuyen tinh phi moi.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.ChinhSachMienPhi', N'U') IS NULL
    CREATE TABLE dbo.ChinhSachMienPhi (
        id int IDENTITY(1,1) CONSTRAINT PK_ChinhSachMienPhi PRIMARY KEY,
        ma varchar(30) NOT NULL CONSTRAINT UQ_ChinhSachMienPhi_ma UNIQUE,
        ten nvarchar(200) NOT NULL,
        canCu nvarchar(500) NOT NULL,
        hieuLucTu date NOT NULL,
        hieuLucDen date NOT NULL,
        yeuCauXacThuc bit NOT NULL CONSTRAINT DF_ChinhSachMienPhi_yeuCauXacThuc DEFAULT 1,
        trangThai bit NOT NULL CONSTRAINT DF_ChinhSachMienPhi_trangThai DEFAULT 1
    );
GO
IF COL_LENGTH(N'dbo.TuyenXe', N'chinhSachMienPhiId') IS NULL
    ALTER TABLE dbo.TuyenXe ADD chinhSachMienPhiId int NULL CONSTRAINT FK_TuyenXe_ChinhSachMienPhi REFERENCES dbo.ChinhSachMienPhi(id);
IF COL_LENGTH(N'dbo.TuyenXe', N'cuLyKm') IS NULL
    ALTER TABLE dbo.TuyenXe ADD cuLyKm decimal(6,2) NULL;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_LoaiVe_ma' AND definition NOT LIKE '%TheMienPhi%')
    ALTER TABLE dbo.LoaiVe DROP CONSTRAINT CK_LoaiVe_ma;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_LoaiVe_ma')
    ALTER TABLE dbo.LoaiVe ADD CONSTRAINT CK_LoaiVe_ma CHECK (maLoai IN ('VeLuot','VeThang','TheMienPhi'));
GO
BEGIN TRANSACTION;
MERGE dbo.ChinhSachMienPhi AS t USING (VALUES
    ('NQ09_DIEU3', N'NQ 09/2026 - Điều 3: tuyến được ngân sách hỗ trợ kinh phí hoạt động', N'Điều 3 Nghị quyết số 09/2026/NQ-HĐND ngày 19/06/2026 của HĐND TP.HCM (Phụ lục 01, 109 tuyến)'),
    ('NQ09_DIEU4', N'NQ 09/2026 - Điều 4: tuyến không được ngân sách hỗ trợ kinh phí hoạt động', N'Điều 4 Nghị quyết số 09/2026/NQ-HĐND ngày 19/06/2026 của HĐND TP.HCM (Phụ lục 02, 25 tuyến)')
) AS s(ma, ten, canCu) ON t.ma = s.ma
WHEN MATCHED THEN UPDATE SET ten = s.ten, canCu = s.canCu
WHEN NOT MATCHED THEN INSERT (ma, ten, canCu, hieuLucTu, hieuLucDen, yeuCauXacThuc, trangThai) VALUES (s.ma, s.ten, s.canCu, '2026-07-01', '2026-12-31', 1, 1);

DECLARE @dieu3 int = (SELECT id FROM dbo.ChinhSachMienPhi WHERE ma = 'NQ09_DIEU3');
DECLARE @dieu4 int = (SELECT id FROM dbo.ChinhSachMienPhi WHERE ma = 'NQ09_DIEU4');
DECLARE @PL02 TABLE (maTuyen varchar(20) PRIMARY KEY);
INSERT INTO @PL02 VALUES ('13'), ('94'), ('75'), ('67'), ('109'), ('171'), ('61-3'), ('61-2'), ('61-4'), ('61-8'), ('61-9'), ('61-77'), ('61-37'), ('61-38'), ('61-39'), ('61-51'), ('61-52'), ('61-53'), ('61-55'), ('61-67'), ('61-68'), ('61-70'), ('61-73'), ('72-4'), ('173');
DECLARE @NQ09 TABLE (maTuyen varchar(20) PRIMARY KEY);
INSERT INTO @NQ09 VALUES ('01'), ('03'), ('04'), ('06'), ('07'), ('08'), ('09'), ('10'), ('100'), ('101'), ('102'), ('103'), ('104'), ('107'), ('109'), ('110'), ('122'), ('126'), ('127'), ('128'), ('13'), ('139'), ('14'), ('140'), ('141'), ('145'), ('146'), ('148'), ('15'), ('150'), ('151'), ('152'), ('153'), ('154'), ('155'), ('156'), ('157'), ('158'), ('159'), ('16'), ('160'), ('161'), ('162'), ('163'), ('164'), ('165'), ('166'), ('167'), ('168'), ('169'), ('170'), ('171'), ('173'), ('18'), ('19'), ('20'), ('22'), ('23'), ('24'), ('25'), ('27'), ('28'), ('29'), ('30'), ('31'), ('32'), ('33'), ('34'), ('36'), ('38'), ('39'), ('41'), ('43'), ('44'), ('45'), ('46'), ('47'), ('48'), ('50'), ('52'), ('53'), ('55'), ('56'), ('57'), ('58'), ('59'), ('61'), ('61-2'), ('61-3'), ('61-37'), ('61-38'), ('61-39'), ('61-4'), ('61-51'), ('61-52'), ('61-53'), ('61-55'), ('61-67'), ('61-68'), ('61-70'), ('61-73'), ('61-77'), ('61-8'), ('61-9'), ('62'), ('64'), ('65'), ('67'), ('68'), ('69'), ('70'), ('71'), ('72'), ('72-4'), ('73'), ('74'), ('75'), ('76'), ('77'), ('78'), ('79'), ('81'), ('84'), ('85'), ('86'), ('87'), ('88'), ('89'), ('90'), ('91'), ('93'), ('94'), ('99'), ('D4');
UPDATE t SET chinhSachMienPhiId = CASE WHEN p.maTuyen IS NOT NULL THEN @dieu4 ELSE @dieu3 END
FROM dbo.TuyenXe t JOIN @NQ09 n ON n.maTuyen = t.maTuyen LEFT JOIN @PL02 p ON p.maTuyen = t.maTuyen;

IF NOT EXISTS (SELECT 1 FROM dbo.LoaiVe WHERE maLoai = 'TheMienPhi')
    INSERT INTO dbo.LoaiVe (maLoai, tenLoai, trangThai) VALUES ('TheMienPhi', N'Thẻ đi xe miễn phí', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.GiaVe WHERE maLoaiVe = 'TheMienPhi')
    INSERT INTO dbo.GiaVe (maLoaiVe, nhomDoiTuongId, tuyenXeId, giaGoc, soTienMienGiam, hieuLucTu, hieuLucDen, canCuChinhSach, trangThai)
    SELECT 'TheMienPhi', id, NULL, 0, 0, '2026-07-01', '2026-12-31 23:59:59', N'NQ 09/2026/NQ-HĐND - miễn 100% giá vé 134 tuyến (Điều 3, Điều 4)', 1
    FROM dbo.NhomDoiTuong WHERE maNhom = 'PhoThong';

-- Bo sung cu ly, gio hoat dong, gian cach cho 110 tuyen NQ 09 co trong file Excel (giu nguyen ten tuyen theo NQ 09)
DECLARE @BoSung TABLE (maTuyen varchar(20) PRIMARY KEY, cuLyKm decimal(6,2) NULL, gioBatDau time(0) NULL, gioKetThuc time(0) NULL, gianCachPhut smallint NULL);
INSERT INTO @BoSung VALUES
    ('01', 8.74, '05:00:00', '20:15:00', 15),
    ('03', 20.6, '04:00:00', '20:45:00', 7),
    ('04', 16.42, '05:00:00', '20:15:00', 10),
    ('06', 26.5, '04:55:00', '21:00:00', 10),
    ('07', 15.5, '05:00:00', '19:30:00', 15),
    ('08', 32.7, '04:40:00', '20:30:00', 3),
    ('09', 25, '03:45:00', '19:30:00', 8),
    ('10', 30.85, '04:45:00', '21:00:00', 15),
    ('100', 16.8, '05:00:00', '19:10:00', 10),
    ('101', 19.85, '05:00:00', '18:30:00', 12),
    ('102', 32.72, '04:30:00', '19:00:00', 12),
    ('103', 24.9, '05:00:00', '19:00:00', 12),
    ('104', 30.5, '04:40:00', '20:00:00', 6),
    ('107', 15.6, '05:00:00', '19:00:00', 20),
    ('109', 9.3, '05:30:00', '22:40:00', 15),
    ('110', 26.3, '05:00:00', '19:45:00', 20),
    ('122', 19.76, '04:45:00', '19:30:00', 6),
    ('126', 19.4, '05:00:00', '18:50:00', 9),
    ('127', 18, '05:30:00', '20:00:00', 10),
    ('128', 24.9, '05:00:00', '20:00:00', 8),
    ('13', 36.3, '04:00:00', '19:45:00', 20),
    ('139', 22, '04:45:00', '19:00:00', 15),
    ('14', 16.4, '04:00:00', '20:00:00', 8),
    ('140', 21.4, '05:00:00', '19:30:00', 20),
    ('141', 21.8, '05:00:00', '19:30:00', 7),
    ('145', 23, '05:00:00', '19:30:00', 17),
    ('146', 19, '04:45:00', '19:00:00', 12),
    ('148', 16.7, '04:45:00', '19:30:00', 15),
    ('15', 17.1, '05:00:00', '19:00:00', 20),
    ('150', 30.9, '04:30:00', '21:15:00', 4),
    ('151', 17.2, '04:00:00', '20:00:00', 7),
    ('152', 14.55, '05:15:00', '19:00:00', 15),
    ('153', 16.7, '05:00:00', '22:00:00', 10),
    ('154', 17.3, '05:00:00', '22:05:00', 10),
    ('155', 6.4, '05:00:00', '22:00:00', 15),
    ('157', 15.7, '05:00:00', '22:05:00', 10),
    ('158', 8.3, '05:00:00', '22:00:00', 10),
    ('159', 5.2, '05:00:00', '22:00:00', 10),
    ('16', 16.8, '05:00:00', '19:00:00', 12),
    ('160', 4.1, '05:00:00', '22:00:00', 15),
    ('161', 15, '05:00:00', '22:05:00', 8),
    ('162', 9.5, '05:00:00', '22:05:00', 10),
    ('163', 8, '05:00:00', '22:00:00', 10),
    ('164', 10.4, '05:00:00', '22:00:00', 10),
    ('165', 20.4, '05:00:00', '22:00:00', 10),
    ('18', 22.73, '04:30:00', '20:30:00', 11),
    ('19', 27.43, '05:00:00', '20:15:00', 8),
    ('20', 16.93, '04:20:00', '21:15:00', 5),
    ('22', 22.75, '04:20:00', '19:00:00', 10),
    ('23', 23.45, '04:00:00', '19:30:00', 12),
    ('24', 26.1, '04:00:00', '20:30:00', 6),
    ('25', 22.3, '04:50:00', '19:00:00', 13),
    ('27', 15.6, '05:00:00', '20:00:00', 12),
    ('28', 26.45, '04:45:00', '19:00:00', 15),
    ('29', 20.75, '05:00:00', '19:30:00', 8),
    ('30', 18.3, '05:00:00', '19:30:00', 12),
    ('31', 21.27, '05:00:00', '19:00:00', 12),
    ('32', 24.8, '04:00:00', '19:30:00', 9),
    ('33', 29.05, '04:30:00', '21:15:00', 4),
    ('34', 22.05, '05:00:00', '19:00:00', 12),
    ('36', 18.97, '04:45:00', '20:00:00', 12),
    ('38', 16.57, '05:15:00', '19:00:00', 15),
    ('39', 17.57, '05:00:00', '19:30:00', 15),
    ('41', 22.15, '05:00:00', '19:20:00', 10),
    ('43', 15.8, '05:00:00', '19:00:00', 18),
    ('44', 16.75, '05:00:00', '19:00:00', 15),
    ('45', 22.3, '04:45:00', '20:00:00', 12),
    ('46', 19.75, '05:00:00', '18:45:00', 12),
    ('47', 17.3, '04:00:00', '19:00:00', 10),
    ('48', 21, '04:30:00', '19:45:00', 13),
    ('50', 28.6, '05:30:00', '18:30:00', 15),
    ('52', 22.53, '05:10:00', '18:15:00', 10),
    ('53', 32.8, '05:00:00', '19:30:00', 4),
    ('55', 16.8, '05:00:00', '19:30:00', 10),
    ('56', 29.35, '05:00:00', '21:00:00', 9),
    ('57', 13.1, '05:00:00', '18:30:00', 10),
    ('58', 20.8, '05:00:00', '19:00:00', 15),
    ('59', 23.7, '04:50:00', '19:00:00', 16),
    ('61', 17.73, '04:45:00', '19:00:00', 12),
    ('61-3', 34, '06:00:00', '16:30:00', 90),
    ('61-8', 22, '05:30:00', '19:30:00', 15),
    ('61-9', 33, '05:40:00', '17:20:00', 60),
    ('62', 24, '05:00:00', '19:00:00', 15),
    ('64', 18, '05:30:00', '19:00:00', 15),
    ('65', 16.3, '04:45:00', '20:45:00', 10),
    ('67', 19.2, '03:00:00', '22:45:00', 30),
    ('68', 20.15, '05:00:00', '19:00:00', 12),
    ('69', 22.45, '05:00:00', '20:00:00', 12),
    ('70', 25.55, '04:00:00', '20:20:00', 15),
    ('71', 24.2, '05:20:00', '19:00:00', 15),
    ('72', 21.6, '04:40:00', '19:15:00', 7),
    ('73', 22.35, '04:30:00', '19:00:00', 12),
    ('74', 21.85, '03:30:00', '21:00:00', 6),
    ('75', 64.2, '04:00:00', '16:30:00', 167),
    ('76', 23.8, '04:50:00', '19:00:00', 10),
    ('77', 16.3, '05:30:00', '19:45:00', 10),
    ('78', 18.5, '05:00:00', '20:00:00', 15),
    ('79', 25, '05:00:00', '20:00:00', 15),
    ('81', 22.4, '04:15:00', '19:50:00', 6),
    ('84', 17.3, '04:40:00', '19:30:00', 12),
    ('85', 14.86, '04:35:00', '19:30:00', 12),
    ('87', 20.8, '05:00:00', '18:30:00', 10),
    ('88', 23.6, '04:45:00', '19:00:00', 10),
    ('89', 17.1, '05:00:00', '19:00:00', 12),
    ('90', 45.6, '04:30:00', '20:00:00', 8),
    ('91', 25.1, '04:30:00', '21:00:00', 15),
    ('93', 26.2, '04:30:00', '19:15:00', 12),
    ('94', 36.1, '04:00:00', '19:00:00', 12),
    ('99', 27.5, '05:00:00', '19:30:00', 8),
    ('D4', 29.5, '05:00:00', '22:00:00', 15);
UPDATE t SET cuLyKm = COALESCE(b.cuLyKm, t.cuLyKm), gioBatDau = COALESCE(b.gioBatDau, t.gioBatDau),
    gioKetThuc = COALESCE(b.gioKetThuc, t.gioKetThuc), gianCachPhut = COALESCE(b.gianCachPhut, t.gianCachPhut)
FROM dbo.TuyenXe t JOIN @BoSung b ON b.maTuyen = t.maTuyen;

-- Nhap 24 tuyen tinh phi (khong thuoc 134 tuyen NQ 09); gia ve luot = muc cao nhat trong bai viet
DECLARE @TinhPhi TABLE (maTuyen varchar(20) PRIMARY KEY, tenTuyen nvarchar(200) NOT NULL, cuLyKm decimal(6,2) NULL, gioBatDau time(0) NOT NULL, gioKetThuc time(0) NOT NULL, gianCachPhut smallint NULL, giaVe decimal(18,2) NOT NULL);
INSERT INTO @TinhPhi VALUES
    ('05', N'Bến xe buýt Chợ Lớn - Bến xe Biên Hòa', 38, '04:50:00', '17:50:00', 20, 30000),
    ('60-1', N'Bến xe Miền Tây - Bến xe Biên Hòa', 62.2, '04:45:00', '18:30:00', 20, 45000),
    ('60-2', N'Đại học Nông Lâm - Bến xe Phú Túc', 71, '05:00:00', '18:30:00', 10, 40000),
    ('60-3', N'Bến xe Miền Đông (cũ) - Khu Công nghiệp Nhơn Trạch', 56, '04:45:00', '18:00:00', 25, 45000),
    ('60-5', N'Bến xe An Sương - Bến xe Biên Hòa', 37.4, '05:00:00', '18:00:00', 30, 30000),
    ('60-7', N'Bến xe buýt Tân Phú - Bến xe Biên Hòa', 47, '05:00:00', '18:05:00', 35, 40000),
    ('61-05', N'Thủ Dầu Một - Tân Vạn - Bến xe Miền Đông mới', 30, '05:35:00', '18:30:00', 40, 35000),
    ('61-7', N'Bến đò Bình Mỹ - Bến xe Bình Dương', 6.6, '04:45:00', '19:30:00', 9, 8000),
    ('62-1', N'Bến xe buýt Chợ Lớn - Bến xe Tân', 47.5, '05:10:00', '18:30:00', 17, 35000),
    ('62-2', N'Bến xe buýt Chợ Lớn - Ngã ba Tân Lân', 34, '03:50:00', '18:55:00', 15, 22000),
    ('62-5', N'Bến xe An Sương - Bến xe Hậu Nghĩa', 31.76, '04:00:00', '19:30:00', 12, 15000),
    ('62-6', N'Bến xe buýt Chợ Lớn - Bến xe Hậu Nghĩa', 36, '05:20:00', '14:25:00', 167, 22000),
    ('62-7', N'Bến xe buýt Chợ Lớn - Bến xe Đức Huệ', 52, '04:50:00', '17:00:00', 20, 22000),
    ('62-8', N'Bến xe buýt Chợ Lớn - Bến xe Tân An', 42, '04:30:00', '19:30:00', 15, 18000),
    ('62-9', N'Bến xe buýt Quận 8 - Thị xã Gò Công', 38.2, '03:15:00', '19:00:00', 15, 40000),
    ('62-10', N'Bến xe buýt Chợ Lớn - Thanh Vĩnh Đông', 69, '03:53:00', '18:23:00', 30, 42000),
    ('62-11', N'Bến xe buýt Quận 8 - Tân Tập', 36.4, '05:15:00', '16:30:00', 150, 32000),
    ('63-1', N'Bến xe buýt Tân Phú - Bến xe Tiền Giang', 85, '04:45:00', '18:15:00', 20, 70000),
    ('70-1', N'Bến xe Củ Chi - Bến xe Tây Ninh', 66, '03:00:00', '19:00:00', 20, 25000),
    ('70-2', N'Bến xe Củ Chi - Tòa Thánh - Khu du lịch Núi Bà Đen', 73, '02:55:00', '19:00:00', 20, 40000),
    ('70-5', N'Bố Heo - Lộc Hưng', 6, '04:45:00', '19:30:00', 20, 10000),
    ('72-1', N'Sân bay Tân Sơn Nhất – Đường cao tốc – Bến xe Vũng Tàu', 104.95, '04:30:00', '22:30:00', 10, 200000),
    ('DL03', N'Vận chuyển hành khách bằng xe 2 tầng, thoáng nóc Sài Gòn – Chợ Lớn', 20.5, '08:00:00', '22:30:00', 30, 500000),
    ('DL04', N'City tour Sài Gòn - Chợ Lớn', 2.22, '09:00:00', '16:00:00', 60, 10000);
INSERT INTO dbo.TuyenXe (maTuyen, tenTuyen, gioBatDau, gioKetThuc, gianCachPhut, trangThai, chinhSachMienPhiId, cuLyKm)
SELECT s.maTuyen, s.tenTuyen, s.gioBatDau, s.gioKetThuc, s.gianCachPhut, 1, NULL, s.cuLyKm
FROM @TinhPhi s WHERE NOT EXISTS (SELECT 1 FROM dbo.TuyenXe t WHERE t.maTuyen = s.maTuyen);
INSERT INTO dbo.GiaVe (maLoaiVe, nhomDoiTuongId, tuyenXeId, giaGoc, soTienMienGiam, hieuLucTu, hieuLucDen, canCuChinhSach, trangThai)
SELECT 'VeLuot', n.id, t.id, s.giaVe, 0, '2026-07-01', NULL, N'Giá vé lượt không trợ giá, mức cao nhất toàn tuyến (nguồn vietnammoi.vn 05/2025)', 1
FROM @TinhPhi s JOIN dbo.TuyenXe t ON t.maTuyen = s.maTuyen CROSS JOIN dbo.NhomDoiTuong n
WHERE n.maNhom = 'PhoThong' AND NOT EXISTS (SELECT 1 FROM dbo.GiaVe g WHERE g.tuyenXeId = t.id AND g.maLoaiVe = 'VeLuot' AND g.nhomDoiTuongId = n.id);

UPDATE dbo.GiaVe SET trangThai = 0 WHERE canCuChinhSach = N'Dữ liệu demo kiểm thử QR trực tuyến - tuyến 01';
COMMIT;
GO
