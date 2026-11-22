using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using Server.Models;

namespace Server.Services;

public interface IVanHanhService
{
    Task<dynamic> layDanhSachTuyenXe(string? loc = null);
    Task<dynamic> layChiTietTuyenXe(int id);
    Task<dynamic> luuTuyenXe(TuyenXeDto thongTin);
    Task<dynamic> layDanhSachTramDung(int? tuyenXeId);
    Task<dynamic> luuTramDung(TramDungDto thongTin);
    Task<dynamic> layDanhSachXeBuyt();
    Task<dynamic> luuXeBuyt(XeBuytDto thongTin);
    Task<dynamic> layThongTinQrDanXe(Guid maQrTinh);
    Task<dynamic> batDauPhienSoatVe(BatDauPhienDto thongTin);
    Task<dynamic> ketThucPhienSoatVe(Guid phienId);
}

public class VanHanhService : IVanHanhService
{
    private readonly SmartBusDbContext _context;
    private readonly ICacheService? _cache;

    public VanHanhService(SmartBusDbContext context, ICacheService? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<dynamic> layDanhSachTuyenXe(string? loc = null)
    {
        var danhSach = await LayTatCaTuyen();
        return loc switch
        {
            "mienPhi" => danhSach.Where(x => x.mienPhi).ToList(),
            "tinhPhi" => danhSach.Where(x => !x.mienPhi).ToList(),
            _ => danhSach
        };
    }

    private async Task<List<TuyenXeListItemDto>> LayTatCaTuyen()
    {
        var ngay = ChinhSachTuyen.HomNay;
        var cacheKey = $"smartbus:cache:tuyen-xe:tat-ca:{ngay:yyyyMMdd}";
        if (_cache != null)
        {
            var cached = await _cache.GetAsync<List<TuyenXeListItemDto>>(cacheKey);
            if (cached != null) return cached;
        }

        var danhSach = await _context.TuyenXes
            .AsNoTracking()
            .OrderBy(x => x.maTuyen)
            .Select(x => new TuyenXeListItemDto
            {
                id = x.id,
                maTuyen = x.maTuyen,
                tenTuyen = x.tenTuyen,
                gioBatDau = x.gioBatDau.ToString("HH:mm"),
                gioKetThuc = x.gioKetThuc.ToString("HH:mm"),
                gianCachPhut = x.gianCachPhut ?? 0,
                trangThai = x.trangThai,
                soLuongLoTrinh = x.LoTrinhs.Count,
                cuLyKm = x.cuLyKm,
                mienPhi = x.chinhSachMienPhi != null && x.chinhSachMienPhi.trangThai && x.chinhSachMienPhi.hieuLucTu <= ngay && x.chinhSachMienPhi.hieuLucDen >= ngay,
                maChinhSach = x.chinhSachMienPhi != null ? x.chinhSachMienPhi.ma : null,
                tenChinhSach = x.chinhSachMienPhi != null ? x.chinhSachMienPhi.ten : null
            })
            .ToListAsync();

        if (_cache != null)
        {
            await _cache.SetAsync(cacheKey, danhSach, TimeSpan.FromMinutes(5));
        }

        return danhSach;
    }

    public async Task<dynamic> layChiTietTuyenXe(int id)
    {
        var tuyen = await _context.TuyenXes
            .AsNoTracking()
            .Where(x => x.id == id)
            .Include(x => x.chinhSachMienPhi)
            .Include(x => x.LoTrinhs)
                .ThenInclude(lt => lt.LoTrinhTramDungs)
                .ThenInclude(lttd => lttd.tramDung)
            .FirstOrDefaultAsync();

        if (tuyen == null) return null!;
        var now = DateTime.UtcNow;

        return new
        {
            tuyen.id,
            tuyen.maTuyen,
            tuyen.tenTuyen,
            gioBatDau = tuyen.gioBatDau.ToString("HH:mm"),
            gioKetThuc = tuyen.gioKetThuc.ToString("HH:mm"),
            tuyen.gianCachPhut,
            tuyen.trangThai,
            tuyen.cuLyKm,
            mienPhi = tuyen.chinhSachMienPhi != null && tuyen.chinhSachMienPhi.trangThai && tuyen.chinhSachMienPhi.hieuLucTu <= ChinhSachTuyen.HomNay && tuyen.chinhSachMienPhi.hieuLucDen >= ChinhSachTuyen.HomNay,
            maChinhSach = tuyen.chinhSachMienPhi?.ma,
            tenChinhSach = tuyen.chinhSachMienPhi?.ten,
            loTrinhs = tuyen.LoTrinhs.Where(lt => lt.hieuLucTu <= now && (lt.hieuLucDen == null || lt.hieuLucDen > now))
            .OrderBy(lt => lt.chieu).ThenByDescending(lt => lt.phienBan).Select(lt => new
            {
                lt.id,
                lt.chieu,
                lt.tenLoTrinh,
                danhSachTram = lt.LoTrinhTramDungs
                    .OrderBy(d => d.thuTu)
                    .Select(d => new
                    {
                        d.thuTu,
                        d.tramDungId,
                        d.tramDung.maTram,
                        d.tramDung.tenTram,
                        d.tramDung.diaChi,
                        d.tramDung.viDo,
                        d.tramDung.kinhDo,
                        d.khoangCachTuDauKm,
                        d.phutTuDau
                    })
            })
        };
    }

    public async Task<dynamic> luuTuyenXe(TuyenXeDto thongTin)
    {
        TimeOnly.TryParse(thongTin.gioBatDau, out var gioBatDau);
        TimeOnly.TryParse(thongTin.gioKetThuc, out var gioKetThuc);
        int? chinhSachId = null;
        if (!string.IsNullOrWhiteSpace(thongTin.maChinhSach))
            chinhSachId = (await _context.ChinhSachMienPhis.FirstOrDefaultAsync(x => x.ma == thongTin.maChinhSach))?.id
                ?? throw new ArgumentException("Chính sách miễn phí không hợp lệ.");

        if (thongTin.id == 0)
        {
            var tuyenMoi = new TuyenXe
            {
                maTuyen = thongTin.maTuyen.Trim(),
                tenTuyen = thongTin.tenTuyen.Trim(),
                gioBatDau = gioBatDau,
                gioKetThuc = gioKetThuc,
                gianCachPhut = thongTin.gianCachPhut,
                trangThai = thongTin.trangThai,
                chinhSachMienPhiId = chinhSachId,
                cuLyKm = thongTin.cuLyKm
            };

            await _context.TuyenXes.AddAsync(tuyenMoi);
            await _context.SaveChangesAsync();

            if (_cache != null)
            {
                await _cache.RemoveByPrefixAsync("smartbus:cache:tuyen-xe");
                await _cache.RemoveByPrefixAsync("smartbus:cache:bieu-gia");
            }

            return new { id = tuyenMoi.id, thongBao = "Thêm mới tuyến xe thành công" };
        }
        else
        {
            var tonTai = await _context.TuyenXes.FirstOrDefaultAsync(x => x.id == thongTin.id);
            if (tonTai == null) return null!;

            tonTai.maTuyen = thongTin.maTuyen.Trim();
            tonTai.tenTuyen = thongTin.tenTuyen.Trim();
            tonTai.gioBatDau = gioBatDau;
            tonTai.gioKetThuc = gioKetThuc;
            tonTai.gianCachPhut = thongTin.gianCachPhut;
            tonTai.trangThai = thongTin.trangThai;
            tonTai.chinhSachMienPhiId = chinhSachId;
            if (thongTin.cuLyKm.HasValue) tonTai.cuLyKm = thongTin.cuLyKm;

            await _context.SaveChangesAsync();

            if (_cache != null)
            {
                await _cache.RemoveByPrefixAsync("smartbus:cache:tuyen-xe");
                await _cache.RemoveByPrefixAsync("smartbus:cache:bieu-gia");
            }

            return new { id = tonTai.id, thongBao = "Cập nhật tuyến xe thành công" };
        }
    }

    public async Task<dynamic> layDanhSachTramDung(int? tuyenXeId)
    {
        if (tuyenXeId.HasValue)
        {
            var danhSach = await _context.LoTrinhTramDungs
                .AsNoTracking()
                .Where(x => x.loTrinh.tuyenXeId == tuyenXeId.Value)
                .Select(x => x.tramDung)
                .Distinct()
                .ToListAsync();

            return danhSach;
        }

        return await _context.TramDungs.AsNoTracking().ToListAsync();
    }

    public async Task<dynamic> luuTramDung(TramDungDto thongTin)
    {
        if (thongTin.id == 0)
        {
            var tramMoi = new TramDung
            {
                maTram = thongTin.maTram.Trim(),
                tenTram = thongTin.tenTram.Trim(),
                diaChi = thongTin.diaChi,
                viDo = thongTin.viDo,
                kinhDo = thongTin.kinhDo,
                trangThai = thongTin.trangThai
            };

            await _context.TramDungs.AddAsync(tramMoi);
            await _context.SaveChangesAsync();
            if (_cache != null)
            {
                await _cache.RemoveByPrefixAsync("smartbus:cache:tuyen-xe");
            }
            return new { id = tramMoi.id, thongBao = "Thêm mới trạm dừng thành công" };
        }
        else
        {
            var tonTai = await _context.TramDungs.FirstOrDefaultAsync(x => x.id == thongTin.id);
            if (tonTai == null) return null!;

            tonTai.maTram = thongTin.maTram.Trim();
            tonTai.tenTram = thongTin.tenTram.Trim();
            tonTai.diaChi = thongTin.diaChi;
            tonTai.viDo = thongTin.viDo;
            tonTai.kinhDo = thongTin.kinhDo;
            tonTai.trangThai = thongTin.trangThai;

            await _context.SaveChangesAsync();
            if (_cache != null)
            {
                await _cache.RemoveByPrefixAsync("smartbus:cache:tuyen-xe");
            }
            return new { id = tonTai.id, thongBao = "Cập nhật trạm dừng thành công" };
        }
    }

    public async Task<dynamic> layDanhSachXeBuyt()
    {
        return await _context.XeBuyts.AsNoTracking().ToListAsync();
    }

    public async Task<dynamic> luuXeBuyt(XeBuytDto thongTin)
    {
        if (thongTin.id == 0)
        {
            var xeMoi = new XeBuyt
            {
                bienSo = thongTin.bienSo.Trim(),
                maQrTinh = thongTin.maQrTinh ?? Guid.NewGuid(),
                sucChua = thongTin.sucChua,
                trangThai = thongTin.trangThai
            };

            await _context.XeBuyts.AddAsync(xeMoi);
            await _context.SaveChangesAsync();
            return new { id = xeMoi.id, thongBao = "Thêm mới xe buýt thành công" };
        }
        else
        {
            var tonTai = await _context.XeBuyts.FirstOrDefaultAsync(x => x.id == thongTin.id);
            if (tonTai == null) return null!;

            tonTai.bienSo = thongTin.bienSo.Trim();
            tonTai.sucChua = thongTin.sucChua;
            tonTai.trangThai = thongTin.trangThai;

            await _context.SaveChangesAsync();
            return new { id = tonTai.id, thongBao = "Cập nhật xe buýt thành công" };
        }
    }

    public async Task<dynamic> layThongTinQrDanXe(Guid maQrTinh)
    {
        var xe = await _context.XeBuyts
            .AsNoTracking()
            .Include(x => x.ChuyenXes)
                .ThenInclude(cx => cx.loTrinh)
                    .ThenInclude(lt => lt.tuyenXe)
            .FirstOrDefaultAsync(x => x.maQrTinh == maQrTinh && x.trangThai);

        if (xe == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin xe buýt hợp lệ hoặc xe đang tạm ngưng hoạt động.");
        }

        var activeTrips = xe.ChuyenXes
            .Where(cx => cx.trangThai == "DangChay")
            .ToList();

        if (activeTrips.Count == 0)
        {
            throw new InvalidOperationException("Phương tiện này hiện không có chuyến xe nào đang chạy.");
        }

        if (activeTrips.Count > 1)
        {
            throw new InvalidOperationException("Xung đột dữ liệu: phát hiện nhiều hơn một chuyến xe đang chạy cho phương tiện này.");
        }

        var trip = activeTrips.First();
        return new
        {
            xeBuytId = xe.id,
            xe.bienSo,
            xe.maQrTinh,
            xe.sucChua,
            chuyenXeHienTai = new
            {
                trip.id,
                tuyenXeId = trip.loTrinh.tuyenXeId,
                maTuyen = trip.loTrinh.tuyenXe.maTuyen,
                tenTuyen = trip.loTrinh.tuyenXe.tenTuyen,
                mienPhi = await ChinhSachTuyen.TuyenMienPhiAsync(_context, trip.loTrinh.tuyenXeId),
                chieu = trip.loTrinh.chieu,
                tenLoTrinh = trip.loTrinh.tenLoTrinh
            }
        };
    }

    public async Task<dynamic> batDauPhienSoatVe(BatDauPhienDto thongTin)
    {
        var thietBi = await _context.ThietBiSoatVes.FindAsync(thongTin.thietBiId);
        if (thietBi == null || !thietBi.trangThai) throw new Exception("Không tìm thấy thiết bị soát vé");
        if (!await _context.ChuyenXes.AnyAsync(x => x.id == thongTin.chuyenXeId && x.trangThai == "DangChay")) throw new Exception("Chuyến xe không đang hoạt động");

        var nhanVien = await _context.NguoiDungs.FindAsync(thongTin.nhanVienId);
        if (nhanVien == null) throw new Exception("Không tìm thấy nhân viên soát vé");

        var phienDangMo = await _context.PhienSoatVes.FirstOrDefaultAsync(x => x.thietBiSoatVeId == thongTin.thietBiId && x.ketThucLuc == null);
        if (phienDangMo != null)
        {
            if (phienDangMo.nhanVienId != thongTin.nhanVienId || phienDangMo.chuyenXeId != thongTin.chuyenXeId)
                throw new Exception("Thiết bị đang có phiên soát vé khác.");
            return new { phienId = phienDangMo.id, thietBi.tenThietBi, nhanVien.hoTen, phienDangMo.batDauLuc };
        }

        var phienMoi = new PhienSoatVe
        {
            id = Guid.NewGuid(),
            thietBiSoatVeId = thongTin.thietBiId,
            nhanVienId = thongTin.nhanVienId,
            chuyenXeId = thongTin.chuyenXeId,
            batDauLuc = DateTime.UtcNow
        };

        await _context.PhienSoatVes.AddAsync(phienMoi);
        await _context.SaveChangesAsync();

        return new
        {
            phienId = phienMoi.id,
            thietBi.tenThietBi,
            nhanVien.hoTen,
            phienMoi.batDauLuc
        };
    }

    public async Task<dynamic> ketThucPhienSoatVe(Guid phienId)
    {
        var phien = await _context.PhienSoatVes.FirstOrDefaultAsync(x => x.id == phienId);
        if (phien == null) return false;

        phien.ketThucLuc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
