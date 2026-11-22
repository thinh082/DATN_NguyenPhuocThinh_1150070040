# Cập nhật 22/11/2026: Địa chỉ hành chính, Xác thực định danh, Nhóm ưu tiên động

> Ghi chú bàn giao cho các AI/dev khác. Chưa commit git (repo vẫn còn các thay đổi dở dang khác từ trước).

## 1. Bối cảnh nghiệp vụ

- NQ 09/2026/NQ-HĐND TP.HCM (`word/09_2026_NQ-HDND_711744.docx`): miễn 100% giá vé cho **mọi hành khách** trên 134 tuyến, hiệu lực 01/07/2026 đến 31/12/2026. Không áp dụng cho tuyến liên tỉnh, tuyến sân bay, xe du lịch. Điểm d khoản 1 Điều 6 yêu cầu dữ liệu hành khách phải được kiểm chứng.
- **Yêu cầu xác thực KHÔNG nằm trong văn bản NQ 09** mà trong hướng dẫn triển khai của Sở Xây dựng (báo Thanh Niên 13/08/2026, CSGT TP.HCM 09/07/2026):
  - Từ 01/07 đến 30/09/2026: miễn phí, không cần xác thực.
  - Từ 01/10 đến 31/12/2026: miễn phí nhưng hành khách phải xác thực khi lên xe bằng CCCD, VNeID, thẻ ngân hàng (Napas/Visa/Mastercard) hoặc ví điện tử.
- Phân biệt hai khái niệm:
  - **Xác thực định danh**: biết hành khách là ai, áp dụng cho mọi người.
  - **Hồ sơ ưu tiên**: chứng minh thuộc nhóm được giảm giá như HSSV hay người cao tuổi. Vẫn giữ để dùng sau 31/12/2026 hoặc cho tuyến ngoài NQ 09.
- Đối tượng mặc định khi tạo tài khoản là nhóm `PhoThong` (không cần xác minh).

## 2. Phạm vi người dùng đã chốt

| Hạng mục | Quyết định |
| --- | --- |
| Địa chỉ | 2 cấp sau sáp nhập: Tỉnh/Thành → Phường/Xã, cộng một ô địa chỉ chi tiết. Nạp **34 tỉnh/thành + 168 phường/xã TP.HCM** (tỉnh khác chưa có danh mục phường/xã) |
| Xác thực CCCD | Chụp hoặc chọn ảnh CCCD → lưu vào `TepMinhChung` → **tự duyệt hoàn toàn** (không qua admin) |
| VNeID / Ngân hàng / Ví điện tử | 3 nút **mô phỏng**, bấm là báo thành công (không có hạ tầng thật) |
| Ràng buộc mua vé | **CHƯA làm**. Chưa xác thực vẫn mua vé bình thường. Chưa seed giá 0đ theo NQ 09 |
| Admin | Chỉ CRUD nhóm đối tượng. **Biểu giá cho nhóm mới vẫn phải thêm bằng SQL** (`GiaVe`), nếu không thì nhóm mới không có vé ưu tiên để mua |

## 3. Database

- Đã backup trước khi sửa: `E:\SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup\SmartBusDb_truoc_xac_thuc_20261122.bak`
- Script mới (đã chạy trên `SmartBusDb` local): `Server/Database/06_dia_chi_va_xac_thuc_dinh_danh.sql`. Script idempotent, chỉ thêm mới, không xóa dữ liệu.
  - Bảng `TinhThanh(ma int PK, ten, loai)`: 34 dòng.
  - Bảng `PhuongXa(ma int PK, ten, loai, maTinhThanh FK)`: 168 dòng của TP.HCM (mã 79). Mã lấy theo provinces.open-api.vn API v2.
  - Cột mới trong `NguoiDung`:
    - `diaChi nvarchar(1000)`: địa chỉ chi tiết (số nhà, đường). **Không** chứa tên phường/tỉnh; tên đầy đủ ghép lúc hiển thị.
    - `maTinhThanh int FK`, `maPhuongXa int FK`: để chọn lại dropdown.
    - `phuongThucXacThuc varchar(20)`: CHECK in (`CCCD`, `VNeID`, `NganHang`, `ViDienTu`). NULL nghĩa là chưa xác thực.
    - `ngayXacThuc datetime2(3)`, `minhChungXacThuc nvarchar(500)` (đường dẫn `/api/XacThuc/minh-chung/{maTep}` khi xác thực bằng CCCD).
