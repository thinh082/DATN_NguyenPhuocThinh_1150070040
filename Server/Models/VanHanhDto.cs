namespace Server.Models;

public class TuyenXeDto
{
    public int id { get; set; } = 0;
    public string maTuyen { get; set; } = string.Empty;
    public string tenTuyen { get; set; } = string.Empty;
    public string gioBatDau { get; set; } = "05:00";
    public string gioKetThuc { get; set; } = "21:00";
    public short? gianCachPhut { get; set; } = 10;
    public bool trangThai { get; set; } = true;
    public string? maChinhSach { get; set; }
    public decimal? cuLyKm { get; set; }
}

public class TramDungDto
{
    public int id { get; set; } = 0;
    public string maTram { get; set; } = string.Empty;
    public string tenTram { get; set; } = string.Empty;
    public string? diaChi { get; set; }
    public decimal viDo { get; set; }
    public decimal kinhDo { get; set; }
    public bool trangThai { get; set; } = true;
}

public class XeBuytDto
{
    public int id { get; set; } = 0;
    public string bienSo { get; set; } = string.Empty;
    public Guid? maQrTinh { get; set; }
    public short sucChua { get; set; } = 40;
    public bool trangThai { get; set; } = true;
}

public class BatDauPhienDto
{
    public int thietBiId { get; set; }
    public int nhanVienId { get; set; }
    public int chuyenXeId { get; set; }
}

public class TuyenXeListItemDto
{
    public int id { get; set; }
    public string maTuyen { get; set; } = string.Empty;
    public string tenTuyen { get; set; } = string.Empty;
    public string gioBatDau { get; set; } = string.Empty;
    public string gioKetThuc { get; set; } = string.Empty;
    public int gianCachPhut { get; set; }
    public bool trangThai { get; set; }
    public int soLuongLoTrinh { get; set; }
    public decimal? cuLyKm { get; set; }
    public bool mienPhi { get; set; }
    public string? maChinhSach { get; set; }
    public string? tenChinhSach { get; set; }
}
