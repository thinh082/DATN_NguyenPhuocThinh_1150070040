using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using Server.Models;

namespace Server.Services;

public interface ISoatVeService
{
    Task<dynamic> layGoiDuLieuNgoaiTuyen(int thietBiSoatVeId);
    Task<dynamic> xacThucVe(QuetVeRequestDto thongTin);
    Task<dynamic> traCuuKhancCap(TraCuuKhancCapDto thongTin);
    Task<dynamic> traCuuKhancCap(int userId, TraCuuKhancCapDto thongTin);
    Task<dynamic> xacNhanThuCong(int userId, XacNhanThuCongDto thongTin);
    Task<dynamic> dongBoLoNhatKy(DongBoLoDto thongTin);
    Task<dynamic> layDanhSachCanhBaoGianLan();
}

public class SoatVeService : ISoatVeService
{
    private readonly SmartBusDbContext _context;
    private readonly IMatMaService _matMaService;

    public SoatVeService(SmartBusDbContext context, IMatMaService matMaService)
    {
        _context = context;
        _matMaService = matMaService;
    }

    public async Task<dynamic> layGoiDuLieuNgoaiTuyen(int thietBiSoatVeId)
    {
        var thietBi = await _context.ThietBiSoatVes.FindAsync(thietBiSoatVeId);
        if (thietBi == null) throw new Exception("Khong tim thay thiet bi soat ve");

        var khoaKySo = await _context.KhoaKySos
            .Where(x => x.trangThai == "DangDung")
            .OrderByDescending(x => x.id)
            .FirstOrDefaultAsync();

        var danhSachThuHoi = await _context.NhatKyThuHoiVes
            .Where(x => x.id > thietBi.mocThuHoiDaNhan)
            .OrderBy(x => x.id)
            .Select(x => new
            {
                x.id,
                x.veId,
                maVe = x.ve.maVe,
                x.hanhDong,
                x.lyDo,
                x.ngayTao
            })
            .ToListAsync();

        var mocThuHoiMoi = danhSachThuHoi.Any() ? danhSachThuHoi.Max(x => x.id) : thietBi.mocThuHoiDaNhan;
        thietBi.mocThuHoiDaNhan = mocThuHoiMoi;
        thietBi.lanDongBoCuoi = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var danhSachTuyen = await _context.TuyenXes.Where(x => x.trangThai).ToListAsync();

        return new
        {
            thietBiId = thietBi.id,
            khoaKySo = khoaKySo == null ? null : new
            {
                khoaKySo.maKhoa,
                khoaKySo.thuatToan,
                khoaKySo.khoaCongKhaiPem,
                khoaKySo.hieuLucTu,
                khoaKySo.hetHanLuc
            },
            mocThuHoiHienTai = mocThuHoiMoi,
            danhSachThuHoi,
            danhSachTuyen = danhSachTuyen.Select(t => new { t.id, t.maTuyen, t.tenTuyen })
        };
    }

