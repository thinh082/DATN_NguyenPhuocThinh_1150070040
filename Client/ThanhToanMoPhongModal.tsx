import React, { useEffect, useState } from 'react';
import {
  Modal,
  ScrollView,
  View,
    Text,
    TouchableOpacity,
    StyleSheet,
    ActivityIndicator,
    Alert,
} from 'react-native';
import * as Crypto from 'expo-crypto';
import { ErrorBanner, Icon } from './components/MobileUi';
import { Colors } from './constants/colors';
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
            <ScrollView keyboardShouldPersistTaps="handled" contentContainerStyle={styles.sheetContent}>
            <View style={styles.header}>
              <View style={styles.logo}><Icon name="wallet-outline" size={23} /></View>
              <Text accessibilityRole="header" style={styles.title}>Thanh toán mô phỏng</Text>
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
              <View style={[styles.row, styles.total]}>
                <Text style={styles.totalLabel}>Tổng thanh toán</Text>
                <Text style={styles.price}>{donHang.phaiThanhToan.toLocaleString('vi-VN')} đ</Text>
              </View>
            </View>

            <View style={styles.noticeBox}>
              <Icon name="information-circle-outline" color={Colors.primaryDark} size={18} />
              <Text style={styles.noticeText}>
                Thanh toán mô phỏng, không phát sinh giao dịch thực tế.
              </Text>
            </View>

            {!!thongBao && <ErrorBanner message={thongBao} />}

            {dangXuLy && <ActivityIndicator color={Colors.primary} style={{ marginVertical: 8 }} />}

            <View style={styles.actions}>
              <TouchableOpacity
                accessibilityRole="button"
                style={[styles.btn, styles.btnSuccess, dangXuLy && styles.disabled]}
                disabled={dangXuLy}
                onPress={() => xuLyKetQua('ThanhCong')}
              >
                <Icon name="checkmark-circle-outline" color={Colors.white} size={18} />
                <Text style={styles.btnText}>Thanh toán thành công</Text>
              </TouchableOpacity>

              <TouchableOpacity
                accessibilityRole="button"
                style={[styles.btn, styles.btnDanger, dangXuLy && styles.disabled]}
                disabled={dangXuLy}
                onPress={() => xuLyKetQua('ThatBai')}
              >
                <Icon name="close-circle-outline" color={Colors.dangerText} size={18} />
                <Text style={[styles.btnText, styles.dangerText]}>Mô phỏng thất bại</Text>
              </TouchableOpacity>

              <TouchableOpacity
                accessibilityRole="button"
                style={[styles.btn, styles.btnCancel, dangXuLy && styles.disabled]}
                disabled={dangXuLy}
                onPress={() => xuLyKetQua('Huy')}
              >
                <Text style={[styles.btnText, { color: Colors.textSecondary }]}>Hủy đơn hàng</Text>
              </TouchableOpacity>
            </View>
          </ScrollView>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: { flex: 1, backgroundColor: Colors.scrim, justifyContent: 'flex-end', alignItems: 'center' },
  modalCard: { width: '100%', maxWidth: 560, maxHeight: '94%', backgroundColor: Colors.surface, borderTopLeftRadius: 24, borderTopRightRadius: 24, overflow: 'hidden' },
  sheetContent: { padding: 16, paddingBottom: 48, gap: 12 },
  header: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  logo: { width: 44, height: 44, borderRadius: 12, backgroundColor: Colors.primaryLight, alignItems: 'center', justifyContent: 'center' },
  title: { flex: 1, fontSize: 21, fontWeight: '800', color: Colors.textPrimary },
  infoBox: { backgroundColor: Colors.background, borderRadius: 16, padding: 14, gap: 10, borderWidth: 1, borderColor: Colors.border },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 12 },
  label: { color: Colors.textSecondary, fontSize: 14, flexShrink: 1 },
  value: { fontSize: 14, fontWeight: '600', color: Colors.textPrimary, maxWidth: '60%', textAlign: 'right' },
  total: { marginTop: 4, paddingTop: 10, borderTopWidth: 1, borderColor: Colors.border },
  totalLabel: { flexShrink: 1, fontWeight: '700', color: Colors.textPrimary, fontSize: 15 },
  price: { fontSize: 22, fontWeight: '800', color: Colors.primary, textAlign: 'right' },
  noticeBox: { flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.primaryLight, borderRadius: 12, padding: 10, gap: 8 },
  noticeText: { flex: 1, fontSize: 13, color: Colors.primaryDark },
  actions: { gap: 8 },
  btn: { minHeight: 48, flexDirection: 'row', gap: 8, padding: 10, borderRadius: 12, alignItems: 'center', justifyContent: 'center' },
  btnSuccess: { backgroundColor: Colors.primary },
  btnDanger: { backgroundColor: Colors.dangerSurface, borderWidth: 1, borderColor: Colors.danger },
  btnCancel: { borderWidth: 1, borderColor: Colors.border },
  btnText: { color: Colors.white, fontWeight: '700', fontSize: 14, textAlign: 'center', flexShrink: 1 },
  dangerText: { color: Colors.dangerText },
  disabled: { opacity: 0.55 },
});
