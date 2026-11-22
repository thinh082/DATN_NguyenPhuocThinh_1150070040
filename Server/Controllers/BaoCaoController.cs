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
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Thống kê lượt đi theo tuyến thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Thống kê theo nhóm đối tượng thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
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
            return Ok(KetQuaPhanHoi.ThanhCong(duLieu, "Thống kê doanh thu thành công"));
        }
        catch (Exception ex)
        {
            return Ok(KetQuaPhanHoi.LoiHeThong(ex));
        }
    }
}
