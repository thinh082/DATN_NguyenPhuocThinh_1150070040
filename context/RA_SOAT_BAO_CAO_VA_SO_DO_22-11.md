# Rà soát báo cáo Word và sơ đồ UML/ERD (22/11/2026, chiều)

> Phối hợp: **Devin** (phiên này) và **Gemini** (phiên chat khác).
> - File báo cáo: `word/SmartBus - Bao cao DATN (HoanChinh).docx`
> - File sơ đồ: `word/SmartBus - So do UML va ERD.drawio` (đã backup: `word/SmartBus - So do UML va ERD.backup_truoc_cap_nhat_22-11.drawio`)
> - Bối cảnh thay đổi hôm nay: `context/CAP_NHAT_XAC_THUC_DINH_DANH_2026-11-22.md` (mục 1–13), `context/PLAN_TUYEN_MIEN_PHI_VA_TINH_PHI.md` (mục 10–11, kết quả test thiết bị).
> - Gemini ghi kết quả rà soát của mình vào `context/GEMINI_RA_SOAT.md` để Devin đối chiếu.

## 0. Định vị đề tài (thống nhất với người dùng)

> Nhà nước ban hành NQ 09/2026/NQ-HĐND hỗ trợ 100% giá vé 134 tuyến và thanh toán cho doanh nghiệp vận tải theo **số lượt hành khách đã được xác thực**. SmartBus là phần mềm **hỗ trợ nhà xe** ở khâu xác thực hành khách (QR động TOTP + chữ ký số **ECDSA P-256**, chống giả mạo), ghi nhận lượt đi theo tuyến và chuyến (kể cả khi mất mạng) và cung cấp thông tin tuyến cho hành khách. Dữ liệu lượt đi có chữ ký số, chống quét trùng và có nhật ký, nên có thể dùng làm **căn cứ để nhà nước đối soát và cấp kinh phí cho nhà xe** (chống khai khống).

Vai trò: Quản trị viên = bộ phận điều hành nhà xe; Nhân viên soát vé = nhân viên trên xe; Hành khách = người dân; Nhà nước = bên ngoài (ban hành chính sách, nhận dữ liệu đối soát).

## 1. Sơ đồ draw.io: Devin đã bổ sung 5 trang mới (giữ nguyên 28 trang cũ)

| Trang | Nội dung | Dùng cho Word |
| --- | --- | --- |
| `28_ERD_CapNhat_DinhDanh_MienPhi` | ERD các bảng liên quan: TinhThanh, PhuongXa, NguoiDung (cột mới), ChinhSachMienPhi, TuyenXe (chinhSachMienPhiId, cuLyKm), LoaiVe (TheMienPhi), GiaVe, Ve, PhienSoatVe, NhatKyQuetVe, LuotDi, CanhBaoGianLan... Cột chữ xanh đậm = mới | Mục 2.4 (thay hoặc bổ sung hình ERD) |
| `29_Class_CapNhat_DinhDanh_MienPhi` | Lớp mới/cập nhật: NguoiDung, TinhThanh/PhuongXa, ChinhSachMienPhi, TuyenXe, Ve, ChinhSachTuyen «service», SoatVeService/OfflineSyncService, LuotDi | Mục 2.2 / 2.3 |
| `30_Seq_XacThucDinhDanh_CapThe` | Lưu thông tin hành chính → xác thực CCCD (ảnh) hoặc VNeID/ngân hàng/ví (mô phỏng) → server cấp ngầm thẻ miễn phí | Mục 2.3 |
| `31_Seq_TheMienPhi_SoatVe` | Mở QR 134 tuyến → soát offline: SAI_PHAM_VI, DA_SU_DUNG (1 lượt/chuyến) → tự đồng bộ → server kiểm tra lại, ghi LuotDi/CanhBao | Mục 2.3 |
| `32_LuuDo_ViVeQR_MienPhi_TinhPhi` | Lưu đồ chọn ví miễn phí / tính phí và quét QR dán xe | Mục 2.3 |

