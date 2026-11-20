import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';
import { generateSync } from 'otplib';
import { toByteArray } from 'base64-js';
import { VeDienTu } from './veService';

const indexKey = (userId: number) => `smartbus_wallet_${userId}`;
const ticketKey = (userId: number, id: string) => `smartbus_ticket_${userId}_${id}`;

export function taoQrOffline(ve: VeDienTu, now: number = Date.now()): string {
  const token = generateSync({ secret: toByteArray(ve.biMatTotp), algorithm: 'sha256', digits: 8, period: 30, epoch: Math.floor(now / 1000) });
  return `SMARTBUS:v1:${ve.maVe}:${token}:${ve.chuKyBase64}`;
}

export async function luuViOffline(userId: number, tickets: VeDienTu[]): Promise<void> {
  if (Platform.OS === 'web') return;
  const oldIds: string[] = JSON.parse(await SecureStore.getItemAsync(indexKey(userId)) || '[]');
  for (const ticket of tickets) await SecureStore.setItemAsync(ticketKey(userId, ticket.maVe), JSON.stringify(ticket));
  await SecureStore.setItemAsync(indexKey(userId), JSON.stringify(tickets.map(ticket => ticket.maVe)));
  for (const id of oldIds.filter(id => !tickets.some(ticket => ticket.maVe === id))) await SecureStore.deleteItemAsync(ticketKey(userId, id));
}

export async function docViOffline(userId: number): Promise<VeDienTu[]> {
  if (Platform.OS === 'web') return [];
  const ids: string[] = JSON.parse(await SecureStore.getItemAsync(indexKey(userId)) || '[]');
  const tickets: VeDienTu[] = [];
  for (const id of ids) {
    const raw = await SecureStore.getItemAsync(ticketKey(userId, id));
    if (raw) {
      const ticket: VeDienTu = JSON.parse(raw);
      if (ticket.trangThai === 'HoatDong' && Date.parse(ticket.hetHanLuc) > Date.now()) tickets.push(ticket);
    }
  }
  return tickets;
}
