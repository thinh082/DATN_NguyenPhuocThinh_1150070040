using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class SuKienThanhToan
{
    public long id { get; set; }

    public string congThanhToan { get; set; } = null!;

    public string maSuKienCong { get; set; } = null!;

    public long? thanhToanId { get; set; }

    public string noiDung { get; set; } = null!;

    public bool chuKyHopLe { get; set; }

    public string trangThaiXuLy { get; set; } = null!;

    public DateTime nhanLuc { get; set; }

    public DateTime? xuLyLuc { get; set; }

    public string? loiXuLy { get; set; }

    public virtual ThanhToan? thanhToan { get; set; }
}
