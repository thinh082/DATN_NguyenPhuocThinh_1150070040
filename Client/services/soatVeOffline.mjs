import { p256 } from '@noble/curves/nist.js';
import { toByteArray } from 'base64-js';
import { generateSync } from 'otplib';

const fail = maLyDo => ({ hopLe: false, maLyDo });

export function xacThucNgoaiTuyen(qr, goi, daQuet, now = Date.now()) {
  try {
    if (typeof qr !== 'string' || qr.length > 4096) return fail('QR_KHONG_HOP_LE');
    const parts = qr.split(':');
    if (parts.length !== 5 || parts[0] !== 'SMARTBUS' || parts[1] !== 'v1' || !/^[0-9a-f-]{36}$/i.test(parts[2]) || !/^\d{8}$/.test(parts[3])) return fail('QR_KHONG_HOP_LE');
    if (!Number.isFinite(Date.parse(goi.hetHanLuc)) || now >= Date.parse(goi.hetHanLuc)) return fail('GOI_HET_HAN');
    const ve = goi.ves.find(x => x.maVe.toLowerCase() === parts[2].toLowerCase());
    if (!ve) return fail('KHONG_CO_DU_LIEU');
    if (ve.trangThai !== 'HoatDong') return fail('VE_BI_KHOA');
    const signedBytes = toByteArray(ve.duLieuDaKy);
    const spki = toByteArray(ve.khoaCongKhai);
    const signature = toByteArray(parts[4]);
    if (spki.length !== 91 || spki[26] !== 4 || signature.length !== 64 || !p256.verify(signature, signedBytes, spki.slice(26), { lowS: false })) return fail('SAI_CHU_KY');
    const signed = JSON.parse(Array.from(signedBytes, byte => String.fromCharCode(byte)).join(''));
    if (signed.v !== 1 || signed.mv.toLowerCase() !== parts[2].toLowerCase()) return fail('SAI_DU_LIEU');
    const start = Date.parse(signed.tu), end = Date.parse(signed.den);
    if (!Number.isFinite(start) || !Number.isFinite(end) || now < start || now >= end) return fail('VE_HET_HAN');
    if (signed.tx != null && signed.tx !== goi.tuyenXeId) return fail('SAI_TUYEN');
    const current = Math.floor(now / 30000);
    const secret = toByteArray(ve.biMatTotp);
    const matched = [current, current - 1, current + 1].find(step => step >= 0 && generateSync({ secret, algorithm: 'sha256', digits: 8, period: 30, epoch: step * 30 }) === parts[3]);
    if (matched === undefined) return fail('SAI_TOTP');
    if (daQuet.some(x => x.maVe?.toLowerCase() === parts[2].toLowerCase() && (signed.lv === 'VeLuot' || x.buocThoiGian === matched))) return fail('DA_SU_DUNG');
    return { hopLe: true, maLyDo: 'HOP_LE', maVe: parts[2].toLowerCase(), buocThoiGian: matched, maLoaiVe: signed.lv };
  } catch { return fail('QR_KHONG_HOP_LE'); }
}
