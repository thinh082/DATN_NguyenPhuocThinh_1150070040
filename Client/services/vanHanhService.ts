import apiClient, { PhanHoiApi } from './api';

export interface TuyenXe {
  id: number;
  maTuyen: string;
  tenTuyen: string;
  gioBatDau: string;
  gioKetThuc: string;
  gianCachPhut: number | null;
  trangThai: boolean;
  soLuongLoTrinh: number;
}

export const layDanhSachTuyenXe = async (): Promise<PhanHoiApi<TuyenXe[]>> => {
  const response = await apiClient.get<PhanHoiApi<TuyenXe[]>>('/api/VanHanh/danh-sach-tuyen');
  return response.data;
};

export interface TramLoTrinh { thuTu: number; tramDungId: number; maTram: string; tenTram: string; diaChi: string | null; phutTuDau: number | null }
export interface ChiTietTuyen extends TuyenXe { loTrinhs: { id: number; chieu: string; tenLoTrinh: string; danhSachTram: TramLoTrinh[] }[] }

export const layChiTietTuyenXe = async (id: number): Promise<PhanHoiApi<ChiTietTuyen>> => {
  const response = await apiClient.get<PhanHoiApi<ChiTietTuyen>>(`/api/VanHanh/chi-tiet-tuyen?id=${id}`);
  return response.data;
};

export const quetQrDanXe = async (maQrTinh: string): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.get<PhanHoiApi<any>>(`/api/VanHanh/quet-qr-dan-xe?maQrTinh=${maQrTinh}`);
  return response.data;
};
