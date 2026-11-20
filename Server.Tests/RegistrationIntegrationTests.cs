using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Tests;

public class RegistrationIntegrationTests
{
    [Fact]
    public async Task PublicRegistrationCannotChooseAdministratorRole()
    {
        var project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(project).AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        var email = $"test-{Guid.NewGuid():N}@example.invalid";
        try
        {
            var service = new XacThucService(db, config);
            await service.dangKy(new DangKyDto { hoTen = "Test", email = email, matKhau = "test1234", vaiTro = "QuanTriVien" });
            Assert.Equal("HanhKhach", (await db.NguoiDungs.SingleAsync(x => x.email == email)).vaiTro);
        }
        finally { await db.NguoiDungs.Where(x => x.email == email).ExecuteDeleteAsync(); }
    }
}