Việc còn lại với sơ đồ:
- **Cần mở bằng draw.io để kiểm tra bố cục** và xuất PNG vào `word/diagrams/` (máy chưa có draw.io CLI). Đề xuất tên: `Hinh_2_xx_ERD_CapNhat.png`, `Hinh_2_xx_Class_CapNhat.png`, `Hinh_2_xx_Seq_XacThucDinhDanh.png`, `Hinh_2_xx_Seq_TheMienPhi_SoatVe.png`, `Hinh_2_xx_LuuDo_ViVeQR.png`.
- Trang cũ nên chỉnh tay (nhỏ):
  - `01_UseCase_TongQuat`: thêm use case "Xác thực định danh", "Sử dụng 134 tuyến miễn phí", "Quản lý nhóm đối tượng" (Admin), "Lọc tuyến miễn phí/tính phí".
  - `15_Seq_SoatVe_Offline` / `05_Activity_SoatVe_Offline`: thêm nhánh SAI_PHAM_VI, 1 lượt/chuyến cho thẻ miễn phí, tự đồng bộ khi có mạng.
  - `22_LuuDo_QuetQRDanhXe`: thêm nhánh "tuyến miễn phí → mở QR thẻ miễn phí".
  - `14_Seq_MuaVe_KySo`: ghi chú "chỉ áp dụng tuyến tính phí".
  - `24_UI_LuuHoSo`: tách 2 màn "Hồ sơ cá nhân (2 bước)" và "Hồ sơ ưu tiên".

## 2. Báo cáo Word: các chỗ còn thiếu hoặc lệch so với hệ thống hiện tại

### Chung
- **Thiếu chú thích hình cho các sơ đồ**: báo cáo có 31 ảnh nhưng chỉ 5 ảnh UI có chú thích (Hình 2.1–2.5). Khoảng 26 ảnh sơ đồ ở mục 2.2–2.4 chưa có "Hình 2.x ..." và danh mục hình chưa có. **Đánh số lại toàn bộ** (sơ đồ trước, UI sau) rồi cập nhật danh mục hình và bảng.
- Thống nhất tên thiết bị: báo cáo ghi "Nubia A36", adb báo model **Z2472** (Android 15). Kiểm tra lại tên thương mại.
- Thống nhất thuật ngữ: ECDSA P-256 (không phải "EC DCDSA").

### Mở đầu và Chương 1
- Bổ sung căn cứ:
  - **NQ 29/2024/NQ-HĐND** (Điều 4: miễn 100% cho người có công, người khuyết tật, người cao tuổi, trẻ em dưới 6 tuổi; NQ 09 loại trừ các đối tượng này).
  - **Hướng dẫn triển khai giai đoạn 2 từ 01/10/2026** của Trung tâm Quản lý GTCC – Sở Xây dựng: xác thực bằng CCCD/VNeID mức 2/QR MultiGo/thẻ ngân hàng/ví điện tử; thanh toán cho nhà xe theo lượt đã xác thực.
- Đoạn định vị đề tài (mục 0) đặt ở Mở đầu hoặc mục 1.1.
- Bảng 1.1 có thể thêm cột so sánh với **MultiGo + đầu đọc trên xe** (hệ thống thực tế).
- Tài liệu tham khảo cần thêm:
  - NQ 29/2024/NQ-HĐND (thuvienphapluat / vbpl.vn).
  - Bản NQ 09 trên Công báo TP.HCM (tphcm.cdnchinhphu.vn).
  - Tuổi Trẻ 24/09/2026, Thanh Niên 13/08/2026, CSGT TP.HCM 09/07/2026, tdtt.gov.vn 13/11/2026, ANTV 01/10/2026.
  - provinces.open-api.vn (danh mục hành chính sau sáp nhập).
  - vietnammoi.vn 24/05/2025 (dữ liệu 24 tuyến tính phí và giá vé).

### Chương 2
- **2.1 Yêu cầu**: thêm chức năng
  - Thông tin hành chính, địa chỉ 2 cấp.
  - Xác thực định danh (CCCD ảnh / VNeID / ngân hàng / ví, mô phỏng).
  - Thẻ đi xe miễn phí cấp tự động.
  - Ví vé QR tách 2 mục (134 tuyến miễn phí / tuyến tính phí).
  - Tra cứu tuyến lọc theo chính sách.
  - Admin: quản lý nhóm đối tượng ưu tiên, gán chính sách cho tuyến.
  - Máy soát: tự đồng bộ khi có mạng, báo trạng thái mạng thực.
