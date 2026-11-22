# Kế hoạch: Tách "134 tuyến miễn phí" và "Tuyến tính phí" (22/11/2026)

> Trạng thái: **ĐÃ TRIỂN KHAI bước 1–5 (22/11/2026, chiều)**, xem mục 11 ở cuối file.

## 1. Mục tiêu (theo yêu cầu người dùng)

1. Ô **"Ví vé QR"** khi bấm vào cho chọn 2 mục:
   - **134 tuyến xe buýt miễn phí** (NQ 09/2026): vào là **hiện QR luôn, không có màn mua vé**. Điều kiện: tài khoản đã **xác thực định danh** (CCCD / VNeID / thẻ ngân hàng / ví điện tử). Chưa xác thực thì hướng dẫn sang Hồ sơ cá nhân → Xác thực.
   - **Các tuyến xe buýt khác** (tính phí): giữ màn hình mua vé như hiện tại, chỉ liệt kê giá của các tuyến tính phí.
2. Quét QR xong thì giữ nguyên flow hiện tại (soát online/offline, đồng bộ) và **ghi nhận lượt đi theo tuyến của chuyến** đang soát.
3. DB có thông tin để **lọc tuyến miễn phí / tính phí** cho 2 mục trên.
4. **Tra cứu tuyến xe**: hiện nhãn "Miễn phí theo NQ 09/2026" và thêm dropdown lọc: *Tất cả / Miễn phí theo chính sách / Tính phí*.

## 2. Hiện trạng liên quan

- `TuyenXe` có 134 dòng, **không có cột phân loại**. Danh sách Phụ lục 01 (109 tuyến) và Phụ lục 02 (25 tuyến) chỉ nằm ở comment trong `04_seed_134_tuyen_NQ09_2026.sql` và file `word/09_2026_NQ-HDND_711744.docx`.
- `GiaVe` có 7 dòng, đều thực thu 0đ, phần lớn là "toàn mạng lưới" (`tuyenXeId = NULL`), không có `hieuLucDen`.
- Soát vé: vé có `tuyenXeId = NULL` hợp lệ trên mọi tuyến; chỉ vé gắn tuyến mới bị kiểm tra `SAI_TUYEN` (`OfflineQrValidationService.cs`, `Client/services/soatVeOffline.mjs`).
- Chống quét trùng khi đồng bộ (`OfflineSyncService`): `VeLuot` chỉ được 1 lượt; loại vé khác bị coi là trùng nếu cùng bước TOTP.
- Ví vé (`App.tsx` → `moTaoQr`) lấy danh sách vé của user rồi hiện QR động; vé được cache offline khi mở ví lúc có mạng.

## 3. Thiết kế DB (đề xuất)

### 3.1 Bảng chính sách miễn phí (mới)
```sql
CREATE TABLE dbo.ChinhSachMienPhi (
    id int IDENTITY PRIMARY KEY,
    ma varchar(30) NOT NULL UNIQUE,          -- 'NQ09_DIEU3', 'NQ09_DIEU4'
    ten nvarchar(200) NOT NULL,              -- 'NQ 09/2026/NQ-HĐND - Điều 3 (tuyến có trợ giá)'
    canCu nvarchar(500) NOT NULL,
    hieuLucTu date NOT NULL,                 -- 2026-07-01
    hieuLucDen date NOT NULL,                -- 2026-12-31
    yeuCauXacThuc bit NOT NULL DEFAULT 1,    -- giai đoạn 2 (từ 01/10/2026) bắt buộc định danh
    trangThai bit NOT NULL DEFAULT 1
);
```
### 3.2 Cột mới trong `TuyenXe`
```sql
ALTER TABLE dbo.TuyenXe ADD chinhSachMienPhiId int NULL REFERENCES dbo.ChinhSachMienPhi(id);
```
- `NULL` nghĩa là **tuyến tính phí**.
- Có giá trị **và** hôm nay nằm trong `hieuLucTu..hieuLucDen` thì **miễn phí**. Hết hạn chính sách (sau 31/12/2026) tuyến tự chuyển sang tính phí, không cần sửa dữ liệu.
- Tách 2 chính sách (Điều 3 / Điều 4) để báo cáo quyết toán đúng nguồn kinh phí.
- Mở rộng được: chính sách mới chỉ cần thêm một dòng và gán cho tuyến.

