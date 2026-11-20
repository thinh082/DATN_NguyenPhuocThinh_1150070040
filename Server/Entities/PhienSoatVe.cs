using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class PhienSoatVe
{
    public Guid id { get; set; }

    public long chuyenXeId { get; set; }

    public int thietBiSoatVeId { get; set; }

    public int nhanVienId { get; set; }

    public DateTime batDauLuc { get; set; }

    public DateTime? ketThucLuc { get; set; }

    public virtual ICollection<NhatKyQuetVe> NhatKyQuetVes { get; set; } = new List<NhatKyQuetVe>();

    public virtual ChuyenXe chuyenXe { get; set; } = null!;

    public virtual NguoiDung nhanVien { get; set; } = null!;

    public virtual ThietBiSoatVe thietBiSoatVe { get; set; } = null!;
}
