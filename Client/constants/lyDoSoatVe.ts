const moTaLyDo: Record<string, string> = {
  HOP_LE: 'Vé hợp lệ',
  DA_SU_DUNG: 'Vé đã được sử dụng cho lượt này (thẻ miễn phí: đã quét trên chuyến này)',
  QUET_TRUNG: 'Vé đã được quét ở thiết bị khác',
  DONG_BO_TRUNG_VE_LUOT: 'Vé lượt đã được ghi nhận trước đó',
  GOI_HET_HAN: 'Gói dữ liệu soát vé đã hết hạn, hãy tải lại gói',
  KHONG_CO_DU_LIEU: 'Vé không có trong gói dữ liệu của chuyến này',
  VE_KHONG_TON_TAI: 'Vé không tồn tại',
  QR_KHONG_HOP_LE: 'Mã QR không hợp lệ',
  MA_QR_KHONG_DUNG_DINH_DANG: 'Mã QR không đúng định dạng SmartBus',
  MA_VE_KHONG_HOP_LE: 'Mã vé không hợp lệ',
  SAI_DU_LIEU: 'Dữ liệu vé bị sai lệch',
  SAI_CHU_KY: 'Chữ ký số không hợp lệ (vé giả mạo)',
  CHU_KY_HONG: 'Chữ ký số bị hỏng',
  CHU_KY_KHONG_KHOP: 'Chữ ký số không khớp',
  SAI_TOTP: 'Mã QR đã hết hiệu lực, nhờ khách mở lại vé',
  MA_TOTP_HET_HAN: 'Mã QR đã hết hiệu lực, nhờ khách mở lại vé',
  SAI_TUYEN: 'Vé không áp dụng cho tuyến này',
  SAI_PHAM_VI: 'Thẻ miễn phí không áp dụng cho tuyến tính phí',
  VE_BI_KHOA: 'Vé đã bị khóa',
  VE_DA_BI_KHOA: 'Vé đã bị khóa',
  VE_HET_HAN: 'Vé đã hết hạn',
  THIET_BI_TU_CHOI: 'Thiết bị đã từ chối khi quét',
  STALE_REVOCATION: 'Vé bị khóa sau khi tải gói ngoại tuyến',
  LOI_LUU_TRU: 'Lỗi lưu dữ liệu trên thiết bị',
};

export function dienGiaiLyDo(maLyDo?: string | null) {
  if (!maLyDo) return 'Không rõ lý do';
  return moTaLyDo[maLyDo] ?? maLyDo;
}