- **2.2 Đặc tả use case**: thêm 3 bảng mới:
  - **UC Xác thực định danh và cấp thẻ miễn phí**.
  - **UC Sử dụng 134 tuyến miễn phí (mở QR, soát vé)**.
  - **UC Quản lý nhóm đối tượng ưu tiên và chính sách tuyến (Admin)**.

  Sửa các bảng cũ:
  - Bảng 2.2 (hồ sơ ưu tiên): chọn nhóm bằng dropdown; bỏ bước người dùng tự nhập thời hạn (mặc định 1 năm).
  - Bảng 2.3 (mua vé): chỉ tuyến tính phí; giá = mức cao nhất toàn tuyến.
  - Bảng 2.4/2.5 (soát/đồng bộ): thêm luồng thay thế SAI_PHAM_VI, DA_SU_DUNG theo chuyến, tự đồng bộ, giữ lý do từ chối của thiết bị khi đồng bộ.
- **2.3**: chèn 3 sơ đồ mới (trang 30, 31, 32) và Class cập nhật (trang 29).
- **2.4 CSDL**:
  - Bảng NguoiDung, TinhThanh, PhuongXa đã có, đúng.
  - **Thiếu bảng `ChinhSachMienPhi`** (id, ma, ten, canCu, hieuLucTu, hieuLucDen, yeuCauXacThuc, trangThai).
  - **Bảng TuyenXe thiếu** `chinhSachMienPhiId` (FK, NULL = tính phí) và `cuLyKm decimal(6,2)`.
  - **Bảng LoaiVe**: giá trị `TheMienPhi`.
  - Bảng GiaVe: ghi chú `tuyenXeId NULL = toàn mạng`, dòng TheMienPhi hiệu lực đến 31/12/2026.
  - Bảng NhatKyQuetVe: danh sách mã lý do (HOP_LE, DA_SU_DUNG, QUET_TRUNG, SAI_TUYEN, **SAI_PHAM_VI**, SAI_CHU_KY, SAI_TOTP, VE_HET_HAN, KHONG_CO_DU_LIEU...).
  - Chèn ERD cập nhật (trang 28).
- **2.5 Giao diện**: thêm ảnh thực tế: Hồ sơ cá nhân (2 bước + dropdown tỉnh/xã), Xác thực định danh, bảng chọn Ví vé QR, Ví 134 tuyến miễn phí, Tra cứu tuyến có lọc, Admin Nhóm đối tượng và Tuyến xe cột Chính sách.

### Chương 3
- 3.1: "134 tuyến và 134 chuyến" → **158 tuyến (109 Điều 3 + 25 Điều 4 + 24 tính phí), 158 chuyến đang chạy**. Thiết bị: nubia Z2472 (Android 15) làm máy soát; máy ảo SmartBus_API31 (Android 12) làm máy hành khách.
- 3.2: chèn bộ ảnh chạy thực tế (đang để placeholder "[Chèn bộ ảnh ...]").
- **Bảng 3.1** cập nhật:
  - `node --test tests/soatVeOffline.test.mjs` → **2/2 đạt** (thêm ca thẻ miễn phí).
  - `dotnet build` OK.
  - Có thể thêm dòng "Kiểm thử API end-to-end (backend cổng 5099)": 15 kiểm tra đạt (lọc tuyến 158/134/24, cấp thẻ, 403 khi chưa xác thực, SAI_PHAM_VI, QUET_TRUNG, QR sai định dạng...).
  - `dotnet test` **chưa chạy lại** (ghi vào DB thật; cần người dùng đồng ý).
- **Bảng 3.2** điền kết quả thực (22/11/2026):

