namespace Server.Models;

public class CapNhatCaNhanDto
{
    public string hoTen { get; set; } = "";
    public string? soDienThoai { get; set; }
    public string? email { get; set; }
    public string? cccd { get; set; }
}

public class DangNhapDto
{
    public string taiKhoan { get; set; } = string.Empty;
    public string matKhau { get; set; } = string.Empty;
}

public class DangKyDto
{
    public string hoTen { get; set; } = string.Empty;
    public string soDienThoai { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string cccd { get; set; } = string.Empty;
    public string matKhau { get; set; } = string.Empty;
    public string vaiTro { get; set; } = "HanhKhach";
}

public class HoSoUuTienDto
{
    public int id { get; set; } = 0;
    public int nguoiDungId { get; set; }
    public int nhomDoiTuongId { get; set; }
    public string duongDanMinhChung { get; set; } = string.Empty;
    public DateTime hieuLucTu { get; set; } = DateTime.UtcNow;
    public DateTime hieuLucDen { get; set; } = DateTime.UtcNow.AddYears(1);
}

public class TepMinhChungDto
{
    public Guid maTep { get; set; }
    public string tenFileGoc { get; set; } = string.Empty;
    public string duongDan { get; set; } = string.Empty;
    public string loaiFile { get; set; } = string.Empty;
    public long kichThuoc { get; set; }
}
