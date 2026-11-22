import React, { useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Modal,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import { useAudioPlayer } from 'expo-audio';
import { StatusBar } from 'expo-status-bar';
import * as Crypto from 'expo-crypto';
import type { SQLiteDatabase } from 'expo-sqlite';
import { Colors } from './constants/colors';
import { dienGiaiLyDo } from './constants/lyDoSoatVe';
import { useKetNoiMayChu } from './services/ketNoi';
import { headerInset, Icon } from './components/MobileUi';
import { apiClient } from './services/api';
import { docGoi, dongBoNhatKy, luuGoi, moKho, quetVaLuu, layThongKeDongBo, datLaiSuKienLoi } from './services/khoSoatVe';
import type { GoiKiemTra } from './services/soatVeOffline.mjs';

type Device = { id: number; tenThietBi: string };
type Trip = { id: number; tuyenXeId: number; maTuyen?: string; tenTuyen: string; bienSo: string };
type DropdownOption = { id: number | null; title: string; badge?: string; detail?: string };

const allRoutes = { id: null, title: 'Tất cả tuyến đang chạy' };
const normalizeSearch = (value: string) =>
  value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/gi, 'd').toLowerCase();

function SearchableDropdown({ label, placeholder, searchLabel, emptyLabel, options, value, disabled, onSelect }: {
  label: string;
  placeholder: string;
  searchLabel: string;
  emptyLabel: string;
  options: DropdownOption[];
  value: number | null;
  disabled: boolean;
  onSelect: (id: number | null) => void;
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const selected = options.find(option => option.id === value);
  const keyword = normalizeSearch(query.trim());
  const matches = options.filter(option =>
    normalizeSearch([option.badge, option.title, option.detail].join(' ')).includes(keyword)
  );

  return (
    <View style={styles.selector}>
      <Text style={styles.label}>{label}</Text>
      <TouchableOpacity
        accessibilityRole="button"
        accessibilityLabel={label}
        accessibilityValue={{ text: selected ? [selected.badge, selected.title, selected.detail].filter(Boolean).join(', ') : placeholder }}
        accessibilityState={{ disabled, expanded: open }}
        disabled={disabled}
        style={[styles.selectorButton, disabled && styles.disabled]}
        onPress={() => { setQuery(''); setOpen(true); }}
      >
        <View style={styles.optionContent}>
          {selected?.badge && <Text style={styles.routeBadge}>{selected.badge}</Text>}
          <Text style={selected ? styles.optionTitle : styles.optionDetail}>{selected?.title ?? placeholder}</Text>
          {selected?.detail && <Text style={styles.optionDetail}>{selected.detail}</Text>}
        </View>
        <Icon name="chevron-down" size={18} />
      </TouchableOpacity>

      <Modal visible={open} animationType="fade" transparent onRequestClose={() => setOpen(false)}>
        <KeyboardAvoidingView style={styles.modalOverlay} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <View style={[styles.modalContent, styles.selectorModal]} accessibilityViewIsModal>
            <View style={styles.modalHeader}>
              <Text accessibilityRole="header" style={styles.modalTitle}>{label}</Text>
              <TouchableOpacity accessibilityRole="button" accessibilityLabel="Đóng danh sách" style={styles.iconButton} onPress={() => setOpen(false)}>
                <Icon name="close" />
              </TouchableOpacity>
            </View>
            <TextInput
              accessibilityLabel={searchLabel}
              placeholder={searchLabel}
              placeholderTextColor={Colors.textMuted}
              value={query}
              onChangeText={setQuery}
              autoCorrect={false}
              returnKeyType="search"
              style={styles.searchInput}
            />
            <FlatList
              style={styles.optionList}
              data={matches}
              keyExtractor={option => option.id === null ? 'all' : String(option.id)}
              keyboardShouldPersistTaps="handled"
              keyboardDismissMode="on-drag"
              ListEmptyComponent={<Text accessibilityLiveRegion="polite" style={[styles.emptyNoticeText, styles.optionEmpty]}>{keyword ? 'Không tìm thấy kết quả.' : emptyLabel}</Text>}
              renderItem={({ item }) => {
                const checked = item.id === value;
                return (
                  <TouchableOpacity
                    accessibilityRole="radio"
                    accessibilityState={{ checked }}
                    style={[styles.selectorButton, styles.optionRow, checked && styles.optionSelected]}
                    onPress={() => { onSelect(item.id); setOpen(false); }}
                  >
                    <View style={styles.optionContent}>
                      {item.badge && <Text style={styles.routeBadge}>{item.badge}</Text>}
                      <Text style={styles.optionTitle}>{item.title}</Text>
                      {item.detail && <Text style={styles.optionDetail}>{item.detail}</Text>}
                    </View>
                    <Icon name={checked ? 'radio-button-on' : 'radio-button-off'} size={18} />
                  </TouchableOpacity>
                );
              }}
            />
          </View>
        </KeyboardAvoidingView>
      </Modal>
    </View>
  );
}

export default function SoatVeScreen({ userId, onLogout, isOffline: phienNgoaiTuyen }: { userId: number; onLogout: () => void; isOffline?: boolean }) {
  const mayChuTrucTuyen = useKetNoiMayChu();
  const isOffline = !!phienNgoaiTuyen || mayChuTrucTuyen === false;
  const [permission, requestPermission] = useCameraPermissions();
  const [db, setDb] = useState<SQLiteDatabase | null>(null);
  const [storageError, setStorageError] = useState<string | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [trips, setTrips] = useState<Trip[]>([]);
  const [device, setDevice] = useState<number | null>(null);
  const [route, setRoute] = useState<number | null>(null);
  const [trip, setTrip] = useState<number | null>(null);
  const [pack, setPack] = useState<GoiKiemTra | null>(null);
  const [message, setMessage] = useState('Tải dữ liệu khi có mạng trước khi quét.');
  const [scanning, setScanning] = useState(false);
  const [busy, setBusy] = useState(false);
  const [syncStats, setSyncStats] = useState<{ choXuLy: number; biLoi: number; daDongBo: number } | null>(null);
  const screenScroll = useRef<React.ComponentRef<typeof ScrollView>>(null);
  const lock = useRef(false);
  const dangTuDongBo = useRef(false);
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
          setStorageError(null);
          setPack(saved);
          if (stats) setSyncStats(stats);
        } else {
          await opened.closeAsync();
        }
      } catch (e: any) {
        if (live) setStorageError(e.message || 'Không mở được kho ngoại tuyến.');
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
      const { thietBis, chuyenXes }: { thietBis: Device[]; chuyenXes: Trip[] } = res.data.data;
      setDevices(thietBis);
      setTrips(chuyenXes);
      setDevice(current => thietBis.some(item => item.id === current) ? current : null);
      setRoute(current => chuyenXes.some(item => item.tuyenXeId === current) ? current : null);
      setTrip(current => chuyenXes.some(item => item.id === current && (route === null || item.tuyenXeId === route)) ? current : null);
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
      setMessage(result.hopLe ? 'Vé hợp lệ — mời hành khách lên xe.' : `Từ chối: ${dienGiaiLyDo(result.maLyDo)}`);
      const player = result.hopLe ? successSound : failureSound;
      try {
        await player.seekTo(0);
        player.play();
      } catch {
        /* Ignore sound playback errors */
      }
      const stats = await layThongKeDongBo(db).catch(() => null);
      if (stats) setSyncStats(stats);
      void tuDongBo();
    } catch (e: any) {
      setMessage(e.message || 'Không thể lưu kết quả quét.');
    } finally {
      lock.current = false;
    }
  }

  async function tuDongBo() {
    if (!db || isOffline || dangTuDongBo.current) return;
    dangTuDongBo.current = true;
    try {
      const result = await dongBoNhatKy(db);
      const canhBao = [
        result.rejected ? `${result.rejected} lượt máy chủ từ chối, cần đối soát.` : '',
        result.warnings ? `${result.warnings} lượt có cảnh báo thu hồi.` : '',
        result.pendingErrors ? `${result.pendingErrors} lượt bị lỗi chưa xác nhận.` : '',
      ].filter(Boolean).join(' ');
      if (canhBao) setMessage((m) => `${m} ${canhBao}`);
    } catch {
      /* Giữ lượt quét trong hàng đợi, sẽ gửi lại khi có mạng */
    } finally {
      dangTuDongBo.current = false;
      const stats = await layThongKeDongBo(db).catch(() => null);
      if (stats) setSyncStats(stats);
    }
  }

  useEffect(() => {
    if (db && !isOffline) void tuDongBo();
  }, [db, isOffline]);

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

  const accepted = message.startsWith('Vé hợp lệ');
  const rejected = message.startsWith('Từ chối:');
  const routes = new Map<number, DropdownOption>();
  for (const item of trips) {
    routes.set(item.tuyenXeId, {
      id: item.tuyenXeId,
      title: item.tenTuyen,
      badge: item.maTuyen ? `Tuyến ${item.maTuyen}` : undefined,
    });
  }
  const visibleTrips = trips.filter(item => route === null || item.tuyenXeId === route);

  useEffect(() => {
    if (!scanning && (accepted || rejected)) screenScroll.current?.scrollTo({ y: 0, animated: false });
  }, [accepted, rejected, scanning]);

  return (
    <ScrollView ref={screenScroll} style={styles.screen} contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
      <StatusBar style="dark" />
      <View style={styles.heading}>
        <View style={styles.headingIcon}><Icon name="scan-outline" size={24} /></View>
        <Text accessibilityRole="header" style={styles.screenTitle}>Soát vé SmartBus</Text>
      </View>
      <View style={[styles.connection, isOffline && styles.connectionOffline]}>
        <Icon name={isOffline ? 'cloud-offline-outline' : 'wifi-outline'} color={isOffline ? Colors.warningText : Colors.successText} size={18} />
        <Text style={[styles.connectionText, isOffline && { color: Colors.warningText }]}>
          {isOffline ? 'Chế độ ngoại tuyến' : 'Đang trực tuyến'}
        </Text>
      </View>

      <View accessibilityLiveRegion="polite" style={[styles.statusBar, accepted && styles.accepted, rejected && styles.rejected]}>
        <Icon name={accepted ? 'checkmark-circle' : rejected ? 'close-circle' : 'information-circle-outline'} size={accepted || rejected ? 36 : 22} color={accepted ? Colors.successText : rejected ? Colors.dangerText : Colors.primaryDark} />
        <View style={styles.statusContent}>
          <Text style={[styles.statusText, accepted && { color: Colors.successText }, rejected && { color: Colors.dangerText }]}>{message}</Text>
        </View>
      </View>

      {syncStats && (
        <View style={styles.syncStatsBox}>
          <View style={styles.heading}>
            <Icon name="cloud-upload-outline" />
            <Text style={styles.cardTitle}>Hàng đợi đồng bộ</Text>
          </View>
          <Text style={styles.syncStatsText}>
            {syncStats.choXuLy} chờ gửi · {syncStats.biLoi} lỗi
          </Text>
          {!!pack && (
            <TouchableOpacity accessibilityRole="button" style={[styles.btnPrimary, (!db || busy || scanning) && styles.disabled]} disabled={!db || busy || scanning} onPress={sync}>
              <Icon name="sync-outline" size={18} color={Colors.white} />
              <Text style={styles.btnPrimaryText}>Đồng bộ ngay</Text>
            </TouchableOpacity>
          )}
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

      {busy && <ActivityIndicator color={Colors.primary} />}

      {!pack && (
        <View style={styles.card}>
          <TouchableOpacity accessibilityRole="button" style={[styles.btnSecondary, busy && styles.disabled]} onPress={loadOptions} disabled={busy}>
            <Text style={styles.btnSecondaryText}>Tải danh sách thiết bị và chuyến</Text>
          </TouchableOpacity>

          <SearchableDropdown
            label="Thiết bị soát vé"
            placeholder="Chọn thiết bị"
            searchLabel="Tìm theo tên thiết bị"
            emptyLabel="Chưa có thiết bị trong danh sách."
            options={devices.map(item => ({ id: item.id, title: item.tenThietBi }))}
            value={device}
            disabled={busy}
            onSelect={setDevice}
          />
          <SearchableDropdown
            label="Lọc theo tuyến xe"
            placeholder={allRoutes.title}
            searchLabel="Tìm theo mã hoặc tên tuyến"
            emptyLabel="Chưa có tuyến đang chạy."
            options={[allRoutes, ...routes.values()]}
            value={route}
            disabled={busy}
            onSelect={id => {
              if (id === route) return;
              setRoute(id);
              setTrip(null);
            }}
          />
          <SearchableDropdown
            label="Chuyến xe đang chạy"
            placeholder="Chọn chuyến xe"
            searchLabel="Tìm theo biển số xe"
            emptyLabel="Chưa có chuyến đang chạy."
            options={visibleTrips.map(item => ({
              id: item.id,
              title: item.tenTuyen,
              badge: routes.get(item.tuyenXeId)?.badge,
              detail: `Biển số xe: ${item.bienSo}`,
            }))}
            value={trip}
            disabled={busy}
            onSelect={setTrip}
          />

          <TouchableOpacity
            style={[styles.btnPrimary, { marginTop: 16 }, (busy || !device || !trip || !db) && styles.disabled]}
            onPress={prepare}
            disabled={busy || !device || !trip || !db}
          >
            <Text style={styles.btnPrimaryText}>Bắt đầu phiên & tải gói offline</Text>
          </TouchableOpacity>
          {storageError && (
            <Text accessibilityLiveRegion="polite" style={styles.cardSubtitle}>{storageError}</Text>
          )}
        </View>
      )}

      {pack && (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>Phiên Soát Vé Đang Hoạt Động</Text>
          <Text style={styles.cardSubtitle}>
            Gói kiểm tra: {pack.ves.length} vé • Hạn dùng: {new Date(pack.hetHanLuc).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' })}
          </Text>

          <View style={styles.actionGrid}>
            {!permission?.granted && (
              <TouchableOpacity style={styles.btnSecondary} onPress={() => void requestPermission()}>
                <Text style={styles.btnSecondaryText}>Cấp quyền camera</Text>
              </TouchableOpacity>
            )}

            <TouchableOpacity
              accessibilityRole="button"
              style={[styles.btnPrimary, styles.scanButton, (!db || !permission?.granted || scanning) && styles.disabled]}
              disabled={!pack || !db || !permission?.granted || scanning}
              onPress={() => setScanning(true)}
            >
              <Icon name="camera-outline" color={Colors.white} size={22} />
              <Text style={styles.btnPrimaryText}>Mở camera quét vé</Text>
            </TouchableOpacity>

            <TouchableOpacity
              accessibilityRole="button"
              style={styles.btnSecondary}
              disabled={!pack || busy || scanning}
              onPress={() => { setLookupModal(true); setLookupResult(null); }}
            >
              <Icon name="search-outline" size={18} />
              <Text style={styles.btnSecondaryText}>Tra cứu khẩn cấp (CCCD/SĐT)</Text>
            </TouchableOpacity>

            {!syncStats && <TouchableOpacity
              accessibilityRole="button"
              style={[styles.btnSecondary]}
              disabled={!db || busy || scanning}
              onPress={sync}
            >
              <Icon name="sync-outline" size={18} />
              <Text style={styles.btnSecondaryText}>Đồng bộ lượt quét</Text>
            </TouchableOpacity>}

            <TouchableOpacity
              style={[styles.btnDanger]}
              disabled={!pack || busy || scanning}
              onPress={endSession}
            >
              <Icon name="stop-circle-outline" color={Colors.dangerText} size={18} />
              <Text style={styles.btnDangerText}>Kết thúc phiên</Text>
            </TouchableOpacity>
          </View>
        </View>
      )}

      {scanning && (
        <View style={styles.cameraBox} onLayout={() => screenScroll.current?.scrollToEnd({ animated: false })}>
          <CameraView
            style={StyleSheet.absoluteFill}
            barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
            onBarcodeScanned={({ data }) => void onScan(data)}
          />
          <TouchableOpacity style={styles.closeCamBtn} onPress={() => setScanning(false)}>
            <Icon name="close" color={Colors.white} size={16} />
            <Text style={styles.closeCamText}>Tắt camera</Text>
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
              <TouchableOpacity accessibilityRole="button" accessibilityLabel="Đóng tra cứu" style={styles.iconButton} onPress={() => setLookupModal(false)}>
                <Icon name="close" color={Colors.textSecondary} />
              </TouchableOpacity>
            </View>
            <Text style={styles.modalSubtitle}>
              Áp dụng khi hành khách hết pin hoặc không có smartphone. Nhập số CCCD (12 số) hoặc số điện thoại.
            </Text>

            <View style={styles.searchRow}>
              <View style={styles.lookupField}>
                <TextInput
                  accessibilityLabel="Số CCCD hoặc số điện thoại"
                  style={styles.lookupInput}
                  placeholder="CCCD hoặc SĐT"
                  placeholderTextColor={Colors.textMuted}
                  value={lookupKeyword}
                  onChangeText={setLookupKeyword}
                  keyboardType="numeric"
                />
                {!!lookupKeyword && (
                  <TouchableOpacity accessibilityRole="button" accessibilityLabel="Xóa nội dung tìm kiếm" style={styles.iconButton} onPress={() => setLookupKeyword('')}>
                    <Icon name="close-circle-outline" color={Colors.textSecondary} size={19} />
                  </TouchableOpacity>
                )}
              </View>
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
                      <View style={styles.heading}>
                        <Icon name="person-outline" size={18} />
                        <Text style={styles.passengerName}>{lookupResult.thongTinKhach?.hoTen}</Text>
                      </View>
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
                            {v.lyDo && <Text style={styles.ticketReason}>{v.lyDo}</Text>}
                          </View>

                          <View style={styles.ticketActions}>
                            {v.coTheSuDung && (
                              <TouchableOpacity
                                style={styles.confirmBtn}
                                onPress={() => xuLyXacNhan(v.maVe)}
                                disabled={lookupLoading}
                              >
                                <Icon name="checkmark" color={Colors.white} size={16} />
                                <Text style={styles.confirmBtnText}>Xác nhận lên xe</Text>
                              </TouchableOpacity>
                            )}

                            {v.trangThai === 'HoatDong' && (
                              <TouchableOpacity
                                style={styles.lockBtn}
                                onPress={() => { setTicketToLock(v.maVe); setLockReason(''); setLockModal(true); }}
                                disabled={lookupLoading}
                              >
                                <Icon name="lock-closed-outline" color={Colors.dangerText} size={16} />
                                <Text style={styles.lockBtnText}>Tạm khóa vé</Text>
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
              accessibilityLabel="Lý do khóa vé"
              placeholder="Ví dụ: Nghi ngờ mượn vé tháng của người khác..."
              placeholderTextColor={Colors.textMuted}
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

const centeredAction = {
  minHeight: 48,
  flexDirection: 'row',
  gap: 8,
  alignItems: 'center',
  justifyContent: 'center',
  paddingHorizontal: 12,
  paddingVertical: 8,
  borderRadius: 12,
} as const;

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.background },
  container: { width: '100%', maxWidth: 760, alignSelf: 'center', padding: 16, paddingTop: headerInset + 16, paddingBottom: 48, gap: 12 },
  heading: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  headingIcon: { height: 44, width: 44, backgroundColor: Colors.primaryLight, borderRadius: 12, alignItems: 'center', justifyContent: 'center' },
  screenTitle: { flex: 1, fontSize: 23, fontWeight: '800', color: Colors.textPrimary },
  connection: { alignSelf: 'flex-start', flexDirection: 'row', gap: 8, alignItems: 'center', paddingVertical: 6, paddingHorizontal: 10, borderRadius: 999, backgroundColor: Colors.successSurface },
  connectionOffline: { backgroundColor: Colors.warningSurface },
  connectionText: { color: Colors.successText, fontSize: 13, fontWeight: '700' },
  statusBar: { flexDirection: 'row', alignItems: 'center', gap: 12, backgroundColor: Colors.primaryLight, padding: 14, borderRadius: 16, borderLeftWidth: 4, borderLeftColor: Colors.primary },
  accepted: { backgroundColor: Colors.successSurface, borderLeftColor: Colors.success },
  rejected: { backgroundColor: Colors.dangerSurface, borderLeftColor: Colors.danger },
  statusContent: { flex: 1 },
  statusText: { fontSize: 15, color: Colors.primaryDark, fontWeight: '700' },
  card: { backgroundColor: Colors.surface, borderRadius: 16, padding: 16, borderWidth: 1, borderColor: Colors.border, elevation: 1 },
  cardTitle: { flexShrink: 1, fontSize: 17, fontWeight: '800', color: Colors.textPrimary },
  cardSubtitle: { fontSize: 13, color: Colors.textSecondary, marginTop: 4 },
  label: { fontSize: 14, fontWeight: '700', color: Colors.textPrimary },
  actionGrid: { marginTop: 12, gap: 8 },
  selector: { marginTop: 12, gap: 6 },
  selectorButton: { flexDirection: 'row', alignItems: 'center', gap: 12, minHeight: 48, padding: 12, borderRadius: 12, borderWidth: 1, borderColor: Colors.border, backgroundColor: Colors.background },
  selectorModal: { gap: 8, maxHeight: '85%' },
  optionList: { flexGrow: 0, flexShrink: 1 },
  optionRow: { marginTop: 8, backgroundColor: Colors.surface },
  optionSelected: { borderColor: Colors.primary, backgroundColor: Colors.primaryLight },
  optionContent: { flex: 1, gap: 4 },
  optionTitle: { fontSize: 15, fontWeight: '700', color: Colors.textPrimary },
  optionDetail: { fontSize: 14, color: Colors.textSecondary },
  optionEmpty: { paddingVertical: 16, textAlign: 'center' },
  routeBadge: { alignSelf: 'flex-start', borderRadius: 8, paddingVertical: 3, paddingHorizontal: 8, backgroundColor: Colors.primaryLight, color: Colors.primaryDark, fontSize: 12, fontWeight: '700' },
  btnPrimary: { ...centeredAction, backgroundColor: Colors.primary },
  btnPrimaryText: { flexShrink: 1, textAlign: 'center', color: Colors.white, fontWeight: '700', fontSize: 14 },
  scanButton: { minHeight: 54 },
  btnSecondary: { ...centeredAction, backgroundColor: Colors.primaryLight, borderWidth: 1, borderColor: Colors.border },
  btnSecondaryText: { flexShrink: 1, textAlign: 'center', color: Colors.primaryDark, fontWeight: '700', fontSize: 14 },
  btnDanger: { ...centeredAction, backgroundColor: Colors.dangerSurface, borderWidth: 1, borderColor: Colors.danger },
  btnDangerText: { flexShrink: 1, textAlign: 'center', color: Colors.dangerText, fontWeight: '700', fontSize: 14 },
  disabled: { opacity: 0.52 },
  cameraBox: { height: 300, borderRadius: 16, overflow: 'hidden', marginTop: 8 },
  closeCamBtn: { ...centeredAction, position: 'absolute', top: 12, right: 12, backgroundColor: Colors.scrim, borderRadius: 999 },
  closeCamText: { color: Colors.white, fontSize: 13, fontWeight: '700' },
  btnLogout: { ...centeredAction, alignSelf: 'center' },
  btnLogoutText: { color: Colors.textSecondary, fontSize: 14, fontWeight: '700' },
  modalOverlay: { flex: 1, backgroundColor: Colors.scrim, justifyContent: 'center', alignItems: 'center', padding: 16 },
  modalContent: { backgroundColor: Colors.surface, borderRadius: 16, padding: 16, width: '100%', maxWidth: 560, maxHeight: '90%' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8 },
  modalTitle: { flexShrink: 1, fontSize: 18, fontWeight: '800', color: Colors.textPrimary },
  modalSubtitle: { fontSize: 13, color: Colors.textSecondary, marginTop: 4, lineHeight: 19 },
  iconButton: { height: 48, width: 48, justifyContent: 'center', alignItems: 'center', borderRadius: 12 },
  searchRow: { flexDirection: 'row', gap: 8, marginTop: 12 },
  lookupField: { flex: 1, flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.background, borderWidth: 1, borderColor: Colors.border, borderRadius: 12 },
  lookupInput: { flex: 1, minHeight: 48, minWidth: 0, paddingHorizontal: 10, color: Colors.textPrimary, fontSize: 15 },
  searchInput: { minHeight: 48, backgroundColor: Colors.background, borderWidth: 1, borderColor: Colors.border, borderRadius: 12, padding: 10, color: Colors.textPrimary, fontSize: 15 },
  searchBtn: { ...centeredAction, backgroundColor: Colors.primary },
  searchBtnText: { color: Colors.white, fontWeight: '700' },
  passengerBox: { backgroundColor: Colors.primaryLight, borderRadius: 12, padding: 12 },
  passengerName: { flexShrink: 1, fontSize: 16, fontWeight: '800', color: Colors.textPrimary },
  passengerMeta: { fontSize: 13, color: Colors.textSecondary, marginTop: 4 },
  ticketItem: { backgroundColor: Colors.surface, borderRadius: 12, padding: 12, borderWidth: 1, borderColor: Colors.border, marginBottom: 8, gap: 8 },
  ticketItemDisabled: { backgroundColor: Colors.background },
  ticketType: { fontSize: 14, fontWeight: '700', color: Colors.textPrimary },
  ticketRoute: { fontSize: 13, color: Colors.textSecondary, marginTop: 2 },
  ticketExpiry: { fontSize: 12, color: Colors.textMuted, marginTop: 2 },
  ticketReason: { fontSize: 12, color: Colors.dangerText, marginTop: 4, fontWeight: '600' },
  ticketActions: { gap: 8 },
  confirmBtn: { ...centeredAction, backgroundColor: Colors.primary },
  confirmBtnText: { flexShrink: 1, color: Colors.white, fontSize: 13, fontWeight: '700' },
  lockBtn: { ...centeredAction, backgroundColor: Colors.dangerSurface, borderWidth: 1, borderColor: Colors.danger },
  lockBtnText: { flexShrink: 1, color: Colors.dangerText, fontSize: 13, fontWeight: '700' },
  emptyNoticeBox: { padding: 16, alignItems: 'center' },
  emptyNoticeText: { fontSize: 14, color: Colors.textMuted },
  emptyNotice: { fontSize: 13, color: Colors.textMuted },
  closeBtn: { ...centeredAction, backgroundColor: Colors.grayLight, marginTop: 12 },
  closeBtnText: { color: Colors.textSecondary, fontWeight: '700', fontSize: 14 },
  syncStatsBox: { backgroundColor: Colors.surface, borderWidth: 1, borderColor: Colors.border, borderRadius: 16, padding: 14, gap: 8 },
  syncStatsText: { color: Colors.textSecondary, fontSize: 14, fontWeight: '600' },
  retryFailedBtn: { ...centeredAction, alignSelf: 'flex-start', backgroundColor: Colors.dangerSurface, borderWidth: 1, borderColor: Colors.danger },
  retryFailedBtnText: { flexShrink: 1, color: Colors.dangerText, fontSize: 13, fontWeight: '700' },
});
