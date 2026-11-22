using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using Server.Models;

namespace Server.Services;

public interface IVeService
{
    Task<dynamic> layDanhSachBieuGia(int? tuyenXeId, bool chiTinhPhi = false);
    Task<dynamic> layHoacCapTheMienPhi(int nguoiDungId);
    Task<dynamic> taoDonHang(TaoDonHangDto thongTin);
    Task<dynamic> thanhToanDonHang(ThanhToanDonHangDto thongTin);
    Task<dynamic> thanhToanGiaLap(int userId, ThanhToanGiaLapDto thongTin);
    Task<dynamic> phatHanhVe(long donHangId);
    Task<dynamic> layDanhSachVeNguoiDung(int nguoiDungId);
    Task<dynamic> layChiTietVeQrDong(Guid maVe);
    Task<dynamic> khoaVe(KhoaVeDto thongTin);
    Task<dynamic> layLichSuLuotDi(int nguoiDungId);
}

public class VeService : IVeService
{
    private readonly SmartBusDbContext _context;
    private readonly IMatMaService _matMaService;
    private readonly SigningKeyService _signingKeys;
    private readonly ICacheService? _cache;

    public VeService(SmartBusDbContext context, IMatMaService matMaService, SigningKeyService signingKeys, ICacheService? cache = null)
    {
        _context = context;
        _matMaService = matMaService;
        _signingKeys = signingKeys;
        _cache = cache;
    }

    public async Task<dynamic> layLichSuLuotDi(int nguoiDungId)
    {
        var rows = await _context.LuotDis.AsNoTracking()
            .Where(x => x.Ve.nguoiDungId == nguoiDungId)
            .OrderByDescending(x => x.NhatKyQuetVe.thoiDiemQuet).ThenByDescending(x => x.id).Take(100)
            .Select(x => new {
                x.id, x.Ve.maVe, x.maLoaiVe, x.trangThai, x.lyDoHuy,
                x.NhatKyQuetVe.thoiDiemQuet, x.NhatKyQuetVe.cheDo,
                x.NhatKyQuetVe.phuongThuc,
                maTuyen = x.NhatKyQuetVe.PhienSoatVe.chuyenXe.loTrinh.tuyenXe.maTuyen,
                tenTuyen = x.NhatKyQuetVe.PhienSoatVe.chuyenXe.loTrinh.tuyenXe.tenTuyen,
                bienSo = x.NhatKyQuetVe.PhienSoatVe.chuyenXe.xeBuyt.bienSo
            }).ToListAsync();
        return rows.Select(x => new { x.id, x.maVe, x.maLoaiVe, x.trangThai, x.lyDoHuy,
            thoiDiemQuet = DateTime.SpecifyKind(x.thoiDiemQuet, DateTimeKind.Utc).ToString("O"),
            x.cheDo, x.phuongThuc, x.maTuyen, x.tenTuyen, x.bienSo }).ToList();
    }

