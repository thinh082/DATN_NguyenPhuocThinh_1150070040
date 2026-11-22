namespace Server.Entities;

public partial class TinhThanh
{
    public int ma { get; set; }

    public string ten { get; set; } = null!;

    public string loai { get; set; } = null!;

    public virtual ICollection<PhuongXa> PhuongXas { get; set; } = new List<PhuongXa>();
}
