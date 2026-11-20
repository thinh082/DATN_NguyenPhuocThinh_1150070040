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
public class VanHanhController : ControllerBase
{
    private readonly IVanHanhService _vanHanhService;
    private readonly SmartBusDbContext _db;

    public VanHanhController(IVanHanhService vanHanhService, SmartBusDbContext db)
    {
        _vanHanhService = vanHanhService;
        _db = db;
    }

    [HttpGet("danh-sach-tuyen")]
    public async Task<IActionResult> layDanhSachTuyenXe()
    {
        try
        {
            var duLieu = await _vanHanhService.layDanhSachTuyenXe();
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay danh sach tuyen xe thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpGet("chi-tiet-tuyen")]
    public async Task<IActionResult> layChiTietTuyenXe([FromQuery] int id)
    {
        try
        {
            var duLieu = await _vanHanhService.layChiTietTuyenXe(id);
            if (duLieu == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay tuyen xe", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay chi tiet tuyen xe thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("luu-tuyen-xe")]
    [Authorize(Roles = "QuanTriVien")]
    public async Task<IActionResult> luuTuyenXe([FromBody] TuyenXeDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.maTuyen) || string.IsNullOrWhiteSpace(thongTin.tenTuyen))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Ma tuyen va ten tuyen khong duoc de trong", 400));
            }

            var ketQua = await _vanHanhService.luuTuyenXe(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay tuyen xe de cap nhat", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Luu tuyen xe thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpGet("danh-sach-tram-dung")]
    public async Task<IActionResult> layDanhSachTramDung([FromQuery] int? tuyenXeId)
    {
        try
        {
            var duLieu = await _vanHanhService.layDanhSachTramDung(tuyenXeId);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay danh sach tram dung thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("luu-tram-dung")]
    [Authorize(Roles = "QuanTriVien")]
    public async Task<IActionResult> luuTramDung([FromBody] TramDungDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.maTram) || string.IsNullOrWhiteSpace(thongTin.tenTram))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Ma tram va ten tram khong duoc de trong", 400));
            }

            var ketQua = await _vanHanhService.luuTramDung(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay tram dung de cap nhat", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Luu tram dung thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpGet("danh-sach-xe-buyt")]
    public async Task<IActionResult> layDanhSachXeBuyt()
    {
        try
        {
            var duLieu = await _vanHanhService.layDanhSachXeBuyt();
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay danh sach xe buyt thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("luu-xe-buyt")]
    [Authorize(Roles = "QuanTriVien")]
    public async Task<IActionResult> luuXeBuyt([FromBody] XeBuytDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.bienSo))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Bien so xe khong duoc de trong", 400));
            }

            var ketQua = await _vanHanhService.luuXeBuyt(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay xe buyt de cap nhat", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Luu xe buyt thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpGet("quet-qr-dan-xe")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> layThongTinQrDanXe([FromQuery] Guid maQrTinh)
    {
        try
        {
            if (maQrTinh == Guid.Empty)
            {
                return BadRequest(KetQuaPhanHoi.ThatBai("Mã QR phương tiện không hợp lệ.", 400));
            }

            var duLieu = await _vanHanhService.layThongTinQrDanXe(maQrTinh);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Nhận diện xe buýt thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(KetQuaPhanHoi.ThatBai(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(KetQuaPhanHoi.ThatBai(ex.Message, 409));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }

    [HttpPost("bat-dau-phien-soat-ve")]
    [Authorize(Roles = "NhanVienSoatVe,QuanTriVien")]
    public async Task<IActionResult> batDauPhienSoatVe([FromBody] BatDauPhienDto thongTin)
    {
        try
        {
            thongTin.nhanVienId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _vanHanhService.batDauPhienSoatVe(thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Bat dau phien soat ve thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }

    [HttpPost("ket-thuc-phien-soat-ve")]
    [Authorize(Roles = "NhanVienSoatVe,QuanTriVien")]
    public async Task<IActionResult> ketThucPhienSoatVe([FromBody] dynamic duLieu)
    {
        try
        {
            Guid phienId = Guid.Parse(duLieu.GetProperty("phienId").GetString());
            var userId = int.Parse(User.FindFirst("id")!.Value);
            if (!User.IsInRole("QuanTriVien") && !await _db.PhienSoatVes.AnyAsync(x => x.id == phienId && x.nhanVienId == userId)) return NotFound();
            var ketQua = await _vanHanhService.ketThucPhienSoatVe(phienId);
            if (!ketQua)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay phien soat ve", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(new { phienId }, "Ket thuc phien soat ve thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }
}
