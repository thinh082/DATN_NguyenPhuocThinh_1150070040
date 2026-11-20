using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Data;
using Server.Entities;

var serverProject = Path.GetFullPath("Server/Server.csproj");
if (!File.Exists(serverProject)) throw new InvalidOperationException("Run from the Project directory.");

var config = new ConfigurationBuilder().SetBasePath(Path.GetFullPath("Server"))
    .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets(typeof(SmartBusDbContext).Assembly, optional: true).AddEnvironmentVariables().Build();

async Task SetUserSecretAsync(string key, string value)
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var arg in new[] { "user-secrets", "set", key, value, "--project", serverProject }) start.ArgumentList.Add(arg);
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start dotnet user-secrets.");
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    if (process.ExitCode != 0) throw new InvalidOperationException($"Cannot store {key}: {error}");
    Console.WriteLine($"[KeySetup] Saved {key} to user-secrets.");
}

// 1. JWT Secret Key
var jwtSecret = config["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    byte[] keyBytes = new byte[32];
    RandomNumberGenerator.Fill(keyBytes);
    jwtSecret = Convert.ToBase64String(keyBytes);
    await SetUserSecretAsync("JwtSettings:SecretKey", jwtSecret);
}
else
{
    Console.WriteLine("[KeySetup] JwtSettings:SecretKey already configured.");
}

// 2. TOTP AES-256 Encryption Key (32 bytes Base64)
var totpKey = config["Security:TotpEncryptionKey"];
if (string.IsNullOrWhiteSpace(totpKey))
{
    byte[] aesBytes = new byte[32];
    RandomNumberGenerator.Fill(aesBytes);
    totpKey = Convert.ToBase64String(aesBytes);
    await SetUserSecretAsync("Security:TotpEncryptionKey", totpKey);
}
else
{
    Console.WriteLine("[KeySetup] Security:TotpEncryptionKey already configured.");
}

// 3. ECDSA P-256 Signing Private Key
var privatePem = config["Security:SigningPrivateKey"];
if (string.IsNullOrWhiteSpace(privatePem))
{
    using var generated = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    privatePem = generated.ExportPkcs8PrivateKeyPem();
    await SetUserSecretAsync("Security:SigningPrivateKey", privatePem);
}
else
{
    Console.WriteLine("[KeySetup] Security:SigningPrivateKey already configured.");
}

using var signing = ECDsa.Create();
signing.ImportFromPem(privatePem);
if (signing.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7")
    throw new InvalidOperationException("Signing key must use P-256.");

var publicBytes = signing.ExportSubjectPublicKeyInfo();
var options = new DbContextOptionsBuilder<SmartBusDbContext>()
    .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
await using var db = new SmartBusDbContext(options);

// Đảm bảo schema tồn tại
await DuLieuKhoiTao.KhoiTaoDuLieuMau(db);

await using var transaction = await db.Database.BeginTransactionAsync();
var keys = await db.KhoaKySos.ToListAsync();
var match = keys.FirstOrDefault(k =>
{
    try
    {
        using var existing = ECDsa.Create();
        existing.ImportFromPem(k.khoaCongKhaiPem);
        return existing.ExportSubjectPublicKeyInfo().SequenceEqual(publicBytes);
    }
    catch (CryptographicException)
    {
        return false;
    }
});

foreach (var key in keys.Where(k => k.trangThai == "DangDung" && k != match))
{
    key.trangThai = "NgungKy";
}

if (match == null)
{
    match = new KhoaKySo
    {
        maKhoa = "DEMO-" + Guid.NewGuid().ToString("N"),
        thuatToan = "ECDSA_P256_SHA256",
        khoaCongKhaiPem = signing.ExportSubjectPublicKeyInfoPem(),
        thamChieuKhoaBiMat = "user-secrets:Security:SigningPrivateKey",
        hieuLucTu = DateTime.UtcNow.AddMinutes(-1),
        hetHanLuc = DateTime.UtcNow.AddYears(5),
        trangThai = "DangDung"
    };
    db.KhoaKySos.Add(match);
}
else
{
    match.trangThai = "DangDung";
}

await db.SaveChangesAsync();
await transaction.CommitAsync();
Console.WriteLine("[KeySetup] Signing public key registered in database: " + match.maKhoa);
