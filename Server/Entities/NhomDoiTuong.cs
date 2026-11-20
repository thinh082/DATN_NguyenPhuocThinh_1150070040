using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class NhomDoiTuong
{
    public int id { get; set; }

    public string maNhom { get; set; } = null!;

    public string tenNhom { get; set; } = null!;

    public bool canXacMinh { get; set; }

    public bool trangThai { get; set; }

    public virtual ICollection<GiaVe> GiaVes { get; set; } = new List<GiaVe>();

    public virtual ICollection<HoSoUuTien> HoSoUuTiens { get; set; } = new List<HoSoUuTien>();

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
}
