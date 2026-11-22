import React, { useImperativeHandle, useRef, useState } from 'react';
import { ActivityIndicator, Keyboard, ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { ErrorBanner, Icon } from './MobileUi';
import { Colors } from '../constants/colors';

export type StepPagerHandle = { goToStep: (step: number) => void };

export function StepPager({ ref, pages, error, busy, nextLabel }: {
  ref?: React.Ref<StepPagerHandle>;
  pages: { title: string; content: React.ReactNode }[];
  error?: string;
  busy?: boolean;
  nextLabel?: (step: number) => string;
}) {
  const [step, setStep] = useState(0);
  const [pageWidth, setPageWidth] = useState(0);
  const pager = useRef<React.ComponentRef<typeof ScrollView>>(null);
  const lastStep = pages.length - 1;

  function goToStep(next: number) {
    Keyboard.dismiss();
    pager.current?.scrollTo({ x: next * pageWidth, animated: true });
  }
  useImperativeHandle(ref, () => ({ goToStep }), [pageWidth]);

  return (
    <View style={styles.container}>
      <View style={styles.navigation}>
        <TouchableOpacity accessibilityRole="button" style={[styles.navButton, step === 0 && styles.disabled]} disabled={step === 0} onPress={() => goToStep(step - 1)}>
          <Icon name="chevron-back" size={15} />
          <Text style={styles.navText}>Quay lại</Text>
        </TouchableOpacity>
        <Text accessibilityLiveRegion="polite" style={styles.progress}>{step + 1} / {pages.length}</Text>
        <TouchableOpacity accessibilityRole="button" style={[styles.navButton, step === lastStep && styles.disabled]} disabled={step === lastStep} onPress={() => goToStep(step + 1)}>
          <Text style={styles.navText}>{nextLabel?.(step) ?? 'Tiếp theo'}</Text>
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
                key={page.title}
                style={{ width: pageWidth }}
                contentContainerStyle={styles.page}
                importantForAccessibility={step === index ? 'auto' : 'no-hide-descendants'}
                accessibilityElementsHidden={step !== index}
                automaticallyAdjustKeyboardInsets
                keyboardShouldPersistTaps="handled"
                nestedScrollEnabled
              >
                <View style={styles.card}>
                  <Text accessibilityRole="header" style={styles.heading}>{page.title}</Text>
                  {page.content}
                </View>
              </ScrollView>
            ))}
          </ScrollView>
        )}
      </View>
    </View>
  );
}

export const formStyles = StyleSheet.create({
  button: { minHeight: 48, justifyContent: 'center', backgroundColor: Colors.primary, borderRadius: 12, padding: 12, alignItems: 'center' },
  buttonText: { color: Colors.white, fontWeight: '700' },
  bold: { color: Colors.textPrimary, fontWeight: '700' },
  subText: { fontSize: 13, color: Colors.textSecondary },
  badge: { alignSelf: 'flex-start', paddingHorizontal: 8, paddingVertical: 4, borderRadius: 999, overflow: 'hidden', fontSize: 12, fontWeight: '700', color: Colors.textSecondary, backgroundColor: Colors.grayLight },
  fileButton: { minHeight: 48, flexDirection: 'row', justifyContent: 'center', gap: 8, borderWidth: 1, borderColor: Colors.primary, borderStyle: 'dashed', borderRadius: 12, padding: 10, alignItems: 'center', backgroundColor: Colors.primaryLight },
  fileButtonText: { flexShrink: 1, color: Colors.primaryDark, fontWeight: '700' },
  fileStatus: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  fileSuccess: { flex: 1, color: Colors.successText, fontSize: 13, fontWeight: '600' },
});

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
});
