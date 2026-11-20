using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class DonHang
{
    public long id { get; set; }

    public Guid maDonHang { get; set; }

    public int nguoiDungId { get; set; }

    public Guid maYeuCau { get; set; }

    public decimal tongGiaGoc { get; set; }

    public decimal tongMienGiam { get; set; }

    public decimal? tongThanhToan { get; set; }

    public string trangThai { get; set; } = null!;

    public DateTime ngayTao { get; set; }

    public DateTime hetHanLuc { get; set; }

    public byte[] phienBan { get; set; } = null!;

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual ThanhToan? ThanhToan { get; set; }

    public virtual NguoiDung nguoiDung { get; set; } = null!;
}
