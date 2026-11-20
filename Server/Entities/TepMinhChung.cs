using System;

namespace Server.Entities;

public partial class TepMinhChung
{
    public int id { get; set; }
    public Guid maTep { get; set; } = Guid.NewGuid();
    public int nguoiDungId { get; set; }
    public string tenFileGoc { get; set; } = null!;
    public string tenFileLuu { get; set; } = null!;
    public string loaiFile { get; set; } = null!;
    public long kichThuoc { get; set; }
    public bool daGanHoSo { get; set; }
    public DateTime ngayTao { get; set; } = DateTime.UtcNow;

    public virtual NguoiDung nguoiDung { get; set; } = null!;
}