    public async Task<dynamic> xacThucVe(QuetVeRequestDto thongTin)
    {
        var parts = thongTin.maQrPayload.Split(':');
        if (parts.Length != 5 || parts[0] != "SMARTBUS" || parts[1] != "v1")
        {
            return await GhiNhanQuetThatBai(thongTin, "MA_QR_KHONG_DUNG_DINH_DANG", "Mã QR không đúng định dạng SmartBus");
        }

        if (!Guid.TryParse(parts[2], out var maVe))
        {
            return await GhiNhanQuetThatBai(thongTin, "MA_VE_KHONG_HOP_LE", "Mã vé không hợp lệ");
        }

        string maTotp = parts[3];
        if (maTotp.Length != 8 || !maTotp.All(char.IsAsciiDigit))
        {
            return await GhiNhanQuetThatBai(thongTin, "MA_QR_KHONG_DUNG_DINH_DANG", "Mã TOTP không hợp lệ");
        }

        byte[] chuKyBytes;
        try
        {
            chuKyBytes = Convert.FromBase64String(parts[4]);
        }
        catch
        {
            return await GhiNhanQuetThatBai(thongTin, "CHU_KY_HONG", "Chữ ký số bị hỏng");
        }

        var ve = await _context.Ves
            .Include(x => x.nguoiDung)
            .Include(x => x.khoaKySo)
            .Include(x => x.maLoaiVeNavigation)
            .Include(x => x.nhomDoiTuong)
            .FirstOrDefaultAsync(x => x.maVe == maVe);

        if (ve == null)
        {
            return await GhiNhanQuetThatBai(thongTin, "VE_KHONG_TON_TAI", "Vé không tồn tại trong hệ thống");
        }

        if (ve.trangThai != "HoatDong")
        {
            return await GhiNhanQuetThatBai(thongTin, "VE_DA_BI_KHOA", $"Vé đã bị {ve.trangThai}");
        }

        var now = DateTime.UtcNow;
        if (now < ve.hieuLucTu || now >= ve.hetHanLuc)
        {
            return await GhiNhanQuetThatBai(thongTin, "VE_HET_HAN", "Vé đã hết hạn sử dụng");
        }

        var session = await _context.PhienSoatVes
            .Include(p => p.chuyenXe).ThenInclude(cx => cx.loTrinh)
            .FirstOrDefaultAsync(p => p.id == thongTin.phienSoatVeId);
        if (session != null && ve.tuyenXeId != null && ve.tuyenXeId != session.chuyenXe.loTrinh.tuyenXeId)
        {
            return await GhiNhanQuetThatBai(thongTin, "SAI_TUYEN", "Vé không áp dụng cho tuyến xe này");
        }

        bool chuKyHopLe = _matMaService.XacThucChuKy(ve.duLieuDaKy, chuKyBytes, ve.khoaKySo.khoaCongKhaiPem);
        if (!chuKyHopLe)
        {
            return await GhiNhanQuetThatBai(thongTin, "CHU_KY_KHONG_KHOP", "Chữ ký mật mã không hợp lệ (nghi vấn vé giả)");
        }

        var biMat = _matMaService.GiaiMaAes(ve.biMatTotpMaHoa);
        var currentStep = _matMaService.TinhBuocThoiGian(now, ve.chuKyTotpGiay);
        long? matchedStep = null;
        foreach (var step in new[] { currentStep, currentStep - 1, currentStep + 1 })
        {
            if (step < 0) continue;
            var instant = DateTimeOffset.FromUnixTimeSeconds(step * ve.chuKyTotpGiay).UtcDateTime;
            if (_matMaService.TinhMaTotp(biMat, instant, ve.chuKyTotpGiay, ve.soChuSoTotp) == maTotp)
            {
                matchedStep = step;
                break;
            }
        }

        if (matchedStep == null)
        {
            return await GhiNhanQuetThatBai(thongTin, "MA_TOTP_HET_HAN", "Mã QR đã hết hiệu lực hoặc chụp ảnh chia sẻ lại (Anti-Replay)");
        }

        if (ve.maLoaiVe == "VeLuot")
        {
            bool daCoLuot = await _context.LuotDis.AnyAsync(x => x.veId == ve.id && x.trangThai == "HopLe");
            if (daCoLuot)
            {
                return await GhiNhanGianLan(thongTin, ve, matchedStep.Value, "QUET_TRUNG", "Vé lượt này đã được sử dụng trước đó");
            }
        }
        else
        {
            bool daQuetTrongBuoc = await _context.LuotDis.AnyAsync(x => x.veId == ve.id && x.buocThoiGian == matchedStep.Value && x.trangThai == "HopLe");
            if (daQuetTrongBuoc)
            {
                return await GhiNhanGianLan(thongTin, ve, matchedStep.Value, "QUET_TRUNG", "Vé tháng bị quét liên tiếp trong cùng 1 chu kỳ 30s");
            }
        }

        var maxStt = await _context.NhatKyQuetVes
            .Where(x => x.thietBiSoatVeId == thongTin.thietBiSoatVeId)
            .MaxAsync(x => (long?)x.soThuTuThietBi) ?? 0;
        var soThuTu = thongTin.soThuTuThietBi > 0 ? thongTin.soThuTuThietBi : maxStt + 1;
        thongTin.soThuTuThietBi = soThuTu;

        var nhatKy = new NhatKyQuetVe
        {
            maSuKien = Guid.NewGuid(),
            thietBiSoatVeId = thongTin.thietBiSoatVeId,
            phienSoatVeId = thongTin.phienSoatVeId,
            soThuTuThietBi = soThuTu,
            veId = ve.id,
            maVeDocDuoc = ve.maVe.ToString(),
            bamDuLieuQr = SHA256.HashData(Encoding.UTF8.GetBytes(thongTin.maQrPayload)),
            buocThoiGian = matchedStep.Value,
            phuongThuc = "Qr",
            cheDo = thongTin.cheDo ?? "Online",
            ketQuaTaiThietBi = "ChapNhan",
            maLyDo = "HOP_LE",
            thoiDiemQuet = thongTin.thoiDiemQuet == default ? DateTime.UtcNow : thongTin.thoiDiemQuet,
            thoiDiemNhan = DateTime.UtcNow
        };

        await _context.NhatKyQuetVes.AddAsync(nhatKy);
        await _context.SaveChangesAsync();

        var luotDi = new LuotDi
        {
            nhatKyQuetVeId = nhatKy.id,
            veId = ve.id,
            maLoaiVe = ve.maLoaiVe,
            buocThoiGian = matchedStep.Value,
            trangThai = "HopLe",
            congNhanLuc = DateTime.UtcNow
        };

        await _context.LuotDis.AddAsync(luotDi);
        await _context.SaveChangesAsync();

        return new
        {
            hopLe = true,
            amThanh = "beep_success.mp3",
            thongBao = "Xác thực vé hợp lệ! Mời lên xe.",
            thongTinKhach = new
            {
                ve.nguoiDung.hoTen,
                ve.nguoiDung.vaiTro,
                nhomDoiTuong = ve.nhomDoiTuong.tenNhom,
                loaiVe = ve.maLoaiVeNavigation.tenLoai,
                ve.hieuLucTu,
                ve.hetHanLuc
            }
        };
    }

