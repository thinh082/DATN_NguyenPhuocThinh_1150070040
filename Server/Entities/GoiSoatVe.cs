namespace Server.Entities;

public class GoiSoatVe
{
    public Guid id { get; set; }
    public Guid phienSoatVeId { get; set; }
    public DateTime taoLuc { get; set; }
    public DateTime hetHanLuc { get; set; }
    public string danhSachVeHopLe { get; set; } = "[]";
}

public class XuLySuKienOffline
{
    public Guid id { get; set; }
    public Guid goiId { get; set; }
    public byte[] bamDuLieu { get; set; } = Array.Empty<byte>();
    public string ketQua { get; set; } = "";
    public string maLyDo { get; set; } = "";
}
