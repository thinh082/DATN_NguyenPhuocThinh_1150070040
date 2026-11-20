using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Services;

public interface IBaoCaoService
{
    Task<dynamic> thongKeLuotDiTheoTuyen(DateTime tuNgay, DateTime denNgay);
    Task<dynamic> thongKeTheoNhomDoiTuong(DateTime tuNgay, DateTime denNgay);
    Task<dynamic> thongKeDoanhThu(DateTime tuNgay, DateTime denNgay);
}

public class BaoCaoService : IBaoCaoService
{
    private readonly SmartBusDbContext _context;

    public BaoCaoService(SmartBusDbContext context)
    {
        _context = context;
    }

    public async Task<dynamic> thongKeLuotDiTheoTuyen(DateTime tuNgay, DateTime denNgay)
    {
        var luotDis = await _context.LuotDis
            .AsNoTracking()
            .Where(x => x.trangThai == "HopLe" && x.congNhanLuc >= tuNgay && x.congNhanLuc <= denNgay)
            .Include(x => x.Ve)
                .ThenInclude(v => v.tuyenXe)
            .ToListAsync();

        var ketQua = luotDis
            .GroupBy(x => new
            {
                maTuyen = x.Ve.tuyenXe != null ? x.Ve.tuyenXe.maTuyen : "TOAN_MANG",
                tenTuyen = x.Ve.tuyenXe != null ? x.Ve.tuyenXe.tenTuyen : "Toan mang luoi"
            })
            .Select(g => new
            {
                g.Key.maTuyen,
                g.Key.tenTuyen,
                tongSoLuotDi = g.Count(),
                soLuotVeLuot = g.Count(x => x.maLoaiVe == "VeLuot"),
                soLuotVeThang = g.Count(x => x.maLoaiVe == "VeThang")
            })
            .ToList();

        return ketQua;
    }

    public async Task<dynamic> thongKeTheoNhomDoiTuong(DateTime tuNgay, DateTime denNgay)
    {
        var luotDis = await _context.LuotDis
            .AsNoTracking()
            .Where(x => x.trangThai == "HopLe" && x.congNhanLuc >= tuNgay && x.congNhanLuc <= denNgay)
            .Include(x => x.Ve)
                .ThenInclude(v => v.nhomDoiTuong)
            .ToListAsync();

        var ketQua = luotDis
            .GroupBy(x => new
            {
                x.Ve.nhomDoiTuongId,
                tenNhom = x.Ve.nhomDoiTuong.tenNhom
            })
            .Select(g => new
            {
                g.Key.nhomDoiTuongId,
                g.Key.tenNhom,
                tongSoLuot = g.Count(),
                tyLePhanTram = luotDis.Count > 0 ? Math.Round((double)g.Count() / luotDis.Count * 100, 2) : 0
            })
            .ToList();

        return new
        {
            tongSoLuotDiToanHeThong = luotDis.Count,
            chiTietNhom = ketQua
        };
    }

    public async Task<dynamic> thongKeDoanhThu(DateTime tuNgay, DateTime denNgay)
    {
        var donHangs = await _context.DonHangs
            .AsNoTracking()
            .Where(x => (x.trangThai == "DaThanhToan" || x.trangThai == "MienPhi") && x.ngayTao >= tuNgay && x.ngayTao <= denNgay)
            .ToListAsync();

        var tongGiaGoc = donHangs.Sum(x => x.tongGiaGoc);
        var tongTroGiaMienGiam = donHangs.Sum(x => x.tongMienGiam);
        var tongDoanhThuThucThu = tongGiaGoc - tongTroGiaMienGiam;

        return new
        {
            tongSoDonHang = donHangs.Count,
            tongGiaGoc,
            tongTroGiaMienGiam,
            tongDoanhThuThucThu
        };
    }
}
