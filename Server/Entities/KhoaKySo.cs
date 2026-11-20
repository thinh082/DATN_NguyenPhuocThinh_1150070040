using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class KhoaKySo
{
    public int id { get; set; }

    public string maKhoa { get; set; } = null!;

    public string thuatToan { get; set; } = null!;

    public string khoaCongKhaiPem { get; set; } = null!;

    public string thamChieuKhoaBiMat { get; set; } = null!;

    public DateTime hieuLucTu { get; set; }

    public DateTime hetHanLuc { get; set; }

    public string trangThai { get; set; } = null!;

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();
}
