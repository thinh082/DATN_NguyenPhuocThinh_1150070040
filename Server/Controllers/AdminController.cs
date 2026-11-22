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
        if (end < start) return BadRequest(KetQuaPhanHoi.ThatBai("Khoảng ngày không hợp lệ.", 400));
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
        if (end < start) return BadRequest(KetQuaPhanHoi.ThatBai("Khoảng ngày không hợp lệ.", 400));

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
        if (loTrinh == null) return NotFound(KetQuaPhanHoi.ThatBai("Không tìm thấy lộ trình", 404));

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
            return BadRequest(KetQuaPhanHoi.ThatBai("Chiều lộ trình phải là lượt đi hoặc lượt về và tên không được để trống.", 400));
        if (dto.hieuLucDen.HasValue && dto.hieuLucDen.Value <= dto.hieuLucTu)
            return BadRequest(KetQuaPhanHoi.ThatBai("Ngày hết hiệu lực phải sau ngày bắt đầu hiệu lực.", 400));
        if (dto.danhSachTramId == null || dto.danhSachTramId.Count < 2)
            return BadRequest(KetQuaPhanHoi.ThatBai("Lộ trình phải có tối thiểu 2 trạm dừng.", 400));
        if (dto.danhSachTramId.Distinct().Count() != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Các trạm dừng trong cùng một lộ trình không được lặp lại.", 400));

        var tuyen = await db.TuyenXes.FindAsync(dto.tuyenXeId);
        if (tuyen == null || !tuyen.trangThai)
            return BadRequest(KetQuaPhanHoi.ThatBai("Tuyến xe không tồn tại hoặc đã bị vô hiệu hóa.", 400));

        var tramCount = await db.TramDungs.CountAsync(x => dto.danhSachTramId.Contains(x.id) && x.trangThai);
        if (tramCount != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Một hoặc nhiều trạm dừng không tồn tại hoặc đã bị vô hiệu hóa.", 400));

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
                return BadRequest(KetQuaPhanHoi.ThatBai("Khoảng thời gian hiệu lực bị chồng lấn với phiên bản khác của cùng tuyến và chiều.", 400));
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

            return Ok(KetQuaPhanHoi.ThanhCong(new { loTrinh.id, phienBan = newVersion }, "Tạo lộ trình thành công"));
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
        if (loTrinh == null) return NotFound(KetQuaPhanHoi.ThatBai("Không tìm thấy lộ trình", 404));

        var coChuyenSuDung = await db.ChuyenXes.AnyAsync(x => x.loTrinhId == id);
        if (coChuyenSuDung)
            return Conflict(KetQuaPhanHoi.ThatBai("Lộ trình đã có chuyến xe sử dụng, không được sửa đổi trực tiếp. Hãy tạo phiên bản mới.", 409));

        if (string.IsNullOrWhiteSpace(dto.tenLoTrinh) || dto.chieu is not ("Di" or "Ve"))
            return BadRequest(KetQuaPhanHoi.ThatBai("Chiều lộ trình phải là lượt đi hoặc lượt về và tên không được để trống.", 400));
        if (dto.hieuLucDen.HasValue && dto.hieuLucDen.Value <= dto.hieuLucTu)
            return BadRequest(KetQuaPhanHoi.ThatBai("Ngày hết hiệu lực phải sau ngày bắt đầu hiệu lực.", 400));
        if (dto.danhSachTramId == null || dto.danhSachTramId.Count < 2)
            return BadRequest(KetQuaPhanHoi.ThatBai("Lộ trình phải có tối thiểu 2 trạm dừng.", 400));
        if (dto.danhSachTramId.Distinct().Count() != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Các trạm dừng trong cùng một lộ trình không được lặp lại.", 400));

        var tramCount = await db.TramDungs.CountAsync(x => dto.danhSachTramId.Contains(x.id) && x.trangThai);
        if (tramCount != dto.danhSachTramId.Count)
            return BadRequest(KetQuaPhanHoi.ThatBai("Một hoặc nhiều trạm dừng không tồn tại hoặc đã bị vô hiệu hóa.", 400));

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

            return Ok(KetQuaPhanHoi.ThanhCong(null, "Cập nhật lộ trình thành công"));
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
            return BadRequest(KetQuaPhanHoi.ThatBai("Lộ trình, xe hoặc thời gian không hợp lệ.", 400));
        var start = dto.gioKhoiHanhDuKien.ToUniversalTime();
        var end = dto.gioKetThucDuKien.ToUniversalTime();
        if (await db.ChuyenXes.AnyAsync(x => x.xeBuytId == dto.xeBuytId && x.trangThai != "Huy" && x.trangThai != "HoanThanh" && x.gioKhoiHanhDuKien < end && x.gioKetThucDuKien > start))
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe đã có chuyến trùng khoảng thời gian này.", 400));
        var trip = new ChuyenXe { loTrinhId = dto.loTrinhId, xeBuytId = dto.xeBuytId, gioKhoiHanhDuKien = start, gioKetThucDuKien = end, trangThai = "DuKien" };
        db.ChuyenXes.Add(trip);
        await db.SaveChangesAsync();
        return Ok(KetQuaPhanHoi.ThanhCong(new { trip.id }));
    }

    [HttpPut("chuyen-xe/{id:long}")]
    public async Task<IActionResult> SuaChuyen(long id, [FromBody] ChuyenDto dto)
    {
        var trip = await db.ChuyenXes.FindAsync(id);
        if (trip == null) return NotFound(KetQuaPhanHoi.ThatBai("Không tìm thấy chuyến xe", 404));
        if (trip.trangThai != "DuKien")
            return BadRequest(KetQuaPhanHoi.ThatBai("Chỉ được sửa chuyến xe ở trạng thái Dự kiến.", 400));
        if (dto.gioKetThucDuKien <= dto.gioKhoiHanhDuKien)
            return BadRequest(KetQuaPhanHoi.ThatBai("Giờ kết thúc phải sau giờ khởi hành.", 400));

        var loTrinh = await db.LoTrinhs.FindAsync(dto.loTrinhId);
        if (loTrinh == null)
            return BadRequest(KetQuaPhanHoi.ThatBai("Lộ trình không tồn tại.", 400));

        var xe = await db.XeBuyts.FindAsync(dto.xeBuytId);
        if (xe == null || !xe.trangThai)
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe buýt không tồn tại hoặc không hoạt động.", 400));

        var start = dto.gioKhoiHanhDuKien.ToUniversalTime();
        var end = dto.gioKetThucDuKien.ToUniversalTime();

        if (await db.ChuyenXes.AnyAsync(x => x.id != id && x.xeBuytId == dto.xeBuytId && x.trangThai != "Huy" && x.trangThai != "HoanThanh" && x.gioKhoiHanhDuKien < end && x.gioKetThucDuKien > start))
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe đã có chuyến trùng khoảng thời gian này.", 400));

        trip.loTrinhId = dto.loTrinhId;
        trip.xeBuytId = dto.xeBuytId;
        trip.gioKhoiHanhDuKien = start;
        trip.gioKetThucDuKien = end;
        await db.SaveChangesAsync();

        return Ok(KetQuaPhanHoi.ThanhCong(new { trip.id }, "Cập nhật chuyến xe thành công"));
    }

    public record TrangThaiChuyenDto(string trangThai);

    [HttpPost("chuyen-xe/{id:long}/trang-thai")]
    public async Task<IActionResult> TrangThaiChuyen(long id, [FromBody] TrangThaiChuyenDto dto)
    {
        var trip = await db.ChuyenXes.FindAsync(id);
        if (trip == null) return NotFound();
        if (!(trip.trangThai == "DuKien" && (dto.trangThai == "DangChay" || dto.trangThai == "Huy")) && !(trip.trangThai == "DangChay" && dto.trangThai == "HoanThanh"))
            return BadRequest(KetQuaPhanHoi.ThatBai("Chuyển trạng thái không hợp lệ.", 400));
        if (dto.trangThai == "DangChay" && await db.ChuyenXes.AnyAsync(x => x.id != id && x.xeBuytId == trip.xeBuytId && x.trangThai == "DangChay"))
            return BadRequest(KetQuaPhanHoi.ThatBai("Xe đang có chuyến hoạt động.", 400));
        if (dto.trangThai == "DangChay") trip.gioKhoiHanhThucTe = DateTime.UtcNow;
        if (dto.trangThai == "HoanThanh") trip.gioKetThucThucTe = DateTime.UtcNow;
        trip.trangThai = dto.trangThai;
        await db.SaveChangesAsync();
        return Ok(KetQuaPhanHoi.ThanhCong());
    }

    // ==========================================
    // 3B. THIẾT BỊ SOÁT VÉ (KHAI BÁO, SỬA, BẬT/TẮT)
    // ==========================================

    [HttpGet("thiet-bi-soat-ve")]
    public async Task<IActionResult> ThietBiSoatVe()
    {
        var items = await db.ThietBiSoatVes.AsNoTracking()
            .OrderByDescending(x => x.id)
            .Select(x => new
            {
                x.id,
                x.maThietBi,
                x.tenThietBi,
                x.lanDongBoCuoi,
                x.mocThuHoiDaNhan,
                x.ngayDangKy,
                x.trangThai
            })
            .ToListAsync();
        return Ok(KetQuaPhanHoi.ThanhCong(items));
    }

    public record LuuThietBiDto(int id, string tenThietBi, bool trangThai);

    [HttpPost("thiet-bi-soat-ve")]
    public async Task<IActionResult> LuuThietBiSoatVe([FromBody] LuuThietBiDto dto)
    {
        var ten = dto.tenThietBi?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ten) || ten.Length > 100)
            return BadRequest(KetQuaPhanHoi.ThatBai("Tên thiết bị soát vé không được để trống và tối đa 100 ký tự.", 400));

        if (await db.ThietBiSoatVes.AnyAsync(x => x.id != dto.id && x.tenThietBi == ten))
            return BadRequest(KetQuaPhanHoi.ThatBai("Tên thiết bị soát vé đã tồn tại.", 400));

        if (dto.id <= 0)
        {
            var device = new ThietBiSoatVe
            {
                maThietBi = Guid.NewGuid(),
                tenThietBi = ten,
                mocThuHoiDaNhan = 0,
                trangThai = dto.trangThai,
                ngayDangKy = DateTime.UtcNow
            };
            db.ThietBiSoatVes.Add(device);
            await db.SaveChangesAsync();
            return Ok(KetQuaPhanHoi.ThanhCong(new { device.id, device.maThietBi, device.tenThietBi, device.trangThai }, "Thêm thiết bị soát vé thành công"));
        }

        var existing = await db.ThietBiSoatVes.FindAsync(dto.id);
        if (existing == null)
            return NotFound(KetQuaPhanHoi.ThatBai("Không tìm thấy thiết bị soát vé.", 404));

        if (!dto.trangThai && await db.PhienSoatVes.AnyAsync(x => x.thietBiSoatVeId == dto.id && x.ketThucLuc == null))
            return BadRequest(KetQuaPhanHoi.ThatBai("Thiết bị đang có phiên soát vé mở, hãy kết thúc phiên trước khi tạm khóa.", 400));

        existing.tenThietBi = ten;
        existing.trangThai = dto.trangThai;
        await db.SaveChangesAsync();
        return Ok(KetQuaPhanHoi.ThanhCong(new { existing.id, existing.maThietBi, existing.tenThietBi, existing.trangThai }, "Cập nhật thiết bị soát vé thành công"));
    }

    // ==========================================
    // 3C. NHÓM ĐỐI TƯỢNG ƯU TIÊN
    // ==========================================

    [HttpGet("nhom-doi-tuong")]
    public async Task<IActionResult> NhomDoiTuong()
    {
        var items = await db.NhomDoiTuongs.AsNoTracking()
            .OrderBy(x => x.id)
            .Select(x => new { x.id, x.maNhom, x.tenNhom, x.canXacMinh, x.trangThai, soHoSo = x.HoSoUuTiens.Count })
            .ToListAsync();
        return Ok(KetQuaPhanHoi.ThanhCong(items));
    }

    public record LuuNhomDoiTuongDto(int id, string maNhom, string tenNhom, bool canXacMinh, bool trangThai);

    [HttpPost("nhom-doi-tuong")]
    public async Task<IActionResult> LuuNhomDoiTuong([FromBody] LuuNhomDoiTuongDto dto)
    {
        var ma = dto.maNhom?.Trim() ?? string.Empty;
        var ten = dto.tenNhom?.Trim() ?? string.Empty;
        if (ma.Length == 0 || ma.Length > 30 || !ma.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            return BadRequest(KetQuaPhanHoi.ThatBai("Mã nhóm bắt buộc, tối đa 30 ký tự, chỉ gồm chữ không dấu, số hoặc dấu gạch dưới.", 400));
        if (ten.Length == 0 || ten.Length > 100)
            return BadRequest(KetQuaPhanHoi.ThatBai("Tên nhóm bắt buộc và tối đa 100 ký tự.", 400));
        if (await db.NhomDoiTuongs.AnyAsync(x => x.id != dto.id && x.maNhom == ma))
            return BadRequest(KetQuaPhanHoi.ThatBai("Mã nhóm đã tồn tại.", 400));

        NhomDoiTuong nhom;
        if (dto.id <= 0)
        {
            nhom = new NhomDoiTuong { maNhom = ma, tenNhom = ten, canXacMinh = dto.canXacMinh, trangThai = dto.trangThai };
            db.NhomDoiTuongs.Add(nhom);
        }
        else
        {
            var tim = await db.NhomDoiTuongs.FindAsync(dto.id);
            if (tim == null) return NotFound(KetQuaPhanHoi.ThatBai("Không tìm thấy nhóm đối tượng.", 404));
            nhom = tim;
            if (nhom.maNhom == "PhoThong" && (ma != "PhoThong" || dto.canXacMinh || !dto.trangThai))
                return BadRequest(KetQuaPhanHoi.ThatBai("Nhóm Phổ thông là nhóm mặc định: không đổi mã, không yêu cầu xác minh và không được tạm khóa.", 400));
            nhom.maNhom = ma;
            nhom.tenNhom = ten;
            nhom.canXacMinh = dto.canXacMinh;
            nhom.trangThai = dto.trangThai;
        }
        await db.SaveChangesAsync();
        if (cache != null) await cache.RemoveByPrefixAsync("smartbus:cache:bieu-gia");
        return Ok(KetQuaPhanHoi.ThanhCong(new { nhom.id, nhom.maNhom, nhom.tenNhom, nhom.canXacMinh, nhom.trangThai }, "Lưu nhóm đối tượng thành công"));
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
            return BadRequest(KetQuaPhanHoi.ThatBai("Giá vé hoặc mức miễn giảm không hợp lệ.", 400));
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
            return BadRequest(KetQuaPhanHoi.ThatBai("Bắt buộc phải nhập lý do mở khóa vé.", 400));

        var ve = await db.Ves.FirstOrDefaultAsync(x => x.maVe == maVe);
        if (ve == null) return NotFound(KetQuaPhanHoi.ThatBai("Không tìm thấy vé", 404));

        if (ve.trangThai != "TamKhoa")
            return BadRequest(KetQuaPhanHoi.ThatBai($"Chỉ được mở khóa vé đang tạm khóa. Trạng thái hiện tại: {ve.trangThai}", 400));

        if (ve.hetHanLuc <= DateTime.UtcNow)
            return BadRequest(KetQuaPhanHoi.ThatBai("Vé đã hết hạn sử dụng, không thể mở khóa.", 400));

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

        return Ok(KetQuaPhanHoi.ThanhCong(new { ve.maVe, ve.trangThai }, "Mở khóa vé thành công"));
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
                return BadRequest(KetQuaPhanHoi.ThatBai("Loại xuất CSV không hợp lệ. Các loại hỗ trợ: don-hang, ve, luot-di, canh-bao.", 400));
        }

        return File(bytes, "text/csv; charset=utf-8", filename);
    }
}
