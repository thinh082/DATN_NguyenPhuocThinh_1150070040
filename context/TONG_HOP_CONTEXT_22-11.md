# TỔNG HỢP CONTEXT DỰ ÁN SMARTBUS (cập nhật 22/11/2026 tối)

> File này dùng để nạp vào agent khác (agy cli, Gemini, Codex...). Đọc file này trước; chi tiết từng phần xem các file được dẫn ở cuối.

## 1. Dự án
- Đồ án tốt nghiệp: **"Xây dựng hệ thống xác thực và quản lý lượt đi xe buýt sử dụng mã QR động, chữ ký số và cơ chế xác thực ngoại tuyến"**. SV: Nguyễn Phước Thịnh, MSSV 1150070040, lớp 11_ĐH_TMĐT, HCMUNRE. GVHD: ThS. Từ Thanh Trí.
- **Hạn nộp: trước 23h00 ngày 22/11/2026.** Nộp 1 file .zip qua email `dklien@hcmunre.edu.vn`, CC `tuthanhtri@hcmunre.edu.vn`, gồm 5 hạng mục: đề cương PDF; báo cáo .docx + .pdf; slide; video ≤ 15 phút (tự thuyết trình + demo thật); link GitHub `https://github.com/thinh082/DATN_NguyenPhuocThinh_1150070040`. Chi tiết: `context/YEU_CAU_NOP_DO_AN.md`.
- **Định vị đề tài (đã thống nhất):** Nhà nước ban hành NQ 09/2026/NQ-HĐND (miễn 100% vé 134 tuyến, 01/07–31/12/2026; từ 01/10/2026 bắt buộc xác thực hành khách) và thanh toán cho nhà xe theo số lượt đã xác thực. SmartBus là **phần mềm hỗ trợ nhà xe** xác thực hành khách (QR động TOTP 30 giây + chữ ký số **ECDSA P-256**, chống giả mạo), ghi nhận lượt đi theo tuyến/chuyến (kể cả offline), cung cấp thông tin tuyến; dữ liệu lượt đi chống khai khống nên dùng làm **căn cứ để nhà nước đối soát, cấp kinh phí cho nhà xe**.

## 2. Kiến trúc và môi trường
- `Server/`: ASP.NET Core 8, EF Core, SQL Server (`THINHPC`, DB `SmartBusDb`, Windows Auth), Redis tùy chọn, Cloudinary. Chạy: `dotnet run --project .\Server --launch-profile http -- --urls "http://0.0.0.0:5056"` (Swagger `/swagger`).
- `Client/`: Expo SDK 58 / React Native, `App.tsx` điều hướng bằng state (không dùng Expo Router). `.env.local`: `EXPO_PUBLIC_API_BASE_URL=http://192.168.1.13:5056`. Chạy: `cd Client; npx expo start`. Đọc `Client/AGENTS.md` trước khi sửa (kiểm tra docs Expo theo đúng version, dùng `npx expo install`, không sửa tay `android/`/`ios/`).
- `Admin/`: React + Vite (`Admin/src/main.tsx`), `npm run dev` → `http://localhost:5173`.
- `Server.Tests/`: xUnit. **Lưu ý: test tích hợp ghi vào DB thật `SmartBusDb`.**
- Thiết bị: máy ảo `emulator-5554` (SmartBus_API31, Android 12) làm máy hành khách; điện thoại thật `LZ0A36VNYD5002773` (nubia, model **Z2472**, Android 15) làm máy soát vé. Cả hai cài bản debug `com.anonymous.Client`.
  - Nếu nubia báo "Unable to load script": `adb -s LZ0A36VNYD5002773 reverse tcp:8081 tcp:8081` rồi Reload.
- Tài khoản demo (mật khẩu `123456`): `admin@smartbus.vn`, `soatve01@smartbus.vn`, `thinh@smartbus.vn`. Tài khoản `thinh` **đang ở trạng thái đã xác thực VNeID và có thẻ miễn phí** (Ve id 20009).
- Script DB chạy theo thứ tự (xem README): 01 → 08, sau đó **chạy lại 05**; các script 05, 07, 08 cần thêm `-I`.
- Backup DB: `E:\SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup\SmartBusDb_truoc_xac_thuc_20261122.bak`, `..._truoc_du_lieu_co_dau_20261122.bak`, `..._truoc_tuyen_mien_phi_20261122.bak`.

