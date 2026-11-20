using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Server.Entities;
using Server.Services;

namespace Server.Tests;

public class OfflineQrValidationTests
{
    [Fact]
    public void CheckRejectsForgedExpiredAndWrongRoute()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var aesKey = RandomNumberGenerator.GetBytes(32);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Security:TotpEncryptionKey"] = Convert.ToBase64String(aesKey) }).Build();
        var crypto = new MatMaService(config);
        var now = DateTime.UtcNow;
        var secret = RandomNumberGenerator.GetBytes(32);
        var data = Encoding.UTF8.GetBytes("signed ticket data");
        var id = Guid.NewGuid();
        var ticket = new Ve { maVe = id, tuyenXeId = 1, hieuLucTu = now.AddHours(-1), hetHanLuc = now.AddHours(1), chuKyTotpGiay = 30, soChuSoTotp = 8,
            duLieuDaKy = data, biMatTotpMaHoa = crypto.MaHoaAes(secret), khoaKySo = new KhoaKySo { khoaCongKhaiPem = key.ExportSubjectPublicKeyInfoPem() } };
        var signature = key.SignData(data, HashAlgorithmName.SHA256);
        var otp = crypto.TinhMaTotp(secret, now);
        var qr = $"SMARTBUS:v1:{id}:{otp}:{Convert.ToBase64String(signature)}";
        var checker = new OfflineQrValidationService(crypto);
        Assert.True(checker.Check(ticket, qr, now, 1).Valid);
        Assert.Equal("SAI_TUYEN", checker.Check(ticket, qr, now, 2).Reason);
        Assert.Equal("VE_HET_HAN", checker.Check(ticket, qr, now.AddDays(2), 1).Reason);
        signature[0] ^= 1;
        Assert.Equal("SAI_CHU_KY", checker.Check(ticket, $"SMARTBUS:v1:{id}:{otp}:{Convert.ToBase64String(signature)}", now, 1).Reason);
    }
}
