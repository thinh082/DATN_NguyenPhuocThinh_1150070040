using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Entities;
using Server.Models;
using Server.Services;

namespace Server.Tests;

public class PaymentIntegrationTests
{
    [Fact]
    public async Task SuccessfulMockPaymentIssuesVerifiableTicket()
    {
        var project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(project).AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        var userId = await db.NguoiDungs.Select(x => x.id).FirstAsync();
        var fare = await db.GiaVes.FirstAsync();
        var order = new DonHang { maDonHang = Guid.NewGuid(), maYeuCau = Guid.NewGuid(), nguoiDungId = userId,
            tongGiaGoc = 7000, tongMienGiam = 0, trangThai = "ChoThanhToan", ngayTao = DateTime.UtcNow, hetHanLuc = DateTime.UtcNow.AddMinutes(10) };
        db.DonHangs.Add(order);
        await db.SaveChangesAsync();
        db.ChiTietDonHangs.Add(new ChiTietDonHang { donHangId = order.id, giaVeId = fare.id, soLuong = 1, donGiaGoc = 7000, mienGiamMoiVe = 0 });
        await db.SaveChangesAsync();
        try
        {
            var crypto = new MatMaService(config);
            var service = new VeService(db, crypto, new SigningKeyService(config));
            await service.thanhToanDonHang(new ThanhToanDonHangDto { donHangId = order.id });
            db.ChangeTracker.Clear();
            var ticket = await db.Ves.Include(x => x.khoaKySo).SingleAsync(x => x.chiTietDonHang.donHangId == order.id);
            Assert.True(crypto.XacThucChuKy(ticket.duLieuDaKy, ticket.chuKy, ticket.khoaKySo.khoaCongKhaiPem));
            Assert.Equal(32, crypto.GiaiMaAes(ticket.biMatTotpMaHoa).Length);
        }
        finally
        {
            await db.Ves.Where(x => x.chiTietDonHang.donHangId == order.id).ExecuteDeleteAsync();
            await db.ThanhToans.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
            await db.ChiTietDonHangs.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
            await db.DonHangs.Where(x => x.id == order.id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task FailedIssuanceMustNotMarkOrderAsPaid()
    {
        var project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(project).AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        var userId = await db.NguoiDungs.Select(x => x.id).FirstAsync();
        var fare = await db.GiaVes.FirstAsync();
        var order = new DonHang { maDonHang = Guid.NewGuid(), maYeuCau = Guid.NewGuid(), nguoiDungId = userId,
            tongGiaGoc = 7000, tongMienGiam = 0, trangThai = "ChoThanhToan", ngayTao = DateTime.UtcNow,
            hetHanLuc = DateTime.UtcNow.AddMinutes(10) };
        db.DonHangs.Add(order);
        await db.SaveChangesAsync();
        db.ChiTietDonHangs.Add(new ChiTietDonHang { donHangId = order.id, giaVeId = fare.id, soLuong = 1, donGiaGoc = 7000, mienGiamMoiVe = 0 });
        await db.SaveChangesAsync();
        try
        {
            var noKey = new ConfigurationBuilder().Build();
            var service = new VeService(db, new MatMaService(), new SigningKeyService(noKey));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.thanhToanDonHang(new ThanhToanDonHangDto { donHangId = order.id }));
            db.ChangeTracker.Clear();
            Assert.Equal("ChoThanhToan", (await db.DonHangs.FindAsync(order.id))!.trangThai);
            Assert.False(await db.ThanhToans.AnyAsync(x => x.donHangId == order.id));
        }
        finally
        {
            await db.ThanhToans.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
            await db.ChiTietDonHangs.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
            await db.DonHangs.Where(x => x.id == order.id).ExecuteDeleteAsync();
        }
    }
}
