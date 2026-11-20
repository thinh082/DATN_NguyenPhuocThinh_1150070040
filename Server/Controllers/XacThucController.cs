using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Server.Commons;
using Server.Models;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class XacThucController : ControllerBase
{
    private readonly IXacThucService _xacThucService;

    public XacThucController(IXacThucService xacThucService)
    {
        _xacThucService = xacThucService;
    }

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> dangNhap([FromBody] DangNhapDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.taiKhoan) || string.IsNullOrWhiteSpace(thongTin.matKhau))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Tai khoan va mat khau khong duoc de trong", 400));
            }

            var ketQua = await _xacThucService.dangNhap(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Tai khoan hoac mat khau khong chinh xac", 401));
            }

            if (ketQua.GetType().GetProperty("biKhoa") != null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Tai khoan da bi khoa, vui long lien he quan tri vien", 403));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Dang nhap thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> dangKy([FromBody] DangKyDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.hoTen) || string.IsNullOrWhiteSpace(thongTin.matKhau))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Ho ten va mat khau la bat buoc", 400));
            }

            var ketQua = await _xacThucService.dangKy(thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Dang ky tai khoan thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }

    [HttpGet("thong-tin-ca-nhan")]
    [Authorize]
    public async Task<IActionResult> layThongTinCaNhan([FromQuery] int nguoiDungId)
    {
        try
        {
            var duLieu = await _xacThucService.layThongTinCaNhan(int.Parse(User.FindFirst("id")!.Value));
            if (duLieu == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay nguoi dung", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay thong tin ca nhan thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPut("thong-tin-ca-nhan")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> capNhatCaNhan([FromBody] CapNhatCaNhanDto thongTin)
    {
        try { return Ok(KetQuaPhanHoi.ThanhCong(await _xacThucService.capNhatThongTin(int.Parse(User.FindFirst("id")!.Value), thongTin), "Cap nhat thanh cong")); }
        catch (ArgumentException ex) { return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400)); }
    }

    [HttpGet("nhom-uu-tien")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> layNhomUuTien() => Ok(KetQuaPhanHoi.ThanhCong(await _xacThucService.layNhomUuTien()));

    [HttpGet("ho-so-cua-toi")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> layHoSoCuaToi() => Ok(KetQuaPhanHoi.ThanhCong(await _xacThucService.layHoSoCuaToi(int.Parse(User.FindFirst("id")!.Value))));

    [HttpPost("nop-ho-so-uu-tien")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> nopHoSoUuTien([FromBody] HoSoUuTienDto hoSo)
    {
        try
        {
            hoSo.nguoiDungId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _xacThucService.nopHoSoUuTien(hoSo);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Nop ho so uu tien thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }

    [HttpGet("danh-sach-ho-so-cho-duyet")]
    [Authorize(Roles = "QuanTriVien")]
    public async Task<IActionResult> layDanhSachHoSoChoDuyet()
    {
        try
        {
            var duLieu = await _xacThucService.layDanhSachHoSoChoDuyet();
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lay danh sach ho so cho duyet thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("duyet-ho-so-uu-tien")]
    [Authorize(Roles = "QuanTriVien")]
    public async Task<IActionResult> duyetHoSoUuTien([FromBody] JsonElement duLieu)
    {
        try
        {
            int hoSoId = duLieu.GetProperty("hoSoId").GetInt32();
            bool chapNhan = duLieu.GetProperty("chapNhan").GetBoolean();
            int nguoiDuyetId = int.Parse(User.FindFirst("id")!.Value);
            string? lyDo = duLieu.TryGetProperty("lyDo", out JsonElement lyDoProp) ? lyDoProp.GetString() : null;

            var ketQua = await _xacThucService.duyetHoSoUuTien(hoSoId, chapNhan, nguoiDuyetId, lyDo);
            if (!ketQua)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Khong tim thay ho so de duyet", 404));
            }

            string thongBao = chapNhan ? "Phe duyet ho so uu tien thanh cong" : "Tu choi ho so uu tien thanh cong";
            return Ok(KetQuaPhanHoi.ThanhCong(new { hoSoId, chapNhan }, thongBao));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpPost("tai-len-minh-chung")]
    [Authorize(Roles = "HanhKhach")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> taiLenMinhChung(IFormFile tep)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _xacThucService.taiLenMinhChung(userId, tep);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Tai len tep minh chung thanh cong"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
        catch (Exception ex)
        {
            return StatusCode(500, KetQuaPhanHoi.ThatBai("Loi he thong khi tai len tep: " + ex.Message, 500));
        }
    }

    [HttpGet("minh-chung/{maTep}")]
    [Authorize]
    public async Task<IActionResult> layMinhChung(Guid maTep)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var (duongDanVatLy, loaiFile, tenFileGoc) = await _xacThucService.layTepMinhChung(maTep, userId, role);
            return PhysicalFile(duongDanVatLy, loaiFile, tenFileGoc, enableRangeProcessing: true);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(KetQuaPhanHoi.ThatBai(ex.Message, 404));
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(KetQuaPhanHoi.ThatBai(ex.Message, 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, KetQuaPhanHoi.ThatBai(ex.Message, 403));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }
}