- Chạy lại trên máy khác: `sqlcmd -S localhost -E -C -b -f i:65001 -d SmartBusDb -i "Server/Database/06_dia_chi_va_xac_thuc_dinh_danh.sql"`
- Lưu ý: khi UPDATE bảng `NguoiDung` bằng sqlcmd phải thêm `-I` (QUOTED_IDENTIFIER) vì bảng có filtered index.

## 4. Backend (`Server/`)

- Entity mới: `Entities/TinhThanh.cs`, `Entities/PhuongXa.cs`. `Entities/NguoiDung.cs` có thêm 6 thuộc tính và navigation `tinhThanh`, `phuongXa`. Đã cấu hình trong `Data/SmartBusDbContext.cs`.
- `Models/XacThucDto.cs`: `CapNhatCaNhanDto` có thêm `diaChi`, `maTinhThanh`, `maPhuongXa`; thêm `XacThucDinhDanhDto { phuongThuc, duongDanMinhChung }`.
- `Services/XacThucService.cs`:
  - Tách helper `ChuanHoaMinhChung(duongDan, nguoiDungId, choPhepUrl)`, dùng chung cho hồ sơ ưu tiên (cho phép URL) và xác thực CCCD (chỉ chấp nhận tệp đã upload của chính user).
  - `capNhatThongTin`: kiểm tra tỉnh tồn tại và phường thuộc đúng tỉnh. Nếu user đổi số CCCD trong khi đang xác thực bằng CCCD thì **reset trạng thái xác thực**.
  - `layThongTinCaNhan`: trả thêm `diaChi`, `maTinhThanh`, `tenTinhThanh`, `maPhuongXa`, `tenPhuongXa`, `phuongThucXacThuc`, `ngayXacThuc`.
  - Hàm mới: `layTinhThanh()`, `layPhuongXa(maTinhThanh)`, `xacThucDinhDanh(userId, dto)`. Xác thực CCCD yêu cầu tài khoản đã lưu số CCCD và có ảnh đã upload.
- API mới trong `Controllers/XacThucController.cs`:
  - `GET /api/XacThuc/dia-chi/tinh-thanh` [Authorize]
  - `GET /api/XacThuc/dia-chi/phuong-xa?maTinhThanh=79` [Authorize]
  - `POST /api/XacThuc/xac-thuc-dinh-danh` [HanhKhach], body `{ "phuongThuc": "CCCD|VNeID|NganHang|ViDienTu", "duongDanMinhChung": "/api/XacThuc/minh-chung/{guid}" }`
- API mới trong `Controllers/AdminController.cs` (mục 3C):
  - `GET /api/Admin/nhom-doi-tuong`: tất cả nhóm, kèm `soHoSo`.
  - `POST /api/Admin/nhom-doi-tuong`: body `{ id (0 = thêm mới), maNhom, tenNhom, canXacMinh, trangThai }`. `maNhom` chỉ gồm chữ không dấu, số và `_`, tối đa 30 ký tự, không trùng. Nhóm `PhoThong` không được đổi mã, bật xác minh hay tạm khóa. Sau khi lưu sẽ xóa cache `smartbus:cache:bieu-gia`.
- Mobile chỉ hiển thị nhóm có `trangThai = 1` **và** `canXacMinh = 1` (giữ nguyên logic cũ của `layNhomUuTien`).

## 5. Mobile (`Client/`)

- `components/MobileUi.tsx`: thêm `SelectField` (dropdown dạng bottom sheet trong `Modal`, tự hiện ô tìm kiếm khi có hơn 8 lựa chọn) và hằng `footerInset` (Android 48, iOS 34) làm khoảng đệm đáy để không bị thanh điều hướng che. Màn camera chụp CCCD cũng dùng `footerInset`.
- `components/StepPager.tsx` (mới): pager vuốt ngang nhiều bước, tách ra từ `HoSoHanhKhach.tsx` cũ. Có `ref.goToStep(i)` và `formStyles` dùng chung.
- `HoSoCaNhan.tsx` (mới; tiêu đề nhóm nút đã đổi thành "Phương thức khác", bỏ chữ "(mô phỏng)" theo yêu cầu), ô "Hồ sơ cá nhân" trên trang chủ (`manHinhCon = 'capNhatThongTin'`):
  - Bước 1, Thông tin hành chính: họ tên, email, SĐT, CCCD, dropdown Tỉnh/Thành (mặc định TP.HCM), dropdown Phường/Xã, ô địa chỉ. Bấm Lưu sẽ gọi `PUT thong-tin-ca-nhan` rồi tự chuyển sang bước 2.
  - Bước 2, Xác thực định danh: thẻ trạng thái; nút "Chụp ảnh CCCD" (Modal `CameraView` + `takePictureAsync({quality:0.6})`, có khung tỷ lệ thẻ 1.586); nút "Chọn ảnh CCCD có sẵn" (`DocumentPicker`); 3 nút mô phỏng VNeID / Thẻ ngân hàng / Ví điện tử.
