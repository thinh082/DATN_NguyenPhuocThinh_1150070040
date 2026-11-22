using System;
using System.Collections.Generic;

namespace Server.Entities;

public partial class NguoiDung
{
    public int id { get; set; }

    public string hoTen { get; set; } = null!;

    public string? soDienThoai { get; set; }

    public string? email { get; set; }

    public string? cccd { get; set; }

    public string? diaChi { get; set; }

    public int? maTinhThanh { get; set; }

    public int? maPhuongXa { get; set; }

    public string? phuongThucXacThuc { get; set; }

    public DateTime? ngayXacThuc { get; set; }

    public string? minhChungXacThuc { get; set; }

    public string matKhauBam { get; set; } = null!;

    public string vaiTro { get; set; } = null!;

    public bool trangThai { get; set; }

    public DateTime ngayTao { get; set; }

    public DateTime ngayCapNhat { get; set; }

    public byte[] phienBan { get; set; } = null!;

    public virtual ICollection<CanhBaoGianLan> CanhBaoGianLans { get; set; } = new List<CanhBaoGianLan>();

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual ICollection<HoSoUuTien> HoSoUuTiennguoiDungs { get; set; } = new List<HoSoUuTien>();

    public virtual ICollection<HoSoUuTien> HoSoUuTiennguoiDuyets { get; set; } = new List<HoSoUuTien>();

    public virtual ICollection<NhatKyThuHoiVe> NhatKyThuHoiVes { get; set; } = new List<NhatKyThuHoiVe>();

    public virtual ICollection<PhienSoatVe> PhienSoatVes { get; set; } = new List<PhienSoatVe>();

    public virtual ICollection<Ve> Ves { get; set; } = new List<Ve>();

    public virtual TinhThanh? tinhThanh { get; set; }

    public virtual PhuongXa? phuongXa { get; set; }
}
