using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Server.Commons;
using Server.Models;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Authorize(Roles = "QuanTriVien")]
[Route("api/[controller]")]
public class NguoiDungController : ControllerBase
{
    private readonly INguoiDungService _nguoiDungService;

    public NguoiDungController(INguoiDungService nguoiDungService)
    {
        _nguoiDungService = nguoiDungService;
    }

    [HttpGet("lay-danh-sach")]
    public async Task<IActionResult> layDanhSach()
    {
        try
        {
            var duLieu = await _nguoiDungService.layDanhSachNguoiDung();
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay danh sach nguoi dung thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpGet("lay-chi-tiet")]
    public async Task<IActionResult> layChiTiet(int id)
    {
        try
        {
            var duLieu = await _nguoiDungService.layChiTietNguoiDung(id);
            if (duLieu == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay nguoi dung", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay chi tiet nguoi dung thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("luu-nguoi-dung")]
    public async Task<IActionResult> luuNguoiDung([FromBody] NguoiDungDto thongTin)
    {
        try
        {
            if (thongTin == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Du lieu truyen vao khong hop le", 400));
            }

            var ketQua = await _nguoiDungService.luuNguoiDung(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay nguoi dung de cap nhat", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Luu nguoi dung thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("khoa-tai-khoan")]
    public async Task<IActionResult> khoaTaiKhoan([FromBody] dynamic duLieu)
    {
        try
        {
            int id = (int)duLieu.GetProperty("id").GetInt32();
            bool trangThai = duLieu.GetProperty("trangThai").GetBoolean();

            var ketQua = await _nguoiDungService.capNhatTrangThai(id, trangThai);
            if (!ketQua)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay nguoi dung", 404));
            }

            string thongBao = trangThai ? "Mo khoa tai khoan thanh cong" : "Khoa tai khoan thanh cong";
            return Ok(KetQuaPhanHoi.ThanhCong(new { id, trangThai }, thongBao));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }
}
