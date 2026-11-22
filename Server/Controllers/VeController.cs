using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Server.Commons;
using Server.Models;
using Server.Services;
using Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VeController : ControllerBase
{
    private readonly IVeService _veService;
    private readonly SmartBusDbContext _db;

    public VeController(IVeService veService, SmartBusDbContext db)
    {
        _veService = veService;
        _db = db;
    }

    [HttpGet("danh-sach-bieu-gia")]
    public async Task<IActionResult> layDanhSachBieuGia([FromQuery] int? tuyenXeId, [FromQuery] bool chiTinhPhi = false)
    {
        try
        {
            var duLieu = await _veService.layDanhSachBieuGia(tuyenXeId, chiTinhPhi);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách biểu giá thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpGet("the-mien-phi")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> layTheMienPhi()
    {
        try
        {
            var duLieu = await _veService.layHoacCapTheMienPhi(int.Parse(User.FindFirst("id")!.Value));
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy thẻ đi xe miễn phí thành công"));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, KetQuaPhanHoi.ThatBai(ex.Message, 403)); }
        catch (InvalidOperationException ex) { return Conflict(KetQuaPhanHoi.ThatBai(ex.Message, 409)); }
        catch (Exception ex) { return Ok(KetQuaPhanHoi.LoiHeThong(ex)); }
    }

    [HttpPost("tao-don-hang")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> taoDonHang([FromBody] TaoDonHangDto thongTin)
    {
        try
        {
            thongTin.nguoiDungId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _veService.taoDonHang(thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Tạo đơn hàng thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(KetQuaPhanHoi.ThatBai(ex.Message, 409));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(KetQuaPhanHoi.ThatBai(ex.Message, 404));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }

    [HttpPost("thanh-toan-gia-lap")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> thanhToanGiaLap([FromBody] ThanhToanGiaLapDto thongTin)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _veService.thanhToanGiaLap(userId, thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Xử lý thanh toán mô phỏng thành công"));
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
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }

    [HttpPost("thanh-toan-don-hang")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> thanhToanDonHang([FromBody] ThanhToanDonHangDto thongTin)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            if (!await _db.DonHangs.AnyAsync(x => x.id == thongTin.donHangId && x.nguoiDungId == userId)) return NotFound();
            var ketQua = await _veService.thanhToanDonHang(thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Thanh toán đơn hàng thành công"));
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
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }

    [HttpGet("vi-ve-cua-toi")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> layDanhSachVeNguoiDung([FromQuery] int nguoiDungId)
    {
        try
        {
            var duLieu = await _veService.layDanhSachVeNguoiDung(int.Parse(User.FindFirst("id")!.Value));
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách vé điện tử thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpGet("lich-su-luot-di")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> lichSuLuotDi()
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);
        return Ok(KetQuaPhanHoi.ThanhCong(await _veService.layLichSuLuotDi(userId)));
    }

    [HttpGet("chi-tiet-qr-dong")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> layChiTietVeQrDong([FromQuery] Guid maVe)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            if (!await _db.Ves.AnyAsync(x => x.maVe == maVe && x.nguoiDungId == userId)) return NotFound();
            var duLieu = await _veService.layChiTietVeQrDong(maVe);
            if (duLieu == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy thông tin vé", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy mã QR động thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpPost("khoa-ve")]
    [Authorize(Roles = "NhanVienSoatVe,QuanTriVien")]
    public async Task<IActionResult> khoaVe([FromBody] KhoaVeDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.lyDo))
            {
                return BadRequest(KetQuaPhanHoi.ThatBai("Lý do khóa vé là bắt buộc.", 400));
            }
            if (!User.IsInRole("QuanTriVien"))
            {
                thongTin.thuHoiVinhVien = false;
            }
            thongTin.nguoiThucHienId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _veService.khoaVe(thongTin);
            if (!ketQua)
            {
                return NotFound(KetQuaPhanHoi.ThatBai("Không tìm thấy vé để khóa", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(new { thongTin.maVe }, "Khóa vé thành công"));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.LoiHeThong(ex, 400));
        }
    }
}
