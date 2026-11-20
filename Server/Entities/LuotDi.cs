using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class LuotDi
{
    public long id { get; set; }

    public long nhatKyQuetVeId { get; set; }

    public long veId { get; set; }

    public string maLoaiVe { get; set; } = null!;

    public long? buocThoiGian { get; set; }

    public string trangThai { get; set; } = null!;

    public DateTime congNhanLuc { get; set; }

    public DateTime? huyLuc { get; set; }

    public string? lyDoHuy { get; set; }

    public virtual NhatKyQuetVe NhatKyQuetVe { get; set; } = null!;

    public virtual Ve Ve { get; set; } = null!;
}
