using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class LoTrinh
{
    public int id { get; set; }

    public int tuyenXeId { get; set; }

    public string chieu { get; set; } = null!;

    public short phienBan { get; set; }

    public string tenLoTrinh { get; set; } = null!;

    public DateTime hieuLucTu { get; set; }

    public DateTime? hieuLucDen { get; set; }

    public virtual ICollection<ChuyenXe> ChuyenXes { get; set; } = new List<ChuyenXe>();

    public virtual ICollection<LoTrinhTramDung> LoTrinhTramDungs { get; set; } = new List<LoTrinhTramDung>();

    public virtual TuyenXe tuyenXe { get; set; } = null!;
}