- `HoSoHanhKhach.tsx` (viết lại), ô **mới** "Hồ sơ ưu tiên" (`manHinhCon = 'hoSoUuTien'`):
  - Bước 1: danh sách hồ sơ đã nộp kèm trạng thái.
  - Bước 2: dropdown nhóm (tải động từ API mỗi lần mở màn hình), upload minh chứng hoặc URL, gửi hồ sơ.
  - Đã **bỏ bước nhập thời hạn**. Client tự gửi hiệu lực từ hôm nay đến +365 ngày (backend `nopHoSoUuTien` không đổi).
- `services/xacThucService.ts`: thêm type `DonViHanhChinh`, `PhuongThucXacThuc`, `ThongTinCaNhanChiTiet` và hàm `layThongTinChiTiet`, `layTinhThanh`, `layPhuongXa`, `xacThucDinhDanh`. `capNhatThongTin` nhận thêm các trường địa chỉ.
- `App.tsx`: import `HoSoCaNhan`; lưới tiện ích nay có 6 ô (đổi tên "Hồ sơ & ưu tiên" thành "Hồ sơ cá nhân", thêm "Hồ sơ ưu tiên" với icon `ribbon-outline`).
- Không thêm thư viện mới (`expo-camera` và `expo-document-picker` đã có sẵn).

## 6. Admin web (`Admin/src/main.tsx`)

- Thêm trang `groups` "Nhóm đối tượng" (menu QUẢN LÝ, icon `BadgeCheck`), dùng cơ chế `sections` + `DataPage` có sẵn: liệt kê, Thêm mới, Sửa.
- `Field` có thêm `labels?: [string, string]` để select boolean hiển thị "Có/Không" thay cho "Hoạt động/Tạm khóa". `format()` hiển thị cột `canXacMinh` là Có/Không.

## 7. Đã kiểm tra

- `dotnet build Server -o %TEMP%/sbbuild`: OK, 0 lỗi. Build mặc định bị khóa file vì backend đang chạy ở cổng 5056.
- Chạy bản build mới ở cổng 5099 và gọi API thật:
  - Danh mục tỉnh và phường/xã đúng.
  - Chọn phường không thuộc tỉnh bị chặn.
  - Lưu địa chỉ UTF-8 đúng.
  - Upload ảnh rồi xác thực CCCD OK; VNeID OK.
  - Phương thức sai bị chặn; CCCD thiếu ảnh bị chặn.
  - Admin: liệt kê nhóm; mã sai, mã trùng và khóa PhoThong đều bị chặn; tạo nhóm mới thì app hành khách thấy ngay (nhóm test đã xóa).
- Dữ liệu tài khoản demo `thinh@smartbus.vn` đã được trả về trạng thái ban đầu (địa chỉ và xác thực = NULL). Tệp ảnh test (`icon.png`) có thể còn trong thư mục lưu minh chứng hoặc Cloudinary (đã xóa bản ghi DB).
- `Client`: `npx tsc --noEmit` OK. `Admin`: `npm run build` OK.
- **Chưa** chạy `dotnet test Server.Tests` vì test ghi vào DB thật. **Chưa** test UI trên điện thoại (camera, dropdown).

## 8. Việc còn lại / gợi ý

1. **Khởi động lại backend** (cổng 5056) để nhận API mới.
2. Thử trên thiết bị thật: chụp CCCD (quyền camera), dropdown, chuyển bước sau khi lưu.
3. (Tùy chọn) Ràng buộc: chưa xác thực thì không mua được vé 0đ trên 134 tuyến từ 01/10/2026. Cần kiểm tra trong `VeService` lúc tạo đơn và seed `GiaVe` 0đ với `canCuChinhSach = 'NQ 09/2026/NQ-HĐND'`, hiệu lực 01/07 đến 31/12/2026.
4. (Tùy chọn) Admin: hiện trạng thái xác thực và ảnh CCCD ở trang Tài khoản; màn hình thêm `GiaVe` cho nhóm mới.
5. Báo cáo: ghi rõ căn cứ là "NQ 09/2026 + hướng dẫn triển khai của Sở Xây dựng", và các phương thức VNeID / ngân hàng / ví là **mô phỏng**.

## 9. Kiểm thử thủ công trên thiết bị (22/11, sáng)

Máy ảo `emulator-5554` (SmartBus_API31) đóng vai hành khách; điện thoại nubia Z2472 (`LZ0A36VNYD5002773`) đóng vai máy soát vé. Cả hai đã cài bản debug `com.anonymous.Client` (có SQLCipher) và lấy JS từ Metro.

