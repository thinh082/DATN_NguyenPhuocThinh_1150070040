import React, { useState } from 'react';
import { ActivityIndicator, Alert, ScrollView, StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native';
import { dangKy } from './services/xacThucService';
import Colors from './constants/colors';

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

  return <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
    <Text style={styles.title}>Đăng ký hành khách</Text>
    <Text>Nhập họ tên và ít nhất một thông tin liên hệ.</Text>
    {!!error && <Text style={styles.error}>{error}</Text>}
    <Text>Họ và tên *</Text><TextInput style={styles.input} value={name} onChangeText={setName} autoCapitalize="words" />
    <Text>Email</Text><TextInput style={styles.input} value={email} onChangeText={setEmail} keyboardType="email-address" autoCapitalize="none" />
    <Text>Số điện thoại</Text><TextInput style={styles.input} value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
    <Text>CCCD (không bắt buộc)</Text><TextInput style={styles.input} value={cccd} onChangeText={setCccd} keyboardType="number-pad" maxLength={12} />
    <Text>Mật khẩu *</Text><TextInput style={styles.input} value={password} onChangeText={setPassword} secureTextEntry />
    <Text>Xác nhận mật khẩu *</Text><TextInput style={styles.input} value={confirm} onChangeText={setConfirm} secureTextEntry />
    <TouchableOpacity style={styles.button} onPress={submit} disabled={busy}>{busy ? <ActivityIndicator color="white" /> : <Text style={styles.buttonText}>Tạo tài khoản</Text>}</TouchableOpacity>
    <TouchableOpacity onPress={onBack} disabled={busy}><Text style={styles.link}>← Quay lại đăng nhập</Text></TouchableOpacity>
  </ScrollView>;
}

const styles = StyleSheet.create({
  container: { padding: 24, paddingTop: 60, gap: 10, backgroundColor: Colors.background, flexGrow: 1 },
  title: { fontSize: 22, fontWeight: 'bold', color: Colors.primary }, input: { backgroundColor: 'white', borderWidth: 1, borderColor: Colors.border, borderRadius: 10, padding: 12 },
  error: { color: Colors.danger }, button: { padding: 14, backgroundColor: Colors.primary, alignItems: 'center', borderRadius: 10 },
  buttonText: { color: 'white', fontWeight: 'bold' }, link: { color: Colors.primary, textAlign: 'center', padding: 10 },
});
