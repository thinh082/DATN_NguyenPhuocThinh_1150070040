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
    Task<dynamic> layTinhThanh();
    Task<dynamic> layPhuongXa(int maTinhThanh);
    Task<dynamic> xacThucDinhDanh(int userId, XacThucDinhDanhDto dto);
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
            throw new Exception("Phải cung cấp ít nhất email hoặc số điện thoại");
        }

        if (email != null && await _context.NguoiDungs.AnyAsync(x => x.email == email))
        {
            throw new Exception("Email đã được sử dụng trên hệ thống");
        }

        if (sdt != null && await _context.NguoiDungs.AnyAsync(x => x.soDienThoai == sdt))
        {
            throw new Exception("Số điện thoại đã được sử dụng trên hệ thống");
        }

        if (cccd != null && await _context.NguoiDungs.AnyAsync(x => x.cccd == cccd))
        {
            throw new Exception("Số CCCD đã được sử dụng trên hệ thống");
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
            throw new ArgumentException("Cần họ tên hợp lệ và mật khẩu từ 8 ký tự.");
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
                x.diaChi,
                x.maTinhThanh,
                tenTinhThanh = x.tinhThanh != null ? x.tinhThanh.ten : null,
                x.maPhuongXa,
                tenPhuongXa = x.phuongXa != null ? x.phuongXa.ten : null,
                x.phuongThucXacThuc,
                x.ngayXacThuc,
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
            throw new ArgumentException("Vui lòng chọn tệp để tải lên.");

        const long maxFileSize = 5 * 1024 * 1024; // 5 MiB
        if (file.Length > maxFileSize)
            throw new ArgumentException("Kích thước tệp không được vượt quá 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".pdf")
            throw new ArgumentException("Định dạng tệp không được hỗ trợ. Chỉ chấp nhận JPG, PNG, PDF.");

        var countChuaGan = await _context.TepMinhChungs.CountAsync(x => x.nguoiDungId == nguoiDungId && !x.daGanHoSo && x.ngayTao >= DateTime.UtcNow.AddHours(-24));
        if (countChuaGan >= 20)
            throw new InvalidOperationException("Bạn đã tải lên quá nhiều tệp chưa xử lý. Vui lòng thử lại sau.");

        byte[] header = new byte[8];
        using (var stream = file.OpenReadStream())
        {
            var bytesRead = await stream.ReadAsync(header, 0, header.Length);
            if (bytesRead < 4)
                throw new ArgumentException("Tệp không hợp lệ hoặc bị hỏng.");
        }

        string mimeType;
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            if (ext != ".jpg" && ext != ".jpeg")
                throw new ArgumentException("Nội dung tệp không khớp với phần mở rộng JPEG.");
            mimeType = "image/jpeg";
        }
        else if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            if (ext != ".png")
                throw new ArgumentException("Nội dung tệp không khớp với phần mở rộng PNG.");
            mimeType = "image/png";
        }
        else if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46) // %PDF
        {
            if (ext != ".pdf")
                throw new ArgumentException("Nội dung tệp không khớp với phần mở rộng PDF.");
            mimeType = "application/pdf";
        }
        else
        {
            throw new ArgumentException("Định dạng nội dung tệp không hợp lệ. Chỉ chấp nhận JPG, PNG, PDF.");
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
            throw new KeyNotFoundException("Không tìm thấy tệp minh chứng.");

        if (vaiTro != "QuanTriVien" && tep.nguoiDungId != nguoiDungId)
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập tệp minh chứng này.");

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
            throw new System.Security.SecurityException("Đường dẫn tệp không hợp lệ.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Tệp tin không tồn tại trên hệ thống.");

        return (File.OpenRead(fullPath), tep.loaiFile, tep.tenFileGoc);
    }

    public async Task<dynamic> nopHoSoUuTien(HoSoUuTienDto hoSo)
    {
        var nguoiDung = await _context.NguoiDungs.FindAsync(hoSo.nguoiDungId);
        if (nguoiDung == null) throw new Exception("Không tìm thấy người dùng");
        if (!await _context.NhomDoiTuongs.AnyAsync(x => x.id == hoSo.nhomDoiTuongId && x.trangThai && x.canXacMinh)) throw new ArgumentException("Nhóm ưu tiên không hợp lệ.");

        var duongDan = await ChuanHoaMinhChung(hoSo.duongDanMinhChung, hoSo.nguoiDungId, true);

        if (hoSo.hieuLucDen.Date < hoSo.hieuLucTu.Date || hoSo.hieuLucDen.Date < DateTime.UtcNow.Date) throw new ArgumentException("Thời hạn hồ sơ không hợp lệ.");
        if (await _context.HoSoUuTiens.AnyAsync(x => x.nguoiDungId == hoSo.nguoiDungId && x.nhomDoiTuongId == hoSo.nhomDoiTuongId && x.trangThai == "ChoDuyet")) throw new InvalidOperationException("Bạn đã có hồ sơ đang chờ duyệt cho nhóm này.");

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

        return new { id = hoSoMoi.id, thongBao = "Nộp hồ sơ ưu tiên thành công, đang chờ xét duyệt" };
    }

    private async Task<string> ChuanHoaMinhChung(string? duongDanGoc, int nguoiDungId, bool choPhepUrl)
    {
        var duongDan = duongDanGoc?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(duongDan) || duongDan.Length > 500)
            throw new ArgumentException("Cần cung cấp tài liệu minh chứng hợp lệ.");

        bool isWebUrl = Uri.TryCreate(duongDan, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        if (isWebUrl && choPhepUrl) return duongDan;

        Guid maTep;
        const string matchPrefix = "/api/XacThuc/minh-chung/";
        if (duongDan.StartsWith(matchPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var guidPart = duongDan.Substring(matchPrefix.Length).TrimEnd('/');
            if (!Guid.TryParse(guidPart, out maTep))
                throw new ArgumentException("Đường dẫn tệp minh chứng không hợp lệ.");
        }
        else if (!Guid.TryParse(duongDan, out maTep))
        {
            throw new ArgumentException(choPhepUrl
                ? "Cần cung cấp đường dẫn minh chứng HTTP(S) hợp lệ hoặc tệp đã tải lên."
                : "Cần tải lên ảnh minh chứng trước khi xác thực.");
        }

        var tep = await _context.TepMinhChungs.FirstOrDefaultAsync(x => x.maTep == maTep);
        if (tep == null)
            throw new ArgumentException("Không tìm thấy tệp minh chứng đã tải lên.");
        if (tep.nguoiDungId != nguoiDungId)
            throw new UnauthorizedAccessException("Tệp minh chứng không thuộc về tài khoản của bạn.");

        tep.daGanHoSo = true;
        return $"/api/XacThuc/minh-chung/{maTep}";
    }

    private static readonly string[] PhuongThucXacThucHopLe = ["CCCD", "VNeID", "NganHang", "ViDienTu"];

    public async Task<dynamic> xacThucDinhDanh(int userId, XacThucDinhDanhDto dto)
    {
        var user = await _context.NguoiDungs.FindAsync(userId) ?? throw new ArgumentException("Không tìm thấy tài khoản.");
        var phuongThuc = dto.phuongThuc?.Trim() ?? "";
        if (!PhuongThucXacThucHopLe.Contains(phuongThuc)) throw new ArgumentException("Phương thức xác thực không hợp lệ.");

        string? minhChung = null;
        if (phuongThuc == "CCCD")
        {
            if (string.IsNullOrEmpty(user.cccd)) throw new ArgumentException("Vui lòng cập nhật số CCCD trong thông tin hành chính trước khi xác thực.");
            minhChung = await ChuanHoaMinhChung(dto.duongDanMinhChung, userId, false);
        }

        user.phuongThucXacThuc = phuongThuc;
        user.ngayXacThuc = DateTime.UtcNow;
        user.minhChungXacThuc = minhChung;
        user.ngayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new { user.phuongThucXacThuc, user.ngayXacThuc };
    }

    public async Task<dynamic> layTinhThanh() => await _context.TinhThanhs.AsNoTracking()
        .OrderBy(x => x.ten).Select(x => new { x.ma, x.ten }).ToListAsync();

    public async Task<dynamic> layPhuongXa(int maTinhThanh) => await _context.PhuongXas.AsNoTracking()
        .Where(x => x.maTinhThanh == maTinhThanh).OrderBy(x => x.ten).Select(x => new { x.ma, x.ten }).ToListAsync();

    private static void KiemTraThongTin(string? email, string? sdt, string? cccd)
    {
        if (email == null && sdt == null) throw new ArgumentException("Cần email hoặc số điện thoại.");
        if (email != null && (email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)) throw new ArgumentException("Email không hợp lệ.");
        if (sdt != null && (sdt.Length < 9 || sdt.Length > 15 || !sdt.All(char.IsAsciiDigit))) throw new ArgumentException("Số điện thoại không hợp lệ.");
        if (cccd != null && (cccd.Length != 12 || !cccd.All(char.IsAsciiDigit))) throw new ArgumentException("CCCD phải gồm 12 chữ số.");
    }

    public async Task<dynamic> capNhatThongTin(int userId, CapNhatCaNhanDto dto)
    {
        var user = await _context.NguoiDungs.FindAsync(userId) ?? throw new ArgumentException("Không tìm thấy tài khoản.");
        var name = dto.hoTen?.Trim() ?? "";
        var email = string.IsNullOrWhiteSpace(dto.email) ? null : dto.email.Trim();
        var phone = string.IsNullOrWhiteSpace(dto.soDienThoai) ? null : dto.soDienThoai.Trim();
        var cccd = string.IsNullOrWhiteSpace(dto.cccd) ? null : dto.cccd.Trim();
        if (name.Length == 0 || name.Length > 150) throw new ArgumentException("Họ tên không hợp lệ.");
        KiemTraThongTin(email, phone, cccd);
        if (await _context.NguoiDungs.AnyAsync(x => x.id != userId && ((email != null && x.email == email) || (phone != null && x.soDienThoai == phone) || (cccd != null && x.cccd == cccd)))) throw new ArgumentException("Email, số điện thoại hoặc CCCD đã được sử dụng.");
        var diaChi = string.IsNullOrWhiteSpace(dto.diaChi) ? null : dto.diaChi.Trim();
        if (diaChi != null && diaChi.Length > 1000) throw new ArgumentException("Địa chỉ tối đa 1000 ký tự.");
        if (dto.maTinhThanh.HasValue && !await _context.TinhThanhs.AnyAsync(x => x.ma == dto.maTinhThanh)) throw new ArgumentException("Tỉnh/thành phố không hợp lệ.");
        if (dto.maPhuongXa.HasValue && !await _context.PhuongXas.AnyAsync(x => x.ma == dto.maPhuongXa && x.maTinhThanh == dto.maTinhThanh)) throw new ArgumentException("Phường/xã không thuộc tỉnh/thành phố đã chọn.");
        if (user.phuongThucXacThuc == "CCCD" && user.cccd != cccd) { user.phuongThucXacThuc = null; user.ngayXacThuc = null; user.minhChungXacThuc = null; }
        user.hoTen = name; user.email = email; user.soDienThoai = phone; user.cccd = cccd;
        user.diaChi = diaChi; user.maTinhThanh = dto.maTinhThanh; user.maPhuongXa = dto.maTinhThanh.HasValue ? dto.maPhuongXa : null;
        user.ngayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new { user.id, user.hoTen, user.email, user.soDienThoai, user.cccd, user.diaChi, user.maTinhThanh, user.maPhuongXa, user.phuongThucXacThuc, user.ngayXacThuc, user.vaiTro };
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
