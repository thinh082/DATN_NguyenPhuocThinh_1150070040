import React, { useCallback, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import {
  Activity,
  ArrowDown,
  ArrowDownLeft,
  ArrowRight,
  ArrowUp,
  BadgeCheck,
  Bell,
  BusFront,
  Check,
  ChevronDown,
  CircleDollarSign,
  Clock3,
  CreditCard,
  Download,
  FileCheck2,
  FileSpreadsheet,
  LayoutDashboard,
  LogOut,
  MapPin,
  Menu,
  Plus,
  Printer,
  RefreshCw,
  Route,
  Search,
  ShieldCheck,
  Ticket,
  Trash2,
  Users,
  X,
} from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import './style.css';

type Row = Record<string, any>;
type Page =
  | 'overview'
  | 'routes'
  | 'patterns'
  | 'stops'
  | 'vehicles'
  | 'trips'
  | 'fares'
  | 'users'
  | 'profiles'
  | 'tickets'
  | 'orders'
  | 'scans'
  | 'alerts';

type Field = { key: string; label: string; type?: string; required?: boolean; options?: [string, string][] };
type Section = {
  editOnly?: boolean;
  title: string;
  desc: string;
  endpoint: string;
  columns: [string, string][];
  icon: React.ElementType;
  fields?: Field[];
  save?: string;
};

const sections: Record<Exclude<Page, 'overview'>, Section> = {
  routes: {
    title: 'Tuyến xe',
    desc: 'Quản lý mạng lưới tuyến xe buýt.',
    endpoint: '/api/VanHanh/danh-sach-tuyen',
    icon: Route,
    columns: [
      ['maTuyen', 'Mã tuyến'],
      ['tenTuyen', 'Tên tuyến'],
      ['gioBatDau', 'Bắt đầu'],
      ['gioKetThuc', 'Kết thúc'],
      ['gianCachPhut', 'Giãn cách (phút)'],
      ['trangThai', 'Trạng thái'],
    ],
    save: '/api/VanHanh/luu-tuyen-xe',
    fields: [
      { key: 'maTuyen', label: 'Mã tuyến', required: true },
      { key: 'tenTuyen', label: 'Tên tuyến', required: true },
      { key: 'gioBatDau', label: 'Giờ bắt đầu', type: 'time', required: true },
      { key: 'gioKetThuc', label: 'Giờ kết thúc', type: 'time', required: true },
      { key: 'gianCachPhut', label: 'Giãn cách (phút)', type: 'number' },
      { key: 'trangThai', label: 'Trạng thái', type: 'boolean' },
    ],
  },
  patterns: {
    title: 'Lộ trình tuyến',
    desc: 'Quản lý phiên bản lộ trình và thứ tự trạm dừng (1..N).',
    endpoint: '/api/Admin/lo-trinh',
    icon: Route,
    columns: [
      ['maTuyen', 'Mã tuyến'],
      ['tenTuyen', 'Tuyến'],
      ['chieu', 'Chiều'],
      ['phienBan', 'Phiên bản'],
      ['tenLoTrinh', 'Tên lộ trình'],
      ['soTram', 'Số trạm'],
      ['hieuLucTu', 'Hiệu lực từ'],
      ['hieuLucDen', 'Hiệu lực đến'],
    ],
  },
  stops: {
    title: 'Trạm dừng',
    desc: 'Danh sách các điểm dừng trong mạng lưới.',
    endpoint: '/api/VanHanh/danh-sach-tram-dung',
    icon: MapPin,
    columns: [
      ['maTram', 'Mã trạm'],
      ['tenTram', 'Tên trạm'],
      ['diaChi', 'Địa chỉ'],
      ['trangThai', 'Trạng thái'],
    ],
    save: '/api/VanHanh/luu-tram-dung',
    fields: [
      { key: 'maTram', label: 'Mã trạm', required: true },
      { key: 'tenTram', label: 'Tên trạm', required: true },
      { key: 'diaChi', label: 'Địa chỉ' },
      { key: 'viDo', label: 'Vĩ độ', type: 'number' },
      { key: 'kinhDo', label: 'Kinh độ', type: 'number' },
      { key: 'trangThai', label: 'Trạng thái', type: 'boolean' },
    ],
  },
  vehicles: {
    title: 'Phương tiện',
    desc: 'Theo dõi phương tiện và mã QR cố định trên xe.',
    endpoint: '/api/VanHanh/danh-sach-xe-buyt',
    icon: BusFront,
    columns: [
      ['bienSo', 'Biển số'],
      ['sucChua', 'Sức chứa'],
      ['maQrTinh', 'Mã QR cố định'],
      ['trangThai', 'Trạng thái'],
    ],
    save: '/api/VanHanh/luu-xe-buyt',
    fields: [
      { key: 'bienSo', label: 'Biển số', required: true },
      { key: 'sucChua', label: 'Sức chứa', type: 'number', required: true },
      { key: 'trangThai', label: 'Trạng thái', type: 'boolean' },
    ],
  },
  trips: {
    title: 'Chuyến xe',
    desc: 'Theo dõi các chuyến dự kiến và đang hoạt động.',
    endpoint: '/api/Admin/chuyen-xe',
    icon: Clock3,
    columns: [
      ['id', 'Mã'],
      ['tenTuyen', 'Tuyến'],
      ['bienSo', 'Xe'],
      ['gioKhoiHanhDuKien', 'Khởi hành'],
      ['gioKetThucDuKien', 'Kết thúc'],
      ['trangThai', 'Trạng thái'],
    ],
  },
  fares: {
    editOnly: true,
    save: '/api/Admin/gia-ve',
    fields: [
      { key: 'giaGoc', label: 'Giá gốc (đ)', type: 'number', required: true },
      { key: 'soTienMienGiam', label: 'Số tiền miễn giảm (đ)', type: 'number', required: true },
    ],
    title: 'Biểu giá',
    desc: 'Giá vé theo tuyến và nhóm hành khách.',
    endpoint: '/api/Ve/danh-sach-bieu-gia',
    icon: CircleDollarSign,
    columns: [
      ['tenLoaiVe', 'Loại vé'],
      ['tenNhomDoiTuong', 'Đối tượng'],
      ['tenTuyen', 'Tuyến'],
      ['giaGoc', 'Giá gốc'],
      ['soTienMienGiam', 'Miễn giảm'],
      ['giaThucThu', 'Thực thu'],
    ],
  },
  users: {
    title: 'Tài khoản',
    desc: 'Quản lý hành khách và nhân viên soát vé.',
    endpoint: '/api/NguoiDung/lay-danh-sach',
    icon: Users,
    columns: [
      ['hoTen', 'Họ và tên'],
      ['email', 'Email'],
      ['soDienThoai', 'Số điện thoại'],
      ['vaiTro', 'Vai trò'],
      ['trangThai', 'Trạng thái'],
    ],
    save: '/api/NguoiDung/luu-nguoi-dung',
    fields: [
      { key: 'hoTen', label: 'Họ và tên', required: true },
      { key: 'email', label: 'Email', type: 'email' },
      { key: 'soDienThoai', label: 'Số điện thoại' },
      { key: 'cccd', label: 'CCCD' },
      {
        key: 'vaiTro',
        label: 'Vai trò',
        type: 'select',
        options: [
          ['NhanVienSoatVe', 'Nhân viên soát vé'],
          ['HanhKhach', 'Hành khách'],
        ],
      },
      { key: 'matKhau', label: 'Mật khẩu khởi tạo', type: 'password', required: true },
      { key: 'trangThai', label: 'Trạng thái', type: 'boolean' },
    ],
  },
  profiles: {
    title: 'Hồ sơ ưu tiên',
    desc: 'Xét duyệt hồ sơ hành khách đang chờ.',
    endpoint: '/api/XacThuc/danh-sach-ho-so-cho-duyet',
    icon: FileCheck2,
    columns: [
      ['hoTenNguoiDung', 'Hành khách'],
      ['tenNhom', 'Nhóm đối tượng'],
      ['soDienThoai', 'Số điện thoại'],
      ['hieuLucTu', 'Hiệu lực từ'],
      ['hieuLucDen', 'Đến ngày'],
      ['duongDanMinhChung', 'Minh chứng'],
    ],
  },
  tickets: {
    title: 'Vé điện tử',
    desc: 'Theo dõi vé đã phát hành và trạng thái sử dụng.',
    endpoint: '/api/Admin/ve',
    icon: Ticket,
    columns: [
      ['maVe', 'Mã vé'],
      ['hoTen', 'Hành khách'],
      ['maLoaiVe', 'Loại vé'],
      ['tenTuyen', 'Tuyến'],
      ['hetHanLuc', 'Hết hạn'],
      ['trangThai', 'Trạng thái'],
    ],
  },
  orders: {
    title: 'Đơn hàng',
    desc: 'Theo dõi đơn và thanh toán mô phỏng.',
    endpoint: '/api/Admin/don-hang',
    icon: CreditCard,
    columns: [
      ['maDonHang', 'Mã đơn'],
      ['hoTen', 'Hành khách'],
      ['tongGiaGoc', 'Giá gốc'],
      ['tongMienGiam', 'Miễn giảm'],
      ['trangThai', 'Trạng thái'],
      ['ngayTao', 'Ngày tạo'],
    ],
  },
  scans: {
    title: 'Nhật ký soát vé',
    desc: 'Theo dõi các sự kiện quét và đồng bộ.',
    endpoint: '/api/Admin/nhat-ky',
    icon: Activity,
    columns: [
      ['thoiDiemQuet', 'Thời gian'],
      ['thietBi', 'Thiết bị'],
      ['maVeDocDuoc', 'Mã vé'],
      ['cheDo', 'Chế độ'],
      ['ketQuaTaiThietBi', 'Kết quả'],
      ['maLyDo', 'Lý do'],
    ],
  },
  alerts: {
    title: 'Cảnh báo',
    desc: 'Theo dõi và xử lý các sự kiện cần đối soát.',
    endpoint: '/api/Admin/canh-bao',
    icon: Bell,
    columns: [
      ['ngayTao', 'Thời gian'],
      ['maCanhBao', 'Mã cảnh báo'],
      ['maVeDocDuoc', 'Mã vé'],
      ['noiDung', 'Nội dung'],
      ['trangThai', 'Trạng thái'],
    ],
  },
};

const nav: { label: string; items: { id: Page; label: string; icon: React.ElementType }[] }[] = [
  { label: 'TỔNG QUAN', items: [{ id: 'overview', label: 'Bảng điều khiển', icon: LayoutDashboard }] },
  {
    label: 'VẬN HÀNH',
    items: [
      { id: 'routes', label: 'Tuyến xe', icon: Route },
      { id: 'patterns', label: 'Lộ trình & Trạm', icon: Route },
      { id: 'stops', label: 'Trạm dừng', icon: MapPin },
      { id: 'vehicles', label: 'Phương tiện', icon: BusFront },
      { id: 'trips', label: 'Chuyến xe', icon: Clock3 },
      { id: 'fares', label: 'Biểu giá', icon: CircleDollarSign },
    ],
  },
  {
    label: 'QUẢN LÝ',
    items: [
      { id: 'users', label: 'Tài khoản', icon: Users },
      { id: 'profiles', label: 'Hồ sơ ưu tiên', icon: FileCheck2 },
      { id: 'tickets', label: 'Vé điện tử', icon: Ticket },
      { id: 'orders', label: 'Đơn hàng', icon: CreditCard },
    ],
  },
  {
    label: 'GIÁM SÁT',
    items: [
      { id: 'scans', label: 'Nhật ký soát vé', icon: Activity },
      { id: 'alerts', label: 'Cảnh báo', icon: Bell },
    ],
  },
];

let token = sessionStorage.getItem('smartbus_admin_token') || '';

async function api<T = any>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });
  if (response.status === 401) {
    token = '';
    sessionStorage.removeItem('smartbus_admin_token');
    window.dispatchEvent(new Event('session-expired'));
    throw new Error('Phiên đăng nhập đã hết hạn.');
  }
  const body = await response.json().catch(() => null);
  if (!response.ok || body?.statusCode !== 200) {
    throw new Error(body?.message || `Không thể thực hiện yêu cầu (${response.status}).`);
  }
  return body.data as T;
}

