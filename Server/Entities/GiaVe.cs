using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class GiaVe
{
    public int id { get; set; }

    public string maLoaiVe { get; set; } = null!;

    public int nhomDoiTuongId { get; set; }

    public int? tuyenXeId { get; set; }

    public decimal giaGoc { get; set; }

    public decimal soTienMienGiam { get; set; }

    public DateTime hieuLucTu { get; set; }

    public DateTime? hieuLucDen { get; set; }

    public string? canCuChinhSach { get; set; }

    public bool trangThai { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual LoaiVe maLoaiVeNavigation { get; set; } = null!;

    public virtual NhomDoiTuong nhomDoiTuong { get; set; } = null!;

    public virtual TuyenXe? tuyenXe { get; set; }
}