    private static string CheSo(string? raw, int dau, int cuoi)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var s = raw.Trim();
        if (s.Length <= dau + cuoi) return s;
        return string.Concat(s.AsSpan(0, dau), new string('*', s.Length - dau - cuoi), s.AsSpan(s.Length - cuoi, cuoi));
    }

    public async Task<dynamic> traCuuKhancCap(TraCuuKhancCapDto thongTin)
    {
        return await traCuuKhancCap(0, thongTin);
    }

    public async Task<dynamic> traCuuKhancCap(int userId, TraCuuKhancCapDto thongTin)
    {
        if (string.IsNullOrWhiteSpace(thongTin.cccd) && string.IsNullOrWhiteSpace(thongTin.soDienThoai))
        {
            throw new ArgumentException("Phải nhập số CCCD hoặc số điện thoại.");
        }

        var session = await _context.PhienSoatVes
            .AsNoTracking()
            .Include(p => p.chuyenXe)
                .ThenInclude(cx => cx.loTrinh)
            .FirstOrDefaultAsync(p => p.id == thongTin.phienSoatVeId && p.ketThucLuc == null);

        if (session == null)
        {
            throw new KeyNotFoundException("Không tìm thấy phiên soát vé đang mở.");
        }

        if (userId > 0 && session.nhanVienId != userId)
        {
            throw new UnauthorizedAccessException("Phiên soát vé không thuộc về nhân viên hiện tại.");
        }

        if (thongTin.thietBiSoatVeId > 0 && session.thietBiSoatVeId != thongTin.thietBiSoatVeId)
        {
            throw new ArgumentException("Thiết bị soát vé không khớp với phiên hiện tại.");
        }

        var query = _context.NguoiDungs.AsNoTracking();
        NguoiDung? nguoiDung = null;
        if (!string.IsNullOrWhiteSpace(thongTin.cccd))
        {
            var cccdTrim = thongTin.cccd.Trim();
            nguoiDung = await query.FirstOrDefaultAsync(x => x.cccd == cccdTrim);
        }
        else if (!string.IsNullOrWhiteSpace(thongTin.soDienThoai))
        {
            var sdtTrim = thongTin.soDienThoai.Trim();
            nguoiDung = await query.FirstOrDefaultAsync(x => x.soDienThoai == sdtTrim);
        }

        if (nguoiDung == null)
        {
            return new
            {
                timThay = false,
                hopLe = false,
                thongBao = "Không tìm thấy thông tin hành khách trong hệ thống.",
                danhSachVe = Array.Empty<object>()
            };
        }

        var routeId = session.chuyenXe.loTrinh.tuyenXeId;
        var now = DateTime.UtcNow;

        var tickets = await _context.Ves
            .AsNoTracking()
            .Where(x => x.nguoiDungId == nguoiDung.id && x.hetHanLuc > now)
            .Include(x => x.maLoaiVeNavigation)
            .Include(x => x.nhomDoiTuong)
            .Include(x => x.tuyenXe)
            .ToListAsync();

        var ticketIds = tickets.Select(x => x.id).ToList();
        var usedSingleTickets = await _context.LuotDis.AsNoTracking()
            .Where(l => ticketIds.Contains(l.veId) && l.trangThai == "HopLe")
            .Select(l => l.veId)
            .Distinct()
            .ToListAsync();

        var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var usedMonthTicketsThisStep = await _context.LuotDis.AsNoTracking()
            .Where(l => ticketIds.Contains(l.veId) && l.trangThai == "HopLe" && l.buocThoiGian == currentStep)
            .Select(l => l.veId)
            .Distinct()
            .ToListAsync();

        var danhSachVe = tickets.Select(v =>
        {
            var dungTuyen = v.tuyenXeId == null || v.tuyenXeId == routeId;
            var daDung = v.maLoaiVe == "VeLuot" && usedSingleTickets.Contains(v.id);
            var trungCuaSo = v.maLoaiVe == "VeThang" && usedMonthTicketsThisStep.Contains(v.id);
            var coTheSuDung = v.trangThai == "HoatDong" && dungTuyen && !daDung && !trungCuaSo;

            string? lyDo = null;
            if (v.trangThai != "HoatDong") lyDo = $"Vé đang ở trạng thái {v.trangThai}";
            else if (!dungTuyen) lyDo = "Vé không áp dụng cho tuyến xe này";
            else if (daDung) lyDo = "Vé lượt này đã được sử dụng trước đó";
            else if (trungCuaSo) lyDo = "Vé tháng vừa được sử dụng trong vòng 30 giây qua";

            return new
            {
                v.maVe,
                tenLoaiVe = v.maLoaiVeNavigation.tenLoai,
                maLoaiVe = v.maLoaiVe,
                tenNhomDoiTuong = v.nhomDoiTuong.tenNhom,
                tenTuyen = v.tuyenXe != null ? v.tuyenXe.tenTuyen : "Toàn mạng lưới",
                v.trangThai,
                dungTuyen,
                daDung,
                coTheSuDung,
                hetHanLuc = v.hetHanLuc.ToString("O"),
                lyDo
            };
        }).ToList();

        return new
        {
            timThay = true,
            hopLe = danhSachVe.Any(x => x.coTheSuDung),
            thongTinKhach = new
            {
                nguoiDung.hoTen,
                cccd = CheSo(nguoiDung.cccd, 3, 4),
                soDienThoai = CheSo(nguoiDung.soDienThoai, 3, 2)
            },
            danhSachVe,
            thongBao = danhSachVe.Any(x => x.coTheSuDung)
                ? "Tìm thấy vé hợp lệ cho chuyến xe này. Vui lòng đối chiếu thông tin hành khách trước khi xác nhận lượt."
                : "Hành khách không có vé hợp lệ hoặc phù hợp với tuyến này."
        };
    }

    public async Task<dynamic> xacNhanThuCong(int userId, XacNhanThuCongDto thongTin)
    {
        if (thongTin.requestId == Guid.Empty) throw new ArgumentException("Mã requestId không được để trống.");
        if (thongTin.maVe == Guid.Empty) throw new ArgumentException("Mã vé không được để trống.");

        var session = await _context.PhienSoatVes
            .Include(p => p.chuyenXe)
                .ThenInclude(cx => cx.loTrinh)
            .FirstOrDefaultAsync(p => p.id == thongTin.phienSoatVeId && p.ketThucLuc == null);

        if (session == null) throw new KeyNotFoundException("Không tìm thấy phiên soát vé đang mở.");
        if (userId > 0 && session.nhanVienId != userId) throw new UnauthorizedAccessException("Phiên soát vé không thuộc về nhân viên hiện tại.");
        if (thongTin.thietBiSoatVeId > 0 && session.thietBiSoatVeId != thongTin.thietBiSoatVeId) throw new ArgumentException("Thiết bị soát vé không khớp với phiên hiện tại.");

        // Idempotency: Kiểm tra nếu requestId đã được ghi nhận trước đó
        var existingScan = await _context.NhatKyQuetVes
            .Include(n => n.LuotDis)
            .FirstOrDefaultAsync(x => x.maSuKien == thongTin.requestId);

        if (existingScan != null)
        {
            var isHopLe = existingScan.ketQuaTaiThietBi == "ChapNhan";
            return new
            {
                hopLe = isHopLe,
                maVe = thongTin.maVe,
                thoiDiem = existingScan.thoiDiemQuet,
                thongBao = isHopLe
                    ? "Lượt đi đã được xác nhận trước đó (yêu cầu trùng lặp)."
                    : $"Lượt đi bị từ chối trước đó: {existingScan.maLyDo}"
            };
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await _context.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = 'SmartBus:OfflineSync', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Cannot acquire synchronization lock', 1;");

        var ve = await _context.Ves
            .Include(x => x.maLoaiVeNavigation)
            .FirstOrDefaultAsync(x => x.maVe == thongTin.maVe);

        if (ve == null) throw new KeyNotFoundException("Không tìm thấy thông tin vé.");

        var routeId = session.chuyenXe.loTrinh.tuyenXeId;
        var now = DateTime.UtcNow;

        if (ve.hetHanLuc <= now)
        {
            return await GhiNhanTuChoi(session, thongTin, ve, "VE_HET_HAN", "Vé đã hết hạn sử dụng.", transaction);
        }

        if (ve.trangThai != "HoatDong")
        {
            return await GhiNhanTuChoi(session, thongTin, ve, "VE_BI_KHOA", $"Vé đang ở trạng thái {ve.trangThai}.", transaction);
        }

        if (ve.tuyenXeId != null && ve.tuyenXeId != routeId)
        {
            return await GhiNhanTuChoi(session, thongTin, ve, "SAI_TUYEN", "Vé không áp dụng cho tuyến xe này.", transaction);
        }

        // Kiểm tra trùng:
        if (ve.maLoaiVe == "VeLuot")
        {
            var daSuDung = await _context.LuotDis.AnyAsync(l => l.veId == ve.id && l.trangThai == "HopLe");
            if (daSuDung)
            {
                return await GhiNhanTuChoi(session, thongTin, ve, "QUET_TRUNG", "Vé lượt này đã được sử dụng trước đó.", transaction, taoCanhBao: true);
            }
        }
        else if (ve.maLoaiVe == "VeThang")
        {
            var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
            var daSuDungTrongStep = await _context.LuotDis.AnyAsync(l => l.veId == ve.id && l.trangThai == "HopLe" && l.buocThoiGian == currentStep);
            if (daSuDungTrongStep)
            {
                return await GhiNhanTuChoi(session, thongTin, ve, "QUET_TRUNG", "Vé tháng vừa được sử dụng trong vòng 30 giây qua.", transaction, taoCanhBao: true);
            }
        }

        var nextSoThuTu = (await _context.NhatKyQuetVes.Where(x => x.thietBiSoatVeId == thongTin.thietBiSoatVeId).MaxAsync(x => (long?)x.soThuTuThietBi) ?? 0) + 1;

        var nhatKy = new NhatKyQuetVe
        {
            maSuKien = thongTin.requestId,
            thietBiSoatVeId = thongTin.thietBiSoatVeId,
            phienSoatVeId = thongTin.phienSoatVeId,
            soThuTuThietBi = nextSoThuTu,
            veId = ve.id,
            maVeDocDuoc = ve.maVe.ToString(),
            phuongThuc = "TraCuuThuCong",
            cheDo = "Online",
            ketQuaTaiThietBi = "ChapNhan",
            thoiDiemQuet = now,
            thoiDiemNhan = now
        };
        await _context.NhatKyQuetVes.AddAsync(nhatKy);
        await _context.SaveChangesAsync();

        var luotDi = new LuotDi
        {
            nhatKyQuetVeId = nhatKy.id,
            veId = ve.id,
            maLoaiVe = ve.maLoaiVe,
            trangThai = "HopLe",
            buocThoiGian = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30,
            congNhanLuc = now
        };
        await _context.LuotDis.AddAsync(luotDi);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new
        {
            hopLe = true,
            maVe = ve.maVe,
            thoiDiem = luotDi.congNhanLuc,
            thongBao = "Xác nhận lượt đi thành công!"
        };
    }

    private async Task<dynamic> GhiNhanTuChoi(
        PhienSoatVe session,
        XacNhanThuCongDto thongTin,
        Ve ve,
        string maLyDo,
        string thongBao,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        bool taoCanhBao = false)
    {
        var now = DateTime.UtcNow;
        var nextSoThuTu = (await _context.NhatKyQuetVes.Where(x => x.thietBiSoatVeId == thongTin.thietBiSoatVeId).MaxAsync(x => (long?)x.soThuTuThietBi) ?? 0) + 1;
        var nhatKy = new NhatKyQuetVe
        {
            maSuKien = thongTin.requestId,
            thietBiSoatVeId = thongTin.thietBiSoatVeId,
            phienSoatVeId = thongTin.phienSoatVeId,
            soThuTuThietBi = nextSoThuTu,
            veId = ve.id,
            maVeDocDuoc = ve.maVe.ToString(),
            phuongThuc = "TraCuuThuCong",
            cheDo = "Online",
            ketQuaTaiThietBi = "TuChoi",
            maLyDo = maLyDo,
            thoiDiemQuet = now,
            thoiDiemNhan = now
        };
        await _context.NhatKyQuetVes.AddAsync(nhatKy);
        await _context.SaveChangesAsync();

        if (taoCanhBao)
        {
            var canhBao = new CanhBaoGianLan
            {
                nhatKyQuetVeId = nhatKy.id,
                maCanhBao = maLyDo,
                noiDung = $"Phát hiện trùng lặp khi xác nhận vé {ve.maVe} qua tra cứu khẩn cấp.",
                trangThai = "Moi",
                ngayTao = now
            };
            await _context.CanhBaoGianLans.AddAsync(canhBao);
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return new
        {
            hopLe = false,
            maLyDo,
            maVe = ve.maVe,
            thoiDiem = now,
            thongBao
        };
    }

    public async Task<dynamic> dongBoLoNhatKy(DongBoLoDto thongTin)
    {
        var loDongBo = new LoDongBo
        {
            id = thongTin.loDongBoId,
            thietBiSoatVeId = thongTin.thietBiSoatVeId,
            soSuKienKhaiBao = thongTin.danhSachSuKien.Count,
            soSuKienDaXuLy = 0,
            trangThai = "DangXuLy",
            nhanLuc = DateTime.UtcNow
        };

        await _context.LoDongBos.AddAsync(loDongBo);
        await _context.SaveChangesAsync();

        int thanhCong = 0;
        int canhBao = 0;

        foreach (var sk in thongTin.danhSachSuKien)
        {
            var nhatKy = new NhatKyQuetVe
            {
                maSuKien = sk.maSuKien,
                thietBiSoatVeId = thongTin.thietBiSoatVeId,
                phienSoatVeId = sk.phienSoatVeId,
                loDongBoId = loDongBo.id,
                soThuTuThietBi = sk.soThuTuThietBi,
                maVeDocDuoc = sk.maVe?.ToString(),
                phuongThuc = sk.phuongThuc,
                cheDo = "Offline",
                ketQuaTaiThietBi = sk.ketQuaTaiThietBi,
                maLyDo = sk.maLyDo,
                thoiDiemQuet = sk.thoiDiemQuet,
                thoiDiemNhan = DateTime.UtcNow
            };

            if (sk.maVe.HasValue)
            {
                var ve = await _context.Ves.FirstOrDefaultAsync(x => x.maVe == sk.maVe.Value);
                if (ve != null)
                {
                    nhatKy.veId = ve.id;
                    await _context.NhatKyQuetVes.AddAsync(nhatKy);
                    await _context.SaveChangesAsync();

                    bool trungLap = await _context.LuotDis.AnyAsync(x => x.veId == ve.id && x.trangThai == "HopLe" && ve.maLoaiVe == "VeLuot");
                    if (trungLap)
                    {
                        var cb = new CanhBaoGianLan
                        {
                            nhatKyQuetVeId = nhatKy.id,
                            maCanhBao = "DONG_BO_TRUNG_VE_LUOT",
                            noiDung = $"Vé lượt {ve.maVe} được đồng bộ ngoại tuyến nhưng đã có lượt đi trước đó",
                            trangThai = "Moi",
                            ngayTao = DateTime.UtcNow
                        };
                        await _context.CanhBaoGianLans.AddAsync(cb);
                        canhBao++;
                    }
                    else
                    {
                        var luotDi = new LuotDi
                        {
                            nhatKyQuetVeId = nhatKy.id,
                            veId = ve.id,
                            maLoaiVe = ve.maLoaiVe,
                            trangThai = "HopLe",
                            congNhanLuc = DateTime.UtcNow
                        };
                        await _context.LuotDis.AddAsync(luotDi);
                        thanhCong++;
                    }
                }
                else
                {
                    await _context.NhatKyQuetVes.AddAsync(nhatKy);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                await _context.NhatKyQuetVes.AddAsync(nhatKy);
                await _context.SaveChangesAsync();
            }
        }

        loDongBo.soSuKienDaXuLy = thongTin.danhSachSuKien.Count;
        loDongBo.trangThai = "HoanTat";
        loDongBo.hoanTatLuc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new
        {
            loDongBoId = loDongBo.id,
            tongSo = thongTin.danhSachSuKien.Count,
            soLuongCongNhan = thanhCong,
            soLuongCanhBaoGianLan = canhBao
        };
    }

    public async Task<dynamic> layDanhSachCanhBaoGianLan()
    {
        var danhSach = await _context.CanhBaoGianLans
            .AsNoTracking()
            .Include(x => x.nhatKyQuetVe)
            .OrderByDescending(x => x.id)
            .Select(x => new
            {
                x.id,
                x.maCanhBao,
                x.noiDung,
                x.trangThai,
                x.ngayTao,
                thietBiSoatVeId = x.nhatKyQuetVe.thietBiSoatVeId,
                maVeDocDuoc = x.nhatKyQuetVe.maVeDocDuoc,
                thoiDiemQuet = x.nhatKyQuetVe.thoiDiemQuet
            })
            .ToListAsync();

        return danhSach;
    }

    private async Task<dynamic> GhiNhanQuetThatBai(QuetVeRequestDto thongTin, string maLyDo, string thongBaoLoi)
    {
        var maxStt = await _context.NhatKyQuetVes
            .Where(x => x.thietBiSoatVeId == thongTin.thietBiSoatVeId)
            .MaxAsync(x => (long?)x.soThuTuThietBi) ?? 0;
        var soThuTu = thongTin.soThuTuThietBi > 0 ? thongTin.soThuTuThietBi : maxStt + 1;

        var nhatKy = new NhatKyQuetVe
        {
            maSuKien = Guid.NewGuid(),
            thietBiSoatVeId = thongTin.thietBiSoatVeId,
            phienSoatVeId = thongTin.phienSoatVeId,
            soThuTuThietBi = soThuTu,
            phuongThuc = "Qr",
            cheDo = thongTin.cheDo ?? "Online",
            ketQuaTaiThietBi = "TuChoi",
            maLyDo = maLyDo,
            bamDuLieuQr = !string.IsNullOrEmpty(thongTin.maQrPayload) ? SHA256.HashData(Encoding.UTF8.GetBytes(thongTin.maQrPayload)) : null,
            thoiDiemQuet = thongTin.thoiDiemQuet == default ? DateTime.UtcNow : thongTin.thoiDiemQuet,
            thoiDiemNhan = DateTime.UtcNow
        };

        await _context.NhatKyQuetVes.AddAsync(nhatKy);
        await _context.SaveChangesAsync();

        return new
        {
            hopLe = false,
            amThanh = "alarm_error.mp3",
            maLyDo,
            thongBao = thongBaoLoi
        };
    }

    private async Task<dynamic> GhiNhanGianLan(QuetVeRequestDto thongTin, Ve ve, long buocThoiGian, string maCanhBao, string noiDung)
    {
        var maxStt = await _context.NhatKyQuetVes
            .Where(x => x.thietBiSoatVeId == thongTin.thietBiSoatVeId)
            .MaxAsync(x => (long?)x.soThuTuThietBi) ?? 0;
        var soThuTu = thongTin.soThuTuThietBi > 0 ? thongTin.soThuTuThietBi : maxStt + 1;

        var nhatKy = new NhatKyQuetVe
        {
            maSuKien = Guid.NewGuid(),
            thietBiSoatVeId = thongTin.thietBiSoatVeId,
            phienSoatVeId = thongTin.phienSoatVeId,
            soThuTuThietBi = soThuTu,
            veId = ve.id,
            maVeDocDuoc = ve.maVe.ToString(),
            bamDuLieuQr = !string.IsNullOrEmpty(thongTin.maQrPayload) ? SHA256.HashData(Encoding.UTF8.GetBytes(thongTin.maQrPayload)) : null,
            buocThoiGian = buocThoiGian,
            phuongThuc = "Qr",
            cheDo = thongTin.cheDo ?? "Online",
            ketQuaTaiThietBi = "TuChoi",
            maLyDo = maCanhBao,
            thoiDiemQuet = thongTin.thoiDiemQuet == default ? DateTime.UtcNow : thongTin.thoiDiemQuet,
            thoiDiemNhan = DateTime.UtcNow
        };

        await _context.NhatKyQuetVes.AddAsync(nhatKy);
        await _context.SaveChangesAsync();

        var canhBao = new CanhBaoGianLan
        {
            nhatKyQuetVeId = nhatKy.id,
            maCanhBao = maCanhBao,
            noiDung = noiDung,
            trangThai = "Moi",
            ngayTao = DateTime.UtcNow
        };

        await _context.CanhBaoGianLans.AddAsync(canhBao);
        await _context.SaveChangesAsync();

        return new
        {
            hopLe = false,
            amThanh = "alarm_error.mp3",
            maLyDo = maCanhBao,
            thongBao = "Cảnh báo gian lận: " + noiDung
        };
    }
}
