using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;

namespace Server.Services;

public static class ChinhSachTuyen
{
    public const string TheMienPhi = "TheMienPhi";

    public static DateOnly HomNay => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    public static Expression<Func<TuyenXe, bool>> LaMienPhi(DateOnly ngay) =>
        x => x.chinhSachMienPhi != null && x.chinhSachMienPhi.trangThai && x.chinhSachMienPhi.hieuLucTu <= ngay && x.chinhSachMienPhi.hieuLucDen >= ngay;

    public static Task<bool> TuyenMienPhiAsync(SmartBusDbContext db, int tuyenXeId)
    {
        var ngay = HomNay;
        return db.TuyenXes.Where(x => x.id == tuyenXeId).Where(LaMienPhi(ngay)).AnyAsync();
    }

    public static Task<bool> DaCoLuotTrongChuyenAsync(SmartBusDbContext db, long veId, long chuyenXeId) =>
        db.LuotDis.AnyAsync(x => x.veId == veId && x.trangThai == "HopLe" && x.NhatKyQuetVe.PhienSoatVe.chuyenXeId == chuyenXeId);
}
