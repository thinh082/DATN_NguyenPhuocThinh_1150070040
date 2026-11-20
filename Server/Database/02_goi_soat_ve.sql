IF OBJECT_ID(N'dbo.GoiSoatVe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoiSoatVe (
        id uniqueidentifier NOT NULL PRIMARY KEY,
        phienSoatVeId uniqueidentifier NOT NULL REFERENCES dbo.PhienSoatVe(id),
        taoLuc datetime2(3) NOT NULL,
        hetHanLuc datetime2(3) NOT NULL,
        danhSachVeHopLe nvarchar(max) NOT NULL,
        CONSTRAINT CK_GoiSoatVe_thoiHan CHECK (hetHanLuc > taoLuc),
        CONSTRAINT CK_GoiSoatVe_json CHECK (ISJSON(danhSachVeHopLe) = 1)
    );
END
IF OBJECT_ID(N'dbo.XuLySuKienOffline', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.XuLySuKienOffline (
        id uniqueidentifier NOT NULL PRIMARY KEY,
        goiId uniqueidentifier NOT NULL REFERENCES dbo.GoiSoatVe(id),
        bamDuLieu binary(32) NOT NULL,
        ketQua varchar(30) NOT NULL,
        maLyDo varchar(50) NOT NULL
    );
END
