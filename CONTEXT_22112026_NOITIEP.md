# CONTEXT NỐI TIẾP DỰ ÁN SMARTBUS (Cập nhật 22/11/2026 — 17:18)

> File đặt tại thư mục gốc ngang hàng với `HIS` để nạp nhanh cho agent/máy khác (laptop, agy cli, Codex, Gemini...). Đã gộp trọn bộ nội dung từ `context/TONG_HOP_CONTEXT_22-11.md` và toàn bộ các bước đã hoàn thiện thêm trong chiều 22/11/2026.

---

## 1. Thông tin Đồ án & Yêu cầu nộp bài
- **Tên đề tài**: **"XÂY DỰNG HỆ THỐNG XÁC THỰC VÀ QUẢN LÝ LƯỢT ĐI XE BUÝT SỬ DỤNG MÃ QR ĐỘNG, CHỮ KÝ SỐ VÀ CƠ CHẾ XÁC THỰC NGOẠI TUYẾN"**.
- **Sinh viên**: Nguyễn Phước Thịnh — MSSV: `1150070040` — Lớp: `11_ĐH_TMĐT` — Ngành: Hệ thống Thông tin — ĐH Tài nguyên và Môi trường TP.HCM (HCMUNRE).
- **GVHD**: ThS. Từ Thanh Trí (`tuthanhtri@hcmunre.edu.vn`).
- **Hạn nộp**: **Trước 23h00 ngày 22/11/2026 (tối nay)** — gửi **01 file `.zip`** qua email `dklien@hcmunre.edu.vn`, Cc `tuthanhtri@hcmunre.edu.vn`, gồm đủ **5 hạng mục** (chi tiết tại `context/YEU_CAU_NOP_DO_AN.md`):
  1. Đề cương đồ án đã duyệt (`.pdf`).
  2. Báo cáo Đồ án tốt nghiệp hoàn chỉnh (`.docx` **và** `.pdf`).
  3. Slide thuyết trình (`.pptx` và/hoặc `.pdf`).
  4. Video thuyết trình + demo sản phẩm thật (**tối đa 15 phút**).
  5. Link GitHub Source Code: `https://github.com/thinh082/DATN_NguyenPhuocThinh_1150070040` (có `README.md`, `Server/Database/`, dữ liệu mẫu, tài khoản kiểm thử, đảm bảo GVHD truy cập được).

### Định vị đề tài & Căn cứ pháp lý (đã kiểm chứng mới nhất 22/11/2026)
- **Nghị quyết `09/2026/NQ-HĐND`** (HĐND TP.HCM khóa XI thông qua 19/06/2026, hiệu lực **01/07/2026 – 31/12/2026**) **là văn bản mới nhất đang có hiệu lực**:
  - **Điều 3**: Hỗ trợ **100% giá vé** trên **109 tuyến** xe buýt được ngân sách hỗ trợ kinh phí hoạt động (Phụ lục 01).
  - **Điều 4**: Hỗ trợ **100% giá vé** trên **25 tuyến** xe buýt nội tỉnh không được ngân sách hỗ trợ kinh phí hoạt động và hoạt động ổn định (Phụ lục 02).
  - **Lộ trình 2 giai đoạn**: GĐ 1 (01/07–30/09/2026) miễn phí chưa bắt buộc định danh; GĐ 2 (01/10–31/12/2026) **bắt buộc xác thực/định danh lượt đi điện tử** để nghiệm thu, thanh quyết toán theo sản lượng thực tế và chống trục lợi chính sách (điểm b, c, d khoản 1 Điều 6).
- **Định vị SmartBus**: Phần mềm hỗ trợ doanh nghiệp/nhà xe xác thực hành khách (QR động TOTP 30s + chữ ký số **ECDSA P-256**, chống giả mạo/chia sẻ), ghi nhận lượt đi theo tuyến/chuyến kể cả **ngoại tuyến**, cung cấp dữ liệu kiểm chứng chống khai khống làm căn cứ cho cơ quan quản lý Nhà nước đối soát và cấp kinh phí trợ giá.

---

## 2. Kiến trúc, Môi trường & Tài khoản chạy Demo
- **`Server/`**: ASP.NET Core 8, EF Core, SQL Server (`THINHPC`, DB `SmartBusDb`, Windows Auth / SQL login `thinh08223` / `123`), Redis tùy chọn, Cloudinary.
  - Lệnh chạy: `dotnet run --project .\Server --launch-profile http -- --urls "http://0.0.0.0:5056"` (Swagger `/swagger`).
  - IP LAN PC hiện tại: `192.168.1.13` (Wi-Fi 4).
