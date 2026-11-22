/*
  SmartBus - Tao du lieu LoTrinh, XeBuyt va ChuyenXe (DangChay) cho du 134 tuyen TP.HCM (NQ 09/2026/NQ-HDND)
  Script idempotent: giu nguyen cac tuyen/xe/chuyen dang chay da ton tai (nhu tuyen 01, xe 51B-123.45),
  chi bo sung cho cac tuyen chua co chuyen DangChay de dropdown chon tuyen/chuyen tren app nhan vien hien du 134 tuyen.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- 1. Bo sung LoTrinh chieu 'Di' cho cac tuyen chua co lo trinh
INSERT INTO dbo.LoTrinh (tuyenXeId, chieu, phienBan, tenLoTrinh, hieuLucTu, hieuLucDen)
SELECT
    t.id,
    'Di',
    1,
    N'Lượt đi - ' + LEFT(t.tenTuyen, 185),
    '2026-07-01T00:00:00',
    NULL
FROM dbo.TuyenXe t
WHERE t.trangThai = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.LoTrinh lt WHERE lt.tuyenXeId = t.id AND lt.chieu = 'Di'
  );

-- 2. Bo sung 1 XeBuyt va 1 ChuyenXe DangChay cho moi tuyen chua co chuyen DangChay
DECLARE @TuyenCanTao TABLE (
    tuyenXeId int NOT NULL PRIMARY KEY,
    loTrinhId int NOT NULL,
    bienSo varchar(20) NOT NULL
);

INSERT INTO @TuyenCanTao (tuyenXeId, loTrinhId, bienSo)
SELECT
    t.id,
    (SELECT TOP 1 lt.id FROM dbo.LoTrinh lt WHERE lt.tuyenXeId = t.id ORDER BY lt.phienBan DESC, lt.id ASC),
    '51B-' + RIGHT('000' + CAST(200 + t.id AS varchar(10)), 3) + '.' + RIGHT('00' + CAST((t.id * 13 + 7) % 100 AS varchar(10)), 2)
FROM dbo.TuyenXe t
WHERE t.trangThai = 1
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.ChuyenXe cx
      INNER JOIN dbo.LoTrinh lt ON cx.loTrinhId = lt.id
      WHERE lt.tuyenXeId = t.id AND cx.trangThai = 'DangChay'
  );

INSERT INTO dbo.XeBuyt (bienSo, maQrTinh, sucChua, trangThai)
SELECT c.bienSo, NEWID(), 45, 1
FROM @TuyenCanTao c
WHERE NOT EXISTS (SELECT 1 FROM dbo.XeBuyt x WHERE x.bienSo = c.bienSo);

DECLARE @now datetime2(3) = SYSUTCDATETIME();

INSERT INTO dbo.ChuyenXe (loTrinhId, xeBuytId, gioKhoiHanhDuKien, gioKetThucDuKien, gioKhoiHanhThucTe, gioKetThucThucTe, trangThai)
SELECT
    c.loTrinhId,
    x.id,
    DATEADD(MINUTE, -30, @now),
    DATEADD(HOUR, 12, @now),
    DATEADD(MINUTE, -25, @now),
    NULL,
    'DangChay'
FROM @TuyenCanTao c
INNER JOIN dbo.XeBuyt x ON x.bienSo = c.bienSo;

COMMIT TRANSACTION;

DECLARE @tongChuyen int = (SELECT COUNT(*) FROM dbo.ChuyenXe WHERE trangThai = 'DangChay');
DECLARE @tongTuyenCoChuyen int = (
    SELECT COUNT(DISTINCT lt.tuyenXeId)
    FROM dbo.ChuyenXe cx
    INNER JOIN dbo.LoTrinh lt ON cx.loTrinhId = lt.id
    WHERE cx.trangThai = 'DangChay'
);
PRINT N'Da bo sung chuyen xe DangChay. Tong chuyen DangChay: ' + CAST(@tongChuyen AS nvarchar(10)) + N' | So tuyen co chuyen DangChay: ' + CAST(@tongTuyenCoChuyen AS nvarchar(10));
