namespace Server.Models;

public class DongBoSuKienOfflineDto
{
    public Guid maSuKien { get; set; }
    public Guid goiId { get; set; }
    public string qr { get; set; } = "";
    public DateTime thoiDiemQuet { get; set; }
    public bool chapNhanTaiThietBi { get; set; }
}

public class QuetVeRequestDto
{
    public Guid phienSoatVeId { get; set; }
    public int thietBiSoatVeId { get; set; }
    public long soThuTuThietBi { get; set; }
    public string maQrPayload { get; set; } = string.Empty;
    public DateTime thoiDiemQuet { get; set; } = DateTime.UtcNow;
    public string cheDo { get; set; } = "Online";
}

public class TraCuuKhancCapDto
{
    public Guid phienSoatVeId { get; set; }
    public int thietBiSoatVeId { get; set; }
    public long soThuTuThietBi { get; set; }
    public string? cccd { get; set; }
    public string? soDienThoai { get; set; }
}

public class XacNhanThuCongDto
{
    public Guid phienSoatVeId { get; set; }
    public int thietBiSoatVeId { get; set; }
    public Guid maVe { get; set; }
    public Guid requestId { get; set; }
}

public class SuKienDongBoItemDto
{
    public Guid maSuKien { get; set; }
    public Guid phienSoatVeId { get; set; }
    public long soThuTuThietBi { get; set; }
    public Guid? maVe { get; set; }
    public string? maTotp { get; set; }
    public string? chuKyBase64 { get; set; }
    public string phuongThuc { get; set; } = "Qr";
    public string ketQuaTaiThietBi { get; set; } = "ChapNhan";
    public string? maLyDo { get; set; }
    public DateTime thoiDiemQuet { get; set; }
}

public class DongBoLoDto
{
    public Guid loDongBoId { get; set; }
    public int thietBiSoatVeId { get; set; }
    public List<SuKienDongBoItemDto> danhSachSuKien { get; set; } = new();
}
