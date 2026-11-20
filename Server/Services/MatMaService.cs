using System.Security.Cryptography;
using System.Text;

namespace Server.Services;

public interface IMatMaService
{
    (byte[] duLieuDaKy, byte[] chuKy) KySoDuLieu(string thongTinVe, ECDsa ecdsaPrivate);
    bool XacThucChuKy(byte[] duLieu, byte[] chuKy, string publicKeyPem);
    byte[] SinhBiMatTotp();
    string TinhMaTotp(byte[] biMat, DateTime thoiDiem, int chuKyGiay = 30, int soChuSo = 8);
    bool XacThucMaTotp(byte[] biMat, string maTotp, DateTime thoiDiem, int chuKyGiay = 30, int soChuSo = 8, int dungSai = 1);
    byte[] MaHoaAes(byte[] duLieuGoc, byte[]? khoa = null);
    byte[] GiaiMaAes(byte[] duLieuMaHoa, byte[]? khoa = null);
    long TinhBuocThoiGian(DateTime thoiDiem, int chuKyGiay = 30);
    int TinhSoGiayConLai(DateTime thoiDiem, int chuKyGiay = 30);
}

public class MatMaService : IMatMaService
{
    private readonly IConfiguration? _configuration;

    public MatMaService(IConfiguration? configuration = null) => _configuration = configuration;

    private byte[] LayKhoa(byte[]? khoa)
    {
        var key = khoa ?? Convert.FromBase64String(_configuration?["Security:TotpEncryptionKey"]
            ?? throw new InvalidOperationException("Chua cau hinh Security:TotpEncryptionKey (Base64, 32 byte)."));
        if (key.Length != 32) throw new CryptographicException("Khoa AES-GCM phai co 32 byte.");
        return key;
    }

    public (byte[] duLieuDaKy, byte[] chuKy) KySoDuLieu(string thongTinVe, ECDsa ecdsaPrivate)
    {
        var duLieuBytes = Encoding.UTF8.GetBytes(thongTinVe);
        var chuKyBytes = ecdsaPrivate.SignData(duLieuBytes, HashAlgorithmName.SHA256);
        return (duLieuBytes, chuKyBytes);
    }

    public bool XacThucChuKy(byte[] duLieu, byte[] chuKy, string publicKeyPem)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(publicKeyPem);
            return ecdsa.VerifyData(duLieu, chuKy, HashAlgorithmName.SHA256);
        }
        catch
        {
            return false;
        }
    }

    public byte[] SinhBiMatTotp()
    {
        var biMat = new byte[32];
        RandomNumberGenerator.Fill(biMat);
        return biMat;
    }

    public long TinhBuocThoiGian(DateTime thoiDiem, int chuKyGiay = 30)
    {
        var unixTimestamp = new DateTimeOffset(thoiDiem.ToUniversalTime()).ToUnixTimeSeconds();
        return unixTimestamp / chuKyGiay;
    }

    public int TinhSoGiayConLai(DateTime thoiDiem, int chuKyGiay = 30)
    {
        var unixTimestamp = new DateTimeOffset(thoiDiem.ToUniversalTime()).ToUnixTimeSeconds();
        return (int)(chuKyGiay - (unixTimestamp % chuKyGiay));
    }

    public string TinhMaTotp(byte[] biMat, DateTime thoiDiem, int chuKyGiay = 30, int soChuSo = 8)
    {
        var buoc = TinhBuocThoiGian(thoiDiem, chuKyGiay);
        var buocBytes = BitConverter.GetBytes(buoc);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(buocBytes);
        }

        using var hmac = new HMACSHA256(biMat);
        var hash = hmac.ComputeHash(buocBytes);

        int offset = hash[^1] & 0x0F;
        int binaryCode = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

        int modulo = (int)Math.Pow(10, soChuSo);
        int otp = binaryCode % modulo;
        return otp.ToString().PadLeft(soChuSo, '0');
    }

    public bool XacThucMaTotp(byte[] biMat, string maTotp, DateTime thoiDiem, int chuKyGiay = 30, int soChuSo = 8, int dungSai = 1)
    {
        var buocGoc = TinhBuocThoiGian(thoiDiem, chuKyGiay);

        for (int i = -dungSai; i <= dungSai; i++)
        {
            var buocThu = buocGoc + i;
            var buocBytes = BitConverter.GetBytes(buocThu);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buocBytes);
            }

            using var hmac = new HMACSHA256(biMat);
            var hash = hmac.ComputeHash(buocBytes);

            int offset = hash[^1] & 0x0F;
            int binaryCode = ((hash[offset] & 0x7F) << 24)
                           | ((hash[offset + 1] & 0xFF) << 16)
                           | ((hash[offset + 2] & 0xFF) << 8)
                           | (hash[offset + 3] & 0xFF);

            int modulo = (int)Math.Pow(10, soChuSo);
            string otp = (binaryCode % modulo).ToString().PadLeft(soChuSo, '0');

            if (otp == maTotp)
            {
                return true;
            }
        }

        return false;
    }

    public byte[] MaHoaAes(byte[] duLieuGoc, byte[]? khoa = null)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[duLieuGoc.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(LayKhoa(khoa), 16);
        aes.Encrypt(nonce, duLieuGoc, ciphertext, tag);
        return nonce.Concat(tag).Concat(ciphertext).ToArray();
    }

    public byte[] GiaiMaAes(byte[] duLieuMaHoa, byte[]? khoa = null)
    {
        if (duLieuMaHoa.Length < 28) throw new CryptographicException("Du lieu AES-GCM khong hop le.");
        var plaintext = new byte[duLieuMaHoa.Length - 28];
        using var aes = new AesGcm(LayKhoa(khoa), 16);
        aes.Decrypt(duLieuMaHoa.AsSpan(0, 12), duLieuMaHoa.AsSpan(28), duLieuMaHoa.AsSpan(12, 16), plaintext);
        return plaintext;
    }
}
