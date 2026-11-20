using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Tests;

public class ProofUploadAndPriorityTests
{
    private static IConfiguration GetConfig()
    {
        var server = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        return new ConfigurationBuilder()
            .SetBasePath(server)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true)
            .Build();
    }

    private static IFormFile CreateMockFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "tep", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task Upload_ValidJpegAndPdf_SucceedsAndEnforcesOwnership()
    {
        var config = GetConfig();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);

        // Đảm bảo bảng TepMinhChung tồn tại
        await DuLieuKhoiTao.KhoiTaoDuLieuMau(db);

        var service = new XacThucService(db, config);

        var emailA = $"userA-{Guid.NewGuid():N}@example.invalid";
        var emailB = $"userB-{Guid.NewGuid():N}@example.invalid";

        int userAId = 0;
        int userBId = 0;
        var createdFilePaths = new List<string>();

        try
        {
            await service.dangKy(new DangKyDto { hoTen = "User A", email = emailA, matKhau = "Password123" });
            await service.dangKy(new DangKyDto { hoTen = "User B", email = emailB, matKhau = "Password123" });

            var userA = await db.NguoiDungs.SingleAsync(x => x.email == emailA);
            var userB = await db.NguoiDungs.SingleAsync(x => x.email == emailB);
            userAId = userA.id;
            userBId = userB.id;

            // 1. Tạo file JPEG hợp lệ (Magic bytes: FF D8 FF E0)
            byte[] jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };
            var jpegFormFile = CreateMockFile("the_sinh_vien.jpg", "image/jpeg", jpegBytes);

            var uploadResult = await service.taiLenMinhChung(userAId, jpegFormFile);
            Guid maTepA = uploadResult.maTep;
            Assert.Equal("image/jpeg", (string)uploadResult.loaiFile);
            Assert.Contains(maTepA.ToString(), (string)uploadResult.duongDan);

            // Kiểm tra truy cập tệp bởi chính User A
            var (pathA, mimeA, nameA) = await service.layTepMinhChung(maTepA, userAId, "HanhKhach");
            Assert.True(File.Exists(pathA));
            createdFilePaths.Add(pathA);
            Assert.Equal("image/jpeg", mimeA);
            Assert.Equal("the_sinh_vien.jpg", nameA);

            // Kiểm tra User B không có quyền truy cập tệp của User A
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await service.layTepMinhChung(maTepA, userBId, "HanhKhach"));

            // Quản trị viên được phép truy cập tệp của User A
            var (pathAdmin, mimeAdmin, _) = await service.layTepMinhChung(maTepA, 999999, "QuanTriVien");
            Assert.Equal(pathA, pathAdmin);
            Assert.Equal("image/jpeg", mimeAdmin);

            // 2. Kiểm tra nộp hồ sơ ưu tiên với tệp của chính mình vs tệp của người khác
            var priority = await db.NhomDoiTuongs.FirstAsync(x => x.canXacMinh && x.trangThai);

            // User B cố tình dùng maTep của User A -> Phải bị từ chối
            var requestFake = new HoSoUuTienDto
            {
                nguoiDungId = userBId,
                nhomDoiTuongId = priority.id,
                duongDanMinhChung = $"/api/XacThuc/minh-chung/{maTepA}",
                hieuLucTu = DateTime.UtcNow.Date,
                hieuLucDen = DateTime.UtcNow.Date.AddYears(1)
            };
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await service.nopHoSoUuTien(requestFake));

            // User A dùng chính tệp của mình -> Nộp thành công
            var requestValid = new HoSoUuTienDto
            {
                nguoiDungId = userAId,
                nhomDoiTuongId = priority.id,
                duongDanMinhChung = $"/api/XacThuc/minh-chung/{maTepA}",
                hieuLucTu = DateTime.UtcNow.Date,
                hieuLucDen = DateTime.UtcNow.Date.AddYears(1)
            };
            var nopResult = await service.nopHoSoUuTien(requestValid);
            Assert.NotNull(nopResult);

            var tepInDb = await db.TepMinhChungs.SingleAsync(x => x.maTep == maTepA);
            Assert.True(tepInDb.daGanHoSo);

            // 3. Kiểm tra nộp hồ sơ với URL web cũ -> Vẫn thành công
            var requestUrlOld = new HoSoUuTienDto
            {
                nguoiDungId = userBId,
                nhomDoiTuongId = priority.id,
                duongDanMinhChung = "https://example.com/minh-chung.png",
                hieuLucTu = DateTime.UtcNow.Date,
                hieuLucDen = DateTime.UtcNow.Date.AddYears(1)
            };
            var nopOldResult = await service.nopHoSoUuTien(requestUrlOld);
            Assert.NotNull(nopOldResult);
        }
        finally
        {
            if (userAId != 0)
            {
                await db.HoSoUuTiens.Where(x => x.nguoiDungId == userAId).ExecuteDeleteAsync();
                await db.TepMinhChungs.Where(x => x.nguoiDungId == userAId).ExecuteDeleteAsync();
                await db.NguoiDungs.Where(x => x.id == userAId).ExecuteDeleteAsync();
            }
            if (userBId != 0)
            {
                await db.HoSoUuTiens.Where(x => x.nguoiDungId == userBId).ExecuteDeleteAsync();
                await db.TepMinhChungs.Where(x => x.nguoiDungId == userBId).ExecuteDeleteAsync();
                await db.NguoiDungs.Where(x => x.id == userBId).ExecuteDeleteAsync();
            }

            foreach (var p in createdFilePaths)
            {
                try { if (File.Exists(p)) File.Delete(p); } catch { }
            }
        }
    }

    [Fact]
    public async Task Upload_FakeMagicBytesOrOversized_ThrowsArgumentException()
    {
        var config = GetConfig();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);

        var service = new XacThucService(db, config);

        var email = $"fake-{Guid.NewGuid():N}@example.invalid";
        int userId = 0;

        try
        {
            await service.dangKy(new DangKyDto { hoTen = "Fake Tester", email = email, matKhau = "Password123" });
            var user = await db.NguoiDungs.SingleAsync(x => x.email == email);
            userId = user.id;

            // 1. Tệp .jpg nhưng nội dung là chuỗi text HTML giả mạo
            byte[] fakeBytes = Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>");
            var fakeFile = CreateMockFile("virus.jpg", "image/jpeg", fakeBytes);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.taiLenMinhChung(userId, fakeFile));

            // 2. Tệp đuôi không được hỗ trợ .exe
            byte[] exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 };
            var exeFile = CreateMockFile("payload.exe", "application/octet-stream", exeBytes);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.taiLenMinhChung(userId, exeFile));

            // 3. Tệp quá 5MB
            byte[] oversized = new byte[5 * 1024 * 1024 + 10];
            oversized[0] = 0xFF; oversized[1] = 0xD8; oversized[2] = 0xFF; oversized[3] = 0xE0;
            var largeFile = CreateMockFile("huge.jpg", "image/jpeg", oversized);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.taiLenMinhChung(userId, largeFile));
        }
        finally
        {
            if (userId != 0)
            {
                await db.TepMinhChungs.Where(x => x.nguoiDungId == userId).ExecuteDeleteAsync();
                await db.NguoiDungs.Where(x => x.id == userId).ExecuteDeleteAsync();
            }
        }
    }
}
