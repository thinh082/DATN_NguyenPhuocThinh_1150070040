import React, { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { ErrorBanner, Icon, InputField, SelectField } from './components/MobileUi';
import { Colors } from './constants/colors';
import { ChiTietTuyen, layChiTietTuyenXe, layDanhSachTuyenXe, LocTuyen, TuyenXe } from './services/vanHanhService';
import { layLichSuLuotDi, LuotDi } from './services/veService';

const locOptions: { value: number; label: string; loc: LocTuyen }[] = [
  { value: 0, label: 'Tất cả tuyến', loc: 'tatCa' },
  { value: 1, label: 'Miễn phí theo chính sách (NQ 09/2026)', loc: 'mienPhi' },
  { value: 2, label: 'Tuyến tính phí', loc: 'tinhPhi' },
];

const normalize = (s: string) => s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/gi, 'd').toLowerCase();

export function TraCuuTuyen() {
  const [routes, setRoutes] = useState<TuyenXe[]>([]);
  const [detail, setDetail] = useState<ChiTietTuyen | null>(null);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [lastRouteId, setLastRouteId] = useState<number | null>(null);
  const [loc, setLoc] = useState(0);
  async function load() {
    setLastRouteId(null);
    try { setLoading(true); setError(''); const res = await layDanhSachTuyenXe(locOptions[loc].loc); if (res.statusCode !== 200) throw new Error(res.message); setRoutes(res.data.filter(x => x.trangThai)); }
    catch (e: any) { setError(e.message || 'Không thể tải tuyến xe.'); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, [loc]);
  const filtered = useMemo(() => routes.filter(x => normalize(`${x.maTuyen} ${x.tenTuyen}`).includes(normalize(query.trim()))), [routes, query]);
  async function open(id: number) {
    setLastRouteId(id);
    try { setLoading(true); setError(''); const res = await layChiTietTuyenXe(id); if (res.statusCode !== 200) throw new Error(res.message); setDetail(res.data); }
    catch (e: any) { setError(e.message || 'Không thể tải lộ trình.'); }
    finally { setLoading(false); }
  }
  return <View style={styles.card}>
    {detail ? <>
      <TouchableOpacity onPress={() => { setDetail(null); setLastRouteId(null); setError(''); }}><Text style={styles.link}>← Danh sách tuyến</Text></TouchableOpacity>
      <View style={styles.routeHeader}><Text style={styles.badge}>{detail.maTuyen}</Text><View style={{ flex: 1 }}><Text style={styles.title}>{detail.tenTuyen}</Text><Text style={styles.sub}>{detail.gioBatDau} – {detail.gioKetThuc}{detail.gianCachPhut != null ? ` · ${detail.gianCachPhut} phút/chuyến` : ''}{detail.cuLyKm ? ` · ${detail.cuLyKm} km` : ''}</Text><PolicyBadge route={detail} /></View></View>
      {detail.mienPhi && !!detail.tenChinhSach && <Text style={styles.sub}>Căn cứ: {detail.tenChinhSach}</Text>}
      {detail.loTrinhs.length === 0 ? <Text style={styles.empty}>Chưa có thông tin lộ trình cho tuyến này.</Text> : detail.loTrinhs.map(path => <View key={path.id} style={styles.path}>
        <Text style={styles.pathTitle}>{path.chieu === 'Di' ? 'Chiều đi' : path.chieu === 'Ve' ? 'Chiều về' : path.chieu} · {path.tenLoTrinh}</Text>
        {path.danhSachTram.length === 0 ? <Text style={styles.empty}>Chưa có trạm dừng.</Text> : path.danhSachTram.map((stop, index) => (
          <View key={`${stop.thuTu}-${stop.tramDungId}`} style={styles.stop}>
            {index < path.danhSachTram.length - 1 && <View style={styles.stopConnector} />}
            <Text style={styles.stopNumber}>{stop.thuTu}</Text>
            <View style={{ flex: 1 }}>
              <Text style={styles.stopName}>{stop.tenTram}</Text>
              {!!stop.diaChi && <Text style={styles.sub}>{stop.diaChi}</Text>}
            </View>
          </View>
        ))}
      </View>)}
    </> : <>
      <Text accessibilityRole="header" style={styles.title}>Tra cứu tuyến xe</Text>
      <InputField label="Mã hoặc tên tuyến" icon="search-outline" placeholder="Ví dụ: 01 hoặc Bến Thành" value={query} onChangeText={setQuery} />
      <SelectField label="Lọc theo giá vé" icon="pricetags-outline" placeholder="Tất cả tuyến" options={locOptions} value={loc} onChange={setLoc} />
      {!loading && <Text style={styles.sub}>{filtered.length} tuyến</Text>}
      {!loading && !error && filtered.length === 0 && <Text style={styles.empty}>{routes.length === 0 ? 'Chưa có tuyến xe hoạt động.' : 'Không tìm thấy tuyến phù hợp.'}</Text>}
      {filtered.map(route => <TouchableOpacity key={route.id} style={styles.route} onPress={() => void open(route.id)} disabled={loading}><Text style={styles.badge}>{route.maTuyen}</Text><View style={{ flex: 1 }}><Text style={styles.routeTitle}>{route.tenTuyen}</Text><Text style={styles.sub}>{route.gioBatDau} – {route.gioKetThuc}{route.gianCachPhut != null ? ` · ${route.gianCachPhut} phút/chuyến` : ''}</Text><PolicyBadge route={route} /></View><Icon name="chevron-forward-outline" size={18} /></TouchableOpacity>)}
    </>}
    {loading && <ActivityIndicator color={Colors.primary} style={{ marginVertical: 16 }} />}
    {!!error && <View style={styles.errorBox}><ErrorBanner message={error} /><TouchableOpacity onPress={() => void (lastRouteId != null ? open(lastRouteId) : load())}><Text style={styles.link}>Thử lại</Text></TouchableOpacity></View>}
  </View>;
}

function PolicyBadge({ route }: { route: TuyenXe }) {
  return <Text style={[styles.policy, route.mienPhi ? styles.policyFree : styles.policyPaid]}>{route.mienPhi ? 'Miễn phí · NQ 09/2026' : 'Tính phí'}</Text>;
}

export function LichSuChuyen() {
  const [history, setHistory] = useState<LuotDi[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  async function load() {
    try { setLoading(true); setError(''); const res = await layLichSuLuotDi(); if (res.statusCode !== 200) throw new Error(res.message); setHistory(res.data); }
    catch (e: any) { setError(e.message || 'Không thể tải lịch sử lượt đi.'); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  return <View style={styles.card}><Text accessibilityRole="header" style={styles.title}>Lịch sử lượt đi</Text>
    {loading && <ActivityIndicator color={Colors.primary} style={{ marginVertical: 16 }} />}
    {!!error && <View style={styles.errorBox}><ErrorBanner message={error} /><TouchableOpacity onPress={() => void load()}><Text style={styles.link}>Thử lại</Text></TouchableOpacity></View>}
    {!loading && !error && history.length === 0 && <Text style={styles.empty}>Bạn chưa có lượt đi nào được ghi nhận.</Text>}
    {history.map(item => (
      <View key={item.id} style={styles.history}>
        <View style={styles.routeHeader}>
          <Text style={styles.badge}>{item.maTuyen}</Text>
          <View style={{ flex: 1 }}>
            <Text style={styles.routeTitle}>{item.tenTuyen}</Text>
            <Text style={styles.sub}>Xe {item.bienSo} · {item.maLoaiVe === 'VeThang' ? 'Vé tháng' : 'Vé lượt'}</Text>
          </View>
        </View>
        <View style={styles.historyMeta}>
          <Icon name="time-outline" color={Colors.textSecondary} size={16} />
          <Text style={styles.historyTime}>{new Date(item.thoiDiemQuet).toLocaleString('vi-VN')}</Text>
          <Text style={[styles.mode, item.cheDo === 'Offline' && styles.offlineMode]}>{item.cheDo === 'Offline' ? 'Offline' : item.cheDo}</Text>
        </View>
        <View style={styles.historyMeta}>
          <Icon name={item.trangThai === 'HopLe' ? 'checkmark-circle-outline' : 'close-circle-outline'} color={item.trangThai === 'HopLe' ? Colors.successText : Colors.dangerText} size={16} />
          <Text style={item.trangThai === 'HopLe' ? styles.valid : styles.error}>{item.trangThai === 'HopLe' ? 'Đã ghi nhận' : 'Lượt đi đã hủy'}</Text>
        </View>
        {!!item.lyDoHuy && <Text style={styles.sub}>{item.lyDoHuy}</Text>}
      </View>
    ))}
  </View>;
}

const styles = StyleSheet.create({
  policy: { alignSelf: 'flex-start', marginTop: 4, paddingHorizontal: 8, paddingVertical: 2, borderRadius: 999, overflow: 'hidden', fontSize: 12, fontWeight: '700' },
  policyFree: { backgroundColor: Colors.successSurface, color: Colors.successText },
  policyPaid: { backgroundColor: Colors.warningSurface, color: Colors.warningText },
  card: { backgroundColor: Colors.surface, borderRadius: 16, padding: 16, borderWidth: 1, borderColor: Colors.border, gap: 12 },
  title: { fontSize: 19, fontWeight: '800', color: Colors.textPrimary },
  sub: { fontSize: 13, color: Colors.textSecondary, marginTop: 3 },
  route: { minHeight: 48, flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 10, borderBottomWidth: 1, borderColor: Colors.border },
  routeHeader: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  badge: { minWidth: 44, textAlign: 'center', overflow: 'hidden', padding: 10, backgroundColor: Colors.primary, color: Colors.white, borderRadius: 10, fontWeight: '700' },
  routeTitle: { fontSize: 15, fontWeight: '700', color: Colors.textPrimary },
  link: { color: Colors.primaryDark, fontWeight: '700', paddingVertical: 14 },
  path: { backgroundColor: Colors.background, borderRadius: 12, borderWidth: 1, borderColor: Colors.border, padding: 12 },
  pathTitle: { fontWeight: '700', color: Colors.primaryDark, marginBottom: 8 },
  stop: { flexDirection: 'row', alignItems: 'flex-start', gap: 10, paddingVertical: 8, minHeight: 42 },
  stopConnector: { position: 'absolute', top: 23, bottom: -12, left: 12, width: 2, backgroundColor: Colors.accent },
  stopNumber: { width: 26, height: 26, overflow: 'hidden', textAlign: 'center', textAlignVertical: 'center', lineHeight: 26, color: Colors.primaryDark, fontWeight: '700', backgroundColor: Colors.primaryLight, borderRadius: 999 },
  stopName: { fontWeight: '600', color: Colors.textPrimary },
  empty: { textAlign: 'center', paddingVertical: 20, color: Colors.textMuted, fontSize: 14 },
  errorBox: { gap: 4 },
  error: { color: Colors.dangerText, fontSize: 13, fontWeight: '600' },
  history: { padding: 12, borderWidth: 1, borderColor: Colors.border, borderRadius: 12, backgroundColor: Colors.background, gap: 8 },
  historyMeta: { flexDirection: 'row', alignItems: 'center', flexWrap: 'wrap', gap: 8 },
  historyTime: { color: Colors.textSecondary, fontSize: 13, flexGrow: 1 },
  mode: { color: Colors.primaryDark, backgroundColor: Colors.primaryLight, paddingHorizontal: 8, paddingVertical: 4, borderRadius: 999, overflow: 'hidden', fontSize: 12, fontWeight: '700' },
  offlineMode: { color: Colors.warningText, backgroundColor: Colors.warningSurface },
  valid: { color: Colors.successText, fontWeight: '600', fontSize: 13 },
});
