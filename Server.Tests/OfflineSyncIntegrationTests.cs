using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Entities;
using Server.Models;
using Server.Services;

namespace Server.Tests;

public class OfflineSyncIntegrationTests
{
    [Fact]
    public async Task RecheckQrAndRetryWithoutDuplicatingRides()
    {
        var server = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(server).AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true).AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        var userId = await db.NguoiDungs.Select(x => x.id).FirstAsync();
        var trip = await db.ChuyenXes.Include(x => x.loTrinh).FirstAsync();
        var fare = await db.GiaVes.FirstAsync(x => x.maLoaiVe == "VeLuot" && x.tuyenXeId == trip.loTrinh.tuyenXeId);
        var device = new ThietBiSoatVe { maThietBi = Guid.NewGuid(), tenThietBi = "Test sync", trangThai = true, ngayDangKy = DateTime.UtcNow };
        var device2 = new ThietBiSoatVe { maThietBi = Guid.NewGuid(), tenThietBi = "Test sync 2", trangThai = true, ngayDangKy = DateTime.UtcNow };
        db.ThietBiSoatVes.Add(device);
        db.ThietBiSoatVes.Add(device2);
        var order = new DonHang { maDonHang = Guid.NewGuid(), maYeuCau = Guid.NewGuid(), nguoiDungId = userId, tongGiaGoc = 7000, tongMienGiam = 0,
            trangThai = "ChoThanhToan", ngayTao = DateTime.UtcNow, hetHanLuc = DateTime.UtcNow.AddMinutes(30) };
        db.DonHangs.Add(order);
        await db.SaveChangesAsync();
        var session = new PhienSoatVe { id = Guid.NewGuid(), thietBiSoatVeId = device.id, nhanVienId = userId, chuyenXeId = trip.id, batDauLuc = DateTime.UtcNow };
        var session2 = new PhienSoatVe { id = Guid.NewGuid(), thietBiSoatVeId = device2.id, nhanVienId = userId, chuyenXeId = trip.id, batDauLuc = DateTime.UtcNow };
        db.PhienSoatVes.Add(session);
        db.PhienSoatVes.Add(session2);
        db.ChiTietDonHangs.Add(new ChiTietDonHang { donHangId = order.id, giaVeId = fare.id, soLuong = 1, donGiaGoc = 7000, mienGiamMoiVe = 0 });
        await db.SaveChangesAsync();
        Guid packageId = Guid.NewGuid();
        Guid packageId2 = Guid.NewGuid();
        try
        {
            var crypto = new MatMaService(config);
            await new VeService(db, crypto, new SigningKeyService(config)).thanhToanDonHang(new ThanhToanDonHangDto { donHangId = order.id });
            var ticket = await db.Ves.Include(x => x.khoaKySo).SingleAsync(x => x.chiTietDonHang.donHangId == order.id);
            db.GoiSoatVes.Add(new GoiSoatVe { id = packageId, phienSoatVeId = session.id, taoLuc = DateTime.UtcNow.AddMinutes(-1), hetHanLuc = DateTime.UtcNow.AddHours(1), danhSachVeHopLe = JsonSerializer.Serialize(new[] { ticket.maVe }) });
            db.GoiSoatVes.Add(new GoiSoatVe { id = packageId2, phienSoatVeId = session2.id, taoLuc = DateTime.UtcNow.AddMinutes(-1), hetHanLuc = DateTime.UtcNow.AddHours(1), danhSachVeHopLe = JsonSerializer.Serialize(new[] { ticket.maVe }) });
            await db.SaveChangesAsync();
            var now = DateTime.UtcNow;
            var secret = crypto.GiaiMaAes(ticket.biMatTotpMaHoa);
            var otp = crypto.TinhMaTotp(secret, now);
            var qr = $"SMARTBUS:v1:{ticket.maVe}:{otp}:{Convert.ToBase64String(ticket.chuKy)}";
            var sync = new OfflineSyncService(db, new OfflineQrValidationService(crypto));
            var fake = new DongBoSuKienOfflineDto { maSuKien = Guid.NewGuid(), goiId = packageId, qr = qr.Replace(otp, "invalid!"), thoiDiemQuet = now, chapNhanTaiThietBi = true };
            Assert.Equal("TuChoi", (await sync.XuLy(fake, userId)).trangThai);
            var rejectedLocally = new DongBoSuKienOfflineDto { maSuKien = Guid.NewGuid(), goiId = packageId, qr = qr, thoiDiemQuet = now, chapNhanTaiThietBi = false };
            Assert.Equal("TuChoi", (await sync.XuLy(rejectedLocally, userId)).trangThai);
            Assert.Equal(0, await db.LuotDis.CountAsync(x => x.veId == ticket.id));
            ticket.trangThai = "TamKhoa";
            await db.SaveChangesAsync();
            var item = new DongBoSuKienOfflineDto { maSuKien = Guid.NewGuid(), goiId = packageId, qr = qr, thoiDiemQuet = now, chapNhanTaiThietBi = true };
            var second = new DongBoSuKienOfflineDto { maSuKien = Guid.NewGuid(), goiId = packageId2, qr = qr, thoiDiemQuet = now, chapNhanTaiThietBi = true };
            await using var db2 = new SmartBusDbContext(options);
            var sync2 = new OfflineSyncService(db2, new OfflineQrValidationService(crypto));
            var results = await Task.WhenAll(sync.XuLy(item, userId), sync2.XuLy(second, userId));
            Assert.Single(results, x => x.trangThai == "ChapNhan" && x.maLyDo == "STALE_REVOCATION");
            Assert.Single(results, x => x.maLyDo == "QUET_TRUNG");
            Assert.Equal(results[0], await sync.XuLy(item, userId));
            Assert.Equal(1, await db.LuotDis.CountAsync(x => x.veId == ticket.id));
            var historyService = new VeService(db, crypto, new SigningKeyService(config));
            var history = JsonSerializer.SerializeToElement((object)await historyService.layLichSuLuotDi(userId));
            Assert.Contains(history.EnumerateArray(), x => x.GetProperty("maVe").GetGuid() == ticket.maVe && x.GetProperty("maTuyen").GetString() != null);
            var someoneElse = JsonSerializer.SerializeToElement((object)await historyService.layLichSuLuotDi(-1));
            Assert.DoesNotContain(someoneElse.EnumerateArray(), x => x.GetProperty("maVe").GetGuid() == ticket.maVe);
            item.qr += "x";
            await Assert.ThrowsAsync<InvalidOperationException>(() => sync.XuLy(item, userId));
        }
        finally
        {
            await db.CanhBaoGianLans.Where(x => x.nhatKyQuetVe.thietBiSoatVeId == device.id || x.nhatKyQuetVe.thietBiSoatVeId == device2.id).ExecuteDeleteAsync();
            await db.LuotDis.Where(x => x.NhatKyQuetVe.thietBiSoatVeId == device.id || x.NhatKyQuetVe.thietBiSoatVeId == device2.id).ExecuteDeleteAsync();
            await db.NhatKyQuetVes.Where(x => x.thietBiSoatVeId == device.id || x.thietBiSoatVeId == device2.id).ExecuteDeleteAsync();
            await db.XuLySuKienOfflines.Where(x => x.goiId == packageId || x.goiId == packageId2).ExecuteDeleteAsync();
            await db.GoiSoatVes.Where(x => x.id == packageId || x.id == packageId2).ExecuteDeleteAsync();
            await db.PhienSoatVes.Where(x => x.id == session.id || x.id == session2.id).ExecuteDeleteAsync();
            await db.Ves.Where(x => x.chiTietDonHang.donHangId == order.id).ExecuteDeleteAsync();
            await db.ThanhToans.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
            await db.ChiTietDonHangs.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
            await db.DonHangs.Where(x => x.id == order.id).ExecuteDeleteAsync();
            await db.ThietBiSoatVes.Where(x => x.id == device.id || x.id == device2.id).ExecuteDeleteAsync();
        }
    }
}
