import React, { useEffect, useState } from 'react';
import { ActivityIndicator, Alert, StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native';
import * as DocumentPicker from 'expo-document-picker';
import Colors from './constants/colors';
import {
  capNhatThongTin,
  HoSoUuTien,
  layHoSoCuaToi,
  layNhomUuTien,
  NhomUuTien,
  nopHoSoUuTien,
  taiLenMinhChung,
  ThongTinNguoiDung,
} from './services/xacThucService';

export default function HoSoHanhKhach({ user, onUpdated }: { user: ThongTinNguoiDung; onUpdated: (user: ThongTinNguoiDung) => void }) {
  const [name, setName] = useState(user.hoTen);
  const [email, setEmail] = useState(user.email || '');
  const [phone, setPhone] = useState(user.soDienThoai || '');
  const [cccd, setCccd] = useState(user.cccd || '');
  const [groups, setGroups] = useState<NhomUuTien[]>([]);
  const [profiles, setProfiles] = useState<HoSoUuTien[]>([]);
  const [group, setGroup] = useState<number | null>(null);
  const [proof, setProof] = useState('');
  const [selectedFileName, setSelectedFileName] = useState('');
  const [start, setStart] = useState(new Date().toISOString().slice(0, 10));
  const [end, setEnd] = useState(new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10));
  const [busy, setBusy] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');

  async function reload() {
    const [g, p] = await Promise.all([layNhomUuTien(), layHoSoCuaToi()]);
    if (g.statusCode !== 200 || p.statusCode !== 200) throw new Error(g.message || p.message || 'Không tải được hồ sơ.');
    setGroups(g.data); setProfiles(p.data);
  }
  useEffect(() => { void reload().catch((e) => setError(e.message || 'Không tải được hồ sơ.')); }, []);

  async function save() {
    try {
      setBusy(true); setError('');
      const res = await capNhatThongTin({ hoTen: name.trim(), email: email.trim(), soDienThoai: phone.trim(), cccd: cccd.trim() });
      if (res.statusCode !== 200) throw new Error(res.message);
      onUpdated(res.data); Alert.alert('Thành công', 'Đã cập nhật thông tin.');
    } catch (e: any) { setError(e.message || 'Không lưu được thông tin.'); }
    finally { setBusy(false); }
  }

  async function chonTepMinhChung() {
    try {
      setError('');
      const res = await DocumentPicker.getDocumentAsync({
        type: ['image/jpeg', 'image/png', 'application/pdf'],
        copyToCacheDirectory: true,
      });
      if (res.canceled || !res.assets || res.assets.length === 0) return;
      const file = res.assets[0];
      if (file.size && file.size > 5 * 1024 * 1024) {
        setError('Kích thước tệp vượt quá 5MB. Vui lòng chọn tệp nhỏ hơn.');
        return;
      }
      setUploading(true);
      const upRes = await taiLenMinhChung({
        uri: file.uri,
        name: file.name,
        type: file.mimeType || 'image/jpeg',
      });
      if (upRes.statusCode !== 200) throw new Error(upRes.message);
      setProof(upRes.data.duongDan);
      setSelectedFileName(file.name);
      Alert.alert('Thành công', `Đã tải lên tệp: ${file.name}`);
    } catch (e: any) {
      setError(e.message || 'Không thể tải lên tệp minh chứng.');
    } finally {
      setUploading(false);
    }
  }

  async function submit() {
    if (!group || !proof.trim()) { setError('Chọn nhóm ưu tiên và cung cấp minh chứng (tải tệp hoặc nhập URL).'); return; }
    if (!/^\d{4}-\d{2}-\d{2}$/.test(start) || !/^\d{4}-\d{2}-\d{2}$/.test(end) || Number.isNaN(Date.parse(start)) || Number.isNaN(Date.parse(end))) { setError('Ngày phải theo định dạng YYYY-MM-DD.'); return; }
    try {
      setBusy(true); setError('');
      const res = await nopHoSoUuTien({ nhomDoiTuongId: group, duongDanMinhChung: proof.trim(), hieuLucTu: `${start}T00:00:00Z`, hieuLucDen: `${end}T00:00:00Z` });
      if (res.statusCode !== 200) throw new Error(res.message);
      setProof(''); setSelectedFileName(''); await reload(); Alert.alert('Đã gửi', 'Hồ sơ đang chờ xét duyệt.');
    } catch (e: any) { setError(e.message || 'Không gửi được hồ sơ.'); }
    finally { setBusy(false); }
  }

  const status: Record<string, string> = { ChoDuyet: 'Chờ duyệt', DaDuyet: 'Đã duyệt', TuChoi: 'Từ chối', ThuHoi: 'Thu hồi' };
  return <View style={styles.card}>
    <Text style={styles.heading}>Thông tin cá nhân</Text>
    {!!error && <Text style={styles.error}>{error}</Text>}
    {busy && <ActivityIndicator color={Colors.primary} />}
    <Text>Họ và tên *</Text><TextInput style={styles.input} value={name} onChangeText={setName} />
    <Text>Email</Text><TextInput style={styles.input} value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" />
    <Text>Số điện thoại</Text><TextInput style={styles.input} value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
    <Text>CCCD</Text><TextInput style={styles.input} value={cccd} onChangeText={setCccd} keyboardType="number-pad" maxLength={12} />
    <TouchableOpacity style={styles.button} disabled={busy} onPress={save}><Text style={styles.buttonText}>Lưu thông tin</Text></TouchableOpacity>
    <Text style={styles.heading}>Hồ sơ ưu tiên</Text>
    {profiles.length === 0 ? <Text>Chưa có hồ sơ ưu tiên.</Text> : profiles.map(p => <View key={p.id} style={styles.profile}>
      <Text style={styles.bold}>{p.tenNhom} — {status[p.trangThai] || p.trangThai}</Text>
      <Text>Hiệu lực: {p.hieuLucTu} đến {p.hieuLucDen}</Text>
      {!!p.duongDanMinhChung && <Text style={styles.subText}>Minh chứng: {p.duongDanMinhChung}</Text>}
      {!!p.lyDo && <Text>Lý do: {p.lyDo}</Text>}
    </View>)}
    <Text style={styles.bold}>Gửi hồ sơ mới</Text>
    <Text>Chọn nhóm ưu tiên:</Text>
    {groups.map(g => <TouchableOpacity key={g.id} style={[styles.option, group === g.id && styles.selected]} onPress={() => setGroup(g.id)}><Text>{group === g.id ? '✓ ' : ''}{g.tenNhom}</Text></TouchableOpacity>)}
    
    <Text style={{ marginTop: 8, fontWeight: '600' }}>Tài liệu minh chứng:</Text>
    <TouchableOpacity style={styles.fileButton} disabled={uploading || busy} onPress={chonTepMinhChung}>
      {uploading ? (
        <ActivityIndicator size="small" color={Colors.primary} />
      ) : (
        <Text style={styles.fileButtonText}>📁 Chọn ảnh hoặc tệp PDF minh chứng (≤ 5MB)</Text>
      )}
    </TouchableOpacity>
    {!!selectedFileName && <Text style={styles.fileSuccess}>✓ Đã chọn: {selectedFileName}</Text>}

    <Text style={{ marginTop: 4 }}>Hoặc nhập đường dẫn URL minh chứng (HTTP/HTTPS):</Text>
    <TextInput style={styles.input} value={proof} onChangeText={(txt) => { setProof(txt); setSelectedFileName(''); }} autoCapitalize="none" placeholder="https://... hoặc tự điền khi chọn tệp" />
    
    <Text>Hiệu lực từ (YYYY-MM-DD)</Text><TextInput style={styles.input} value={start} onChangeText={setStart} placeholder="2026-01-01" />
    <Text>Hiệu lực đến (YYYY-MM-DD)</Text><TextInput style={styles.input} value={end} onChangeText={setEnd} placeholder="2026-12-31" />
    <TouchableOpacity style={styles.button} disabled={busy || uploading} onPress={submit}><Text style={styles.buttonText}>Gửi hồ sơ</Text></TouchableOpacity>
  </View>;
}

