import apiClient, { PhanHoiApi, setAccessToken } from './api';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';
import { toByteArray } from 'base64-js';

export interface ThongTinNguoiDung {
  id: number;
  hoTen: string;
  email: string;
  soDienThoai: string;
  cccd: string;
  vaiTro: string;
}

export interface KetQuaDangNhap {
  token: string;
  nguoiDung: ThongTinNguoiDung;
}

export const dangNhap = async (taiKhoan: string, matKhau: string): Promise<PhanHoiApi<KetQuaDangNhap>> => {
  const response = await apiClient.post<PhanHoiApi<KetQuaDangNhap>>('/api/XacThuc/dang-nhap', {
    taiKhoan,
    matKhau,
  });
  return response.data;
};

export const dangKy = async (thongTin: {
  hoTen: string;
  soDienThoai?: string;
  email?: string;
  cccd?: string;
  matKhau: string;
}): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.post<PhanHoiApi<any>>('/api/XacThuc/dang-ky', thongTin);
  return response.data;
};

export const layThongTinCaNhan = async (nguoiDungId: number): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.get<PhanHoiApi<any>>(`/api/XacThuc/thong-tin-ca-nhan?nguoiDungId=${nguoiDungId}`);
  return response.data;
};

export const nopHoSoUuTien = async (hoSo: {
  nhomDoiTuongId: number;
  duongDanMinhChung: string;
  hieuLucTu: string;
  hieuLucDen: string;
}): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.post<PhanHoiApi<any>>('/api/XacThuc/nop-ho-so-uu-tien', hoSo);
  return response.data;
};

export type CapNhatCaNhan = { hoTen: string; email: string; soDienThoai: string; cccd: string };
export type NhomUuTien = { id: number; tenNhom: string };
export type HoSoUuTien = { id: number; tenNhom: string; trangThai: string; duongDanMinhChung?: string; hieuLucTu: string; hieuLucDen: string; lyDo?: string | null };

export interface KetQuaTaiLenMinhChung {
  maTep: string;
  tenFileGoc: string;
  duongDan: string;
  loaiFile: string;
  kichThuoc: number;
}

export async function taiLenMinhChung(file: { uri: string; name: string; type: string }): Promise<PhanHoiApi<KetQuaTaiLenMinhChung>> {
  const formData = new FormData();
  if (Platform.OS === 'web') {
    const res = await fetch(file.uri);
    const blob = await res.blob();
    formData.append('tep', blob, file.name);
  } else {
    formData.append('tep', {
      uri: file.uri,
      name: file.name,
      type: file.type || 'application/octet-stream',
    } as any);
  }

  const response = await apiClient.post<PhanHoiApi<KetQuaTaiLenMinhChung>>('/api/XacThuc/tai-len-minh-chung', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
}

export async function capNhatThongTin(thongTin: CapNhatCaNhan): Promise<PhanHoiApi<ThongTinNguoiDung>> {
  return (await apiClient.put('/api/XacThuc/thong-tin-ca-nhan', thongTin)).data;
}
export async function layNhomUuTien(): Promise<PhanHoiApi<NhomUuTien[]>> {
  return (await apiClient.get('/api/XacThuc/nhom-uu-tien')).data;
}
export async function layHoSoCuaToi(): Promise<PhanHoiApi<HoSoUuTien[]>> {
  return (await apiClient.get('/api/XacThuc/ho-so-cua-toi')).data;
}

// ==========================================
// QUẢN LÝ PHIÊN, SECURESTORE & NGOẠI TUYẾN
// ==========================================

const KEY_TOKEN = 'smartbus_access_token';
const KEY_USER = 'smartbus_user_info';
const KEY_LAST_ONLINE = 'smartbus_last_online_at';

export function parseJwtExp(token: string): number | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    let b64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    while (b64.length % 4) b64 += '=';
    const bytes = toByteArray(b64);
    let str = '';
    for (let i = 0; i < bytes.length; i++) {
      str += String.fromCharCode(bytes[i]);
    }
    const parsed = JSON.parse(decodeURIComponent(escape(str)));
    return typeof parsed.exp === 'number' ? parsed.exp : null;
  } catch {
    return null;
  }
}

export async function luuPhienDangNhap(token: string, nguoiDung: ThongTinNguoiDung): Promise<void> {
  await setAccessToken(token);
  if (Platform.OS !== 'web') {
    await SecureStore.setItemAsync(KEY_USER, JSON.stringify(nguoiDung));
    await SecureStore.setItemAsync(KEY_LAST_ONLINE, new Date().toISOString());
  }
}

