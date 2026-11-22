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
    private readonly IVeService _veService;

    public XacThucController(IXacThucService xacThucService, IVeService veService)
    {
        _xacThucService = xacThucService;
        _veService = veService;
    }

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> dangNhap([FromBody] DangNhapDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.taiKhoan) || string.IsNullOrWhiteSpace(thongTin.matKhau))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Tài khoản và mật khẩu không được để trống", 400));
            }

            var ketQua = await _xacThucService.dangNhap(thongTin);
            if (ketQua == null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Tài khoản hoặc mật khẩu không chính xác", 401));
            }

            if (ketQua.GetType().GetProperty("biKhoa") != null)
            {
                return Ok(KetQuaPhanHoi.ThatBai("Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên", 403));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Đăng nhập thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> dangKy([FromBody] DangKyDto thongTin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thongTin.hoTen) || string.IsNullOrWhiteSpace(thongTin.matKhau))
            {
                return Ok(KetQuaPhanHoi.ThatBai("Họ tên và mật khẩu là bắt buộc", 400));
            }

            var ketQua = await _xacThucService.dangKy(thongTin);
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Đăng ký tài khoản thành công"));
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
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy người dùng", 404));
            }

            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy thông tin cá nhân thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }

    [HttpPut("thong-tin-ca-nhan")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> capNhatCaNhan([FromBody] CapNhatCaNhanDto thongTin)
    {
        try { return Ok(KetQuaPhanHoi.ThanhCong(await _xacThucService.capNhatThongTin(int.Parse(User.FindFirst("id")!.Value), thongTin), "Cập nhật thành công")); }
        catch (ArgumentException ex) { return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400)); }
    }

    [HttpGet("nhom-uu-tien")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> layNhomUuTien() => Ok(KetQuaPhanHoi.ThanhCong(await _xacThucService.layNhomUuTien()));

    [HttpGet("dia-chi/tinh-thanh")]
    [Authorize]
    public async Task<IActionResult> layTinhThanh() => Ok(KetQuaPhanHoi.ThanhCong(await _xacThucService.layTinhThanh()));

    [HttpGet("dia-chi/phuong-xa")]
    [Authorize]
    public async Task<IActionResult> layPhuongXa([FromQuery] int maTinhThanh) => Ok(KetQuaPhanHoi.ThanhCong(await _xacThucService.layPhuongXa(maTinhThanh)));

    [HttpPost("xac-thuc-dinh-danh")]
    [Authorize(Roles = "HanhKhach")]
    public async Task<IActionResult> xacThucDinhDanh([FromBody] XacThucDinhDanhDto dto)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var ketQua = await _xacThucService.xacThucDinhDanh(userId, dto);
            try { await _veService.layHoacCapTheMienPhi(userId); }
            catch (Exception ex) { Console.Error.WriteLine($"[CanhBao] Chua cap duoc the mien phi cho nguoi dung {userId}: {ex.Message}"); }
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Xác thực định danh thành công"));
        }
        catch (ArgumentException ex) { return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400)); }
        catch (UnauthorizedAccessException ex) { return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400)); }
    }

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
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Nộp hồ sơ ưu tiên thành công"));
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
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Lấy danh sách hồ sơ chờ duyệt thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
                return Ok(KetQuaPhanHoi.ThatBai("Không tìm thấy hồ sơ để duyệt", 404));
            }

            string thongBao = chapNhan ? "Phê duyệt hồ sơ ưu tiên thành công" : "Từ chối hồ sơ ưu tiên thành công";
            return Ok(KetQuaPhanHoi.ThanhCong(new { hoSoId, chapNhan }, thongBao));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
            return Ok(KetQuaPhanHoi.ThanhCong(ketQua, "Tải lên tệp minh chứng thành công"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
        catch (IOException)
        {
            return StatusCode(502, KetQuaPhanHoi.ThatBai("Không lưu được tệp minh chứng.", 502));
        }
        catch (Exception ex)
        {
            return StatusCode(500, KetQuaPhanHoi.LoiHeThong(ex));
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
            var (noiDung, loaiFile, tenFileGoc) = await _xacThucService.layTepMinhChung(maTep, userId, role);
            return File(noiDung, loaiFile, tenFileGoc, enableRangeProcessing: true);
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
        catch (IOException)
        {
            return StatusCode(502, KetQuaPhanHoi.ThatBai("Không đọc được tệp minh chứng.", 502));
        }
        catch (Exception ex)
        {
            return BadRequest(KetQuaPhanHoi.ThatBai(ex.Message, 400));
        }
    }
}
