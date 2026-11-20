using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class ThietBiSoatVe
{
    public int id { get; set; }

    public Guid maThietBi { get; set; }

    public string tenThietBi { get; set; } = null!;

    public string? khoaCongKhaiThietBi { get; set; }

    public DateTime? lanDongBoCuoi { get; set; }

    public long mocThuHoiDaNhan { get; set; }

    public bool trangThai { get; set; }

    public DateTime ngayDangKy { get; set; }

    public virtual ICollection<LoDongBo> LoDongBos { get; set; } = new List<LoDongBo>();

    public virtual ICollection<NhatKyQuetVe> NhatKyQuetVes { get; set; } = new List<NhatKyQuetVe>();

    public virtual PhienSoatVe? PhienSoatVe { get; set; }
}