    public async Task<dynamic> layDanhSachBieuGia(int? tuyenXeId, bool chiTinhPhi = false)
    {
        var ngay = ChinhSachTuyen.HomNay;
        string cacheKey = $"smartbus:cache:bieu-gia:{tuyenXeId ?? 0}:{(chiTinhPhi ? "tinh-phi" : "tat-ca")}:{ngay:yyyyMMdd}";
        if (_cache != null)
        {
            var cached = await _cache.GetAsync<List<BieuGiaListItemDto>>(cacheKey);
            if (cached != null) return cached;
        }

        var danhSach = await _context.GiaVes
            .AsNoTracking()
            .Where(x => x.trangThai && (tuyenXeId == null || x.tuyenXeId == null || x.tuyenXeId == tuyenXeId))
            .Where(x => !chiTinhPhi || (x.maLoaiVe != ChinhSachTuyen.TheMienPhi && x.tuyenXe != null && !(x.tuyenXe.chinhSachMienPhi != null && x.tuyenXe.chinhSachMienPhi.trangThai && x.tuyenXe.chinhSachMienPhi.hieuLucTu <= ngay && x.tuyenXe.chinhSachMienPhi.hieuLucDen >= ngay)))
            .Include(x => x.maLoaiVeNavigation)
            .Include(x => x.nhomDoiTuong)
            .Include(x => x.tuyenXe)
            .Select(x => new BieuGiaListItemDto
            {
                id = x.id,
                maLoaiVe = x.maLoaiVe,
                tenLoaiVe = x.maLoaiVeNavigation.tenLoai,
                nhomDoiTuongId = x.nhomDoiTuongId,
                tenNhomDoiTuong = x.nhomDoiTuong.tenNhom,
                tuyenXeId = x.tuyenXeId,
                tenTuyen = x.tuyenXe != null ? x.tuyenXe.tenTuyen : "Toàn mạng lưới",
                maTuyen = x.tuyenXe != null ? x.tuyenXe.maTuyen : null,
                giaGoc = x.giaGoc,
                soTienMienGiam = x.soTienMienGiam,
                giaThucThu = x.giaGoc - x.soTienMienGiam,
                canCuChinhSach = x.canCuChinhSach
            })
            .ToListAsync();

        if (_cache != null)
        {
            await _cache.SetAsync(cacheKey, danhSach, TimeSpan.FromMinutes(5));
        }

        return danhSach;
    }

