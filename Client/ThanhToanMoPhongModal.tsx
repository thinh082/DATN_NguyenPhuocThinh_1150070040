import React, { useEffect, useState } from 'react';
import {
  Modal,
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from 'react-native';
import * as Crypto from 'expo-crypto';
import Colors from './constants/colors';
import { thanhToanGiaLap } from './services/veService';

export interface DonHangThanhToan {
  donHangId: number;
  maDonHang: string;
  phaiThanhToan: number;
  tenLoaiVe?: string;
  tenTuyen?: string;
}

interface Props {
  visible: boolean;
  donHang: DonHangThanhToan | null;
  onClose: () => void;
  onSuccess: () => void;
}

export default function ThanhToanMoPhongModal({ visible, donHang, onClose, onSuccess }: Props) {
  const [requestId, setRequestId] = useState<string>('');
  const [dangXuLy, setDangXuLy] = useState(false);
  const [thongBao, setThongBao] = useState('');

  useEffect(() => {
    if (visible && donHang) {
      setRequestId(Crypto.randomUUID());
      setThongBao('');
    }
  }, [visible, donHang]);

  if (!donHang) return null;

  const xuLyKetQua = async (ketQua: 'ThanhCong' | 'ThatBai' | 'Huy') => {
    try {
      setDangXuLy(true);
      setThongBao('');
      const reqId = requestId || Crypto.randomUUID();
      const res = await thanhToanGiaLap({
        donHangId: donHang.donHangId,
        requestId: reqId,
        ketQua,
      });

      if (res.statusCode === 200) {
        if (ketQua === 'ThanhCong') {
          Alert.alert('Thành công', 'Thanh toán mô phỏng thành công! Hệ thống đã ký số và phát hành vé vào ví.');
          onSuccess();
        } else if (ketQua === 'ThatBai') {
          Alert.alert('Thất bại', 'Giao dịch mô phỏng thất bại. Bạn có thể chọn Thử lại hoặc Hủy đơn.');
          setThongBao('Giao dịch thất bại. Vui lòng thử lại hoặc hủy đơn.');
        } else {
          Alert.alert('Đã hủy', 'Đơn hàng đã được hủy.');
          onClose();
        }
      } else {
        setThongBao(res.message || 'Xử lý thất bại');
      }
    } catch (e: any) {
      setThongBao(e.message || 'Lỗi khi gửi yêu cầu thanh toán');
    } finally {
      setDangXuLy(false);
    }
  };

  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <View style={styles.overlay}>
        <View style={styles.modalCard}>
          <View style={styles.header}>
            <Text style={styles.logo}>🚌</Text>
            <Text style={styles.title}>THANH TOÁN MÔ PHỎNG</Text>
          </View>

          <View style={styles.infoBox}>
            <View style={styles.row}>
              <Text style={styles.label}>Mã đơn hàng:</Text>
              <Text style={styles.value} numberOfLines={1}>{donHang.maDonHang}</Text>
            </View>
            {donHang.tenLoaiVe && (
              <View style={styles.row}>
                <Text style={styles.label}>Loại vé:</Text>
                <Text style={styles.value}>{donHang.tenLoaiVe}</Text>
              </View>
            )}
            {donHang.tenTuyen && (
              <View style={styles.row}>
                <Text style={styles.label}>Tuyến áp dụng:</Text>
                <Text style={styles.value}>{donHang.tenTuyen}</Text>
              </View>
            )}
            <View style={[styles.row, { marginTop: 8, borderTopWidth: 1, borderColor: '#E2E8F0', paddingTop: 8 }]}>
              <Text style={[styles.label, { fontSize: 16, fontWeight: 'bold' }]}>Số tiền thanh toán:</Text>
              <Text style={styles.price}>{donHang.phaiThanhToan.toLocaleString('vi-VN')} đ</Text>
            </View>
          </View>

          <View style={styles.noticeBox}>
            <Text style={styles.noticeText}>
              ℹ️ Thanh toán chỉ là mô phỏng trong phạm vi đồ án. Không phát sinh giao dịch tiền tệ thực tế.
            </Text>
          </View>

          {thongBao ? (
            <View style={styles.errorBox}>
              <Text style={styles.errorText}>{thongBao}</Text>
            </View>
          ) : null}

          {dangXuLy && <ActivityIndicator color={Colors.primary} style={{ marginVertical: 8 }} />}

          <View style={styles.actions}>
            <TouchableOpacity
              style={[styles.btn, styles.btnSuccess]}
              disabled={dangXuLy}
              onPress={() => xuLyKetQua('ThanhCong')}
            >
              <Text style={styles.btnText}>✓ Thành công (Mô phỏng)</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.btn, styles.btnDanger]}
              disabled={dangXuLy}
              onPress={() => xuLyKetQua('ThatBai')}
            >
              <Text style={styles.btnText}>✕ Thất bại (Mô phỏng)</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.btn, styles.btnCancel]}
              disabled={dangXuLy}
              onPress={() => xuLyKetQua('Huy')}
            >
              <Text style={[styles.btnText, { color: '#475569' }]}>Hủy đơn hàng</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  modalCard: {
    width: '100%',
    backgroundColor: '#FFFFFF',
    borderRadius: 20,
    padding: 24,
    shadowColor: '#000',
    shadowOpacity: 0.15,
    shadowRadius: 10,
    elevation: 8,
  },
  header: {
    alignItems: 'center',
    marginBottom: 16,
  },
  logo: {
    fontSize: 32,
    marginBottom: 4,
  },
  title: {
    fontSize: 18,
    fontWeight: 'bold',
    color: Colors.primary,
  },
  infoBox: {
    backgroundColor: '#F8FAFC',
    borderRadius: 12,
    padding: 16,
    gap: 8,
    borderWidth: 1,
    borderColor: '#E2E8F0',
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  label: {
    fontSize: 14,
    color: '#64748B',
  },
  value: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1E293B',
    maxWidth: '65%',
  },
  price: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#059669',
  },
  noticeBox: {
    backgroundColor: '#EFF6FF',
    borderRadius: 8,
    padding: 10,
    marginTop: 12,
    borderWidth: 1,
    borderColor: '#BFDBFE',
  },
  noticeText: {
    fontSize: 12,
    color: '#1E40AF',
    lineHeight: 16,
  },
  errorBox: {
    backgroundColor: '#FEF2F2',
    borderRadius: 8,
    padding: 10,
    marginTop: 10,
  },
  errorText: {
    color: '#DC2626',
    fontSize: 13,
  },
  actions: {
    marginTop: 18,
    gap: 10,
  },
  btn: {
    paddingVertical: 12,
    borderRadius: 10,
    alignItems: 'center',
  },
  btnSuccess: {
    backgroundColor: '#10B981',
  },
  btnDanger: {
    backgroundColor: '#EF4444',
  },
  btnCancel: {
    backgroundColor: '#E2E8F0',
  },
  btnText: {
    color: '#FFFFFF',
    fontWeight: 'bold',
    fontSize: 14,
  },
});
