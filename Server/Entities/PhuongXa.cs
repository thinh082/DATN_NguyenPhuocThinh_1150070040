namespace Server.Entities;

public partial class PhuongXa
{
    public int ma { get; set; }

    public string ten { get; set; } = null!;

    public string loai { get; set; } = null!;

    public int maTinhThanh { get; set; }

    public virtual TinhThanh tinhThanh { get; set; } = null!;
}
