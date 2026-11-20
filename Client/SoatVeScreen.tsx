import React, { useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Modal,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import { useAudioPlayer } from 'expo-audio';
import * as Crypto from 'expo-crypto';
import type { SQLiteDatabase } from 'expo-sqlite';
import Colors from './constants/colors';
import apiClient from './services/api';
import { docGoi, dongBoNhatKy, luuGoi, moKho, quetVaLuu, layThongKeDongBo, datLaiSuKienLoi } from './services/khoSoatVe';
import type { GoiKiemTra } from './services/soatVeOffline.mjs';

type Device = { id: number; tenThietBi: string };
type Trip = { id: number; tenTuyen: string; bienSo: string };

export default function SoatVeScreen({ userId, onLogout, isOffline }: { userId: number; onLogout: () => void; isOffline?: boolean }) {
  const [permission, requestPermission] = useCameraPermissions();
  const [db, setDb] = useState<SQLiteDatabase | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [trips, setTrips] = useState<Trip[]>([]);
  const [device, setDevice] = useState<number | null>(null);
  const [trip, setTrip] = useState<number | null>(null);
  const [pack, setPack] = useState<GoiKiemTra | null>(null);
  const [message, setMessage] = useState('Tải dữ liệu khi có mạng trước khi quét.');
  const [scanning, setScanning] = useState(false);
  const [busy, setBusy] = useState(false);
  const [syncStats, setSyncStats] = useState<{ choXuLy: number; biLoi: number; daDongBo: number } | null>(null);
  const lock = useRef(false);
  const successSound = useAudioPlayer(require('./assets/scan-ok.wav'));
  const failureSound = useAudioPlayer(require('./assets/scan-error.wav'));

  // Tra cứu khẩn cấp & Khóa vé
  const [lookupModal, setLookupModal] = useState(false);
  const [lookupKeyword, setLookupKeyword] = useState('');
  const [lookupLoading, setLookupLoading] = useState(false);
  const [lookupResult, setLookupResult] = useState<any | null>(null);

  // Popup Khóa vé
  const [lockModal, setLockModal] = useState(false);
  const [ticketToLock, setTicketToLock] = useState<string | null>(null);
  const [lockReason, setLockReason] = useState('');
  const [lockLoading, setLockLoading] = useState(false);

  useEffect(() => {
    let live = true;
    let opened: SQLiteDatabase | null = null;
    (async () => {
      try {
        opened = await moKho(userId);
        const saved = await docGoi(opened);
        const stats = await layThongKeDongBo(opened).catch(() => null);
        if (live) {
          setDb(opened);
          setPack(saved);
          if (stats) setSyncStats(stats);
        } else {
          await opened.closeAsync();
        }
      } catch (e: any) {
        if (live) setMessage(e.message || 'Không mở được kho ngoại tuyến.');
      }
    })();
    return () => {
      live = false;
      if (opened) void opened.closeAsync();
    };
  }, [userId]);

  async function loadOptions() {
    if (isOffline) {
      setMessage('Thiết bị đang ở chế độ ngoại tuyến. Không thể tải danh sách phiên mới.');
      return;
    }
    try {
      setBusy(true);
      const res = await apiClient.get('/api/SoatVe/chuan-bi');
      if (res.data.statusCode !== 200) throw new Error(res.data.message);
      setDevices(res.data.data.thietBis);
      setTrips(res.data.data.chuyenXes);
      setMessage('Chọn thiết bị và chuyến đang chạy.');
    } catch (e: any) {
      setMessage(e.message || 'Không tải được dữ liệu.');
    } finally {
      setBusy(false);
    }
  }

  async function prepare() {
    if (!db || !device || !trip) return;
    if (isOffline) {
      setMessage('Không thể bắt đầu phiên mới khi đang ngoại tuyến.');
      return;
    }
    try {
      setBusy(true);
      const session = await apiClient.post('/api/VanHanh/bat-dau-phien-soat-ve', { thietBiId: device, chuyenXeId: trip });
      if (session.data.statusCode !== 200) throw new Error(session.data.message);
      const res = await apiClient.get('/api/SoatVe/goi-kiem-tra', { params: { phienId: session.data.data.phienId } });
      if (res.data.statusCode !== 200) throw new Error(res.data.message);
      await luuGoi(db, res.data.data);
      setPack(res.data.data);
      setMessage(`Đã tải ${res.data.data.ves.length} vé. Có thể ngắt mạng để quét.`);
    } catch (e: any) {
      setMessage(e.message || 'Không tải được gói kiểm tra.');
    } finally {
      setBusy(false);
    }
  }

  async function onScan(data: string) {
    if (!db || lock.current) return;
    lock.current = true;
    setScanning(false);
    try {
      const result = await quetVaLuu(db, data);
      setMessage(result.hopLe ? 'Vé hợp lệ — mời hành khách lên xe.' : `Từ chối: ${result.maLyDo}`);
      const player = result.hopLe ? successSound : failureSound;
      try {
        await player.seekTo(0);
        player.play();
      } catch {
        /* Ignore sound playback errors */
      }
      const stats = await layThongKeDongBo(db).catch(() => null);
      if (stats) setSyncStats(stats);
    } catch (e: any) {
      setMessage(e.message || 'Không thể lưu kết quả quét.');
    } finally {
      lock.current = false;
    }
  }

  async function sync() {
    if (!db) return;
    try {
      setBusy(true);
      const result = await dongBoNhatKy(db);
      setMessage(`Đã đồng bộ ${result.sent} lượt.${result.rejected ? ` ${result.rejected} lượt máy chủ từ chối, cần đối soát.` : ''}${result.warnings ? ` ${result.warnings} lượt có cảnh báo thu hồi.` : ''}${result.pendingErrors ? ` ${result.pendingErrors} lượt bị lỗi chưa xác nhận.` : ''}${result.legacy ? ` ${result.legacy} lượt cũ chưa có mã gói.` : ''}`);
      const stats = await layThongKeDongBo(db).catch(() => null);
      if (stats) setSyncStats(stats);
    } catch (e: any) {
      setMessage(e.message || 'Đồng bộ chưa hoàn tất, hãy thử lại khi có mạng.');
    } finally {
      setBusy(false);
    }
  }

  async function endSession() {
    if (!db || !pack) return;
    try {
      setBusy(true);
      const response = await apiClient.post('/api/VanHanh/ket-thuc-phien-soat-ve', { phienId: pack.phienId });
      if (response.data.statusCode !== 200) throw new Error(response.data.message);
      await db.runAsync('DELETE FROM package WHERE id=1');
      setPack(null);
      setScanning(false);
      setMessage('Đã kết thúc phiên. Các lượt chưa đồng bộ vẫn được lưu an toàn trên máy.');
    } catch (e: any) {
      setMessage(e.message || 'Không kết thúc được phiên. Hãy kiểm tra kết nối.');
    } finally {
      setBusy(false);
    }
  }

  // Tra cứu khẩn cấp (Online)
  async function xuLyTraCuu() {
    if (isOffline) {
      Alert.alert('Chế độ ngoại tuyến', 'Chức năng tra cứu khẩn cấp theo CCCD/SĐT yêu cầu kết nối mạng trực tuyến.');
      return;
    }
    if (!pack) return;
    const kw = lookupKeyword.trim();
    if (!kw) {
      Alert.alert('Thông báo', 'Vui lòng nhập số CCCD (12 chữ số) hoặc số điện thoại.');
      return;
    }
    const isCccd = /^\d{12}$/.test(kw);
    try {
      setLookupLoading(true);
      setLookupResult(null);
      const res = await apiClient.post('/api/SoatVe/tra-cuu-khan-cap', {
        phienSoatVeId: pack.phienId,
        thietBiSoatVeId: pack.thietBiId,
        cccd: isCccd ? kw : undefined,
        soDienThoai: !isCccd ? kw : undefined,
      });

      if (res.data.statusCode === 200) {
        setLookupResult(res.data.data);
      } else {
        Alert.alert('Kết quả', res.data.message || 'Không tìm thấy dữ liệu.');
      }
    } catch (e: any) {
      Alert.alert('Lỗi tra cứu', e.message || 'Không thể kết nối đến máy chủ.');
    } finally {
      setLookupLoading(false);
    }
  }

  // Xác nhận lượt đi thủ công (Online)
  async function xuLyXacNhan(maVe: string) {
    if (!pack) return;
    try {
      setLookupLoading(true);
      const reqId = Crypto.randomUUID();
      const res = await apiClient.post('/api/SoatVe/xac-nhan-thu-cong', {
        phienSoatVeId: pack.phienId,
        thietBiSoatVeId: pack.thietBiId,
        maVe,
        requestId: reqId,
      });

      if (res.data.statusCode === 200 && res.data.data.hopLe) {
        try { await successSound.seekTo(0); successSound.play(); } catch {}
        Alert.alert('Thành công', 'Đã ghi nhận lượt đi hợp lệ cho hành khách!');
        setMessage(`Xác nhận thủ công thành công vé ${maVe.slice(0, 8)}...`);
        setLookupModal(false);
        setLookupResult(null);
        setLookupKeyword('');
      } else {
        try { await failureSound.seekTo(0); failureSound.play(); } catch {}
        Alert.alert('Từ chối lượt đi', res.data.data?.thongBao || res.data.message || 'Vé không hợp lệ');
      }
    } catch (e: any) {
      try { await failureSound.seekTo(0); failureSound.play(); } catch {}
      Alert.alert('Lỗi xác nhận', e.message || 'Không thể xác nhận lượt đi.');
    } finally {
      setLookupLoading(false);
    }
  }

  // Khóa vé nghi ngờ
  async function xuLyKhoaVe() {
    if (!ticketToLock || !lockReason.trim()) {
      Alert.alert('Thông báo', 'Vui lòng nhập lý do khóa vé nghi ngờ.');
      return;
    }
    try {
      setLockLoading(true);
      const res = await apiClient.post('/api/Ve/khoa-ve', {
        maVe: ticketToLock,
        lyDo: lockReason.trim(),
        thuHoiVinhVien: false,
      });

      if (res.data.statusCode === 200) {
        Alert.alert('Thành công', 'Vé đã được chuyển sang trạng thái Tạm khóa để đối soát.');
        setLockModal(false);
        setTicketToLock(null);
        setLockReason('');
        // Refresh lại kết quả tra cứu
        void xuLyTraCuu();
      } else {
        Alert.alert('Lỗi', res.data.message || 'Không thể khóa vé.');
      }
    } catch (e: any) {
      Alert.alert('Lỗi', e.message || 'Không thể khóa vé.');
    } finally {
      setLockLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.screenTitle}>Soát vé SmartBus</Text>

      {isOffline && (
        <View style={styles.offlineBanner}>
          <Text style={styles.offlineBannerText}>⚠️ Ngoại tuyến · trạng thái có thể chưa cập nhật</Text>
        </View>
      )}

      {syncStats && (syncStats.choXuLy > 0 || syncStats.biLoi > 0) && (
        <View style={styles.syncStatsBox}>
          <Text style={styles.syncStatsText}>
            📦 Hàng đợi offline: {syncStats.choXuLy} chưa gửi
            {syncStats.biLoi > 0 ? ` • ${syncStats.biLoi} bị lỗi máy chủ` : ''}
          </Text>
          {syncStats.biLoi > 0 && (
            <TouchableOpacity
              style={styles.retryFailedBtn}
              disabled={busy}
              onPress={async () => {
                if (!db) return;
                await datLaiSuKienLoi(db);
                await sync();
              }}
            >
              <Text style={styles.retryFailedBtnText}>Thử lại sự kiện lỗi ({syncStats.biLoi})</Text>
            </TouchableOpacity>
          )}
        </View>
      )}

      <View style={styles.statusBar}>
        <Text style={styles.statusText}>{message}</Text>
      </View>
      <Text style={styles.infoHint}>Nhật ký quét được lưu an toàn trên máy cho đến khi đồng bộ.</Text>
      {busy && <ActivityIndicator color={Colors.primary} />}

      {!pack && (
        <View style={styles.card}>
          <TouchableOpacity style={styles.btnSecondary} onPress={loadOptions} disabled={busy || !db}>
            <Text style={styles.btnSecondaryText}>Tải danh sách thiết bị và chuyến</Text>
          </TouchableOpacity>

          {devices.length > 0 && (
            <View style={{ marginTop: 12 }}>
              <Text style={styles.label}>Chọn thiết bị soát vé:</Text>
              {devices.map(d => (
                <TouchableOpacity
                  key={d.id}
                  style={[styles.itemBtn, device === d.id && styles.itemBtnActive]}
                  onPress={() => setDevice(d.id)}
                >
                  <Text style={[styles.itemBtnText, device === d.id && styles.itemBtnTextActive]}>
                    {device === d.id ? '✓ ' : ''}{d.tenThietBi}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>
          )}

          {trips.length > 0 && (
            <View style={{ marginTop: 12 }}>
              <Text style={styles.label}>Chọn chuyến xe đang chạy:</Text>
              {trips.map(t => (
                <TouchableOpacity
                  key={t.id}
                  style={[styles.itemBtn, trip === t.id && styles.itemBtnActive]}
                  onPress={() => setTrip(t.id)}
                >
                  <Text style={[styles.itemBtnText, trip === t.id && styles.itemBtnTextActive]}>
                    {trip === t.id ? '✓ ' : ''}{t.tenTuyen} — {t.bienSo}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>
          )}

          <TouchableOpacity
            style={[styles.btnPrimary, { marginTop: 16 }]}
            onPress={prepare}
            disabled={busy || !device || !trip || !db}
          >
            <Text style={styles.btnPrimaryText}>Bắt đầu phiên & tải gói offline</Text>
          </TouchableOpacity>
        </View>
      )}

      {pack && (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>Phiên Soát Vé Đang Hoạt Động</Text>
          <Text style={styles.cardSubtitle}>
            Gói kiểm tra: {pack.ves.length} vé • Hạn dùng: {new Date(pack.hetHanLuc).toLocaleTimeString('vi-VN')}
          </Text>

          <View style={styles.actionGrid}>
            {!permission?.granted && (
              <TouchableOpacity style={styles.btnSecondary} onPress={() => void requestPermission()}>
                <Text style={styles.btnSecondaryText}>Cấp quyền camera</Text>
              </TouchableOpacity>
            )}

            <TouchableOpacity
              style={[styles.btnPrimary, { backgroundColor: '#0284C7' }]}
              disabled={!pack || !db || !permission?.granted || scanning}
              onPress={() => setScanning(true)}
            >
              <Text style={styles.btnPrimaryText}>📷 Bật Camera Quét Mã QR</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.btnPrimary, { backgroundColor: '#F59E0B' }]}
              disabled={!pack || busy || scanning}
              onPress={() => { setLookupModal(true); setLookupResult(null); }}
            >
              <Text style={styles.btnPrimaryText}>🔍 Tra cứu khẩn cấp (CCCD / SĐT)</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.btnSecondary]}
              disabled={!db || busy || scanning}
              onPress={sync}
            >
              <Text style={styles.btnSecondaryText}>🔄 Đồng bộ lượt quét lên máy chủ</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.btnDanger]}
              disabled={!pack || busy || scanning}
              onPress={endSession}
            >
              <Text style={styles.btnDangerText}>🛑 Kết thúc phiên soát vé</Text>
            </TouchableOpacity>
          </View>
        </View>
      )}

      {scanning && (
        <View style={styles.cameraBox}>
          <CameraView
            style={StyleSheet.absoluteFill}
            barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
            onBarcodeScanned={({ data }) => void onScan(data)}
          />
          <TouchableOpacity style={styles.closeCamBtn} onPress={() => setScanning(false)}>
            <Text style={styles.closeCamText}>✕ Tắt Camera</Text>
          </TouchableOpacity>
        </View>
      )}

      <TouchableOpacity style={styles.btnLogout} onPress={onLogout}>
        <Text style={styles.btnLogoutText}>Đăng xuất tài khoản</Text>
      </TouchableOpacity>

      {/* Modal Tra Cứu Khẩn Cấp */}
      <Modal visible={lookupModal} animationType="slide" transparent onRequestClose={() => setLookupModal(false)}>
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>TRA CỨU VÉ KHẨN CẤP</Text>
              <TouchableOpacity onPress={() => setLookupModal(false)}>
                <Text style={styles.closeModalText}>✕</Text>
              </TouchableOpacity>
            </View>
            <Text style={styles.modalSubtitle}>
              Áp dụng khi hành khách hết pin hoặc không có smartphone. Nhập số CCCD (12 số) hoặc số điện thoại.
            </Text>

            <View style={styles.searchRow}>
              <TextInput
                style={styles.searchInput}
                placeholder="Nhập số CCCD hoặc SĐT..."
                value={lookupKeyword}
                onChangeText={setLookupKeyword}
                keyboardType="numeric"
              />
              <TouchableOpacity style={styles.searchBtn} onPress={xuLyTraCuu} disabled={lookupLoading}>
                <Text style={styles.searchBtnText}>Tìm</Text>
              </TouchableOpacity>
            </View>

            {lookupLoading && <ActivityIndicator color={Colors.primary} style={{ marginVertical: 12 }} />}

            {lookupResult && (
              <ScrollView style={{ maxHeight: 340, marginTop: 12 }}>
                {lookupResult.timThay ? (
                  <View>
                    <View style={styles.passengerBox}>
                      <Text style={styles.passengerName}>👤 {lookupResult.thongTinKhach?.hoTen}</Text>
                      <Text style={styles.passengerMeta}>
                        CCCD: {lookupResult.thongTinKhach?.cccd} • SĐT: {lookupResult.thongTinKhach?.soDienThoai}
                      </Text>
                    </View>

                    <Text style={[styles.label, { marginTop: 12, marginBottom: 6 }]}>Danh sách vé:</Text>
                    {lookupResult.danhSachVe?.length === 0 ? (
                      <Text style={styles.emptyNotice}>Hành khách không có vé nào còn hạn.</Text>
                    ) : (
                      lookupResult.danhSachVe.map((v: any) => (
                        <View key={v.maVe} style={[styles.ticketItem, !v.coTheSuDung && styles.ticketItemDisabled]}>
                          <View style={{ flex: 1 }}>
                            <Text style={styles.ticketType}>{v.tenLoaiVe} — {v.tenNhomDoiTuong}</Text>
                            <Text style={styles.ticketRoute}>Tuyến: {v.tenTuyen}</Text>
                            <Text style={styles.ticketExpiry}>Hạn dùng: {new Date(v.hetHanLuc).toLocaleDateString('vi-VN')}</Text>
                            {v.lyDo && <Text style={styles.ticketReason}>⚠️ {v.lyDo}</Text>}
                          </View>

                          <View style={styles.ticketActions}>
                            {v.coTheSuDung && (
                              <TouchableOpacity
                                style={styles.confirmBtn}
                                onPress={() => xuLyXacNhan(v.maVe)}
                                disabled={lookupLoading}
                              >
                                <Text style={styles.confirmBtnText}>✓ Xác nhận</Text>
                              </TouchableOpacity>
                            )}

                            {v.trangThai === 'HoatDong' && (
                              <TouchableOpacity
                                style={styles.lockBtn}
                                onPress={() => { setTicketToLock(v.maVe); setLockReason(''); setLockModal(true); }}
                                disabled={lookupLoading}
                              >
                                <Text style={styles.lockBtnText}>🔒 Tạm khóa</Text>
                              </TouchableOpacity>
                            )}
                          </View>
                        </View>
                      ))
                    )}
                  </View>
                ) : (
                  <View style={styles.emptyNoticeBox}>
                    <Text style={styles.emptyNoticeText}>{lookupResult.thongBao || 'Không tìm thấy hành khách'}</Text>
                  </View>
                )}
              </ScrollView>
            )}

            <TouchableOpacity style={styles.closeBtn} onPress={() => setLookupModal(false)}>
              <Text style={styles.closeBtnText}>Đóng</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>

      {/* Modal Nhập lý do Tạm khóa vé */}
      <Modal visible={lockModal} animationType="fade" transparent onRequestClose={() => setLockModal(false)}>
        <View style={styles.modalOverlay}>
          <View style={[styles.modalContent, { padding: 20 }]}>
            <Text style={styles.modalTitle}>TẠM KHÓA VÉ NGHI NGỜ</Text>
            <Text style={styles.modalSubtitle}>
              Hành động này sẽ tạm dừng hiệu lực của vé trên hệ thống để chuyển ban điều hành đối soát.
            </Text>

            <Text style={[styles.label, { marginTop: 12 }]}>Lý do khóa vé (*):</Text>
            <TextInput
              style={[styles.searchInput, { marginTop: 6, minHeight: 60, textAlignVertical: 'top' }]}
              placeholder="Ví dụ: Nghi ngờ mượn vé tháng của người khác..."
              value={lockReason}
              onChangeText={setLockReason}
              multiline
            />

            {lockLoading && <ActivityIndicator color={Colors.primary} style={{ marginTop: 10 }} />}

            <View style={{ flexDirection: 'row', gap: 10, marginTop: 16 }}>
              <TouchableOpacity
                style={[styles.btnSecondary, { flex: 1 }]}
                onPress={() => setLockModal(false)}
                disabled={lockLoading}
              >
                <Text style={styles.btnSecondaryText}>Hủy</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.btnDanger, { flex: 1 }]}
                onPress={xuLyKhoaVe}
                disabled={lockLoading}
              >
                <Text style={styles.btnDangerText}>Xác nhận khóa</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    padding: 20,
    paddingTop: 50,
    gap: 12,
    backgroundColor: '#F8FAFC',
  },
  screenTitle: {
    fontSize: 22,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  statusBar: {
    backgroundColor: '#E0F2FE',
    padding: 10,
    borderRadius: 8,
    borderLeftWidth: 4,
    borderLeftColor: Colors.primary,
  },
  statusText: {
    fontSize: 13,
    color: '#0369A1',
    fontWeight: '600',
  },
  infoHint: {
    fontSize: 12,
    color: Colors.textSecondary,
  },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 14,
    padding: 16,
    borderWidth: 1,
    borderColor: '#E2E8F0',
    elevation: 2,
    shadowColor: '#000',
    shadowOpacity: 0.05,
    shadowRadius: 6,
  },
  cardTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  cardSubtitle: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 4,
  },
  label: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.textPrimary,
  },
  actionGrid: {
    marginTop: 14,
    gap: 10,
  },
  itemBtn: {
    padding: 10,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#E2E8F0',
    marginTop: 6,
  },
  itemBtnActive: {
    borderColor: Colors.primary,
    backgroundColor: '#EFF6FF',
  },
  itemBtnText: {
    fontSize: 13,
    color: Colors.textPrimary,
  },
  itemBtnTextActive: {
    color: Colors.primary,
    fontWeight: 'bold',
  },
  btnPrimary: {
    backgroundColor: Colors.primary,
    paddingVertical: 12,
    borderRadius: 10,
    alignItems: 'center',
  },
  btnPrimaryText: {
    color: '#FFFFFF',
    fontWeight: 'bold',
    fontSize: 14,
  },
  btnSecondary: {
    backgroundColor: '#F1F5F9',
    paddingVertical: 12,
    borderRadius: 10,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#CBD5E1',
  },
  btnSecondaryText: {
    color: '#334155',
    fontWeight: '600',
    fontSize: 14,
  },
  btnDanger: {
    backgroundColor: '#FEE2E2',
    paddingVertical: 12,
    borderRadius: 10,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#FCA5A5',
  },
  btnDangerText: {
    color: '#DC2626',
    fontWeight: 'bold',
    fontSize: 14,
  },
  cameraBox: {
    height: 300,
    borderRadius: 14,
    overflow: 'hidden',
    position: 'relative',
    marginTop: 10,
  },
  closeCamBtn: {
    position: 'absolute',
    top: 12,
    right: 12,
    backgroundColor: 'rgba(0,0,0,0.6)',
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 16,
  },
  closeCamText: {
    color: '#FFF',
    fontSize: 12,
    fontWeight: '600',
  },
  btnLogout: {
    marginTop: 10,
    paddingVertical: 12,
    alignItems: 'center',
  },
  btnLogoutText: {
    color: '#64748B',
    fontSize: 14,
    fontWeight: '600',
  },
  // Modal Tra Cuu
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 16,
  },
  modalContent: {
    backgroundColor: '#FFF',
    borderRadius: 18,
    padding: 18,
    width: '100%',
    maxHeight: '90%',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  modalTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: Colors.primary,
  },
  modalSubtitle: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 4,
    lineHeight: 16,
  },
  closeModalText: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#94A3B8',
    padding: 4,
  },
  searchRow: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 14,
  },
  searchInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#CBD5E1',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 14,
  },
  searchBtn: {
    backgroundColor: Colors.primary,
    paddingHorizontal: 18,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  searchBtnText: {
    color: '#FFF',
    fontWeight: 'bold',
  },
  passengerBox: {
    backgroundColor: '#F8FAFC',
    borderRadius: 10,
    padding: 12,
    borderWidth: 1,
    borderColor: '#E2E8F0',
  },
  passengerName: {
    fontSize: 15,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  passengerMeta: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 4,
  },
  ticketItem: {
    backgroundColor: '#FFFFFF',
    borderRadius: 10,
    padding: 12,
    borderWidth: 1,
    borderColor: '#E2E8F0',
    marginBottom: 8,
    flexDirection: 'row',
    alignItems: 'center',
  },
  ticketItemDisabled: {
    backgroundColor: '#F8FAFC',
    borderColor: '#CBD5E1',
    opacity: 0.8,
  },
  ticketType: {
    fontSize: 13,
    fontWeight: 'bold',
    color: Colors.textPrimary,
  },
  ticketRoute: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  ticketExpiry: {
    fontSize: 11,
    color: '#64748B',
    marginTop: 2,
  },
  ticketReason: {
    fontSize: 11,
    color: '#DC2626',
    marginTop: 4,
    fontWeight: '500',
  },
  ticketActions: {
    gap: 6,
    marginLeft: 8,
  },
  confirmBtn: {
    backgroundColor: '#10B981',
    paddingVertical: 6,
    paddingHorizontal: 10,
    borderRadius: 6,
    alignItems: 'center',
  },
  confirmBtnText: {
    color: '#FFF',
    fontSize: 12,
    fontWeight: 'bold',
  },
  lockBtn: {
    backgroundColor: '#FEE2E2',
    paddingVertical: 6,
    paddingHorizontal: 10,
    borderRadius: 6,
    alignItems: 'center',
  },
  lockBtnText: {
    color: '#DC2626',
    fontSize: 11,
    fontWeight: '600',
  },
  emptyNoticeBox: {
    padding: 16,
    alignItems: 'center',
  },
  emptyNoticeText: {
    fontSize: 13,
    color: '#64748B',
  },
  emptyNotice: {
    fontSize: 12,
    color: '#64748B',
    fontStyle: 'italic',
  },
  closeBtn: {
    backgroundColor: '#F1F5F9',
    paddingVertical: 10,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 14,
  },
  closeBtnText: {
    color: '#475569',
    fontWeight: '600',
    fontSize: 13,
  },
  offlineBanner: {
    backgroundColor: '#FEF3C7',
    borderWidth: 1,
    borderColor: '#F59E0B',
    borderRadius: 8,
    paddingVertical: 10,
    paddingHorizontal: 14,
    marginBottom: 10,
    alignItems: 'center',
  },
  offlineBannerText: {
    color: '#92400E',
    fontWeight: 'bold',
    fontSize: 13,
  },
  syncStatsBox: {
    backgroundColor: '#EFF6FF',
    borderWidth: 1,
    borderColor: '#93C5FD',
    borderRadius: 8,
    paddingVertical: 10,
    paddingHorizontal: 14,
    marginBottom: 10,
  },
  syncStatsText: {
    color: '#1E40AF',
    fontSize: 13,
    fontWeight: '600',
  },
  retryFailedBtn: {
    marginTop: 6,
    backgroundColor: '#FEE2E2',
    borderWidth: 1,
    borderColor: '#FCA5A5',
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 6,
    alignSelf: 'flex-start',
  },
  retryFailedBtnText: {
    color: '#DC2626',
    fontSize: 12,
    fontWeight: 'bold',
  },
});
