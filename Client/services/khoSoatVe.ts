import * as SQLite from 'expo-sqlite';
import * as SecureStore from 'expo-secure-store';
import * as Crypto from 'expo-crypto';
import { GoiKiemTra, KetQuaOffline, LuotDaQuet, xacThucNgoaiTuyen } from './soatVeOffline.mjs';
import apiClient from './api';

export async function moKho(userId: number) {
  const db = await SQLite.openDatabaseAsync(`smartbus_scan_${userId}.db`);
  if (!await db.getFirstAsync<{ cipher_version: string }>('PRAGMA cipher_version')) {
    await db.closeAsync();
    throw new Error('Cần development build có SQLCipher để lưu dữ liệu ngoại tuyến.');
  }
  const keyName = `smartbus_scan_key_${userId}`;
  let key = await SecureStore.getItemAsync(keyName);
  if (!key) {
    const bytes = await Crypto.getRandomBytesAsync(32);
    key = Array.from(bytes, x => x.toString(16).padStart(2, '0')).join('');
    await SecureStore.setItemAsync(keyName, key);
  }
  if (!/^[0-9a-f]{64}$/.test(key)) throw new Error('Khóa dữ liệu không hợp lệ.');
  await db.execAsync(`PRAGMA key = "x'${key}'";`);
  await db.execAsync('CREATE TABLE IF NOT EXISTS package (id INTEGER PRIMARY KEY CHECK(id=1), data TEXT NOT NULL); CREATE TABLE IF NOT EXISTS events (event_id TEXT PRIMARY KEY, ticket_id TEXT, time_step INTEGER, qr TEXT NOT NULL, reason TEXT NOT NULL, accepted INTEGER NOT NULL, scanned_at TEXT NOT NULL, session_id TEXT NOT NULL);');
  const columns = await db.getAllAsync<{ name: string }>('PRAGMA table_info(events)');
  if (!columns.some(c => c.name === 'package_id')) await db.execAsync('ALTER TABLE events ADD COLUMN package_id TEXT');
  if (!columns.some(c => c.name === 'synced')) await db.execAsync('ALTER TABLE events ADD COLUMN synced INTEGER NOT NULL DEFAULT 0');
  if (!columns.some(c => c.name === 'server_status')) await db.execAsync('ALTER TABLE events ADD COLUMN server_status TEXT');
  if (!columns.some(c => c.name === 'server_reason')) await db.execAsync('ALTER TABLE events ADD COLUMN server_reason TEXT');
  return db;
}

export async function luuGoi(db: SQLite.SQLiteDatabase, goi: GoiKiemTra) {
  await db.runAsync('INSERT OR REPLACE INTO package (id,data) VALUES (1,?)', JSON.stringify(goi));
}

export async function docGoi(db: SQLite.SQLiteDatabase): Promise<GoiKiemTra | null> {
  const row = await db.getFirstAsync<{ data: string }>('SELECT data FROM package WHERE id=1');
  return row ? JSON.parse(row.data) : null;
}

export async function quetVaLuu(db: SQLite.SQLiteDatabase, qr: string): Promise<KetQuaOffline> {
  let result: KetQuaOffline = { hopLe: false, maLyDo: 'LOI_LUU_TRU' };
  await db.execAsync('BEGIN IMMEDIATE');
  try {
    const row = await db.getFirstAsync<{ data: string }>('SELECT data FROM package WHERE id=1');
    if (!row) throw new Error('Hãy tải gói dữ liệu trước khi quét.');
    const goi: GoiKiemTra = JSON.parse(row.data);
    if (!goi.packageId) throw new Error('Gói cũ chưa hỗ trợ đồng bộ. Hãy tải lại gói.');
    const prior = await db.getAllAsync<LuotDaQuet>('SELECT ticket_id AS maVe, time_step AS buocThoiGian FROM events WHERE accepted=1');
    result = xacThucNgoaiTuyen(qr, goi, [...goi.daQuet, ...prior]);
    await db.runAsync('INSERT INTO events (event_id,ticket_id,time_step,qr,reason,accepted,scanned_at,session_id,package_id) VALUES (?,?,?,?,?,?,?,?,?)',
      Crypto.randomUUID(), result.maVe ?? null, result.buocThoiGian ?? null, qr.slice(0, 4096), result.maLyDo, result.hopLe ? 1 : 0, new Date().toISOString(), goi.phienId, goi.packageId);
    await db.execAsync('COMMIT');
  } catch (error) {
    await db.execAsync('ROLLBACK');
    throw error;
  }
  return result;
}

