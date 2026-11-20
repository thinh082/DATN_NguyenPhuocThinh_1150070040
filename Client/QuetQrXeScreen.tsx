import React, { useRef, useState } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  useWindowDimensions,
} from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import { StatusBar } from 'expo-status-bar';
import { headerInset, Icon } from './components/MobileUi';
import { Colors } from './constants/colors';
import { quetQrDanXe } from './services/vanHanhService';

interface Props {
  onBack: () => void;
  onXeNhanDien: (thongTinXe: any) => void;
}

const GUID_REGEX = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export default function QuetQrXeScreen({ onBack, onXeNhanDien }: Props) {
  const { width, height } = useWindowDimensions();
  const [permission, requestPermission] = useCameraPermissions();
  const [dangXuLy, setDangXuLy] = useState(false);
  const [thongBao, setThongBao] = useState('Hướng camera vào mã QR cố định dán trên xe buýt.');
  const isLocked = useRef(false);

  if (!permission) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  if (!permission.granted) {
    return (
      <View style={styles.centerContainer}>
        <StatusBar style="dark" />
        <Icon name="camera-outline" size={30} />
        <Text style={styles.permissionTitle}>Yêu cầu quyền Camera</Text>
        <Text style={styles.permissionText}>
          SmartBus cần quyền truy cập camera để quét mã QR dán trên xe buýt và tự động nhận diện chuyến xe.
        </Text>
        <TouchableOpacity style={styles.btnPrimary} onPress={requestPermission}>
          <Text style={styles.btnPrimaryText}>Cấp quyền camera</Text>
        </TouchableOpacity>
        <TouchableOpacity style={[styles.btnSecondary, { marginTop: 12 }]} onPress={onBack}>
          <Text style={styles.btnSecondaryText}>Quay lại</Text>
        </TouchableOpacity>
      </View>
    );
  }

  const handleBarcodeScanned = async ({ data }: { data: string }) => {
    if (isLocked.current || dangXuLy) return;
    const cleanData = data?.trim();
    if (!cleanData) return;

    if (!GUID_REGEX.test(cleanData)) {
      isLocked.current = true;
      Alert.alert(
        'Mã QR không đúng định dạng',
        'Mã QR trên xe buýt phải là mã định danh GUID hợp lệ. Vui lòng quét đúng mã dán trên xe.',
        [{ text: 'Quét lại', onPress: () => { isLocked.current = false; } }]
      );
      return;
    }

    try {
      isLocked.current = true;
      setDangXuLy(true);
      setThongBao('Đang kiểm tra thông tin xe buýt và chuyến đang chạy...');

      const res = await quetQrDanXe(cleanData);
      if (res.statusCode === 200) {
        onXeNhanDien(res.data);
      } else {
        Alert.alert(
          'Không thể nhận diện xe buýt',
          res.message || 'Phương tiện không hoạt động hoặc không có chuyến đang chạy.',
          [{ text: 'Thử lại', onPress: () => { isLocked.current = false; setThongBao('Hướng camera vào mã QR dán trên xe.'); } }]
        );
      }
    } catch (e: any) {
      Alert.alert(
        'Lỗi quét mã QR',
        e.message || 'Không thể kết nối đến máy chủ để xác thực mã QR xe buýt.',
        [{ text: 'Thử lại', onPress: () => { isLocked.current = false; setThongBao('Hướng camera vào mã QR dán trên xe.'); } }]
      );
    } finally {
      setDangXuLy(false);
    }
  };

  return (
    <View style={styles.container}>
      <StatusBar style="light" />
      <CameraView
        style={StyleSheet.absoluteFill}
        facing="back"
        barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
        onBarcodeScanned={handleBarcodeScanned}
      />

      {/* Overlay giao dien */}
      <View style={styles.overlay}>
        <View style={styles.topBar}>
          <TouchableOpacity accessibilityRole="button" style={styles.backBtn} onPress={onBack}>
            <Icon name="close" color={Colors.white} size={18} />
            <Text style={styles.backBtnText}>Đóng</Text>
          </TouchableOpacity>
          <Text style={styles.screenTitle}>Quét QR trên xe</Text>
        </View>

        <View pointerEvents="none" style={[styles.scanTargetBox, { width: Math.min(260, width - 64, height * 0.4) }]}>
          <View style={styles.cornerTL} />
          <View style={styles.cornerTR} />
          <View style={styles.cornerBL} />
          <View style={styles.cornerBR} />
        </View>

        <View style={styles.bottomInfo}>
          {dangXuLy ? (
            <ActivityIndicator color="#FFFFFF" size="small" style={{ marginBottom: 8 }} />
          ) : null}
          <Text style={styles.infoText}>{thongBao}</Text>
          <Text style={styles.subInfoText}>Hệ thống sẽ tự xác định tuyến và chuyến xe đang hoạt động</Text>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000',
  },
  centerContainer: {
    flex: 1,
    backgroundColor: Colors.background,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  permissionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: Colors.textPrimary,
    marginBottom: 8,
  },
  permissionText: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
    marginBottom: 20,
    lineHeight: 20,
  },
  btnPrimary: {
    backgroundColor: Colors.primary,
    paddingVertical: 12,
    paddingHorizontal: 24,
    borderRadius: 12,
    minHeight: 48,
    justifyContent: 'center',
  },
  btnPrimaryText: {
    color: '#FFF',
    fontWeight: 'bold',
  },
  btnSecondary: {
    minHeight: 48,
    justifyContent: 'center',
    paddingVertical: 10,
    paddingHorizontal: 20,
  },
  btnSecondaryText: {
    color: Colors.primary,
    fontWeight: '600',
  },
  overlay: {
    flex: 1,
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: headerInset + 12,
    paddingBottom: 48,
    gap: 12,
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    width: '90%',
    gap: 12,
    backgroundColor: Colors.scrim,
    borderRadius: 16,
    padding: 6,
  },
  backBtn: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    backgroundColor: Colors.primaryDark,
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 999,
  },
  backBtnText: {
    color: '#FFF',
    fontSize: 14,
    fontWeight: '600',
  },
  screenTitle: {
    color: '#FFF',
    fontWeight: 'bold',
    fontSize: 15,
    letterSpacing: 0.5,
  },
  scanTargetBox: {
    aspectRatio: 1,
    borderRadius: 16,
    position: 'relative',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.2)',
  },
  cornerTL: {
    position: 'absolute',
    top: -2,
    left: -2,
    width: 32,
    height: 32,
    borderTopWidth: 4,
    borderLeftWidth: 4,
    borderColor: Colors.accent,
    borderTopLeftRadius: 16,
  },
  cornerTR: {
    position: 'absolute',
    top: -2,
    right: -2,
    width: 32,
    height: 32,
    borderTopWidth: 4,
    borderRightWidth: 4,
    borderColor: Colors.accent,
    borderTopRightRadius: 16,
  },
  cornerBL: {
    position: 'absolute',
    bottom: -2,
    left: -2,
    width: 32,
    height: 32,
    borderBottomWidth: 4,
    borderLeftWidth: 4,
    borderColor: Colors.accent,
    borderBottomLeftRadius: 16,
  },
  cornerBR: {
    position: 'absolute',
    bottom: -2,
    right: -2,
    width: 32,
    height: 32,
    borderBottomWidth: 4,
    borderRightWidth: 4,
    borderColor: Colors.accent,
    borderBottomRightRadius: 16,
  },
  bottomInfo: {
    backgroundColor: Colors.scrim,
    paddingVertical: 14,
    paddingHorizontal: 20,
    borderRadius: 14,
    alignItems: 'center',
    width: '88%',
  },
  infoText: {
    color: '#FFF',
    fontSize: 14,
    fontWeight: '600',
    textAlign: 'center',
  },
  subInfoText: {
    color: '#94A3B8',
    fontSize: 12,
    marginTop: 4,
    textAlign: 'center',
  },
});
