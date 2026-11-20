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

public class OnlineQrValidationTests
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
    public async Task XacThucVe_ValidTicket_AcceptsAndRejectsDuplicate()
    {
        var (db, config) = GetContext();
        await using (db)
        {
            var user = await db.NguoiDungs.FirstAsync(x => x.vaiTro == "HanhKhach");
            var staff = await db.NguoiDungs.FirstAsync(x => x.vaiTro == "NhanVienSoatVe");
            var trip = await db.ChuyenXes.Include(x => x.loTrinh).FirstAsync();
            var routeId = trip.loTrinh.tuyenXeId;

            var device = new ThietBiSoatVe
            {
                maThietBi = Guid.NewGuid(),
                tenThietBi = "Test Online Validator",
                trangThai = true,
                ngayDangKy = DateTime.UtcNow
            };
            db.ThietBiSoatVes.Add(device);

            var fare = await db.GiaVes.FirstAsync(x => x.maLoaiVe == "VeLuot" && x.tuyenXeId == routeId);

            var order = new DonHang
            {
                maDonHang = Guid.NewGuid(),
                maYeuCau = Guid.NewGuid(),
                nguoiDungId = user.id,
                tongGiaGoc = 7000,
                tongMienGiam = 0,
                trangThai = "ChoThanhToan",
                ngayTao = DateTime.UtcNow,
                hetHanLuc = DateTime.UtcNow.AddMinutes(30)
            };
            db.DonHangs.Add(order);
            await db.SaveChangesAsync();

            var session = new PhienSoatVe
            {
                id = Guid.NewGuid(),
                thietBiSoatVeId = device.id,
                nhanVienId = staff.id,
                chuyenXeId = trip.id,
                batDauLuc = DateTime.UtcNow
            };
            db.PhienSoatVes.Add(session);

            db.ChiTietDonHangs.Add(new ChiTietDonHang
            {
                donHangId = order.id,
                giaVeId = fare.id,
                soLuong = 1,
                donGiaGoc = 7000,
                mienGiamMoiVe = 0
            });
            await db.SaveChangesAsync();

            try
            {
                var crypto = new MatMaService(config);
                var veService = new VeService(db, crypto, new SigningKeyService(config));
                await veService.thanhToanDonHang(new ThanhToanDonHangDto { donHangId = order.id });

                var ticket = await db.Ves.Include(x => x.khoaKySo).SingleAsync(x => x.chiTietDonHang.donHangId == order.id);
                var now = DateTime.UtcNow;
                var secret = crypto.GiaiMaAes(ticket.biMatTotpMaHoa);
                var otp = crypto.TinhMaTotp(secret, now);
                var qrPayload = $"SMARTBUS:v1:{ticket.maVe}:{otp}:{Convert.ToBase64String(ticket.chuKy)}";

                var soatVeService = new SoatVeService(db, crypto);

                // Lần 1: Quét hợp lệ
                var res1 = ToJson(await soatVeService.xacThucVe(new QuetVeRequestDto
                {
                    phienSoatVeId = session.id,
                    thietBiSoatVeId = device.id,
                    soThuTuThietBi = 1,
                    maQrPayload = qrPayload,
                    thoiDiemQuet = now,
                    cheDo = "Online"
                }));

                Assert.True(res1.GetProperty("hopLe").GetBoolean());

                // Lần 2: Quét lại cùng vé lượt -> Từ chối QUET_TRUNG
                var res2 = ToJson(await soatVeService.xacThucVe(new QuetVeRequestDto
                {
                    phienSoatVeId = session.id,
                    thietBiSoatVeId = device.id,
                    soThuTuThietBi = 2,
                    maQrPayload = qrPayload,
                    thoiDiemQuet = now,
                    cheDo = "Online"
                }));

                Assert.False(res2.GetProperty("hopLe").GetBoolean());
                Assert.Equal("QUET_TRUNG", res2.GetProperty("maLyDo").GetString());

                // Đảm bảo chỉ có 1 lượt đi được công nhận
                var luotCount = await db.LuotDis.CountAsync(x => x.veId == ticket.id && x.trangThai == "HopLe");
                Assert.Equal(1, luotCount);

                // Có cảnh báo gian lận
                var alert = await db.CanhBaoGianLans.AnyAsync(x => x.nhatKyQuetVe.thietBiSoatVeId == device.id && x.maCanhBao == "QUET_TRUNG");
                Assert.True(alert);
            }
            finally
            {
                await db.CanhBaoGianLans.Where(x => x.nhatKyQuetVe.thietBiSoatVeId == device.id).ExecuteDeleteAsync();
                await db.LuotDis.Where(x => x.NhatKyQuetVe.thietBiSoatVeId == device.id).ExecuteDeleteAsync();
                await db.NhatKyQuetVes.Where(x => x.thietBiSoatVeId == device.id).ExecuteDeleteAsync();
                await db.PhienSoatVes.Where(x => x.id == session.id).ExecuteDeleteAsync();
                await db.Ves.Where(x => x.chiTietDonHang.donHangId == order.id).ExecuteDeleteAsync();
                await db.ThanhToans.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
                await db.ChiTietDonHangs.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
                await db.DonHangs.Where(x => x.id == order.id).ExecuteDeleteAsync();
                await db.ThietBiSoatVes.Where(x => x.id == device.id).ExecuteDeleteAsync();
            }
        }
    }

    [Fact]
    public async Task XacThucVe_WrongRoute_RejectsWithSaiTuyen()
    {
        var (db, config) = GetContext();
        await using (db)
        {
            var user = await db.NguoiDungs.FirstAsync(x => x.vaiTro == "HanhKhach");
            var staff = await db.NguoiDungs.FirstAsync(x => x.vaiTro == "NhanVienSoatVe");

            var trip = await db.ChuyenXes.Include(x => x.loTrinh).FirstAsync();
            var routeId = trip.loTrinh.tuyenXeId;

            // Tìm một tuyến khác
            var otherFare = await db.GiaVes.FirstOrDefaultAsync(x => x.maLoaiVe == "VeLuot" && x.tuyenXeId != routeId && x.tuyenXeId != null);
            if (otherFare == null) return; // Không có tuyến thứ 2 trong DB

            var device = new ThietBiSoatVe
            {
                maThietBi = Guid.NewGuid(),
                tenThietBi = "Test Wrong Route Validator",
                trangThai = true,
                ngayDangKy = DateTime.UtcNow
            };
            db.ThietBiSoatVes.Add(device);

            var order = new DonHang
            {
                maDonHang = Guid.NewGuid(),
                maYeuCau = Guid.NewGuid(),
                nguoiDungId = user.id,
                tongGiaGoc = 7000,
                tongMienGiam = 0,
                trangThai = "ChoThanhToan",
                ngayTao = DateTime.UtcNow,
                hetHanLuc = DateTime.UtcNow.AddMinutes(30)
            };
            db.DonHangs.Add(order);
            await db.SaveChangesAsync();

            var session = new PhienSoatVe
            {
                id = Guid.NewGuid(),
                thietBiSoatVeId = device.id,
                nhanVienId = staff.id,
                chuyenXeId = trip.id,
                batDauLuc = DateTime.UtcNow
            };
            db.PhienSoatVes.Add(session);

            db.ChiTietDonHangs.Add(new ChiTietDonHang
            {
                donHangId = order.id,
                giaVeId = otherFare.id,
                soLuong = 1,
                donGiaGoc = 7000,
                mienGiamMoiVe = 0
            });
            await db.SaveChangesAsync();

            try
            {
                var crypto = new MatMaService(config);
                var veService = new VeService(db, crypto, new SigningKeyService(config));
                await veService.thanhToanDonHang(new ThanhToanDonHangDto { donHangId = order.id });

                var ticket = await db.Ves.Include(x => x.khoaKySo).SingleAsync(x => x.chiTietDonHang.donHangId == order.id);
                var now = DateTime.UtcNow;
                var secret = crypto.GiaiMaAes(ticket.biMatTotpMaHoa);
                var otp = crypto.TinhMaTotp(secret, now);
                var qrPayload = $"SMARTBUS:v1:{ticket.maVe}:{otp}:{Convert.ToBase64String(ticket.chuKy)}";

                var soatVeService = new SoatVeService(db, crypto);

                var res = ToJson(await soatVeService.xacThucVe(new QuetVeRequestDto
                {
                    phienSoatVeId = session.id,
                    thietBiSoatVeId = device.id,
                    soThuTuThietBi = 1,
                    maQrPayload = qrPayload,
                    thoiDiemQuet = now,
                    cheDo = "Online"
                }));

                Assert.False(res.GetProperty("hopLe").GetBoolean());
                Assert.Equal("SAI_TUYEN", res.GetProperty("maLyDo").GetString());
            }
            finally
            {
                await db.CanhBaoGianLans.Where(x => x.nhatKyQuetVe.thietBiSoatVeId == device.id).ExecuteDeleteAsync();
                await db.LuotDis.Where(x => x.NhatKyQuetVe.thietBiSoatVeId == device.id).ExecuteDeleteAsync();
                await db.NhatKyQuetVes.Where(x => x.thietBiSoatVeId == device.id).ExecuteDeleteAsync();
                await db.PhienSoatVes.Where(x => x.id == session.id).ExecuteDeleteAsync();
                await db.Ves.Where(x => x.chiTietDonHang.donHangId == order.id).ExecuteDeleteAsync();
                await db.ThanhToans.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
                await db.ChiTietDonHangs.Where(x => x.donHangId == order.id).ExecuteDeleteAsync();
                await db.DonHangs.Where(x => x.id == order.id).ExecuteDeleteAsync();
                await db.ThietBiSoatVes.Where(x => x.id == device.id).ExecuteDeleteAsync();
            }
        }
    }
}