| Mã ca | Kết quả thực tế |
| --- | --- |
| TC-02 Quét QR dán xe | Nhận diện xe 51B-123.45 tuyến 01; tuyến miễn phí → mở QR thẻ miễn phí |
| TC-03 Soát hợp lệ khi mất mạng | ĐẠT: nubia tắt Wi-Fi/data, vé lượt hợp lệ, hàng đợi 1 (10:39); thẻ miễn phí Tuyến 03 hợp lệ (13:48) |
| TC-04 Quét lại | ĐẠT: vé lượt → DA_SU_DUNG (10:41); thẻ miễn phí cùng chuyến, QR đã đổi mã → DA_SU_DUNG (13:49) |
| TC-05 Hai máy cùng quét offline | Chỉ có test tự động `OfflineSyncIntegrationTests` (1 lượt ChapNhan + 1 QUET_TRUNG). Chưa test tay vì chỉ 1 máy có camera |
| **TC-06 (mới)** Đặt vé trùng | ĐẠT: bấm đúp 4 lần (kể cả GPRS giả lập) → 1 đơn; 2 request đồng thời cùng maYeuCau → cùng đơn 20008, 1 vé |
| **TC-07 (mới)** Đồng bộ sau khi có mạng | ĐẠT: "Đã đồng bộ 2 lượt", server 1 LuotDi, lý do DA_SU_DUNG giữ nguyên |
| **TC-08 (mới)** Thẻ miễn phí trên tuyến tính phí | ĐẠT: Tuyến 05 → "Thẻ miễn phí không áp dụng cho tuyến tính phí", server SAI_PHAM_VI, 0 LuotDi (13:53) |
| **TC-09 (mới)** Trạng thái mạng | ĐẠT: tắt Wi-Fi khoảng 10 giây → "Chế độ ngoại tuyến"; hạn gói hiện ngày + giờ |

  - TC-01 (FLAG_SECURE) và thời gian xử lý p95 **chưa đo**: cần đo trên bản release hoặc ghi rõ "bản debug cho phép chụp để quay demo".

### Kết luận, Phụ lục
- Hạn chế và hướng phát triển thêm:
  - VNeID/ngân hàng/ví đang mô phỏng.
  - Chưa tính giá theo chặng (dùng mức cao nhất).
  - Chưa nhập 4.897 điểm lộ trình chi tiết làm trạm.
  - Chưa có báo cáo lượt đi theo Điều 3 / Điều 4 / tính phí để quyết toán.
  - Chưa có nút làm mới gói soát vé giữa phiên.
- **Phụ lục 1**: "Chạy tuần tự 5 tệp" → **8 tệp 01→08**, chạy lại 05 sau 08, các script 05/07/08 cần `-I` (xem README).
- Phụ lục 2: tài khoản demo giữ nguyên. Ghi chú: `thinh@smartbus.vn` đã xác thực VNeID và có thẻ miễn phí.

## 3. Phân công đề xuất

| Việc | Ai |
| --- | --- |
| Sơ đồ draw.io (5 trang mới, chỉnh trang cũ) | Devin đã làm 5 trang mới; người dùng mở draw.io kiểm tra và xuất PNG |
| Soạn nội dung 3 bảng use case mới + bảng CSDL ChinhSachMienPhi + sửa TuyenXe/LoaiVe | Gemini (hoặc Devin nếu được yêu cầu) |
| Rà chú thích hình, danh mục hình/bảng, đánh số lại | Gemini |
| Cập nhật Chương 3 (môi trường, Bảng 3.1, 3.2) theo số liệu mục 2 | Devin/Gemini |
| Chương 1 + Tài liệu tham khảo bổ sung | Gemini |

## 4. Prompt gửi Gemini

```text
Bạn đang cùng Devin rà soát báo cáo đồ án SmartBus. Hãy đọc:
- word/SmartBus - Bao cao DATN (HoanChinh).docx (báo cáo đang viết dở)
- context/RA_SOAT_BAO_CAO_VA_SO_DO_22-11.md (danh sách Devin đã rà, định vị đề tài ở mục 0)
- context/CAP_NHAT_XAC_THUC_DINH_DANH_2026-11-22.md và context/PLAN_TUYEN_MIEN_PHI_VA_TINH_PHI.md (thay đổi hệ thống ngày 22/11)
Nhiệm vụ:
1. Kiểm tra lại danh sách ở mục 2 file rà soát: bổ sung chỗ còn sót, ghi rõ vị trí (chương/mục/bảng/hình) và nội dung cần sửa. KHÔNG sửa code.
2. Soạn sẵn nội dung (văn phong báo cáo, tiếng Việt có dấu) cho: 3 bảng đặc tả use case mới (Xác thực định danh và cấp thẻ miễn phí; Sử dụng 134 tuyến miễn phí; Quản lý nhóm đối tượng và chính sách tuyến), bảng cấu trúc ChinhSachMienPhi, các dòng bổ sung cho TuyenXe/LoaiVe/NhatKyQuetVe, đoạn định vị đề tài, và các tài liệu tham khảo mới theo đúng định dạng [số] của báo cáo.
3. Đề xuất đánh số lại toàn bộ Hình 2.x (sơ đồ trước, giao diện sau) kèm tên hình.
4. Ghi toàn bộ kết quả vào file context/GEMINI_RA_SOAT.md (Markdown), mỗi mục ghi rõ "Vị trí trong Word" và "Nội dung đề xuất".
Lưu ý: thuật ngữ đúng là ECDSA P-256; dữ liệu: 158 tuyến = 109 (Điều 3) + 25 (Điều 4) + 24 tính phí; VNeID/ngân hàng/ví là mô phỏng.
```

