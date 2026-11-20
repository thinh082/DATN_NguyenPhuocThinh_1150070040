using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Controllers;
using Server.Data;

namespace Server.Tests;

public class AdminTripTests
{
    [Fact]
    public async Task CannotScheduleVehicleTwiceDuringSamePeriod()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync();
        var existing = await db.ChuyenXes.FirstAsync(x => x.trangThai == "DangChay");
        var controller = new AdminController(db);
        var dto = new AdminController.ChuyenDto(existing.loTrinhId, existing.xeBuytId, existing.gioKhoiHanhDuKien.AddMinutes(1), existing.gioKetThucDuKien.AddMinutes(-1));
        Assert.IsType<BadRequestObjectResult>(await controller.TaoChuyen(dto));
    }
}
