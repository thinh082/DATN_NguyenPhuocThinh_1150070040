using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class ThanhToan
{
    public long id { get; set; }

    public long donHangId { get; set; }

    public Guid maYeuCau { get; set; }

    public string congThanhToan { get; set; } = null!;

    public string? maGiaoDichCong { get; set; }

    public decimal soTien { get; set; }

    public string donViTien { get; set; } = null!;

    public string trangThai { get; set; } = null!;

    public DateTime ngayTao { get; set; }

    public DateTime? thanhCongLuc { get; set; }

    public virtual ICollection<SuKienThanhToan> SuKienThanhToans { get; set; } = new List<SuKienThanhToan>();

    public virtual DonHang donHang { get; set; } = null!;
}
