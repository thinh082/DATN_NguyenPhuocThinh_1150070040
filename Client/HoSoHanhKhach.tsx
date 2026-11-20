import React, { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Alert, Keyboard, ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import * as DocumentPicker from 'expo-document-picker';
import { ErrorBanner, Icon, InputField } from './components/MobileUi';
import { Colors } from './constants/colors';
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

const stepTitles = ['Thông tin cá nhân', 'Hồ sơ ưu tiên', 'Nhóm và minh chứng', 'Thời hạn hồ sơ'];
const profileStatuses = {
  ChoDuyet: { label: 'Chờ duyệt', background: Colors.warningSurface, color: Colors.warningText },
  DaDuyet: { label: 'Đã duyệt', background: Colors.successSurface, color: Colors.successText },
  TuChoi: { label: 'Từ chối', background: Colors.dangerSurface, color: Colors.dangerText },
  ThuHoi: { label: 'Thu hồi', background: Colors.grayLight, color: Colors.textSecondary },
};
const proofStep = 2;
const lastStep = stepTitles.length - 1;

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
  const [step, setStep] = useState(0);
  const [pageWidth, setPageWidth] = useState(0);
  const pager = useRef<React.ComponentRef<typeof ScrollView>>(null);

  function goToStep(next: number) {
    Keyboard.dismiss();
    pager.current?.scrollTo({ x: next * pageWidth, animated: true });
  }

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
    if (!group || !proof.trim()) {
      setError('Chọn nhóm ưu tiên và cung cấp minh chứng (tải tệp hoặc nhập URL).');
      goToStep(proofStep);
      return;
    }
    if (!/^\d{4}-\d{2}-\d{2}$/.test(start) || !/^\d{4}-\d{2}-\d{2}$/.test(end) || Number.isNaN(Date.parse(start)) || Number.isNaN(Date.parse(end))) { setError('Ngày phải theo định dạng YYYY-MM-DD.'); return; }
    try {
      setBusy(true); setError('');
      const res = await nopHoSoUuTien({ nhomDoiTuongId: group, duongDanMinhChung: proof.trim(), hieuLucTu: `${start}T00:00:00Z`, hieuLucDen: `${end}T00:00:00Z` });
      if (res.statusCode !== 200) throw new Error(res.message);
      setProof(''); setSelectedFileName(''); await reload();
      goToStep(1);
      Alert.alert('Đã gửi', 'Hồ sơ đang chờ xét duyệt.');
    } catch (e: any) { setError(e.message || 'Không gửi được hồ sơ.'); }
    finally { setBusy(false); }
  }

  const pages = [
    <>
      <InputField label="Họ và tên *" icon="person-outline" value={name} onChangeText={setName} />
      <InputField label="Email" icon="mail-outline" value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" />
      <InputField label="Số điện thoại" icon="call-outline" value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
      <InputField label="CCCD" icon="card-outline" value={cccd} onChangeText={setCccd} keyboardType="number-pad" maxLength={12} />
      <TouchableOpacity style={styles.button} disabled={busy} onPress={save}><Text style={styles.buttonText}>Lưu thông tin</Text></TouchableOpacity>
    </>,
    <>
      {profiles.length === 0 ? <Text style={styles.subText}>Chưa có hồ sơ ưu tiên.</Text> : profiles.map(p => {
        const status = profileStatuses[p.trangThai as keyof typeof profileStatuses];
        return (
          <View key={p.id} style={styles.profile}>
            <Text style={styles.bold}>{p.tenNhom}</Text>
            <Text style={[styles.badge, status && { backgroundColor: status.background, color: status.color }]}>
              {status?.label ?? p.trangThai}
            </Text>
            <Text style={styles.subText}>Hiệu lực: {p.hieuLucTu} đến {p.hieuLucDen}</Text>
            {!!p.duongDanMinhChung && <Text style={styles.subText}>Minh chứng: {p.duongDanMinhChung}</Text>}
            {!!p.lyDo && <Text style={styles.subText}>Lý do: {p.lyDo}</Text>}
          </View>
        );
      })}
    </>,
    <>
      <Text style={styles.bold}>Chọn nhóm ưu tiên</Text>
      {groups.map(g => (
        <TouchableOpacity key={g.id} accessibilityRole="radio" accessibilityState={{ checked: group === g.id }} style={[styles.option, group === g.id && styles.selected]} onPress={() => setGroup(g.id)}>
          <Icon name={group === g.id ? 'radio-button-on' : 'radio-button-off'} size={18} />
          <Text style={styles.optionText}>{g.tenNhom}</Text>
        </TouchableOpacity>
      ))}

      <Text style={{ marginTop: 8, fontWeight: '600' }}>Tài liệu minh chứng:</Text>
      <TouchableOpacity style={styles.fileButton} disabled={uploading || busy} onPress={chonTepMinhChung}>
        {uploading ? (
          <ActivityIndicator size="small" color={Colors.primary} />
        ) : (
          <>
            <Icon name="attach-outline" />
            <Text style={styles.fileButtonText}>Chọn ảnh hoặc PDF (tối đa 5 MB)</Text>
          </>
        )}
      </TouchableOpacity>
      {!!selectedFileName && <View style={styles.fileStatus}><Icon name="checkmark-circle-outline" color={Colors.successText} size={16} /><Text style={styles.fileSuccess}>Đã chọn: {selectedFileName}</Text></View>}

      <InputField label="Hoặc nhập URL minh chứng (HTTP/HTTPS)" icon="link-outline" value={proof} onChangeText={(txt) => { setProof(txt); setSelectedFileName(''); }} autoCapitalize="none" placeholder="https://... hoặc tự điền khi chọn tệp" />

    </>,
    <>
      <InputField label="Hiệu lực từ (YYYY-MM-DD)" icon="calendar-outline" value={start} onChangeText={setStart} placeholder="2026-01-01" />
      <InputField label="Hiệu lực đến (YYYY-MM-DD)" icon="calendar-outline" value={end} onChangeText={setEnd} placeholder="2026-12-31" />
      <TouchableOpacity style={styles.button} disabled={busy || uploading} onPress={submit}><Text style={styles.buttonText}>Gửi hồ sơ</Text></TouchableOpacity>
    </>,
  ];

  return (
    <View style={styles.container}>
      <View style={styles.navigation}>
        <TouchableOpacity
          accessibilityRole="button"
          style={[styles.navButton, step === 0 && styles.disabled]}
          disabled={step === 0}
          onPress={() => goToStep(step - 1)}
        >
          <Icon name="chevron-back" size={15} />
          <Text style={styles.navText}>Quay lại</Text>
        </TouchableOpacity>
        <Text accessibilityLiveRegion="polite" style={styles.progress}>{step + 1} / {stepTitles.length}</Text>
        <TouchableOpacity
          accessibilityRole="button"
          style={[styles.navButton, step === lastStep && styles.disabled]}
          disabled={step === lastStep}
          onPress={() => goToStep(step + 1)}
        >
          <Text style={styles.navText}>{step === 1 ? 'Tạo hồ sơ' : 'Tiếp theo'}</Text>
          <Icon name="chevron-forward" size={15} />
        </TouchableOpacity>
      </View>
      {!!error && <ErrorBanner message={error} />}
      {busy && <ActivityIndicator color={Colors.primary} />}
      <View style={styles.container} onLayout={({ nativeEvent }) => setPageWidth(nativeEvent.layout.width)}>
        {pageWidth > 0 && (
          <ScrollView
            ref={pager}
            horizontal
            pagingEnabled
            showsHorizontalScrollIndicator={false}
            keyboardDismissMode="on-drag"
            keyboardShouldPersistTaps="handled"
            scrollEventThrottle={16}
            onScroll={({ nativeEvent }) => {
              const current = Math.round(nativeEvent.contentOffset.x / pageWidth);
              setStep(Math.max(0, Math.min(lastStep, current)));
            }}
          >
            {pages.map((page, index) => (
              <ScrollView
                key={stepTitles[index]}
                style={{ width: pageWidth }}
                contentContainerStyle={styles.page}
                importantForAccessibility={step === index ? 'auto' : 'no-hide-descendants'}
                accessibilityElementsHidden={step !== index}
                automaticallyAdjustKeyboardInsets
                keyboardShouldPersistTaps="handled"
                nestedScrollEnabled
              >
                <View style={styles.card}>
                  <Text accessibilityRole="header" style={styles.heading}>{stepTitles[index]}</Text>
                  {page}
                </View>
              </ScrollView>
            ))}
          </ScrollView>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  navigation: { flexDirection: 'row', alignItems: 'center', marginBottom: 12, gap: 8 },
  navButton: { flex: 1, flexDirection: 'row', gap: 4, minHeight: 48, alignItems: 'center', justifyContent: 'center', padding: 6, backgroundColor: Colors.primaryLight, borderRadius: 12 },
  navText: { flexShrink: 1, color: Colors.primaryDark, fontWeight: '700', textAlign: 'center' },
  progress: { color: Colors.textSecondary, fontWeight: '700', paddingHorizontal: 4 },
  disabled: { opacity: 0.48 },
  page: { paddingBottom: 48 },
  card: { backgroundColor: Colors.surface, borderRadius: 16, padding: 16, gap: 12, borderWidth: 1, borderColor: Colors.border },
  heading: { fontSize: 19, fontWeight: '800', color: Colors.textPrimary },
  button: { minHeight: 48, justifyContent: 'center', backgroundColor: Colors.primary, borderRadius: 12, padding: 12, alignItems: 'center' },
  buttonText: { color: Colors.white, fontWeight: '700' },
  bold: { color: Colors.textPrimary, fontWeight: '700' },
  profile: { borderWidth: 1, borderColor: Colors.border, borderRadius: 12, backgroundColor: Colors.background, padding: 12, gap: 6 },
  badge: { alignSelf: 'flex-start', paddingHorizontal: 8, paddingVertical: 4, borderRadius: 999, overflow: 'hidden', fontSize: 12, fontWeight: '700', color: Colors.textSecondary, backgroundColor: Colors.grayLight },
  subText: { fontSize: 13, color: Colors.textSecondary },
  option: { minHeight: 48, flexDirection: 'row', alignItems: 'center', gap: 8, borderWidth: 1, borderColor: Colors.border, borderRadius: 12, padding: 10 },
  selected: { borderColor: Colors.primary, backgroundColor: Colors.primaryLight },
  optionText: { flex: 1, color: Colors.textPrimary },
  fileButton: { minHeight: 48, flexDirection: 'row', justifyContent: 'center', gap: 8, borderWidth: 1, borderColor: Colors.primary, borderStyle: 'dashed', borderRadius: 12, padding: 10, alignItems: 'center', backgroundColor: Colors.primaryLight },
  fileButtonText: { flexShrink: 1, color: Colors.primaryDark, fontWeight: '700' },
  fileStatus: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  fileSuccess: { flex: 1, color: Colors.successText, fontSize: 13, fontWeight: '600' },
});
