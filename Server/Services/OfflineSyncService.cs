using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using Server.Models;

namespace Server.Services;

public record OfflineSyncResult(Guid maSuKien, string trangThai, string maLyDo);

public sealed class OfflineSyncService(SmartBusDbContext db, OfflineQrValidationService verifier)
{
    private static readonly HashSet<string> LyDoTuChoiTaiThietBi = ["DA_SU_DUNG", "GOI_HET_HAN", "KHONG_CO_DU_LIEU", "QR_KHONG_HOP_LE", "SAI_CHU_KY", "SAI_DU_LIEU", "SAI_TOTP", "SAI_TUYEN", "VE_BI_KHOA", "VE_HET_HAN", "SAI_PHAM_VI"];

    public async Task<OfflineSyncResult> XuLy(DongBoSuKienOfflineDto item, int userId)
    {
        if (item.maSuKien == Guid.Empty || item.goiId == Guid.Empty || item.qr.Length > 4096)
            throw new ArgumentException("Dữ liệu sự kiện không hợp lệ.");
        var scanTime = item.thoiDiemQuet.ToUniversalTime();
        var canonical = $"{item.goiId:D}|{scanTime.Ticks}|{item.chapNhanTaiThietBi}|{item.qr}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // One short, transaction-owned lock gives every offline synchronizer the same lock order.
        // Unique database indexes remain the final protection against other writers.
        await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = 'SmartBus:OfflineSync', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Cannot acquire offline synchronization lock', 1;");
        var package = await db.GoiSoatVes.SingleOrDefaultAsync(x => x.id == item.goiId);
        if (package == null) throw new ArgumentException("Không tìm thấy gói soát vé.");
        var session = await db.PhienSoatVes.Include(x => x.chuyenXe).ThenInclude(x => x.loTrinh)
            .SingleOrDefaultAsync(x => x.id == package.phienSoatVeId && x.nhanVienId == userId);
        if (session == null) throw new UnauthorizedAccessException("Phiên soát vé không thuộc tài khoản này.");
        var previous = await db.XuLySuKienOfflines.SingleOrDefaultAsync(x => x.id == item.maSuKien);
        if (previous != null)
        {
            if (previous.goiId != item.goiId || !CryptographicOperations.FixedTimeEquals(previous.bamDuLieu, digest))
                throw new InvalidOperationException("Mã sự kiện đã được dùng với dữ liệu khác.");
            await tx.CommitAsync();
            return new(item.maSuKien, previous.ketQua, previous.maLyDo);
        }
        if (scanTime < package.taoLuc || scanTime >= package.hetHanLuc || scanTime > DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Thời điểm quét nằm ngoài hạn gói hoặc ở tương lai.");
        var ids = JsonSerializer.Deserialize<HashSet<Guid>>(package.danhSachVeHopLe) ?? new();
        var parts = item.qr.Split(':');
        Guid ticketId = Guid.Empty;
        if (parts.Length == 5) Guid.TryParse(parts[2], out ticketId);
        var ticket = ticketId == Guid.Empty ? null : await db.Ves.Include(x => x.khoaKySo).SingleOrDefaultAsync(x => x.maVe == ticketId);
        var check = ticket == null || !ids.Contains(ticketId) ? new OfflineQrResult(false, "KHONG_CO_DU_LIEU") :
            verifier.Check(ticket, item.qr, scanTime, session.chuyenXe.loTrinh.tuyenXeId);
        if (check.Valid && ticket!.maLoaiVe == ChinhSachTuyen.TheMienPhi && !await ChinhSachTuyen.TuyenMienPhiAsync(db, session.chuyenXe.loTrinh.tuyenXeId))
            check = new OfflineQrResult(false, "SAI_PHAM_VI");
        var accepted = item.chapNhanTaiThietBi && check.Valid;
        var reason = item.chapNhanTaiThietBi ? check.Reason
            : item.maLyDoTaiThietBi != null && LyDoTuChoiTaiThietBi.Contains(item.maLyDoTaiThietBi) ? item.maLyDoTaiThietBi : "THIET_BI_TU_CHOI";
        var stale = accepted && ticket!.trangThai != "HoatDong";
        var duplicate = accepted && (ticket!.maLoaiVe == ChinhSachTuyen.TheMienPhi
            ? await ChinhSachTuyen.DaCoLuotTrongChuyenAsync(db, ticket.id, session.chuyenXeId)
            : await db.LuotDis.AnyAsync(x => x.veId == ticket.id && x.trangThai == "HopLe" &&
                (ticket.maLoaiVe == "VeLuot" || x.buocThoiGian == check.TimeStep)));
        if (duplicate) { accepted = false; reason = "QUET_TRUNG"; }
        else if (stale) reason = "STALE_REVOCATION";
        var sequence = (await db.NhatKyQuetVes.Where(x => x.thietBiSoatVeId == session.thietBiSoatVeId)
            .Select(x => (long?)x.soThuTuThietBi).MaxAsync() ?? 0) + 1;
        var log = new NhatKyQuetVe { maSuKien = item.maSuKien, thietBiSoatVeId = session.thietBiSoatVeId,
            phienSoatVeId = session.id, soThuTuThietBi = sequence, veId = ticket?.id, maVeDocDuoc = ticketId == Guid.Empty ? null : ticketId.ToString(),
            bamDuLieuQr = SHA256.HashData(Encoding.UTF8.GetBytes(item.qr)), buocThoiGian = check.TimeStep,
            phuongThuc = "Qr", cheDo = "Offline", ketQuaTaiThietBi = item.chapNhanTaiThietBi ? "ChapNhan" : "TuChoi",
            maLyDo = reason, thoiDiemQuet = scanTime, thoiDiemNhan = DateTime.UtcNow };
        db.NhatKyQuetVes.Add(log);
        await db.SaveChangesAsync();
        if (accepted)
            db.LuotDis.Add(new LuotDi { nhatKyQuetVeId = log.id, veId = ticket!.id, maLoaiVe = ticket.maLoaiVe,
                buocThoiGian = check.TimeStep, trangThai = "HopLe", congNhanLuc = DateTime.UtcNow });
        if (duplicate || stale)
            db.CanhBaoGianLans.Add(new CanhBaoGianLan { nhatKyQuetVeId = log.id, maCanhBao = reason,
                noiDung = duplicate ? "Phát hiện lượt quét trùng khi đồng bộ." : "Vé bị thu hồi sau khi tải gói ngoại tuyến.", trangThai = "Moi", ngayTao = DateTime.UtcNow });
        var status = accepted ? "ChapNhan" : "TuChoi";
        db.XuLySuKienOfflines.Add(new XuLySuKienOffline { id = item.maSuKien, goiId = item.goiId, bamDuLieu = digest, ketQua = status, maLyDo = reason });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(item.maSuKien, status, reason);
    }
}
