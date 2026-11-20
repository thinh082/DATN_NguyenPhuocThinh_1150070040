using System.Security.Cryptography;
using Server.Services;

namespace Server.Tests;

public class MatMaTests
{
    [Fact]
    public void TotpSha256MatchesRfc6238Vector()
    {
        var service = new MatMaService();
        var secret = System.Text.Encoding.ASCII.GetBytes("12345678901234567890123456789012");
        Assert.Equal("46119246", service.TinhMaTotp(secret, DateTimeOffset.FromUnixTimeSeconds(59).UtcDateTime));
    }
    [Fact]
    public void MaHoaPhaiPhatHienDuLieuBiThayDoi()
    {
        var service = new MatMaService();
        var key = RandomNumberGenerator.GetBytes(32);
        var original = RandomNumberGenerator.GetBytes(32);
        var encrypted = service.MaHoaAes(original, key);
        Assert.Equal(original, service.GiaiMaAes(encrypted, key));
        encrypted[0] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => service.GiaiMaAes(encrypted, key));
    }

    [Fact]
    public void MoiLanMaHoaPhaiDungNonceMoi()
    {
        var service = new MatMaService();
        var key = RandomNumberGenerator.GetBytes(32);
        var data = new byte[32];
        Assert.NotEqual(service.MaHoaAes(data, key), service.MaHoaAes(data, key));
    }
}
