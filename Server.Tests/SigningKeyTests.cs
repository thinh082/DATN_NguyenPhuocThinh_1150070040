using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Server.Services;

namespace Server.Tests;

public class SigningKeyTests
{
    [Fact]
    public void PrivateKeyMustUseP256Curve()
    {
        using var key = ECDsa.Create(ECCurve.CreateFromFriendlyName("secP256k1"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:SigningPrivateKey"] = key.ExportPkcs8PrivateKeyPem()
        }).Build();
        var service = new SigningKeyService(config);
        Assert.Throws<InvalidOperationException>(() => service.Open(key.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void PrivateKeyMustMatchRegisteredPublicKey()
    {
        using var expected = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:SigningPrivateKey"] = expected.ExportPkcs8PrivateKeyPem()
        }).Build();
        var service = new SigningKeyService(config);
        using var actual = service.Open(expected.ExportSubjectPublicKeyInfoPem());
        var data = new byte[] { 1, 2, 3 };
        Assert.True(expected.VerifyData(data, actual.SignData(data, HashAlgorithmName.SHA256), HashAlgorithmName.SHA256));
        Assert.Throws<InvalidOperationException>(() => service.Open(other.ExportSubjectPublicKeyInfoPem()));
    }
}
