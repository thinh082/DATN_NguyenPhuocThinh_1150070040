using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class LoDongBo
{
    public Guid id { get; set; }

    public int thietBiSoatVeId { get; set; }

    public int soSuKienKhaiBao { get; set; }

    public int soSuKienDaXuLy { get; set; }

    public string trangThai { get; set; } = null!;

    public DateTime nhanLuc { get; set; }

    public DateTime? hoanTatLuc { get; set; }

    public virtual ICollection<NhatKyQuetVe> NhatKyQuetVes { get; set; } = new List<NhatKyQuetVe>();

    public virtual ThietBiSoatVe thietBiSoatVe { get; set; } = null!;
}
