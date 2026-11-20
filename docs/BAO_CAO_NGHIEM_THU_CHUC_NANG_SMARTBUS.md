# BÁO CÁO NGHIỆM THU & BÀN GIAO TOÀN DIỆN HỆ THỐNG SMARTBUS

**Dự án:** Hệ thống vé xe buýt thông minh SmartBus (QR động, Chữ ký số ECDSA & Soát vé ngoại tuyến)  
**Căn cứ đặc tả:** [context/spec/SMARTBUS_SPEC_CHUC_NANG_CON_THIEU.md](file:///D:/mon%20hoc/DoAnTN/Thinh/context/spec/SMARTBUS_SPEC_CHUC_NANG_CON_THIEU.md)  
**Kế hoạch thực hiện:** [plan.md](file:///C:/Users/ADMIN/.gemini/antigravity-cli/brain/23cca3d2-e4ae-4308-aa5f-693092b84264/plan.md)  
**Nhánh Git:** `codex/smartbus-completion` tại thư mục `Project/`  
**Ngày hoàn thành:** 15/11/2026  
**Trạng thái nghiệm thu:** **HOÀN THÀNH 100% (7/7 Giai đoạn)**  

---

## 1. TỔNG QUAN KẾT QUẢ TRIỂN KHAI

| Giai đoạn | Hạng mục | Mức độ ưu tiên | Trạng thái |
| :---: | :--- | :---: | :---: |
| **1** | Mua vé, Thanh toán mô phỏng (Mock Payment 3 trạng thái) & Mua nhanh QR xe | **P0** | **ĐÃ XONG** |
| **2** | Tra cứu khẩn cấp 2 bước, Khóa vé nhân viên & Audit trail | **P0** | **ĐÃ XONG** |
| **3** | Phục hồi phiên ngoại tuyến, Bảo vệ màn hình QR & Đồng bộ an toàn | **P0** | **ĐÃ XONG** |
| **4** | Web Admin: Lộ trình 1..N, Chuyến xe, Mở khóa vé, Báo cáo & Xuất CSV an toàn | **P1** | **ĐÃ XONG** |
| **5** | Tải tệp minh chứng ưu tiên (Magic bytes, 5MB, bảo mật quyền & URL cũ) | **P1** | **ĐÃ XONG** |
| **6** | CSDL Demo idempotent, Redis Cache (Fallback SQL) & Cấu hình môi trường | **P1** | **ĐÃ XONG** |
| **7** | Kiểm thử tổng thể tự động (Server 37 tests, Client, Admin) & Bàn giao | - | **ĐÃ XONG** |

---

## 2. CHI TIẾT CÁC TÍNH NĂNG ĐÃ HOÀN THIỆN

### 2.1. Giai đoạn 1 (P0): Mua vé, Thanh toán Mô phỏng & QR dán xe
- **Mua vé linh hoạt:** Ví vé luôn hiển thị nút "Mua vé mới" bất kể trong ví đã có vé hay chưa.
- **Nhận diện xe buýt (`GET /api/VanHanh/quet-qr-dan-xe`):**
  - Quét mã QR tĩnh dán trên xe (định dạng GUID).
  - Server xác thực: Xe phải đang hoạt động và có **duy nhất 1 chuyến `DangChay`**. Nếu không có chuyến hoặc xung đột dữ liệu, trả lỗi tiếng Việt rõ ràng, không tự suy đoán.
- **Idempotency & Giao dịch nguyên tử:**
  - `POST /api/Ve/tao-don-hang`: Sinh `maYeuCau` một lần. Nếu gửi lại cùng nội dung $\rightarrow$ trả lại đơn cũ; nếu khác nội dung $\rightarrow$ trả HTTP 409 Conflict.
  - Số lượng vé giới hạn 1–10. Ràng buộc đối tượng ưu tiên phải `DaDuyet` và còn hiệu lực ngày hôm nay.
- **Thanh toán mô phỏng (`POST /api/Ve/thanh-toan-gia-lap`):**
  - Nhận `{ donHangId, requestId, ketQua: "ThanhCong" | "ThatBai" | "Huy" }`.
  - State machine chuẩn hóa: Chỉ khi `ThanhCong` mới phát hành vé (ký số ECDSA P-256 + mã hóa AES-GCM TOTP secret trong transaction). Nhánh 0đ tạo giao dịch 0đ và phát hành vé nguyên tử.

### 2.2. Giai đoạn 2 (P0): Tra cứu Khẩn cấp & Khóa vé Nhân viên
- **Tách quy trình 2 bước an toàn:**
  - **Bước 1 (Tìm kiếm - `POST /api/SoatVe/tra-cuu-khan-cap`):** Nhập CCCD (12 số) hoặc SĐT. Chỉ nhân viên có phiên soát vé mở mới được tra cứu. **Không tạo lượt đi**. Trả thông tin đã che mờ (ví dụ `079*****1234`), không lộ TOTP secret hay chữ ký.
  - **Bước 2 (Xác nhận - `POST /api/SoatVe/xac-nhan-thu-cong`):** Sau khi nhân viên đối chiếu trực tiếp hành khách, chọn "Xác nhận lượt". Ghi nhận `LuotDi` (phuongThuc = "ThuCong") và nhật ký trong transaction.
- **Chống quét trùng / xác nhận trùng:**
  - Vé lượt chỉ được dùng 1 lần duy nhất.
  - Vé tháng bị chặn xác nhận 2 lần trong cùng cửa sổ 30 giây (step TOTP).
- **Khóa vé nghi ngờ gian lận (`POST /api/Ve/khoa-ve`):**
  - Nhân viên có quyền chuyển vé sang `TamKhoa` kèm bắt buộc nhập lý do.
  - Ghi vết kiểm toán vào `NhatKyThuHoiVe` với `nguoiThucHienId` từ JWT.

### 2.3. Giai đoạn 3 (P0): Phiên, Ví vé & Vận hành Ngoại tuyến
- **Khôi phục phiên offline an toàn:**
  - Khởi động online: Kiểm tra token với server, cập nhật vai trò; nếu 401/403 xóa phiên sạch sẽ.
  - Khởi động offline: Chỉ khôi phục phiên nếu lần online gần nhất **$\le$ 24 giờ** và JWT `exp` còn hạn. Hiển thị banner cảnh báo: *"⚠️ Ngoại tuyến · trạng thái có thể chưa cập nhật"*.
  - Logout: Xóa token và ví vé của tài khoản đó, nhưng bảo toàn nguyên vẹn hàng đợi soát vé SQLite `smartbus_scan_${userId}.db`.
- **Bảo vệ màn hình QR:**
  - Tích hợp `expo-screen-capture`: Bật chống chụp màn hình khi xem QR vé, tắt khi rời màn hình/logout.
  - Quản lý độ sáng (`expo-brightness`): Tăng sáng tối đa khi xem vé, khôi phục mức sáng cũ khi thoát/unmount.
  - Tự động làm mới TOTP mỗi giây và ngay khi app chuyển từ background về foreground.
- **Đồng bộ sự kiện & Khử lỗi:**
  - `POST /api/SoatVe/dong-bo-su-kien`: Server ACK từng sự kiện. Lỗi 400/409 ở một sự kiện ghi nhận `synced = 2` và không làm chặn các sự kiện khác.
  - Giao diện soát vé có thống kê hàng đợi ngoại tuyến và nút "Thử lại sự kiện lỗi".
  - `SoatVeService.cs`: Đồng bộ quy tắc kiểm tra QR trực tuyến và ngoại tuyến (format `SMARTBUS:v1:...`, kiểm tra tuyến `SAI_TUYEN`, cửa sổ `[-1, 0, +1]`, chống quét trùng `QUET_TRUNG`).

### 2.4. Giai đoạn 4 (P1): Hoàn thiện Web Admin
- **Quản lý lộ trình & trạm dừng (`/api/Admin/lo-trinh`):**
  - Tạo/sửa lộ trình: Tuyến xe, chiều `Di`/`Ve`, thứ tự trạm dừng 1..N liên tục, tối thiểu 2 trạm, không lặp trạm.
  - Cơ chế phiên bản hiệu lực: Tự động đóng phiên bản cũ có hiệu lực mở vô hạn khi tạo phiên bản mới, không sửa đè lịch sử của lộ trình đã có chuyến sử dụng.
- **Quản lý chuyến xe:**
  - Chỉnh sửa và Hủy chuyến xe dự kiến (`trangThai = "Huy"`), kiểm tra xe không bị trùng giờ.
- **Mở khóa vé (`POST /api/Admin/ve/{maVe}/mo-khoa`):**
  - Cho phép mở khóa vé từ `TamKhoa` $\rightarrow$ `HoatDong` kèm lý do và ghi `NhatKyThuHoiVe`.
- **Báo cáo tổng hợp & Xuất CSV:**
  - Dashboard lọc theo khoảng ngày UTC (giờ Việt Nam) và Tuyến xe.
  - Xuất CSV hỗ trợ UTF-8 BOM, escape dấu ngoặc kép và **ngăn chặn triệt để CSV Formula Injection** (thêm nháy đơn trước `=`, `+`, `-`, `@`).
  - Phân trang server-side (`trang`, `soLuong`) trên các bảng dữ liệu.
  - Nút tải ảnh PNG và in mã QR cho xe buýt.

### 2.5. Giai đoạn 5 (P1): Tải tệp Minh chứng Ưu tiên
- **Backend Upload & Bảo mật File:**
  - `POST /api/XacThuc/tai-len-minh-chung`: Nhận file JPEG, PNG, PDF; dung lượng tối đa 5 MiB.
  - Kiểm tra Magic Bytes thực tế (`0xFF, 0xD8` cho JPEG; `0x89, 0x50, 0x4E, 0x47` cho PNG; `%PDF` cho PDF).
  - Tên file UUID ngẫu nhiên, lưu trữ tại thư mục cấu hình ngoài web root (`App_Data/MinhChung/`).
  - Entity & Bảng `TepMinhChung` lưu metadata tệp.
- **Phân quyền truy cập tệp (`GET /api/XacThuc/minh-chung/{maTep}`):**
  - Bắt buộc xác thực JWT. Chỉ chủ sở hữu tệp hoặc `QuanTriVien` mới được xem/tải.
  - Chống Path Traversal và trả về `PhysicalFile`.
- **Nộp hồ sơ ưu tiên linh hoạt:**
  - Chấp nhận cả URL HTTP(S) cũ lẫn đường dẫn tải tệp nội bộ `/api/XacThuc/minh-chung/{maTep}`.
  - Kiểm tra quyền sở hữu: Người nộp hồ sơ chỉ được gắn file do chính mình upload.
- **Frontend Client & Admin:**
  - App Mobile: Tích hợp `expo-document-picker` chọn ảnh/PDF minh chứng trực tiếp, tự tải lên và điền đường dẫn.
  - Web Admin: Xem minh chứng an toàn thông qua Bearer token (fetch blob $\rightarrow$ object URL), mở tab mới bảo mật.

### 2.6. Giai đoạn 6 (P1): CSDL Demo, Redis Cache & Cấu hình Môi trường
- **Redis Cache & Fallback SQL Server:**
  - `ICacheService` & `CacheService` tích hợp `StackExchange.Redis`.
  - Cache danh sách tuyến xe (`smartbus:cache:tuyen-xe:tat-ca`) và biểu giá vé (`smartbus:cache:bieu-gia:{tuyenXeId}`) với TTL 5 phút.
  - Invalidate cache tự động khi Admin/Service lưu tuyến xe, trạm dừng, lộ trình hoặc cập nhật biểu giá.
  - **Cơ chế Fallback an toàn:** Nếu Redis không khả dụng hoặc mất kết nối, hệ thống tự động đọc trực tiếp từ SQL Server mà không làm gián đoạn nghiệp vụ.
- **Cấu hình Client đa môi trường:**
  - Loại bỏ URL ngrok cố định trong `Client/services/api.ts`.
  - Hỗ trợ biến môi trường `EXPO_PUBLIC_API_BASE_URL` với fallback: Android Emulator (`http://10.0.2.2:5000`), Web/Desktop (`http://localhost:5000`).
  - Tạo file `.env.example` và bổ sung `.env` vào `.gitignore`.
- **Seed CSDL Idempotent & Key Setup Tool:**
  - `DuLieuKhoiTao.cs`: Khởi tạo mẫu 3 vai trò tài khoản, Tuyến 01 & Tuyến 150 (trạm dừng và lộ trình 2 chiều 1..N), xe buýt với mã QR GUID cố định, chuyến xe đang chạy & dự kiến, biểu giá vé lượt & vé tháng đầy đủ.
  - `tools/SmartBus.KeySetup`: Tự động khởi tạo và đăng ký `JwtSettings:SecretKey`, `Security:TotpEncryptionKey` (AES-256) và `Security:SigningPrivateKey` (ECDSA P-256) vào user-secrets và CSDL, đảm bảo chạy lần 2 không làm đổi khóa đang dùng.

---

## 3. BẰNG CHỨNG KIỂM THỬ TỰ ĐỘNG

### 3.1. Server Unit Tests (xUnit)
```text
Test run for Project/Server.Tests/bin/Debug/net8.0/Server.Tests.dll (.NETCoreApp,Version=v8.0)
Passed! - Failed: 0, Passed: 37, Skipped: 0, Total: 37, Duration: 1 s
```
*Danh sách 37 tests bao gồm:*
- `AdminAccountTests`
- `AdminManagementTests` (4 tests: Lộ trình 1..N trạm, Chống chồng lấn hiệu lực, Hủy chuyến xe dự kiến, Mở khóa vé TamKhoa)
- `AdminTripTests`
- `AuthorizationTests`
- `EmergencyLookupAndInspectionTests` (5 tests: Tra cứu không tăng lượt, Xác nhận lượt thành công, Chống quét trùng vé lượt/tháng, Khóa vé nghi ngờ)
- `MatMaTests`
- `MockPaymentAndOrderTests` (6 tests: Idempotency maYeuCau, State machine thanh toán, Giao dịch 0đ, Xác thực QR xe)
- `OfflineQrValidationTests`
- `OfflineSyncIntegrationTests`
- `OnlineQrValidationTests` (2 tests: Xác thực QR trực tuyến, Cửa sổ TOTP, Sai tuyến, Quét trùng)
- `PassengerAccountTests`
- `PaymentIntegrationTests`
- `RegistrationIntegrationTests`
- `RouteDetailsTests`
- `SigningKeyTests`
- `ProofUploadAndPriorityTests` (2 tests: Upload JPEG/PDF hợp lệ & phân quyền chủ sở hữu/Admin, Chặn file giả mạo magic bytes & quá cỡ 5MB)
- `CacheAndFallbackTests` (3 tests: Fallback khi Redis không khả dụng, Cache hit & Invalidate tuyến xe, Cache biểu giá)

### 3.2. Client Verification
- **TypeScript Typecheck:** `npx tsc --noEmit` $\rightarrow$ **0 errors (PASS)**.
- **Offline Validation Vectors:** `node --test tests/soatVeOffline.test.mjs` $\rightarrow$ **1/1 subtest PASS** (Xác thực vé ký số ECDSA IEEE P1363, từ chối vé giả mạo, vé hết hạn và quét trùng).

### 3.3. Admin Build Verification
- **Vite Production Build:** `npm run build` $\rightarrow$ **Thành công (0 errors)**.
```text
vite building client environment for production...
✓ 2085 modules transformed.
dist/index.html                   0.40 kB
dist/assets/index-DKBKjrIL.css   12.42 kB
dist/assets/index-z1U53rxw.js   288.44 kB
✓ built in 908ms
```

---

## 4. HƯỚNG DẪN VẬN HÀNH & DEMO

### 4.1. Thiết lập CSDL & Khóa bảo mật ban đầu
1. Chạy SQL Server local với cơ sở dữ liệu `SmartBusDb`.
2. Khởi tạo khóa bảo mật (chạy từ thư mục `Project/`):
   ```bash
   dotnet run --project tools/SmartBus.KeySetup
   ```
   Lệnh này tự động thiết lập user-secrets cho `JwtSettings:SecretKey`, `Security:TotpEncryptionKey`, `Security:SigningPrivateKey` và đăng ký public key vào database.

### 4.2. Khởi chạy Server
```bash
cd Project/Server
dotnet run
```
Server sẽ chạy tại `http://localhost:5000` (Swagger UI: `http://localhost:5000/swagger`). Dữ liệu demo sẽ được tự động seed (idempotent) khi khởi động.

### 4.3. Khởi chạy Web Admin
```bash
cd Project/Admin
npm run dev
```
Truy cập `http://localhost:5173`.  
Đăng nhập tài khoản Quản trị viên:
- **Tài khoản:** `admin@smartbus.vn`
- **Mật khẩu:** `123456`

### 4.4. Khởi chạy Client Mobile (Expo)
```bash
cd Project/Client
# Tạo file .env từ .env.example nếu cần đổi URL Backend
npx expo start
```
Tài khoản demo có sẵn:
- **Hành khách:** `thinh@smartbus.vn` / `123456`
- **Nhân viên soát vé:** `soatve01@smartbus.vn` / `123456`
- **Mã QR xe buýt demo để quét bằng camera:** `11111111-1111-1111-1111-111111111111` (Xe `51B-123.45`, Tuyến 01 đang chạy).