### 3.3 Script `08_chinh_sach_mien_phi_tuyen.sql`
- Tạo bảng, thêm 2 chính sách.
- Gán Điều 4 cho **25 tuyến Phụ lục 02**, Điều 3 cho **109 tuyến Phụ lục 01**. Lấy danh sách từ docx, đối chiếu với `TuyenXe.maTuyen`; mã nào không khớp thì in ra để kiểm tra.
- Các tuyến cào thêm từ vietnammoi mà không thuộc 134 tuyến thì `chinhSachMienPhiId = NULL` (tính phí).

### 3.4 Loại vé miễn phí
- Thêm `LoaiVe`: `('TheMienPhi', N'Thẻ đi xe miễn phí')`.
- **Thẻ miễn phí** là một vé (dùng lại toàn bộ hạ tầng ký số ECDSA + TOTP + soát offline):
  - `maLoaiVe = 'TheMienPhi'`, `tuyenXeId = NULL`, nhóm `PhoThong`, giá 0đ.
  - Hiệu lực từ lúc cấp đến `hieuLucDen` của chính sách (31/12/2026).
  - Dữ liệu ký số có thêm trường phạm vi `pv = 'MP'` (chỉ dùng trên tuyến miễn phí).
  - Mỗi tài khoản đã xác thực có tối đa **1 thẻ còn hiệu lực**. Cấp tự động và idempotent.

## 4. Backend

1. Entity `ChinhSachMienPhi`, cột `TuyenXe.chinhSachMienPhiId`, cấu hình DbContext.
2. `VanHanhService.layDanhSachTuyen`: trả thêm `mienPhi` (bool, đã tính theo ngày hiệu lực), `maChinhSach`, `tenChinhSach`. Hỗ trợ query `?loc=tatCa|mienPhi|tinhPhi`.
3. API mới `POST /api/Ve/the-mien-phi` [HanhKhach]:
   - Chưa xác thực định danh (`NguoiDung.phuongThucXacThuc IS NULL`) thì trả 403 "Vui lòng xác thực định danh để sử dụng tuyến miễn phí".
   - Đã có thẻ còn hiệu lực thì trả thẻ đó; chưa có thì phát hành thẻ mới qua luồng ký số hiện có (đơn hàng `MienPhi` 0đ để giữ dấu vết, hoặc phát hành trực tiếp).
4. `layDanhSachBieuGia` (tab tính phí): chỉ trả `GiaVe` của tuyến tính phí, không trả giá "toàn mạng" 0đ. Bỏ hoặc tắt các dòng 0đ toàn mạng hiện tại (dòng test `1002` tắt luôn).
5. **Soát vé (online + offline + đồng bộ)**:
   - Gói offline (`GoiSoatVe`/`chuan-bi`) gửi thêm `tuyenMienPhi: bool` của chuyến.
   - Vé có `pv = 'MP'` quét trên tuyến **không** miễn phí → mã mới `SAI_PHAM_VI` ("Thẻ miễn phí không áp dụng cho tuyến tính phí"). Sửa ở cả `OfflineQrValidationService.cs`, `SoatVeService` (online), `Client/services/soatVeOffline.mjs`, `Client/constants/lyDoSoatVe.ts`, từ điển `human` của Admin.
   - **Chống trùng cho thẻ nhiều lượt**: hiện chỉ coi trùng khi cùng bước TOTP, chưa đủ. Đề xuất: cùng thẻ + cùng chuyến xe (`chuyenXeId`) đã có lượt `HopLe` thì báo `QUET_TRUNG`. Áp dụng ở `OfflineSyncService`, xác thực online và `xacThucNgoaiTuyen` phía máy soát (dựa trên các sự kiện đã quét trong phiên).
   - `LuotDi` đã gắn chuyến qua `NhatKyQuetVe → PhienSoatVe → ChuyenXe → LoTrinh → TuyenXe`, nên **lượt đi tự gắn đúng tuyến**, không cần đổi.
