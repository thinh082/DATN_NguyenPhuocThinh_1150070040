import React, { useState, useEffect, useRef } from 'react';
import { StatusBar } from 'expo-status-bar';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  ScrollView,
  ActivityIndicator,
  useWindowDimensions,
  Alert,
  AppState,
} from 'react-native';
import * as ScreenCapture from 'expo-screen-capture';
import * as Brightness from 'expo-brightness';
import QRCode from 'react-native-qrcode-svg';
import SoatVeScreen from './SoatVeScreen';
import DangKyScreen from './DangKyScreen';
import HoSoHanhKhach from './HoSoHanhKhach';
import { BrandHeader, ErrorBanner, headerInset, Icon, InputField } from './components/MobileUi';
import { TraCuuTuyen, LichSuChuyen } from './TuyenVaLichSu';
import { Colors } from './constants/colors';
import * as Crypto from 'expo-crypto';
import ThanhToanMoPhongModal, { DonHangThanhToan } from './ThanhToanMoPhongModal';
import QuetQrXeScreen from './QuetQrXeScreen';
import {
  dangNhap,
  ThongTinNguoiDung,
  luuPhienDangNhap,
  xoaPhienDangNhap,
  khoiPhucPhienDangNhap,
} from './services/xacThucService';
import { taoQrOffline, luuViOffline, docViOffline } from './services/viVeOffline';
import { layDanhSachVeCuaToi, layDanhSachBieuGia, taoDonHang, VeDienTu, BieuGia } from './services/veService';

const ticketStatusLabels: Record<string, string> = {
  HoatDong: 'Đang hoạt động',
  TamKhoa: 'Tạm khóa',
  ThuHoi: 'Thu hồi',
};

