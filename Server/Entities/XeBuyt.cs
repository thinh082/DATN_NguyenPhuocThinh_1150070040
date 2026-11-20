using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class XeBuyt
{
    public int id { get; set; }

    public string bienSo { get; set; } = null!;

    public Guid maQrTinh { get; set; }

    public short sucChua { get; set; }

    public bool trangThai { get; set; }

    public virtual ICollection<ChuyenXe> ChuyenXes { get; set; } = new List<ChuyenXe>();
}
