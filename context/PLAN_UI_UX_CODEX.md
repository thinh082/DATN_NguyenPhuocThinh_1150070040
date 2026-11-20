# Kế hoạch nâng cấp UI/UX SmartBus cho Codex (Tone Xanh dương – Xám – Trắng)

> **Hướng dẫn cho Codex**: Sử dụng skill `ui-ux-pro-max` (`~/.agents/skills/ui-ux-pro-max`) làm chuẩn thiết kế UI/UX, thực hiện nâng cấp giao diện cho **Mobile (`Client/`)** và đồng bộ màu sắc cho **Web Admin (`Admin/`)** theo đúng bảng màu **Xanh dương – Xám – Trắng** của SmartBus.

---

## 1. Quy tắc bắt buộc (Không phá vỡ nghiệp vụ)

- **CHỈ sửa UI/UX (layout, StyleSheet, component hiển thị, icon, CSS)**.
- **KHÔNG sửa** bất kỳ logic nghiệp vụ, API endpoint, payload, hoặc luồng mật mã/offline nào trong:
  - `Client/services/api.ts`, `khoSoatVe.ts`, `soatVeOffline.mjs`, `viVeOffline.ts`, `veService.ts`, `vanHanhService.ts`, `xacThucService.ts`, `nguoiDungService.ts`
  - Các hook bảo mật & phần cứng: `ScreenCapture.preventScreenCaptureAsync`, `Brightness.setBrightnessAsync`, `useAudioPlayer`, `CameraView`, chu kỳ đếm ngược TOTP 30s.
- **Tương thích Expo Go & Android build**: Chỉ dùng thư viện đã có hoặc `@expo/vector-icons` (có sẵn trong Expo SDK 58). Không cài thêm thư viện native đòi rebuild phức tạp.
- **Thay thế emoji thô** (`🚌`, `📦`, `⚠️`, `ℹ️`, `✓`, `✕`) bằng icon vector sắc nét (`Ionicons` hoặc `MaterialCommunityIcons` từ `@expo/vector-icons`).

---

## 2. Hệ thống Design Tokens (Tone Xanh dương – Xám – Trắng)

Cập nhật chuẩn hóa trong `Client/constants/colors.ts` (giữ nguyên các key cũ để không vỡ import, bổ sung thêm token spacing/radius/shadow nếu cần):

| Nhóm | Token | Mã màu | Mục đích |
| --- | --- | --- | --- |
| **Primary Blue** | `primary` | `#0066CC` | Nút chính, header, active tab, điểm nhấn thương hiệu |
| **Primary Dark** | `primaryDark` | `#004C99` | Trạng thái nhấn, header gradient/sâu, login banner |
| **Primary Light** | `primaryLight` / `lightBlue` | `#E6F0FA` | Nền badge, icon container, item đang chọn |
| **Accent Blue** | `accent` | `#1A85FF` | Thanh tiến độ QR 30s, viền focus input |
| **Neutral White** | `white` / `surface` / `card` | `#FFFFFF` | Thẻ nội dung, modal, vùng hiển thị mã QR |
| **Neutral Gray** | `background` | `#F4F6F9` | Nền tổng thể các màn hình |
| **Neutral Subtle** | `grayLight` | `#EDF2F7` | Nền input phụ, chip, track progress |
| **Border** | `border` | `#E2E8F0` | Viền thẻ 1px sắc nét, đường ngăn cách |
| **Text Primary** | `textPrimary` | `#0F172A` | Tiêu đề, số tiền, nội dung chính (tương phản cao) |
| **Text Secondary** | `textSecondary` | `#475569` | Nhãn, mô tả phụ |
| **Text Muted** | `textMuted` | `#64748B` | Chú thích, thời gian, placeholder |
| **Semantic** | `success` / `warning` / `danger` | `#16A34A` / `#D97706` / `#DC2626` | Vé hợp lệ / cảnh báo offline / từ chối & lỗi |

**Nguyên tắc hình khối & chữ**:
- Bo góc nhất quán: Card `16px`, Button/Input `12px`, Badge/Pill `999px`.
- Vùng chạm tối thiểu của nút bấm: chiều cao `>= 46px`.
- Bóng đổ nhẹ (subtle elevation) kết hợp viền `1px solid #E2E8F0` tạo cảm giác sạch, hiện đại kiểu app giao thông đô thị.