## 3. Chức năng đã làm trong ngày 22/11
1. **Định danh hành khách:** `NguoiDung` thêm `diaChi`, `maTinhThanh`, `maPhuongXa`, `phuongThucXacThuc`, `ngayXacThuc`, `minhChungXacThuc`; bảng `TinhThanh` (34) và `PhuongXa` (168 phường/xã TP.HCM). API địa chỉ và `POST /api/XacThuc/xac-thuc-dinh-danh` (CCCD cần ảnh; VNeID/Ngân hàng/Ví điện tử **mô phỏng**). Mobile tách `HoSoCaNhan.tsx` (2 bước: thông tin hành chính → xác thực) và `HoSoHanhKhach.tsx` (hồ sơ ưu tiên). Admin có CRUD nhóm đối tượng ưu tiên (`/api/Admin/nhom-doi-tuong`).
2. **Chính sách tuyến (script `08_chinh_sach_mien_phi_tuyen.sql`):** bảng `ChinhSachMienPhi` (NQ09_DIEU3, NQ09_DIEU4, hiệu lực 01/07–31/12/2026); `TuyenXe.chinhSachMienPhiId` (NULL = tính phí) + `cuLyKm`. Tổng **158 tuyến = 109 Điều 3 + 25 Điều 4 + 24 tuyến tính phí** (nhập từ `word/xe_buyt_tphcm_2025.xlsx`, giá = mức cao nhất toàn tuyến; 61-05, 61-7, 70-5 tính phí). `LoaiVe` thêm `TheMienPhi`.
3. **Thẻ đi xe miễn phí:** cấp ngầm khi xác thực định danh thành công (`GET /api/Ve/the-mien-phi`; chưa xác thực trả 403). Không tạo lịch sử khi mở QR; chỉ ghi lượt đi khi máy soát quét.
4. **Mobile hành khách:** "Ví vé QR" mở bảng chọn **134 tuyến miễn phí** (hiện QR ngay, không có màn mua vé) / **Các tuyến xe buýt khác** (mua vé, chỉ giá tuyến tính phí). Quét QR dán xe: tuyến miễn phí mở QR thẻ, tuyến tính phí hiện giá đúng tuyến. Tra cứu tuyến có nhãn và dropdown lọc Miễn phí/Tính phí. Trạng thái mạng tự cập nhật mỗi 5 giây (`useKetNoiMayChu`).
5. **Soát vé:** thẻ miễn phí trên tuyến tính phí → `SAI_PHAM_VI`; **1 lượt/thẻ/chuyến** (`DA_SU_DUNG` tại máy, `QUET_TRUNG` ở server). Áp dụng ở online, xác nhận thủ công, tra cứu khẩn cấp, offline validator (`Client/services/soatVeOffline.mjs`), đồng bộ (`OfflineSyncService`). Gói offline có `tuyenMienPhi`. **Máy soát tự đồng bộ ngay sau mỗi lượt quét khi có mạng** và khi có mạng trở lại.
6. Sửa lỗi có sẵn: quét online bị từ chối trước đây trả lỗi 500 (do `NhatKyQuetVe.veId`); thông báo backend và dữ liệu demo đã có dấu tiếng Việt.
7. **Kiểm thử:** `dotnet build` OK; `npx tsc --noEmit` OK; `node --test tests/soatVeOffline.test.mjs` 2/2; Admin build OK; API end-to-end 15/15. Trên nubia thật (offline): thẻ miễn phí Tuyến 03 hợp lệ → quét lại DA_SU_DUNG → đồng bộ 1 LuotDi; Tuyến 05 → SAI_PHAM_VI. **Chưa chạy lại `dotnet test`. Code hôm nay CHƯA commit/push GitHub.**