- **`Client/`**: React Native + Expo SDK 58 (`App.tsx` điều hướng theo state, không dùng Expo Router).
  - `Client/.env.local`: `EXPO_PUBLIC_API_BASE_URL=http://192.168.1.13:5056`.
  - Lệnh chạy: `cd Client; npx expo start`. *(Đọc `Client/AGENTS.md` trước khi sửa; không sửa tay `android/`/`ios/`).*
- **`Admin/`**: React + TypeScript + Vite (`Admin/src/main.tsx`), chạy `npm run dev` → `http://localhost:5173`.
- **`Server.Tests/`**: xUnit (**lưu ý: test tích hợp ghi vào DB thật `SmartBusDb`**).
- **Thiết bị demo**:
  - Máy ảo Android 12 (`emulator-5554`, AVD `SmartBus_API31`) → **App Hành khách**.
  - Điện thoại thật **nubia Z2472** (`LZ0A36VNYD5002773`, Android 15) → **App Soát vé**.
  - Nếu nubia báo "Unable to load script": `adb -s LZ0A36VNYD5002773 reverse tcp:8081 tcp:8081` rồi Reload.
- **Tài khoản demo (mật khẩu `123456`)**:
  - Quản trị viên: `admin@smartbus.vn`
  - Nhân viên soát vé: `soatve01@smartbus.vn`
  - Hành khách: `thinh@smartbus.vn` *(đã xác thực VNeID, có sẵn Thẻ miễn phí `Ve id 20009`)* — đồng thời có thể demo **Tạo tài khoản mới → Định danh VNeID → Cấp ngầm Thẻ miễn phí**.
- **Thứ tự script SQL DB (`Server/Database/`)**: chạy `01` → `08`, sau đó **chạy lại `05`** (các script `05`, `07`, `08` cần cờ `-I`).
- **Backup DB**: `E:\SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup\SmartBusDb_truoc_xac_thuc_20261122.bak`, `..._truoc_du_lieu_co_dau_20261122.bak`, `..._truoc_tuyen_mien_phi_20261122.bak`.

---

## 3. Các chức năng phần mềm đã hoàn thiện (22/11/2026)
1. **Định danh hành khách & Địa chỉ 2 cấp**:
   - `NguoiDung` bổ sung `diaChi`, `maTinhThanh`, `maPhuongXa`, `phuongThucXacThuc`, `ngayXacThuc`, `minhChungXacThuc`; bảng `TinhThanh` (34) và `PhuongXa` (168 phường/xã TP.HCM).
   - API địa chỉ + `POST /api/XacThuc/xac-thuc-dinh-danh` (CCCD cần ảnh; VNeID/Ngân hàng/Ví điện tử mô phỏng).
   - Mobile tách `HoSoCaNhan.tsx` (2 bước: hành chính → xác thực) và `HoSoHanhKhach.tsx` (hồ sơ ưu tiên). Web Admin có CRUD nhóm đối tượng ưu tiên (`/api/Admin/nhom-doi-tuong`).
2. **Chính sách tuyến & 158 tuyến thực tế (`08_chinh_sach_mien_phi_tuyen.sql`)**:
   - Bảng `ChinhSachMienPhi` (`NQ09_DIEU3`, `NQ09_DIEU4`, 01/07–31/12/2026); `TuyenXe.chinhSachMienPhiId` (`NULL` = tính phí) + `cuLyKm`.
   - Tổng **158 tuyến** = **109 Điều 3** + **25 Điều 4** + **24 tuyến tính phí** (nhập từ `word/xe_buyt_tphcm_2025.xlsx`). `LoaiVe` thêm `TheMienPhi`.
3. **Thẻ đi xe miễn phí & Ví vé QR động 30s**:
   - Cấp ngầm `TheMienPhi` khi xác thực định danh thành công (`GET /api/Ve/the-mien-phi`; chưa xác thực trả `403`).
   - Mobile "Ví vé QR" mở bảng chọn **134 tuyến miễn phí** (mở QR ngay) / **Các tuyến xe buýt khác** (mua vé tính phí). Quét QR dán xe nhận diện đúng tuyến miễn phí/tính phí. Tra cứu tuyến có bộ lọc Miễn phí/Tính phí. Tự cập nhật trạng thái mạng mỗi 5s (`useKetNoiMayChu`).
4. **Soát vé ngoại tuyến 5 lớp & Tự đồng bộ**:
   - Thẻ miễn phí quét trên tuyến tính phí → `SAI_PHAM_VI`; ràng buộc **1 lượt/thẻ/chuyến** (`DA_SU_DUNG` tại máy, `QUET_TRUNG` trên Server).
   - Gói offline lưu SQLCipher có cờ `tuyenMienPhi`. **Tự động đồng bộ ngay sau mỗi lượt quét khi có mạng (`tuDongBo()`) và tự đẩy hàng đợi khi có mạng trở lại** (`Client/SoatVeScreen.tsx`).
   - Nghiệp vụ QR: QR phía máy khách vẫn xoay 30s sau khi quét (do thiết kế ưu tiên ngoại tuyến hoàn toàn, không push từ máy soát về máy khách); việc chặn dùng lại được đảm bảo tại tầng máy soát (SQLCipher) và máy chủ (`LuotDi`).
