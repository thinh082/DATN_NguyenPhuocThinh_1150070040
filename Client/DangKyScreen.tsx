import React, { useState } from 'react';
import { ActivityIndicator, Alert, ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { BrandHeader, ErrorBanner, Icon, InputField } from './components/MobileUi';
import { dangKy } from './services/xacThucService';
import { Colors } from './constants/colors';

export default function DangKyScreen({ onBack, onRegistered }: { onBack: () => void; onRegistered: (account: string) => void }) {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [cccd, setCccd] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit() {
    if (!name.trim() || (!email.trim() && !phone.trim())) { setError('Nhập họ tên và ít nhất email hoặc số điện thoại.'); return; }
    if (password.length < 8) { setError('Mật khẩu phải có ít nhất 8 ký tự.'); return; }
    if (password !== confirm) { setError('Mật khẩu xác nhận chưa khớp.'); return; }
    try {
      setBusy(true); setError('');
      const res = await dangKy({ hoTen: name.trim(), email: email.trim(), soDienThoai: phone.trim(), cccd: cccd.trim(), matKhau: password });
      if (res.statusCode !== 200) { setError(res.message); return; }
      Alert.alert('Đăng ký thành công', 'Hãy đăng nhập bằng tài khoản vừa tạo.');
      onRegistered(email.trim() || phone.trim());
    } catch (e: any) { setError(e.message || 'Không thể kết nối máy chủ.'); }
    finally { setBusy(false); }
  }

  return (
    <ScrollView style={styles.screen} keyboardShouldPersistTaps="handled" automaticallyAdjustKeyboardInsets>
      <StatusBar style="light" />
      <BrandHeader />
      <View style={styles.content}>
        <TouchableOpacity accessibilityRole="button" style={styles.back} onPress={onBack} disabled={busy}>
          <Icon name="arrow-back" size={16} />
          <Text style={styles.link}>Quay lại đăng nhập</Text>
        </TouchableOpacity>
        <View style={styles.card}>
          <Text accessibilityRole="header" style={styles.title}>Đăng ký hành khách</Text>
          <Text style={styles.subtitle}>Nhập họ tên và ít nhất một thông tin liên hệ.</Text>
          {!!error && <ErrorBanner message={error} />}
          <InputField label="Họ và tên *" icon="person-outline" value={name} onChangeText={setName} autoCapitalize="words" />
          <InputField label="Email" icon="mail-outline" value={email} onChangeText={setEmail} keyboardType="email-address" autoCapitalize="none" />
          <InputField label="Số điện thoại" icon="call-outline" value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
          <InputField label="CCCD (không bắt buộc)" icon="card-outline" value={cccd} onChangeText={setCccd} keyboardType="number-pad" maxLength={12} />
          <InputField label="Mật khẩu *" icon="lock-closed-outline" value={password} onChangeText={setPassword} autoComplete="new-password" secureTextEntry />
          <InputField label="Xác nhận mật khẩu *" icon="lock-closed-outline" value={confirm} onChangeText={setConfirm} autoComplete="new-password" secureTextEntry />
          <TouchableOpacity accessibilityRole="button" style={[styles.button, busy && styles.disabled]} onPress={submit} disabled={busy}>
            {busy ? <ActivityIndicator color={Colors.white} /> : <Text style={styles.buttonText}>Tạo tài khoản</Text>}
          </TouchableOpacity>
        </View>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.background },
  content: { width: '100%', maxWidth: 480, alignSelf: 'center', padding: 16, paddingBottom: 48, gap: 8 },
  card: { gap: 12, padding: 16, borderWidth: 1, borderColor: Colors.border, borderRadius: 16, backgroundColor: Colors.surface, elevation: 1 },
  title: { fontSize: 22, fontWeight: '800', color: Colors.textPrimary },
  subtitle: { fontSize: 14, color: Colors.textSecondary },
  button: { minHeight: 48, padding: 12, backgroundColor: Colors.primary, alignItems: 'center', justifyContent: 'center', borderRadius: 12 },
  buttonText: { color: Colors.white, fontWeight: '700', fontSize: 16 },
  disabled: { opacity: 0.55 },
  back: { minHeight: 48, alignSelf: 'flex-start', flexDirection: 'row', gap: 8, alignItems: 'center', paddingHorizontal: 10 },
  link: { color: Colors.primaryDark, fontWeight: '700' },
});