## 5. Đã cập nhật Word (Devin, 22/11 chiều)
- File mới: `word/SmartBus - Bao cao DATN (HoanChinh) - Devin cap nhat 22-11.docx`. Bản gốc giữ nguyên, backup: `word/SmartBus - Bao cao DATN (HoanChinh).backup_truoc_devin_22-11.docx`.
- Đã mở bằng Word (COM) và cập nhật 4 mục lục/danh mục: 76 trang, 36 hình, 45 bảng.
- Nội dung đã sửa:
  - Chương 1: đoạn bối cảnh NQ 09 / NQ 29 / xác thực từ 01/10 và định vị đề tài (trước mục 1.2).
  - Bảng use case mới 2.8–2.10 (định danh và thẻ miễn phí; 134 tuyến miễn phí; nhóm đối tượng và chính sách tuyến). Các bảng CSDL được đánh số lại 2.11–2.39, thêm **Bảng 2.40 ChinhSachMienPhi**. TuyenXe có thêm `chinhSachMienPhiId`, `cuLyKm`; LoaiVe có thêm TheMienPhi.
  - Chú thích **Hình 2.1–2.29** cho toàn bộ sơ đồ, gồm 5 hình mới (2.22–2.24 ở mục 2.3; 2.28–2.29 ở mục 2.4). Ảnh chụp bằng plugin Draw.io của Orca, lưu ở `word/diagrams/Hinh_*.png`. Hình giao diện đánh số lại 2.30–2.34.
  - Chương 3: môi trường 158 tuyến; Bảng 3.1 (2/2 + dòng API end-to-end 15/15); Bảng 3.2 điền kết quả TC-01..05 và thêm TC-06..09.
  - Kết luận: đoạn hạn chế. Phụ lục 1: 8 tệp SQL. Tài liệu tham khảo [16]–[22].
- Chưa làm: ảnh chụp giao diện thực tế cho mục 2.5/3.2 (placeholder vẫn còn); sửa tay các trang sơ đồ cũ (Use case tổng quát...); tên thiết bị "Nubia A36" và "Z2472" cần người dùng xác nhận.
- Đã chụp 15 ảnh giao diện thật (`word/screenshots/`): 9 ảnh app hành khách (máy ảo, adb), 1 ảnh máy soát vé nubia (kết quả SAI_PHAM_VI), 5 ảnh Web Admin (trình duyệt Orca). Đã chèn vào mục 3.2 thay placeholder, chú thích **Hình 3.1–3.15**. Word cập nhật mục lục: 88 trang, 51 hình. Backup trước khi chèn ảnh: `...Devin cap nhat 22-11.backup_truoc_anh.docx`.
- Còn thiếu: ảnh máy soát vé lúc báo "Vé hợp lệ" và "Đã sử dụng" (cần người dùng quét QR bằng nubia).
- Sửa chú thích (22/11 tối): sơ đồ dùng nhãn **Sơ đồ 2.1–2.29** (bỏ chữ 'Sơ đồ' lặp trong tên, xóa 24 chú thích trùng do Devin chèn thêm, đưa chú thích fc_04/ssms về đúng vị trí); hình phác thảo UI là **Hình 2.1–2.5**; ảnh thực tế **Hình 3.1–3.15**; hết lỗi 'Error! Bookmark not defined'. Backup trước khi sửa: ...backup_truoc_sua_chu_thich.docx.
- 22/11 tối: theo yêu cầu đã xóa Sơ đồ triển khai kiến trúc (sơ đồ còn 2.1–2.28) và xóa toàn bộ Phụ lục; vẽ lại ERD/Class cập nhật (đen trắng, không chồng lấn, page 28/29 trong drawio) và thay ảnh trong Word. File: 81 trang, 50 hình. Backup: ...backup_truoc_xoa_phuluc.docx.
