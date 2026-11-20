using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class ChiTietDonHang
{
    public long id { get; set; }

    public long donHangId { get; set; }

    public int giaVeId { get; set; }

    public short soLuong { get; set; }

    public decimal donGiaGoc { get; set; }

    public decimal mienGiamMoiVe { get; set; }

    public decimal? thanhTien { get; set; }

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();

    public virtual DonHang donHang { get; set; } = null!;

    public virtual GiaVe giaVe { get; set; } = null!;
}