6. `QuetQrXe` (quét QR dán trên xe): API trả thêm `mienPhi`. Tuyến miễn phí → app mở thẳng QR thẻ miễn phí; tuyến tính phí → hiện giá của tuyến đó.
7. Báo cáo: thống kê lượt đi theo `ChinhSachMienPhi` (Điều 3 / Điều 4 / tính phí) để phục vụ quyết toán.

## 5. Mobile (`Client/`)

1. Ô "Ví vé QR" mở bottom sheet chọn **"134 tuyến xe buýt miễn phí"** hoặc **"Các tuyến xe buýt khác"**.
2. **Màn tuyến miễn phí** (mới, ví dụ `ViMienPhiScreen.tsx`):
   - Chưa xác thực: thẻ cảnh báo + nút "Xác thực ngay" (mở Hồ sơ cá nhân, bước 2).
   - Đã xác thực: gọi `POST /api/Ve/the-mien-phi`, hiện **QR động** (dùng lại component QR, đếm ngược 30s, chống chụp màn hình, tăng độ sáng), nhãn "Miễn phí 134 tuyến – NQ 09/2026", hạn 31/12/2026, phương thức đã xác thực.
   - Offline: dùng thẻ đã cache (giống ví hiện tại).
3. **Màn tuyến tính phí**: chính là màn ví + mua vé hiện tại, nhưng danh sách vé và biểu giá chỉ gồm tuyến tính phí; ẩn thẻ miễn phí khỏi danh sách.
4. **Tra cứu tuyến** (`TuyenVaLichSu.tsx`): badge "Miễn phí · NQ 09" trên mỗi tuyến miễn phí; `SelectField` lọc *Tất cả / Miễn phí theo chính sách / Tính phí*.
5. Quét QR xe: rẽ nhánh theo `mienPhi` như mục 4.6.

## 6. Admin web

- Trang Tuyến xe: thêm cột "Chính sách" (tên chính sách hoặc "Tính phí") và trường chọn chính sách khi sửa tuyến.
- (Tùy chọn) Trang "Chính sách miễn phí": xem/sửa ngày hiệu lực.
- Từ điển `human`: `TheMienPhi`, `SAI_PHAM_VI`.

## 7. Dữ liệu tuyến tính phí (chờ người dùng)

- Nguồn dự kiến: https://vietnammoi.vn/lich-trinh-cac-tuyen-xe-buyt-tphcm-moi-nhat-2025-thoi-gian-gia-ve-lo-trinh-chi-tiet-202552415731119.htm (đăng 24/05/2025, **trước** sáp nhập và trước NQ 09; khoảng 128 tuyến; mỗi tuyến có mã, tên, lộ trình 2 chiều, cự ly, giờ hoạt động, giá vé lượt/HSSV/vé tập, số chuyến, thời gian chuyến, giãn cách).
- Khi có file Excel: import vào bảng tạm, đối chiếu `maTuyen` với 134 tuyến. Tuyến **không** thuộc 134 thì thêm vào `TuyenXe` với `chinhSachMienPhiId = NULL` và tạo `GiaVe` theo tuyến. Tuyến thuộc 134 thì chỉ cập nhật thông tin bổ sung (cự ly, giờ, giãn cách) nếu người dùng đồng ý.
- Prompt cào dữ liệu cho AI khác: xem mục 9.

## 8. Thứ tự triển khai & rủi ro

