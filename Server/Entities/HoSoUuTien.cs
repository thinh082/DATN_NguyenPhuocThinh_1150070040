using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class HoSoUuTien
{
    public int id { get; set; }

    public int nguoiDungId { get; set; }

    public int nhomDoiTuongId { get; set; }

    public string duongDanMinhChung { get; set; } = null!;

    public string trangThai { get; set; } = null!;

    public DateOnly hieuLucTu { get; set; }

    public DateOnly hieuLucDen { get; set; }

    public int? nguoiDuyetId { get; set; }

    public DateTime? ngayDuyet { get; set; }

    public string? lyDo { get; set; }

    public DateTime ngayTao { get; set; }

    public virtual NguoiDung nguoiDung { get; set; } = null!;

    public virtual NguoiDung? nguoiDuyet { get; set; }

    public virtual NhomDoiTuong nhomDoiTuong { get; set; } = null!;
}
