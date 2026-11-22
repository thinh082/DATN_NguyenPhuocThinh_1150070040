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
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách người dùng thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy người dùng", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy chi tiết người dùng thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpPost("luu-nguoi-dung")]
    public async Task<IActionResult> luuNguoiDung([FromBody] NguoiDungDto thongTin)
    {
        try
        {
            if (thongTin == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Dữ liệu truyền vào không hợp lệ", 400));
            }

            var ketQua = await _nguoiDungService.luuNguoiDung(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy người dùng để cập nhật", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Lưu người dùng thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy người dùng", 404));
            }

            string thongBao = trangThai ? "Mở khóa tài khoản thành công" : "Khóa tài khoản thành công";
            return Ok(KetQuaPhanHoi.ThanhCong(new { id, trangThai }, thongBao));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }
}