| Bước | Nội dung | Rủi ro |
| --- | --- | --- |
| 1 | Script 08 + entity + API danh sách tuyến có `mienPhi` + lọc ở Tra cứu tuyến + Admin | Thấp (thêm dữ liệu, không đổi logic vé) |
| 2 | Bottom sheet 2 mục + màn tuyến tính phí (lọc biểu giá) | Thấp |
| 3 | Thẻ miễn phí: API cấp thẻ + màn QR miễn phí + chặn khi chưa xác thực | Trung bình |
| 4 | Soát vé: `SAI_PHAM_VI` + chống trùng theo chuyến cho thẻ nhiều lượt (server + máy soát offline) | **Cao**: đụng logic ký vé và soát offline, phải test lại TC2/TC3 trên nubia |
| 5 | Import tuyến tính phí + giá khi có Excel | Thấp–trung bình |

Phải backup DB trước bước 1 và bước 5. Sau bước 4 chạy lại `node --test tests/soatVeOffline.test.mjs` và các test backend liên quan (cần người dùng đồng ý vì test ghi vào DB).

## 9. Prompt cào dữ liệu (đưa cho AI khác)

Xem nguyên văn trong câu trả lời chat ngày 22/11/2026 hoặc bản dưới đây:

```text
Bạn là trợ lý trích xuất dữ liệu. Hãy đọc trang:
https://vietnammoi.vn/lich-trinh-cac-tuyen-xe-buyt-tphcm-moi-nhat-2025-thoi-gian-gia-ve-lo-trinh-chi-tiet-202552415731119.htm
và trích xuất TOÀN BỘ các tuyến xe buýt trong phần "Lộ trình tuyến xe buýt..." (mỗi tuyến là một tiêu đề "Tuyến ..." kèm một bảng thông tin).

Xuất 1 file Excel (.xlsx), mã hóa UTF-8, giữ nguyên tiếng Việt có dấu, gồm 3 sheet:

Sheet "Tuyen" (1 dòng / tuyến), đúng thứ tự và tên cột sau:
maTuyen | tenTuyen | diemDau | diemCuoi | cuLyKm | gioBatDau | gioKetThuc | soChuyenNgay | thoiGianChuyenPhut | gianCachPhutMin | gianCachPhutMax | giaVeLuot | giaVeLuotHSSV | giaVeTap | soLuotVeTap | loaiTuyen | donViKhaiThac | ghiChu | nguonUrl

Sheet "LoTrinh" (1 dòng / một điểm trên lộ trình):
maTuyen | chieu | thuTu | tenDiem
  - chieu: "Di" cho chiều thứ nhất được liệt kê, "Ve" cho chiều ngược lại.
  - Tách lộ trình theo dấu "–" hoặc "-" giữa các đoạn; thuTu bắt đầu từ 1; bỏ khoảng trắng thừa.

Sheet "KiemTra": liệt kê mọi tuyến/ô bạn không chắc chắn hoặc không đọc được:
maTuyen | truong | giaTriGoc | lyDo

Quy tắc bắt buộc:
1. maTuyen là chuỗi, giữ nguyên dạng gốc: "01", "03", "61-3", "D4", "150". KHÔNG đổi thành số (không làm mất số 0 đầu).
2. tenTuyen lấy phần tên sau mã tuyến trong tiêu đề, ví dụ "Tuyến 03: Bến Thành - Thạnh Xuân" → maTuyen "03", tenTuyen "Bến Thành - Thạnh Xuân". diemDau/diemCuoi tách từ tenTuyen theo dấu " - " đầu tiên và cuối cùng.
3. cuLyKm là số thập phân dùng dấu chấm: "8,74 km" → 8.74.
4. gioBatDau/gioKetThuc định dạng HH:mm (24h): "05:00 – 20:15" → 05:00 và 20:15. Nếu có nhiều khung giờ, lấy giờ sớm nhất và muộn nhất, ghi chi tiết vào ghiChu.
5. Giá vé là số nguyên VND, không dấu chấm phân cách: "5.000 VND" → 5000. giaVeLuot = vé lượt thường (trợ giá hoặc không). giaVeLuotHSSV = vé học sinh sinh viên. giaVeTap = giá vé tập, soLuotVeTap = số lượt của vé tập (ví dụ "Vé tập (120 lượt): 112.500 VND" → 112500 và 120). Nếu giá vé theo cự ly/chặng (nhiều mức), ghi mức thấp nhất vào giaVeLuot và toàn bộ các mức vào ghiChu.
6. Giãn cách: "10 – 15 phút" → gianCachPhutMin 10, gianCachPhutMax 15; một giá trị thì Min = Max. soChuyenNgay, thoiGianChuyenPhut là số nguyên.
7. loaiTuyen: "NoiDo" nếu bài xếp vào nhóm nội đô; "KetNoiVung" nếu tuyến đi tỉnh khác hoặc bài ghi là tuyến kết nối vùng; nếu không rõ thì để trống và ghi vào KiemTra.
8. TUYỆT ĐỐI không tự bịa hoặc suy đoán dữ liệu. Ô nào bài không có thì để trống và ghi vào sheet KiemTra.
9. Không bỏ sót tuyến nào; cuối cùng báo tổng số tuyến đã trích xuất và số tuyến có dòng trong KiemTra.
10. nguonUrl ghi đúng URL trên cho mọi dòng.
11. Nếu không xuất được .xlsx thì xuất 3 file CSV UTF-8 có BOM, phân cách dấu phẩy, có dòng tiêu đề, tên file Tuyen.csv / LoTrinh.csv / KiemTra.csv.

Lưu ý: dữ liệu bài viết là thời điểm 05/2025 (trước sáp nhập tỉnh và trước chính sách miễn phí NQ 09/2026); chỉ trích xuất đúng như bài viết, không cập nhật theo thông tin khác.
```

