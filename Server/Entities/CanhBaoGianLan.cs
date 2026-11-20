using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class CanhBaoGianLan
{
    public long id { get; set; }

    public long nhatKyQuetVeId { get; set; }

    public string maCanhBao { get; set; } = null!;

    public string noiDung { get; set; } = null!;

    public string trangThai { get; set; } = null!;

    public DateTime ngayTao { get; set; }

    public int? nguoiXuLyId { get; set; }

    public DateTime? xuLyLuc { get; set; }

    public string? ketLuan { get; set; }

    public virtual NguoiDung? nguoiXuLy { get; set; }

    public virtual NhatKyQuetVe nhatKyQuetVe { get; set; } = null!;
}