type PendingEvent = { event_id: string; package_id: string | null; qr: string; scanned_at: string; accepted: number };

export async function dongBoNhatKy(db: SQLite.SQLiteDatabase): Promise<{ sent: number; legacy: number; rejected: number; warnings: number; pendingErrors: number }> {
  const events = await db.getAllAsync<PendingEvent>('SELECT event_id, package_id, qr, scanned_at, accepted FROM events WHERE synced=0 ORDER BY rowid');
  let sent = 0, legacy = 0, rejected = 0, warnings = 0, pendingErrors = 0;
  for (const event of events) {
    if (!event.package_id) { legacy++; continue; }
    let response;
    try {
      response = await apiClient.post('/api/SoatVe/dong-bo-su-kien', {
        maSuKien: event.event_id, goiId: event.package_id, qr: event.qr,
        thoiDiemQuet: event.scanned_at, chapNhanTaiThietBi: event.accepted === 1,
      });
    } catch (error: any) {
      if (error.statusCode === 400 || error.statusCode === 409) {
        pendingErrors++;
        // Cập nhật trạng thái lỗi để không làm kẹt hàng đợi của các sự kiện tiếp theo
        await db.runAsync(
          'UPDATE events SET synced=2, server_status=?, server_reason=? WHERE event_id=?',
          'Loi',
          error.message || `HTTP_${error.statusCode}`,
          event.event_id
        );
        continue;
      }
      throw error;
    }
    if (response.data.statusCode !== 200 || response.data.data.maSuKien !== event.event_id)
      throw new Error('Máy chủ chưa xác nhận sự kiện.');
    const { trangThai, maLyDo } = response.data.data;
    await db.runAsync('UPDATE events SET synced=1, server_status=?, server_reason=? WHERE event_id=?', trangThai, maLyDo, event.event_id);
    if (event.accepted === 1 && trangThai !== 'ChapNhan') rejected++;
    if (maLyDo === 'STALE_REVOCATION') warnings++;
    sent++;
  }
  return { sent, legacy, rejected, warnings, pendingErrors };
}

export async function layThongKeDongBo(db: SQLite.SQLiteDatabase): Promise<{
  choXuLy: number;
  biLoi: number;
  daDongBo: number;
  danhSachLoi: Array<{ event_id: string; server_reason: string | null; scanned_at: string }>;
}> {
  const rows = await db.getAllAsync<{ event_id: string; synced: number; server_reason: string | null; scanned_at: string }>(
    'SELECT event_id, synced, server_reason, scanned_at FROM events WHERE synced != 1'
  );
  const choXuLy = rows.filter(r => r.synced === 0).length;
  const biLoi = rows.filter(r => r.synced === 2).length;
  const daDongBoRow = await db.getFirstAsync<{ count: number }>('SELECT count(*) as count FROM events WHERE synced = 1');
  return {
    choXuLy,
    biLoi,
    daDongBo: daDongBoRow?.count ?? 0,
    danhSachLoi: rows.filter(r => r.synced === 2).map(r => ({
      event_id: r.event_id,
      server_reason: r.server_reason,
      scanned_at: r.scanned_at,
    })),
  };
}

export async function datLaiSuKienLoi(db: SQLite.SQLiteDatabase): Promise<void> {
  await db.runAsync('UPDATE events SET synced=0 WHERE synced=2');
}
