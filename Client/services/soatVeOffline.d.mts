export interface VeKiemTra { maVe: string; duLieuDaKy: string; khoaCongKhai: string; biMatTotp: string; trangThai: string }
export interface LuotDaQuet { maVe: string; buocThoiGian: number | null }
export interface GoiKiemTra { phienId: string; packageId: string; thietBiId: number; tuyenXeId: number; hetHanLuc: string; ves: VeKiemTra[]; daQuet: LuotDaQuet[] }
export interface KetQuaOffline { hopLe: boolean; maLyDo: string; maVe?: string; buocThoiGian?: number; maLoaiVe?: string }
export function xacThucNgoaiTuyen(qr: string, goi: GoiKiemTra, daQuet: LuotDaQuet[], now?: number): KetQuaOffline;