5. **Kiểm thử đã pass**:
   - `dotnet build` OK; `npx tsc --noEmit` OK; `node --test tests/soatVeOffline.test.mjs` 2/2; `Admin` build OK; API end-to-end 15/15.
   - Thực nghiệm thiết bị thật nubia Z2472 (offline & online): Vé hợp lệ (xanh) → Quét lại `DA_SU_DUNG` (đỏ) → Đồng bộ 1 `LuotDi` → Tuyến tính phí `SAI_PHAM_VI` (đỏ).

---

## 4. Trạng thái Báo cáo Word, Sơ đồ Draw.io & Slide PowerPoint (ĐÃ HOÀN THIỆN)

### 4.1. Báo cáo Word (Bản mới nhất sẵn sàng xuất PDF)
- **File chính mới nhất**: `word/SmartBus - Bao cao DATN (HoanChinh) - Devin cap nhat 22-11.docx` (**83 trang**).
  - Đã chèn bổ sung 2 ảnh thật vừa chụp từ nubia Z2472:
    - **Hình 3.16**: `word/screenshots/Soat_02_KetQua_HopLe.png` (*Vé hợp lệ — mời hành khách lên xe*).
    - **Hình 3.17**: `word/screenshots/Soat_03_KetQua_DaSuDung.png` (*Từ chối: Vé đã được sử dụng cho lượt này*).
  - **Đã sửa lỗi trộn Danh mục Sơ đồ & Danh mục Hình** và **chạy trực tiếp Microsoft Word COM `Update entire table`** cho cả 4 bảng mục lục:
    - `MỤC LỤC`: Đã cập nhật số trang mới nhất.
    - `DANH MỤC SƠ ĐỒ` (style `DanhMucSoDo`): Tách riêng đủ **28 sơ đồ** (`Sơ đồ 2.1` → `Sơ đồ 2.28`).
    - `DANH MỤC HÌNH` (style `DanhMucHinh`): Tách riêng đủ **22 hình ảnh** (`Hình 2.1` → `Hình 2.5` phác thảo UI + `Hình 3.1` → `Hình 3.17` ảnh chạy thực tế).
    - `DANH MỤC BẢNG` (style `DanhMucBang`): Đủ **45 bảng** (Bảng 1.1, Bảng 2.1–2.40, Bảng 3.1–3.2).
  - Backup an toàn trước lần chèn 3.16–3.17: `word/SmartBus - Bao cao DATN (HoanChinh) - Devin cap nhat 22-11.backup_before_316_317.docx`.
  - *(Lưu ý nhỏ: nếu muốn thay ảnh Sơ đồ 2.27 trong Word bằng ảnh ERD `tab28` vừa sửa hết đường cắt chéo, có thể ghi đè nhanh trước khi xuất PDF).*

### 4.2. Sơ đồ Draw.io & Ảnh PNG
- **File Draw.io gốc**: `word/SmartBus - So do UML va ERD.drawio` (**33 tab**, chuẩn đen–trắng Times New Roman, đã xóa sạch dòng `Nguồn:` và đã sửa sạch lỗi đường chéo đè bảng ở `tab28` — `28_ERD_CapNhat_DinhDanh_MienPhi`).
- **Ảnh sơ đồ PNG nền trắng**: `word/diagrams/` (gồm cả `Hinh_ERD_CapNhat_DinhDanh_MienPhi.png` đã xuất lại sạch 100% không cắt bảng).
- **Ảnh chụp màn hình thực tế**: `word/screenshots/` (đủ 17 ảnh: `App_01..09`, `Soat_01..03`, `Admin_01..05`).

### 4.3. Slide Thuyết trình PowerPoint (ĐÃ XONG 13 SLIDE)
- **File PowerPoint chuẩn**: `word/SmartBus - Slide Bao Ve DATN (Chuan).pptx` (**13 slide 16:9**).
- **File PDF xuất sẵn**: `word/SmartBus - Slide Bao Ve DATN (Chuan).pdf`.
- **Thiết kế**: Đồng bộ tông **Xanh dương – Xám – Trắng**, không có khung nhãn `01 • ...`, có logo ĐH TN&MT TP.HCM, đã dùng ảnh ERD `tab28` mới sạch không đè bảng, Slide 10 dùng 2 ảnh thật nubia (`Vé hợp lệ` & `Đã sử dụng`) + 2 ảnh Web Admin, Slide 12 là Kết luận/Hạn chế/Hướng phát triển, Slide 13 là Slide Cảm ơn riêng, kèm sẵn **Speaker Notes** cho từng slide.

