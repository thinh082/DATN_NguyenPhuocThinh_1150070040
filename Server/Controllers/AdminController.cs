using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Commons;
using Server.Data;
using Server.Entities;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Authorize(Roles = "QuanTriVien")]
[Route("api/[controller]")]
public class AdminController(SmartBusDbContext db, ICacheService? cache = null) : ControllerBase
{
    private static byte[] GenerateCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        // Formula injection protection: prepend single quote if starts with =, +, -, @
        if (value.StartsWith('=') || value.StartsWith('+') || value.StartsWith('-') || value.StartsWith('@'))
        {
            value = "'" + value;
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    // ==========================================
    // 1. DASHBOARD & BÁO CÁO TỔNG HỢP
    // ==========================================

    [HttpGet("tong-quan")]
    public async Task<IActionResult> TongQuan([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        var start = tuNgay ?? DateTime.UtcNow.Date.AddDays(-29);
        var end = denNgay ?? DateTime.UtcNow;
        if (end < start) return BadRequest(KetQuaPhanHoi.ThatBai("Khoang ngay khong hop le.", 400));
        var orders = db.DonHangs.AsNoTracking().Where(x => x.ngayTao >= start && x.ngayTao <= end && (x.trangThai == "DaThanhToan" || x.trangThai == "MienPhi"));
        var total = await orders.SumAsync(x => (decimal?)(x.tongGiaGoc - x.tongMienGiam)) ?? 0;
        return Ok(KetQuaPhanHoi.ThanhCong(new
        {
            luotDi = await db.LuotDis.CountAsync(x => x.trangThai == "HopLe" && x.congNhanLuc >= start && x.congNhanLuc <= end),
            vePhatHanh = await db.Ves.CountAsync(x => x.ngayPhatHanh >= start && x.ngayPhatHanh <= end),
            doanhThu = total,
            canhBao = await db.CanhBaoGianLans.CountAsync(x => x.trangThai == "Moi"),
            hoSoChoDuyet = await db.HoSoUuTiens.CountAsync(x => x.trangThai == "ChoDuyet"),
            chuyenDangChay = await db.ChuyenXes.CountAsync(x => x.trangThai == "DangChay")
        }));
    }

    [HttpGet("bao-cao-tong-hop")]
    public async Task<IActionResult> BaoCaoTongHop([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay, [FromQuery] int? tuyenXeId)
    {
        var start = (tuNgay ?? DateTime.UtcNow.Date.AddDays(-29)).ToUniversalTime();
        var end = (denNgay ?? DateTime.UtcNow).ToUniversalTime();
        if (end < start) return BadRequest(KetQuaPhanHoi.ThatBai("Khoang ngay khong hop le.", 400));

        var donHangQuery = db.DonHangs.AsNoTracking().Where(x => x.ngayTao >= start && x.ngayTao <= end && (x.trangThai == "DaThanhToan" || x.trangThai == "MienPhi"));
        if (tuyenXeId.HasValue)
            donHangQuery = donHangQuery.Where(d => d.ChiTietDonHangs.Any(c => c.giaVe.tuyenXeId == tuyenXeId.Value));

        var luotDiQuery = db.LuotDis.AsNoTracking().Where(x => x.trangThai == "HopLe" && x.congNhanLuc >= start && x.congNhanLuc <= end);
        if (tuyenXeId.HasValue)
            luotDiQuery = luotDiQuery.Where(l => l.Ve.tuyenXeId == tuyenXeId.Value || l.NhatKyQuetVe.PhienSoatVe.chuyenXe.loTrinh.tuyenXeId == tuyenXeId.Value);

        var veQuery = db.Ves.AsNoTracking().Where(x => x.ngayPhatHanh >= start && x.ngayPhatHanh <= end);
        if (tuyenXeId.HasValue)
            veQuery = veQuery.Where(v => v.tuyenXeId == tuyenXeId.Value);

        var totalDoanhThu = await donHangQuery.SumAsync(x => (decimal?)(x.tongGiaGoc - x.tongMienGiam)) ?? 0;
        var totalTroGia = await donHangQuery.SumAsync(x => (decimal?)x.tongMienGiam) ?? 0;
        var tongLuotDi = await luotDiQuery.CountAsync();
        var tongVe = await veQuery.CountAsync();
        var tongDonHang = await donHangQuery.CountAsync();
        var canhBaoMoi = await db.CanhBaoGianLans.CountAsync(x => x.trangThai == "Moi" && x.ngayTao >= start && x.ngayTao <= end);
        var hoSoChoDuyet = await db.HoSoUuTiens.CountAsync(x => x.trangThai == "ChoDuyet");

        return Ok(KetQuaPhanHoi.ThanhCong(new
        {
            tuNgay = start,
            denNgay = end,
            tuyenXeId,
            tongLuotDi,
            tongVePhatHanh = tongVe,
            tongDonHang,
            doanhThuMock = totalDoanhThu,
            soTienTroGia = totalTroGia,
            canhBaoMoi,
            hoSoChoDuyet
        }));
    }

    // ==========================================
    // 2. LỘ TRÌNH & THỨ TỰ TRẠM DỪNG
    // ==========================================

    [HttpGet("lo-trinh")]
    public async Task<IActionResult> LoTrinh()
    {
        var list = await db.LoTrinhs.AsNoTracking()
            .OrderBy(x => x.tuyenXeId).ThenBy(x => x.chieu).ThenByDescending(x => x.phienBan)
            .Select(x => new
            {
                x.id,
                x.tuyenXeId,
                tenTuyen = x.tuyenXe.tenTuyen,
                maTuyen = x.tuyenXe.maTuyen,
                x.chieu,
                x.phienBan,
                x.tenLoTrinh,
                x.hieuLucTu,
                x.hieuLucDen,
                soTram = x.LoTrinhTramDungs.Count,
                soChuyen = x.ChuyenXes.Count
            }).ToListAsync();
        return Ok(KetQuaPhanHoi.ThanhCong(list));
    }

    [HttpGet("lo-trinh/{id:int}")]
    public async Task<IActionResult> ChiTietLoTrinh(int id)
    {
        var loTrinh = await db.LoTrinhs.AsNoTracking()
            .Include(x => x.tuyenXe)
            .Include(x => x.LoTrinhTramDungs)
                .ThenInclude(t => t.tramDung)
            .FirstOrDefaultAsync(x => x.id == id);
        if (loTrinh == null) return NotFound(KetQuaPhanHoi.ThatBai("Khong tim thay lo trinh", 404));

        return Ok(KetQuaPhanHoi.ThanhCong(new
        {
            loTrinh.id,
            loTrinh.tuyenXeId,
            tenTuyen = loTrinh.tuyenXe.tenTuyen,
            maTuyen = loTrinh.tuyenXe.maTuyen,
            loTrinh.chieu,
            loTrinh.phienBan,
            loTrinh.tenLoTrinh,
            loTrinh.hieuLucTu,
            loTrinh.hieuLucDen,
            soChuyen = await db.ChuyenXes.CountAsync(c => c.loTrinhId == id),
            danhSachTram = loTrinh.LoTrinhTramDungs
                .OrderBy(t => t.thuTu)
                .Select(t => new
                {
                    t.id,
                    t.tramDungId,
                    tenTram = t.tramDung.tenTram,
                    maTram = t.tramDung.maTram,
                    diaChi = t.tramDung.diaChi,
                    t.thuTu,
                    t.khoangCachTuDauKm,
                    t.phutTuDau
                })
        }));
    }

    public record TaoLoTrinhDto(int tuyenXeId, string chieu, string tenLoTrinh, DateTime hieuLucTu, DateTime? hieuLucDen, List<int> danhSachTramId);

    [HttpPost("lo-trinh")]
    public async Task<IActionResult> TaoLoTrinh([FromBody] TaoLoTrinhDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.tenLoTrinh) || dto.chieu is not ("Di" or "Ve"))
            return BadRequest(KetQuaPhanHoi.ThatBai("Chieu lo trinh phai la 'Di' hoac 'Ve' va ten khong duoc de trong.", 400));
        if (dto.hieuLucDen.HasValue && dto.hieuLucDen.Value <= dto.hieuLucTu)
            return BadRequest(KetQuaPhanHoi.ThatBai("Hieu luc den phai sau hieu luc tu.", 400));
        if (dto.danhSachTramId == null || dto.danhSachTramId.Count < 2)
            return BadRequest(KetQuaPhanHoi.ThatBai("Lo trinh phai co toi thieu 2 tram dung.", 400));
        if (dto.danhSachTramId.Distinct().Count() != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Cac tram dung trong cung mot lo trinh khong duoc lap lai.", 400));

        var tuyen = await db.TuyenXes.FindAsync(dto.tuyenXeId);
        if (tuyen == null || !tuyen.trangThai)
            return BadRequest(KetQuaPhanHoi.ThatBai("Tuyen xe khong ton tai hoac da bi vo hieu hoa.", 400));

        var tramCount = await db.TramDungs.CountAsync(x => dto.danhSachTramId.Contains(x.id) && x.trangThai);
        if (tramCount != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Mot hoac nhieu tram dung khong ton tai hoac da bi vo hieu hoa.", 400));

        var currentOpen = await db.LoTrinhs.FirstOrDefaultAsync(x => x.tuyenXeId == dto.tuyenXeId && x.chieu == dto.chieu && x.hieuLucDen == null);
        if (currentOpen != null)
        {
            if (dto.hieuLucTu <= currentOpen.hieuLucTu)
                return BadRequest(KetQuaPhanHoi.ThatBai("Phiên bản mới phải có hiệu lực từ sau thời điểm bắt đầu của phiên bản hiện tại.", 400));
            currentOpen.hieuLucDen = dto.hieuLucTu;
        }
        else
        {
            var overlap = await db.LoTrinhs.AnyAsync(x => x.tuyenXeId == dto.tuyenXeId && x.chieu == dto.chieu &&
                x.hieuLucTu < (dto.hieuLucDen ?? DateTime.MaxValue) &&
                dto.hieuLucTu < (x.hieuLucDen ?? DateTime.MaxValue));
            if (overlap)
                return BadRequest(KetQuaPhanHoi.ThatBai("Khoang thoi gian hieu luc bi chong lan voi phien ban khac cua cung tuyen va chieu.", 400));
        }

        var maxVersion = await db.LoTrinhs
            .Where(x => x.tuyenXeId == dto.tuyenXeId && x.chieu == dto.chieu)
            .MaxAsync(x => (short?)x.phienBan) ?? 0;
        var newVersion = (short)(maxVersion + 1);

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var loTrinh = new LoTrinh
            {
                tuyenXeId = dto.tuyenXeId,
                chieu = dto.chieu,
                phienBan = newVersion,
                tenLoTrinh = dto.tenLoTrinh.Trim(),
                hieuLucTu = dto.hieuLucTu.ToUniversalTime(),
                hieuLucDen = dto.hieuLucDen?.ToUniversalTime()
            };
            db.LoTrinhs.Add(loTrinh);
            await db.SaveChangesAsync();

            short order = 1;
            foreach (var tramId in dto.danhSachTramId)
            {
                db.LoTrinhTramDungs.Add(new LoTrinhTramDung
                {
                    loTrinhId = loTrinh.id,
                    tramDungId = tramId,
                    thuTu = order++,
                    khoangCachTuDauKm = (order - 2) * 1.5m,
                    phutTuDau = (short?)((order - 2) * 3)
                });
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            if (cache != null) await cache.RemoveByPrefixAsync("smartbus:cache:tuyen-xe");

            return Ok(KetQuaPhanHoi.ThanhCong(new { loTrinh.id, phienBan = newVersion }, "Tao lo trinh thanh cong"));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpPut("lo-trinh/{id:int}")]
    public async Task<IActionResult> CapNhatLoTrinh(int id, [FromBody] TaoLoTrinhDto dto)
    {
        var loTrinh = await db.LoTrinhs.Include(x => x.LoTrinhTramDungs).FirstOrDefaultAsync(x => x.id == id);
        if (loTrinh == null) return NotFound(KetQuaPhanHoi.ThatBai("Khong tim thay lo trinh", 404));

        var coChuyenSuDung = await db.ChuyenXes.AnyAsync(x => x.loTrinhId == id);
        if (coChuyenSuDung)
            return Conflict(KetQuaPhanHoi.ThatBai("Lộ trình đã có chuyến xe sử dụng, không được sửa đổi trực tiếp. Hãy tạo phiên bản mới.", 409));

        if (string.IsNullOrWhiteSpace(dto.tenLoTrinh) || dto.chieu is not ("Di" or "Ve"))
            return BadRequest(KetQuaPhanHoi.ThatBai("Chieu lo trinh phai la 'Di' hoac 'Ve' va ten khong duoc de trong.", 400));
        if (dto.hieuLucDen.HasValue && dto.hieuLucDen.Value <= dto.hieuLucTu)
            return BadRequest(KetQuaPhanHoi.ThatBai("Hieu luc den phai sau hieu luc tu.", 400));
        if (dto.danhSachTramId == null || dto.danhSachTramId.Count < 2)
            return BadRequest(KetQuaPhanHoi.ThatBai("Lo trinh phai co toi thieu 2 tram dung.", 400));
        if (dto.danhSachTramId.Distinct().Count() != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Cac tram dung trong cung mot lo trinh khong duoc lap lai.", 400));

        var tramCount = await db.TramDungs.CountAsync(x => dto.danhSachTramId.Contains(x.id) && x.trangThai);
        if (tramCount != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Mot hoac nhieu tram dung khong ton tai hoac da bi vo hieu hoa.", 400));

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            loTrinh.tenLoTrinh = dto.tenLoTrinh.Trim();
            loTrinh.chieu = dto.chieu;
            loTrinh.hieuLucTu = dto.hieuLucTu.ToUniversalTime();
            loTrinh.hieuLucDen = dto.hieuLucDen?.ToUniversalTime();

            db.LoTrinhTramDungs.RemoveRange(loTrinh.LoTrinhTramDungs);
            await db.SaveChangesAsync();

            short order = 1;
            foreach (var tramId in dto.danhSachTramId)
            {
                db.LoTrinhTramDungs.Add(new LoTrinhTramDung
                {
                    loTrinhId = loTrinh.id,
                    tramDungId = tramId,
                    thuTu = order++,
                    khoangCachTuDauKm = (order - 2) * 1.5m,
                    phutTuDau = (short?)((order - 2) * 3)
                });
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            if (cache != null) await cache.RemoveByPrefixAsync("smartbus:cache:tuyen-xe");

            return Ok(KetQuaPhanHoi.ThanhCong(null, "Cap nhat lo trinh thanh cong"));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ==========================================
    // 3. CHUYẾN XE (TẠO, SỬA, HỦY, ĐỔI TRẠNG THÁI)
    // ==========================================

    [HttpGet("chuyen-xe")]
    public async Task<IActionResult> ChuyenXe([FromQuery] int? trang = null, [FromQuery] int? soLuong = null)
    {
        var query = db.ChuyenXes.AsNoTracking().OrderByDescending(x => x.id);
        if (trang.HasValue && trang.Value > 0)
        {
            var page = trang.Value;
            var size = soLuong.HasValue && soLuong.Value > 0 ? soLuong.Value : 20;
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size)
                .Select(x => new { x.id, x.loTrinhId, x.xeBuytId, tenTuyen = x.loTrinh.tuyenXe.tenTuyen, tenLoTrinh = x.loTrinh.tenLoTrinh, x.xeBuyt.bienSo, x.gioKhoiHanhDuKien, x.gioKetThucDuKien, x.trangThai })
                .ToListAsync();
            return Ok(KetQuaPhanHoi.ThanhCong(new { tongSo = total, trang = page, soLuong = size, danhSach = items }));
        }

        return Ok(KetQuaPhanHoi.ThanhCong(await query.Take(200)
            .Select(x => new { x.id, x.loTrinhId, x.xeBuytId, tenTuyen = x.loTrinh.tuyenXe.tenTuyen, tenLoTrinh = x.loTrinh.tenLoTrinh, x.xeBuyt.bienSo, x.gioKhoiHanhDuKien, x.gioKetThucDuKien, x.trangThai }).ToListAsync()));
    }

    public record ChuyenDto(int loTrinhId, int xeBuytId, DateTime gioKhoiHanhDuKien, DateTime gioKetThucDuKien);

    [HttpPost("chuyen-xe")]
    public async Task<IActionResult> TaoChuyen([FromBody] ChuyenDto dto)
    {
        if (dto.gioKetThucDuKien <= dto.gioKhoiHanhDuKien || !await db.LoTrinhs.AnyAsync(x => x.id == dto.loTrinhId) || !await db.XeBuyts.AnyAsync(x => x.id == dto.xeBuytId && x.trangThai))
            return BadRequest(KetQuaPhanHoi.ThatBai("Lo trinh, xe hoac thoi gian khong hop le.", 400));
        var start = dto.gioKhoiHanhDuKien.ToUniversalTime();
        var end = dto.gioKetThucDuKien.ToUniversalTime();
        if (await db.ChuyenXes.AnyAsync(x => x.xeBuytId == dto.xeBuytId && x.trangThai != "Huy" && x.trangThai != "HoanThanh" && x.gioKhoiHanhDuKien < end && x.gioKetThucDuKien > start))
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe da co chuyen trung khoang thoi gian nay.", 400));
        var trip = new ChuyenXe { loTrinhId = dto.loTrinhId, xeBuytId = dto.xeBuytId, gioKhoiHanhDuKien = start, gioKetThucDuKien = end, trangThai = "DuKien" };
        db.ChuyenXes.Add(trip);
        await db.SaveChangesAsync();
        return Ok(KetQuaPhanHoi.ThanhCong(new { trip.id }));
    }

    [HttpPut("chuyen-xe/{id:long}")]
    public async Task<IActionResult> SuaChuyen(long id, [FromBody] ChuyenDto dto)
    {
        var trip = await db.ChuyenXes.FindAsync(id);
        if (trip == null) return NotFound(KetQuaPhanHoi.ThatBai("Khong tim thay chuyen xe", 404));
        if (trip.trangThai != "DuKien")
            return BadRequest(KetQuaPhanHoi.ThatBai("Chi duoc sua chuyen xe o trang thai Du kien.", 400));
        if (dto.gioKetThucDuKien <= dto.gioKhoiHanhDuKien)
            return BadRequest(KetQuaPhanHoi.ThatBai("Gio ket thuc phai sau gio khoi hanh.", 400));

        var loTrinh = await db.LoTrinhs.FindAsync(dto.loTrinhId);
        if (loTrinh == null)
            return BadRequest(KetQuaPhanHoi.ThatBai("Lo trinh khong ton tai.", 400));

        var xe = await db.XeBuyts.FindAsync(dto.xeBuytId);
        if (xe == null || !xe.trangThai)
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe buyt khong ton tai hoac khong hoat dong.", 400));

        var start = dto.gioKhoiHanhDuKien.ToUniversalTime();
        var end = dto.gioKetThucDuKien.ToUniversalTime();

        if (await db.ChuyenXes.AnyAsync(x => x.id != id && x.xeBuytId == dto.xeBuytId && x.trangThai != "Huy" && x.trangThai != "HoanThanh" && x.gioKhoiHanhDuKien < end && x.gioKetThucDuKien > start))
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe da co chuyen trung khoang thoi gian nay.", 400));

        trip.loTrinhId = dto.loTrinhId;
        trip.xeBuytId = dto.xeBuytId;
        trip.gioKhoiHanhDuKien = start;
        trip.gioKetThucDuKien = end;
        await db.SaveChangesAsync();

        return Ok(KetQuaPhanHoi.ThanhCong(new { trip.id }, "Cap nhat chuyen xe thanh cong"));
    }

    public record TrangThaiChuyenDto(string trangThai);

    [HttpPost("chuyen-xe/{id:long}/trang-thai")]
    public async Task<IActionResult> TrangThaiChuyen(long id, [FromBody] TrangThaiChuyenDto dto)
    {
        var trip = await db.ChuyenXes.FindAsync(id);
        if (trip == null) return NotFound();
        if (!(trip.trangThai == "DuKien" && (dto.trangThai == "DangChay" || dto.trangThai == "Huy")) && !(trip.trangThai == "DangChay" && dto.trangThai == "HoanThanh"))
            return BadRequest(KetQuaPhanHoi.ThatBai("Chuyen trang thai khong hop le.", 400));
        if (dto.trangThai == "DangChay" && await db.ChuyenXes.AnyAsync(x => x.id != id && x.xeBuytId == trip.xeBuytId && x.trangThai == "DangChay"))
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe dang co chuyen hoat dong.", 400));
        if (dto.trangThai == "DangChay") trip.gioKhoiHanhThucTe = DateTime.UtcNow;
        if (dto.trangThai == "HoanThanh") trip.gioKetThucThucTe = DateTime.UtcNow;
        trip.trangThai = dto.trangThai;
        await db.SaveChangesAsync();
        return Ok(KetQuaPhanHoi.ThanhCong());
    }

    // ==========================================
    // 4. ĐƠN HÀNG, BIỂU GIÁ & VÉ ĐIỆN TỬ
    // ==========================================

    [HttpGet("don-hang")]
    public async Task<IActionResult> DonHang([FromQuery] int? trang = null, [FromQuery] int? soLuong = null)
    {
        var query = db.DonHangs.AsNoTracking().OrderByDescending(x => x.id);
        if (trang.HasValue && trang.Value > 0)
        {
            var page = trang.Value;
            var size = soLuong.HasValue && soLuong.Value > 0 ? soLuong.Value : 20;
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size)
                .Select(x => new { x.id, x.maDonHang, x.nguoiDung.hoTen, x.tongGiaGoc, x.tongMienGiam, x.trangThai, x.ngayTao })
                .ToListAsync();
            return Ok(KetQuaPhanHoi.ThanhCong(new { tongSo = total, trang = page, soLuong = size, danhSach = items }));
        }

        return Ok(KetQuaPhanHoi.ThanhCong(await query.Take(200)
            .Select(x => new { x.id, x.maDonHang, x.nguoiDung.hoTen, x.tongGiaGoc, x.tongMienGiam, x.trangThai, x.ngayTao }).ToListAsync()));
    }

    public record GiaVeDto(int id, decimal giaGoc, decimal soTienMienGiam);

    [HttpPost("gia-ve")]
    public async Task<IActionResult> CapNhatGiaVe([FromBody] GiaVeDto dto)
    {
        if (dto.giaGoc < 0 || dto.soTienMienGiam < 0 || dto.soTienMienGiam > dto.giaGoc)
            return BadRequest(KetQuaPhanHoi.ThatBai("Gia ve hoac mien giam khong hop le.", 400));
        var fare = await db.GiaVes.FindAsync(dto.id);
        if (fare == null) return NotFound();
        fare.giaGoc = dto.giaGoc;
        fare.soTienMienGiam = dto.soTienMienGiam;
        await db.SaveChangesAsync();

        if (cache != null) await cache.RemoveByPrefixAsync("smartbus:cache:bieu-gia");

        return Ok(KetQuaPhanHoi.ThanhCong());
    }

    [HttpGet("ve")]
    public async Task<IActionResult> Ve([FromQuery] int? trang = null, [FromQuery] int? soLuong = null)
    {
        var query = db.Ves.AsNoTracking().OrderByDescending(x => x.id);
        if (trang.HasValue && trang.Value > 0)
        {
            var page = trang.Value;
            var size = soLuong.HasValue && soLuong.Value > 0 ? soLuong.Value : 20;
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size)
                .Select(x => new { x.maVe, x.nguoiDung.hoTen, x.maLoaiVe, tenTuyen = x.tuyenXe != null ? x.tuyenXe.tenTuyen : "Toan mang", x.hieuLucTu, x.hetHanLuc, x.trangThai })
                .ToListAsync();
            return Ok(KetQuaPhanHoi.ThanhCong(new { tongSo = total, trang = page, soLuong = size, danhSach = items }));
        }

        return Ok(KetQuaPhanHoi.ThanhCong(await query.Take(200)
            .Select(x => new { x.maVe, x.nguoiDung.hoTen, x.maLoaiVe, tenTuyen = x.tuyenXe != null ? x.tuyenXe.tenTuyen : "Toan mang", x.hieuLucTu, x.hetHanLuc, x.trangThai }).ToListAsync()));
    }

    public record MoKhoaVeDto(string lyDo);

    [HttpPost("ve/{maVe:guid}/mo-khoa")]
    public async Task<IActionResult> MoKhoaVe(Guid maVe, [FromBody] MoKhoaVeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.lyDo))
            return BadRequest(KetQuaPhanHoi.ThatBai("Bat buoc phai nhap ly do mo khoa ve.", 400));

        var ve = await db.Ves.FirstOrDefaultAsync(x => x.maVe == maVe);
        if (ve == null) return NotFound(KetQuaPhanHoi.ThatBai("Khong tim thay ve", 404));

        if (ve.trangThai != "TamKhoa")
            return BadRequest(KetQuaPhanHoi.ThatBai($"Chi duoc mo khoa ve o trang thai TamKhoa. Trang thai hien tai: {ve.trangThai}", 400));

        if (ve.hetHanLuc <= DateTime.UtcNow)
            return BadRequest(KetQuaPhanHoi.ThatBai("Ve da het han su dung, khong the mo khoa.", 400));

        var userId = int.TryParse(User.FindFirst("id")?.Value, out var uid) ? uid : 1;

        ve.trangThai = "HoatDong";
        db.NhatKyThuHoiVes.Add(new NhatKyThuHoiVe
        {
            veId = ve.id,
            hanhDong = "MoKhoa",
            lyDo = dto.lyDo.Trim(),
            nguoiThucHienId = userId,
            ngayTao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return Ok(KetQuaPhanHoi.ThanhCong(new { ve.maVe, ve.trangThai }, "Mo khoa ve thanh cong"));
    }

    // ==========================================
    // 5. NHẬT KÝ & CẢNH BÁO GIAN LẬN
    // ==========================================

    [HttpGet("nhat-ky")]
    public async Task<IActionResult> NhatKy([FromQuery] int? trang = null, [FromQuery] int? soLuong = null)
    {
        var query = db.NhatKyQuetVes.AsNoTracking().OrderByDescending(x => x.id);
        if (trang.HasValue && trang.Value > 0)
        {
            var page = trang.Value;
            var size = soLuong.HasValue && soLuong.Value > 0 ? soLuong.Value : 20;
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size)
                .Select(x => new { x.id, x.maVeDocDuoc, x.cheDo, x.ketQuaTaiThietBi, x.maLyDo, x.thoiDiemQuet, thietBi = x.thietBiSoatVe.tenThietBi })
                .ToListAsync();
            return Ok(KetQuaPhanHoi.ThanhCong(new { tongSo = total, trang = page, soLuong = size, danhSach = items }));
        }

        return Ok(KetQuaPhanHoi.ThanhCong(await query.Take(200)
            .Select(x => new { x.id, x.maVeDocDuoc, x.cheDo, x.ketQuaTaiThietBi, x.maLyDo, x.thoiDiemQuet, thietBi = x.thietBiSoatVe.tenThietBi }).ToListAsync()));
    }

    [HttpGet("canh-bao")]
    public async Task<IActionResult> CanhBao([FromQuery] int? trang = null, [FromQuery] int? soLuong = null)
    {
        var query = db.CanhBaoGianLans.AsNoTracking().OrderByDescending(x => x.id);
        if (trang.HasValue && trang.Value > 0)
        {
            var page = trang.Value;
            var size = soLuong.HasValue && soLuong.Value > 0 ? soLuong.Value : 20;
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size)
                .Select(x => new { x.id, x.maCanhBao, x.noiDung, x.trangThai, x.ngayTao, x.ketLuan, x.nhatKyQuetVe.maVeDocDuoc })
                .ToListAsync();
            return Ok(KetQuaPhanHoi.ThanhCong(new { tongSo = total, trang = page, soLuong = size, danhSach = items }));
        }

        return Ok(KetQuaPhanHoi.ThanhCong(await query.Take(200)
            .Select(x => new { x.id, x.maCanhBao, x.noiDung, x.trangThai, x.ngayTao, x.ketLuan, x.nhatKyQuetVe.maVeDocDuoc }).ToListAsync()));
    }

    public record XuLyCanhBaoDto(string trangThai, string? ketLuan);

    [HttpPost("canh-bao/{id:long}")]
    public async Task<IActionResult> XuLyCanhBao(long id, [FromBody] XuLyCanhBaoDto dto)
    {
        if (dto.trangThai is not ("DangXuLy" or "DaXuLy" or "BoQua")) return BadRequest();
        var alert = await db.CanhBaoGianLans.FindAsync(id);
        if (alert == null) return NotFound();
        alert.trangThai = dto.trangThai;
        alert.ketLuan = dto.ketLuan?.Trim();
        alert.nguoiXuLyId = int.TryParse(User.FindFirst("id")?.Value, out var uid) ? uid : 1;
        alert.xuLyLuc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(KetQuaPhanHoi.ThanhCong());
    }

    // ==========================================
    // 6. XUẤT CSV AN TOÀN (UTF-8 BOM, ANTI-INJECTION)
    // ==========================================

    [HttpGet("xuat-csv/{loai}")]
    public async Task<IActionResult> XuatCsv(string loai)
    {
        byte[] bytes;
        string filename;
        switch (loai.ToLower())
        {
            case "don-hang":
                var orders = await db.DonHangs.AsNoTracking().OrderByDescending(x => x.id).Take(1000)
                    .Select(x => new[] { x.maDonHang.ToString(), x.nguoiDung.hoTen, x.tongGiaGoc.ToString(), x.tongMienGiam.ToString(), (x.tongGiaGoc - x.tongMienGiam).ToString(), x.trangThai, x.ngayTao.ToString("yyyy-MM-dd HH:mm:ss") })
                    .ToListAsync();
                bytes = GenerateCsv(new[] { "Mã đơn hàng", "Họ tên", "Tổng giá gốc", "Miễn giảm", "Thực thu", "Trạng thái", "Ngày tạo" }, orders);
                filename = $"smartbus_don_hang_{DateTime.UtcNow:yyyyMMdd}.csv";
                break;

            case "ve":
                var tickets = await db.Ves.AsNoTracking().OrderByDescending(x => x.id).Take(1000)
                    .Select(x => new[] { x.maVe.ToString(), x.nguoiDung.hoTen, x.maLoaiVe, x.tuyenXe != null ? x.tuyenXe.tenTuyen : "Toàn mạng", x.hieuLucTu.ToString("yyyy-MM-dd HH:mm:ss"), x.hetHanLuc.ToString("yyyy-MM-dd HH:mm:ss"), x.trangThai })
                    .ToListAsync();
                bytes = GenerateCsv(new[] { "Mã vé", "Hành khách", "Loại vé", "Tuyến", "Hiệu lực từ", "Hết hạn lúc", "Trạng thái" }, tickets);
                filename = $"smartbus_ve_{DateTime.UtcNow:yyyyMMdd}.csv";
                break;

            case "luot-di":
                var rides = await db.LuotDis.AsNoTracking().OrderByDescending(x => x.id).Take(1000)
                    .Select(x => new[] { x.id.ToString(), x.Ve.maVe.ToString(), x.maLoaiVe, x.congNhanLuc.ToString("yyyy-MM-dd HH:mm:ss"), x.trangThai, x.NhatKyQuetVe.thietBiSoatVe.tenThietBi })
                    .ToListAsync();
                bytes = GenerateCsv(new[] { "ID", "Mã vé", "Loại vé", "Thời điểm công nhận", "Trạng thái", "Thiết bị soát" }, rides);
                filename = $"smartbus_luot_di_{DateTime.UtcNow:yyyyMMdd}.csv";
                break;

            case "canh-bao":
                var alerts = await db.CanhBaoGianLans.AsNoTracking().OrderByDescending(x => x.id).Take(1000)
                    .Select(x => new[] { x.id.ToString(), x.maCanhBao, x.noiDung, x.trangThai, x.ngayTao.ToString("yyyy-MM-dd HH:mm:ss"), x.ketLuan ?? "", x.nhatKyQuetVe.maVeDocDuoc ?? "" })
                    .ToListAsync();
                bytes = GenerateCsv(new[] { "ID", "Mã cảnh báo", "Nội dung", "Trạng thái", "Ngày tạo", "Kết luận", "Mã vé" }, alerts);
                filename = $"smartbus_canh_bao_{DateTime.UtcNow:yyyyMMdd}.csv";
                break;

            default:
                return BadRequest(KetQuaPhanHoi.ThatBai("Loai xuat CSV khong hop le. Cac loai ho tro: don-hang, ve, luot-di, canh-bao.", 400));
        }

        return File(bytes, "text/csv; charset=utf-8", filename);
    }
}