---

## 3. Kế hoạch chỉnh sửa theo từng màn hình

### Giai đoạn 1 — Mobile Hành khách (`Client/App.tsx`, `DangKyScreen.tsx`, `QuetQrXeScreen.tsx`, `ThanhToanMoPhongModal.tsx`, `HoSoHanhKhach.tsx`, `TuyenVaLichSu.tsx`)

1. **Màn hình Đăng nhập (`Client/App.tsx`) & Đăng ký (`Client/DangKyScreen.tsx`)**
   - Header thương hiệu SmartBus gọn gàng với icon xe buýt trong khung bo tròn xanh dương `#0066CC`, tiêu đề rõ ràng.
   - Form nằm trong card trắng nổi trên nền xám nhạt `#F4F6F9`, input có icon dẫn đầu, viền focus xanh `#0066CC`, nút chính full-width cao `48px`.
   - Hiển thị lỗi dạng banner đỏ nhạt có icon cảnh báo rõ ràng.

2. **Trang chủ Hành khách (`Client/App.tsx`)**
   - Header xanh dương `#0066CC` bo góc đáy mềm (`borderBottomLeftRadius/RightRadius: 24`), hiển thị lời chào + badge trạng thái mạng (`Trực tuyến` / `Ngoại tuyến`) + nút đăng xuất gọn gàng.
   - **Thẻ CTA nổi bật nhất**: "Quét QR trên xe mua vé nhanh" (nền trắng nổi lên trên rìa header hoặc card nhấn xanh đậm) + "Mở ví vé QR của tôi".
   - **Lưới chức năng 2 cột** (card trắng, viền `#E2E8F0`, icon trong vòng tròn `#E6F0FA`): Ví vé QR, Quét QR xe, Tra cứu tuyến, Lịch sử lượt đi, Hồ sơ ưu tiên.

3. **Ví vé & Hiển thị QR động (`Client/App.tsx` — màn `taoQr`)**
   - Thiết kế thẻ vé theo dạng **Boarding Pass / Thẻ đi xe điện tử**:
     - Phần trên (nền `#0066CC`): Tên loại vé (Vé lượt / Vé tháng), tên tuyến, badge trạng thái (`Đang hoạt động`).
     - Đường cắt răng cưa / nét đứt (`dashed`) phân tách thân vé.
     - Phần giữa (nền trắng tuyệt đối `#FFFFFF` padding rộng để camera dễ bắt nét): Mã QR cỡ lớn chính giữa.
     - **Thanh đếm ngược 30 giây dạng Progress Bar** (`width` co dần theo `demLui / 30`) + dòng chữ nhỏ `"Tự động làm mới sau Xs • Chống chụp màn hình"`.
   - Danh sách chọn vé phía trên/dưới dạng horizontal pill/card dễ chuyển đổi giữa nhiều vé.
   - Phần mua vé mới (biểu giá) trình bày dạng card rõ giá gốc, miễn giảm (nếu có) và giá thực thu nổi bật màu `#0066CC`.

4. **Quét QR xe mua vé nhanh (`Client/QuetQrXeScreen.tsx`)**
   - Top bar trong suốt tối nhẹ, nút đóng dạng pill rõ ràng.
   - Khung ngắm QR 4 góc bo tròn màu xanh dương sáng `#1A85FF`, hướng dẫn đặt trong thanh glass/dark pill phía dưới dễ đọc trên nền camera.

5. **Modal Thanh toán mô phỏng (`Client/ThanhToanMoPhongModal.tsx`)**
   - Chuyển sang kiểu **Bottom Sheet / Card hiện đại**, tóm tắt đơn hàng theo bảng 2 cột căn lề phải giá trị, tổng tiền nổi bật cỡ `22px` màu `#0066CC`.
   - 3 nút kết quả rõ phân cấp:
     - `Thanh toán thành công` (nút chính nền `#0066CC`, chữ trắng)
     - `Mô phỏng thất bại` (nút phụ viền đỏ nhạt `#DC2626`)
     - `Hủy đơn hàng` (nút text/outline xám `#64748B`).

