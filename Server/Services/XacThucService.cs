using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Server.Data;
using Server.Entities;
using Server.Models;

namespace Server.Services;

public interface IXacThucService
{
    Task<dynamic> dangNhap(DangNhapDto thongTin);
    Task<dynamic> dangKy(DangKyDto thongTin);
    Task<dynamic> layThongTinCaNhan(int nguoiDungId);
    Task<dynamic> nopHoSoUuTien(HoSoUuTienDto hoSo);
    Task<dynamic> duyetHoSoUuTien(int hoSoId, bool chapNhan, int nguoiDuyetId, string? lyDo);
    Task<dynamic> layDanhSachHoSoChoDuyet();
    Task<dynamic> capNhatThongTin(int userId, CapNhatCaNhanDto thongTin);
    Task<dynamic> layHoSoCuaToi(int userId);
    Task<dynamic> layNhomUuTien();
    Task<TepMinhChungDto> taiLenMinhChung(int nguoiDungId, Microsoft.AspNetCore.Http.IFormFile file);
    Task<(Stream noiDung, string loaiFile, string tenFileGoc)> layTepMinhChung(Guid maTep, int nguoiDungId, string vaiTro);
}

public class XacThucService : IXacThucService
{
    private const string CloudinaryPrefix = "cloudinary:";
    private readonly SmartBusDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly CloudinaryMinhChung? _cloudinary;
    private readonly PasswordHasher<NguoiDung> _hasher = new();

    public XacThucService(SmartBusDbContext context, IConfiguration configuration, CloudinaryMinhChung? cloudinary = null)
    {
        _context = context;
        _configuration = configuration;
        _cloudinary = cloudinary;
    }

    public async Task<dynamic> dangNhap(DangNhapDto thongTin)
    {
        var taiKhoan = thongTin.taiKhoan.Trim();

        var nguoiDung = await _context.NguoiDungs
            .FirstOrDefaultAsync(x => x.email == taiKhoan || x.soDienThoai == taiKhoan || x.cccd == taiKhoan);

        if (nguoiDung == null)
        {
            return null!;
        }

        var legacy = BamMatKhau(thongTin.matKhau);
        var legacyMatch = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(legacy), Encoding.UTF8.GetBytes(nguoiDung.matKhauBam));
        var check = PasswordVerificationResult.Failed;
        if (!legacyMatch)
        {
            try { check = _hasher.VerifyHashedPassword(nguoiDung, nguoiDung.matKhauBam, thongTin.matKhau); }
            catch (FormatException) { return null!; }
        }
        if (!legacyMatch && check == PasswordVerificationResult.Failed) return null!;

        if (!nguoiDung.trangThai)
        {
            return new { biKhoa = true };
        }

        if (legacyMatch || check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            nguoiDung.matKhauBam = _hasher.HashPassword(nguoiDung, thongTin.matKhau);
            await _context.SaveChangesAsync();
        }

        var token = TaoJwtToken(nguoiDung);

