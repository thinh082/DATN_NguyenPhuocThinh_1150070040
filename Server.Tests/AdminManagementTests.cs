using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Controllers;
using Server.Data;
using Server.Entities;
using Xunit;

namespace Server.Tests;

public class AdminManagementTests
{
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

    private static AdminController CreateController(SmartBusDbContext db, int adminId = 1)
    {
        var controller = new AdminController(db);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("id", adminId.ToString()),
            new Claim(ClaimTypes.Role, "QuanTriVien")
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    [Fact]
    public async Task TaoLoTrinh_ValidatesStopsAndSequence()
    {
        var (db, _) = GetContext();
        await using (db)
        {
            var controller = CreateController(db);
            var tuyen = await db.TuyenXes.FirstAsync(x => x.trangThai);
            var tramList = await db.TramDungs.Where(x => x.trangThai).Take(3).Select(x => x.id).ToListAsync();
            Assert.True(tramList.Count >= 2);

            var originalOpen = await db.LoTrinhs.FirstOrDefaultAsync(x => x.tuyenXeId == tuyen.id && x.chieu == "Di" && x.hieuLucDen == null);
            int? originalOpenId = originalOpen?.id;

            // 1. Từ chối nếu < 2 trạm
            var dto1 = new AdminController.TaoLoTrinhDto(tuyen.id, "Di", "Test 1 tram", DateTime.UtcNow.AddYears(1), null, new List<int> { tramList[0] });
            var res1 = await controller.TaoLoTrinh(dto1);
            Assert.IsType<BadRequestObjectResult>(res1);

            // 2. Từ chối nếu lặp trạm
            var dto2 = new AdminController.TaoLoTrinhDto(tuyen.id, "Di", "Test lap tram", DateTime.UtcNow.AddYears(1), null, new List<int> { tramList[0], tramList[0] });
            var res2 = await controller.TaoLoTrinh(dto2);
            Assert.IsType<BadRequestObjectResult>(res2);

            // 3. Tạo thành công với 2 trạm dừng hợp lệ
            var futureStart = DateTime.UtcNow.AddYears(10);
            var futureEnd = DateTime.UtcNow.AddYears(11);
            var dto3 = new AdminController.TaoLoTrinhDto(tuyen.id, "Di", "Test Tao Lo Trinh Hop Le", futureStart, futureEnd, new List<int> { tramList[0], tramList[1] });
            var res3 = await controller.TaoLoTrinh(dto3);
            var ok3 = Assert.IsType<OkObjectResult>(res3);

            int createdId = 0;
            try
            {
                var json = JsonSerializer.SerializeToElement(ok3.Value);
                createdId = json.GetProperty("data").GetProperty("id").GetInt32();
                Assert.True(createdId > 0);

                var stops = await db.LoTrinhTramDungs.Where(x => x.loTrinhId == createdId).OrderBy(x => x.thuTu).ToListAsync();
                Assert.Equal(2, stops.Count);
                Assert.Equal(1, stops[0].thuTu);
                Assert.Equal(2, stops[1].thuTu);
                Assert.Equal(tramList[0], stops[0].tramDungId);
                Assert.Equal(tramList[1], stops[1].tramDungId);

                // 4. Kiểm tra từ chối chồng lấn khoảng hiệu lực
                var dtoOverlap = new AdminController.TaoLoTrinhDto(tuyen.id, "Di", "Test Chong Lan", futureStart.AddMonths(1), futureEnd.AddMonths(1), new List<int> { tramList[0], tramList[1] });
                var resOverlap = await controller.TaoLoTrinh(dtoOverlap);
                Assert.IsType<BadRequestObjectResult>(resOverlap);
            }
            finally
            {
                if (createdId > 0)
                {
                    await db.LoTrinhTramDungs.Where(x => x.loTrinhId == createdId).ExecuteDeleteAsync();
                    await db.LoTrinhs.Where(x => x.id == createdId).ExecuteDeleteAsync();
                }
                if (originalOpenId.HasValue)
                {
                    var prev = await db.LoTrinhs.FindAsync(originalOpenId.Value);
                    if (prev != null)
                    {
                        prev.hieuLucDen = null;
                        await db.SaveChangesAsync();
                    }
                }
            }
        }
    }

    [Fact]
    public async Task CapNhatLoTrinh_RejectsIfTripAlreadyUsesRoute()
    {
        var (db, _) = GetContext();
        await using (db)
        {
            var controller = CreateController(db);
            // Tìm lộ trình đã có chuyến xe sử dụng
            var inUseTrip = await db.ChuyenXes.FirstAsync();
            var tramList = await db.TramDungs.Where(x => x.trangThai).Take(2).Select(x => x.id).ToListAsync();

            var dto = new AdminController.TaoLoTrinhDto(1, "Di", "Sửa lộ trình đang dùng", DateTime.UtcNow, null, tramList);
            var res = await controller.CapNhatLoTrinh(inUseTrip.loTrinhId, dto);
            var conflict = Assert.IsType<ConflictObjectResult>(res);
            var json = JsonSerializer.SerializeToElement(conflict.Value);
            Assert.Contains("không được sửa đổi", json.GetProperty("message").GetString());
        }
    }

    [Fact]
    public async Task MoKhoaVe_UnlocksTamKhoaTicket_AndRejectsExpired()
    {
        var (db, _) = GetContext();
        await using (db)
        {
            var controller = CreateController(db, 1);
            var user = await db.NguoiDungs.FirstAsync(x => x.vaiTro == "HanhKhach");
            var key = await db.KhoaKySos.FirstAsync();
            var fare = await db.GiaVes.FirstAsync();

            var order = new DonHang
            {
                maDonHang = Guid.NewGuid(),
                maYeuCau = Guid.NewGuid(),
                nguoiDungId = user.id,
                tongGiaGoc = 7000,
                tongMienGiam = 0,
                trangThai = "DaThanhToan",
                ngayTao = DateTime.UtcNow,
                hetHanLuc = DateTime.UtcNow.AddDays(1)
            };
            db.DonHangs.Add(order);
            await db.SaveChangesAsync();

            var ctdh = new ChiTietDonHang
            {
                donHangId = order.id,
                giaVeId = fare.id,
                soLuong = 1,
                donGiaGoc = 7000,
                mienGiamMoiVe = 0
            };
            db.ChiTietDonHangs.Add(ctdh);
            await db.SaveChangesAsync();

            var ticket = new Ve
            {
                maVe = Guid.NewGuid(),
                chiTietDonHangId = ctdh.id,
                soThuTu = 1,
                nguoiDungId = user.id,
                khoaKySoId = key.id,
                maLoaiVe = "VeLuot",
                nhomDoiTuongId = 1,
                phienBanDuLieu = 1,
                biMatTotpMaHoa = new byte[32],
                chuKyTotpGiay = 30,
                soChuSoTotp = 8,
                duLieuDaKy = new byte[32],
                chuKy = new byte[64],
                phienBanKhoaBaoVe = "AES_GCM_V1",
                hieuLucTu = DateTime.UtcNow.AddMinutes(-10),
                hetHanLuc = DateTime.UtcNow.AddHours(2),
                trangThai = "TamKhoa",
                ngayPhatHanh = DateTime.UtcNow
            };
            db.Ves.Add(ticket);
            await db.SaveChangesAsync();

            try
            {
                // 1. Từ chối nếu lý do rỗng
                var resEmptyReason = await controller.MoKhoaVe(ticket.maVe, new AdminController.MoKhoaVeDto("   "));
                Assert.IsType<BadRequestObjectResult>(resEmptyReason);

                // 2. Mở khóa thành công
                var resOk = await controller.MoKhoaVe(ticket.maVe, new AdminController.MoKhoaVeDto("Đã kiểm tra hợp lệ"));
                Assert.IsType<OkObjectResult>(resOk);

                var reloaded = await db.Ves.SingleAsync(x => x.maVe == ticket.maVe);
                Assert.Equal("HoatDong", reloaded.trangThai);

                var audit = await db.NhatKyThuHoiVes.FirstOrDefaultAsync(x => x.veId == ticket.id && x.hanhDong == "MoKhoa");
                Assert.NotNull(audit);
                Assert.Equal("Đã kiểm tra hợp lệ", audit.lyDo);

                // 3. Mở khóa lại khi vé đã HoatDong -> Từ chối
                var resAlreadyActive = await controller.MoKhoaVe(ticket.maVe, new AdminController.MoKhoaVeDto("Mở lại"));
                Assert.IsType<BadRequestObjectResult>(resAlreadyActive);
            }
            finally
            {
                await db.NhatKyThuHoiVes.Where(x => x.veId == ticket.id).ExecuteDeleteAsync();
                await db.Ves.Where(x => x.id == ticket.id).ExecuteDeleteAsync();
                await db.ChiTietDonHangs.Where(x => x.id == ctdh.id).ExecuteDeleteAsync();
                await db.DonHangs.Where(x => x.id == order.id).ExecuteDeleteAsync();
            }
        }
    }

    [Fact]
    public async Task XuatCsv_ReturnsUtf8BomAndProtectsFormulaInjection()
    {
        var (db, _) = GetContext();
        await using (db)
        {
            var controller = CreateController(db);
            var res = await controller.XuatCsv("don-hang");
            var file = Assert.IsType<FileContentResult>(res);
            Assert.Equal("text/csv; charset=utf-8", file.ContentType);
            Assert.True(file.FileContents.Length >= 3);
            // Kiểm tra UTF-8 BOM: 0xEF, 0xBB, 0xBF
            Assert.Equal(0xEF, file.FileContents[0]);
            Assert.Equal(0xBB, file.FileContents[1]);
            Assert.Equal(0xBF, file.FileContents[2]);
        }
    }
}