---

## 5. Luồng quay Video Demo & Việc còn lại trước 23h00 tối nay (22/11/2026)

### Luồng quay Demo đã thống nhất (tổng ≤ 15 phút)
- **Trên PC (4 clip demo phần mềm, ~7–8 phút)**:
  1. **Clip 1 — App Hành khách (Máy ảo)**: Đăng ký/Đăng nhập (`thinh@smartbus.vn` hoặc tài khoản mới) → Định danh VNeID + địa chỉ 2 cấp → Tra cứu 158 tuyến (lọc Miễn phí/Tính phí) → Mở Ví vé QR (Thẻ miễn phí 134 tuyến QR động 30s + xem mục tuyến tính phí).
  2. **Clip 2 — App Soát vé (Nubia thật)**: Mở phiên 1 tuyến miễn phí chưa quét (vd Tuyến 02/04) → Tắt Wi-Fi (Ngoại tuyến) → Quét lần 1 **XANH (Hợp lệ)** → Quét lần 2 **ĐỎ (Đã sử dụng)** → Bật lại Wi-Fi xem **tự đồng bộ** về `0 chờ gửi` → Kết thúc phiên.
  3. **Clip 3 — App Soát vé (Nubia thật)**: Mở phiên tuyến tính phí (**Tuyến 05**) → Quét Thẻ miễn phí → **ĐỎ (`SAI_PHAM_VI`)** + lướt nhanh Tra cứu khẩn cấp CCCD/SĐT.
  4. **Clip 4 — Web Admin (`http://localhost:5173`)**: Đăng nhập `admin@smartbus.vn` → Bảng điều khiển → 158 Tuyến xe (chính sách Điều 3/4/Tính phí) → Nhật ký soát vé (chỉ đúng các lượt vừa quét) → Cảnh báo gian lận.
- **Trên Laptop**: Quay thuyết trình **Slide 1–11** (~3,5–4 phút) + **Slide 12–13** (~45 giây), rồi cắt ghép theo thứ tự: `Slide 1–11 → Clip 1 → Clip 2 → Clip 3 → Clip 4 → Slide 12–13`.

### Checklist chốt nộp bài trước 23h00
- [ ] **1. Hoàn tất quay & ghép Video demo** (≤ 15 phút).
- [ ] **2. Xuất file PDF từ bản Word mới nhất** (`SmartBus - Bao cao DATN (HoanChinh) - Devin cap nhat 22-11.docx` → `.pdf`, tùy chọn thay nhanh Sơ đồ 2.27 bằng ảnh ERD sạch trước khi xuất).
- [ ] **3. Chốt tên thiết bị** trong báo cáo (`Nubia A36` hay `nubia Z2472`) nếu cần đồng nhất 1 tên.
- [ ] **4. Commit & Push toàn bộ code mới nhất** lên GitHub `https://github.com/thinh082/DATN_NguyenPhuocThinh_1150070040` *(lưu ý: code ngày 22/11 hiện CHƯA commit/push; kiểm tra quyền truy cập cho `tuthanhtri@hcmunre.edu.vn`)*.
- [ ] **5. Đóng gói 01 file `.zip`** đủ 5 hạng mục (Đề cương PDF, Báo cáo `.docx` + `.pdf`, Slide `.pptx`/`.pdf`, Video ≤ 15p, file/link GitHub) và gửi email tới `dklien@hcmunre.edu.vn`, Cc `tuthanhtri@hcmunre.edu.vn`.

---

## 6. Nguyên tắc khi agent khác làm tiếp
- Luôn backup trước khi sửa `.docx` hoặc DB; không sửa Word song song nhiều agent.
- Mở phiên soát vé (**Bắt đầu phiên**) **sau khi** hành khách đã mua vé hoặc xác thực định danh xong.
- Không tự ý chạy thao tác phá hủy (`git push --force`, xóa DB, xóa file gốc) khi chưa được người dùng đồng ý.
- Tài liệu chi tiết tham chiếu thêm: `context/TONG_HOP_CONTEXT_22-11.md`, `context/YEU_CAU_NOP_DO_AN.md`, `context/CAP_NHAT_XAC_THUC_DINH_DANH_2026-11-22.md`, `context/PLAN_TUYEN_MIEN_PHI_VA_TINH_PHI.md`, `context/RA_SOAT_BAO_CAO_VA_SO_DO_22-11.md`.
