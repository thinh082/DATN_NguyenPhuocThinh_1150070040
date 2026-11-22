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
    public async Task<IActionResult> layDanhSachTuyenXe([FromQuery] string? loc = null)
    {
        try
        {
            var duLieu = await _vanHanhService.layDanhSachTuyenXe(loc);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách tuyến xe thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy tuyến xe", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy chi tiết tuyến xe thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
                return Ok(KetQuaPhanHoi.ThatBai("Mã tuyến và tên tuyến không được để trống", 400));
            }

            var ketQua = await _vanHanhService.luuTuyenXe(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy tuyến xe để cập nhật", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Lưu tuyến xe thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpGet("danh-sach-tram-dung")]
    public async Task<IActionResult> layDanhSachTramDung([FromQuery] int? tuyenXeId)
    {
        try
        {
            var duLieu = await _vanHanhService.layDanhSachTramDung(tuyenXeId);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách trạm dừng thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
                return Ok(KetQuaPhanHoi.ThatBai("Mã trạm và tên trạm không được để trống", 400));
            }

            var ketQua = await _vanHanhService.luuTramDung(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy trạm dừng để cập nhật", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Lưu trạm dừng thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpGet("danh-sach-xe-buyt")]
    public async Task<IActionResult> layDanhSachXeBuyt()
    {
        try
        {
            var duLieu = await _vanHanhService.layDanhSachXeBuyt();
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách xe buýt thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
                return Ok(KetQuaPhanHoi.ThatBai("Biển số xe không được để trống", 400));
            }

            var ketQua = await _vanHanhService.luuXeBuyt(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy xe buýt để cập nhật", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Lưu xe buýt thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Bắt đầu phiên soát vé thành công"));
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
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy phiên soát vé", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(new { phienId }, "Kết thúc phiên soát vé thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }
}
