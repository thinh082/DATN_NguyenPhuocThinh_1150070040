using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class TuyenXe
{
    public int id { get; set; }

    public string maTuyen { get; set; } = null!;

    public string tenTuyen { get; set; } = null!;

    public TimeOnly gioBatDau { get; set; }

    public TimeOnly gioKetThuc { get; set; }

    public short? gianCachPhut { get; set; }

    public bool trangThai { get; set; }

    public int? chinhSachMienPhiId { get; set; }

    public decimal? cuLyKm { get; set; }

    public virtual ChinhSachMienPhi? chinhSachMienPhi { get; set; }

    public virtual ICollection<GiaVe> GiaVes { get; set; } = new List<GiaVe>();

    public virtual ICollection<LoTrinh> LoTrinhs { get; set; } = new List<LoTrinh>();

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
}