| Test | Kết quả |
| --- | --- |
| TC1 Đặt vé trùng | ĐẠT. Bấm đúp trên UI (kể cả giả lập GPRS) chỉ tạo 1 đơn mỗi lần. 2 request đồng thời cùng `maYeuCau` → cùng đơn 20008, 1 vé |
| TC2 Soát offline, đồng bộ sau | ĐẠT. Tắt Wi-Fi/data trên nubia → quét hợp lệ, hàng đợi 1. Bật mạng → "Đã đồng bộ 2 lượt" |
| TC3 Vé trùng trên cùng máy offline | ĐẠT. Lần quét 2 bị từ chối `DA_SU_DUNG`; server chỉ có 1 `LuotDi` |
| Hai máy khác nhau cùng quét một vé offline | CHƯA test tay (chỉ có test tự động `OfflineSyncIntegrationTests`) |

Dữ liệu test còn lại: tài khoản `thinh@smartbus.vn` có thêm 5 vé miễn phí (đơn 20003, 20005–20008); nubia còn mở 1 phiên soát vé (thiết bị 1, chuyến 1).

## 10. Sửa 4 lỗi phát hiện khi test

1. **Mất lý do từ chối khi đồng bộ**: `DongBoSuKienOfflineDto` có thêm `maLyDoTaiThietBi`. Client (`khoSoatVe.dongBoNhatKy`) gửi cột `reason`. `OfflineSyncService` lưu lý do đó nếu thuộc danh sách mã hợp lệ của thiết bị, ngược lại vẫn ghi `THIET_BI_TU_CHOI`. Lý do không nằm trong `bamDuLieu`, nên tính idempotent không đổi.
2. **Mã thô trên màn soát vé**: thêm `Client/constants/lyDoSoatVe.ts` (`dienGiaiLyDo`). `SoatVeScreen` hiện "Từ chối: Vé đã được sử dụng cho lượt này". Admin: từ điển `human` có thêm các mã lý do/cảnh báo, nên cột lý do trong Nhật ký soát vé hiển thị tiếng Việt.
3. **Huy hiệu trực tuyến sai**: thêm `GET /api/ping` (`Server/Program.cs`, không cần đăng nhập) và hook `useKetNoiMayChu` (`Client/services/ketNoi.ts`, ping mỗi 5 giây, timeout 3 giây; có phản hồi HTTP bất kỳ = trực tuyến). `SoatVeScreen` coi là ngoại tuyến khi phiên đăng nhập offline **hoặc** không gọi được máy chủ. Đã kiểm tra trên nubia: tắt Wi-Fi khoảng 10 giây thì đổi sang "Chế độ ngoại tuyến".
4. **Hạn gói chỉ có giờ**: nay hiển thị dạng `10:38 23/11/2026`.

Kiểm tra: `npx tsc --noEmit` OK, `node --test` OK, Admin build OK, `dotnet build` OK. **Cần khởi động lại backend** để có `/api/ping` và lưu lý do mới (hook vẫn chạy với backend cũ vì 404 cũng được tính là có mạng).

## 11. Rà soát chuỗi không dấu / mã có `_` (chưa sửa, chờ quyết định)

- Backend: khoảng **80** thông báo lỗi trong `ThatBai(...)` và **60** thông báo trong `throw new ...Exception(...)` viết không dấu. Các thông báo này đi thẳng lên app/admin qua `e.message`. Ngoài ra **38** thông báo thành công không dấu (ít khi hiển thị). Tập trung nhiều ở `XacThucService/Controller`, `AdminController`, `VanHanhController/Service`, `VeController`, `NguoiDungController`, `OfflineSyncService`. Các thông báo mình thêm hôm nay trong `XacThucService` cũng không dấu (theo style cũ).
- **27** chỗ `"Loi he thong: " + ex.Message`: lộ thông báo exception (thường là tiếng Anh) ra người dùng.
- Log/cấu hình không dấu (`CacheService`, `MatMaService`, `SigningKeyService`, `Program.cs`): chỉ dev thấy, có thể giữ nguyên.
- Dữ liệu DB không dấu (từ `DuLieuKhoiTao.cs`): 3 tên người dùng demo (`Nguyen Phuoc Thinh`, …), 1 thiết bị (`May Soat Ve Xe 51B-123.45`), 4 trạm, 4 lộ trình, 7 `GiaVe.canCuChinhSach`.
- Nội dung cảnh báo gian lận mới (`OfflineSyncService`): "Phat hien luot quet trung khi dong bo.", "Ve bi thu hoi sau khi tai goi ngoai tuyen." (hiện trên trang Cảnh báo của admin).
- Client: chỉ còn `services/api.ts` có fallback "Loi ket noi den may chu". Các mã trạng thái đã được map sang nhãn tiếng Việt.

