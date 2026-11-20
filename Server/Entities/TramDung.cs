using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class TramDung
{
    public int id { get; set; }

    public string maTram { get; set; } = null!;

    public string tenTram { get; set; } = null!;

    public string? diaChi { get; set; }

    public decimal viDo { get; set; }

    public decimal kinhDo { get; set; }

    public bool trangThai { get; set; }

    public virtual ICollection<LoTrinhTramDung> LoTrinhTramDungs { get; set; } = new List<LoTrinhTramDung>();
}
