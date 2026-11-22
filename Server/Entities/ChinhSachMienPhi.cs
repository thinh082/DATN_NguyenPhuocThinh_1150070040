namespace Server.Entities;

public partial class ChinhSachMienPhi
{
    public int id { get; set; }

    public string ma { get; set; } = null!;

    public string ten { get; set; } = null!;

    public string canCu { get; set; } = null!;

    public DateOnly hieuLucTu { get; set; }

    public DateOnly hieuLucDen { get; set; }

    public bool yeuCauXacThuc { get; set; }

    public bool trangThai { get; set; }

    public virtual ICollection<TuyenXe> TuyenXes { get; set; } = new List<TuyenXe>();
}
