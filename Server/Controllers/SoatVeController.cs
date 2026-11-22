using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Server.Commons;
using Server.Models;
using Server.Services;
using Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using Server.Entities;

namespace Server.Controllers;

[ApiController]
[Authorize(Roles = "NhanVienSoatVe,QuanTriVien")]
[Route("api/[controller]")]
public class SoatVeController : ControllerBase
{
    private readonly ISoatVeService _soatVeService;
    private readonly SmartBusDbContext _db;
    private readonly IMatMaService _crypto;
    private readonly OfflineSyncService _sync;

    public SoatVeController(ISoatVeService soatVeService, SmartBusDbContext db, IMatMaService crypto, OfflineSyncService sync)
    {
        _soatVeService = soatVeService;
        _db = db;
        _crypto = crypto;
        _sync = sync;
    }

    [HttpPost("dong-bo-su-kien")]
    public async Task<IActionResult> dongBoSuKien([FromBody] DongBoSuKienOfflineDto item)
    {
        try { return Ok(KetQuaPhanHoi.ThanhCong(await _sync.XuLy(item, int.Parse(User.FindFirst("id")!.Value)))); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400)); }
        catch (InvalidOperationException ex) { return Conflict(KetQuaPhanHoi.ThatBai(ex.Message, 409)); }
    }

    [HttpGet("chuan-bi")]
    public async Task<IActionResult> chuanBi()
    {
        var ngay = ChinhSachTuyen.HomNay;
        var devices = await _db.ThietBiSoatVes.AsNoTracking().Where(x => x.trangThai)
            .Select(x => new { x.id, x.tenThietBi }).ToListAsync();
        var trips = await _db.ChuyenXes.AsNoTracking().Where(x => x.trangThai == "DangChay")
            .Select(x => new
            {
                x.id,
                tuyenXeId = x.loTrinh.tuyenXeId,
                maTuyen = x.loTrinh.tuyenXe.maTuyen,
                tenTuyen = x.loTrinh.tuyenXe.tenTuyen,
                bienSo = x.xeBuyt.bienSo,
                mienPhi = x.loTrinh.tuyenXe.chinhSachMienPhi != null && x.loTrinh.tuyenXe.chinhSachMienPhi.trangThai
                    && x.loTrinh.tuyenXe.chinhSachMienPhi.hieuLucTu <= ngay && x.loTrinh.tuyenXe.chinhSachMienPhi.hieuLucDen >= ngay
            }).ToListAsync();
        return Ok(KetQuaPhanHoi.ThanhCong(new { thietBis = devices, chuyenXes = trips }, "Dữ liệu chuẩn bị phiên soát vé"));
    }

    [HttpGet("goi-kiem-tra")]
    public async Task<IActionResult> goiKiemTra([FromQuery] Guid phienId)
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);
        var session = await _db.PhienSoatVes.AsNoTracking().Include(x => x.chuyenXe).ThenInclude(x => x.loTrinh)
            .FirstOrDefaultAsync(x => x.id == phienId && x.nhanVienId == userId && x.ketThucLuc == null);
        if (session == null) return NotFound();
        var routeId = session.chuyenXe.loTrinh.tuyenXeId;
        var now = DateTime.UtcNow;
        var tickets = await _db.Ves.AsNoTracking().Include(x => x.khoaKySo)
            .Where(x => x.hetHanLuc > now && (x.tuyenXeId == null || x.tuyenXeId == routeId)).ToListAsync();
        var ticketIds = tickets.Select(x => x.id).ToArray();
        var used = await _db.LuotDis.AsNoTracking().Where(x => ticketIds.Contains(x.veId) && x.trangThai == "HopLe")
            .Select(x => new { maVe = x.Ve.maVe, x.buocThoiGian, cungChuyen = x.NhatKyQuetVe.PhienSoatVe.chuyenXeId == session.chuyenXeId }).ToListAsync();
        var tuyenMienPhi = await ChinhSachTuyen.TuyenMienPhiAsync(_db, routeId);
        var data = tickets.Select(x =>
        {
            using var key = ECDsa.Create();
            key.ImportFromPem(x.khoaKySo.khoaCongKhaiPem);
            return new { x.maVe, duLieuDaKy = Convert.ToBase64String(x.duLieuDaKy), khoaCongKhai = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
                biMatTotp = Convert.ToBase64String(_crypto.GiaiMaAes(x.biMatTotpMaHoa)), x.trangThai };
        }).ToList();
        var package = new GoiSoatVe { id = Guid.NewGuid(), phienSoatVeId = session.id, taoLuc = now, hetHanLuc = now.AddHours(24),
            danhSachVeHopLe = JsonSerializer.Serialize(tickets.Where(x => x.trangThai == "HoatDong").Select(x => x.maVe)) };
        _db.GoiSoatVes.Add(package);
        await _db.SaveChangesAsync();
        Response.Headers.CacheControl = "no-store";
        return Ok(KetQuaPhanHoi.ThanhCong(new { phienId, packageId = package.id, thietBiId = session.thietBiSoatVeId, tuyenXeId = routeId, tuyenMienPhi,
            hetHanLuc = package.hetHanLuc.ToString("O"), ves = data, daQuet = used }, "Gói kiểm tra ngoại tuyến"));
    }

    [HttpGet("goi-du-lieu-ngoai-tuyen")]
    public async Task<IActionResult> layGoiDuLieuNgoaiTuyen([FromQuery] int thietBiSoatVeId)
    {
        try
        {
            var duLieu = await _soatVeService.layGoiDuLieuNgoaiTuyen(thietBiSoatVeId);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy gói dữ liệu ngoại tuyến thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpPost("xac-thuc-ve")]
    public async Task<IActionResult> xacThucVe([FromBody] QuetVeRequestDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.maQrPayload))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Dữ liệu mã QR không được để trống", 400));
            }
            var userId = int.Parse(User.FindFirst("id")!.Value);
            if (!await _db.PhienSoatVes.AnyAsync(x => x.id == thongTin.phienSoatVeId && x.nhanVienId == userId && x.thietBiSoatVeId == thongTin.thietBiSoatVeId && x.ketThucLuc == null)) return NotFound();
            thongTin.cheDo = "Online";
            thongTin.thoiDiemQuet = DateTime.UtcNow;
            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = 'SmartBus:OfflineSync', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Cannot acquire synchronization lock', 1;");
            var ketQua = await _soatVeService.xacThucVe(thongTin);
            await transaction.CommitAsync();
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Xử lý xác thực vé hoàn tất"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpPost("tra-cuu-khan-cap")]
    public async Task<IActionResult> traCuuKhancCap([FromBody] TraCuuKhancCapDto thongTin)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _soatVeService.traCuuKhancCap(userId, thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Tra cứu khẩn cấp hoàn tất"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(KetQuaPhanHoi.ThatBai(ex.Message, 404));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai("Lỗi hệ thống: " + ex.Message, 400));
        }
    }

    [HttpPost("xac-nhan-thu-cong")]
    public async Task<IActionResult> xacNhanThuCong([FromBody] XacNhanThuCongDto thongTin)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _soatVeService.xacNhanThuCong(userId, thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Xử lý xác nhận lượt đi hoàn tất"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(KetQuaPhanHoi.ThatBai(ex.Message, 404));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(KetQuaPhanHoi.ThatBai(ex.Message, 409));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai("Lỗi hệ thống: " + ex.Message, 400));
        }
    }

    [HttpPost("dong-bo-lo")]
    public IActionResult dongBoLoNhatKy([FromBody] DongBoLoDto thongTin)
    {
        return StatusCode(410, KetQuaPhanHoi.ThatBai("API đồng bộ cũ đã ngừng do chưa xác minh QR.", 410));
    }

    [HttpGet("canh-bao-gian-lan")]
    public async Task<IActionResult> layDanhSachCanhBaoGianLan()
    {
        try
        {
            var duLieu = await _soatVeService.layDanhSachCanhBaoGianLan();
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách cảnh báo gian lận thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }
}
