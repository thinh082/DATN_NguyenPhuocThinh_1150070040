using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Server.Services;

namespace Server.Tests;

public class MatMaTests
{
    [Fact]
    public void HkdfSha256TaoCungKhoaChoCungDauVao()
    {
        var service = new MatMaService();
        var maVe = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var salt = Enumerable.Repeat((byte)0xA5, 16).ToArray();
        var khoaGoc = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        var biMat = service.PhaiSinhBiMatTotp(maVe, salt, khoaGoc);

        Assert.Equal(32, biMat.Length);
        Assert.Equal(Convert.FromHexString("BADB52356EE494676B33B7DBB26DD00907A5D154FD1355CE8A5A5CA27FDA71C5"), biMat);
        Assert.Equal(biMat, service.PhaiSinhBiMatTotp(maVe, salt, khoaGoc));
    }

    [Fact]
    public void HkdfSha256DoiKhoaKhiDoiMaVeHoacSalt()
    {
        var service = new MatMaService();
        var maVe = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var maVeKhac = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var salt = new byte[16];
        var saltKhac = Enumerable.Repeat((byte)1, 16).ToArray();
        var khoaGoc = new byte[32];

        var biMat = service.PhaiSinhBiMatTotp(maVe, salt, khoaGoc);

        Assert.False(biMat.AsSpan().SequenceEqual(service.PhaiSinhBiMatTotp(maVeKhac, salt, khoaGoc)));
        Assert.False(biMat.AsSpan().SequenceEqual(service.PhaiSinhBiMatTotp(maVe, saltKhac, khoaGoc)));
        Assert.False(service.PhaiSinhBiMatTotp(maVe, khoaGoc: khoaGoc).AsSpan()
            .SequenceEqual(service.PhaiSinhBiMatTotp(maVe, khoaGoc: khoaGoc)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HkdfSha256UuTienKhoaGocRiengVaHoTroCauHinhCu(bool coKhoaGoc)
    {
        var maVe = Guid.NewGuid();
        var salt = new byte[16];
        var khoaAes = Enumerable.Repeat((byte)2, 32).ToArray();
        var khoaGoc = Enumerable.Repeat((byte)3, 32).ToArray();
        var settings = new Dictionary<string, string?> { ["Security:TotpEncryptionKey"] = Convert.ToBase64String(khoaAes) };
        if (coKhoaGoc)
            settings["Security:TotpMasterKey"] = Convert.ToBase64String(khoaGoc);
        var service = new MatMaService(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        Assert.Equal(service.PhaiSinhBiMatTotp(maVe, salt, coKhoaGoc ? khoaGoc : khoaAes),
            service.PhaiSinhBiMatTotp(maVe, salt));
    }

    [Fact]
    public void BiMatTotpPhaiSinhHoatDongSauKhiMaHoaVaDocLai()
    {
        var service = new MatMaService();
        var biMat = service.PhaiSinhBiMatTotp(Guid.NewGuid(), new byte[16], new byte[32]);
        var khoaAes = RandomNumberGenerator.GetBytes(32);
        var biMatDaLuu = service.MaHoaAes(biMat, khoaAes);
        var thoiDiem = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000).UtcDateTime;
        var otp = service.TinhMaTotp(biMat, thoiDiem);

        Assert.Matches("^[0-9]{8}$", otp);
        Assert.True(service.XacThucMaTotp(service.GiaiMaAes(biMatDaLuu, khoaAes), otp, thoiDiem));
    }

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
