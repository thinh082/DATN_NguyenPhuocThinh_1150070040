using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Tests;

public class PassengerAccountTests
{
    [Fact]
    public async Task PassengerCanEditOwnProfileAndTrackPriorityApplications()
    {
        var server = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(server).AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true).AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        var email = $"account-{Guid.NewGuid():N}@example.invalid";
        var service = new XacThucService(db, config);
        int userId = 0;
        try
        {
            await service.dangKy(new DangKyDto { hoTen = "Test user", email = email, matKhau = "test1234" });
            var user = await db.NguoiDungs.SingleAsync(x => x.email == email);
            userId = user.id;
            Assert.NotEqual(Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("test1234SmartBusSalt2026"))), user.matKhauBam);
            Assert.NotNull(await service.dangNhap(new DangNhapDto { taiKhoan = email, matKhau = "test1234" }));
            user.matKhauBam = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("test1234SmartBusSalt2026")));
            await db.SaveChangesAsync();
            Assert.NotNull(await service.dangNhap(new DangNhapDto { taiKhoan = email, matKhau = "test1234" }));
            Assert.NotEqual(Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("test1234SmartBusSalt2026"))), user.matKhauBam);
            await service.capNhatThongTin(userId, new CapNhatCaNhanDto { hoTen = "Tên mới", email = email, soDienThoai = "0901234567" });
            Assert.Equal("Tên mới", (await db.NguoiDungs.FindAsync(userId))!.hoTen);
            var priority = await db.NhomDoiTuongs.FirstAsync(x => x.canXacMinh && x.trangThai);
            var request = new HoSoUuTienDto { nguoiDungId = userId, nhomDoiTuongId = priority.id, duongDanMinhChung = "https://example.com/proof", hieuLucTu = DateTime.UtcNow.Date, hieuLucDen = DateTime.UtcNow.Date.AddYears(1) };
            await service.nopHoSoUuTien(request);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.nopHoSoUuTien(request));
            var profiles = JsonSerializer.SerializeToElement(await service.layHoSoCuaToi(userId));
            Assert.Equal("ChoDuyet", profiles[0].GetProperty("trangThai").GetString());
            var profile = await db.HoSoUuTiens.SingleAsync(x => x.nguoiDungId == userId);
            profile.trangThai = "DaDuyet"; profile.nguoiDuyetId = userId; profile.ngayDuyet = DateTime.UtcNow;
            profile.hieuLucTu = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            await db.SaveChangesAsync();
            var fare = await db.GiaVes.FirstAsync(x => x.nhomDoiTuongId == priority.id && x.trangThai);
            var tickets = new VeService(db, new MatMaService(config), new SigningKeyService(config));
            await Assert.ThrowsAsync<Exception>(async () => await tickets.taoDonHang(new TaoDonHangDto { nguoiDungId = userId, giaVeId = fare.id, soLuong = 1 }));
        }
        finally
        {
            if (userId != 0) { await db.HoSoUuTiens.Where(x => x.nguoiDungId == userId).ExecuteDeleteAsync(); await db.NguoiDungs.Where(x => x.id == userId).ExecuteDeleteAsync(); }
        }
    }
}
