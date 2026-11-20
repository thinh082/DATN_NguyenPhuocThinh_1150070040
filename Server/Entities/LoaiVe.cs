using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class LoaiVe
{
    public string maLoai { get; set; } = null!;

    public string tenLoai { get; set; } = null!;

    public bool trangThai { get; set; }

    public virtual ICollection<GiaVe> GiaVes { get; set; } = new List<GiaVe>();

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
}
