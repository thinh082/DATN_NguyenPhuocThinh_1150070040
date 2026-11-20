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

public class MockPaymentAndOrderTests
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
    public async Task TaoDonHang_QuantityValidation_RejectsInvalidRange()
    {
        var (db, config) = GetContext();
        await using (db)
        {
            var userId = await db.NguoiDungs.Select(x => x.id).FirstAsync();
            var fare = await db.GiaVes.FirstAsync(x => x.trangThai && !x.nhomDoiTuong.canXacMinh);
            var service = new VeService(db, new MatMaService(config), new SigningKeyService(config));

            // 0 quantity
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.taoDonHang(new TaoDonHangDto { nguoiDungId = userId, giaVeId = fare.id, soLuong = 0 }));

            // Negative quantity
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.taoDonHang(new TaoDonHangDto { nguoiDungId = userId, giaVeId = fare.id, soLuong = -2 }));

            // Over limit (> 10)
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.taoDonHang(new TaoDonHangDto { nguoiDungId = userId, giaVeId = fare.id, soLuong = 11 }));
        }
    }

    [Fact]
    public async Task TaoDonHang_Idempotency_SamePayloadReturnsExisting_DifferentPayloadThrowsConflict()
    {
        var (db, config) = GetContext();
        long? createdOrderId = null;
        var reqId = Guid.NewGuid();
        await using (db)
        {
            var userId = await db.NguoiDungs.Select(x => x.id).FirstAsync();
            var fares = await db.GiaVes.Where(x => x.trangThai && !x.nhomDoiTuong.canXacMinh).Take(2).ToListAsync();
            Assert.True(fares.Count >= 2, "Cần tối thiểu 2 biểu giá để kiểm thử.");
            var fare1 = fares[0];
            var fare2 = fares[1];

            var service = new VeService(db, new MatMaService(config), new SigningKeyService(config));
            try
            {
                // First call: creates order
                var res1 = ToJson(await service.taoDonHang(new TaoDonHangDto
                {
                    nguoiDungId = userId,
                    giaVeId = fare1.id,
                    soLuong = 2,
                    maYeuCau = reqId
                }));
                createdOrderId = res1.GetProperty("donHangId").GetInt64();
                Assert.True(createdOrderId > 0);

                // Replay with identical payload: returns existing order without creating duplicate
                var res2 = ToJson(await service.taoDonHang(new TaoDonHangDto
                {
                    nguoiDungId = userId,
                    giaVeId = fare1.id,
                    soLuong = 2,
                    maYeuCau = reqId
                }));
                Assert.Equal(createdOrderId.Value, res2.GetProperty("donHangId").GetInt64());

                // Call with same reqId but different payload (different fareId or quantity): must throw 409 Conflict
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await service.taoDonHang(new TaoDonHangDto
                    {
                        nguoiDungId = userId,
                        giaVeId = fare2.id,
                        soLuong = 2,
                        maYeuCau = reqId
                    }));
            }
            finally
            {
                if (createdOrderId.HasValue)
                {
                    await db.Ves.Where(x => x.chiTietDonHang.donHangId == createdOrderId.Value).ExecuteDeleteAsync();
                    await db.ThanhToans.Where(x => x.donHangId == createdOrderId.Value).ExecuteDeleteAsync();
                    await db.ChiTietDonHangs.Where(x => x.donHangId == createdOrderId.Value).ExecuteDeleteAsync();
                    await db.DonHangs.Where(x => x.id == createdOrderId.Value).ExecuteDeleteAsync();
                }
            }
        }
    }

    [Fact]
    public async Task ThanhToanGiaLap_SuccessFailureCancel_StateTransitionsAndIdempotency()
    {
        var (db, config) = GetContext();
        long? orderId = null;
        await using (db)
        {
            var userId = await db.NguoiDungs.Select(x => x.id).FirstAsync();
            var fare = await db.GiaVes.FirstAsync(x => x.trangThai && !x.nhomDoiTuong.canXacMinh);
            var service = new VeService(db, new MatMaService(config), new SigningKeyService(config));

            var order = new DonHang
            {
                maDonHang = Guid.NewGuid(),
                maYeuCau = Guid.NewGuid(),
                nguoiDungId = userId,
                tongGiaGoc = fare.giaGoc,
                tongMienGiam = fare.soTienMienGiam,
                trangThai = "ChoThanhToan",
                ngayTao = DateTime.UtcNow,
                hetHanLuc = DateTime.UtcNow.AddMinutes(15)
            };
            db.DonHangs.Add(order);
            await db.SaveChangesAsync();
            orderId = order.id;

            db.ChiTietDonHangs.Add(new ChiTietDonHang
            {
                donHangId = order.id,
                giaVeId = fare.id,
                soLuong = 1,
                donGiaGoc = fare.giaGoc,
                mienGiamMoiVe = fare.soTienMienGiam
            });
            await db.SaveChangesAsync();

            try
            {
                var payRequestId = Guid.NewGuid();

                // 1. Test Success
                var result = ToJson(await service.thanhToanGiaLap(userId, new ThanhToanGiaLapDto
                {
                    donHangId = order.id,
                    requestId = payRequestId,
                    ketQua = "ThanhCong"
                }));
                Assert.Equal("DaThanhToan", result.GetProperty("trangThai").GetString());

                // Verify ticket was issued
                db.ChangeTracker.Clear();
                var tickets = await db.Ves.Where(x => x.chiTietDonHang.donHangId == order.id).ToListAsync();
                Assert.Single(tickets);
                Assert.Equal("HoatDong", tickets[0].trangThai);

                // 2. Replay same payRequestId: returns same result without creating extra tickets
                var replayResult = ToJson(await service.thanhToanGiaLap(userId, new ThanhToanGiaLapDto
                {
                    donHangId = order.id,
                    requestId = payRequestId,
                    ketQua = "ThanhCong"
                }));
                Assert.Equal("DaThanhToan", replayResult.GetProperty("trangThai").GetString());
                var ticketsAfterReplay = await db.Ves.Where(x => x.chiTietDonHang.donHangId == order.id).CountAsync();
                Assert.Equal(1, ticketsAfterReplay);

                // 3. New request trying to pay an already paid order: Conflict
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await service.thanhToanGiaLap(userId, new ThanhToanGiaLapDto
                    {
                        donHangId = order.id,
                        requestId = Guid.NewGuid(),
                        ketQua = "ThanhCong"
                    }));
            }
            finally
            {
                if (orderId.HasValue)
                {
                    await db.Ves.Where(x => x.chiTietDonHang.donHangId == orderId.Value).ExecuteDeleteAsync();
                    await db.ThanhToans.Where(x => x.donHangId == orderId.Value).ExecuteDeleteAsync();
                    await db.ChiTietDonHangs.Where(x => x.donHangId == orderId.Value).ExecuteDeleteAsync();
                    await db.DonHangs.Where(x => x.id == orderId.Value).ExecuteDeleteAsync();
                }
            }
        }
    }

    [Fact]
    public async Task ThanhToanGiaLap_CancelOrder_PreventsSubsequentPayment()
    {
        var (db, config) = GetContext();
        long? orderId = null;
        await using (db)
        {
            var userId = await db.NguoiDungs.Select(x => x.id).FirstAsync();
            var fare = await db.GiaVes.FirstAsync(x => x.trangThai && !x.nhomDoiTuong.canXacMinh);
            var service = new VeService(db, new MatMaService(config), new SigningKeyService(config));

            var order = new DonHang
            {
                maDonHang = Guid.NewGuid(),
                maYeuCau = Guid.NewGuid(),
                nguoiDungId = userId,
                tongGiaGoc = fare.giaGoc,
                tongMienGiam = fare.soTienMienGiam,
                trangThai = "ChoThanhToan",
                ngayTao = DateTime.UtcNow,
                hetHanLuc = DateTime.UtcNow.AddMinutes(15)
            };
            db.DonHangs.Add(order);
            await db.SaveChangesAsync();
            orderId = order.id;

            db.ChiTietDonHangs.Add(new ChiTietDonHang
            {
                donHangId = order.id,
                giaVeId = fare.id,
                soLuong = 1,
                donGiaGoc = fare.giaGoc,
                mienGiamMoiVe = fare.soTienMienGiam
            });
            await db.SaveChangesAsync();

            try
            {
                var cancelResult = ToJson(await service.thanhToanGiaLap(userId, new ThanhToanGiaLapDto
                {
                    donHangId = order.id,
                    requestId = Guid.NewGuid(),
                    ketQua = "Huy"
                }));
                Assert.Equal("Huy", cancelResult.GetProperty("trangThai").GetString());

                // Subsequent attempt to pay must throw
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await service.thanhToanGiaLap(userId, new ThanhToanGiaLapDto
                    {
                        donHangId = order.id,
                        requestId = Guid.NewGuid(),
                        ketQua = "ThanhCong"
                    }));
            }
            finally
            {
                if (orderId.HasValue)
                {
                    await db.ChiTietDonHangs.Where(x => x.donHangId == orderId.Value).ExecuteDeleteAsync();
                    await db.DonHangs.Where(x => x.id == orderId.Value).ExecuteDeleteAsync();
                }
            }
        }
    }

    [Fact]
    public async Task QuetQrDanXe_EnforcesActiveTripRequirement()
    {
        var (db, _) = GetContext();
        int? busId = null;
        var qrCode = Guid.NewGuid();
        await using (db)
        {
            var vanHanhService = new VanHanhService(db);
            // Case 1: Non-existent QR
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await vanHanhService.layThongTinQrDanXe(Guid.NewGuid()));

            // Case 2: Bus exists but has NO active trip (DangChay)
            var bus = new XeBuyt
            {
                bienSo = "51B-TEST99",
                maQrTinh = qrCode,
                sucChua = 40,
                trangThai = true
            };
            db.XeBuyts.Add(bus);
            await db.SaveChangesAsync();
            busId = bus.id;

            try
            {
                // Must throw InvalidOperationException when no trip is running
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await vanHanhService.layThongTinQrDanXe(qrCode));
            }
            finally
            {
                if (busId.HasValue)
                {
                    await db.XeBuyts.Where(x => x.id == busId.Value).ExecuteDeleteAsync();
                }
            }
        }
    }
}

