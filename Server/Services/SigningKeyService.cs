using System.Security.Cryptography;

namespace Server.Services;

public sealed class SigningKeyService(IConfiguration configuration)
{
    public ECDsa Open(string publicKeyPem)
    {
        var privateKeyPem = configuration["Security:SigningPrivateKey"]
            ?? throw new InvalidOperationException("Chua cau hinh Security:SigningPrivateKey.");
        var key = ECDsa.Create();
        try
        {
            key.ImportFromPem(privateKeyPem);
            using var registered = ECDsa.Create();
            registered.ImportFromPem(publicKeyPem);
            if (key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7" || !CryptographicOperations.FixedTimeEquals(
                key.ExportSubjectPublicKeyInfo(), registered.ExportSubjectPublicKeyInfo()))
                throw new InvalidOperationException("Khoa ky khong khop khoa cong khai da dang ky.");
            return key;
        }
        catch { key.Dispose(); throw; }
    }
}
