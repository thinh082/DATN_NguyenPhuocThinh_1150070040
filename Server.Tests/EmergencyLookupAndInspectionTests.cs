using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Entities;
using Server.Models;
using Server.Services;
using Xunit;

namespace Server.Tests;

public class EmergencyLookupAndInspectionTests
{
    private static JsonElement ToJson(object obj) => JsonSerializer.SerializeToElement(obj);

    private static (SmartBusDbContext db, IConfiguration config) GetContext()
    {
        var project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(project)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true)
            .Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        return (new SmartBusDbContext(options), config);
    }

    [Fact]
    public async Task TraCuuKhancCap_FindsTicket_DoesNotCreateJourney()
    {
        var (db, config) = GetContext();
        await using (db)
        {
            var session = await db.PhienSoatVes
                .Include(x => x.chuyenXe)
                    .ThenInclude(cx => cx.loTrinh)
                .FirstOrDefaultAsync(x => x.ketThucLuc == null);

            var passenger = await db.NguoiDungs.FirstAsync(x => x.vaiTro == "HanhKhach" && !string.IsNullOrEmpty(x.cccd));
            Assert.NotNull(session);

            var service = new SoatVeService(db, new MatMaService(config));

            var luotDiCountBefore = await db.LuotDis.CountAsync();
            var nhatKyCountBefore = await db.NhatKyQuetVes.CountAsync();

            var result = ToJson(await service.traCuuKhancCap(session.nhanVienId, new TraCuuKhancCapDto
            {
                phienSoatVeId = session.id,
                thietBiSoatVeId = session.thietBiSoatVeId,
                cccd = passenger.cccd
            }));

            Assert.True(result.GetProperty("timThay").GetBoolean());
            var maskedCccd = result.GetProperty("thongTinKhach").GetProperty("cccd").GetString();
            Assert.Contains("*", maskedCccd);

            // Verification step must NOT increase journey or scan count
            var luotDiCountAfter = await db.LuotDis.CountAsync();
            var nhatKyCountAfter = await db.NhatKyQuetVes.CountAsync();
            Assert.Equal(luotDiCountBefore, luotDiCountAfter);
            Assert.Equal(nhatKyCountBefore, nhatKyCountAfter);
        }
    }

    [Fact]
    public async Task XacNhanThuCong_RecordsJourney_EnforcesIdempotencyAndSingleUse()
    {
        var (db, config) = GetContext();
        long? createdTicketId = null;
        Guid testMaVe = Guid.NewGuid();
        await using (db)
        {
            var session = await db.PhienSoatVes
                .Include(x => x.chuyenXe)
                    .ThenInclude(cx => cx.loTrinh)
                .FirstOrDefaultAsync(x => x.ketThucLuc == null);
            Assert.NotNull(session);

            var passengerId = await db.NguoiDungs.Where(x => x.vaiTro == "HanhKhach").Select(x => x.id).FirstAsync();
            var key = await db.KhoaKySos.FirstAsync(x => x.trangThai == "DangDung");
            var orderDetailId = await db.ChiTietDonHangs.Select(x => x.id).FirstAsync();
            var maxStt = await db.Ves.Where(x => x.chiTietDonHangId == orderDetailId).MaxAsync(x => (short?)x.soThuTu) ?? 0;

            // Create a temporary single-use ticket for testing
            var ticket = new Ve
            {
                maVe = testMaVe,
                chiTietDonHangId = orderDetailId,
                soThuTu = (short)(maxStt + 1),
                nguoiDungId = passengerId,
                maLoaiVe = "VeLuot",
                nhomDoiTuongId = 1,
                tuyenXeId = session.chuyenXe.loTrinh.tuyenXeId,
                khoaKySoId = key.id,
                phienBanDuLieu = 1,
                duLieuDaKy = new byte[] { 1, 2, 3 },
                chuKy = new byte[64],
                biMatTotpMaHoa = new byte[] { 7, 8, 9 },
                phienBanKhoaBaoVe = "AES_GCM_V1",
                chuKyTotpGiay = 30,
                soChuSoTotp = 8,
                hieuLucTu = DateTime.UtcNow.AddHours(-1),
                hetHanLuc = DateTime.UtcNow.AddHours(23),
                trangThai = "HoatDong",
                ngayPhatHanh = DateTime.UtcNow
            };
            db.Ves.Add(ticket);
            await db.SaveChangesAsync();
            createdTicketId = ticket.id;

            var service = new SoatVeService(db, new MatMaService(config));
            var reqId = Guid.NewGuid();

            try
            {
                // 1. Confirm journey manually: Success
                var res1 = ToJson(await service.xacNhanThuCong(session.nhanVienId, new XacNhanThuCongDto
                {
                    phienSoatVeId = session.id,
                    thietBiSoatVeId = session.thietBiSoatVeId,
                    maVe = testMaVe,
                    requestId = reqId
                }));
                Assert.True(res1.GetProperty("hopLe").GetBoolean());

                var journeys = await db.LuotDis.Where(x => x.veId == ticket.id).ToListAsync();
                Assert.Single(journeys);
                Assert.Equal("HopLe", journeys[0].trangThai);

                // 2. Replay with same requestId: Idempotent return without duplicate journey
                var resReplay = ToJson(await service.xacNhanThuCong(session.nhanVienId, new XacNhanThuCongDto
                {
                    phienSoatVeId = session.id,
                    thietBiSoatVeId = session.thietBiSoatVeId,
                    maVe = testMaVe,
                    requestId = reqId
                }));
                Assert.True(resReplay.GetProperty("hopLe").GetBoolean());
                var journeysAfterReplay = await db.LuotDis.Where(x => x.veId == ticket.id).CountAsync();
                Assert.Equal(1, journeysAfterReplay);

                // 3. New confirmation request on already used single ticket: must reject as duplicate
                var resSecondAttempt = ToJson(await service.xacNhanThuCong(session.nhanVienId, new XacNhanThuCongDto
                {
                    phienSoatVeId = session.id,
                    thietBiSoatVeId = session.thietBiSoatVeId,
                    maVe = testMaVe,
                    requestId = Guid.NewGuid()
                }));
                Assert.False(resSecondAttempt.GetProperty("hopLe").GetBoolean());
                Assert.Equal("QUET_TRUNG", resSecondAttempt.GetProperty("maLyDo").GetString());

                // Check fraud alert was generated
                var alerts = await db.CanhBaoGianLans.Where(x => x.maCanhBao == "QUET_TRUNG" && x.nhatKyQuetVe.veId == ticket.id).ToListAsync();
                Assert.NotEmpty(alerts);
            }
            finally
            {
                if (createdTicketId.HasValue)
                {
                    await db.CanhBaoGianLans.Where(x => x.nhatKyQuetVe.veId == createdTicketId.Value).ExecuteDeleteAsync();
                    await db.LuotDis.Where(x => x.veId == createdTicketId.Value).ExecuteDeleteAsync();
                    await db.NhatKyQuetVes.Where(x => x.veId == createdTicketId.Value).ExecuteDeleteAsync();
                    await db.Ves.Where(x => x.id == createdTicketId.Value).ExecuteDeleteAsync();
                }
            }
        }
    }

    [Fact]
    public async Task KhoaVe_SuspendsTicketAndCreatesAuditLog()
    {
        var (db, config) = GetContext();
        long? ticketId = null;
        var maVe = Guid.NewGuid();
        await using (db)
        {
            var staffId = await db.NguoiDungs.Where(x => x.vaiTro == "NhanVienSoatVe").Select(x => x.id).FirstAsync();
            var passengerId = await db.NguoiDungs.Where(x => x.vaiTro == "HanhKhach").Select(x => x.id).FirstAsync();
            var key = await db.KhoaKySos.FirstAsync(x => x.trangThai == "DangDung");
            var orderDetailId = await db.ChiTietDonHangs.Select(x => x.id).FirstAsync();
            var maxStt = await db.Ves.Where(x => x.chiTietDonHangId == orderDetailId).MaxAsync(x => (short?)x.soThuTu) ?? 0;

            var ticket = new Ve
            {
                maVe = maVe,
                chiTietDonHangId = orderDetailId,
                soThuTu = (short)(maxStt + 1),
                nguoiDungId = passengerId,
                maLoaiVe = "VeLuot",
                nhomDoiTuongId = 1,
                khoaKySoId = key.id,
                phienBanDuLieu = 1,
                duLieuDaKy = new byte[] { 1, 2 },
                chuKy = new byte[64],
                biMatTotpMaHoa = new byte[] { 5, 6 },
                phienBanKhoaBaoVe = "AES_GCM_V1",
                chuKyTotpGiay = 30,
                soChuSoTotp = 8,
                hieuLucTu = DateTime.UtcNow.AddHours(-1),
                hetHanLuc = DateTime.UtcNow.AddHours(23),
                trangThai = "HoatDong",
                ngayPhatHanh = DateTime.UtcNow
            };
            db.Ves.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.id;

            var veService = new VeService(db, new MatMaService(config), new SigningKeyService(config));
            try
            {
                var success = await veService.khoaVe(new KhoaVeDto
                {
                    maVe = maVe,
                    lyDo = "Nghi ngờ gian lận dùng chung mã QR",
                    nguoiThucHienId = staffId,
                    thuHoiVinhVien = false
                });

                Assert.True((bool)success);

                db.ChangeTracker.Clear();
                var updatedTicket = await db.Ves.FindAsync(ticket.id);
                Assert.Equal("TamKhoa", updatedTicket!.trangThai);

                var auditLogs = await db.NhatKyThuHoiVes.Where(x => x.veId == ticket.id).ToListAsync();
                Assert.Single(auditLogs);
                Assert.Equal("TamKhoa", auditLogs[0].hanhDong);
                Assert.Equal("Nghi ngờ gian lận dùng chung mã QR", auditLogs[0].lyDo);
            }
            finally
            {
                if (ticketId.HasValue)
                {
                    await db.NhatKyThuHoiVes.Where(x => x.veId == ticketId.Value).ExecuteDeleteAsync();
                    await db.Ves.Where(x => x.id == ticketId.Value).ExecuteDeleteAsync();
                }
            }
        }
    }
}