6. **Hồ sơ ưu tiên (`Client/HoSoHanhKhach.tsx`) & Tuyến/Lịch sử (`Client/TuyenVaLichSu.tsx`)**
   - Card thông tin cá nhân & hồ sơ ưu tiên có badge trạng thái rõ màu (`Chờ duyệt`, `Đã duyệt`, `Từ chối`).
   - Danh sách tuyến xe có mã tuyến đặt trong pill vuông màu `#0066CC` (giống biển số tuyến xe buýt thật), lộ trình trạm hiển thị dạng timeline dọc chấm tròn nối đường thẳng.
   - Lịch sử lượt đi dạng card gọn với giờ quét, biển số xe, tuyến và badge chế độ (`Online` / `Offline`).

---

### Giai đoạn 2 — Mobile Nhân viên soát vé (`Client/SoatVeScreen.tsx`)

1. **Header & Trạng thái phiên**
   - Thanh trạng thái kết nối rõ ràng (`Đang trực tuyến` xanh lá / `Chế độ ngoại tuyến` cam).
   - Khối chọn Thiết bị & Chuyến xe dạng chip/card 1 chạm có viền xanh `#0066CC` khi được chọn.
2. **Màn hình Soát vé & Phản hồi tức thì**
   - **Thẻ trạng thái kết quả quét nổi bật** (màu nền xanh lá nhạt + icon check lớn khi hợp lệ; nền đỏ nhạt + icon X lớn khi từ chối) để nhân viên nhìn lướt là biết ngay trên xe đông.
   - **Hàng đợi đồng bộ offline** hiển thị gọn trong 1 thanh summary card (Số lượt chờ gửi / lỗi) kèm nút `Đồng bộ ngay` nổi bật.
   - Nút **Mở camera quét vé** to, rõ (cao `54px`, màu `#0066CC`), đặt ngay tầm ngón cái.
   - Nhóm nút phụ: `Tra cứu khẩn cấp (CCCD/SĐT)`, `Đồng bộ lượt quét`, `Kết thúc phiên` xếp gọn gàng, phân biệt rõ nút nguy hiểm/kết thúc.
3. **Modal Tra cứu khẩn cấp & Khóa vé**
   - Ô tìm kiếm CCCD/SĐT có nút xóa nhanh, kết quả hành khách + danh sách vé hiển thị rõ nút `Xác nhận lên xe` (xanh dương) và `Tạm khóa vé` (viền đỏ).

---

### Giai đoạn 3 — Đồng bộ Web Admin sang Tone Xanh dương – Xám – Trắng (`Admin/src/style.css`)

Hiện `Admin/src/style.css` đang dùng tone xanh lục (`#16836f` / `#1ba98e` / `#153d31`). Chỉnh lại bảng màu trong `Admin/src/style.css` để **đồng nhất 100% thương hiệu SmartBus với Mobile**:
- Màu chủ đạo `#16836f` / `#17856f` → `#0066CC` (hover `#004C99`).
- Màu nhấn `#1ba98e` / `#37aa90` → `#1A85FF`.
- Nền active sidebar `#e8f5f1` → `#E6F0FA`, chữ active `#12816d` → `#0066CC`.
- Nền trang `#f5f7f6` → `#F4F6F9`, viền `#e7eeeb` → `#E2E8F0`.
- Banner trang đăng nhập Admin `#153d31` → `#003B73` (gradient nhẹ sang `#0066CC`).
- Giữ nguyên màu ngữ nghĩa cho badge trạng thái (xanh lá cho hoạt động/thành công, cam cho chờ duyệt, đỏ cho cảnh báo/khóa).

---

## 4. Kiểm tra sau khi sửa (Bắt buộc chạy)

```powershell
# 1. Kiểm tra TypeScript & unit test thuật toán offline của Mobile
cd Client
npx tsc --noEmit
node --test tests/soatVeOffline.test.mjs

# 2. Kiểm tra build production của Web Admin
cd ..\Admin
npm run build
```

**Tiêu chí hoàn thành**:
- `tsc --noEmit`, `soatVeOffline.test.mjs`, và `Admin npm run build` đều pass 0 lỗi.
- Toàn bộ Mobile & Web Admin đồng nhất tone **Xanh dương (`#0066CC`) – Xám (`#F4F6F9` / `#E2E8F0`) – Trắng (`#FFFFFF`)**.
- Không thay đổi bất kỳ hành vi API, SQLCipher, TOTP, ECDSA hay `FLAG_SECURE` nào.
