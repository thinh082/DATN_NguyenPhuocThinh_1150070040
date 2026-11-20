IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TepMinhChung')
BEGIN
    CREATE TABLE TepMinhChung (
        id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        maTep uniqueidentifier NOT NULL DEFAULT NEWID(),
        nguoiDungId int NOT NULL,
        tenFileGoc nvarchar(255) NOT NULL,
        tenFileLuu nvarchar(255) NOT NULL,
        loaiFile varchar(100) NOT NULL,
        kichThuoc bigint NOT NULL,
        daGanHoSo bit NOT NULL DEFAULT 0,
        ngayTao datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TepMinhChung_NguoiDung FOREIGN KEY (nguoiDungId) REFERENCES NguoiDung(id) ON DELETE CASCADE,
        CONSTRAINT UQ_TepMinhChung_maTep UNIQUE (maTep)
    );
END
GO
