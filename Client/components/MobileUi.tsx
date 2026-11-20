import React, { useState } from 'react';
import { Platform, StatusBar, StyleSheet, Text, TextInput, View, type TextInputProps } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Colors } from '../constants/colors';

export const headerInset = Platform.select({ android: StatusBar.currentHeight ?? 24, ios: 54, default: 16 });

export type IconName = React.ComponentProps<typeof Ionicons>['name'];

export function Icon({ name, color = Colors.primary, size = 20 }: { name: IconName; color?: string; size?: number }) {
  return <Ionicons name={name} color={color} size={size} accessible={false} importantForAccessibility="no" />;
}

export function BrandHeader() {
  return (
    <View style={styles.brand}>
      <View style={styles.brandMark}><Icon name="bus-outline" color={Colors.white} size={29} /></View>
      <Text accessibilityRole="header" style={styles.brandTitle}>SmartBus TP.HCM</Text>
      <Text style={styles.brandSubtitle}>Vé xe buýt điện tử</Text>
    </View>
  );
}

export function InputField({ label, icon, ...props }: TextInputProps & { label: string; icon: IconName }) {
  const [focused, setFocused] = useState(false);
  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <View style={[styles.inputShell, focused && styles.focused]}>
        <Icon name={icon} size={18} color={focused ? Colors.primary : Colors.textSecondary} />
        <TextInput
          {...props}
          accessibilityLabel={label}
          placeholderTextColor={Colors.textMuted}
          style={[styles.input, props.style]}
          onFocus={(event) => { setFocused(true); props.onFocus?.(event); }}
          onBlur={(event) => { setFocused(false); props.onBlur?.(event); }}
        />
      </View>
    </View>
  );
}

export function ErrorBanner({ message }: { message: string }) {
  return (
    <View accessibilityLiveRegion="polite" style={styles.error}>
      <Icon name="alert-circle-outline" color={Colors.dangerText} />
      <Text style={styles.errorText}>{message}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  brand: {
    backgroundColor: Colors.primaryDark,
    paddingTop: headerInset + 16,
    paddingBottom: 24,
    paddingHorizontal: 16,
    borderBottomLeftRadius: 24,
    borderBottomRightRadius: 24,
    alignItems: 'center',
    gap: 6,
  },
  brandMark: { width: 52, height: 52, borderRadius: 16, backgroundColor: Colors.primary, alignItems: 'center', justifyContent: 'center', marginBottom: 4 },
  brandTitle: { color: Colors.white, fontSize: 23, fontWeight: '800', textAlign: 'center' },
  brandSubtitle: { color: Colors.primaryLight, fontSize: 14, textAlign: 'center' },
  field: { gap: 6 },
  label: { color: Colors.textPrimary, fontSize: 14, fontWeight: '600' },
  inputShell: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 12, minHeight: 48, borderRadius: 12, borderWidth: 1, borderColor: Colors.border, backgroundColor: Colors.background },
  focused: { borderColor: Colors.primary },
  input: { flex: 1, minHeight: 46, paddingVertical: 8, color: Colors.textPrimary, fontSize: 16 },
  error: { flexDirection: 'row', alignItems: 'center', gap: 8, borderRadius: 12, padding: 12, backgroundColor: Colors.dangerSurface },
  errorText: { flex: 1, color: Colors.dangerText, fontSize: 14 },
});
