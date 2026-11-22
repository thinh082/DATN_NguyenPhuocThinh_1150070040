using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using Server.Models;
using Microsoft.AspNetCore.Identity;

namespace Server.Services;

public interface INguoiDungService
{
    Task<dynamic> layDanhSachNguoiDung();
    Task<dynamic> layChiTietNguoiDung(int id);
    Task<dynamic> luuNguoiDung(NguoiDungDto thongTin);
    Task<dynamic> capNhatTrangThai(int id, bool trangThai);
}

public class NguoiDungService : INguoiDungService
{
    private readonly SmartBusDbContext _context;

    public NguoiDungService(SmartBusDbContext context)
    {
        _context = context;
    }

    public async Task<dynamic> layDanhSachNguoiDung()
    {
        var danhSach = await _context.NguoiDungs
            .AsNoTracking()
            .Select(x => new
            {
                x.id,
                x.hoTen,
                x.soDienThoai,
                x.email,
                x.cccd,
                x.vaiTro,
                x.trangThai,
                x.ngayTao
            })
            .ToListAsync();

        return danhSach;
    }

    public async Task<dynamic> layChiTietNguoiDung(int id)
    {
        var nguoiDung = await _context.NguoiDungs
            .AsNoTracking()
            .Where(x => x.id == id)
            .Select(x => new
            {
                x.id,
                x.hoTen,
                x.soDienThoai,
                x.email,
                x.cccd,
                x.vaiTro,
                x.trangThai,
                x.ngayTao,
                x.ngayCapNhat
            })
            .FirstOrDefaultAsync();

        return nguoiDung!;
    }

    public async Task<dynamic> luuNguoiDung(NguoiDungDto thongTin)
    {
        if (thongTin.id == 0)
        {
            if (string.IsNullOrWhiteSpace(thongTin.hoTen) || string.IsNullOrWhiteSpace(thongTin.matKhau) || thongTin.matKhau.Length < 8)
                throw new ArgumentException("Cần họ tên và mật khẩu ít nhất 8 ký tự.");
            if (thongTin.vaiTro != "NhanVienSoatVe" && thongTin.vaiTro != "HanhKhach") throw new ArgumentException("Vai trò không hợp lệ.");
            var nguoiDungMoi = new NguoiDung
            {
                hoTen = thongTin.hoTen,
                soDienThoai = string.IsNullOrWhiteSpace(thongTin.soDienThoai) ? null : thongTin.soDienThoai.Trim(),
                email = string.IsNullOrWhiteSpace(thongTin.email) ? null : thongTin.email.Trim(),
                cccd = string.IsNullOrWhiteSpace(thongTin.cccd) ? null : thongTin.cccd.Trim(),
                matKhauBam = "",
                vaiTro = string.IsNullOrWhiteSpace(thongTin.vaiTro) ? "HanhKhach" : thongTin.vaiTro,
                trangThai = thongTin.trangThai,
                ngayTao = DateTime.UtcNow,
                ngayCapNhat = DateTime.UtcNow
            };

            nguoiDungMoi.matKhauBam = new PasswordHasher<NguoiDung>().HashPassword(nguoiDungMoi, thongTin.matKhau);

            await _context.NguoiDungs.AddAsync(nguoiDungMoi);
            await _context.SaveChangesAsync();

            return new { id = nguoiDungMoi.id, thongBao = "Thêm mới người dùng thành công" };
        }
        else
        {
            var tonTai = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.id == thongTin.id);
            if (tonTai == null)
            {
                return null!;
            }

            tonTai.hoTen = thongTin.hoTen;
            tonTai.soDienThoai = string.IsNullOrWhiteSpace(thongTin.soDienThoai) ? null : thongTin.soDienThoai.Trim();
            tonTai.email = string.IsNullOrWhiteSpace(thongTin.email) ? null : thongTin.email.Trim();
            tonTai.cccd = string.IsNullOrWhiteSpace(thongTin.cccd) ? null : thongTin.cccd.Trim();
            tonTai.vaiTro = string.IsNullOrWhiteSpace(thongTin.vaiTro) ? tonTai.vaiTro : thongTin.vaiTro;
            tonTai.trangThai = thongTin.trangThai;
            tonTai.ngayCapNhat = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new { id = tonTai.id, thongBao = "Cập nhật người dùng thành công" };
        }
    }

    public async Task<dynamic> capNhatTrangThai(int id, bool trangThai)
    {
        var tonTai = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.id == id);
        if (tonTai == null) return false;

        tonTai.trangThai = trangThai;
        tonTai.ngayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}
