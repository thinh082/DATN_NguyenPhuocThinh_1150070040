using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class Ve
{
    public long id { get; set; }

    public Guid maVe { get; set; }

    public long chiTietDonHangId { get; set; }

    public short soThuTu { get; set; }

    public int nguoiDungId { get; set; }

    public string maLoaiVe { get; set; } = null!;

    public int nhomDoiTuongId { get; set; }

    public int? tuyenXeId { get; set; }

    public int khoaKySoId { get; set; }

    public short phienBanDuLieu { get; set; }

    public byte[] duLieuDaKy { get; set; } = null!;

    public byte[] chuKy { get; set; } = null!;

    public byte[] biMatTotpMaHoa { get; set; } = null!;

    public string phienBanKhoaBaoVe { get; set; } = null!;

    public short chuKyTotpGiay { get; set; }

    public byte soChuSoTotp { get; set; }

    public DateTime hieuLucTu { get; set; }

    public DateTime hetHanLuc { get; set; }

    public string trangThai { get; set; } = null!;

    public DateTime ngayPhatHanh { get; set; }

    public byte[] phienBan { get; set; } = null!;

    public virtual ICollection<LuotDi> LuotDis { get; set; } = new List<LuotDi>();

    public virtual ICollection<NhatKyQuetVe> NhatKyQuetVes { get; set; } = new List<NhatKyQuetVe>();

    public virtual ICollection<NhatKyThuHoiVe> NhatKyThuHoiVes { get; set; } = new List<NhatKyThuHoiVe>();

    public virtual ChiTietDonHang chiTietDonHang { get; set; } = null!;

    public virtual KhoaKySo khoaKySo { get; set; } = null!;

    public virtual LoaiVe maLoaiVeNavigation { get; set; } = null!;

    public virtual NguoiDung nguoiDung { get; set; } = null!;

    public virtual NhomDoiTuong nhomDoiTuong { get; set; } = null!;

    public virtual TuyenXe? tuyenXe { get; set; }
}
