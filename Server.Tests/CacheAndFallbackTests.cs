using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Tests;

public class InMemoryTestCache : ICacheService
{
    private readonly Dictionary<string, (object value, DateTime expiresAt)> _store = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (_store.TryGetValue(key, out var item))
        {
            if (item.expiresAt >= DateTime.UtcNow)
            {
                return Task.FromResult<T?>((T)item.value);
            }
            _store.Remove(key);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        if (value != null)
        {
            _store[key] = (value, DateTime.UtcNow.Add(ttl));
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        var keys = _store.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var k in keys) _store.Remove(k);
        return Task.CompletedTask;
    }

    public int Count => _store.Count;
}

public class CacheAndFallbackTests
{
    private static IConfiguration GetConfig()
    {
        var server = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Server"));
        return new ConfigurationBuilder()
            .SetBasePath(server)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true)
            .Build();
    }

    [Fact]
    public async Task CacheService_WhenRedisUnavailable_FallsBackGracefully()
    {
        // Khi không có IConnectionMultiplexer (môi trường không chạy Redis)
        var sp = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider();
        var cache = new CacheService(sp, NullLogger<CacheService>.Instance);

        var val = await cache.GetAsync<string>("any_key");
        Assert.Null(val);

        // Các thao tác ghi, xóa không ném Exception
        await cache.SetAsync("any_key", "test_val", TimeSpan.FromMinutes(5));
        await cache.RemoveAsync("any_key");
        await cache.RemoveByPrefixAsync("smartbus:cache:");
    }

    [Fact]
    public async Task VanHanhService_WithCache_CachesRouteListAndInvalidatesOnSave()
    {
        var config = GetConfig();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);

        var mockCache = new InMemoryTestCache();
        var service = new VanHanhService(db, mockCache);

        // Lần 1: Cache trống -> Đọc DB và ghi cache
        Assert.Equal(0, mockCache.Count);
        var res1 = await service.layDanhSachTuyenXe();
        Assert.NotNull(res1);
        Assert.True(mockCache.Count > 0);

        // Lần 2: Đã có cache -> Cache hit
        var cachedData = await mockCache.GetAsync<List<TuyenXeListItemDto>>("smartbus:cache:tuyen-xe:tat-ca");
        Assert.NotNull(cachedData);

        // Thêm một tuyến test để kiểm tra Invalidate cache
        var maTuyenTest = $"T-{Guid.NewGuid():N}"[..8];
        var dto = new TuyenXeDto
        {
            maTuyen = maTuyenTest,
            tenTuyen = "Tuyen Test Cache Invalidation",
            gioBatDau = "06:00",
            gioKetThuc = "20:00",
            gianCachPhut = 15,
            trangThai = true
        };

        try
        {
            await service.luuTuyenXe(dto);
            // Cache phải bị xóa sau khi lưu
            var afterSave = await mockCache.GetAsync<List<TuyenXeListItemDto>>("smartbus:cache:tuyen-xe:tat-ca");
            Assert.Null(afterSave);
        }
        finally
        {
            await db.TuyenXes.Where(x => x.maTuyen == maTuyenTest).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task VeService_WithCache_CachesFareList()
    {
        var config = GetConfig();
        var options = new DbContextOptionsBuilder<SmartBusDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
        await using var db = new SmartBusDbContext(options);

        var mockCache = new InMemoryTestCache();
        var service = new VeService(db, new MatMaService(config), new SigningKeyService(config), mockCache);

        // Lần 1: Lấy biểu giá toàn mạng (tuyenXeId = null)
        var fares1 = await service.layDanhSachBieuGia(null);
        Assert.NotNull(fares1);
        Assert.True(mockCache.Count > 0);

        var cachedFares = await mockCache.GetAsync<List<BieuGiaListItemDto>>("smartbus:cache:bieu-gia:0");
        Assert.NotNull(cachedFares);
    }
}
