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
    public async Task<IActionResult> layDanhSachBieuGia([FromQuery] int? tuyenXeId)
    {
        try
        {
            var duLieu = await _veService.layDanhSachBieuGia(tuyenXeId);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay danh sach bieu gia thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("tao-don-hang")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> taoDonHang([FromBody] TaoDonHangDto thongTin)
    {
        try
        {
            thongTin.nguoiDungId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _veService.taoDonHang(thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Tao don hang thanh cong"));
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
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Xu ly thanh toan mo phong thanh cong"));
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
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Thanh toan don hang thanh cong"));
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
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay danh sach ve dien tu thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
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
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay thong tin ve", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay ma QR dong thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
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
                return NotFound(KetQuaPhanHoi.ThatBai("Khong tim thay ve de khoa", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(new { thongTin.maVe }, "Khoa ve thanh cong"));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 400));
        }
    }
}