## 10. Đánh giá file `word/xe_buyt_tphcm_2025.xlsx` (22/11/2026)

- Cấu trúc đúng prompt: 3 sheet `Tuyen` (155 dòng), `LoTrinh` (4.897 điểm), `KiemTra` (445 dòng). Tên cột đúng; `maTuyen` giữ dạng chuỗi ("01", "61-05"), không trùng, không rỗng; giờ đúng HH:mm; số liệu là số; giãn cách Min ≤ Max; `thuTu` liên tục; mọi tuyến đều có lộ trình.
- Thiếu / cần lưu ý:
  - `donViKhaiThac` trống 155/155 (bài không có).
  - `thoiGianChuyenPhut` trống 26 (bài ghi khoảng, chi tiết ở `ghiChu`).
  - 21 tuyến chỉ có lộ trình 1 chiều (18, 19, 155, 158–160, 165, các tuyến D/V, DL01–04).
  - 13 điểm lộ trình dài (kèm ghi chú trong ngoặc).
- Đối chiếu với 134 tuyến NQ 09 trong DB:
  - 110 tuyến khớp mã. 37 tuyến có tên khác (DB theo NQ 09, mới hơn) → **giữ tên DB**.
  - 24 tuyến NQ 09 không có trong Excel: 156, 166–169 (Excel tách thành 156D/156V…), 170, 171, 173, 86, 72-4, 61-2/4/37/38/39/51/52/53/55/67/68/70/73/77 (tuyến Bình Dương/BR-VT/Côn Đảo, bài viết trước sáp nhập). Không ảnh hưởng vì đều là tuyến miễn phí.
  - 45 mã Excel không thuộc 134:
    - Liên tỉnh / kết nối vùng (**tính phí**, NQ 09 loại trừ): 05, 60-1/2/3/5/7, 62-1/2/5/6/7/8/9/10/11, 63-1, 70-1, 70-2, 72-1 (sân bay – Vũng Tàu).
    - Nội tỉnh mới sau sáp nhập nhưng không có trong Phụ lục: 61-05, 61-7, 70-5 → **tính phí** (cần người dùng xác nhận).
    - Du lịch, xe thoáng nóc (**tính phí**, NQ 09 loại trừ): DL01–DL04 (DL01, DL02 không có giá).
    - **Không nhập**: 156D/V, 166D/V, 167D/V, 168D/V, 169D/V (là chiều của tuyến 156, 166–169 đã có, miễn phí); HS-01..07 (xe đưa đón học sinh, giá 0); MRT1 (metro), SWB1 (buýt đường sông).
