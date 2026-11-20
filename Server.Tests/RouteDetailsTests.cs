using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Services;
using Server.Entities;

namespace Server.Tests;

public class RouteDetailsTests
{
    [Fact]
    public async Task RouteDetailsDoNotDisplayExpiredItineraries()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync();
        var route = await db.TuyenXes.FirstAsync(x => x.maTuyen == "01");
        var old = new LoTrinh { tuyenXeId = route.id, chieu = "Di", phienBan = 32000, tenLoTrinh = "Expired test", hieuLucTu = DateTime.UtcNow.AddDays(-4), hieuLucDen = DateTime.UtcNow.AddDays(-2) };
        db.LoTrinhs.Add(old); await db.SaveChangesAsync();
        var result = JsonSerializer.SerializeToElement((object)await new VanHanhService(db).layChiTietTuyenXe(route.id));
        Assert.DoesNotContain(result.GetProperty("loTrinhs").EnumerateArray(), x => x.GetProperty("id").GetInt32() == old.id);
    }
    [Fact]
    public async Task ExistingRouteContainsStopsInTravelOrder()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true).Build();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);
        var route = await db.TuyenXes.FirstAsync(x => x.maTuyen == "01");
        var result = JsonSerializer.SerializeToElement((object)await new VanHanhService(db).layChiTietTuyenXe(route.id));
        var stops = result.GetProperty("loTrinhs")[0].GetProperty("danhSachTram").EnumerateArray().Select(x => x.GetProperty("thuTu").GetInt32()).ToArray();
        Assert.NotEmpty(stops);
        Assert.Equal(stops.Order().ToArray(), stops);
    }
}
