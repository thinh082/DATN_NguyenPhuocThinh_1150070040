import React, { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native';
import Colors from './constants/colors';
import { ChiTietTuyen, layChiTietTuyenXe, layDanhSachTuyenXe, TuyenXe } from './services/vanHanhService';
import { layLichSuLuotDi, LuotDi } from './services/veService';

const normalize = (s: string) => s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/gi, 'd').toLowerCase();

export function TraCuuTuyen() {
  const [routes, setRoutes] = useState<TuyenXe[]>([]);
  const [detail, setDetail] = useState<ChiTietTuyen | null>(null);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [lastRouteId, setLastRouteId] = useState<number | null>(null);
  async function load() {
    setLastRouteId(null);
    try { setLoading(true); setError(''); const res = await layDanhSachTuyenXe(); if (res.statusCode !== 200) throw new Error(res.message); setRoutes(res.data.filter(x => x.trangThai)); }
    catch (e: any) { setError(e.message || 'Không thể tải tuyến xe.'); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
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
      <View style={styles.routeHeader}><Text style={styles.badge}>{detail.maTuyen}</Text><View style={{ flex: 1 }}><Text style={styles.title}>{detail.tenTuyen}</Text><Text style={styles.sub}>{detail.gioBatDau} – {detail.gioKetThuc}{detail.gianCachPhut != null ? ` · ${detail.gianCachPhut} phút/chuyến` : ''}</Text></View></View>
      {detail.loTrinhs.length === 0 ? <Text style={styles.empty}>Chưa có thông tin lộ trình cho tuyến này.</Text> : detail.loTrinhs.map(path => <View key={path.id} style={styles.path}>
        <Text style={styles.pathTitle}>{path.chieu === 'Di' ? 'Chiều đi' : path.chieu === 'Ve' ? 'Chiều về' : path.chieu} · {path.tenLoTrinh}</Text>
        {path.danhSachTram.length === 0 ? <Text style={styles.empty}>Chưa có trạm dừng.</Text> : path.danhSachTram.map(stop => <View key={`${stop.thuTu}-${stop.tramDungId}`} style={styles.stop}><Text style={styles.stopNumber}>{stop.thuTu}</Text><View style={{ flex: 1 }}><Text style={styles.stopName}>{stop.tenTram}</Text>{!!stop.diaChi && <Text style={styles.sub}>{stop.diaChi}</Text>}</View></View>)}
      </View>)}
    </> : <>
      <Text style={styles.title}>Tra cứu tuyến xe</Text><Text style={styles.sub}>Tìm theo mã hoặc tên tuyến.</Text>
      <TextInput accessibilityLabel="Tìm tuyến xe" style={styles.search} placeholder="Ví dụ: 01 hoặc Bến Thành" value={query} onChangeText={setQuery} />
      {!loading && !error && filtered.length === 0 && <Text style={styles.empty}>{routes.length === 0 ? 'Chưa có tuyến xe hoạt động.' : 'Không tìm thấy tuyến phù hợp.'}</Text>}
      {filtered.map(route => <TouchableOpacity key={route.id} style={styles.route} onPress={() => void open(route.id)} disabled={loading}><Text style={styles.badge}>{route.maTuyen}</Text><View style={{ flex: 1 }}><Text style={styles.routeTitle}>{route.tenTuyen}</Text><Text style={styles.sub}>{route.gioBatDau} – {route.gioKetThuc}{route.gianCachPhut != null ? ` · ${route.gianCachPhut} phút/chuyến` : ''}</Text></View><Text style={styles.arrow}>›</Text></TouchableOpacity>)}
    </>}
    {loading && <ActivityIndicator color={Colors.primary} style={{ marginVertical: 16 }} />}
    {!!error && <View style={styles.errorBox}><Text style={styles.error}>{error}</Text><TouchableOpacity onPress={() => void (lastRouteId != null ? open(lastRouteId) : load())}><Text style={styles.link}>Thử lại</Text></TouchableOpacity></View>}
  </View>;
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
  return <View style={styles.card}><Text style={styles.title}>Lịch sử lượt đi</Text><Text style={styles.sub}>Tối đa 100 lượt gần nhất đã được hệ thống ghi nhận.</Text>
    {loading && <ActivityIndicator color={Colors.primary} style={{ marginVertical: 16 }} />}
    {!!error && <View style={styles.errorBox}><Text style={styles.error}>{error}</Text><TouchableOpacity onPress={() => void load()}><Text style={styles.link}>Thử lại</Text></TouchableOpacity></View>}
    {!loading && !error && history.length === 0 && <Text style={styles.empty}>Bạn chưa có lượt đi nào được ghi nhận.</Text>}
    {history.map(item => <View key={item.id} style={styles.history}><View style={styles.routeHeader}><Text style={styles.badge}>{item.maTuyen}</Text><View style={{ flex: 1 }}><Text style={styles.routeTitle}>{item.tenTuyen}</Text><Text style={styles.sub}>Xe {item.bienSo} · {item.maLoaiVe === 'VeThang' ? 'Vé tháng' : 'Vé lượt'}</Text></View></View><Text style={styles.historyTime}>{new Date(item.thoiDiemQuet).toLocaleString('vi-VN')}</Text><Text style={item.trangThai === 'HopLe' ? styles.valid : styles.error}>{item.trangThai === 'HopLe' ? '✓ Đã ghi nhận' : 'Lượt đi đã hủy'}{item.cheDo === 'Offline' ? ' · Soát ngoại tuyến' : ''}</Text>{!!item.lyDoHuy && <Text style={styles.sub}>{item.lyDoHuy}</Text>}</View>)}
  </View>;
}

const styles = StyleSheet.create({
  card: { backgroundColor: 'white', borderRadius: 16, padding: 16, borderWidth: 1, borderColor: Colors.border, gap: 10 },
  title: { fontSize: 17, fontWeight: '700', color: Colors.textPrimary }, sub: { fontSize: 12, color: Colors.textSecondary, marginTop: 4 },
  search: { borderWidth: 1, borderColor: Colors.border, borderRadius: 10, padding: 12, marginTop: 5 }, route: { flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 12, borderBottomWidth: 1, borderColor: Colors.border },
  routeHeader: { flexDirection: 'row', alignItems: 'center', gap: 12 }, badge: { minWidth: 43, textAlign: 'center', overflow: 'hidden', padding: 10, backgroundColor: Colors.primary, color: 'white', borderRadius: 10, fontWeight: '700' },
  routeTitle: { fontSize: 14, fontWeight: '700', color: Colors.textPrimary }, arrow: { color: Colors.primary, fontSize: 24 }, link: { color: Colors.primary, fontWeight: '700', paddingVertical: 5 },
  path: { marginTop: 12 }, pathTitle: { fontWeight: '700', color: Colors.primary, marginBottom: 8 }, stop: { flexDirection: 'row', alignItems: 'flex-start', gap: 10, paddingVertical: 10, borderBottomWidth: 1, borderColor: Colors.border },
  stopNumber: { width: 26, height: 26, textAlign: 'center', textAlignVertical: 'center', color: Colors.primary, fontWeight: '700', backgroundColor: Colors.primaryLight, borderRadius: 13 }, stopName: { fontWeight: '600', color: Colors.textPrimary },
  empty: { textAlign: 'center', paddingVertical: 20, color: Colors.textMuted, fontSize: 13 }, errorBox: { gap: 4, paddingVertical: 10 }, error: { color: Colors.danger, fontSize: 12 },
  history: { paddingVertical: 14, borderBottomWidth: 1, borderColor: Colors.border, gap: 7 }, historyTime: { color: Colors.textSecondary, fontSize: 12 }, valid: { color: Colors.success, fontWeight: '600', fontSize: 12 },
});
