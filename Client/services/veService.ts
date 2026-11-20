import apiClient, { PhanHoiApi } from './api';

export interface BieuGia {
  id: number;
  maLoaiVe: string;
  tenLoaiVe: string;
  nhomDoiTuongId: number;
  tenNhomDoiTuong: string;
  tuyenXeId: number | null;
  tenTuyen: string;
  giaGoc: number;
  soTienMienGiam: number;
  giaThucThu: number;
}

export interface VeDienTu {
  id: number;
  maVe: string;
  tenLoaiVe: string;
  tenNhomDoiTuong: string;
  tenTuyen: string;
  maTuyen: string;
  hieuLucTu: string;
  hetHanLuc: string;
  trangThai: string;
  maTotpHienTai: string;
  soGiayConLai: number;
  maQrPayload: string;
  biMatTotp: string;
  chuKyBase64: string;
}

export interface LuotDi {
  id: number; maVe: string; maLoaiVe: string; trangThai: string; lyDoHuy: string | null;
  thoiDiemQuet: string; cheDo: string; phuongThuc: string; maTuyen: string; tenTuyen: string; bienSo: string;
}

export async function layLichSuLuotDi(): Promise<PhanHoiApi<LuotDi[]>> {
  return (await apiClient.get<PhanHoiApi<LuotDi[]>>('/api/Ve/lich-su-luot-di')).data;
}

export const layDanhSachBieuGia = async (tuyenXeId?: number): Promise<PhanHoiApi<BieuGia[]>> => {
  const url = tuyenXeId ? `/api/Ve/danh-sach-bieu-gia?tuyenXeId=${tuyenXeId}` : '/api/Ve/danh-sach-bieu-gia';
  const response = await apiClient.get<PhanHoiApi<BieuGia[]>>(url);
  return response.data;
};

export const taoDonHang = async (thongTin: {
  nguoiDungId: number;
  giaVeId: number;
  soLuong?: number;
  maYeuCau?: string;
}): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.post<PhanHoiApi<any>>('/api/Ve/tao-don-hang', thongTin);
  return response.data;
};

export interface ThanhToanGiaLapRequest {
  donHangId: number;
  requestId: string;
  ketQua: 'ThanhCong' | 'ThatBai' | 'Huy';
}

export const thanhToanGiaLap = async (thongTin: ThanhToanGiaLapRequest): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.post<PhanHoiApi<any>>('/api/Ve/thanh-toan-gia-lap', thongTin);
  return response.data;
};

export const thanhToanDonHang = async (thongTin: {
  donHangId: number;
  congThanhToan?: string;
}): Promise<PhanHoiApi<any>> => {
  const response = await apiClient.post<PhanHoiApi<any>>('/api/Ve/thanh-toan-don-hang', thongTin);
  return response.data;
};

export const layDanhSachVeCuaToi = async (nguoiDungId: number): Promise<PhanHoiApi<VeDienTu[]>> => {
  const response = await apiClient.get<PhanHoiApi<VeDienTu[]>>(`/api/Ve/vi-ve-cua-toi?nguoiDungId=${nguoiDungId}`);
  return response.data;
};

export const layChiTietQrDong = async (maVe: string): Promise<PhanHoiApi<VeDienTu>> => {
  const response = await apiClient.get<PhanHoiApi<VeDienTu>>(`/api/Ve/chi-tiet-qr-dong?maVe=${maVe}`);
  return response.data;
};
