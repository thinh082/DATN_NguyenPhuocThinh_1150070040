import apiClient, { PhanHoiApi } from './api';

export interface NguoiDung {
  id: number;
  hoTen: string;
  soDienThoai: string;
  email: string;
  cccd: string;
  vaiTro: string;
  trangThai: boolean;
}

export const layDanhSachNguoiDung = async (): Promise<PhanHoiApi<NguoiDung[]>> => {
  const response = await apiClient.get<PhanHoiApi<NguoiDung[]>>('/api/NguoiDung/lay-danh-sach');
  return response.data;
};

export const layChiTietNguoiDung = async (id: number): Promise<PhanHoiApi<NguoiDung>> => {
  const response = await apiClient.get<PhanHoiApi<NguoiDung>>(`/api/NguoiDung/lay-chi-tiet?id=${id}`);
  return response.data;
};

export const luuNguoiDung = async (thongTin: Partial<NguoiDung>): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.post<PhanHoiApi<any>>('/api/NguoiDung/luu-nguoi-dung', thongTin);
  return response.data;
};

export const khoaTaiKhoan = async (id: number, trangThai: boolean): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.post<PhanHoiApi<any>>('/api/NguoiDung/khoa-tai-khoan', {
    id,
    trangThai,
  });
  return response.data;
};
