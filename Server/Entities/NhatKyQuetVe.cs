using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class NhatKyQuetVe
{
    public long id { get; set; }

    public Guid maSuKien { get; set; }

    public int thietBiSoatVeId { get; set; }

    public Guid phienSoatVeId { get; set; }

    public Guid? loDongBoId { get; set; }

    public long soThuTuThietBi { get; set; }

    public long? veId { get; set; }

    public string? maVeDocDuoc { get; set; }

    public byte[]? bamDuLieuQr { get; set; }

    public long? buocThoiGian { get; set; }

    public string phuongThuc { get; set; } = null!;

    public string cheDo { get; set; } = null!;

    public string ketQuaTaiThietBi { get; set; } = null!;

    public string? maLyDo { get; set; }

    public DateTime thoiDiemQuet { get; set; }

    public DateTime thoiDiemNhan { get; set; }

    public int? doLechDongHoGiay { get; set; }

    public virtual ICollection<CanhBaoGianLan> CanhBaoGianLans { get; set; } = new List<CanhBaoGianLan>();

    public virtual LoDongBo? LoDongBo { get; set; }

    public virtual ICollection<LuotDi> LuotDis { get; set; } = new List<LuotDi>();

    public virtual PhienSoatVe PhienSoatVe { get; set; } = null!;

    public virtual ThietBiSoatVe thietBiSoatVe { get; set; } = null!;

    public virtual Ve ve { get; set; } = null!;
}