const post = (path: string, body: unknown) => api(path, { method: 'POST', body: JSON.stringify(body) });

const human: Record<string, string> = {
  HanhKhach: 'Hành khách',
  NhanVienSoatVe: 'Nhân viên',
  QuanTriVien: 'Quản trị viên',
  HoatDong: 'Hoạt động',
  TamKhoa: 'Tạm khóa',
  ThuHoi: 'Thu hồi',
  ChoThanhToan: 'Chờ thanh toán',
  DaThanhToan: 'Đã thanh toán',
  MienPhi: 'Miễn phí',
  DangChay: 'Đang chạy',
  DuKien: 'Dự kiến',
  HoanThanh: 'Hoàn thành',
  Huy: 'Đã hủy',
  Moi: 'Mới',
  DangXuLy: 'Đang xử lý',
  DaXuLy: 'Đã xử lý',
  BoQua: 'Bỏ qua',
  ChapNhan: 'Chấp nhận',
  TuChoi: 'Từ chối',
  VeLuot: 'Vé lượt',
  VeThang: 'Vé tháng',
  Online: 'Trực tuyến',
  Offline: 'Ngoại tuyến',
  Di: 'Lượt đi',
  Ve: 'Lượt về',
};

async function downloadCsv(loai: string) {
  try {
    const res = await fetch(`/api/Admin/xuat-csv/${loai}`, {
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    });
    if (!res.ok) {
      alert('Không thể xuất tệp CSV.');
      return;
    }
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `smartbus_${loai}_${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);
  } catch (err: any) {
    alert(err.message || 'Lỗi khi tải tệp CSV');
  }
}

async function xemMinhChung(duongDan: string) {
  if (!duongDan) return;
  if (/^https?:\/\//i.test(duongDan)) {
    window.open(duongDan, '_blank', 'noopener,noreferrer');
    return;
  }
  try {
    const url = duongDan.startsWith('/') ? duongDan : `/${duongDan}`;
    const res = await fetch(url, {
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    });
    if (!res.ok) {
      alert(`Không thể mở tệp minh chứng (HTTP ${res.status}).`);
      return;
    }
    const blob = await res.blob();
    const objectUrl = window.URL.createObjectURL(blob);
    window.open(objectUrl, '_blank');
  } catch (err: any) {
    alert(err.message || 'Lỗi khi mở tệp minh chứng.');
  }
}

function downloadQrPng(bienSo: string) {
  const svg = document.getElementById('vehicle-qr-svg');
  if (!svg) return;
  const svgData = new XMLSerializer().serializeToString(svg);
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');
  const img = new Image();
  img.onload = () => {
    canvas.width = img.width + 40;
    canvas.height = img.height + 80;
    if (ctx) {
      ctx.fillStyle = '#FFFFFF';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(img, 20, 20);
      ctx.fillStyle = '#0F172A';
      ctx.font = 'bold 16px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(`XE BUÝT: ${bienSo}`, canvas.width / 2, img.height + 50);
      const a = document.createElement('a');
      a.download = `QR_Xe_${bienSo}.png`;
      a.href = canvas.toDataURL('image/png');
      a.click();
    }
  };
  img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
}

function NewTrip({ close, done }: { close: () => void; done: () => void }) {
  const [routes, setRoutes] = useState<Row[]>([]);
  const [vehicles, setVehicles] = useState<Row[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    Promise.all([api<Row[]>('/api/Admin/lo-trinh'), api<Row[]>('/api/VanHanh/danh-sach-xe-buyt')])
      .then(([r, v]) => {
        setRoutes(r);
        setVehicles(v.filter((x) => x.trangThai));
      })
      .catch((e) => setError(e.message));
  }, []);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    try {
      setBusy(true);
      setError('');
      await post('/api/Admin/chuyen-xe', {
        loTrinhId: Number(form.get('loTrinhId')),
        xeBuytId: Number(form.get('xeBuytId')),
        gioKhoiHanhDuKien: new Date(String(form.get('start'))).toISOString(),
        gioKetThucDuKien: new Date(String(form.get('end'))).toISOString(),
      });
      done();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không tạo được chuyến.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="modal-backdrop" onClick={close}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-heading">
          <div>
            <span className="eyebrow">VẬN HÀNH</span>
            <h2>Tạo chuyến xe</h2>
          </div>
          <button className="icon-button" onClick={close}>
            <X size={18} />
          </button>
        </div>
        {error && <div className="error">{error}</div>}
        <form onSubmit={submit}>
          <div className="form-grid">
            <label>
              Lộ trình *
              <select name="loTrinhId" required defaultValue="">
                <option value="" disabled>
                  Chọn lộ trình
                </option>
                {routes.map((x) => (
                  <option value={x.id} key={x.id}>
                    {x.tenTuyen} · {x.tenLoTrinh} ({human[x.chieu] || x.chieu})
                  </option>
                ))}
              </select>
            </label>
            <label>
              Phương tiện *
              <select name="xeBuytId" required defaultValue="">
                <option value="" disabled>
                  Chọn xe
                </option>
                {vehicles.map((x) => (
                  <option value={x.id} key={x.id}>
                    {x.bienSo}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Khởi hành dự kiến *<input name="start" type="datetime-local" required />
            </label>
            <label>
              Kết thúc dự kiến *<input name="end" type="datetime-local" required />
            </label>
          </div>
          <div className="modal-actions">
            <button type="button" className="btn outline" onClick={close}>
              Hủy
            </button>
            <button className="btn primary" disabled={busy || !routes.length || !vehicles.length}>
              Tạo chuyến
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function NewPattern({ close, done }: { close: () => void; done: () => void }) {
  const [tuyens, setTuyens] = useState<Row[]>([]);
  const [allStops, setAllStops] = useState<Row[]>([]);
  const [tuyenId, setTuyenId] = useState<number | ''>('');
  const [chieu, setChieu] = useState<'Di' | 'Ve'>('Di');
  const [tenLoTrinh, setTenLoTrinh] = useState('');
  const [hieuLucTu, setHieuLucTu] = useState(new Date().toISOString().slice(0, 16));
  const [hieuLucDen, setHieuLucDen] = useState('');
  const [selectedStops, setSelectedStops] = useState<Row[]>([]);
  const [stopToAdd, setStopToAdd] = useState<number | ''>('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    Promise.all([api<Row[]>('/api/VanHanh/danh-sach-tuyen'), api<Row[]>('/api/VanHanh/danh-sach-tram-dung')])
      .then(([tList, sList]) => {
        setTuyens(tList.filter((x) => x.trangThai));
        setAllStops(sList.filter((x) => x.trangThai));
      })
      .catch((e) => setError(e.message));
  }, []);

  function handleAddStop() {
    if (!stopToAdd) return;
    const stop = allStops.find((s) => s.id === Number(stopToAdd));
    if (!stop) return;
    if (selectedStops.some((s) => s.id === stop.id)) {
      alert('Trạm dừng này đã có trong lộ trình.');
      return;
    }
    setSelectedStops([...selectedStops, stop]);
    setStopToAdd('');
  }

  function moveStop(index: number, direction: 'up' | 'down') {
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= selectedStops.length) return;
    const updated = [...selectedStops];
    const [moved] = updated.splice(index, 1);
    updated.splice(targetIndex, 0, moved);
    setSelectedStops(updated);
  }

  function removeStop(index: number) {
    setSelectedStops(selectedStops.filter((_, i) => i !== index));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!tuyenId) {
      setError('Vui lòng chọn tuyến xe.');
      return;
    }
    if (!tenLoTrinh.trim()) {
      setError('Vui lòng nhập tên lộ trình.');
      return;
    }
    if (selectedStops.length < 2) {
      setError('Lộ trình phải có tối thiểu 2 trạm dừng.');
      return;
    }

    try {
      setBusy(true);
      setError('');
      await post('/api/Admin/lo-trinh', {
        tuyenXeId: Number(tuyenId),
        chieu,
        tenLoTrinh: tenLoTrinh.trim(),
        hieuLucTu: new Date(hieuLucTu).toISOString(),
        hieuLucDen: hieuLucDen ? new Date(hieuLucDen).toISOString() : null,
        danhSachTramId: selectedStops.map((s) => s.id),
      });
      done();
    } catch (err: any) {
      setError(err.message || 'Không thể tạo lộ trình.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="modal-backdrop" onClick={close}>
      <div className="modal" style={{ maxWidth: 640 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-heading">
          <div>
            <span className="eyebrow">LỘ TRÌNH VÀ TRẠM</span>
            <h2>Tạo lộ trình tuyến mới</h2>
          </div>
          <button className="icon-button" onClick={close}>
            <X size={18} />
          </button>
        </div>
        {error && <div className="error">{error}</div>}
        <form onSubmit={submit}>
          <div className="form-grid">
            <label>
              Tuyến xe *
              <select
                required
                value={tuyenId}
                onChange={(e) => setTuyenId(Number(e.target.value))}
              >
                <option value="" disabled>
                  Chọn tuyến xe
                </option>
                {tuyens.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.maTuyen} - {t.tenTuyen}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Chiều di chuyển *
              <select
                value={chieu}
                onChange={(e) => setChieu(e.target.value as 'Di' | 'Ve')}
              >
                <option value="Di">Lượt đi (Di)</option>
                <option value="Ve">Lượt về (Ve)</option>
              </select>
            </label>
            <label style={{ gridColumn: '1 / -1' }}>
              Tên lộ trình *
              <input
                placeholder="Ví dụ: Bến xe Chợ Lớn → Bến Thành"
                value={tenLoTrinh}
                onChange={(e) => setTenLoTrinh(e.target.value)}
                required
              />
            </label>
            <label>
              Hiệu lực từ *
              <input
                type="datetime-local"
                value={hieuLucTu}
                onChange={(e) => setHieuLucTu(e.target.value)}
                required
              />
            </label>
            <label>
              Hiệu lực đến (tùy chọn)
              <input
                type="datetime-local"
                value={hieuLucDen}
                onChange={(e) => setHieuLucDen(e.target.value)}
              />
            </label>
          </div>

          <div style={{ marginTop: 20 }}>
            <span className="eyebrow">THỨ TỰ TRẠM DỪNG (1..N)</span>
            <p style={{ fontSize: 12, color: '#64748B', marginTop: 4 }}>
              Chọn trạm và sắp xếp theo đúng trình tự đón trả khách (tối thiểu 2 trạm).
            </p>

            <div style={{ display: 'flex', gap: 8, marginTop: 10 }}>
              <select
                style={{ flex: 1 }}
                value={stopToAdd}
                onChange={(e) => setStopToAdd(Number(e.target.value))}
              >
                <option value="">-- Chọn trạm dừng để thêm --</option>
                {allStops.map((s) => (
                  <option key={s.id} value={s.id}>
                    [{s.maTram}] {s.tenTram}
                  </option>
                ))}
              </select>
              <button
                type="button"
                className="btn outline"
                onClick={handleAddStop}
                disabled={!stopToAdd}
              >
                <Plus size={16} /> Thêm trạm
              </button>
            </div>

            <div style={{ marginTop: 12, maxHeight: 220, overflowY: 'auto', border: '1px solid #E2E8F0', borderRadius: 8, padding: 8 }}>
              {selectedStops.length === 0 ? (
                <div style={{ textAlign: 'center', padding: 20, color: '#94A3B8', fontSize: 12 }}>
                  Chưa có trạm dừng nào được chọn.
                </div>
              ) : (
                selectedStops.map((s, idx) => (
                  <div
                    key={s.id}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      padding: '8px 12px',
                      background: idx % 2 === 0 ? '#F8FAFC' : '#FFFFFF',
                      borderRadius: 6,
                      marginBottom: 4,
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <span style={{ fontWeight: 'bold', fontSize: 12, color: '#0F766E' }}>#{idx + 1}</span>
                      <span style={{ fontSize: 13, color: '#1E293B' }}>
                        [{s.maTram}] <strong>{s.tenTram}</strong>
                      </span>
                    </div>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button
                        type="button"
                        className="icon-button"
                        style={{ width: 28, height: 28 }}
                        disabled={idx === 0}
                        onClick={() => moveStop(idx, 'up')}
                      >
                        <ArrowUp size={14} />
                      </button>
                      <button
                        type="button"
                        className="icon-button"
                        style={{ width: 28, height: 28 }}
                        disabled={idx === selectedStops.length - 1}
                        onClick={() => moveStop(idx, 'down')}
                      >
                        <ArrowDown size={14} />
                      </button>
                      <button
                        type="button"
                        className="icon-button"
                        style={{ width: 28, height: 28, color: '#EF4444' }}
                        onClick={() => removeStop(idx)}
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn outline" onClick={close}>
              Hủy
            </button>
            <button className="btn primary" disabled={busy || selectedStops.length < 2}>
              <Check size={16} /> Lưu lộ trình
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function ViewPatternModal({ patternId, close }: { patternId: number; close: () => void }) {
  const [data, setData] = useState<Row | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api<Row>(`/api/Admin/lo-trinh/${patternId}`)
      .then(setData)
      .catch((e) => setError(e.message));
  }, [patternId]);

  return (
    <div className="modal-backdrop" onClick={close}>
      <div className="modal" style={{ maxWidth: 580 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-heading">
          <div>
            <span className="eyebrow">CHI TIẾT LỘ TRÌNH</span>
            <h2>{data?.tenLoTrinh || 'Đang tải…'}</h2>
          </div>
          <button className="icon-button" onClick={close}>
            <X size={18} />
          </button>
        </div>
        {error && <div className="error">{error}</div>}
        {data && (
          <div>
            <div style={{ display: 'flex', gap: 16, marginBottom: 16, fontSize: 12, color: '#64748B' }}>
              <span>Tuyến: <strong>{data.tenTuyen}</strong></span>
              <span>Chiều: <strong>{human[data.chieu] || data.chieu}</strong></span>
              <span>Phiên bản: <strong>v{data.phienBan}</strong></span>
              <span>Số chuyến: <strong>{data.soChuyen}</strong></span>
            </div>
            <div style={{ maxHeight: 350, overflowY: 'auto', border: '1px solid #E2E8F0', borderRadius: 8 }}>
              {data.danhSachTram?.map((s: any) => (
                <div
                  key={s.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    padding: '10px 14px',
                    borderBottom: '1px solid #F1F5F9',
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <span style={{ fontWeight: 'bold', color: '#0F766E' }}>#{s.thuTu}</span>
                    <div>
                      <div style={{ fontSize: 13, fontWeight: '600' }}>{s.tenTram}</div>
                      <div style={{ fontSize: 11, color: '#94A3B8' }}>{s.diaChi || `Mã: ${s.maTram}`}</div>
                    </div>
                  </div>
                  <div style={{ fontSize: 11, color: '#64748B', textAlign: 'right' }}>
                    {s.khoangCachTuDauKm ? `${s.khoangCachTuDauKm} km` : '0 km'}
                    {s.phutTuDau ? ` · +${s.phutTuDau}p` : ''}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
        <div className="modal-actions">
          <button type="button" className="btn primary" onClick={close}>
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}

function format(value: unknown, key = ''): string {
  if (value === null || value === undefined || value === '') return '—';
  if (typeof value === 'boolean') return value ? 'Hoạt động' : 'Tạm khóa';
  if (
    typeof value === 'number' &&
    ['giaGoc', 'soTienMienGiam', 'giaThucThu', 'tongGiaGoc', 'tongMienGiam', 'doanhThu', 'doanhThuMock', 'soTienTroGia'].includes(key)
  )
    return `${value.toLocaleString('vi-VN')} ₫`;
  const text = String(value);
  if (/^\d{4}-\d{2}-\d{2}$/.test(text)) return text.split('-').reverse().join('/');
  if (/^(ngay|gio|thoiDiem|hetHan|hieuLuc)/i.test(key) && /^\d{4}-\d\d-\d\d/.test(text))
    return new Date(text.endsWith('Z') || /[+-]\d\d:\d\d$/.test(text) ? text : `${text}Z`).toLocaleString('vi-VN');
  return human[text] || text;
}

function Login({ onLogin }: { onLogin: (name: string) => void }) {
  const [account, setAccount] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      const result = await api<Row>('/api/XacThuc/dang-nhap', {
        method: 'POST',
        body: JSON.stringify({ taiKhoan: account.trim(), matKhau: password }),
      });
      if (result.nguoiDung?.vaiTro !== 'QuanTriVien') throw new Error('Tài khoản không có quyền quản trị.');
      token = result.token;
      sessionStorage.setItem('smartbus_admin_token', token);
      sessionStorage.setItem('smartbus_admin_name', result.nguoiDung.hoTen);
      onLogin(result.nguoiDung.hoTen);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể đăng nhập.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="login-shell">
      <div className="login-art">
        <div className="brand light">
          <div className="brand-mark">
            <BusFront size={21} />
          </div>
          <div>
            <strong>smartbus<span>.</span></strong>
            <small>HỆ THỐNG ĐIỀU HÀNH</small>
          </div>
        </div>
        <div className="login-copy">
          <span className="eyebrow inverse">NỀN TẢNG GIAO THÔNG THÔNG MINH</span>
          <h1>
            Điều hành
            <br />
            chuyển động<span>.</span>
          </h1>
          <p>Một nơi để theo dõi tuyến xe, lượt đi và vận hành hệ thống vé điện tử.</p>
          <div className="login-line">
            <ShieldCheck size={18} /> Không gian dành cho quản trị viên
          </div>
        </div>
        <div className="login-footer">SMARTBUS &nbsp; / &nbsp; TP. HỒ CHÍ MINH</div>
      </div>
      <div className="login-form-wrap">
        <form className="login-form" onSubmit={submit}>
          <div className="login-icon">
            <ShieldCheck size={23} />
          </div>
          <span className="eyebrow">TRUNG TÂM ĐIỀU HÀNH</span>
          <h2>Chào mừng trở lại</h2>
          <p>Đăng nhập để tiếp tục quản lý hệ thống.</p>
          {error && <div className="error">{error}</div>}
          <label>
            Tài khoản
            <input
              value={account}
              onChange={(e) => setAccount(e.target.value)}
              placeholder="Email hoặc số điện thoại"
              required
              autoComplete="username"
            />
          </label>
          <label>
            Mật khẩu
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Nhập mật khẩu"
              required
              autoComplete="current-password"
            />
          </label>
          <button className="btn primary login-button" disabled={busy}>
            {busy ? 'Đang đăng nhập…' : 'Đăng nhập'} <ArrowRight size={17} />
          </button>
          <p className="login-note">Sử dụng tài khoản quản trị viên đã được cấp.</p>
        </form>
        <div className="form-foot">© 2026 SmartBus · Hệ thống quản lý vé xe buýt</div>
      </div>
    </main>
  );
}

function Dashboard({ go }: { go: (p: Page) => void }) {
  const [data, setData] = useState<Row | null>(null);
  const [routes, setRoutes] = useState<Row[]>([]);
  const [tuyens, setTuyens] = useState<Row[]>([]);
  const [alerts, setAlerts] = useState<Row[]>([]);
  const [range, setRange] = useState<'today' | '7days' | '30days'>('30days');
  const [tuyenId, setTuyenId] = useState<string>('');
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    try {
      setError('');
      let tuNgay: string;
      const now = new Date();
      if (range === 'today') {
        const d = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        tuNgay = d.toISOString();
      } else if (range === '7days') {
        const d = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        tuNgay = d.toISOString();
      } else {
        const d = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        tuNgay = d.toISOString();
      }
      const query = `?tuNgay=${encodeURIComponent(tuNgay)}${tuyenId ? `&tuyenXeId=${tuyenId}` : ''}`;

      const [a, b, c, tList] = await Promise.all([
        api<Row>(`/api/Admin/bao-cao-tong-hop${query}`),
        api<Row[]>('/api/BaoCao/luot-di-theo-tuyen'),
        api<Row[]>('/api/Admin/canh-bao'),
        api<Row[]>('/api/VanHanh/danh-sach-tuyen'),
      ]);
      setData(a);
      setRoutes(b);
      setAlerts(c);
      setTuyens(tList);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không tải được dữ liệu.');
    }
  }, [range, tuyenId]);

  useEffect(() => {
    void load();
  }, [load]);

  const metrics = [
    {
      label: 'Lượt đi hợp lệ',
      value: data?.tongLuotDi,
      icon: ArrowDownLeft,
      note: range === 'today' ? 'Trong ngày hôm nay' : range === '7days' ? 'Trong 7 ngày qua' : 'Trong 30 ngày qua',
      tone: 'mint',
    },
    {
      label: 'Vé đã phát hành',
      value: data?.tongVePhatHanh,
      icon: Ticket,
      note: range === 'today' ? 'Trong ngày hôm nay' : range === '7days' ? 'Trong 7 ngày qua' : 'Trong 30 ngày qua',
      tone: 'blue',
    },
    {
      label: 'Doanh thu mô phỏng',
      value: data ? `${Number(data.doanhThuMock).toLocaleString('vi-VN')} ₫` : undefined,
      icon: CircleDollarSign,
      note: 'Từ đơn đã thanh toán',
      tone: 'amber',
    },
    {
      label: 'Cảnh báo mới',
      value: data?.canhBaoMoi,
      icon: Bell,
      note: 'Cần kiểm tra đối soát',
      tone: 'rose',
    },
  ];

  const max = Math.max(1, ...routes.map((x) => Number(x.tongSoLuotDi) || 0));

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="eyebrow">TỔNG QUAN HỆ THỐNG</span>
          <h1>
            Bảng điều khiển <span className="title-dot">.</span>
          </h1>
          <p>Theo dõi hoạt động, phân bổ lượt đi và xuất dữ liệu đối soát.</p>
        </div>
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          {/* Bộ lọc khoảng ngày */}
          <div style={{ display: 'flex', background: '#FFFFFF', borderRadius: 8, border: '1px solid #E2E8F0', padding: 2 }}>
            <button
              style={{
                border: 0,
                background: range === 'today' ? '#0F766E' : 'none',
                color: range === 'today' ? '#FFF' : '#475569',
                padding: '6px 12px',
                borderRadius: 6,
                fontWeight: 'bold',
                fontSize: 11,
              }}
              onClick={() => setRange('today')}
            >
              Hôm nay
            </button>
            <button
              style={{
                border: 0,
                background: range === '7days' ? '#0F766E' : 'none',
                color: range === '7days' ? '#FFF' : '#475569',
                padding: '6px 12px',
                borderRadius: 6,
                fontWeight: 'bold',
                fontSize: 11,
              }}
              onClick={() => setRange('7days')}
            >
              7 ngày
            </button>
            <button
              style={{
                border: 0,
                background: range === '30days' ? '#0F766E' : 'none',
                color: range === '30days' ? '#FFF' : '#475569',
                padding: '6px 12px',
                borderRadius: 6,
                fontWeight: 'bold',
                fontSize: 11,
              }}
              onClick={() => setRange('30days')}
            >
              30 ngày
            </button>
          </div>

          {/* Bộ lọc tuyến */}
          <select
            style={{ height: 35, borderRadius: 8, borderColor: '#CBD5E1', fontSize: 12, padding: '0 8px' }}
            value={tuyenId}
            onChange={(e) => setTuyenId(e.target.value)}
          >
            <option value="">Toàn mạng lưới</option>
            {tuyens.map((t) => (
              <option key={t.id} value={t.id}>
                {t.maTuyen} - {t.tenTuyen}
              </option>
            ))}
          </select>

          {/* Nút Xuất CSV */}
          <button className="btn outline" onClick={() => void downloadCsv('don-hang')} title="Xuất tệp CSV">
            <FileSpreadsheet size={15} /> Xuất CSV
          </button>
          <button className="btn outline" onClick={load}>
            <RefreshCw size={15} /> Làm mới
          </button>
        </div>
      </div>

      {error && <div className="error">{error}</div>}

      <div className="metrics">
        {metrics.map((m) => (
          <article className="metric" key={m.label}>
            <div className="metric-head">
              <span>{m.label}</span>
              <div className={`metric-icon ${m.tone}`}>
                <m.icon size={19} />
              </div>
            </div>
            <strong>{m.value ?? '—'}</strong>
            <small>
              <span className="tiny-dot" /> {m.note}
            </small>
          </article>
        ))}
      </div>

      <div className="main-grid">
        <section className="panel route-panel">
          <div className="panel-head">
            <div>
              <span className="eyebrow">HOẠT ĐỘNG</span>
              <h2>Lượt đi theo tuyến</h2>
              <p>Phân bổ lượt đi trong 30 ngày gần nhất</p>
            </div>
            <button className="text-button" onClick={() => go('routes')}>
              Xem tuyến <ArrowRight size={14} />
            </button>
          </div>
          {routes.length ? (
            <div className="bars">
              {routes.slice(0, 6).map((r, i) => (
                <div className="bar-row" key={i}>
                  <div>
                    <strong>{r.maTuyen}</strong>
                    <span>{r.tenTuyen}</span>
                  </div>
                  <div className="track">
                    <div style={{ width: `${Math.max(3, (r.tongSoLuotDi / max) * 100)}%` }} />
                  </div>
                  <b>{r.tongSoLuotDi}</b>
                </div>
              ))}
            </div>
          ) : (
            <div className="empty">
              <Activity size={22} />
              <strong>Chưa có lượt đi trong kỳ</strong>
              <span>Dữ liệu sẽ xuất hiện sau khi vé được soát.</span>
            </div>
          )}
        </section>

        <section className="panel activity-panel">
          <div className="panel-head">
            <div>
              <span className="eyebrow">CẦN CHÚ Ý</span>
              <h2>Công việc của bạn</h2>
              <p>Những việc cần theo dõi hôm nay</p>
            </div>
          </div>
          <button className="task" onClick={() => go('profiles')}>
            <div className="task-icon lavender">
              <FileCheck2 size={19} />
            </div>
            <div>
              <strong>Hồ sơ chờ duyệt</strong>
              <span>Xem hồ sơ ưu tiên</span>
            </div>
            <b>{data?.hoSoChoDuyet ?? '—'}</b>
            <ArrowRight size={15} />
          </button>
          <button className="task" onClick={() => go('alerts')}>
            <div className="task-icon peach">
              <Bell size={19} />
            </div>
            <div>
              <strong>Cảnh báo mới</strong>
              <span>Kiểm tra đối soát</span>
            </div>
            <b>{data?.canhBaoMoi ?? '—'}</b>
            <ArrowRight size={15} />
          </button>
          <button className="task" onClick={() => go('trips')}>
            <div className="task-icon mint">
              <BusFront size={19} />
            </div>
            <div>
              <strong>Chuyến đang chạy</strong>
              <span>Theo dõi vận hành</span>
            </div>
            <b>{data?.chuyenDangChay ?? '—'}</b>
            <ArrowRight size={15} />
          </button>
        </section>
      </div>

      <section className="panel recent">
        <div className="panel-head">
          <div>
            <span className="eyebrow">ĐỐI SOÁT</span>
            <h2>Cảnh báo gần đây</h2>
          </div>
          <button className="text-button" onClick={() => go('alerts')}>
            Xem tất cả <ArrowRight size={14} />
          </button>
        </div>
        {alerts.length ? (
          <div className="recent-list">
            {alerts.slice(0, 4).map((a) => (
              <div className="recent-row" key={a.id}>
                <div className="recent-icon">
                  <Bell size={16} />
                </div>
                <div>
                  <strong>{a.maCanhBao}</strong>
                  <span>{a.noiDung}</span>
                </div>
                <span className="badge">{format(a.trangThai)}</span>
                <small>{format(a.ngayTao, 'ngayTao')}</small>
              </div>
            ))}
          </div>
        ) : (
          <div className="empty compact">
            <BadgeCheck size={23} />
            <strong>Chưa có cảnh báo</strong>
            <span>Các sự kiện cần chú ý sẽ hiển thị tại đây.</span>
          </div>
        )}
      </section>
    </>
  );
}

function DataPage({ page }: { page: Exclude<Page, 'overview'> }) {
  const section = sections[page];
  const [rows, setRows] = useState<Row[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<Row | null>(null);
  const [busy, setBusy] = useState(false);
  const [newTrip, setNewTrip] = useState(false);
  const [newPattern, setNewPattern] = useState(false);
  const [viewPatternId, setViewPatternId] = useState<number | null>(null);
  const [qrVehicle, setQrVehicle] = useState<Row | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setRows(await api<Row[]>(section.endpoint));
      setError('');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không tải được dữ liệu.');
    } finally {
      setLoading(false);
    }
  }, [section.endpoint]);

  useEffect(() => {
    setSearch('');
    setEditing(null);
    void load();
  }, [load]);

  const filtered = rows.filter((r) =>
    Object.values(r).some((v) => String(v ?? '').toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi')))
  );

  async function action(path: string, payload: unknown) {
    try {
      setBusy(true);
      await post(path, payload);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể lưu dữ liệu.');
    } finally {
      setBusy(false);
    }
  }

  async function save(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!editing || !section.fields || !section.save) return;
    const raw = new FormData(e.currentTarget);
    const payload: Row = { id: editing.id || 0 };
    for (const f of section.fields) {
      const value = raw.get(f.key)?.toString() ?? '';
      payload[f.key] =
        f.type === 'boolean'
          ? value === 'true'
          : f.type === 'number'
          ? Number(value)
          : value;
    }
    if (page === 'users' && editing.id) delete payload.matKhau;
    try {
      setBusy(true);
      await post(section.save, payload);
      setEditing(null);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể lưu dữ liệu.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="eyebrow">SMARTBUS / QUẢN LÝ</span>
          <h1>
            {section.title} <span className="title-dot">.</span>
          </h1>
          <p>{section.desc}</p>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          {page === 'trips' && (
            <button className="btn primary" onClick={() => setNewTrip(true)}>
              <Plus size={17} /> Tạo chuyến
            </button>
          )}
          {page === 'patterns' && (
            <button className="btn primary" onClick={() => setNewPattern(true)}>
              <Plus size={17} /> Tạo lộ trình
            </button>
          )}
          {section.save && !section.editOnly && (
            <button
              className="btn primary"
              onClick={() =>
                setEditing({
                  trangThai: true,
                  gioBatDau: '05:00',
                  gioKetThuc: '21:00',
                  gianCachPhut: 10,
                  sucChua: 40,
                  vaiTro: 'NhanVienSoatVe',
                })
              }
            >
              <Plus size={17} /> Thêm mới
            </button>
          )}
          {['tickets', 'orders', 'scans', 'alerts'].includes(page) && (
            <button
              className="btn outline"
              onClick={() => {
                const loai =
                  page === 'tickets'
                    ? 've'
                    : page === 'orders'
                    ? 'don-hang'
                    : page === 'scans'
                    ? 'luot-di'
                    : 'canh-bao';
                void downloadCsv(loai);
              }}
            >
              <Download size={15} /> Xuất CSV
            </button>
          )}
        </div>
      </div>

      {error && <div className="error">{error}</div>}

      <section className="panel table-panel">
        <div className="table-toolbar">
          <div>
            <h2>Danh sách {section.title.toLocaleLowerCase('vi')}</h2>
            <p>
              {filtered.length} bản ghi{' '}
              {['tickets', 'orders', 'scans', 'alerts', 'trips'].includes(page) && '· tối đa 200 bản ghi gần nhất'}
            </p>
          </div>
          <div className="table-actions">
            <div className="search">
              <Search size={16} />
              <input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Tìm trong danh sách..."
              />
            </div>
            <button className="icon-button" onClick={load} title="Làm mới">
              <RefreshCw size={16} />
            </button>
          </div>
        </div>

        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                {section.columns.map((c) => (
                  <th key={c[0]}>{c[1]}</th>
                ))}
                {(section.save || ['profiles', 'users', 'tickets', 'alerts', 'trips', 'patterns'].includes(page)) && (
                  <th>Thao tác</th>
                )}
              </tr>
            </thead>
            <tbody>
              {filtered.map((row, i) => (
                <tr key={row.id ?? row.maVe ?? i}>
                  {section.columns.map(([key]) => (
                    <td key={key}>
                      {key === 'trangThai' || key === 'ketQuaTaiThietBi' ? (
                        <span
                          className={`badge ${
                            row[key] === false || ['TamKhoa', 'TuChoi', 'Huy', 'Moi'].includes(row[key])
                              ? 'warning'
                              : ''
                          }`}
                        >
                          {format(row[key], key)}
                        </span>
                      ) : key === 'duongDanMinhChung' && row[key] ? (
                        <button
                          type="button"
                          style={{
                            color: '#0066CC',
                            textDecoration: 'underline',
                            background: 'none',
                            border: 'none',
                            cursor: 'pointer',
                            padding: 0,
                            font: 'inherit',
                          }}
                          onClick={() => void xemMinhChung(row[key])}
                        >
                          Xem minh chứng ↗
                        </button>
                      ) : (
                        <span title={String(row[key] ?? '')}>{format(row[key], key)}</span>
                      )}
                    </td>
                  ))}
                  {(section.save || ['profiles', 'users', 'tickets', 'alerts', 'trips', 'patterns'].includes(page)) && (
                    <td>
                      <div className="row-actions">
                        {page === 'patterns' && (
                          <button onClick={() => setViewPatternId(row.id)}>Xem trạm</button>
                        )}
                        {section.save && page !== 'users' && (
                          <button onClick={() => setEditing(row)}>Sửa</button>
                        )}
                        {page === 'vehicles' && (
                          <button onClick={() => setQrVehicle(row)}>Xem QR</button>
                        )}
                        {page === 'users' && (
                          <button
                            disabled={busy || row.vaiTro === 'QuanTriVien'}
                            onClick={() => {
                              if (window.confirm(`${row.trangThai ? 'Khóa' : 'Mở khóa'} tài khoản ${row.hoTen}?`))
                                void action('/api/NguoiDung/khoa-tai-khoan', { id: row.id, trangThai: !row.trangThai });
                            }}
                          >
                            {row.trangThai ? 'Khóa' : 'Mở khóa'}
                          </button>
                        )}
                        {page === 'profiles' && (
                          <>
                            <button onClick={() => void action('/api/XacThuc/duyet-ho-so-uu-tien', { hoSoId: row.id, chapNhan: true })}>
                              Duyệt
                            </button>
                            <button
                              onClick={() => {
                                const reason = window.prompt('Lý do từ chối:');
                                if (reason !== null)
                                  void action('/api/XacThuc/duyet-ho-so-uu-tien', { hoSoId: row.id, chapNhan: false, lyDo: reason });
                              }}
                            >
                              Từ chối
                            </button>
                          </>
                        )}
                        {page === 'tickets' && (
                          <>
                            {row.trangThai === 'HoatDong' && (
                              <button
                                style={{ color: '#DC2626' }}
                                onClick={() => {
                                  const reason = window.prompt('Lý do tạm khóa vé:');
                                  if (reason)
                                    void action('/api/Ve/khoa-ve', { maVe: row.maVe, lyDo: reason, thuHoiVinhVien: false });
                                }}
                              >
                                Tạm khóa
                              </button>
                            )}
                            {row.trangThai === 'TamKhoa' && (
                              <button
                                style={{ color: '#10B981', fontWeight: 'bold' }}
                                onClick={() => {
                                  const reason = window.prompt('Lý do mở khóa vé:');
                                  if (reason)
                                    void action(`/api/Admin/ve/${row.maVe}/mo-khoa`, { lyDo: reason });
                                }}
                              >
                                Mở khóa
                              </button>
                            )}
                          </>
                        )}
                        {page === 'alerts' && row.trangThai !== 'DaXuLy' && (
                          <button
                            onClick={() => {
                              const note = window.prompt('Kết luận xử lý:');
                              if (note !== null)
                                void action(`/api/Admin/canh-bao/${row.id}`, { trangThai: 'DaXuLy', ketLuan: note });
                            }}
                          >
                            Xử lý
                          </button>
                        )}
                        {page === 'trips' && row.trangThai === 'DuKien' && (
                          <>
                            <button onClick={() => void action(`/api/Admin/chuyen-xe/${row.id}/trang-thai`, { trangThai: 'DangChay' })}>
                              Bắt đầu
                            </button>
                            <button
                              style={{ color: '#DC2626' }}
                              onClick={() => {
                                if (window.confirm('Bạn có chắc muốn hủy chuyến xe dự kiến này?'))
                                  void action(`/api/Admin/chuyen-xe/${row.id}/trang-thai`, { trangThai: 'Huy' });
                              }}
                            >
                              Hủy
                            </button>
                          </>
                        )}
                        {page === 'trips' && row.trangThai === 'DangChay' && (
                          <button onClick={() => void action(`/api/Admin/chuyen-xe/${row.id}/trang-thai`, { trangThai: 'HoanThanh' })}>
                            Kết thúc
                          </button>
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
          {!loading && !filtered.length && (
            <div className="empty">
              <section.icon size={23} />
              <strong>Chưa có dữ liệu</strong>
              <span>Không tìm thấy bản ghi phù hợp.</span>
            </div>
          )}
          {loading && (
            <div className="empty">
              <span>Đang tải dữ liệu…</span>
            </div>
          )}
        </div>
      </section>

      {/* Modal QR Phương tiện */}
      {qrVehicle && (
        <div className="modal-backdrop" onClick={() => setQrVehicle(null)}>
          <div className="modal qr-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-heading">
              <div>
                <span className="eyebrow">SMARTBUS XE BUÝT</span>
                <h2>Mã QR {qrVehicle.bienSo}</h2>
              </div>
              <button className="icon-button" onClick={() => setQrVehicle(null)}>
                <X size={18} />
              </button>
            </div>
            <div className="qr-body">
              <QRCodeSVG id="vehicle-qr-svg" value={qrVehicle.maQrTinh} size={200} />
              <p style={{ marginTop: 8, fontWeight: 'bold' }}>GUID: {qrVehicle.maQrTinh}</p>
              <div style={{ display: 'flex', gap: 10, marginTop: 12 }}>
                <button
                  type="button"
                  className="btn primary"
                  onClick={() => downloadQrPng(qrVehicle.bienSo)}
                >
                  <Download size={15} /> Tải ảnh PNG
                </button>
                <button
                  type="button"
                  className="btn outline"
                  onClick={() => window.print()}
                >
                  <Printer size={15} /> In mã QR
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {newTrip && <NewTrip close={() => setNewTrip(false)} done={() => { setNewTrip(false); void load(); }} />}
      {newPattern && <NewPattern close={() => setNewPattern(false)} done={() => { setNewPattern(false); void load(); }} />}
      {viewPatternId && <ViewPatternModal patternId={viewPatternId} close={() => setViewPatternId(null)} />}

      {editing && section.fields && (
        <div className="modal-backdrop" onClick={() => setEditing(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-heading">
              <div>
                <span className="eyebrow">SMARTBUS</span>
                <h2>{editing.id ? 'Cập nhật' : 'Thêm mới'} {section.title.toLocaleLowerCase('vi')}</h2>
              </div>
              <button className="icon-button" onClick={() => setEditing(null)}>
                <X size={18} />
              </button>
            </div>
            <form onSubmit={save}>
              <div className="form-grid">
                {section.fields
                  .filter((f) => !(page === 'users' && editing.id && f.key === 'matKhau'))
                  .map((f) => (
                    <label key={f.key}>
                      {f.label}
                      {f.required && ' *'}
                      {f.type === 'boolean' ? (
                        <select name={f.key} defaultValue={String(editing[f.key] ?? true)}>
                          <option value="true">Hoạt động</option>
                          <option value="false">Tạm khóa</option>
                        </select>
                      ) : f.type === 'select' ? (
                        <select name={f.key} defaultValue={editing[f.key] ?? f.options?.[0][0]}>
                          {f.options?.map(([value, label]) => (
                            <option value={value} key={value}>
                              {label}
                            </option>
                          ))}
                        </select>
                      ) : (
                        <input
                          name={f.key}
                          type={f.type || 'text'}
                          step={f.type === 'number' ? 'any' : undefined}
                          required={f.required}
                          defaultValue={editing[f.key] ?? ''}
                        />
                      )}
                    </label>
                  ))}
              </div>
              <div className="modal-actions">
                <button type="button" className="btn outline" onClick={() => setEditing(null)}>
                  Hủy
                </button>
                <button className="btn primary" disabled={busy}>
                  <Check size={16} /> Lưu thay đổi
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
}

function App() {
  const [name, setName] = useState(token ? sessionStorage.getItem('smartbus_admin_name') || 'Quản trị viên' : '');
  const [page, setPage] = useState<Page>('overview');
  const [menu, setMenu] = useState(false);

  useEffect(() => {
    const end = () => setName('');
    window.addEventListener('session-expired', end);
    return () => window.removeEventListener('session-expired', end);
  }, []);

  function logout() {
    token = '';
    sessionStorage.removeItem('smartbus_admin_token');
    sessionStorage.removeItem('smartbus_admin_name');
    setName('');
  }

  if (!name) return <Login onLogin={setName} />;

  return (
    <div className="app-shell">
      <aside className={`sidebar ${menu ? 'open' : ''}`}>
        <div className="brand">
          <div className="brand-mark">
            <BusFront size={20} />
          </div>
          <div>
            <strong>smartbus<span>.</span></strong>
            <small>QUẢN TRỊ ĐIỀU HÀNH</small>
          </div>
        </div>
        <div className="workspace">
          <div className="workspace-icon">SB</div>
          <div>
            <strong>SmartBus</strong>
            <span>Trung tâm điều hành</span>
          </div>
          <ChevronDown size={15} />
        </div>
        <nav>
          {nav.map((group) => (
            <div className="nav-group" key={group.label}>
              <div className="nav-label">{group.label}</div>
              {group.items.map((item) => (
                <button
                  key={item.id}
                  className={`nav-item ${page === item.id ? 'active' : ''}`}
                  onClick={() => {
                    setPage(item.id);
                    setMenu(false);
                  }}
                >
                  <item.icon size={17} />
                  <span>{item.label}</span>
                  {page === item.id && <span className="active-dot" />}
                </button>
              ))}
            </div>
          ))}
        </nav>
        <div className="sidebar-bottom">
          <div className="system-status">
            <span className="online-dot" /> Hệ thống điều hành <small>SMARTBUS</small>
          </div>
          <button className="logout" onClick={logout}>
            <LogOut size={16} /> Đăng xuất
          </button>
        </div>
      </aside>

      {menu && <div className="mobile-overlay" onClick={() => setMenu(false)} />}

      <div className="main">
        <header className="topbar">
          <button className="mobile-menu icon-button" onClick={() => setMenu(true)}>
            <Menu size={20} />
          </button>
          <div className="breadcrumb">
            SmartBus <span>/</span>{' '}
            <strong>{page === 'overview' ? 'Bảng điều khiển' : sections[page].title}</strong>
          </div>
          <div className="top-actions">
            <span className="date-label">
              {new Date().toLocaleDateString('vi-VN', {
                weekday: 'long',
                day: 'numeric',
                month: 'long',
                year: 'numeric',
              })}
            </span>
            <button className="top-bell" onClick={() => setPage('alerts')} aria-label="Cảnh báo">
              <Bell size={19} />
            </button>
            <div className="avatar">{name.charAt(0).toUpperCase()}</div>
            <div className="user-name">
              <strong>{name}</strong>
              <span>Quản trị viên</span>
            </div>
          </div>
        </header>

        <main className="content">
          {page === 'overview' ? <Dashboard go={setPage} /> : <DataPage key={page} page={page} />}
          <footer className="content-footer">
            <span>© 2026 SmartBus · Hệ thống quản lý vé xe buýt</span>
            <span>Trung tâm điều hành <span className="footer-dot">●</span></span>
          </footer>
        </main>
      </div>
    </div>
  );
}

createRoot(document.getElementById('app')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