- **Vấn đề giá**: các tuyến liên tỉnh tính giá **theo chặng/cự ly** nhiều mức (ví dụ 60-1: 7.000–45.000đ; 72-1: 150.000–200.000đ), `giaVeLuot` chỉ là mức thấp nhất. Hệ thống chưa hỗ trợ giá theo chặng → cần chốt: dùng 1 giá cố định/tuyến (thấp nhất hay toàn tuyến cao nhất), hoặc tạo nhiều `GiaVe` theo chặng (chưa có chọn điểm lên/xuống).
- Giá trong file là thời điểm 05/2025.

## 11. Đã triển khai (22/11/2026, chiều)

Quyết định của người dùng: giá tuyến liên tỉnh lấy **mức cao nhất**; 61-05, 61-7, 70-5 **tính phí**; bổ sung cự ly/giờ/giãn cách cho tuyến trùng (giữ tên theo NQ 09); thẻ miễn phí **cấp khi xác thực**, **1 lượt/chuyến**, **từ chối trên tuyến tính phí**.

### DB
- Backup: `E:\SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup\SmartBusDb_truoc_tuyen_mien_phi_20261122.bak`.
- `Server/Database/08_chinh_sach_mien_phi_tuyen.sql` (đã chạy, chạy với `-I`), rồi chạy lại `05_seed_chuyen_xe_134_tuyen.sql`:
  - Bảng `ChinhSachMienPhi` (`NQ09_DIEU3`, `NQ09_DIEU4`, hiệu lực 01/07–31/12/2026, `yeuCauXacThuc`).
  - `TuyenXe.chinhSachMienPhiId` (NULL = tính phí) và `TuyenXe.cuLyKm`.
  - Gán 109 tuyến Điều 3, 25 tuyến Điều 4 (danh sách Phụ lục 02 lấy từ docx).
  - Bổ sung cự ly, giờ, giãn cách (Min) cho 110 tuyến NQ 09 có trong Excel.
  - Thêm 24 tuyến tính phí: 05, 60-1/2/3/5/7, 61-05, 61-7, 62-1/2/5/6/7/8/9/10/11, 63-1, 70-1, 70-2, 70-5, 72-1, DL03, DL04. Mỗi tuyến có `GiaVe` vé lượt Phổ thông = mức cao nhất. Có lộ trình "Lượt đi", xe và chuyến đang chạy nhờ chạy lại script 05.
  - **Không nhập**: DL01, DL02 (không có giá), các tuyến D/V, HS-xx, MRT1, SWB1.
  - `LoaiVe` thêm `TheMienPhi` (ràng buộc `CK_LoaiVe_ma` được mở rộng, script 01 cũng sửa). Thêm `GiaVe` TheMienPhi 0đ hạn 31/12/2026 23:59:59.
  - Tắt `GiaVe` dòng test "Dữ liệu demo kiểm thử QR trực tuyến - tuyến 01".
- Kết quả: 158 tuyến = 109 (Điều 3) + 25 (Điều 4) + 24 tính phí; 158 tuyến có chuyến đang chạy.

### Backend
- `Entities/ChinhSachMienPhi.cs`, `TuyenXe` có thêm `chinhSachMienPhiId`, `cuLyKm`, navigation. Cấu hình trong DbContext.
- `Services/ChinhSachTuyen.cs`: `TheMienPhi`, `HomNay` (giờ VN), `LaMienPhi(ngay)`, `TuyenMienPhiAsync`, `DaCoLuotTrongChuyenAsync`.
- `GET /api/VanHanh/danh-sach-tuyen?loc=tatCa|mienPhi|tinhPhi`: trả thêm `mienPhi`, `maChinhSach`, `tenChinhSach`, `cuLyKm`. Chi tiết tuyến cũng có các trường này. `luu-tuyen-xe` nhận `maChinhSach` ('' = tính phí).
- `GET /api/Ve/danh-sach-bieu-gia?chiTinhPhi=true`: chỉ giá của tuyến tính phí; có thêm `maTuyen`.
- `GET /api/Ve/the-mien-phi` [HanhKhach]:
  - Chưa xác thực → 403.
  - Chính sách hết hạn → 409.
  - Ngược lại trả hoặc cấp thẻ (đơn hàng MienPhi 0đ ẩn + `phatHanhVe`; `hetHanLuc` = `GiaVe.hieuLucDen`). Gọi lại không cấp thêm thẻ.