export async function xoaPhienDangNhap(userId?: number): Promise<void> {
  await setAccessToken(null);
  if (Platform.OS !== 'web') {
    await SecureStore.deleteItemAsync(KEY_USER);
    await SecureStore.deleteItemAsync(KEY_LAST_ONLINE);
    if (userId) {
      try {
        const indexKey = `smartbus_wallet_${userId}`;
        const rawIds = await SecureStore.getItemAsync(indexKey);
        if (rawIds) {
          const ids: string[] = JSON.parse(rawIds);
          for (const id of ids) {
            await SecureStore.deleteItemAsync(`smartbus_ticket_${userId}_${id}`);
          }
          await SecureStore.deleteItemAsync(indexKey);
        }
      } catch {
        // Bo qua loi don dep vi
      }
    }
  }
}

export type KetQuaKhoiPhucPhien = {
  trangThai: 'Online' | 'Offline' | 'HetHan' | 'ChuaDangNhap';
  nguoiDung?: ThongTinNguoiDung;
  token?: string;
  thongBao?: string;
};

export async function khoiPhucPhienDangNhap(): Promise<KetQuaKhoiPhucPhien> {
  if (Platform.OS === 'web') {
    return { trangThai: 'ChuaDangNhap' };
  }

  const token = await SecureStore.getItemAsync(KEY_TOKEN);
  const userJson = await SecureStore.getItemAsync(KEY_USER);
  const lastOnlineAt = await SecureStore.getItemAsync(KEY_LAST_ONLINE);

  if (!token || !userJson) {
    return { trangThai: 'ChuaDangNhap' };
  }

  let nguoiDung: ThongTinNguoiDung;
  try {
    nguoiDung = JSON.parse(userJson);
  } catch {
    await xoaPhienDangNhap();
    return { trangThai: 'ChuaDangNhap' };
  }

  // Set memory token truoc de goi API
  await setAccessToken(token);

  // 1. Thu xac thuc online
  try {
    const res = await layThongTinCaNhan(nguoiDung.id);
    if (res.statusCode === 200) {
      await SecureStore.setItemAsync(KEY_LAST_ONLINE, new Date().toISOString());
      if (res.data) {
        nguoiDung = { ...nguoiDung, ...res.data };
        await SecureStore.setItemAsync(KEY_USER, JSON.stringify(nguoiDung));
      }
      return { trangThai: 'Online', nguoiDung, token };
    }
  } catch (err: any) {
    // 401/403: Server tu choi phien
    if (err.statusCode === 401 || err.statusCode === 403) {
      await xoaPhienDangNhap(nguoiDung.id);
      return {
        trangThai: 'HetHan',
        thongBao: 'Phiên đăng nhập đã hết hạn hoặc tài khoản đã bị khóa bởi hệ thống.',
      };
    }
    // Loi mang (mat mang / timeout): tiep tuc kiem tra dieu kien offline
  }

  // 2. Kiem tra thoi han offline (<= 24 gio tu lan online gan nhat)
  if (!lastOnlineAt) {
    await xoaPhienDangNhap(nguoiDung.id);
    return {
      trangThai: 'HetHan',
      thongBao: 'Không có mốc thời gian xác thực trực tuyến. Vui lòng kết nối mạng để đăng nhập.',
    };
  }

  const msSinceLastOnline = Date.now() - Date.parse(lastOnlineAt);
  const MAX_OFFLINE_MS = 24 * 60 * 60 * 1000;
  if (isNaN(msSinceLastOnline) || msSinceLastOnline > MAX_OFFLINE_MS) {
    await xoaPhienDangNhap(nguoiDung.id);
    return {
      trangThai: 'HetHan',
      thongBao: 'Đã quá 24 giờ kể từ lần trực tuyến gần nhất. Vui lòng kết nối mạng để tiếp tục.',
    };
  }

  // 3. Kiem tra JWT exp
  const exp = parseJwtExp(token);
  if (exp !== null && exp * 1000 <= Date.now()) {
    await xoaPhienDangNhap(nguoiDung.id);
    return {
      trangThai: 'HetHan',
      thongBao: 'Phiên xác thực đã hết hạn theo thời gian cấp vé. Vui lòng kết nối mạng để đăng nhập lại.',
    };
  }

  // Du dieu kien khoi phuc ngoai tuyen
  return {
    trangThai: 'Offline',
    nguoiDung,
    token,
    thongBao: 'Ngoại tuyến · trạng thái có thể chưa cập nhật',
  };
}