const styles = StyleSheet.create({
  card: { backgroundColor: 'white', borderRadius: 16, padding: 16, gap: 8 }, heading: { fontSize: 17, fontWeight: 'bold', color: Colors.primary, marginTop: 8 },
  input: { borderWidth: 1, borderColor: Colors.border, borderRadius: 8, padding: 10 }, button: { backgroundColor: Colors.primary, borderRadius: 8, padding: 12, alignItems: 'center' },
  buttonText: { color: 'white', fontWeight: 'bold' }, error: { color: Colors.danger }, bold: { fontWeight: 'bold' }, profile: { borderBottomWidth: 1, borderColor: Colors.border, paddingVertical: 8 },
  subText: { fontSize: 12, color: Colors.textSecondary },
  option: { borderWidth: 1, borderColor: Colors.border, borderRadius: 8, padding: 10 }, selected: { borderColor: Colors.primary, backgroundColor: Colors.primaryLight },
  fileButton: { borderWidth: 1, borderColor: Colors.primary, borderStyle: 'dashed', borderRadius: 8, padding: 12, alignItems: 'center', backgroundColor: '#f0f9ff' },
  fileButtonText: { color: Colors.primary, fontWeight: '600' },
  fileSuccess: { color: '#059669', fontSize: 13, fontWeight: '500' },
});