- `POST /api/XacThuc/xac-thuc-dinh-danh`: sau khi xác thực thành công tự gọi cấp thẻ (lỗi cấp thẻ chỉ ghi log).
- `tao-don-hang` từ chối mua `TheMienPhi`. Ví vé trả thêm `maLoaiVe`; tên tuyến của thẻ: "134 tuyến miễn phí (NQ 09/2026)".
- `quet-qr-dan-xe`: `chuyenXeHienTai.mienPhi`. `SoatVe/chuan-bi`: mỗi chuyến có `mienPhi`. `goi-kiem-tra`: `tuyenMienPhi` và `daQuet[].cungChuyen`.
- Soát vé (B4): thẻ miễn phí trên tuyến tính phí → `SAI_PHAM_VI`; thẻ miễn phí đã có lượt trên cùng chuyến → `QUET_TRUNG`. Áp dụng ở quét online (`xacThucVe`), xác nhận thủ công (`xacNhanThuCong`), tra cứu khẩn cấp, đồng bộ offline (`OfflineSyncService`, thêm `SAI_PHAM_VI` vào danh sách mã của thiết bị).
- Sửa lỗi có sẵn: quét online bị từ chối (sai tuyến, hết hạn, chữ ký…) từng gây lỗi 500 vì `NhatKyQuetVe.veId` không được gán. Nay `GhiNhanQuetThatBai` nhận `ve` và gán `veId`; trường hợp không xác định được vé (QR sai định dạng, vé không tồn tại) trả lý do từ chối mà không ghi nhật ký (`veId` bắt buộc do khóa ghép `UQ_NhatKyQuetVe_ve`).

### Máy soát vé (client offline)
- `soatVeOffline.mjs`: `SAI_PHAM_VI` khi `lv === 'TheMienPhi'` và `goi.tuyenMienPhi === false`; thẻ miễn phí trùng khi có lượt `cungChuyen`.
- `khoSoatVe.ts`: lượt quét cục bộ đánh dấu `cungChuyen` khi cùng `session_id` với gói hiện tại.
- Test mới trong `tests/soatVeOffline.test.mjs` (2/2 đạt).

### Mobile hành khách
- "Ví vé QR" và "Mở ví vé QR của tôi" mở bottom sheet: **134 tuyến miễn phí** / **Các tuyến xe buýt khác**.
- Chế độ miễn phí: gọi `the-mien-phi`, hiện QR ngay (dùng lại cơ chế QR động, chống chụp, tăng sáng), ẩn danh sách vé và mua vé. Chưa xác thực thì hiện nút "Xác thực ngay". Offline thì dùng thẻ lưu trong ví offline.
- Chế độ tính phí: ví không gồm thẻ miễn phí; mua vé chỉ liệt kê giá tuyến tính phí (kèm mã tuyến).
- Quét QR xe: tuyến miễn phí → mở thẳng QR thẻ miễn phí; tuyến tính phí → panel "Xe buýt đã nhận diện" với giá đúng tuyến (sửa luôn lỗi panel trước đây đọc sai trường `bienSoXe`/`danhSachGiaVe`).
- Tra cứu tuyến: nhãn "Miễn phí · NQ 09/2026" / "Tính phí", dropdown lọc, số tuyến, cự ly và căn cứ chính sách ở chi tiết.

### Admin web
- Trang Tuyến xe: cột "Cự ly (km)", "Chính sách" (trống = "Tính phí"); form sửa có chọn chính sách giá vé. Từ điển thêm `SAI_PHAM_VI`, `TheMienPhi`.

