using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class LoTrinhTramDung
{
    public int id { get; set; }

    public int loTrinhId { get; set; }

    public int tramDungId { get; set; }

    public short thuTu { get; set; }

    public decimal? khoangCachTuDauKm { get; set; }

    public short? phutTuDau { get; set; }

    public virtual LoTrinh loTrinh { get; set; } = null!;

    public virtual TramDung tramDung { get; set; } = null!;
}
