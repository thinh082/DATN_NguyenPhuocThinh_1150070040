import apiClient, { PhanHoiApi } from './api';

export interface KetQuaXacThuc {
  hopLe: boolean;
  amThanh: string;
  thongBao: string;
  thongTinKhach?: any;
}

export const xacThucVe = async (thongTin: {
  phienSoatVeId: string;
  thietBiSoatVeId: number;
  soThuTuThietBi: number;
  maQrPayload: string;
  cheDo?: string;
}): Promise<PhanHoiApi<KetQuaXacThuc>> => {
  const response = await apiClient.post<PhanHoiApi<KetQuaXacThuc>>('/api/SoatVe/xac-thuc-ve', thongTin);
  return response.data;
};

export const traCuuKhancCap = async (thongTin: {
  phienSoatVeId: string;
  thietBiSoatVeId: number;
  soThuTuThietBi: number;
  cccd?: string;
  soDienThoai?: string;
}): Promise<PhanHoiApi<KetQuaXacThuc>> => {
  const response = await apiClient.post<PhanHoiApi<KetQuaXacThuc>>('/api/SoatVe/tra-cuu-khan-cap', thongTin);
  return response.data;
};

export const layGoiDuLieuNgoaiTuyen = async (thietBiSoatVeId: number): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.get<PhanHoiApi<any>>(`/api/SoatVe/goi-du-lieu-ngoai-tuyen?thietBiSoatVeId=${thietBiSoatVeId}`);
  return response.data;
};
