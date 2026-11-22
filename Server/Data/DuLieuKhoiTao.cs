using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Server.Entities;

namespace Server.Data;

public static class DuLieuKhoiTao
{
    public static async Task KhoiTaoDuLieuMau(SmartBusDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (context.Database.IsSqlServer())
        {
            await context.Database.ExecuteSqlRawAsync(@"
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
            ");
        }

        // 1. Khoi tao Nguoi dung mau (idempotent theo email)
        var matKhauMacDinh = BamMatKhau("123456");

        if (!await context.NguoiDungs.AnyAsync(x => x.email == "admin@smartbus.vn"))
        {
            context.NguoiDungs.Add(new NguoiDung
            {
                hoTen = "Quản trị viên hệ thống",
                email = "admin@smartbus.vn",
                soDienThoai = "0900000001",
                cccd = "079000000001",
                matKhauBam = matKhauMacDinh,
                vaiTro = "QuanTriVien",
                trangThai = true,
                ngayTao = DateTime.UtcNow,
                ngayCapNhat = DateTime.UtcNow
            });
        }

        if (!await context.NguoiDungs.AnyAsync(x => x.email == "soatve01@smartbus.vn"))
        {
            context.NguoiDungs.Add(new NguoiDung
            {
                hoTen = "Nhân viên soát vé 01",
                email = "soatve01@smartbus.vn",
                soDienThoai = "0900000002",
                cccd = "079000000002",
                matKhauBam = matKhauMacDinh,
                vaiTro = "NhanVienSoatVe",
                trangThai = true,
                ngayTao = DateTime.UtcNow,
                ngayCapNhat = DateTime.UtcNow
            });
        }

        if (!await context.NguoiDungs.AnyAsync(x => x.email == "thinh@smartbus.vn"))
        {
            context.NguoiDungs.Add(new NguoiDung
            {
                hoTen = "Nguyễn Phước Thịnh",
                email = "thinh@smartbus.vn",
                soDienThoai = "0900000003",
                cccd = "079000000003",
                matKhauBam = matKhauMacDinh,
                vaiTro = "HanhKhach",
                trangThai = true,
                ngayTao = DateTime.UtcNow,
                ngayCapNhat = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        // 2. Khoi tao Tuyen xe mau (idempotent theo maTuyen)
        if (!await context.TuyenXes.AnyAsync(x => x.maTuyen == "01"))
        {
            context.TuyenXes.Add(new TuyenXe
            {
                maTuyen = "01",
                tenTuyen = "Bến Thành - Bến xe Chợ Lớn",
                gioBatDau = new TimeOnly(5, 0),
                gioKetThuc = new TimeOnly(21, 0),
                gianCachPhut = 10,
                trangThai = true
            });
        }

        if (!await context.TuyenXes.AnyAsync(x => x.maTuyen == "150"))
        {
            context.TuyenXes.Add(new TuyenXe
            {
                maTuyen = "150",
                tenTuyen = "Bến xe Chợ Lớn - Tân Vạn",
                gioBatDau = new TimeOnly(4, 30),
                gioKetThuc = new TimeOnly(20, 30),
                gianCachPhut = 8,
                trangThai = true
            });
        }
        await context.SaveChangesAsync();

        // 3. Khoi tao Tram dung mau (idempotent theo maTram)
        var tramMau = new[]
        {
            new TramDung { maTram = "BT01", tenTram = "Trạm trung chuyển Bến Thành", diaChi = "Đường Hàm Nghi, Quận 1, TP.HCM", viDo = 10.771970m, kinhDo = 106.698330m, trangThai = true },
            new TramDung { maTram = "DH01", tenTram = "Trường ĐH Tài nguyên và Môi trường", diaChi = "236 Lê Văn Sỹ, Quận Tân Bình, TP.HCM", viDo = 10.793750m, kinhDo = 106.666920m, trangThai = true },
            new TramDung { maTram = "CL01", tenTram = "Bến xe Chợ Lớn", diaChi = "Lê Quang Sung, Quận 5, TP.HCM", viDo = 10.751650m, kinhDo = 106.654270m, trangThai = true },
            new TramDung { maTram = "TV01", tenTram = "Bến xe Tân Vạn", diaChi = "Xã Bình Thắng, Dĩ An, Bình Dương", viDo = 10.875410m, kinhDo = 106.812340m, trangThai = true }
        };

        foreach (var tram in tramMau)
        {
            if (!await context.TramDungs.AnyAsync(x => x.maTram == tram.maTram))
            {
                context.TramDungs.Add(tram);
            }
        }
        await context.SaveChangesAsync();

        // 4. Khoi tao Lo trinh va gan Tram dung 2 chieu cho ca 2 tuyen
        var tuyen01 = await context.TuyenXes.FirstOrDefaultAsync(x => x.maTuyen == "01");
        var tuyen150 = await context.TuyenXes.FirstOrDefaultAsync(x => x.maTuyen == "150");

        var tramBT = await context.TramDungs.FirstOrDefaultAsync(x => x.maTram == "BT01");
        var tramDH = await context.TramDungs.FirstOrDefaultAsync(x => x.maTram == "DH01");
        var tramCL = await context.TramDungs.FirstOrDefaultAsync(x => x.maTram == "CL01");
        var tramTV = await context.TramDungs.FirstOrDefaultAsync(x => x.maTram == "TV01");

        // Tuyến 01 - Chiều Đi
        if (tuyen01 != null && tramBT != null && tramDH != null && tramCL != null)
        {
            if (!await context.LoTrinhs.AnyAsync(x => x.tuyenXeId == tuyen01.id && x.chieu == "Di"))
            {
                var ltDi = new LoTrinh
                {
                    tuyenXeId = tuyen01.id,
                    chieu = "Di",
                    phienBan = 1,
                    tenLoTrinh = "Bến Thành đi Chợ Lớn",
                    hieuLucTu = DateTime.UtcNow.AddDays(-30)
                };
                context.LoTrinhs.Add(ltDi);
                await context.SaveChangesAsync();

                context.LoTrinhTramDungs.AddRange(
                    new LoTrinhTramDung { loTrinhId = ltDi.id, tramDungId = tramBT.id, thuTu = 1, khoangCachTuDauKm = 0, phutTuDau = 0 },
                    new LoTrinhTramDung { loTrinhId = ltDi.id, tramDungId = tramDH.id, thuTu = 2, khoangCachTuDauKm = 4.5m, phutTuDau = 15 },
                    new LoTrinhTramDung { loTrinhId = ltDi.id, tramDungId = tramCL.id, thuTu = 3, khoangCachTuDauKm = 9.2m, phutTuDau = 35 }
                );
                await context.SaveChangesAsync();
            }

            // Tuyến 01 - Chiều Về
            if (!await context.LoTrinhs.AnyAsync(x => x.tuyenXeId == tuyen01.id && x.chieu == "Ve"))
            {
                var ltVe = new LoTrinh
                {
                    tuyenXeId = tuyen01.id,
                    chieu = "Ve",
                    phienBan = 1,
                    tenLoTrinh = "Chợ Lớn đi Bến Thành",
                    hieuLucTu = DateTime.UtcNow.AddDays(-30)
                };
                context.LoTrinhs.Add(ltVe);
                await context.SaveChangesAsync();

                context.LoTrinhTramDungs.AddRange(
                    new LoTrinhTramDung { loTrinhId = ltVe.id, tramDungId = tramCL.id, thuTu = 1, khoangCachTuDauKm = 0, phutTuDau = 0 },
                    new LoTrinhTramDung { loTrinhId = ltVe.id, tramDungId = tramDH.id, thuTu = 2, khoangCachTuDauKm = 4.7m, phutTuDau = 16 },
                    new LoTrinhTramDung { loTrinhId = ltVe.id, tramDungId = tramBT.id, thuTu = 3, khoangCachTuDauKm = 9.2m, phutTuDau = 35 }
                );
                await context.SaveChangesAsync();
            }
        }

        // Tuyến 150 - Chiều Đi & Về
        if (tuyen150 != null && tramCL != null && tramBT != null && tramTV != null)
        {
            if (!await context.LoTrinhs.AnyAsync(x => x.tuyenXeId == tuyen150.id && x.chieu == "Di"))
            {
                var lt150Di = new LoTrinh
                {
                    tuyenXeId = tuyen150.id,
                    chieu = "Di",
                    phienBan = 1,
                    tenLoTrinh = "Chợ Lớn đi Tân Vạn",
                    hieuLucTu = DateTime.UtcNow.AddDays(-30)
                };
                context.LoTrinhs.Add(lt150Di);
                await context.SaveChangesAsync();

                context.LoTrinhTramDungs.AddRange(
                    new LoTrinhTramDung { loTrinhId = lt150Di.id, tramDungId = tramCL.id, thuTu = 1, khoangCachTuDauKm = 0, phutTuDau = 0 },
                    new LoTrinhTramDung { loTrinhId = lt150Di.id, tramDungId = tramBT.id, thuTu = 2, khoangCachTuDauKm = 9.2m, phutTuDau = 30 },
                    new LoTrinhTramDung { loTrinhId = lt150Di.id, tramDungId = tramTV.id, thuTu = 3, khoangCachTuDauKm = 28.5m, phutTuDau = 80 }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.LoTrinhs.AnyAsync(x => x.tuyenXeId == tuyen150.id && x.chieu == "Ve"))
            {
                var lt150Ve = new LoTrinh
                {
                    tuyenXeId = tuyen150.id,
                    chieu = "Ve",
                    phienBan = 1,
                    tenLoTrinh = "Tân Vạn đi Chợ Lớn",
                    hieuLucTu = DateTime.UtcNow.AddDays(-30)
                };
                context.LoTrinhs.Add(lt150Ve);
                await context.SaveChangesAsync();

                context.LoTrinhTramDungs.AddRange(
                    new LoTrinhTramDung { loTrinhId = lt150Ve.id, tramDungId = tramTV.id, thuTu = 1, khoangCachTuDauKm = 0, phutTuDau = 0 },
                    new LoTrinhTramDung { loTrinhId = lt150Ve.id, tramDungId = tramBT.id, thuTu = 2, khoangCachTuDauKm = 19.3m, phutTuDau = 50 },
                    new LoTrinhTramDung { loTrinhId = lt150Ve.id, tramDungId = tramCL.id, thuTu = 3, khoangCachTuDauKm = 28.5m, phutTuDau = 80 }
                );
                await context.SaveChangesAsync();
            }
        }

        // 5. Khoi tao Xe buyt mau
        var qrXe1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var qrXe2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        if (!await context.XeBuyts.AnyAsync(x => x.bienSo == "51B-123.45"))
        {
            context.XeBuyts.Add(new XeBuyt
            {
                bienSo = "51B-123.45",
                maQrTinh = qrXe1,
                sucChua = 40,
                trangThai = true
            });
        }

        if (!await context.XeBuyts.AnyAsync(x => x.bienSo == "51B-678.90"))
        {
            context.XeBuyts.Add(new XeBuyt
            {
                bienSo = "51B-678.90",
                maQrTinh = qrXe2,
                sucChua = 55,
                trangThai = true
            });
        }
        await context.SaveChangesAsync();

        // 6. Khoi tao Thiet bi soat ve mau
        var maThietBi1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        if (!await context.ThietBiSoatVes.AnyAsync(x => x.maThietBi == maThietBi1))
        {
            context.ThietBiSoatVes.Add(new ThietBiSoatVe
            {
                maThietBi = maThietBi1,
                tenThietBi = "Máy soát vé xe 51B-123.45",
                mocThuHoiDaNhan = 0,
                trangThai = true,
                ngayDangKy = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // 7. Khoi tao Chuyen xe mau (1 DangChay cho Xe 1, 1 DuKien cho Xe 2)
        var xe1 = await context.XeBuyts.FirstOrDefaultAsync(x => x.bienSo == "51B-123.45");
        var xe2 = await context.XeBuyts.FirstOrDefaultAsync(x => x.bienSo == "51B-678.90");
        var ltTuyen01Di = await context.LoTrinhs.FirstOrDefaultAsync(x => x.tuyenXe.maTuyen == "01" && x.chieu == "Di");
        var ltTuyen150Di = await context.LoTrinhs.FirstOrDefaultAsync(x => x.tuyenXe.maTuyen == "150" && x.chieu == "Di");

        if (xe1 != null && ltTuyen01Di != null && !await context.ChuyenXes.AnyAsync(x => x.xeBuytId == xe1.id && x.trangThai == "DangChay"))
        {
            context.ChuyenXes.Add(new ChuyenXe
            {
                xeBuytId = xe1.id,
                loTrinhId = ltTuyen01Di.id,
                gioKhoiHanhDuKien = DateTime.UtcNow.AddMinutes(-30),
                gioKetThucDuKien = DateTime.UtcNow.AddMinutes(45),
                gioKhoiHanhThucTe = DateTime.UtcNow.AddMinutes(-30),
                trangThai = "DangChay"
            });
            await context.SaveChangesAsync();
        }

        if (xe2 != null && ltTuyen150Di != null && !await context.ChuyenXes.AnyAsync(x => x.xeBuytId == xe2.id))
        {
            context.ChuyenXes.Add(new ChuyenXe
            {
                xeBuytId = xe2.id,
                loTrinhId = ltTuyen150Di.id,
                gioKhoiHanhDuKien = DateTime.UtcNow.AddHours(1),
                gioKetThucDuKien = DateTime.UtcNow.AddHours(2.5),
                trangThai = "DuKien"
            });
            await context.SaveChangesAsync();
        }

        // 8. Khoi tao Bieu gia ve mau
        if (!await context.GiaVes.AnyAsync())
        {
            int? tuyen01Id = tuyen01?.id;
            int? tuyen150Id = tuyen150?.id;

            context.GiaVes.AddRange(
                // Toàn mạng
                new GiaVe { maLoaiVe = "VeLuot", nhomDoiTuongId = 1, tuyenXeId = null, giaGoc = 7000, soTienMienGiam = 0, hieuLucTu = DateTime.UtcNow.AddDays(-30), canCuChinhSach = "Vé lượt phổ thông toàn mạng", trangThai = true },
                new GiaVe { maLoaiVe = "VeLuot", nhomDoiTuongId = 2, tuyenXeId = null, giaGoc = 7000, soTienMienGiam = 4000, hieuLucTu = DateTime.UtcNow.AddDays(-30), canCuChinhSach = "Trợ giá 4.000đ cho học sinh, sinh viên", trangThai = true },
                new GiaVe { maLoaiVe = "VeLuot", nhomDoiTuongId = 3, tuyenXeId = null, giaGoc = 7000, soTienMienGiam = 7000, hieuLucTu = DateTime.UtcNow.AddDays(-30), canCuChinhSach = "Miễn 100% cho người cao tuổi từ 60 tuổi trở lên", trangThai = true },
                new GiaVe { maLoaiVe = "VeThang", nhomDoiTuongId = 1, tuyenXeId = null, giaGoc = 300000, soTienMienGiam = 0, hieuLucTu = DateTime.UtcNow.AddDays(-30), canCuChinhSach = "Vé tháng toàn mạng lưới", trangThai = true },
                // Tuyến 01
                new GiaVe { maLoaiVe = "VeThang", nhomDoiTuongId = 1, tuyenXeId = tuyen01Id, giaGoc = 200000, soTienMienGiam = 0, hieuLucTu = DateTime.UtcNow.AddDays(-30), canCuChinhSach = "Vé tháng 1 tuyến (Tuyến 01)", trangThai = true },
                // Tuyến 150
                new GiaVe { maLoaiVe = "VeThang", nhomDoiTuongId = 1, tuyenXeId = tuyen150Id, giaGoc = 200000, soTienMienGiam = 0, hieuLucTu = DateTime.UtcNow.AddDays(-30), canCuChinhSach = "Vé tháng 1 tuyến (Tuyến 150)", trangThai = true }
            );
            await context.SaveChangesAsync();
        }

        // 9. Khoi tao Khoa ky so ECDSA P-256
        if (!await context.KhoaKySos.AnyAsync())
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKeyPem = ecdsa.ExportSubjectPublicKeyInfoPem();

            var khoaKySo = new KhoaKySo
            {
                maKhoa = "ECDSA-SMARTBUS-2026-KEY01",
                thuatToan = "ECDSA_P256_SHA256",
                khoaCongKhaiPem = publicKeyPem,
                thamChieuKhoaBiMat = "LOCAL_ECDSA_SERVER_PRIVATE_KEY_2026",
                hieuLucTu = DateTime.UtcNow.AddDays(-1),
                hetHanLuc = DateTime.UtcNow.AddYears(5),
                trangThai = "DangDung"
            };

            await context.KhoaKySos.AddAsync(khoaKySo);
            await context.SaveChangesAsync();
        }
    }

    private static string BamMatKhau(string matKhau)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(matKhau + "SmartBusSalt2026");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
