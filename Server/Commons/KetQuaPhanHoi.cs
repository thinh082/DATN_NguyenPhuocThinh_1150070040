namespace Server.Commons;

public class KetQuaPhanHoi
{
    public object? data { get; set; }
    public int statusCode { get; set; }
    public string message { get; set; } = string.Empty;

    public static KetQuaPhanHoi ThanhCong(object? duLieu = null, string thongBao = "Thanh cong", int maTrangThai = 200)
    {
        return new KetQuaPhanHoi
        {
            data = duLieu,
            statusCode = maTrangThai,
            message = thongBao
        };
    }

    public static KetQuaPhanHoi ThatBai(string thongBao = "That bai", int maTrangThai = 500, object? duLieu = null)
    {
        return new KetQuaPhanHoi
        {
            data = duLieu,
            statusCode = maTrangThai,
            message = thongBao
        };
    }
}
