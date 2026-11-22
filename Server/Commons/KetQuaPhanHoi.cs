namespace Server.Commons;

public class KetQuaPhanHoi
{
    private static readonly string TenAssembly = typeof(KetQuaPhanHoi).Assembly.GetName().Name!;

    public object? data { get; set; }
    public int statusCode { get; set; }
    public string message { get; set; } = string.Empty;

    public static KetQuaPhanHoi ThanhCong(object? duLieu = null, string thongBao = "Thành công", int maTrangThai = 200)
    {
        return new KetQuaPhanHoi
        {
            data = duLieu,
            statusCode = maTrangThai,
            message = thongBao
        };
    }

    public static KetQuaPhanHoi ThatBai(string thongBao = "Thất bại", int maTrangThai = 500, object? duLieu = null)
    {
        return new KetQuaPhanHoi
        {
            data = duLieu,
            statusCode = maTrangThai,
            message = thongBao
        };
    }

    public static KetQuaPhanHoi LoiHeThong(Exception ex, int maTrangThai = 500)
    {
        var loiNghiepVu = ex.Source == TenAssembly && ex is not (NullReferenceException or InvalidCastException or IndexOutOfRangeException or FormatException);
        if (loiNghiepVu) return ThatBai(ex.Message, maTrangThai);
        Console.Error.WriteLine($"[Loi] {ex}");
        return ThatBai("Hệ thống đang gặp sự cố, vui lòng thử lại sau.", maTrangThai);
    }
}
