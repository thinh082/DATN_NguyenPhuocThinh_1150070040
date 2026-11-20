using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Server.Commons;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Authorize(Roles = "QuanTriVien")]
[Route("api/[controller]")]
public class BaoCaoController : ControllerBase
{
    private readonly IBaoCaoService _baoCaoService;

    public BaoCaoController(IBaoCaoService baoCaoService)
    {
        _baoCaoService = baoCaoService;
    }

    [HttpGet("luot-di-theo-tuyen")]
    public async Task<IActionResult> layLuotDiTheoTuyen([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        try
        {
            var batDau = tuNgay ?? DateTime.UtcNow.AddMonths(-1);
            var ketThuc = denNgay ?? DateTime.UtcNow;

            var duLieu = await _baoCaoService.thongKeLuotDiTheoTuyen(batDau, ketThuc);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Thong ke luot di theo tuyen thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpGet("theo-nhom-doi-tuong")]
    public async Task<IActionResult> layThongKeTheoNhom([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        try
        {
            var batDau = tuNgay ?? DateTime.UtcNow.AddMonths(-1);
            var ketThuc = denNgay ?? DateTime.UtcNow;

            var duLieu = await _baoCaoService.thongKeTheoNhomDoiTuong(batDau, ketThuc);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Thong ke theo nhom doi tuong thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }

    [HttpGet("doanh-thu")]
    public async Task<IActionResult> layThongKeDoanhThu([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        try
        {
            var batDau = tuNgay ?? DateTime.UtcNow.AddMonths(-1);
            var ketThuc = denNgay ?? DateTime.UtcNow;

            var duLieu = await _baoCaoService.thongKeDoanhThu(batDau, ketThuc);
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Thong ke doanh thu thanh cong"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.ThatBai("Loi he thong: " + ex.Message, 500));
        }
    }
}