### Kiểm tra
- `dotnet build` OK, `npx tsc --noEmit` OK, `node --test` 2/2, Admin build OK.
- Chạy backend ở cổng 5099 và gọi API thật: 158/134/24 tuyến; 24 giá tính phí; không mua được thẻ; chưa xác thực → 403; xác thực → cấp thẻ, gọi lại cùng thẻ; gói offline có `tuyenMienPhi` đúng; quét thẻ trên tuyến 03 hợp lệ, quét lại → QUET_TRUNG; quét trên tuyến 05 → SAI_PHAM_VI; QR lạ → `MA_QR_KHONG_DUNG_DINH_DANG`; QR dán xe trả `mienPhi`.
- Dữ liệu test đã dọn (8 phiên soát vé thử trên thiết bị 2). Tài khoản `thinh@smartbus.vn` **giữ trạng thái đã xác thực VNeID và có thẻ miễn phí** (id 20009) để demo.
- **Chưa** test trên thiết bị thật; **chưa** chạy `dotnet test`.

### Giới hạn / còn lại
- Giá theo chặng chưa hỗ trợ (dùng mức cao nhất). Vé HSSV/người cao tuổi cho tuyến tính phí chưa tạo.
- Lộ trình chi tiết (sheet LoTrinh 4.897 điểm) chưa nhập làm trạm dừng.
- Báo cáo thống kê theo Điều 3 / Điều 4 / tính phí chưa làm.

### Test trên thiết bị thật (22/11, 13:48–13:50)
- nubia (máy soát vé), chuyến **Tuyến 03 (miễn phí)**, gói mới 8 vé (có thẻ miễn phí), **tắt Wi-Fi**.
  - Lần 1: "Vé hợp lệ" (NhatKyQuetVe #12, HOP_LE).
  - Lần 2 (QR đã đổi mã): "Từ chối: Vé đã được sử dụng cho lượt này (thẻ miễn phí: đã quét trên chuyến này)" (#13, DA_SU_DUNG). Lý do thiết bị được giữ nguyên khi đồng bộ.
- Bật Wi-Fi → "Đã đồng bộ 2 lượt", hàng đợi 0. Server: **1 LuotDi** cho thẻ miễn phí, 0 cảnh báo (vì lần 2 bị chặn ngay tại máy). ĐẠT.
- Còn lại: thử thẻ miễn phí trên chuyến Tuyến 05 (tính phí) → mong đợi SAI_PHAM_VI.
- nubia, chuyến **Tuyến 05 (tính phí)**, gói mới: quét thẻ miễn phí → "Từ chối: Thẻ miễn phí không áp dụng cho tuyến tính phí". Đồng bộ → server ghi NhatKyQuetVe #14 `SAI_PHAM_VI`, 0 LuotDi. ĐẠT. **Bước 4 đã kiểm thử xong trên thiết bị.**

### Tự đồng bộ khi có mạng (22/11, chiều)
- Trước đây camera soát vé **luôn** kiểm tra cục bộ (`quetVaLuu`) và chỉ gửi server khi bấm "Đồng bộ ngay"; API `xac-thuc-ve` không được màn soát vé dùng.
- `SoatVeScreen.tsx` có thêm `tuDongBo()`: chạy ngay sau mỗi lượt quét nếu máy đang có mạng (`!isOffline`), và chạy khi máy có mạng trở lại (`useEffect` theo `isOffline`). Lỗi mạng thì giữ lượt quét trong hàng đợi. Chỉ hiện thêm thông báo nếu máy chủ từ chối, có cảnh báo thu hồi hoặc lỗi. Nút "Đồng bộ ngay" vẫn giữ.
- Server idempotent theo `maSuKien`, nên tự đồng bộ chạy cùng lúc với nút đồng bộ cũng không ghi trùng.
- `tsc` OK, `node --test` 2/2. Chưa thử trên thiết bị.
