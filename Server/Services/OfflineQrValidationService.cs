using Server.Entities;

namespace Server.Services;

public record OfflineQrResult(bool Valid, string Reason, long? TimeStep = null);

public sealed class OfflineQrValidationService(IMatMaService crypto)
{
    public OfflineQrResult Check(Ve ticket, string qr, DateTime scannedAt, int routeId)
    {
        if (qr.Length > 4096) return new(false, "QR_KHONG_HOP_LE");
        var parts = qr.Split(':');
        if (parts.Length != 5 || parts[0] != "SMARTBUS" || parts[1] != "v1" ||
            !Guid.TryParse(parts[2], out var id) || id != ticket.maVe || parts[3].Length != 8 || !parts[3].All(char.IsAsciiDigit))
            return new(false, "QR_KHONG_HOP_LE");
        if (scannedAt < ticket.hieuLucTu || scannedAt >= ticket.hetHanLuc) return new(false, "VE_HET_HAN");
        if (ticket.tuyenXeId != null && ticket.tuyenXeId != routeId) return new(false, "SAI_TUYEN");
        byte[] signature;
        try { signature = Convert.FromBase64String(parts[4]); }
        catch (FormatException) { return new(false, "SAI_CHU_KY"); }
        if (!crypto.XacThucChuKy(ticket.duLieuDaKy, signature, ticket.khoaKySo.khoaCongKhaiPem)) return new(false, "SAI_CHU_KY");
        var secret = crypto.GiaiMaAes(ticket.biMatTotpMaHoa);
        var current = crypto.TinhBuocThoiGian(scannedAt, ticket.chuKyTotpGiay);
        foreach (var step in new[] { current, current - 1, current + 1 })
        {
            if (step < 0) continue;
            var instant = DateTimeOffset.FromUnixTimeSeconds(step * ticket.chuKyTotpGiay).UtcDateTime;
            if (crypto.TinhMaTotp(secret, instant, ticket.chuKyTotpGiay, ticket.soChuSoTotp) == parts[3]) return new(true, "HOP_LE", step);
        }
        return new(false, "SAI_TOTP");
    }
}
