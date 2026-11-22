import React, { useMemo, useState } from 'react';
import { FlatList, Modal, Platform, Pressable, StatusBar, StyleSheet, Text, TextInput, TouchableOpacity, View, type TextInputProps } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Colors } from '../constants/colors';

export const headerInset = Platform.select({ android: StatusBar.currentHeight ?? 24, ios: 54, default: 16 });
export const footerInset = Platform.select({ android: 48, ios: 34, default: 0 });

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

export type SelectOption = { value: number; label: string };

export function SelectField({ label, icon, placeholder, options, value, onChange, disabled, emptyText = 'Không có dữ liệu' }: {
  label: string;
  icon: IconName;
  placeholder: string;
  options: SelectOption[];
  value: number | null;
  onChange: (value: number) => void;
  disabled?: boolean;
  emptyText?: string;
}) {
  const [open, setOpen] = useState(false);
  const [keyword, setKeyword] = useState('');
  const selected = options.find((o) => o.value === value);
  const filtered = useMemo(() => {
    const k = keyword.trim().toLocaleLowerCase('vi');
    return k ? options.filter((o) => o.label.toLocaleLowerCase('vi').includes(k)) : options;
  }, [keyword, options]);

  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <TouchableOpacity
        accessibilityRole="button"
        accessibilityLabel={`${label}: ${selected?.label ?? placeholder}`}
        disabled={disabled}
        style={[styles.inputShell, disabled && styles.selectDisabled]}
        onPress={() => { setKeyword(''); setOpen(true); }}
      >
        <Icon name={icon} size={18} color={Colors.textSecondary} />
        <Text numberOfLines={1} style={[styles.selectText, !selected && { color: Colors.textMuted }]}>{selected?.label ?? placeholder}</Text>
        <Icon name="chevron-down" size={18} color={Colors.textSecondary} />
      </TouchableOpacity>
      <Modal visible={open} transparent animationType="slide" onRequestClose={() => setOpen(false)}>
        <Pressable style={styles.sheetBackdrop} onPress={() => setOpen(false)} />
        <View style={styles.sheet}>
          <View style={styles.sheetHeader}>
            <Text accessibilityRole="header" style={styles.sheetTitle}>{label}</Text>
            <TouchableOpacity accessibilityRole="button" accessibilityLabel="Đóng" style={styles.sheetClose} onPress={() => setOpen(false)}>
              <Icon name="close" color={Colors.textSecondary} />
            </TouchableOpacity>
          </View>
          {options.length > 8 && (
            <View style={styles.inputShell}>
              <Icon name="search-outline" size={18} color={Colors.textSecondary} />
              <TextInput value={keyword} onChangeText={setKeyword} placeholder="Tìm kiếm..." placeholderTextColor={Colors.textMuted} style={styles.input} accessibilityLabel={`Tìm ${label}`} />
            </View>
          )}
          <FlatList
            data={filtered}
            keyExtractor={(item) => String(item.value)}
            keyboardShouldPersistTaps="handled"
            ListEmptyComponent={<Text style={styles.sheetEmpty}>{emptyText}</Text>}
            renderItem={({ item }) => {
              const active = item.value === value;
              return (
                <TouchableOpacity
                  accessibilityRole="radio"
                  accessibilityState={{ checked: active }}
                  style={[styles.sheetItem, active && styles.sheetItemActive]}
                  onPress={() => { onChange(item.value); setOpen(false); }}
                >
                  <Text style={[styles.sheetItemText, active && { color: Colors.primaryDark, fontWeight: '700' }]}>{item.label}</Text>
                  {active && <Icon name="checkmark" />}
                </TouchableOpacity>
              );
            }}
          />
        </View>
      </Modal>
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
  selectText: { flex: 1, color: Colors.textPrimary, fontSize: 16 },
  selectDisabled: { opacity: 0.5 },
  sheetBackdrop: { flex: 1, backgroundColor: 'rgba(15,23,42,0.45)' },
  sheet: { maxHeight: '75%', backgroundColor: Colors.surface, borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 16, paddingBottom: 16 + footerInset, gap: 12 },
  sheetHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  sheetTitle: { fontSize: 18, fontWeight: '800', color: Colors.textPrimary },
  sheetClose: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  sheetItem: { minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingHorizontal: 12, borderRadius: 12 },
  sheetItemActive: { backgroundColor: Colors.primaryLight },
  sheetItemText: { flex: 1, color: Colors.textPrimary, fontSize: 15 },
  sheetEmpty: { color: Colors.textMuted, textAlign: 'center', paddingVertical: 24 },
});
