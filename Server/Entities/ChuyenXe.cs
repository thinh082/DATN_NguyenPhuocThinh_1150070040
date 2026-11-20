using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class ChuyenXe
{
    public long id { get; set; }

    public int loTrinhId { get; set; }

    public int xeBuytId { get; set; }

    public DateTime gioKhoiHanhDuKien { get; set; }

    public DateTime gioKetThucDuKien { get; set; }

    public DateTime? gioKhoiHanhThucTe { get; set; }

    public DateTime? gioKetThucThucTe { get; set; }

    public string trangThai { get; set; } = null!;

    public virtual ICollection<PhienSoatVe> PhienSoatVes { get; set; } = new List<PhienSoatVe>();

    public virtual LoTrinh loTrinh { get; set; } = null!;

    public virtual XeBuyt xeBuyt { get; set; } = null!;
}
