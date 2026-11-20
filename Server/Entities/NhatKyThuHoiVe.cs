using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class NhatKyThuHoiVe
{
    public long id { get; set; }

    public long veId { get; set; }

    public string hanhDong { get; set; } = null!;

    public string lyDo { get; set; } = null!;

    public int? nguoiThucHienId { get; set; }

    public DateTime ngayTao { get; set; }

    public virtual NguoiDung? nguoiThucHien { get; set; }

    public virtual Ve ve { get; set; } = null!;
}
