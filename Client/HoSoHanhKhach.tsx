import React, { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Alert, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import * as DocumentPicker from 'expo-document-picker';
import { Icon, InputField, SelectField } from './components/MobileUi';
import { StepPager, formStyles, type StepPagerHandle } from './components/StepPager';
import { Colors } from './constants/colors';
import { HoSoUuTien, layHoSoCuaToi, layNhomUuTien, NhomUuTien, nopHoSoUuTien, taiLenMinhChung } from './services/xacThucService';

const profileStatuses = {
  ChoDuyet: { label: 'Chờ duyệt', background: Colors.warningSurface, color: Colors.warningText },
  DaDuyet: { label: 'Đã duyệt', background: Colors.successSurface, color: Colors.successText },
  TuChoi: { label: 'Từ chối', background: Colors.dangerSurface, color: Colors.dangerText },
  ThuHoi: { label: 'Thu hồi', background: Colors.grayLight, color: Colors.textSecondary },
};

export default function HoSoHanhKhach() {
  const [groups, setGroups] = useState<NhomUuTien[]>([]);
  const [profiles, setProfiles] = useState<HoSoUuTien[]>([]);
  const [group, setGroup] = useState<number | null>(null);
  const [proof, setProof] = useState('');
  const [selectedFileName, setSelectedFileName] = useState('');
  const [busy, setBusy] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const pager = useRef<StepPagerHandle>(null);

  async function reload() {
    const [g, p] = await Promise.all([layNhomUuTien(), layHoSoCuaToi()]);
    if (g.statusCode !== 200 || p.statusCode !== 200) throw new Error(g.message || p.message || 'Không tải được hồ sơ.');
    setGroups(g.data); setProfiles(p.data);
  }
  useEffect(() => { void reload().catch((e) => setError(e.message || 'Không tải được hồ sơ.')); }, []);

  async function chonTepMinhChung() {
    try {
      setError('');
      const res = await DocumentPicker.getDocumentAsync({ type: ['image/jpeg', 'image/png', 'application/pdf'], copyToCacheDirectory: true });
      if (res.canceled || !res.assets || res.assets.length === 0) return;
      const file = res.assets[0];
      if (file.size && file.size > 5 * 1024 * 1024) {
        setError('Kích thước tệp vượt quá 5MB. Vui lòng chọn tệp nhỏ hơn.');
        return;
      }
      setUploading(true);
      const upRes = await taiLenMinhChung({ uri: file.uri, name: file.name, type: file.mimeType || 'image/jpeg' });
      if (upRes.statusCode !== 200) throw new Error(upRes.message);
      setProof(upRes.data.duongDan);
      setSelectedFileName(file.name);
    } catch (e: any) {
      setError(e.message || 'Không thể tải lên tệp minh chứng.');
    } finally {
      setUploading(false);
    }
  }

  async function submit() {
    if (!group || !proof.trim()) {
      setError('Chọn nhóm ưu tiên và cung cấp tài liệu minh chứng (tải tệp hoặc nhập URL).');
      return;
    }
    const today = new Date();
    const nextYear = new Date(today.getTime() + 365 * 86400000);
    try {
      setBusy(true); setError('');
      const res = await nopHoSoUuTien({
        nhomDoiTuongId: group,
        duongDanMinhChung: proof.trim(),
        hieuLucTu: `${today.toISOString().slice(0, 10)}T00:00:00Z`,
        hieuLucDen: `${nextYear.toISOString().slice(0, 10)}T00:00:00Z`,
      });
      if (res.statusCode !== 200) throw new Error(res.message);
      setProof(''); setSelectedFileName(''); setGroup(null);
      await reload();
      pager.current?.goToStep(0);
      Alert.alert('Đã gửi', 'Hồ sơ đang chờ quản trị viên xét duyệt.');
    } catch (e: any) { setError(e.message || 'Không gửi được hồ sơ.'); }
    finally { setBusy(false); }
  }

  const pages = [
    {
      title: 'Hồ sơ ưu tiên của tôi',
      content: profiles.length === 0
        ? <Text style={formStyles.subText}>Chưa có hồ sơ ưu tiên. Chuyển sang bước tiếp theo để nộp hồ sơ.</Text>
        : profiles.map((p) => {
          const status = profileStatuses[p.trangThai as keyof typeof profileStatuses];
          return (
            <View key={p.id} style={styles.profile}>
              <Text style={formStyles.bold}>{p.tenNhom}</Text>
              <Text style={[formStyles.badge, status && { backgroundColor: status.background, color: status.color }]}>{status?.label ?? p.trangThai}</Text>
              <Text style={formStyles.subText}>Hiệu lực: {p.hieuLucTu} đến {p.hieuLucDen}</Text>
              {!!p.lyDo && <Text style={formStyles.subText}>Lý do: {p.lyDo}</Text>}
            </View>
          );
        }),
    },
    {
      title: 'Nộp hồ sơ ưu tiên',
      content: (
        <>
          <SelectField
            label="Nhóm đối tượng ưu tiên"
            icon="people-outline"
            placeholder="Chọn nhóm đối tượng"
            options={groups.map((g) => ({ value: g.id, label: g.tenNhom }))}
            value={group}
            onChange={setGroup}
            emptyText="Chưa có nhóm ưu tiên nào đang mở"
          />
          <Text style={formStyles.bold}>Tài liệu minh chứng</Text>
          <TouchableOpacity accessibilityRole="button" style={formStyles.fileButton} disabled={uploading || busy} onPress={chonTepMinhChung}>
            {uploading ? <ActivityIndicator size="small" color={Colors.primary} /> : (
              <>
                <Icon name="attach-outline" />
                <Text style={formStyles.fileButtonText}>Chọn ảnh hoặc PDF (tối đa 5 MB)</Text>
              </>
            )}
          </TouchableOpacity>
          {!!selectedFileName && (
            <View style={formStyles.fileStatus}>
              <Icon name="checkmark-circle-outline" color={Colors.successText} size={16} />
              <Text style={formStyles.fileSuccess}>Đã tải lên: {selectedFileName}</Text>
            </View>
          )}
          <InputField
            label="Hoặc nhập URL minh chứng (HTTP/HTTPS)"
            icon="link-outline"
            value={selectedFileName ? '' : proof}
            onChangeText={(txt) => { setProof(txt); setSelectedFileName(''); }}
            autoCapitalize="none"
            placeholder="https://..."
          />
          <TouchableOpacity accessibilityRole="button" style={formStyles.button} disabled={busy || uploading} onPress={submit}>
            <Text style={formStyles.buttonText}>Gửi hồ sơ</Text>
          </TouchableOpacity>
        </>
      ),
    },
  ];

  return <StepPager ref={pager} pages={pages} error={error} busy={busy} nextLabel={(s) => (s === 0 ? 'Nộp hồ sơ' : 'Tiếp theo')} />;
}

const styles = StyleSheet.create({
  profile: { borderWidth: 1, borderColor: Colors.border, borderRadius: 12, backgroundColor: Colors.background, padding: 12, gap: 6 },
});