## 4. Báo cáo Word và sơ đồ (trạng thái hiện tại)
- **File báo cáo mới nhất:** `word/SmartBus - Bao cao DATN (HoanChinh) - Devin cap nhat 22-11.docx` (81 trang, 50 hình, 45 bảng; mục lục và danh mục đã cập nhật bằng Word). File gốc `...(HoanChinh).docx` và các bản `*.backup_*.docx` vẫn giữ nguyên.
- Đã sửa trong Word:
  - Chương 1: đoạn bối cảnh NQ 09 / NQ 29 / xác thực từ 01/10 và định vị đề tài.
  - Bảng use case mới 2.8–2.10; bảng CSDL đánh số lại 2.11–2.39; **Bảng 2.40 ChinhSachMienPhi**; TuyenXe +2 cột; LoaiVe có TheMienPhi.
  - Chú thích **Sơ đồ 2.1–2.28** (đã xóa sơ đồ triển khai theo yêu cầu); phác thảo UI **Hình 2.1–2.5**; ảnh thực tế **Hình 3.1–3.15** (mục 3.2).
  - Chương 3: 158 tuyến; Bảng 3.1 và 3.2 điền kết quả thật (TC-01 → TC-09).
  - Kết luận: đoạn hạn chế. Tài liệu tham khảo [16]–[22]. **Đã xóa Phụ lục** theo yêu cầu.
- File sơ đồ: `word/SmartBus - So do UML va ERD.drawio` (33 trang; trang 28–32 là sơ đồ mới: ERD cập nhật, Class cập nhật, Seq xác thực định danh, Seq thẻ miễn phí và soát vé, Lưu đồ ví vé). Backup: `...backup_truoc_cap_nhat_22-11.drawio`. Ảnh PNG: `word/diagrams/Hinh_*.png`; ảnh giao diện: `word/screenshots/`.
- Cách chụp sơ đồ: plugin Orca `lindoo67.drawio` (`orca plugin shot ... --editor <file 1 trang> --width 2400 --height 1500`) rồi cắt bằng PowerShell System.Drawing. Ảnh Web Admin: `orca snapshot` / `orca click` / `orca screenshot`. Ảnh app: `adb exec-out screencap -p`.

## 5. Việc còn lại (ưu tiên trước hạn nộp)
1. **Commit + push code lên GitHub** (chưa làm; hỏi người dùng trước).
2. Người dùng xem lại file Word mới, xuất **PDF**.
3. Còn thiếu 2 ảnh máy soát vé "Vé hợp lệ" và "Đã sử dụng" (cần quét thật bằng nubia trên một chuyến miễn phí **chưa quét**), chèn làm Hình 3.16–3.17.
4. Xác nhận tên thiết bị trong báo cáo ("Nubia A36" hay model Z2472).
5. Một số trang sơ đồ cũ chưa cập nhật theo luồng mới (Use case tổng quát, Seq/Activity soát vé offline, Lưu đồ quét QR dán xe).
6. Slide thuyết trình; quay video demo (kịch bản gợi ý: slide 3,5 phút → Admin → đăng ký và xác thực → tra cứu → ví 134 tuyến / tính phí → soát vé nubia online, offline, trùng, sai phạm vi → Admin nhật ký → kết luận; ≤ 15 phút).
7. Hướng phát triển (không làm kịp): giá theo chặng; nhập 4.897 điểm lộ trình; báo cáo lượt đi theo Điều 3/Điều 4/tính phí; đổi chữ "Doanh thu mô phỏng" thành "Giá trị vé đề nghị hỗ trợ"; nút làm mới gói soát vé giữa phiên.

## 6. Lưu ý khi làm tiếp
- Mỗi phiên soát vé gắn 1 thiết bị + 1 chuyến; gói offline chỉ chứa vé có tại lúc tải. Mở phiên **sau khi** hành khách đã mua vé hoặc xác thực.
- Không sửa Word song song với agent khác. Luôn backup trước khi sửa `.docx` hoặc DB.
- Không tự chạy thao tác phá hủy (xóa dữ liệu, force push) khi chưa được đồng ý.

## 7. File context chi tiết
- `context/CAP_NHAT_XAC_THUC_DINH_DANH_2026-11-22.md`: định danh, địa chỉ, nhóm ưu tiên, sửa UI.
- `context/PLAN_TUYEN_MIEN_PHI_VA_TINH_PHI.md`: kế hoạch, mục 10 (phân tích Excel), mục 11 (đã triển khai, kết quả test thiết bị, tự đồng bộ).
- `context/RA_SOAT_BAO_CAO_VA_SO_DO_22-11.md`: rà soát Word/sơ đồ, các lần sửa Word.
- `context/YEU_CAU_NOP_DO_AN.md`, `context/de_cuong_datn_context.md`, `context/PLAN_UI_UX_CODEX.md`, `README.md`.