        return new
        {
            token,
            nguoiDung = new
            {
                nguoiDung.id,
                nguoiDung.hoTen,
                nguoiDung.email,
                nguoiDung.soDienThoai,
                nguoiDung.cccd,
                nguoiDung.vaiTro
            }
        };
    }

    public async Task<dynamic> dangKy(DangKyDto thongTin)
    {
        var email = string.IsNullOrWhiteSpace(thongTin.email) ? null : thongTin.email.Trim();
        var sdt = string.IsNullOrWhiteSpace(thongTin.soDienThoai) ? null : thongTin.soDienThoai.Trim();
        var cccd = string.IsNullOrWhiteSpace(thongTin.cccd) ? null : thongTin.cccd.Trim();

        if (email == null && sdt == null)
        {
            throw new Exception("Phai cung cap it nhat email hoac so dien thoai");
        }

        if (email != null && await _context.NguoiDungs.AnyAsync(x => x.email == email))
        {
            throw new Exception("Email da duoc su dung tren he thong");
        }

        if (sdt != null && await _context.NguoiDungs.AnyAsync(x => x.soDienThoai == sdt))
        {
            throw new Exception("So dien thoai da duoc su dung tren he thong");
        }

        if (cccd != null && await _context.NguoiDungs.AnyAsync(x => x.cccd == cccd))
        {
            throw new Exception("So CCCD da duoc su dung tren he thong");
        }

        var nguoiDung = new NguoiDung
        {
            hoTen = thongTin.hoTen.Trim(),
            email = email,
            soDienThoai = sdt,
            cccd = cccd,
            matKhauBam = "",
            vaiTro = "HanhKhach",
            trangThai = true,
            ngayTao = DateTime.UtcNow,
            ngayCapNhat = DateTime.UtcNow
        };

        if (string.IsNullOrWhiteSpace(thongTin.hoTen) || thongTin.hoTen.Trim().Length > 150 || thongTin.matKhau.Length < 8)
            throw new ArgumentException("Ho ten hop le va mat khau tu 8 ky tu la bat buoc.");
        KiemTraThongTin(email, sdt, cccd);
        nguoiDung.matKhauBam = _hasher.HashPassword(nguoiDung, thongTin.matKhau);

        await _context.NguoiDungs.AddAsync(nguoiDung);
        await _context.SaveChangesAsync();

        var token = TaoJwtToken(nguoiDung);

        return new
        {
            token,
            id = nguoiDung.id,
            hoTen = nguoiDung.hoTen,
            vaiTro = nguoiDung.vaiTro
        };
    }

    public async Task<dynamic> layThongTinCaNhan(int nguoiDungId)
    {
        var nguoiDung = await _context.NguoiDungs
            .AsNoTracking()
            .Where(x => x.id == nguoiDungId)
            .Select(x => new
            {
                x.id,
                x.hoTen,
                x.email,
                x.soDienThoai,
                x.cccd,
                x.vaiTro,
                x.trangThai,
                x.ngayTao
            })
            .FirstOrDefaultAsync();

        if (nguoiDung == null) return null!;

        var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
        var hoSoUuTien = await _context.HoSoUuTiens
            .AsNoTracking()
            .Where(x => x.nguoiDungId == nguoiDungId && x.trangThai == "DaDuyet" && x.hieuLucTu <= homNay && x.hieuLucDen >= homNay)
            .Include(x => x.nhomDoiTuong)
            .OrderByDescending(x => x.id)
            .Select(x => new
            {
                x.id,
                x.nhomDoiTuongId,
                tenNhom = x.nhomDoiTuong.tenNhom,
                maNhom = x.nhomDoiTuong.maNhom,
                hieuLucDen = x.hieuLucDen
            })
            .FirstOrDefaultAsync();

        return new
        {
            thongTin = nguoiDung,
            nhomUuTien = hoSoUuTien
        };
    }

    private string LayThuMucLuuTru()
    {
        var dir = _configuration["Storage:MinhChungPath"] ?? "App_Data/MinhChung";
        if (!Path.IsPathRooted(dir))
        {
            dir = Path.Combine(AppContext.BaseDirectory, dir);
        }
        return dir;
    }

    public async Task<TepMinhChungDto> taiLenMinhChung(int nguoiDungId, Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Vui long chon tep de tai len.");

        const long maxFileSize = 5 * 1024 * 1024; // 5 MiB
        if (file.Length > maxFileSize)
            throw new ArgumentException("Kich thuoc tep khong duoc vuot qua 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".pdf")
            throw new ArgumentException("Dinh dang tep khong duoc ho tro. Chi chap nhan JPG, PNG, PDF.");

        var countChuaGan = await _context.TepMinhChungs.CountAsync(x => x.nguoiDungId == nguoiDungId && !x.daGanHoSo && x.ngayTao >= DateTime.UtcNow.AddHours(-24));
        if (countChuaGan >= 20)
            throw new InvalidOperationException("Ban da tai len qua nhieu tep chua xu ly. Vui long thu lai sau.");

        byte[] header = new byte[8];
        using (var stream = file.OpenReadStream())
        {
            var bytesRead = await stream.ReadAsync(header, 0, header.Length);
            if (bytesRead < 4)
                throw new ArgumentException("Tep khong hop le hoac bi hong.");
        }

        string mimeType;
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            if (ext != ".jpg" && ext != ".jpeg")
                throw new ArgumentException("Noi dung tep khong khop voi phan mo rong JPEG.");
            mimeType = "image/jpeg";
        }
        else if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            if (ext != ".png")
                throw new ArgumentException("Noi dung tep khong khop voi phan mo rong PNG.");
            mimeType = "image/png";
        }
        else if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46) // %PDF
        {
            if (ext != ".pdf")
                throw new ArgumentException("Noi dung tep khong khop voi phan mo rong PDF.");
            mimeType = "application/pdf";
        }
        else
        {
            throw new ArgumentException("Dinh dang noi dung tep khong hop le. Chi chap nhan JPG, PNG, PDF.");
        }

        var maTep = Guid.NewGuid();
        var tenFileLuu = $"{maTep:N}{ext}";
        var cloudinary = _cloudinary is { Enabled: true } ? _cloudinary : null;
        var publicId = $"smartbus/minh-chung/{tenFileLuu}";

        if (cloudinary != null)
        {
            tenFileLuu = CloudinaryPrefix + await cloudinary.UploadAsync(file, publicId, mimeType);
        }
        else
        {
            var dir = LayThuMucLuuTru();
            Directory.CreateDirectory(dir);
            await using var fileStream = new FileStream(Path.Combine(dir, tenFileLuu), FileMode.CreateNew);
            await file.CopyToAsync(fileStream);
        }

        var tep = new TepMinhChung
        {
            maTep = maTep,
            nguoiDungId = nguoiDungId,
            tenFileGoc = Path.GetFileName(file.FileName),
            tenFileLuu = tenFileLuu,
            loaiFile = mimeType,
            kichThuoc = file.Length,
            daGanHoSo = false,
            ngayTao = DateTime.UtcNow
        };

        await _context.TepMinhChungs.AddAsync(tep);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (cloudinary != null)
        {
            await cloudinary.TryDeleteAsync(publicId);
            throw;
        }

        return new TepMinhChungDto
        {
            maTep = tep.maTep,
            tenFileGoc = tep.tenFileGoc,
            duongDan = $"/api/XacThuc/minh-chung/{tep.maTep}",
            loaiFile = tep.loaiFile,
            kichThuoc = tep.kichThuoc
        };
    }

    public async Task<(Stream noiDung, string loaiFile, string tenFileGoc)> layTepMinhChung(Guid maTep, int nguoiDungId, string vaiTro)
    {
        var tep = await _context.TepMinhChungs.FirstOrDefaultAsync(x => x.maTep == maTep);
        if (tep == null)
            throw new KeyNotFoundException("Khong tim thay tep minh chung.");

        if (vaiTro != "QuanTriVien" && tep.nguoiDungId != nguoiDungId)
            throw new UnauthorizedAccessException("Ban khong co quyen truy cap tep minh chung nay.");

        if (tep.tenFileLuu.StartsWith(CloudinaryPrefix, StringComparison.Ordinal))
        {
            if (_cloudinary == null)
                throw new IOException("Chưa cấu hình dịch vụ lưu tệp minh chứng.");
            var noiDung = await _cloudinary.DownloadAsync(tep.tenFileLuu[CloudinaryPrefix.Length..]);
            return (noiDung, tep.loaiFile, tep.tenFileGoc);
        }

        var dir = LayThuMucLuuTru();
        var fullPath = Path.GetFullPath(Path.Combine(dir, tep.tenFileLuu));
        var baseDir = Path.GetFullPath(dir);

        if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            throw new System.Security.SecurityException("Duong dan tep khong hop le.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Tep tin khong ton tai tren he thong.");

        return (File.OpenRead(fullPath), tep.loaiFile, tep.tenFileGoc);
    }

    public async Task<dynamic> nopHoSoUuTien(HoSoUuTienDto hoSo)
    {
        var nguoiDung = await _context.NguoiDungs.FindAsync(hoSo.nguoiDungId);
        if (nguoiDung == null) throw new Exception("Khong tim thay nguoi dung");
        if (!await _context.NhomDoiTuongs.AnyAsync(x => x.id == hoSo.nhomDoiTuongId && x.trangThai && x.canXacMinh)) throw new ArgumentException("Nhom uu tien khong hop le.");

        var duongDan = hoSo.duongDanMinhChung?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(duongDan) || duongDan.Length > 500)
            throw new ArgumentException("Can cung cap duong dan minh chung hop le.");

        bool isWebUrl = Uri.TryCreate(duongDan, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        if (!isWebUrl)
        {
            Guid maTep;
            const string matchPrefix = "/api/XacThuc/minh-chung/";
            if (duongDan.StartsWith(matchPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var guidPart = duongDan.Substring(matchPrefix.Length).TrimEnd('/');
                if (!Guid.TryParse(guidPart, out maTep))
                    throw new ArgumentException("Duong dan tep minh chung khong hop le.");
            }
            else if (!Guid.TryParse(duongDan, out maTep))
            {
                throw new ArgumentException("Can cung cap duong dan minh chung HTTP(S) hop le hoac ma tep da tai len.");
            }

            var tep = await _context.TepMinhChungs.FirstOrDefaultAsync(x => x.maTep == maTep);
            if (tep == null)
                throw new ArgumentException("Khong tim thay tep minh chung da tai len.");
            if (tep.nguoiDungId != hoSo.nguoiDungId)
                throw new UnauthorizedAccessException("Tep minh chung khong thuoc ve tai khoan cua ban.");

            tep.daGanHoSo = true;
            duongDan = $"/api/XacThuc/minh-chung/{maTep}";
        }

        if (hoSo.hieuLucDen.Date < hoSo.hieuLucTu.Date || hoSo.hieuLucDen.Date < DateTime.UtcNow.Date) throw new ArgumentException("Thoi han ho so khong hop le.");
        if (await _context.HoSoUuTiens.AnyAsync(x => x.nguoiDungId == hoSo.nguoiDungId && x.nhomDoiTuongId == hoSo.nhomDoiTuongId && x.trangThai == "ChoDuyet")) throw new InvalidOperationException("Da co ho so dang cho duyet.");

        var hoSoMoi = new HoSoUuTien
        {
            nguoiDungId = hoSo.nguoiDungId,
            nhomDoiTuongId = hoSo.nhomDoiTuongId,
            duongDanMinhChung = duongDan,
            trangThai = "ChoDuyet",
            hieuLucTu = DateOnly.FromDateTime(hoSo.hieuLucTu),
            hieuLucDen = DateOnly.FromDateTime(hoSo.hieuLucDen),
            ngayTao = DateTime.UtcNow
        };

        await _context.HoSoUuTiens.AddAsync(hoSoMoi);
        await _context.SaveChangesAsync();

        return new { id = hoSoMoi.id, thongBao = "Nop ho so uu tien thanh cong, dang cho xet duyet" };
    }

    private static void KiemTraThongTin(string? email, string? sdt, string? cccd)
    {
        if (email == null && sdt == null) throw new ArgumentException("Can email hoac so dien thoai.");
        if (email != null && (email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)) throw new ArgumentException("Email khong hop le.");
        if (sdt != null && (sdt.Length < 9 || sdt.Length > 15 || !sdt.All(char.IsAsciiDigit))) throw new ArgumentException("So dien thoai khong hop le.");
        if (cccd != null && (cccd.Length != 12 || !cccd.All(char.IsAsciiDigit))) throw new ArgumentException("CCCD phai gom 12 chu so.");
    }

    public async Task<dynamic> capNhatThongTin(int userId, CapNhatCaNhanDto dto)
    {
        var user = await _context.NguoiDungs.FindAsync(userId) ?? throw new ArgumentException("Khong tim thay tai khoan.");
        var name = dto.hoTen?.Trim() ?? "";
        var email = string.IsNullOrWhiteSpace(dto.email) ? null : dto.email.Trim();
        var phone = string.IsNullOrWhiteSpace(dto.soDienThoai) ? null : dto.soDienThoai.Trim();
        var cccd = string.IsNullOrWhiteSpace(dto.cccd) ? null : dto.cccd.Trim();
        if (name.Length == 0 || name.Length > 150) throw new ArgumentException("Ho ten khong hop le.");
        KiemTraThongTin(email, phone, cccd);
        if (await _context.NguoiDungs.AnyAsync(x => x.id != userId && ((email != null && x.email == email) || (phone != null && x.soDienThoai == phone) || (cccd != null && x.cccd == cccd)))) throw new ArgumentException("Email, so dien thoai hoac CCCD da duoc su dung.");
        user.hoTen = name; user.email = email; user.soDienThoai = phone; user.cccd = cccd; user.ngayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new { user.id, user.hoTen, user.email, user.soDienThoai, user.cccd, user.vaiTro };
    }

    public async Task<dynamic> layHoSoCuaToi(int userId) => await _context.HoSoUuTiens.AsNoTracking().Where(x => x.nguoiDungId == userId)
        .OrderByDescending(x => x.id).Select(x => new { x.id, x.nhomDoiTuongId, tenNhom = x.nhomDoiTuong.tenNhom, x.duongDanMinhChung, x.trangThai, x.hieuLucTu, x.hieuLucDen, x.lyDo, x.ngayTao }).ToListAsync();

    public async Task<dynamic> layNhomUuTien() => await _context.NhomDoiTuongs.AsNoTracking().Where(x => x.trangThai && x.canXacMinh)
        .Select(x => new { x.id, x.tenNhom }).ToListAsync();

    public async Task<dynamic> duyetHoSoUuTien(int hoSoId, bool chapNhan, int nguoiDuyetId, string? lyDo)
    {
        var hoSo = await _context.HoSoUuTiens.FirstOrDefaultAsync(x => x.id == hoSoId);
        if (hoSo == null) return false;

        hoSo.trangThai = chapNhan ? "DaDuyet" : "TuChoi";
        hoSo.nguoiDuyetId = nguoiDuyetId;
        hoSo.ngayDuyet = DateTime.UtcNow;
        hoSo.lyDo = lyDo;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<dynamic> layDanhSachHoSoChoDuyet()
    {
        var danhSach = await _context.HoSoUuTiens
            .AsNoTracking()
            .Where(x => x.trangThai == "ChoDuyet")
            .Include(x => x.nguoiDung)
            .Include(x => x.nhomDoiTuong)
            .Select(x => new
            {
                x.id,
                x.nguoiDungId,
                hoTenNguoiDung = x.nguoiDung.hoTen,
                soDienThoai = x.nguoiDung.soDienThoai,
                email = x.nguoiDung.email,
                cccd = x.nguoiDung.cccd,
                tenNhom = x.nhomDoiTuong.tenNhom,
                x.duongDanMinhChung,
                x.hieuLucTu,
                x.hieuLucDen,
                x.ngayTao
            })
            .ToListAsync();

        return danhSach;
    }

    private string TaoJwtToken(NguoiDung nguoiDung)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("Missing JWT secret");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("id", nguoiDung.id.ToString()),
            new Claim("hoTen", nguoiDung.hoTen),
            new Claim(ClaimTypes.Role, nguoiDung.vaiTro),
            new Claim("vaiTro", nguoiDung.vaiTro)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"] ?? "SmartBusServer",
            audience: _configuration["JwtSettings:Audience"] ?? "SmartBusClient",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string BamMatKhau(string matKhau)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(matKhau + "SmartBusSalt2026");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
