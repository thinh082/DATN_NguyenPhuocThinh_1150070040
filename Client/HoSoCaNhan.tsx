import React, { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Alert, Modal, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import * as DocumentPicker from 'expo-document-picker';
import { Icon, InputField, SelectField, footerInset, headerInset, type IconName } from './components/MobileUi';
import { StepPager, formStyles, type StepPagerHandle } from './components/StepPager';
import { Colors } from './constants/colors';
import {
  capNhatThongTin,
  DonViHanhChinh,
  layPhuongXa,
  layThongTinChiTiet,
  layTinhThanh,
  PhuongThucXacThuc,
  taiLenMinhChung,
  ThongTinCaNhanChiTiet,
  ThongTinNguoiDung,
  xacThucDinhDanh,
} from './services/xacThucService';

const MA_TP_HCM = 79;
const phuongThucLabels: Record<PhuongThucXacThuc, string> = {
  CCCD: 'Căn cước công dân',
  VNeID: 'VNeID',
  NganHang: 'Thẻ ngân hàng',
  ViDienTu: 'Ví điện tử',
};
const phuongThucMoPhong: { key: Exclude<PhuongThucXacThuc, 'CCCD'>; title: string; desc: string; icon: IconName }[] = [
  { key: 'VNeID', title: 'Xác thực qua VNeID', desc: 'Tài khoản định danh điện tử mức 2', icon: 'shield-checkmark-outline' },
  { key: 'NganHang', title: 'Thẻ ngân hàng', desc: 'Napas, Visa, Mastercard', icon: 'card-outline' },
  { key: 'ViDienTu', title: 'Ví điện tử', desc: 'Ví đã liên kết định danh', icon: 'wallet-outline' },
];

export default function HoSoCaNhan({ user, onUpdated }: { user: ThongTinNguoiDung; onUpdated: (user: ThongTinNguoiDung) => void }) {
  const [name, setName] = useState(user.hoTen);
  const [email, setEmail] = useState(user.email || '');
  const [phone, setPhone] = useState(user.soDienThoai || '');
  const [cccd, setCccd] = useState(user.cccd || '');
  const [savedCccd, setSavedCccd] = useState(user.cccd || '');
  const [provinces, setProvinces] = useState<DonViHanhChinh[]>([]);
  const [wards, setWards] = useState<DonViHanhChinh[]>([]);
  const [province, setProvince] = useState<number | null>(null);
  const [ward, setWard] = useState<number | null>(null);
  const [address, setAddress] = useState('');
  const [method, setMethod] = useState<PhuongThucXacThuc | null>(null);
  const [verifiedAt, setVerifiedAt] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [cameraOpen, setCameraOpen] = useState(false);
  const [permission, requestPermission] = useCameraPermissions();
  const camera = useRef<CameraView>(null);
  const pager = useRef<StepPagerHandle>(null);

  function applyVerification(t: ThongTinCaNhanChiTiet) {
    setMethod(t.phuongThucXacThuc ?? null);
    setVerifiedAt(t.ngayXacThuc ?? null);
  }

  useEffect(() => {
    void (async () => {
      try {
        const [info, tinh] = await Promise.all([layThongTinChiTiet(), layTinhThanh()]);
        setProvinces(tinh.data ?? []);
        const t = info.data?.thongTin;
        if (!t) return;
        setCccd(t.cccd || '');
        setSavedCccd(t.cccd || '');
        setProvince(t.maTinhThanh ?? MA_TP_HCM);
        setWard(t.maPhuongXa ?? null);
        setAddress(t.diaChi || '');
        applyVerification(t);
      } catch (e: any) { setError(e.message || 'Không tải được thông tin.'); }
    })();
  }, []);

  useEffect(() => {
    if (!province) { setWards([]); return; }
    layPhuongXa(province).then((r) => setWards(r.data ?? [])).catch(() => setWards([]));
  }, [province]);

  async function save() {
    if (province && wards.length > 0 && !ward) { setError('Vui lòng chọn phường/xã.'); return; }
    try {
      setBusy(true); setError('');
      const res = await capNhatThongTin({
        hoTen: name.trim(),
        email: email.trim(),
        soDienThoai: phone.trim(),
        cccd: cccd.trim(),
        diaChi: address.trim() || null,
        maTinhThanh: province,
        maPhuongXa: ward,
      });
      if (res.statusCode !== 200) throw new Error(res.message);
      onUpdated({ ...user, ...res.data });
      setSavedCccd(res.data.cccd || '');
      applyVerification(res.data);
      Alert.alert('Đã lưu', 'Thông tin hành chính đã được cập nhật. Tiếp tục xác thực định danh.');
      pager.current?.goToStep(1);
    } catch (e: any) { setError(e.message || 'Không lưu được thông tin.'); }
    finally { setBusy(false); }
  }

  async function verify(phuongThuc: PhuongThucXacThuc, duongDan?: string) {
    try {
      setBusy(true); setError('');
      const res = await xacThucDinhDanh(phuongThuc, duongDan);
      if (res.statusCode !== 200) throw new Error(res.message);
      setMethod(res.data.phuongThucXacThuc);
      setVerifiedAt(res.data.ngayXacThuc);
      Alert.alert('Xác thực thành công', `Tài khoản đã được định danh qua ${phuongThucLabels[phuongThuc]}.`);
    } catch (e: any) { setError(e.message || 'Xác thực không thành công.'); }
    finally { setBusy(false); }
  }

  async function uploadAndVerify(file: { uri: string; name: string; type: string }) {
    try {
      setBusy(true); setError('');
      const up = await taiLenMinhChung(file);
      if (up.statusCode !== 200) throw new Error(up.message);
      await verify('CCCD', up.data.duongDan);
    } catch (e: any) {
      setError(e.message || 'Không tải được ảnh CCCD.');
      setBusy(false);
    }
  }

  function ensureCccd() {
    if (/^\d{12}$/.test(savedCccd)) return true;
    setError('Vui lòng nhập và lưu số CCCD (12 chữ số) ở bước Thông tin hành chính trước.');
    pager.current?.goToStep(0);
    return false;
  }

  async function openCamera() {
    if (!ensureCccd()) return;
    if (!permission?.granted && !(await requestPermission()).granted) { setError('Cần quyền camera để chụp CCCD.'); return; }
    setCameraOpen(true);
  }

  async function capture() {
    try {
      const photo = await camera.current?.takePictureAsync({ quality: 0.6 });
      setCameraOpen(false);
      if (photo?.uri) await uploadAndVerify({ uri: photo.uri, name: `cccd_${Date.now()}.jpg`, type: 'image/jpeg' });
    } catch (e: any) {
      setCameraOpen(false);
      setError(e.message || 'Không chụp được ảnh.');
    }
  }

  async function pickImage() {
    if (!ensureCccd()) return;
    const res = await DocumentPicker.getDocumentAsync({ type: ['image/jpeg', 'image/png'], copyToCacheDirectory: true });
    if (res.canceled || !res.assets?.length) return;
    const file = res.assets[0];
    if (file.size && file.size > 5 * 1024 * 1024) { setError('Ảnh vượt quá 5MB.'); return; }
    await uploadAndVerify({ uri: file.uri, name: file.name, type: file.mimeType || 'image/jpeg' });
  }

  const pages = [
    {
      title: 'Thông tin hành chính',
      content: (
        <>
          <InputField label="Họ và tên *" icon="person-outline" value={name} onChangeText={setName} />
          <InputField label="Email" icon="mail-outline" value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" />
          <InputField label="Số điện thoại" icon="call-outline" value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
          <InputField label="Số CCCD" icon="card-outline" value={cccd} onChangeText={setCccd} keyboardType="number-pad" maxLength={12} />
          <SelectField
            label="Tỉnh / Thành phố"
            icon="business-outline"
            placeholder="Chọn tỉnh/thành phố"
            options={provinces.map((p) => ({ value: p.ma, label: p.ten }))}
            value={province}
            onChange={(v) => { if (v !== province) { setProvince(v); setWard(null); } }}
          />
          <SelectField
            label="Phường / Xã"
            icon="location-outline"
            placeholder={wards.length ? 'Chọn phường/xã' : 'Chưa có dữ liệu cho tỉnh này'}
            options={wards.map((w) => ({ value: w.ma, label: w.ten }))}
            value={ward}
            onChange={setWard}
            disabled={!province || wards.length === 0}
          />
          <InputField label="Địa chỉ (số nhà, tên đường)" icon="home-outline" value={address} onChangeText={setAddress} maxLength={1000} placeholder="VD: 12 Nguyễn Huệ" />
          <TouchableOpacity accessibilityRole="button" style={formStyles.button} disabled={busy} onPress={save}>
            <Text style={formStyles.buttonText}>Lưu thông tin</Text>
          </TouchableOpacity>
        </>
      ),
    },
    {
      title: 'Xác thực định danh',
      content: (
        <>
          <View style={[styles.status, method ? styles.statusOk : styles.statusWarn]}>
            <Icon name={method ? 'checkmark-circle' : 'alert-circle-outline'} color={method ? Colors.successText : Colors.warningText} size={22} />
            <View style={styles.statusBody}>
              <Text style={[formStyles.bold, { color: method ? Colors.successText : Colors.warningText }]}>
                {method ? `Đã xác thực qua ${phuongThucLabels[method]}` : 'Chưa xác thực định danh'}
              </Text>
              <Text style={formStyles.subText}>
                {method && verifiedAt
                  ? `Lúc ${new Date(verifiedAt).toLocaleString('vi-VN')}`
                  : 'Từ 01/10/2026, hành khách cần định danh (CCCD, VNeID, thẻ ngân hàng hoặc ví điện tử) để đi miễn phí 134 tuyến theo NQ 09/2026/NQ-HĐND.'}
              </Text>
            </View>
          </View>

          <Text style={formStyles.bold}>Căn cước công dân</Text>
          <Text style={formStyles.subText}>Chụp mặt trước CCCD số {savedCccd || '(chưa cập nhật)'}. Ảnh được lưu làm tài liệu minh chứng.</Text>
          <TouchableOpacity accessibilityRole="button" style={formStyles.button} disabled={busy} onPress={openCamera}>
            <Text style={formStyles.buttonText}>Chụp ảnh CCCD</Text>
          </TouchableOpacity>
          <TouchableOpacity accessibilityRole="button" style={formStyles.fileButton} disabled={busy} onPress={pickImage}>
            <Icon name="image-outline" />
            <Text style={formStyles.fileButtonText}>Chọn ảnh CCCD có sẵn</Text>
          </TouchableOpacity>

          <Text style={[formStyles.bold, styles.sectionGap]}>Phương thức khác</Text>
          {phuongThucMoPhong.map((m) => (
            <TouchableOpacity
              key={m.key}
              accessibilityRole="button"
              style={[styles.method, method === m.key && styles.methodActive]}
              disabled={busy}
              onPress={() => verify(m.key)}
            >
              <View style={styles.methodIcon}><Icon name={m.icon} /></View>
              <View style={styles.statusBody}>
                <Text style={formStyles.bold}>{m.title}</Text>
                <Text style={formStyles.subText}>{m.desc}</Text>
              </View>
              <Icon name={method === m.key ? 'checkmark-circle' : 'chevron-forward'} color={method === m.key ? Colors.successText : Colors.primary} size={18} />
            </TouchableOpacity>
          ))}
        </>
      ),
    },
  ];

  return (
    <>
      <StepPager ref={pager} pages={pages} error={error} busy={busy} />
      <Modal visible={cameraOpen} animationType="slide" onRequestClose={() => setCameraOpen(false)}>
        <View style={styles.cameraWrap}>
          <CameraView ref={camera} style={StyleSheet.absoluteFill} facing="back" />
          <View style={styles.cameraTop}>
            <TouchableOpacity accessibilityRole="button" accessibilityLabel="Đóng camera" style={styles.cameraClose} onPress={() => setCameraOpen(false)}>
              <Icon name="close" color={Colors.white} />
            </TouchableOpacity>
          </View>
          <View pointerEvents="none" style={styles.cardFrame} />
          <View style={styles.cameraBottom}>
            <Text style={styles.cameraHint}>Đặt mặt trước CCCD vào khung, đủ sáng và rõ chữ</Text>
            <TouchableOpacity accessibilityRole="button" accessibilityLabel="Chụp ảnh" style={styles.shutter} onPress={capture}>
              {busy ? <ActivityIndicator color={Colors.primary} /> : <View style={styles.shutterInner} />}
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  status: { flexDirection: 'row', gap: 10, alignItems: 'flex-start', borderRadius: 12, padding: 12 },
  statusOk: { backgroundColor: Colors.successSurface },
  statusWarn: { backgroundColor: Colors.warningSurface },
  statusBody: { flex: 1, gap: 2 },
  sectionGap: { marginTop: 4 },
  method: { minHeight: 56, flexDirection: 'row', alignItems: 'center', gap: 10, borderWidth: 1, borderColor: Colors.border, borderRadius: 12, padding: 10 },
  methodActive: { borderColor: Colors.primary, backgroundColor: Colors.primaryLight },
  methodIcon: { width: 40, height: 40, borderRadius: 20, backgroundColor: Colors.primaryLight, alignItems: 'center', justifyContent: 'center' },
  cameraWrap: { flex: 1, backgroundColor: '#000' },
  cameraTop: { paddingTop: headerInset + 8, paddingHorizontal: 16, alignItems: 'flex-end' },
  cameraClose: { width: 44, height: 44, borderRadius: 22, backgroundColor: 'rgba(15,23,42,0.6)', alignItems: 'center', justifyContent: 'center' },
  cardFrame: { position: 'absolute', left: '7%', right: '7%', top: '30%', aspectRatio: 1.586, borderWidth: 3, borderColor: Colors.accent, borderRadius: 16 },
  cameraBottom: { position: 'absolute', left: 0, right: 0, bottom: 24 + footerInset, alignItems: 'center', gap: 16, paddingHorizontal: 24 },
  cameraHint: { color: Colors.white, backgroundColor: 'rgba(15,23,42,0.6)', paddingHorizontal: 14, paddingVertical: 8, borderRadius: 999, textAlign: 'center' },
  shutter: { width: 72, height: 72, borderRadius: 36, backgroundColor: Colors.white, alignItems: 'center', justifyContent: 'center' },
  shutterInner: { width: 58, height: 58, borderRadius: 29, borderWidth: 2, borderColor: Colors.primary },
});