export default function App() {
  const { width } = useWindowDimensions();
  // Trang thai dang nhap: mac dinh chua dang nhap (null)
  const [nguoiDungHienTai, setNguoiDungHienTai] = useState<ThongTinNguoiDung | null>(null);
  const [dangKyMoi, setDangKyMoi] = useState(false);
  const [dangKhoiPhuc, setDangKhoiPhuc] = useState(true);
  const [cheDoKetNoi, setCheDoKetNoi] = useState<'Online' | 'Offline'>('Online');

  // Form dang nhap
  const [taiKhoan, setTaiKhoan] = useState('');
  const [matKhau, setMatKhau] = useState('');
  const [dangTai, setDangTai] = useState<boolean>(false);
  const [thongBaoLoi, setThongBaoLoi] = useState<string>('');

  // Dieu huong trong Trang Chu: 'trangChu' | 'capNhatThongTin' | 'traCuuTuyen' | 'taoQr' | 'lichSuChuyen' | 'quetQrXe'
  const [manHinhCon, setManHinhCon] = useState<string>('trangChu');

  // Du lieu nghiep vu
  const [danhSachVe, setDanhSachVe] = useState<VeDienTu[]>([]);
  const [danhSachGia, setDanhSachGia] = useState<BieuGia[]>([]);
  const [veChon, setVeChon] = useState<VeDienTu | null>(null);
  const [demLui, setDemLui] = useState<number>(30);
  const [qrHienTai, setQrHienTai] = useState<string>('');

  // Thanh toan mo phong & Mua nhanh QR
  const [donHangCanThanhToan, setDonHangCanThanhToan] = useState<DonHangThanhToan | null>(null);
  const [modalThanhToanVisible, setModalThanhToanVisible] = useState(false);
  const [xeDaNhanDien, setXeDaNhanDien] = useState<any | null>(null);

  // Quản lý độ sáng ban đầu trước khi bật màn QR
  const originalBrightnessRef = useRef<number | null>(null);

  // Khôi phục phiên ngoại tuyến an toàn khi khởi động ứng dụng
  useEffect(() => {
    (async () => {
      try {
        const res = await khoiPhucPhienDangNhap();
        if (res.trangThai === 'Online' && res.nguoiDung) {
          setNguoiDungHienTai(res.nguoiDung);
          setCheDoKetNoi('Online');
        } else if (res.trangThai === 'Offline' && res.nguoiDung) {
          setNguoiDungHienTai(res.nguoiDung);
          setCheDoKetNoi('Offline');
        } else if (res.trangThai === 'HetHan') {
          setNguoiDungHienTai(null);
          setThongBaoLoi(res.thongBao || 'Phiên đăng nhập đã hết hạn.');
        }
      } catch {
        // Giữ ở màn hình đăng nhập nếu lỗi
      } finally {
        setDangKhoiPhuc(false);
      }
    })();
  }, []);

  // Đếm lùi và bảo vệ màn hình Tạo/Mở QR
  useEffect(() => {
    let timer: any;
    if (manHinhCon === 'taoQr' && veChon) {
      if (__DEV__) {
        ScreenCapture.allowScreenCaptureAsync().catch(() => {});
      } else {
        ScreenCapture.preventScreenCaptureAsync().catch(() => {});
      }

      // Lưu độ sáng ban đầu và tăng tối đa để máy quét dễ đọc
      Brightness.getBrightnessAsync()
        .then((b) => {
          originalBrightnessRef.current = b;
          Brightness.setBrightnessAsync(1.0).catch(() => {});
        })
        .catch(() => {});

      const refresh = () => {
        const now = Date.now();
        setDemLui(30 - (Math.floor(now / 1000) % 30));
        if (veChon.trangThai !== 'HoatDong' || now >= Date.parse(veChon.hetHanLuc)) {
          setQrHienTai('');
          return;
        }
        try {
          setQrHienTai(taoQrOffline(veChon, now));
        } catch {
          setQrHienTai('');
        }
      };

      refresh();
      timer = setInterval(refresh, 1000);

      // Tự động làm mới QR ngay lập tức khi ứng dụng quay trở lại từ background
      const sub = AppState.addEventListener('change', (state) => {
        if (state === 'active') {
          refresh();
        }
      });

      return () => {
        clearInterval(timer);
        sub.remove();
        // Khôi phục độ sáng ban đầu
        if (originalBrightnessRef.current !== null) {
          Brightness.setBrightnessAsync(originalBrightnessRef.current).catch(() => {});
          originalBrightnessRef.current = null;
        }
        ScreenCapture.allowScreenCaptureAsync().catch(() => {});
      };
    } else {
      ScreenCapture.allowScreenCaptureAsync().catch(() => {});
    }

    return () => clearInterval(timer);
  }, [manHinhCon, veChon]);

  // Xu ly Dang nhap
  const xuLyDangNhap = async () => {
    try {
      setDangTai(true);
      setThongBaoLoi('');
      const res = await dangNhap(taiKhoan.trim(), matKhau);
      if (res.statusCode === 200) {
        await luuPhienDangNhap(res.data.token, res.data.nguoiDung);
        setNguoiDungHienTai(res.data.nguoiDung);
        setCheDoKetNoi('Online');
        setManHinhCon('trangChu');
      } else {
        setThongBaoLoi(res.message || 'Tài khoản hoặc mật khẩu không đúng');
      }
    } catch (e: any) {
      setThongBaoLoi(e.message || 'Không thể kết nối đến máy chủ ngrok');
    } finally {
      setDangTai(false);
    }
  };

  const xuLyDangXuat = async () => {
    if (originalBrightnessRef.current !== null) {
      Brightness.setBrightnessAsync(originalBrightnessRef.current).catch(() => {});
      originalBrightnessRef.current = null;
    }
    ScreenCapture.allowScreenCaptureAsync().catch(() => {});
    await xoaPhienDangNhap(nguoiDungHienTai?.id);
    setTaiKhoan('');
    setMatKhau('');
    setNguoiDungHienTai(null);
    setCheDoKetNoi('Online');
    setManHinhCon('trangChu');
    setVeChon(null);
    setDanhSachVe([]);
    setDanhSachGia([]);
  };

  // Tai vi ve khi mo muc Tao QR
  const moTaoQr = async () => {
    setManHinhCon('taoQr');
    if (nguoiDungHienTai) {
      try {
        setDangTai(true);
        if (cheDoKetNoi === 'Offline') {
          const cached = await docViOffline(nguoiDungHienTai.id).catch(() => []);
          setDanhSachVe(cached);
          setVeChon(cached[0] || null);
          return;
        }

        const res = await layDanhSachVeCuaToi(nguoiDungHienTai.id);
        if (res.statusCode === 200) {
          await luuViOffline(nguoiDungHienTai.id, res.data);
          setDanhSachVe(res.data);
          if (res.data.length > 0) {
            setVeChon(res.data[0]);
            setDemLui(res.data[0].soGiayConLai);
          }
        }
        const resGia = await layDanhSachBieuGia();
        if (resGia.statusCode === 200) setDanhSachGia(resGia.data);
      } catch (e: any) {
        if (e.statusCode === 0) {
          const cached = await docViOffline(nguoiDungHienTai.id).catch(() => []);
          setDanhSachVe(cached);
          setVeChon(cached[0] || null);
        } else {
          setDanhSachVe([]);
          setVeChon(null);
          Alert.alert('Không thể tải ví', e.message || 'Vui lòng đăng nhập lại.');
        }
      } finally {
        setDangTai(false);
      }
    }
  };

  const xuLyBatDauMuaVe = async (giaVeId: number, tenLoaiVe?: string, tenTuyen?: string) => {
    if (!nguoiDungHienTai) return;
    if (cheDoKetNoi === 'Offline') {
      Alert.alert('Chế độ ngoại tuyến', 'Không thể mua vé khi ngoại tuyến. Vui lòng kết nối Internet để tiếp tục.');
      return;
    }

    try {
      setDangTai(true);
      const maYeuCau = Crypto.randomUUID();
      const res = await taoDonHang({ nguoiDungId: nguoiDungHienTai.id, giaVeId, soLuong: 1, maYeuCau });
      if (res.statusCode === 200) {
        if (res.data.trangThai === 'MienPhi') {
          Alert.alert('Thành công', 'Vé miễn phí 100% đã được cấp vào ví!');
          setXeDaNhanDien(null);
          moTaoQr();
        } else {
          setDonHangCanThanhToan({
            donHangId: res.data.donHangId,
            maDonHang: res.data.maDonHang,
            phaiThanhToan: res.data.phaiThanhToan,
            tenLoaiVe,
            tenTuyen,
          });
          setModalThanhToanVisible(true);
        }
      } else {
        Alert.alert('Lỗi tạo đơn', res.message || 'Không thể tạo đơn hàng');
      }
    } catch (e: any) {
      Alert.alert('Lỗi', e.message || 'Không thể mua vé');
    } finally {
      setDangTai(false);
    }
  };

  const onThanhToanThanhCong = () => {
    setModalThanhToanVisible(false);
    setDonHangCanThanhToan(null);
    setXeDaNhanDien(null);
    moTaoQr();
  };

  // Màn hình đang kiểm tra khôi phục phiên
  if (dangKhoiPhuc) {
    return (
      <View style={[styles.container, { justifyContent: 'center', alignItems: 'center' }]}>
        <ActivityIndicator size="large" color={Colors.primary} />
        <Text style={{ marginTop: 12, color: Colors.textSecondary, fontSize: 14 }}>
          Đang khôi phục phiên làm việc...
        </Text>
      </View>
    );
  }

  // ==========================================
  // 1. GIAO DIỆN CHƯA ĐĂNG NHẬP: MÀN HÌNH LOGIN
  // ==========================================
  if (!nguoiDungHienTai) {
    if (dangKyMoi) return <DangKyScreen onBack={() => setDangKyMoi(false)} onRegistered={(account) => { setTaiKhoan(account); setMatKhau(''); setThongBaoLoi(''); setDangKyMoi(false); }} />;
    return (
      <View style={styles.container}>
        <StatusBar style="light" />

        <ScrollView keyboardShouldPersistTaps="handled" automaticallyAdjustKeyboardInsets>
          <BrandHeader />
          <View style={styles.loginContent}>
            <View style={styles.card}>
              <Text accessibilityRole="header" style={styles.formTitle}>Đăng nhập</Text>

              {!!thongBaoLoi && <ErrorBanner message={thongBaoLoi} />}

              <InputField
                label="Tài khoản"
                icon="person-outline"
                placeholder="Email hoặc số điện thoại"
                autoComplete="username"
                value={taiKhoan}
                onChangeText={setTaiKhoan}
                autoCapitalize="none"
                keyboardType="email-address"
              />

              <InputField
                label="Mật khẩu"
                icon="lock-closed-outline"
                placeholder="Nhập mật khẩu"
                autoComplete="current-password"
                value={matKhau}
                onChangeText={setMatKhau}
                secureTextEntry
              />

              <TouchableOpacity
                accessibilityRole="button"
                style={[styles.primaryButton, dangTai && styles.disabled]}
                onPress={xuLyDangNhap}
                disabled={dangTai}
              >
                {dangTai ? (
                  <ActivityIndicator color={Colors.white} />
                ) : (
                  <Text style={styles.primaryButtonText}>Đăng nhập</Text>
                )}
              </TouchableOpacity>

              <TouchableOpacity accessibilityRole="button" style={styles.registerLink} onPress={() => setDangKyMoi(true)}>
                <Text style={styles.backButtonText}>Tạo tài khoản</Text>
              </TouchableOpacity>
            </View>
          </View>
        </ScrollView>
      </View>
    );
  }

  // ==========================================
  // 2. GIAO DIỆN ĐÃ ĐĂNG NHẬP: TRANG CHỦ & 4 Ô VUÔNG
  // ==========================================
  if (nguoiDungHienTai.vaiTro === 'NhanVienSoatVe') {
    return <SoatVeScreen userId={nguoiDungHienTai.id} onLogout={xuLyDangXuat} isOffline={cheDoKetNoi === 'Offline'} />;
  }

  if (manHinhCon === 'quetQrXe') {
    return (
      <QuetQrXeScreen
        onBack={() => setManHinhCon('trangChu')}
        onXeNhanDien={(chuyen: any) => {
          setXeDaNhanDien(chuyen);
          setManHinhCon('trangChu');
        }}
      />
    );
  }

  function moQuetQrMuaNhanh() {
    if (cheDoKetNoi === 'Offline') {
      Alert.alert('Chế độ ngoại tuyến', 'Không thể nhận diện xe buýt khi ngoại tuyến. Vui lòng kết nối Internet.');
      return;
    }
    layDanhSachBieuGia().then((res) => { if (res.statusCode === 200) setDanhSachGia(res.data); });
    setManHinhCon('quetQrXe');
  }

  return (
    <View style={styles.container}>
      <StatusBar style="light" />

      {/* Header Trang Chu */}
      <View style={styles.homeHeader}>
        <View style={{ flex: 1 }}>
          <Text style={styles.homeGreeting}>Xin chào,</Text>
          <Text style={styles.homeUserName}>{nguoiDungHienTai.hoTen}</Text>
          <View style={[styles.networkBadge, cheDoKetNoi === 'Offline' && styles.networkOffline]}>
            <Icon name={cheDoKetNoi === 'Offline' ? 'cloud-offline-outline' : 'wifi-outline'} color={cheDoKetNoi === 'Offline' ? Colors.warningText : Colors.primaryDark} size={14} />
            <Text style={[styles.networkText, cheDoKetNoi === 'Offline' && { color: Colors.warningText }]}>{cheDoKetNoi === 'Offline' ? 'Ngoại tuyến' : 'Trực tuyến'}</Text>
          </View>
        </View>
        <TouchableOpacity accessibilityRole="button" style={styles.logoutButton} onPress={xuLyDangXuat}>
          <Icon name="log-out-outline" size={16} color={Colors.white} />
          <Text style={styles.logoutText}>Đăng xuất</Text>
        </TouchableOpacity>
      </View>

      {cheDoKetNoi === 'Offline' && (
        <View style={styles.offlineBanner}>
          <Icon name="warning-outline" color={Colors.warningText} size={16} />
          <Text style={styles.offlineBannerText}>Trạng thái có thể chưa cập nhật</Text>
        </View>
      )}

      {manHinhCon === 'capNhatThongTin' && (
        <View style={[styles.content, styles.profileScreen]}>
          <TouchableOpacity accessibilityRole="button" style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
            <Icon name="arrow-back" size={16} />
              <Text style={styles.backButtonText}>Trang chủ</Text>
          </TouchableOpacity>
          <HoSoHanhKhach user={nguoiDungHienTai} onUpdated={setNguoiDungHienTai} />
        </View>
      )}
      {manHinhCon !== 'capNhatThongTin' && <ScrollView contentContainerStyle={styles.content}>
        {dangTai && <ActivityIndicator size="small" color={Colors.primary} style={{ marginVertical: 8 }} />}

        {/* 2.1. MÀN HÌNH CHÍNH (TRANG CHỦ VỚI 4 Ô VUÔNG) */}
        {manHinhCon === 'trangChu' && (
          <View>
            {/* Nut Quet QR Mua Nhanh Tren Xe */}
            <TouchableOpacity
              accessibilityRole="button"
              style={styles.quickScanCard}
              onPress={moQuetQrMuaNhanh}
              activeOpacity={0.85}
            >
              <View style={styles.quickScanLeft}>
                <View style={styles.scanMark}><Icon name="scan-outline" color={Colors.primaryDark} size={24} /></View>
                <View style={{ flex: 1 }}>
                  <Text style={styles.quickScanTitle}>Quét QR trên xe mua vé nhanh</Text>
                  <Text style={styles.quickScanSubtitle}>Quét mã dán trên xe để tìm đúng chuyến</Text>
                </View>
              </View>
              <Icon name="arrow-forward" color={Colors.white} />
            </TouchableOpacity>

            <TouchableOpacity accessibilityRole="button" style={styles.walletCta} onPress={moTaoQr}>
              <Icon name="ticket-outline" />
              <Text style={styles.walletCtaText}>Mở ví vé QR của tôi</Text>
              <Icon name="chevron-forward" size={17} />
            </TouchableOpacity>

            <Text accessibilityRole="header" style={styles.sectionHeaderTitle}>Tiện ích</Text>

            {/* Lưới chức năng */}
            <View style={styles.gridContainer}>
              {/* O 1: CẬP NHẬT THÔNG TIN / NỘP HỒ SƠ ƯU TIÊN */}
              <TouchableOpacity
                accessibilityRole="button"
                style={styles.gridSquare}
                onPress={() => {
                  if (cheDoKetNoi === 'Offline') {
                    Alert.alert('Chế độ ngoại tuyến', 'Chức năng hồ sơ hành khách yêu cầu kết nối mạng trực tuyến.');
                    return;
                  }
                  setManHinhCon('capNhatThongTin');
                }}
              >
                <View style={styles.iconCircle}>
                  <Icon name="person-outline" size={23} />
                </View>
                <Text style={styles.gridTitle}>Hồ sơ & ưu tiên</Text>
              </TouchableOpacity>

              {/* O 2: TRA CỨU TUYẾN XE & TRẠM DỪNG */}
              <TouchableOpacity
                accessibilityRole="button"
                style={styles.gridSquare}
                onPress={() => setManHinhCon('traCuuTuyen')}
              >
                <View style={styles.iconCircle}>
                  <Icon name="map-outline" size={23} />
                </View>
                <Text style={styles.gridTitle}>Tra cứu tuyến xe</Text>
              </TouchableOpacity>

              {/* O 3: TẠO QR / VÍ VÉ CỦA TÔI */}
              <TouchableOpacity
                accessibilityRole="button"
                style={styles.gridSquare}
                onPress={moTaoQr}
              >
                <View style={styles.iconCircle}>
                  <Icon name="qr-code-outline" size={23} />
                </View>
                <Text style={styles.gridTitle}>Ví vé QR</Text>
              </TouchableOpacity>

              {/* O 4: LỊCH SỬ CHUYẾN ĐI */}
              <TouchableOpacity
                accessibilityRole="button"
                style={styles.gridSquare}
                onPress={() => {
                  if (cheDoKetNoi === 'Offline') {
                    Alert.alert('Chế độ ngoại tuyến', 'Xem lịch sử chuyến đi yêu cầu kết nối mạng.');
                    return;
                  }
                  setManHinhCon('lichSuChuyen');
                }}
              >
                <View style={styles.iconCircle}>
                  <Icon name="time-outline" size={23} />
                </View>
                <Text style={styles.gridTitle}>Lịch sử lượt đi</Text>
              </TouchableOpacity>
              <TouchableOpacity accessibilityRole="button" style={styles.gridSquare} onPress={moQuetQrMuaNhanh}>
                <View style={styles.iconCircle}><Icon name="scan-outline" size={23} /></View>
                <Text style={styles.gridTitle}>Quét QR xe</Text>
              </TouchableOpacity>
            </View>
          </View>
        )}

        {/* 2.3. MÀN HÌNH CON: TRA CỨU TUYẾN XE */}
        {manHinhCon === 'traCuuTuyen' && (
          <View style={styles.subScreenContainer}>
            <TouchableOpacity accessibilityRole="button" style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
              <Icon name="arrow-back" size={16} />
              <Text style={styles.backButtonText}>Trang chủ</Text>
            </TouchableOpacity>

            <TraCuuTuyen />
          </View>
        )}

        {/* 2.4. MÀN HÌNH CON: TẠO QR / VÍ VÉ */}
        {manHinhCon === 'taoQr' && (
          <View style={styles.subScreenContainer}>
            <View style={styles.walletTools}>
              <TouchableOpacity accessibilityRole="button" style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
                <Icon name="arrow-back" size={16} />
              <Text style={styles.backButtonText}>Trang chủ</Text>
              </TouchableOpacity>
              <TouchableOpacity
                accessibilityRole="button"
                style={styles.backButton}
                onPress={() => {
                  if (cheDoKetNoi === 'Offline') {
                    Alert.alert('Chế độ ngoại tuyến', 'Không thể nhận diện xe buýt khi ngoại tuyến. Vui lòng kết nối Internet.');
                    return;
                  }
                  setManHinhCon('quetQrXe');
                }}
              >
                <Icon name="scan-outline" size={16} />
                <Text style={styles.backButtonText}>Quét QR xe</Text>
              </TouchableOpacity>
            </View>

            {danhSachVe.length === 0 ? (
              <View style={styles.card}>
                <Text style={styles.emptyText}>Ví vé hiện đang trống. Hãy chọn vé bên dưới để mua hoặc quét mã QR trên xe buýt!</Text>
              </View>
            ) : (
              <>
                {veChon && (
                  <View style={styles.ticketCard}>
                    <View style={styles.ticketHeader}>
                      <View style={styles.ticketHeading}>
                        <Text style={styles.ticketType}>{veChon.tenLoaiVe}</Text>
                        <Text style={[styles.ticketActiveBadge, veChon.trangThai !== 'HoatDong' && styles.inactiveTicketBadge]}>
                          {ticketStatusLabels[veChon.trangThai] ?? veChon.trangThai}
                        </Text>
                      </View>
                      <Text style={styles.ticketRouteTitle}>{veChon.tenTuyen}</Text>
                      <Text style={styles.ticketSubTitle}>Đối tượng: {veChon.tenNhomDoiTuong}</Text>
                    </View>
                    <View style={styles.ticketPerforation} />
                    <View style={styles.qrCodeBox}>
                      {qrHienTai ? (
                        <QRCode value={qrHienTai} size={Math.min(216, Math.max(140, width - 100))} backgroundColor={Colors.white} quietZone={12} />
                      ) : (
                        <View style={{ alignItems: 'center', padding: 20 }}>
                          <Icon name="alert-circle-outline" color={Colors.dangerText} />
                          <Text style={styles.invalidTicketText}>
                            {veChon.trangThai !== 'HoatDong' ? 'Vé không còn hoạt động' : 'Vé đã hết hạn sử dụng'}
                          </Text>
                        </View>
                      )}
                    </View>

                    {qrHienTai ? (
                      <View style={styles.timerPanel}>
                        <View
                          accessibilityRole="progressbar"
                          accessibilityLabel="Thời gian còn lại của mã QR"
                          accessibilityValue={{ min: 0, max: 30, now: Math.max(0, Math.min(demLui, 30)) }}
                          style={styles.timerTrack}
                        >
                          <View style={[styles.timerFill, { width: `${Math.max(0, Math.min(demLui / 30, 1)) * 100}%` }]} />
                        </View>
                        <Text style={styles.timerLabel}>
                          Tự động làm mới sau {demLui}s{__DEV__ ? '' : ' • Chống chụp màn hình'}
                        </Text>
                      </View>
                    ) : null}

                    {cheDoKetNoi === 'Offline' && (
                      <View style={styles.secureAlert}>
                        <Icon name="warning-outline" color={Colors.warningText} size={17} />
                        <Text style={styles.secureAlertText}>
                          Ngoại tuyến. Trạng thái vé theo lần đồng bộ gần nhất.
                        </Text>
                      </View>
                    )}
                  </View>
                )}

                {/* Danh sach ve */}
                <View style={styles.card}>
                  <Text style={styles.cardTitle}>Vé trong ví ({danhSachVe.length})</Text>
                  <ScrollView horizontal contentContainerStyle={styles.ticketPicker}>
                    {danhSachVe.map((v) => (
                      <TouchableOpacity
                        key={v.id}
                        accessibilityRole="button"
                        accessibilityState={{ selected: veChon?.id === v.id }}
                        style={[styles.ticketRowItem, veChon?.id === v.id && styles.ticketRowActive]}
                        onPress={() => {
                          setVeChon(v);
                          setDemLui(v.soGiayConLai);
                        }}
                      >
                        <Text style={styles.ticketRowTitleText}>{v.tenLoaiVe} - {v.tenTuyen}</Text>
                        <Text style={styles.ticketRowSubText}>Hạn dùng: {new Date(v.hetHanLuc).toLocaleDateString('vi-VN')}</Text>
                      </TouchableOpacity>
                    ))}
                  </ScrollView>
                </View>
              </>
            )}

            {/* Mục Mua vé mới / Mua thêm vé (Luôn hiển thị kể cả khi ví đã có vé) */}
            <View style={styles.card}>
              <Text style={styles.cardTitle}>Mua vé xe buýt</Text>
              {danhSachGia.map((g) => (
                <View key={g.id} style={styles.fareRow}>
                  <View style={{ flex: 1 }}>
                    <Text style={styles.fareTitle}>{g.tenLoaiVe} - {g.tenNhomDoiTuong}</Text>
                    <Text style={styles.ticketRowSubText}>Tuyến: {g.tenTuyen}</Text>
                    <Text style={styles.ticketRowSubText}>Giá gốc {g.giaGoc.toLocaleString('vi-VN')} đ</Text>
                    {g.soTienMienGiam > 0 && <Text style={styles.ticketRowSubText}>Miễn giảm {g.soTienMienGiam.toLocaleString('vi-VN')} đ</Text>}
                    <Text style={styles.farePrice}>Thực thu {g.giaThucThu.toLocaleString('vi-VN')} đ</Text>
                  </View>
                  <TouchableOpacity
                    style={styles.buyBtn}
                    onPress={() => xuLyBatDauMuaVe(g.id, g.tenLoaiVe, g.tenTuyen)}
                  >
                    <Text style={styles.buyBtnText}>Mua vé</Text>
                  </TouchableOpacity>
                </View>
              ))}
            </View>
          </View>
        )}

        {/* 2.5. MÀN HÌNH CON: LỊCH SỬ CHUYẾN XE */}
        {manHinhCon === 'lichSuChuyen' && (
          <View style={styles.subScreenContainer}>
            <TouchableOpacity accessibilityRole="button" style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
              <Icon name="arrow-back" size={16} />
              <Text style={styles.backButtonText}>Trang chủ</Text>
            </TouchableOpacity>

            <LichSuChuyen />
          </View>
        )}

        {/* Panel Mua Nhanh Vé khi vừa quét được xe buýt */}
        {xeDaNhanDien && manHinhCon === 'trangChu' && (
          <View style={styles.quickBuyPanel}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <Text style={styles.quickBuyHeader}>XE BUÝT ĐÃ NHẬN DIỆN</Text>
              <TouchableOpacity accessibilityRole="button" accessibilityLabel="Đóng thông tin xe" style={styles.iconButton} onPress={() => setXeDaNhanDien(null)}>
                <Icon name="close" color={Colors.textMuted} />
              </TouchableOpacity>
            </View>
            <Text style={styles.quickBuyBusNumber}>Xe: {xeDaNhanDien.bienSoXe}</Text>
            <Text style={styles.quickBuyTripName}>Tuyến: {xeDaNhanDien.tenTuyen}</Text>

            <Text style={[styles.ticketRowSubText, { marginTop: 12, marginBottom: 8, fontWeight: 'bold' }]}>
              Chọn biểu giá để mua ngay vé cho chuyến này:
            </Text>

            {xeDaNhanDien.danhSachGiaVe && xeDaNhanDien.danhSachGiaVe.length > 0 ? (
              xeDaNhanDien.danhSachGiaVe.map((gv: any) => (
                <View key={gv.giaVeId} style={styles.fareRow}>
                  <View style={{ flex: 1 }}>
                    <Text style={styles.fareTitle}>{gv.tenLoaiVe} - {gv.tenNhomDoiTuong}</Text>
                    <Text style={styles.farePrice}>{gv.giaThucThu.toLocaleString('vi-VN')} đ</Text>
                  </View>
                  <TouchableOpacity
                    style={styles.buyBtn}
                    onPress={() => xuLyBatDauMuaVe(gv.giaVeId, gv.tenLoaiVe, xeDaNhanDien.tenTuyen)}
                  >
                    <Text style={styles.buyBtnText}>Mua vé này</Text>
                  </TouchableOpacity>
                </View>
              ))
            ) : (
              <Text style={{ color: Colors.textSecondary, fontStyle: 'italic', marginVertical: 6 }}>
                Không có biểu giá phù hợp cho tuyến này.
              </Text>
            )}
          </View>
        )}
      </ScrollView>}

      {/* Modal Thanh toán mô phỏng 3 trạng thái */}
      <ThanhToanMoPhongModal
        visible={modalThanhToanVisible}
        donHang={donHangCanThanhToan}
        onSuccess={onThanhToanThanhCong}
        onClose={() => {
          setModalThanhToanVisible(false);
          setDonHangCanThanhToan(null);
        }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: Colors.background },
  loginContent: { width: '100%', maxWidth: 480, alignSelf: 'center', padding: 16, paddingBottom: 48 },
  formTitle: { fontSize: 22, fontWeight: '800', color: Colors.textPrimary },
  primaryButton: { minHeight: 48, backgroundColor: Colors.primary, borderRadius: 12, alignItems: 'center', justifyContent: 'center', padding: 12 },
  primaryButtonText: { color: Colors.white, fontSize: 16, fontWeight: '700' },
  disabled: { opacity: 0.55 },
  registerLink: { minHeight: 48, alignItems: 'center', justifyContent: 'center' },
  homeHeader: {
    backgroundColor: Colors.primary,
    paddingTop: headerInset + 12,
    paddingBottom: 20,
    paddingHorizontal: 20,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    borderBottomLeftRadius: 24,
    borderBottomRightRadius: 24,
  },
  homeGreeting: { fontSize: 14, color: Colors.primaryLight },
  homeUserName: { fontSize: 22, fontWeight: '800', color: Colors.white },
  networkBadge: { flexDirection: 'row', alignItems: 'center', alignSelf: 'flex-start', gap: 5, borderRadius: 999, backgroundColor: Colors.primaryLight, marginTop: 8, paddingVertical: 4, paddingHorizontal: 8 },
  networkOffline: { backgroundColor: Colors.warningSurface },
  networkText: { color: Colors.primaryDark, fontSize: 12, fontWeight: '700' },
  logoutButton: { flexDirection: 'row', gap: 6, alignItems: 'center', minHeight: 48, paddingHorizontal: 12, borderRadius: 12, backgroundColor: Colors.primaryDark },
  logoutText: { color: Colors.white, fontSize: 13, fontWeight: '700' },
  offlineBanner: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, backgroundColor: Colors.warningSurface, padding: 10 },
  offlineBannerText: { flexShrink: 1, color: Colors.warningText, fontSize: 13, fontWeight: '600' },
  content: { width: '100%', maxWidth: 760, alignSelf: 'center', padding: 16, paddingBottom: 48, gap: 16 },
  profileScreen: { flex: 1, paddingBottom: 0 },
  sectionHeaderTitle: { fontSize: 18, fontWeight: '800', color: Colors.textPrimary, marginBottom: 12 },
  gridContainer: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', rowGap: 12 },
  gridSquare: { width: '48%', minHeight: 106, backgroundColor: Colors.surface, borderRadius: 16, padding: 12, gap: 8, borderWidth: 1, borderColor: Colors.border, elevation: 1 },
  iconCircle: { width: 40, height: 40, borderRadius: 999, backgroundColor: Colors.primaryLight, justifyContent: 'center', alignItems: 'center' },
  gridTitle: { fontSize: 14, fontWeight: '700', color: Colors.textPrimary },
  subScreenContainer: { gap: 12 },
  backButton: { flexDirection: 'row', gap: 6, alignItems: 'center', minHeight: 48, backgroundColor: Colors.surface, paddingHorizontal: 12, borderRadius: 12, borderWidth: 1, borderColor: Colors.border, alignSelf: 'flex-start' },
  backButtonText: { fontSize: 13, color: Colors.primaryDark, fontWeight: '700' },
  walletTools: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 8 },
  iconButton: { width: 48, height: 48, justifyContent: 'center', alignItems: 'center', borderRadius: 12 },
  card: { backgroundColor: Colors.surface, borderRadius: 16, padding: 16, gap: 12, borderWidth: 1, borderColor: Colors.border, elevation: 1 },
  cardTitle: { fontSize: 17, fontWeight: '800', color: Colors.textPrimary },
  emptyText: { fontSize: 14, color: Colors.textMuted, textAlign: 'center', paddingVertical: 16 },
  fareRow: { flexDirection: 'row', alignItems: 'center', gap: 12, padding: 12, borderRadius: 12, borderWidth: 1, borderColor: Colors.border, backgroundColor: Colors.background },
  fareTitle: { fontSize: 14, fontWeight: '700', color: Colors.textPrimary },
  farePrice: { fontSize: 18, fontWeight: '800', color: Colors.primary, marginTop: 4 },
  buyBtn: { minHeight: 48, justifyContent: 'center', alignItems: 'center', backgroundColor: Colors.primary, padding: 12, borderRadius: 12 },
  buyBtnText: { color: Colors.white, fontSize: 13, fontWeight: '700' },
  ticketCard: { backgroundColor: Colors.white, borderRadius: 16, borderWidth: 1, borderColor: Colors.border, overflow: 'hidden' },
  ticketHeader: { backgroundColor: Colors.primary, padding: 16, gap: 6 },
  ticketHeading: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 8 },
  ticketType: { fontSize: 20, fontWeight: '800', color: Colors.white },
  ticketActiveBadge: { fontSize: 12, fontWeight: '700', color: Colors.successText, backgroundColor: Colors.successSurface, borderRadius: 999, overflow: 'hidden', paddingVertical: 4, paddingHorizontal: 8 },
  inactiveTicketBadge: { color: Colors.dangerText, backgroundColor: Colors.dangerSurface },
  ticketRouteTitle: { fontSize: 16, fontWeight: '700', color: Colors.white },
  ticketSubTitle: { fontSize: 13, color: Colors.primaryLight },
  ticketPerforation: { marginHorizontal: 12, borderBottomWidth: 1, borderStyle: 'dashed', borderColor: Colors.primary },
  qrCodeBox: { backgroundColor: Colors.white, minHeight: 112, padding: 16, justifyContent: 'center', alignItems: 'center' },
  invalidTicketText: { color: Colors.dangerText, fontWeight: '700', textAlign: 'center' },
  timerPanel: { paddingHorizontal: 16, paddingBottom: 16, gap: 8 },
  timerTrack: { height: 8, borderRadius: 999, backgroundColor: Colors.grayLight, overflow: 'hidden' },
  timerFill: { height: '100%', borderRadius: 999, backgroundColor: Colors.accent },
  timerLabel: { fontSize: 12, color: Colors.textSecondary, textAlign: 'center' },
  secureAlert: { flexDirection: 'row', alignItems: 'center', gap: 8, backgroundColor: Colors.warningSurface, margin: 12, padding: 10, borderRadius: 12 },
  secureAlertText: { flex: 1, fontSize: 12, color: Colors.warningText, fontWeight: '600' },
  ticketPicker: { gap: 8, paddingBottom: 5 },
  ticketRowItem: { minHeight: 48, maxWidth: 220, padding: 12, borderRadius: 12, borderWidth: 1, borderColor: Colors.border },
  ticketRowActive: { borderColor: Colors.primary, backgroundColor: Colors.primaryLight },
  ticketRowTitleText: { fontSize: 14, fontWeight: '700', color: Colors.textPrimary },
  ticketRowSubText: { fontSize: 12, color: Colors.textSecondary, marginTop: 2 },
  quickScanCard: { backgroundColor: Colors.primaryDark, minHeight: 72, borderRadius: 16, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', gap: 12 },
  quickScanLeft: { flexDirection: 'row', alignItems: 'center', gap: 12, flex: 1 },
  scanMark: { width: 44, height: 44, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: Colors.primaryLight },
  quickScanTitle: { fontSize: 15, fontWeight: '800', color: Colors.white },
  quickScanSubtitle: { fontSize: 12, color: Colors.primaryLight, marginTop: 3 },
  walletCta: { minHeight: 48, flexDirection: 'row', alignItems: 'center', gap: 10, borderWidth: 1, borderColor: Colors.border, backgroundColor: Colors.surface, padding: 12, borderRadius: 12, marginBottom: 20 },
  walletCtaText: { flex: 1, fontSize: 14, fontWeight: '700', color: Colors.primaryDark },
  quickBuyPanel: { backgroundColor: Colors.surface, borderRadius: 16, padding: 16, gap: 8, borderWidth: 1, borderColor: Colors.border, elevation: 2 },
  quickBuyHeader: { fontSize: 13, fontWeight: '700', color: Colors.primaryDark },
  quickBuyBusNumber: { fontSize: 18, fontWeight: '800', color: Colors.textPrimary },
  quickBuyTripName: { fontSize: 13, color: Colors.textSecondary },
});