## 12. Đã sửa 3 mục ưu tiên cao của phần rà soát (22/11, trưa)

1. **Thông báo lỗi backend có dấu**: 111 chuỗi trong `ThatBai(...)` và `throw new ...Exception(...)` ở `Server/Controllers`, `Server/Services`. Gồm cả nội dung cảnh báo gian lận trong `OfflineSyncService` ("Phát hiện lượt quét trùng khi đồng bộ.", …). `KetQuaPhanHoi` mặc định đổi thành "Thành công" / "Thất bại".
   - Cố ý giữ không dấu: lỗi cấu hình dành cho dev (`MatMaService`, `SigningKeyService`, "Missing JWT secret").
   - Đã sửa tiếp: 52 thông báo **thành công** trong `ThanhCong(...)` và `thongBao = "..."` (52 lượt thay, 52 chuỗi khác nhau) và fallback `Client/services/api.ts` thành "Lỗi kết nối đến máy chủ". Không còn thông báo trả về người dùng nào viết không dấu; chỉ còn lỗi cấu hình cho dev.
2. **`"Loi he thong: " + ex.Message`** (27 chỗ) được thay bằng `KetQuaPhanHoi.LoiHeThong(ex[, 400])`:
   - Nếu exception do code SmartBus ném ra (`ex.Source == "Server"`, trừ NullReference/InvalidCast/IndexOutOfRange/Format), trả nguyên `ex.Message` (đã có dấu), giữ hành vi thông báo nghiệp vụ như cũ.
   - Ngược lại (lỗi EF/SQL/.NET), trả "Hệ thống đang gặp sự cố, vui lòng thử lại sau." và ghi chi tiết ra `Console.Error` với tiền tố `[Loi]`.
3. **Dữ liệu demo có dấu**:
   - Backup trước khi sửa: `E:\SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup\SmartBusDb_truoc_du_lieu_co_dau_20261122.bak`.
   - Script mới (đã chạy): `Server/Database/07_du_lieu_demo_co_dau.sql`. Chỉ UPDATE các dòng có đúng giá trị không dấu cũ (so sánh binary), nên chạy lại không ảnh hưởng. Phải chạy với `-I`.
   - Phạm vi: 3 người dùng demo (Quản trị viên hệ thống / Nhân viên soát vé 01 / Nguyễn Phước Thịnh), thiết bị "Máy soát vé xe 51B-123.45", 4 trạm và địa chỉ, 4 lộ trình, 2 tên tuyến seed, 7 căn cứ giá vé, 2 nội dung cảnh báo.
   - `Server/Data/DuLieuKhoiTao.cs` sửa 24 chuỗi tương ứng. Seed kiểm tra tồn tại theo khóa (email, mã trạm, mã tuyến, chiều, biển số), nên đổi tên không gây seed trùng.
- Kiểm tra: `dotnet build` OK. Chạy bản build ở cổng 5099: sai mật khẩu, CCCD sai, phương thức xác thực sai, CSV sai, người dùng không tồn tại đều trả thông báo có dấu; tên admin và thiết bị hiển thị có dấu. Không có test nào so khớp nội dung thông báo (đã grep `Server.Tests`). **Chưa chạy `dotnet test`.**
- **Cần khởi động lại backend 5056.**

## 13. Trạng thái mạng ở màn hành khách (22/11, chiều)

- `Client/App.tsx` dùng `useKetNoiMayChu()`: khi đã đăng nhập, nếu ping máy chủ thất bại thì chuyển `cheDoKetNoi` sang `Offline`; có mạng lại thì tự về `Online` và gọi `layThongTinCaNhan` để làm mới thông tin người dùng. Prop `isOffline` truyền cho `SoatVeScreen` cũng tự cập nhật theo.
- Sửa lỗi có sẵn trong `khoiPhucPhienDangNhap` (`services/xacThucService.ts`): trước đây merge cả object `{ thongTin, nhomUuTien }` vào user nên tên/CCCD lưu trong máy không bao giờ được cập nhật (lời chào vẫn "Nguyen Phuoc Thinh"). Nay merge `res.data.thongTin`.
- Nguyên nhân ảnh chụp "Ngoại tuyến" khi có mạng: backend 5056 đang tắt (không phải do code).
- `npx tsc --noEmit` OK. Chưa kiểm tra trên máy.