    public async Task<dynamic> taoDonHang(TaoDonHangDto thongTin)
    {
        if (thongTin.soLuong < 1 || thongTin.soLuong > 10)
        {
            throw new ArgumentException("Số lượng vé mua trong một đơn hàng phải từ 1 đến 10 vé.");
        }

        // Kiểm tra chống trùng / Idempotency theo maYeuCau
        if (thongTin.maYeuCau.HasValue && thongTin.maYeuCau.Value != Guid.Empty)
        {
            var existingOther = await _context.DonHangs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.maYeuCau == thongTin.maYeuCau.Value && x.nguoiDungId != thongTin.nguoiDungId);
            if (existingOther != null)
            {
                throw new InvalidOperationException("Mã yêu cầu trùng lặp với giao dịch của người dùng khác.");
            }

            var existing = await _context.DonHangs
                .Include(x => x.ChiTietDonHangs)
                .FirstOrDefaultAsync(x => x.maYeuCau == thongTin.maYeuCau.Value && x.nguoiDungId == thongTin.nguoiDungId);

            if (existing != null)
            {
                var detail = existing.ChiTietDonHangs.FirstOrDefault();
                if (detail != null && detail.giaVeId == thongTin.giaVeId && detail.soLuong == thongTin.soLuong)
                {
                    var phaiThu = existing.tongGiaGoc - existing.tongMienGiam;
                    return new
                    {
                        donHangId = existing.id,
                        maDonHang = existing.maDonHang,
                        tongGiaGoc = existing.tongGiaGoc,
                        tongMienGiam = existing.tongMienGiam,
                        phaiThanhToan = phaiThu,
                        trangThai = existing.trangThai,
                        thongBao = existing.trangThai == "MienPhi"
                            ? "Vé miễn phí đã được cấp vào ví."
                            : "Đơn hàng đã tồn tại cho yêu cầu này."
                    };
                }
                throw new InvalidOperationException("Mã yêu cầu đã được sử dụng cho một đơn hàng khác với nội dung khác.");
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var giaVe = await _context.GiaVes
            .Include(x => x.nhomDoiTuong)
            .FirstOrDefaultAsync(x => x.id == thongTin.giaVeId && x.trangThai);

        if (giaVe == null) throw new ArgumentException("Không tìm thấy biểu giá vé hợp lệ hoặc biểu giá đã ngừng hoạt động.");
        if (giaVe.maLoaiVe == ChinhSachTuyen.TheMienPhi) throw new ArgumentException("Thẻ đi xe miễn phí được cấp tự động sau khi xác thực định danh, không cần mua.");

        var nguoiDung = await _context.NguoiDungs.FindAsync(thongTin.nguoiDungId);
        if (nguoiDung == null) throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");

        if (giaVe.nhomDoiTuong.canXacMinh)
        {
            var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
            var hopLe = await _context.HoSoUuTiens.AnyAsync(x =>
                x.nguoiDungId == thongTin.nguoiDungId
                && x.nhomDoiTuongId == giaVe.nhomDoiTuongId
                && x.trangThai == "DaDuyet"
                && x.hieuLucTu <= homNay
                && x.hieuLucDen >= homNay);

            if (!hopLe)
            {
                throw new Exception($"Bạn chưa có hồ sơ ưu tiên hợp lệ hoặc còn hạn cho nhóm: {giaVe.nhomDoiTuong.tenNhom}");
            }
        }

        var donGiaGoc = giaVe.giaGoc;
        var mienGiamMoiVe = giaVe.soTienMienGiam;
        var tongGiaGoc = donGiaGoc * thongTin.soLuong;
        var tongMienGiam = mienGiamMoiVe * thongTin.soLuong;
        var phaiThanhToan = tongGiaGoc - tongMienGiam;

        var donHang = new DonHang
        {
            maDonHang = Guid.NewGuid(),
            nguoiDungId = thongTin.nguoiDungId,
            maYeuCau = thongTin.maYeuCau.HasValue && thongTin.maYeuCau.Value != Guid.Empty ? thongTin.maYeuCau.Value : Guid.NewGuid(),
            tongGiaGoc = tongGiaGoc,
            tongMienGiam = tongMienGiam,
            tongThanhToan = phaiThanhToan,
            trangThai = phaiThanhToan == 0 ? "MienPhi" : "ChoThanhToan",
            ngayTao = DateTime.UtcNow,
            hetHanLuc = DateTime.UtcNow.AddMinutes(30)
        };

        try
        {
            await _context.DonHangs.AddAsync(donHang);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (thongTin.maYeuCau.HasValue && thongTin.maYeuCau.Value != Guid.Empty)
            {
                var concurrentOrder = await _context.DonHangs.AsNoTracking()
                    .Include(x => x.ChiTietDonHangs)
                    .FirstOrDefaultAsync(x => x.maYeuCau == thongTin.maYeuCau.Value);

                if (concurrentOrder != null && concurrentOrder.nguoiDungId == thongTin.nguoiDungId)
                {
                    var detail = concurrentOrder.ChiTietDonHangs.FirstOrDefault();
                    if (detail != null && detail.giaVeId == thongTin.giaVeId && detail.soLuong == thongTin.soLuong)
                    {
                        return new
                        {
                            donHangId = concurrentOrder.id,
                            maDonHang = concurrentOrder.maDonHang,
                            tongGiaGoc = concurrentOrder.tongGiaGoc,
                            tongMienGiam = concurrentOrder.tongMienGiam,
                            phaiThanhToan = concurrentOrder.tongGiaGoc - concurrentOrder.tongMienGiam,
                            trangThai = concurrentOrder.trangThai,
                            thongBao = "Đơn hàng đã tồn tại cho yêu cầu này."
                        };
                    }
                }
            }
            throw new InvalidOperationException("Xung đột dữ liệu khi tạo đơn hàng (trùng lặp yêu cầu).");
        }

        var chiTiet = new ChiTietDonHang
        {
            donHangId = donHang.id,
            giaVeId = giaVe.id,
            soLuong = thongTin.soLuong,
            donGiaGoc = donGiaGoc,
            mienGiamMoiVe = mienGiamMoiVe
        };

        await _context.ChiTietDonHangs.AddAsync(chiTiet);
        await _context.SaveChangesAsync();

        if (phaiThanhToan == 0)
        {
            await phatHanhVe(donHang.id);
            await transaction.CommitAsync();

            return new
            {
                donHangId = donHang.id,
                maDonHang = donHang.maDonHang,
                tongGiaGoc,
                tongMienGiam,
                phaiThanhToan = 0m,
                trangThai = "MienPhi",
                thongBao = "Bạn được hưởng chính sách miễn phí 100%. Đã cấp vé thành công."
            };
        }

        await transaction.CommitAsync();
        return new
        {
            donHangId = donHang.id,
            maDonHang = donHang.maDonHang,
            tongGiaGoc,
            tongMienGiam,
            phaiThanhToan,
            trangThai = "ChoThanhToan",
            thongBao = "Tạo đơn hàng thành công, vui lòng thanh toán mô phỏng."
        };
    }

    public async Task<dynamic> thanhToanGiaLap(int userId, ThanhToanGiaLapDto thongTin)
    {
        if (thongTin.requestId == Guid.Empty)
        {
            throw new ArgumentException("Mã requestId không được để trống.");
        }

        var ketQuaChuan = thongTin.ketQua?.Trim();
        if (ketQuaChuan != "ThanhCong" && ketQuaChuan != "ThatBai" && ketQuaChuan != "Huy")
        {
            throw new ArgumentException("Kết quả thanh toán không hợp lệ. Chỉ chấp nhận: ThanhCong, ThatBai, Huy.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var donHang = await _context.DonHangs
            .Include(x => x.ChiTietDonHangs)
            .Include(x => x.ThanhToan)
            .FirstOrDefaultAsync(x => x.id == thongTin.donHangId);

        if (donHang == null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        }

        if (userId > 0 && donHang.nguoiDungId != userId)
        {
            throw new UnauthorizedAccessException("Đơn hàng không thuộc về người dùng hiện tại.");
        }

        var soTien = donHang.tongGiaGoc - donHang.tongMienGiam;

        // Idempotency: Kiểm tra nếu requestId đã được sử dụng
        var existingThanhToan = await _context.ThanhToans
            .FirstOrDefaultAsync(x => x.maYeuCau == thongTin.requestId);

        if (existingThanhToan != null)
        {
            if (existingThanhToan.donHangId != donHang.id)
            {
                throw new InvalidOperationException("Mã requestId đã được sử dụng cho một đơn hàng khác.");
            }

            if (existingThanhToan.trangThai == ketQuaChuan)
            {
                return new
                {
                    donHangId = donHang.id,
                    maDonHang = donHang.maDonHang,
                    requestId = thongTin.requestId,
                    soTien = existingThanhToan.soTien,
                    trangThai = donHang.trangThai,
                    thongBao = existingThanhToan.trangThai == "ThanhCong"
                        ? "Thanh toán mô phỏng thành công, vé đã được cấp."
                        : $"Đơn hàng ở trạng thái: {donHang.trangThai}"
                };
            }
            throw new InvalidOperationException($"Yêu cầu requestId trùng lặp nhưng kết quả khác nhau ({existingThanhToan.trangThai}).");
        }

        // Kiểm tra trạng thái đơn hàng nếu chưa từng chạy requestId này
        if (donHang.trangThai == "DaThanhToan" || donHang.trangThai == "MienPhi")
        {
            throw new InvalidOperationException("Đơn hàng này đã được thanh toán hoặc cấp vé trước đó.");
        }

        if (donHang.trangThai == "Huy")
        {
            if (ketQuaChuan == "Huy")
            {
                return new
                {
                    donHangId = donHang.id,
                    maDonHang = donHang.maDonHang,
                    requestId = thongTin.requestId,
                    soTien,
                    trangThai = "Huy",
                    thongBao = "Đơn hàng đã được hủy trước đó."
                };
            }
            throw new InvalidOperationException("Đơn hàng này đã bị hủy, không thể thanh toán.");
        }

        if (donHang.hetHanLuc <= DateTime.UtcNow)
        {
            donHang.trangThai = "HetHan";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            throw new InvalidOperationException("Đơn hàng đã hết hạn thanh toán (quá thời hạn 30 phút).");
        }

        if (ketQuaChuan == "ThanhCong")
        {
            var thanhToan = new ThanhToan
            {
                maYeuCau = thongTin.requestId,
                donHangId = donHang.id,
                soTien = soTien,
                donViTien = "VND",
                congThanhToan = "Mock",
                maGiaoDichCong = "MOCK_" + thongTin.requestId.ToString("N")[..12],
                trangThai = "ThanhCong",
                ngayTao = DateTime.UtcNow,
                thanhCongLuc = DateTime.UtcNow
            };

            await _context.ThanhToans.AddAsync(thanhToan);
            donHang.trangThai = "DaThanhToan";
            await _context.SaveChangesAsync();

            await phatHanhVe(donHang.id);
            await transaction.CommitAsync();

            return new
            {
                donHangId = donHang.id,
                maDonHang = donHang.maDonHang,
                requestId = thongTin.requestId,
                soTien,
                trangThai = "DaThanhToan",
                thongBao = "Thanh toán mô phỏng thành công, hệ thống đã ký số và phát hành vé."
            };
        }
        else if (ketQuaChuan == "ThatBai")
        {
            var thanhToan = new ThanhToan
            {
                maYeuCau = thongTin.requestId,
                donHangId = donHang.id,
                soTien = soTien,
                donViTien = "VND",
                congThanhToan = "Mock",
                maGiaoDichCong = "MOCK_FAIL_" + thongTin.requestId.ToString("N")[..12],
                trangThai = "ThatBai",
                ngayTao = DateTime.UtcNow
            };

            await _context.ThanhToans.AddAsync(thanhToan);
            donHang.trangThai = "ThatBai";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new
            {
                donHangId = donHang.id,
                maDonHang = donHang.maDonHang,
                requestId = thongTin.requestId,
                soTien,
                trangThai = "ThatBai",
                thongBao = "Thanh toán mô phỏng thất bại. Bạn có thể thử lại hoặc hủy đơn hàng."
            };
        }
        else // "Huy"
        {
            donHang.trangThai = "Huy";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new
            {
                donHangId = donHang.id,
                maDonHang = donHang.maDonHang,
                requestId = thongTin.requestId,
                soTien,
                trangThai = "Huy",
                thongBao = "Đơn hàng đã được hủy thành công."
            };
        }
    }

    public async Task<dynamic> thanhToanDonHang(ThanhToanDonHangDto thongTin)
    {
        var giaLapDto = new ThanhToanGiaLapDto
        {
            donHangId = thongTin.donHangId,
            requestId = Guid.NewGuid(),
            ketQua = "ThanhCong"
        };
        return await thanhToanGiaLap(0, giaLapDto);
    }

    public async Task<dynamic> phatHanhVe(long donHangId)
    {
        var donHang = await _context.DonHangs
            .Include(x => x.ChiTietDonHangs)
                .ThenInclude(ct => ct.giaVe)
            .FirstOrDefaultAsync(x => x.id == donHangId);

        if (donHang == null) throw new Exception("Không tìm thấy đơn hàng");

        var khoaKySo = await _context.KhoaKySos
            .Where(x => x.trangThai == "DangDung")
            .OrderByDescending(x => x.id)
            .FirstOrDefaultAsync();

        if (khoaKySo == null) throw new Exception("Hệ thống chưa có khóa ký số hợp lệ");

        using var ecdsaPrivate = _signingKeys.Open(khoaKySo.khoaCongKhaiPem);

        short soThuTu = 1;
        var danhSachVe = new List<Ve>();

        foreach (var chiTiet in donHang.ChiTietDonHangs)
        {
            for (short i = 0; i < chiTiet.soLuong; i++)
            {
                var maVe = Guid.NewGuid();
                var laVeThang = chiTiet.giaVe.maLoaiVe == "VeThang";

                var hieuLucTu = DateTime.UtcNow;
                var hetHanLuc = chiTiet.giaVe.maLoaiVe == ChinhSachTuyen.TheMienPhi
                    ? chiTiet.giaVe.hieuLucDen ?? DateTime.UtcNow.AddDays(30)
                    : laVeThang ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(24);

                var thongTinKy = JsonSerializer.Serialize(new
                {
                    v = 1,
                    mv = maVe,
                    u = donHang.nguoiDungId,
                    lv = chiTiet.giaVe.maLoaiVe,
                    nd = chiTiet.giaVe.nhomDoiTuongId,
                    tx = chiTiet.giaVe.tuyenXeId,
                    tu = hieuLucTu.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    den = hetHanLuc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    mk = khoaKySo.maKhoa
                });

                var (duLieuBytes, chuKyBytes) = _matMaService.KySoDuLieu(thongTinKy, ecdsaPrivate);

                var biMatTotpRaw = _matMaService.PhaiSinhBiMatTotp(maVe);
                var biMatMaHoa = _matMaService.MaHoaAes(biMatTotpRaw);

                var ve = new Ve
                {
                    maVe = maVe,
                    chiTietDonHangId = chiTiet.id,
                    soThuTu = soThuTu++,
                    nguoiDungId = donHang.nguoiDungId,
                    maLoaiVe = chiTiet.giaVe.maLoaiVe,
                    nhomDoiTuongId = chiTiet.giaVe.nhomDoiTuongId,
                    tuyenXeId = chiTiet.giaVe.tuyenXeId,
                    khoaKySoId = khoaKySo.id,
                    phienBanDuLieu = 1,
                    duLieuDaKy = duLieuBytes,
                    chuKy = chuKyBytes,
                    biMatTotpMaHoa = biMatMaHoa,
                    phienBanKhoaBaoVe = "AES_GCM_V1",
                    chuKyTotpGiay = 30,
                    soChuSoTotp = 8,
                    hieuLucTu = hieuLucTu,
                    hetHanLuc = hetHanLuc,
                    trangThai = "HoatDong",
                    ngayPhatHanh = DateTime.UtcNow
                };

                danhSachVe.Add(ve);
            }
        }

        await _context.Ves.AddRangeAsync(danhSachVe);
        await _context.SaveChangesAsync();

        return new { soLuongVeDaCap = danhSachVe.Count };
    }

    public async Task<dynamic> layHoacCapTheMienPhi(int nguoiDungId)
    {
        var nguoiDung = await _context.NguoiDungs.FindAsync(nguoiDungId) ?? throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");
        if (string.IsNullOrEmpty(nguoiDung.phuongThucXacThuc))
            throw new UnauthorizedAccessException("Vui lòng xác thực định danh để sử dụng 134 tuyến xe buýt miễn phí.");

        var now = DateTime.UtcNow;
        var daCoThe = await _context.Ves.AnyAsync(x => x.nguoiDungId == nguoiDungId && x.maLoaiVe == ChinhSachTuyen.TheMienPhi && x.trangThai == "HoatDong" && x.hetHanLuc > now);
        if (!daCoThe)
        {
            var giaVe = await _context.GiaVes.FirstOrDefaultAsync(x => x.maLoaiVe == ChinhSachTuyen.TheMienPhi && x.trangThai && x.hieuLucTu <= now && (x.hieuLucDen == null || x.hieuLucDen > now))
                ?? throw new InvalidOperationException("Chính sách miễn phí 134 tuyến hiện không còn hiệu lực.");
            var donHang = new DonHang
            {
                maDonHang = Guid.NewGuid(),
                nguoiDungId = nguoiDungId,
                maYeuCau = Guid.NewGuid(),
                tongGiaGoc = 0,
                tongMienGiam = 0,
                tongThanhToan = 0,
                trangThai = "MienPhi",
                ngayTao = now,
                hetHanLuc = now.AddMinutes(30)
            };
            donHang.ChiTietDonHangs.Add(new ChiTietDonHang { giaVeId = giaVe.id, soLuong = 1, donGiaGoc = 0, mienGiamMoiVe = 0 });
            _context.DonHangs.Add(donHang);
            await _context.SaveChangesAsync();
            await phatHanhVe(donHang.id);
        }

        var danhSach = (IEnumerable<dynamic>)await layDanhSachVeNguoiDung(nguoiDungId);
        return danhSach.First(x => x.maLoaiVe == ChinhSachTuyen.TheMienPhi);
    }

    public async Task<dynamic> layDanhSachVeNguoiDung(int nguoiDungId)
    {
        var now = DateTime.UtcNow;
        var danhSach = await _context.Ves
            .AsNoTracking()
            .Where(x => x.nguoiDungId == nguoiDungId && x.trangThai == "HoatDong" && x.hetHanLuc > now)
            .Include(x => x.maLoaiVeNavigation)
            .Include(x => x.nhomDoiTuong)
            .Include(x => x.tuyenXe)
            .OrderByDescending(x => x.id)
            .ToListAsync();

        var ketQua = new List<dynamic>();

        foreach (var v in danhSach)
        {
            var biMat = _matMaService.GiaiMaAes(v.biMatTotpMaHoa);
            var maTotp = _matMaService.TinhMaTotp(biMat, now, v.chuKyTotpGiay, v.soChuSoTotp);
            var soGiayConLai = _matMaService.TinhSoGiayConLai(now, v.chuKyTotpGiay);
            var chuKyBase64 = Convert.ToBase64String(v.chuKy);

            var maQrPayload = $"SMARTBUS:v1:{v.maVe}:{maTotp}:{chuKyBase64}";

            ketQua.Add(new
            {
                v.id,
                v.maVe,
                tenLoaiVe = v.maLoaiVeNavigation.tenLoai,
                tenNhomDoiTuong = v.nhomDoiTuong.tenNhom,
                maLoaiVe = v.maLoaiVe,
                tenTuyen = v.maLoaiVe == ChinhSachTuyen.TheMienPhi ? "134 tuyến miễn phí (NQ 09/2026)" : v.tuyenXe != null ? v.tuyenXe.tenTuyen : "Toàn mạng lưới",
                maTuyen = v.tuyenXe != null ? v.tuyenXe.maTuyen : "ALL",
                v.hieuLucTu,
                v.hetHanLuc,
                v.trangThai,
                maTotpHienTai = maTotp,
                soGiayConLai,
                biMatTotp = Convert.ToBase64String(biMat),
                chuKyBase64,
                maQrPayload
            });
        }

        return ketQua;
    }

    public async Task<dynamic> layChiTietVeQrDong(Guid maVe)
    {
        var v = await _context.Ves
            .AsNoTracking()
            .Include(x => x.maLoaiVeNavigation)
            .Include(x => x.nhomDoiTuong)
            .Include(x => x.tuyenXe)
            .FirstOrDefaultAsync(x => x.maVe == maVe);

        if (v == null) return null!;

        var now = DateTime.UtcNow;
        var biMat = _matMaService.GiaiMaAes(v.biMatTotpMaHoa);
        var maTotp = _matMaService.TinhMaTotp(biMat, now, v.chuKyTotpGiay, v.soChuSoTotp);
        var soGiayConLai = _matMaService.TinhSoGiayConLai(now, v.chuKyTotpGiay);
        var chuKyBase64 = Convert.ToBase64String(v.chuKy);

        var maQrPayload = $"SMARTBUS:v1:{v.maVe}:{maTotp}:{chuKyBase64}";

        return new
        {
            v.id,
            v.maVe,
            tenLoaiVe = v.maLoaiVeNavigation.tenLoai,
            tenNhomDoiTuong = v.nhomDoiTuong.tenNhom,
            maLoaiVe = v.maLoaiVe,
            tenTuyen = v.maLoaiVe == ChinhSachTuyen.TheMienPhi ? "134 tuyến miễn phí (NQ 09/2026)" : v.tuyenXe != null ? v.tuyenXe.tenTuyen : "Toàn mạng lưới",
            maTuyen = v.tuyenXe != null ? v.tuyenXe.maTuyen : "ALL",
            v.hieuLucTu,
            v.hetHanLuc,
            v.trangThai,
            maTotpHienTai = maTotp,
            soGiayConLai,
            maQrPayload
        };
    }

    public async Task<dynamic> khoaVe(KhoaVeDto thongTin)
    {
        var ve = await _context.Ves.FirstOrDefaultAsync(x => x.maVe == thongTin.maVe);
        if (ve == null) return false;

        ve.trangThai = thongTin.thuHoiVinhVien ? "ThuHoi" : "TamKhoa";

        var nhatKy = new NhatKyThuHoiVe
        {
            veId = ve.id,
            hanhDong = ve.trangThai,
            lyDo = thongTin.lyDo,
            nguoiThucHienId = thongTin.nguoiThucHienId,
            ngayTao = DateTime.UtcNow
        };

        await _context.NhatKyThuHoiVes.AddAsync(nhatKy);
        await _context.SaveChangesAsync();

        return true;
    }
}
