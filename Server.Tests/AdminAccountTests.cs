using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Tests;

public class AdminAccountTests
{
    [Fact]
    public async Task AdminCanCreateStaffWhoCanSignIn()
    {
        var project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(project).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true).AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        var email = $"staff-{Guid.NewGuid():N}@example.invalid";
        try
        {
            await new NguoiDungService(db).luuNguoiDung(new NguoiDungDto { hoTen = "Test Staff", email = email, vaiTro = "NhanVienSoatVe", matKhau = "Test12345", trangThai = true });
            var user = await db.NguoiDungs.SingleAsync(x => x.email == email);
            Assert.Equal("NhanVienSoatVe", user.vaiTro);
            Assert.NotNull(await new XacThucService(db, config).dangNhap(new DangNhapDto { taiKhoan = email, matKhau = "Test12345" }));
        }
        finally { await db.NguoiDungs.Where(x => x.email == email).ExecuteDeleteAsync(); }
    }
}
