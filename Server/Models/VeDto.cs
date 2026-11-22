namespace Server.Models;

public class TaoDonHangDto
{
    public int nguoiDungId { get; set; }
    public int giaVeId { get; set; }
    public short soLuong { get; set; } = 1;
    public Guid? maYeuCau { get; set; }
}

public class ThanhToanDonHangDto
{
    public long donHangId { get; set; }
    public string congThanhToan { get; set; } = "MoMo";
    public string maGiaoDichCong { get; set; } = string.Empty;
}

public class ThanhToanGiaLapDto
{
    public long donHangId { get; set; }
    public Guid requestId { get; set; }
    public string ketQua { get; set; } = "ThanhCong"; // "ThanhCong" | "ThatBai" | "Huy"
}

public class KhoaVeDto
{
    public Guid maVe { get; set; }
    public string lyDo { get; set; } = string.Empty;
    public int nguoiThucHienId { get; set; }
    public bool thuHoiVinhVien { get; set; } = false;
}

public class BieuGiaListItemDto
{
    public int id { get; set; }
    public string maLoaiVe { get; set; } = string.Empty;
    public string tenLoaiVe { get; set; } = string.Empty;
    public int nhomDoiTuongId { get; set; }
    public string tenNhomDoiTuong { get; set; } = string.Empty;
    public int? tuyenXeId { get; set; }
    public string tenTuyen { get; set; } = string.Empty;
    public string? maTuyen { get; set; }
    public decimal giaGoc { get; set; }
    public decimal soTienMienGiam { get; set; }
    public decimal giaThucThu { get; set; }
    public string? canCuChinhSach { get; set; }
}
