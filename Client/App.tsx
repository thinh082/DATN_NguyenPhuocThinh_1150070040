import React, { useState, useEffect, useRef } from 'react';
import { StatusBar } from 'expo-status-bar';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  ScrollView,
  ActivityIndicator,
  TextInput,
  Alert,
  AppState,
} from 'react-native';
import * as ScreenCapture from 'expo-screen-capture';
import * as Brightness from 'expo-brightness';
import QRCode from 'react-native-qrcode-svg';
import SoatVeScreen from './SoatVeScreen';
import DangKyScreen from './DangKyScreen';
import HoSoHanhKhach from './HoSoHanhKhach';
import { TraCuuTuyen, LichSuChuyen } from './TuyenVaLichSu';
import Colors from './constants/colors';
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
import { setAccessToken } from './services/api';
import { taoQrOffline, luuViOffline, docViOffline } from './services/viVeOffline';
import { layDanhSachVeCuaToi, layDanhSachBieuGia, taoDonHang, VeDienTu, BieuGia } from './services/veService';

export default function App() {
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
      // Chống chụp ảnh màn hình
      ScreenCapture.preventScreenCaptureAsync().catch(() => {});

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

        <View style={styles.loginHeader}>
          <Text style={styles.loginLogo}>🚌</Text>
          <Text style={styles.loginTitle}>SMARTBUS TP.HCM</Text>
          <Text style={styles.loginSubtitle}>Hệ Thống Vé Xe Buýt Thông Minh</Text>
        </View>

        <ScrollView contentContainerStyle={styles.loginContent}>
          <View style={styles.card}>
            <Text style={styles.formTitle}>Đăng Nhập Tài Khoản</Text>

            {thongBaoLoi ? (
              <View style={styles.errorBox}>
                <Text style={styles.errorText}>{thongBaoLoi}</Text>
              </View>
            ) : null}

            <Text style={styles.inputLabel}>Tài khoản (Email):</Text>
            <TextInput
              style={styles.input}
              placeholder="Nhập email"
              value={taiKhoan}
              onChangeText={setTaiKhoan}
              autoCapitalize="none"
              keyboardType="email-address"
            />

            <Text style={styles.inputLabel}>Mật khẩu:</Text>
            <TextInput
              style={styles.input}
              placeholder="Nhập mật khẩu"
              value={matKhau}
              onChangeText={setMatKhau}
              secureTextEntry
            />

            <TouchableOpacity
              style={styles.primaryButton}
              onPress={xuLyDangNhap}
              disabled={dangTai}
            >
              {dangTai ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.primaryButtonText}>Đăng Nhập</Text>
              )}
            </TouchableOpacity>

            <View style={{ flexDirection: 'row', justifyContent: 'center', marginTop: 16 }}>
              <Text style={{ color: Colors.textSecondary }}>Chưa có tài khoản? </Text>
              <TouchableOpacity onPress={() => setDangKyMoi(true)}>
                <Text style={{ color: Colors.primary, fontWeight: 'bold' }}>Đăng ký ngay</Text>
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

  return (
    <View style={styles.container}>
      <StatusBar style="light" />

      {/* Header Trang Chu */}
      <View style={styles.homeHeader}>
        <View style={{ flex: 1 }}>
          <Text style={styles.homeGreeting}>Xin chào,</Text>
          <Text style={styles.homeUserName}>{nguoiDungHienTai.hoTen}</Text>
          <Text style={styles.homeRoleBadge}>
            {nguoiDungHienTai.vaiTro === 'HanhKhach' ? 'Hành Khách' : nguoiDungHienTai.vaiTro}
          </Text>
        </View>
        <TouchableOpacity style={styles.logoutButton} onPress={xuLyDangXuat}>
          <Text style={styles.logoutText}>Đăng xuất</Text>
        </TouchableOpacity>
      </View>

      {cheDoKetNoi === 'Offline' && (
        <View style={styles.offlineBanner}>
          <Text style={styles.offlineBannerText}>⚠️ Ngoại tuyến · trạng thái có thể chưa cập nhật</Text>
        </View>
      )}

      <ScrollView contentContainerStyle={styles.content}>
        {dangTai && <ActivityIndicator size="small" color={Colors.primary} style={{ marginVertical: 8 }} />}

        {/* 2.1. MÀN HÌNH CHÍNH (TRANG CHỦ VỚI 4 Ô VUÔNG) */}
        {manHinhCon === 'trangChu' && (
          <View>
            {/* Nut Quet QR Mua Nhanh Tren Xe */}
            <TouchableOpacity
              style={styles.quickScanCard}
              onPress={() => {
                if (cheDoKetNoi === 'Offline') {
                  Alert.alert('Chế độ ngoại tuyến', 'Không thể nhận diện xe buýt khi ngoại tuyến. Vui lòng kết nối Internet.');
                  return;
                }
                layDanhSachBieuGia().then((res) => { if (res.statusCode === 200) setDanhSachGia(res.data); });
                setManHinhCon('quetQrXe');
              }}
              activeOpacity={0.85}
            >
              <View style={styles.quickScanLeft}>
                <Text style={styles.quickScanIcon}>📸</Text>
                <View style={{ marginLeft: 12 }}>
                  <Text style={styles.quickScanTitle}>Quét QR Xe Buýt (Mua Nhanh)</Text>
                  <Text style={styles.quickScanSubtitle}>Quét mã dán trên xe để chọn tuyến & mua vé ngay</Text>
                </View>
              </View>
              <Text style={styles.quickScanArrow}>→</Text>
            </TouchableOpacity>

            <Text style={styles.sectionHeaderTitle}>Dịch Vụ Nổi Bật</Text>

            {/* 4 Ô VUÔNG (GRID 2x2) */}
            <View style={styles.gridContainer}>
              {/* O 1: CẬP NHẬT THÔNG TIN / NỘP HỒ SƠ ƯU TIÊN */}
              <TouchableOpacity
                style={styles.gridSquare}
                onPress={() => {
                  if (cheDoKetNoi === 'Offline') {
                    Alert.alert('Chế độ ngoại tuyến', 'Chức năng hồ sơ hành khách yêu cầu kết nối mạng trực tuyến.');
                    return;
                  }
                  setManHinhCon('capNhatThongTin');
                }}
              >
                <View style={[styles.iconCircle, { backgroundColor: '#E0F2FE' }]}>
                  <Text style={styles.gridIcon}>👤</Text>
                </View>
                <Text style={styles.gridTitle}>Hồ Sơ & Ưu Tiên</Text>
              </TouchableOpacity>

              {/* O 2: TRA CỨU TUYẾN XE & TRẠM DỪNG */}
              <TouchableOpacity
                style={styles.gridSquare}
                onPress={() => setManHinhCon('traCuuTuyen')}
              >
                <View style={[styles.iconCircle, { backgroundColor: '#FEF3C7' }]}>
                  <Text style={styles.gridIcon}>🗺️</Text>
                </View>
                <Text style={styles.gridTitle}>Tra Cứu Tuyến Xe</Text>
              </TouchableOpacity>

              {/* O 3: TẠO QR / VÍ VÉ CỦA TÔI */}
              <TouchableOpacity
                style={styles.gridSquare}
                onPress={moTaoQr}
              >
                <View style={[styles.iconCircle, { backgroundColor: '#DCFCE7' }]}>
                  <Text style={styles.gridIcon}>📱</Text>
                </View>
                <Text style={styles.gridTitle}>Ví Vé & Tạo QR</Text>
              </TouchableOpacity>

              {/* O 4: LỊCH SỬ CHUYẾN ĐI */}
              <TouchableOpacity
                style={styles.gridSquare}
                onPress={() => {
                  if (cheDoKetNoi === 'Offline') {
                    Alert.alert('Chế độ ngoại tuyến', 'Xem lịch sử chuyến đi yêu cầu kết nối mạng.');
                    return;
                  }
                  setManHinhCon('lichSuChuyen');
                }}
              >
                <View style={[styles.iconCircle, { backgroundColor: '#F3E8FF' }]}>
                  <Text style={styles.gridIcon}>🕒</Text>
                </View>
                <Text style={styles.gridTitle}>Lịch Sử Lượt Đi</Text>
              </TouchableOpacity>
            </View>
          </View>
        )}

        {/* 2.2. MÀN HÌNH CON: CẬP NHẬT THÔNG TIN / NỘP HỒ SƠ */}
        {manHinhCon === 'capNhatThongTin' && (
          <View style={styles.subScreenContainer}>
            <TouchableOpacity style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
              <Text style={styles.backButtonText}>← Quay lại Trang Chủ</Text>
            </TouchableOpacity>

            <HoSoHanhKhach user={nguoiDungHienTai} onUpdated={setNguoiDungHienTai} />
          </View>
        )}

        {/* 2.3. MÀN HÌNH CON: TRA CỨU TUYẾN XE */}
        {manHinhCon === 'traCuuTuyen' && (
          <View style={styles.subScreenContainer}>
            <TouchableOpacity style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
              <Text style={styles.backButtonText}>← Quay lại Trang Chủ</Text>
            </TouchableOpacity>

            <TraCuuTuyen />
          </View>
        )}

        {/* 2.4. MÀN HÌNH CON: TẠO QR / VÍ VÉ */}
        {manHinhCon === 'taoQr' && (
          <View style={styles.subScreenContainer}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
              <TouchableOpacity style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
                <Text style={styles.backButtonText}>← Quay lại Trang Chủ</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.quickScanCard, { marginTop: 0, paddingVertical: 8, paddingHorizontal: 12 }]}
                onPress={() => {
                  if (cheDoKetNoi === 'Offline') {
                    Alert.alert('Chế độ ngoại tuyến', 'Không thể nhận diện xe buýt khi ngoại tuyến. Vui lòng kết nối Internet.');
                    return;
                  }
                  setManHinhCon('quetQrXe');
                }}
              >
                <Text style={{ fontSize: 13, fontWeight: 'bold', color: Colors.primary }}>📸 Quét QR xe</Text>
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
                      <Text style={styles.ticketType}>{veChon.tenLoaiVe.toUpperCase()}</Text>
                      <Text style={[styles.ticketActiveBadge, veChon.trangThai !== 'HoatDong' && { color: '#EF4444' }]}>
                        {veChon.trangThai === 'HoatDong' ? '● HOẠT ĐỘNG' : `● ${veChon.trangThai.toUpperCase()}`}
                      </Text>
                    </View>

                    <Text style={styles.ticketRouteTitle}>{veChon.tenTuyen}</Text>
                    <Text style={styles.ticketSubTitle}>Đối tượng: {veChon.tenNhomDoiTuong}</Text>

                    <View style={styles.qrCodeBox}>
                      {qrHienTai ? (
                        <QRCode value={qrHienTai} size={150} />
                      ) : (
                        <View style={{ alignItems: 'center', padding: 20 }}>
                          <Text style={{ color: '#EF4444', fontWeight: 'bold' }}>
                            {veChon.trangThai !== 'HoatDong' ? 'Vé không còn hoạt động' : 'Vé đã hết hạn sử dụng'}
                          </Text>
                        </View>
                      )}
                    </View>

                    {qrHienTai ? (
                      <View style={styles.timerRow}>
                        <Text style={styles.timerLabel}>Mã tự đổi sau: </Text>
                        <View style={styles.timerBox}>
                          <Text style={styles.timerNumber}>{demLui}s</Text>
                        </View>
                      </View>
                    ) : null}

                    {cheDoKetNoi === 'Offline' && (
                      <View style={[styles.secureAlert, { backgroundColor: '#FEF3C7', borderColor: '#F59E0B' }]}>
                        <Text style={[styles.secureAlertText, { color: '#92400E' }]}>
                          ⚠️ Chế độ ngoại tuyến: Trạng thái vé được lưu theo lần đồng bộ online gần nhất.
                        </Text>
                      </View>
                    )}
                  </View>
                )}

                {/* Danh sach ve */}
                <View style={styles.card}>
                  <Text style={styles.cardTitle}>Vé Đã Mua Trong Ví ({danhSachVe.length})</Text>
                  {danhSachVe.map((v) => (
                    <TouchableOpacity
                      key={v.id}
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
                </View>
              </>
            )}

            {/* Mục Mua vé mới / Mua thêm vé (Luôn hiển thị kể cả khi ví đã có vé) */}
            <View style={styles.card}>
              <Text style={styles.cardTitle}>Biểu Giá Mua Vé Xe Buýt</Text>
              <Text style={[styles.ticketRowSubText, { marginBottom: 10 }]}>Chọn loại vé để mua vé lượt hoặc vé tháng:</Text>
              {danhSachGia.map((g) => (
                <View key={g.id} style={styles.fareRow}>
                  <View style={{ flex: 1 }}>
                    <Text style={styles.fareTitle}>{g.tenLoaiVe} - {g.tenNhomDoiTuong}</Text>
                    <Text style={styles.ticketRowSubText}>Tuyến: {g.tenTuyen}</Text>
                    <Text style={styles.farePrice}>{g.giaThucThu.toLocaleString('vi-VN')} đ</Text>
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
            <TouchableOpacity style={styles.backButton} onPress={() => setManHinhCon('trangChu')}>
              <Text style={styles.backButtonText}>← Quay lại Trang Chủ</Text>
            </TouchableOpacity>

            <LichSuChuyen />
          </View>
        )}

        {/* 2.6. MÀN HÌNH CON: QUÉT QR XE ĐỂ MUA NHANH */}
        {manHinhCon === 'quetQrXe' && (
          <View style={{ minHeight: 500 }}>
            <QuetQrXeScreen
              onBack={() => setManHinhCon('trangChu')}
              onXeNhanDien={(chuyen: any) => {
                setXeDaNhanDien(chuyen);
                setManHinhCon('trangChu');
              }}
            />
          </View>
        )}

        {/* Panel Mua Nhanh Vé khi vừa quét được xe buýt */}
        {xeDaNhanDien && manHinhCon === 'trangChu' && (
          <View style={styles.quickBuyPanel}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <Text style={styles.quickBuyHeader}>XE BUÝT ĐÃ NHẬN DIỆN</Text>
              <TouchableOpacity onPress={() => setXeDaNhanDien(null)}>
                <Text style={{ fontSize: 16, color: '#94A3B8', fontWeight: 'bold' }}>✕</Text>
              </TouchableOpacity>
            </View>
            <Text style={styles.quickBuyBusNumber}>🚌 Xe: {xeDaNhanDien.bienSoXe}</Text>
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
      </ScrollView>

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
  container: {
    flex: 1,
    backgroundColor: Colors.background,
  },
  offlineBanner: {
    backgroundColor: '#FEF3C7',
    borderBottomWidth: 1,
    borderBottomColor: '#F59E0B',
    paddingVertical: 10,
    paddingHorizontal: 16,
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'center',
  },
  offlineBannerText: {
    color: '#92400E',
    fontSize: 13,
    fontWeight: '600',
  },
  // Style Dang Nhap
  loginHeader: {
    backgroundColor: Colors.primary,
    paddingTop: 60,
    paddingBottom: 24,
    alignItems: 'center',
    borderBottomLeftRadius: 24,
    borderBottomRightRadius: 24,
  },
  loginLogo: {
    fontSize: 48,
    marginBottom: 8,
  },
  loginTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: Colors.white,
    letterSpacing: 0.5,
  },
  loginSubtitle: {
    fontSize: 12,
    color: Colors.primaryLight,
    marginTop: 4,
  },
  loginContent: {
    padding: 20,
  },
  formTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: Colors.textPrimary,
    marginBottom: 16,
    textAlign: 'center',
  },
  inputLabel: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.textSecondary,
    marginBottom: 6,
    marginTop: 10,
  },
  input: {
    backgroundColor: Colors.background,
    borderWidth: 1,
    borderColor: Colors.border,
    borderRadius: 10,
    padding: 12,
    fontSize: 14,
    color: Colors.textPrimary,
  },
  primaryButton: {
    backgroundColor: Colors.primary,
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
    marginTop: 20,
  },
  primaryButtonText: {
    color: Colors.white,
    fontSize: 15,
    fontWeight: 'bold',
  },
  errorBox: {
    backgroundColor: '#FEE2E2',
    padding: 10,
    borderRadius: 8,
    marginBottom: 12,
  },
  errorText: {
    color: Colors.danger,
    fontSize: 13,
    textAlign: 'center',
  },

  // Style Trang Chu
  homeHeader: {
    backgroundColor: Colors.primary,
    paddingTop: 54,
    paddingBottom: 16,
    paddingHorizontal: 20,
    flexDirection: 'row',
    alignItems: 'center',
    borderBottomLeftRadius: 20,
    borderBottomRightRadius: 20,
  },
  homeGreeting: {
    fontSize: 12,
    color: Colors.primaryLight,
  },
  homeUserName: {
    fontSize: 18,
    fontWeight: 'bold',
    color: Colors.white,
  },
  homeRoleBadge: {
    fontSize: 11,
    backgroundColor: Colors.primaryDark,
    color: Colors.primaryLight,
    alignSelf: 'flex-start',
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 6,
    marginTop: 4,
    fontWeight: '600',
  },
  logoutButton: {
    backgroundColor: 'rgba(255,255,255,0.2)',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
  },
  logoutText: {
    color: Colors.white,
    fontSize: 12,
    fontWeight: '600',
  },
  content: {
    padding: 16,
    gap: 16,
  },
  sectionHeaderTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: Colors.textPrimary,
    marginBottom: 14,
  },

  // 4 Ô VUÔNG (GRID 2x2)
  gridContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    gap: 14,
  },
  gridSquare: {
    width: '47%',
    aspectRatio: 1,
    backgroundColor: Colors.white,
    borderRadius: 18,
    padding: 16,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: Colors.border,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 6,
    elevation: 3,
  },
  iconCircle: {
    width: 60,
    height: 60,
    borderRadius: 30,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 12,
  },
  gridIcon: {
    fontSize: 30,
  },
  gridTitle: {
    fontSize: 14,
    fontWeight: '700',
    color: Colors.textPrimary,
    textAlign: 'center',
    lineHeight: 18,
  },

  // Style cac man hinh con
  subScreenContainer: {
    gap: 14,
  },
  backButton: {
    backgroundColor: Colors.white,
    paddingVertical: 10,
    paddingHorizontal: 14,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: Colors.border,
    alignSelf: 'flex-start',
  },
  backButtonText: {
    fontSize: 13,
    color: Colors.primary,
    fontWeight: '700',
  },
  card: {
    backgroundColor: Colors.white,
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: Colors.border,
  },
  cardTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: Colors.textPrimary,
    marginBottom: 12,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  infoLabel: {
    fontSize: 13,
    color: Colors.textSecondary,
  },
  infoValue: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.textPrimary,
  },
  routeItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  routeBadge: {
    backgroundColor: Colors.primary,
    width: 42,
    height: 42,
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
  },
  routeBadgeText: {
    color: Colors.white,
    fontSize: 15,
    fontWeight: 'bold',
  },
  routeTitle: {
    fontSize: 14,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  routeSubtitle: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  emptyText: {
    fontSize: 13,
    color: Colors.textMuted,
    textAlign: 'center',
    paddingVertical: 16,
  },
  fareRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  fareTitle: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.textPrimary,
  },
  farePrice: {
    fontSize: 14,
    fontWeight: 'bold',
    color: Colors.primary,
    marginTop: 2,
  },
  buyBtn: {
    backgroundColor: Colors.primary,
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 8,
  },
  buyBtnText: {
    color: Colors.white,
    fontSize: 12,
    fontWeight: 'bold',
  },
  ticketCard: {
    backgroundColor: Colors.white,
    borderRadius: 18,
    padding: 16,
    borderWidth: 2,
    borderColor: Colors.primary,
    alignItems: 'center',
  },
  ticketHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    width: '100%',
    marginBottom: 6,
  },
  ticketType: {
    fontSize: 13,
    fontWeight: 'bold',
    color: Colors.primary,
  },
  ticketActiveBadge: {
    fontSize: 11,
    fontWeight: 'bold',
    color: Colors.success,
  },
  ticketRouteTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: Colors.textPrimary,
    textAlign: 'center',
  },
  ticketSubTitle: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  qrCodeBox: {
    width: 170,
    height: 170,
    backgroundColor: '#F1F5F9',
    borderRadius: 16,
    borderWidth: 2,
    borderColor: Colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginVertical: 14,
  },
  qrCodeText: {
    fontSize: 26,
    fontWeight: '900',
    color: Colors.primaryDark,
    letterSpacing: 2,
  },
  qrHint: {
    fontSize: 10,
    color: Colors.textMuted,
    marginTop: 4,
    fontWeight: '600',
  },
  timerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 10,
  },
  timerLabel: {
    fontSize: 12,
    color: Colors.textSecondary,
  },
  timerBox: {
    backgroundColor: Colors.primaryLight,
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 6,
  },
  timerNumber: {
    fontSize: 13,
    fontWeight: 'bold',
    color: Colors.primary,
  },
  secureAlert: {
    backgroundColor: '#FEF3C7',
    padding: 8,
    borderRadius: 8,
    width: '100%',
    alignItems: 'center',
    marginBottom: 8,
  },
  secureAlertText: {
    fontSize: 11,
    color: '#92400E',
    fontWeight: '600',
  },
  payloadText: {
    fontSize: 9,
    color: Colors.textMuted,
    fontFamily: 'monospace',
    textAlign: 'center',
  },
  ticketRowItem: {
    padding: 12,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: Colors.border,
    marginBottom: 8,
  },
  ticketRowActive: {
    borderColor: Colors.primary,
    backgroundColor: Colors.primaryLight,
  },
  ticketRowTitleText: {
    fontSize: 13,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  ticketRowSubText: {
    fontSize: 11,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  historyItem: {
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  historyRoute: {
    fontSize: 14,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  historyTime: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  historyStatus: {
    fontSize: 12,
    color: Colors.success,
    marginTop: 4,
    fontWeight: '500',
  },
  quickScanCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 14,
    padding: 14,
    marginBottom: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 1,
    borderColor: '#E2E8F0',
    shadowColor: '#000',
    shadowOpacity: 0.05,
    shadowRadius: 5,
    elevation: 2,
  },
  quickScanLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  quickScanIcon: {
    fontSize: 26,
  },
  quickScanTitle: {
    fontSize: 14,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  quickScanSubtitle: {
    fontSize: 11,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  quickScanArrow: {
    fontSize: 18,
    color: Colors.primary,
    fontWeight: 'bold',
    marginLeft: 8,
  },
  quickBuyPanel: {
    backgroundColor: '#FFFFFF',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 18,
    borderWidth: 1,
    borderColor: '#E2E8F0',
    marginTop: -20,
    elevation: 10,
    shadowColor: '#000',
    shadowOpacity: 0.15,
    shadowRadius: 10,
  },
  quickBuyHeader: {
    fontSize: 13,
    fontWeight: 'bold',
    color: '#059669',
    marginBottom: 4,
  },
  quickBuyBusNumber: {
    fontSize: 18,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  quickBuyTripName: {
    fontSize: 13,
    color: '#475569',
    marginTop: 2,
  },
});
