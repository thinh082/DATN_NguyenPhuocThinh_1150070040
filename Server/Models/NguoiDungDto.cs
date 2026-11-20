namespace Server.Models;

public class NguoiDungDto
{
    public int id { get; set; } = 0;
    public string hoTen { get; set; } = string.Empty;
    public string soDienThoai { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string cccd { get; set; } = string.Empty;
    public string vaiTro { get; set; } = "HanhKhach";
    public bool trangThai { get; set; } = true;
    public string? matKhau { get; set; }
}
