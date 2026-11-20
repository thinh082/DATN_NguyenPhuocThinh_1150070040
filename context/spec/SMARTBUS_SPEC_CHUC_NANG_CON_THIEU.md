# Đặc tả bàn giao: hoàn thiện các chức năng còn thiếu của SmartBus

**Ngày lập:** 15/11/2026  
**Đối tượng:** AI/developer tiếp tục triển khai trên workspace local.  
**Trạng thái:** Đặc tả triển khai được soạn từ yêu cầu của người dùng, đề cương và mã nguồn tại thời điểm lập; không phải báo cáo rằng các chức năng dưới đây đã hoàn thành.  
**Mục tiêu:** Hoàn thiện các chức năng còn thiếu để trình diễn và kiểm chứng hệ thống vé xe buýt QR động, chữ ký số và soát vé ngoại tuyến trong phạm vi đồ án.

## 1. Điểm bắt đầu bắt buộc

- Workspace: `D:\mon hoc\DoAnTN\Thinh`; mã nguồn: `Project\`; đề cương: `context\de_cuong_datn_context.md`.
- Các thành phần: `Project\Server` (.NET 8, EF Core, SQL Server), `Project\Client` (Expo 58, React Native, TypeScript), `Project\Admin` (React, TypeScript, Vite), `Project\Server.Tests` (xUnit).
- Đọc thêm `Project\Client\AGENTS.md`, `Project\docs\soat-ve-offline.md`, `Project\Admin\README.md`, `Project\Server\Database\01_khoi_tao_SmartBusDb.sql`, `02_goi_soat_ve.sql` và mã liên quan trước khi sửa.
- `Project\docs\superpowers\specs\2026-11-13-smartbus-demo-design.md` và `Project\docs\superpowers\plans\2026-11-15-smartbus-completion.md` là tài liệu lịch sử/dự thảo. Nếu khác nhau, ưu tiên yêu cầu người dùng, đề cương, và đặc tả này; không coi mọi mục trong tài liệu lịch sử là đã được thực hiện.
- Checkout hiện có nhiều thay đổi **chưa commit**, gồm `Admin/`, test và phần lớn code offline dưới dạng untracked. Phải đọc `git status`, tiếp tục trên nội dung hiện tại; không reset, clean, xóa, checkout đè, cherry-pick hoặc tạo worktree từ HEAD rồi bỏ sót các thay đổi này. Không commit/push nếu không phù hợp quy trình của người dùng.
- Người dùng đã cho phép đọc/kiểm thử/thao tác database **local** của dự án. Kiểm thử có ghi dữ liệu phải dùng database test riêng hoặc transaction rollback và dọn đúng bản ghi test; không drop, reset hoặc xóa hàng loạt `SmartBusDb`. Không chạm database ngoài local chỉ dựa vào quyền này.
- Cấu hình nhạy cảm nằm trong .NET user-secrets/environment. Không in hoặc commit token, password, private key, TOTP secret, connection string có mật khẩu, CCCD/SĐT thật. Không thay khóa đang dùng hoặc dữ liệu vé hiện có mà không có đường nâng cấp tương thích được kiểm chứng.

## 2. Phạm vi, ưu tiên và ranh giới

### 2.1. Đã có nền để mở rộng

- Hành khách có đăng ký, đăng nhập, sửa hồ sơ, nộp URL minh chứng, xem trạng thái ưu tiên; tìm tuyến, xem trạm và lịch sử thực; mua vé/mock thành công và tạo QR TOTP sau khi đã tải ví.
- Nhân viên có màn hình chọn thiết bị/chuyến, tải gói, quét QR bằng camera, xác thực cục bộ, âm thanh, lưu SQLite/SQLCipher và gửi từng sự kiện khi có mạng.
- Admin có React dashboard, danh sách và chỉnh sửa tuyến/trạm/xe, tạo/đổi trạng thái chuyến, xem/sửa giá, quản lý tài khoản, duyệt hồ sơ, xem/khóa vé, xem đơn/lượt quét/cảnh báo.
- Backend có JWT/role, ECDSA P-256, AES-GCM bảo vệ secret, TOTP HMAC-SHA256, kiểm tra lại sự kiện offline, snapshot gói, idempotency theo `maSuKien` và xử lý xung đột. Hai thiết bị offline không thể phát hiện quét trùng của nhau ngay; server đối soát sau đồng bộ.
- Những câu trên chỉ mô tả code hiện có; không thay thế kiểm thử trên thiết bị, không tuyên bố chức năng chạy hoàn chỉnh.

### 2.2. Công việc trong đặc tả

1. **P0:** sửa luồng mua vé/mock và mua nhanh bằng QR cố định; đảm bảo giao dịch và idempotency.
2. **P0:** tra cứu khẩn cấp, khóa vé và ghi nhận nghi ngờ gian lận trên app nhân viên.
3. **P0:** hoàn thiện bảo vệ/phục hồi phiên và ví offline, điều kiện trạng thái gói, báo lỗi và đồng bộ an toàn.
4. **P1:** hoàn thiện quản lý lộ trình, trạm theo thứ tự, báo cáo và vé trên Admin.
5. **P1:** cho phép gửi tệp minh chứng ưu tiên, giữ hỗ trợ URL cũ; bảo vệ dữ liệu riêng tư.
6. **P1:** thiết lập CSDL demo, Redis cache và cấu hình chạy lại được.
7. **P0/P1:** kiểm thử, nghiệm thu trên thiết bị, đo hiệu năng và tài liệu bàn giao.

### 2.3. Quyết định đã chốt

- **Thanh toán chỉ là mock.** Không tích hợp cổng thật/sandbox ngoài, không cần Webhook/IPN hay ký callback. Giao diện và báo cáo ghi rõ “Thanh toán mô phỏng”; không gọi là thanh toán MoMo thật.
- Giữ một Expo app phân vai trò và Admin React riêng; không viết lại hệ thống, không xóa chức năng đang chạy, không thêm GPS/tìm đường/bản đồ, không làm hệ thống thanh toán thương mại.
- Vé lượt demo dùng một lần, hạn 24 giờ; vé tháng hạn 30 ngày, nhiều lượt nhưng không chấp nhận hai lượt trong cùng bước TOTP của cùng vé. Giữ quy tắc phát hiện trùng đa thiết bị sau đồng bộ.
- QR **đang triển khai** là `SMARTBUS:v1:<maVe-GUID>:<TOTP-8-so>:<chuKy-base64>`. Nội dung vé cố định hiện là JSON do server ký ECDSA, lưu tại `Ve.duLieuDaKy`; chữ ký .NET hiện sử dụng định dạng IEEE P1363. HMAC-SHA256, chu kỳ 30 giây, cho phép bước hiện tại ±1. Không tự đổi sang `SB1|...`/DER như tài liệu dự thảo cũ; nếu thấy cần đổi giao thức phải có yêu cầu riêng, kế hoạch tương thích vé/gói cũ và test đa nền tảng trước.
- Lịch sử lượt đi hiển thị theo thời điểm quét và chỉ thuộc tài khoản từ JWT. Lượt offline chỉ xuất hiện sau khi server ghi nhận. Không xóa lịch sử đã có.
- `NhatKyQuetVe.maSuKien` và `XuLySuKienOffline.id` là UUID do thiết bị sinh; backend hiện xử lý `POST /api/SoatVe/dong-bo-su-kien` từng sự kiện. Không khôi phục API `dong-bo-lo` cũ đã vô hiệu hóa, không tin `ketQuaTaiThietBi` làm căn cứ công nhận.
- ID tuyến/trạm/xe/người dùng là `int`, ID vé công khai/phiên/gói/sự kiện là `Guid`, `Ve.id`/`ChuyenXe.id` là `long`. Không tự đổi kiểu ID.
- Thời gian CSDL lưu UTC; API trả timestamp kèm `Z` hoặc offset rõ ràng. Bộ lọc ngày của Admin dùng múi giờ `Asia/Ho_Chi_Minh`, chuyển sang khoảng UTC **[đầu ngày, đầu ngày sau)**. Tránh biến date-only thành thời điểm lệch ngày.

## 3. Quy tắc chung về bảo mật và API

- Mọi quyền phải được kiểm tra ở server, không chỉ ẩn nút. Không nhận `nguoiDungId`, `nhanVienId`, `nguoiThucHienId` từ client làm danh tính; lấy từ JWT. Hành khách chỉ truy cập dữ liệu của chính mình; soát vé chỉ dùng phiên/thiết bị được phép; quản trị chỉ dành `QuanTriVien`.
- Kiểm tra tài khoản khóa/đổi vai trò với token còn hạn. Kết quả token sai/hết hạn: HTTP 401; sai vai trò: 403; tài nguyên không thuộc người gọi: 404; input sai: 400; xung đột/idempotency khác payload: 409. API mới dùng `KetQuaPhanHoi` khi có body và giữ tương thích client cũ. Rà soát vì nhiều endpoint cũ trả HTTP 200 nhưng body `statusCode` lỗi.
- Endpoint mock chỉ bật Development hoặc môi trường Demo cấu hình rõ. Đơn giá, điều kiện ưu tiên, trạng thái, thời gian, chủ sở hữu và tuyến do server xác nhận; client không quyết định quyền hoặc số tiền.
- Dữ liệu mã QR, OTP và secret không đưa vào log, analytics, ảnh chụp màn hình chẩn đoán, URL query, Admin hay file công khai. Không expose TOTP secret qua endpoint khác ngoài luồng ví của chủ vé và gói offline cho phiên nhân viên được phép.
- Giữ và mở rộng test hiện có; mã mới tách thành service/component vừa đủ, không viết lại `App.tsx` hoặc toàn bộ Admin nếu không cần. Theo `Client/AGENTS.md`, kiểm tra tài liệu Expo đúng SDK 58 trước khi đụng API native, dùng `npx expo install` với package Expo.

## 4. P0 — Mua vé, thanh toán mock và mua nhanh bằng QR xe

### 4.1. Hiện trạng cần sửa

- `Client/App.tsx` chỉ hiển thị biểu giá/nút mua khi ví trống và tự gọi `thanhToanDonHang` như thành công; không có màn hình thất bại/hủy.
- `Server/Services/VeService.cs`: đã có transaction khi tạo đơn/thanh toán nhưng chưa có đầy đủ state machine và idempotency theo yêu cầu; không dựa vào nhãn cổng client gửi. `TaoDonHangDto.maYeuCau` đã tồn tại, phải dùng có kiểm tra ownership và payload.
- Có `GET /api/VanHanh/quet-qr-dan-xe?maQrTinh=...` và `Client/services/vanHanhService.ts`; chưa có màn hình camera và xác nhận mua. QR Admin đang mã hóa `XeBuyt.maQrTinh` dạng GUID.

### 4.2. Hành vi phải có

1. Ví hiển thị danh sách vé và hành động **Mua vé** kể cả khi đã có vé. Có thể mua vé lượt/vé tháng từ biểu giá hợp lệ; hiển thị tuyến, đối tượng, giá, mức giảm và số tiền trước khi xác nhận. Vé miễn phí vẫn ghi nhận đơn và giao dịch 0 đồng.
2. Mua nhanh: hành khách đang đăng nhập mở camera, cấp quyền; chỉ chấp nhận QR là GUID hợp lệ có độ dài hợp lý; gửi mã tới server qua HTTPS/API đã xác thực; server kiểm tra xe hoạt động và có đúng một chuyến `DangChay`. Nếu không có chuyến hoặc có xung đột dữ liệu, không đoán tuyến, báo lỗi tiếng Việt và cho quét lại. Xác nhận tuyến/chuyến và chọn giá phù hợp; dùng chung luồng tạo đơn/mua mock, không tạo đường phát hành riêng.
3. Client sinh `maYeuCau` GUID **một lần cho một thao tác**, giữ lại khi retry. `POST /api/Ve/tao-don-hang` dùng chủ sở hữu từ JWT và `maYeuCau`; retry cùng người dùng và cùng nội dung trả đơn cũ; cùng ID khác nội dung trả 409; không tạo hai đơn. Kiểm tra giá đang hoạt động, ngày hiệu lực, tuyến của chuyến mua nhanh và đối tượng ưu tiên đã duyệt **đang hiệu lực**. Số lượng trong demo: 1–10; số âm/0/quá giới hạn từ chối.
4. Đơn trả `donHangId`, `maDonHang`, `trangThai`, `phaiThanhToan`. Với đơn >0, hiển thị màn **Thanh toán mô phỏng** với ba hành động **Thành công**, **Thất bại**, **Hủy**. Chỉ server phát hành khi chọn thành công. Đơn hết hạn/hủy không phát hành; nếu thất bại cho phép tạo giao dịch/đơn mới theo state machine được tài liệu hóa.
5. Đề xuất hợp đồng `POST /api/Ve/thanh-toan-gia-lap` body `{ donHangId, requestId: Guid, ketQua: "ThanhCong" | "ThatBai" | "Huy" }`. Chủ đơn lấy từ JWT. Trả trạng thái đơn và vé/đơn liên quan; gọi lại cùng request trả cùng kết quả, request trùng khác nội dung 409. Có thể giữ endpoint cũ để tương thích nhưng không để đường cũ bỏ qua kiểm tra hay phát vé hai lần.
6. Tạo đơn/chi tiết; thanh toán/trạng thái/phát hành; hoặc nhánh miễn phí/giao dịch 0 đồng/phát hành là các transaction nguyên tử. Dùng unique index và transaction phù hợp SQL Server, không chỉ kiểm tra `Any` rồi insert. Thử gọi đồng thời. Nếu mạng mất sau server commit, client tra trạng thái bằng ID và không tuyên bố thất bại/thành công khi chưa biết.
7. Không tin mã ưu tiên hoặc giá do client truyền: kiểm tra `HoSoUuTien.trangThai == DaDuyet`, `hieuLucTu <= ngayHienTai <= hieuLucDen`. Việc mua nhanh không thay đổi quy tắc này.

### 4.3. Nghiệm thu và test

- Test API: QR sai/xe không hoạt động/không có chuyến, giá sai tuyến, thiếu quyền, hồ sơ hết hạn/chưa hiệu lực, số lượng sai. QR hợp lệ trả đúng tuyến/chuyến.
- Test SQL: hai lần tạo cùng request → một đơn; cùng ID khác payload → 409; hai lần thanh toán đồng thời/retry → tối đa một giao dịch thành công và một bộ vé; không phát hành khi fail/hủy; rollback khi ký lỗi; miễn phí có giao dịch 0 đồng.
- Trên mobile: mua vé khi ví đã có vé, chọn ba kết quả mock, mô phỏng rớt mạng trước/sau phản hồi, QR cố định thật đọc được bằng camera. Vé mới hiển thị sau khi tải lại ví.

## 5. P0 — Tra cứu khẩn cấp và khóa vé của nhân viên

### 5.1. Hiện trạng và vấn đề

`SoatVeController.traCuuKhancCap`/`SoatVeService.traCuuKhancCap` đã có, nhưng hiện truy vấn rồi **tự ghi lượt ngay**, chưa cho xác nhận, chưa kiểm tra đầy đủ quyền sở hữu phiên/thiết bị, tuyến, hạn, trùng và chưa đảm bảo transaction toàn vẹn. `VeController.khoaVe` đã có và lấy người thao tác từ JWT; app nhân viên chưa có màn hình tương ứng.

### 5.2. Hành vi phải có

1. Thêm màn nhân viên **Tra cứu khẩn cấp**, chỉ hoạt động online. Nhập một CCCD 12 chữ số hoặc một SĐT hợp lệ; chỉ tra khi đang có phiên soát vé thuộc chính nhân viên và thiết bị đó. Không hiển thị/caching danh sách tài khoản rộng; log không chứa đầy đủ số nhập.
2. Tách rõ hai bước: `POST /api/SoatVe/tra-cuu-khan-cap` chỉ **tìm**, không tạo lượt; trả thông tin đối chiếu tối thiểu (tên, một phần số định danh đã che, danh sách vé hợp lệ/đúng tuyến hoặc lý do không có vé). Không trả bí mật TOTP, chữ ký đầy đủ hoặc thông tin cá nhân thừa.
3. Người soát đối chiếu trực tiếp hành khách và chọn **Xác nhận lượt**. Đề xuất `POST /api/SoatVe/xac-nhan-thu-cong` body `{ phienSoatVeId, thietBiSoatVeId, maVe, requestId }`; server lấy nhân viên từ JWT, xác nhận phiên mở/thiết bị/xe/chuyến/tuyến, quyền vé, hạn và chống trùng. Ghi nhật ký `TraCuuThuCong` + `LuotDi` trong transaction, ghi `requestId` duy nhất để retry không tạo lượt mới.
4. Vé lượt chỉ có một lượt hợp lệ dù trước đó quét QR hoặc tra cứu thủ công. Vé tháng: khóa trùng theo **bước 30 giây tại thời điểm xác nhận**. Phải phối hợp cùng lock/index/transaction của luồng QR online và đồng bộ offline; phát hiện trùng tạo cảnh báo và không ghi lượt hợp lệ thứ hai.
5. Thêm nút **Khóa vé nghi ngờ** trong màn chi tiết kết quả: yêu cầu lý do; mặc định `TamKhoa`, không khóa tài khoản tùy tiện; chỉ admin mới thu hồi vĩnh viễn/mở khóa theo mục 7. Ghi lịch sử `NhatKyThuHoiVe` với người thao tác lấy từ JWT; thay đổi trạng thái và audit cùng transaction. Không xóa các lượt trước đó.
6. UI hiển thị kết quả rõ ràng, âm thanh hợp lệ/từ chối; hủy thao tác không tạo lượt. Không hỗ trợ tra cứu cá nhân offline trong phạm vi demo.

### 5.3. Nghiệm thu

- Tìm thành công không làm tăng `LuotDi`; xác nhận thành công tăng đúng một; hủy không tăng.
- Sai vai trò, sai nhân viên/phiên đã đóng/thiết bị không khớp/sai tuyến/vé khóa/hết hạn bị từ chối; input sai và không có mạng báo rõ.
- QR trước rồi tra cứu, tra cứu trước rồi QR, hai yêu cầu đồng thời và retry cùng request đều không tạo hai lượt. Lượt bị từ chối không làm thay đổi trạng thái vé trừ khi nhân viên chủ động khóa và có lý do.

## 6. P0 — Phiên, ví vé và vận hành ngoại tuyến

### 6.1. Phiên và cache

- `Client/services/api.ts` lưu token trong SecureStore nhưng biến token/nguoiDung chỉ ở bộ nhớ; khi app bị đóng, mở lại offline không khôi phục được phiên. Phân biệt **khôi phục để xem dữ liệu đã cache** với việc khẳng định token/role đang còn hợp lệ ở server.
- Dùng SecureStore cho token và metadata danh tính tối thiểu; không lưu mật khẩu. Sau đăng nhập online thành công mới lưu. Khi online khởi động, xác nhận token qua endpoint hiện có, lấy lại vai trò/trạng thái; 401/403 xóa phiên và cache nhạy cảm theo chính sách; không fallback offline khi server đã từ chối.
- Khi **không có mạng**, chỉ khôi phục phiên đã lưu trong thời hạn tối đa **24 giờ từ lần xác nhận online gần nhất**, đồng thời JWT chưa hết hạn theo `exp`, để mở vé/gói đã cache; hiển thị rõ “Ngoại tuyến · trạng thái có thể chưa cập nhật”. Không cho đăng ký, mua vé, tra cứu CCCD, quản trị hay tải gói mới offline. Nếu không chứng minh được hạn phiên/đồng hồ hợp lý, yêu cầu online. Đây là lựa chọn demo, ghi rõ không thể nhận khóa tài khoản ngay khi thiết bị mất mạng.
- Logout phải xóa token và dữ liệu vé/secret của đúng tài khoản trong SecureStore; dữ liệu nhật ký soát vé **chưa đồng bộ** phải được giữ an toàn theo userId và không hiển thị cho người khác. Không xóa hàng đợi để đạt chức năng logout. Khi login tài khoản khác, không dùng gói/vé/queue của tài khoản trước.
- Tách lỗi `mất mạng` khỏi 401/403 và lỗi server. Nếu không lấy được trạng thái vé hiện tại online, không thay ví bằng dữ liệu sai. Gói offline quá hạn, thiết bị chưa có vé, hoặc clock lệch vượt cửa sổ đều từ chối có lý do; không tự gia hạn gói theo đồng hồ client.

### 6.2. Màn vé và scanner

- Chỉ hiển thị QR của vé có trạng thái hoạt động, còn hiệu lực tại thời điểm hiện tại và được cache. Mã QR phải tự thay đổi theo thời gian thực mỗi 30 giây, kể cả app chuyển nền rồi trở lại. Vé được server khóa sau lần cache chỉ có thể được cập nhật khi online; hiển thị giới hạn này.
- Dùng `expo-screen-capture` đúng SDK để ngăn chụp trên nền tảng hỗ trợ; không hiển thị “đã bật” nếu API lỗi/không hỗ trợ. Khi vào màn vé lưu độ sáng ban đầu, tăng sáng; khi rời màn hình, đăng xuất, unmount hoặc lỗi phải khôi phục. Không xin quyền/đổi độ sáng hệ thống ngoài phạm vi cần thiết.
- SQLCipher phải được kiểm tra bằng `PRAGMA cipher_version` trước khi ghi và dùng key trong SecureStore. Expo Go không có SQLCipher: báo cần development build, không âm thầm ghi SQLite plaintext. Xử lý lỗi đầy bộ nhớ, key bị mất, app dừng giữa transaction; chỉ phát âm thanh “hợp lệ” sau khi log đã commit.
- Giới hạn TOTP secret trên scanner: gói gắn với phiên/nhân viên/thiết bị, chỉ vé phù hợp tuyến, hết hạn 24 giờ; secret được bảo vệ at rest và khi truyền. Khóa/thu hồi thiết bị và vòng đời gói cần được xác minh với dữ liệu thực, không khẳng định loại bỏ rủi ro khi thiết bị bị chiếm quyền.

### 6.3. Đồng bộ và kiểm tra lại

- Giữ event UUID, packageId, payload QR gốc, thời điểm UTC, kết quả cục bộ; kiểm tra checksum cùng ID. Chỉ đánh dấu đã gửi sau server xác nhận **đúng event**; timeout sau commit retry trả kết quả cũ. Một event lỗi 400/409 không được xóa hoặc ngăn gửi event khác; hiển thị danh sách sự kiện chưa xử lý và mã lỗi, hỗ trợ thử lại. Lượt cũ không có packageId được giữ để đối soát thủ công, không tự gán gói mới.
- Server kiểm tra gói thuộc nhân viên/phiên, thời điểm quét trong hạn gói, không vượt giờ server quá 5 phút, QR đúng định dạng/chữ ký/TOTP/tuyến/hạn tại thời điểm quét, và vé nằm trong snapshot hoạt động. `ChapNhanTaiThietBi=false` không trở thành lượt hợp lệ. Vé bị khóa sau snapshot: giữ theo chính sách hiện tại và tạo `STALE_REVOCATION`; quét trùng tạo cảnh báo và không tạo lượt thứ hai.
- Hai thiết bị đồng bộ cùng vé: transaction/unique index/khóa SQL bảo đảm tối đa một lượt hợp lệ; ứng dụng nhận kết quả từng event, không dựa vào thứ tự timestamp client để chọn lại lượt đã công nhận. Tránh deadlock; nếu lock timeout/transient, để event pending và retry có backoff. Kiểm thử cả QR online và offline đồng thời vì hai đường cùng chạm lượt đi.
- **Cần rà soát hiện trạng trước khi sửa:** `SoatVeService.xacThucVe` có luồng online riêng, hiện không có cùng tập kiểm tra tuyến/timeStep như `OfflineQrValidationService`; các đường phải dùng chung quy tắc hoặc có test chứng minh tương đương. Endpoint tra cứu khẩn cấp cũ không được lách cơ chế chống trùng.

### 6.4. Nghiệm thu

- Kiểm thử thiết bị thật: login online → tải vé/gói → tắt Internet hoàn toàn → đóng/mở app trong giới hạn phiên → mở vé đã cache/quét/lưu → bật Internet → đồng bộ → retry cùng event không nhân đôi; có trường hợp vé chưa nằm trong gói và gói quá hạn.
- Test unit/TOTP vectors .NET–TypeScript; sai chữ ký, sửa payload, sai tuyến, quá hạn, gói cũ, mã ±1 và ngoài cửa sổ, đồng hồ sai, vé bị khóa, quét trùng, mất mạng sau server commit, account đổi/khóa, app bị kill trước/sau ghi SQLite.
- Lượt chưa đồng bộ phải còn sau logout/restart và không xuất hiện cho tài khoản khác. Kiểm tra SQLCipher thật trên development build, không suy luận từ config plugin.

## 7. P1 — Hoàn thiện Web Admin

### 7.1. Lộ trình, trạm và chuyến

- `Admin/src/main.tsx`/`Server/Controllers/AdminController.cs` đã có tuyến, trạm, xe và chuyến; chưa có tạo/chỉnh sửa lộ trình, thứ tự trạm hoặc lịch trình đầy đủ.
- Admin tạo lộ trình gắn tuyến, chiều `Di`/`Ve`, tên, khoảng hiệu lực và phiên bản; chọn trạm hoạt động, sắp xếp thứ tự liên tục 1..N, không lặp trạm trong cùng một chiều/phiên bản, tối thiểu 2 trạm. Địa chỉ/thứ tự thay đổi sau khi lưu phải hiển thị đúng ở màn tra cứu hành khách.
- Không sửa/xóa trực tiếp lộ trình đã được chuyến sử dụng làm thay đổi lịch sử; tạo phiên bản mới và thời điểm hiệu lực. Không cho hai phiên bản cùng tuyến/chiều có khoảng hiệu lực chồng lấn. Ghi transaction khi lưu danh sách trạm, rollback toàn bộ nếu invalid.
- Admin tạo/đổi trạng thái chuyến đã có; bổ sung sửa chuyến dự kiến và hủy chuyến dự kiến, kiểm tra xe/truyến hoạt động, lộ trình có hiệu lực và thời gian kết thúc sau bắt đầu. Chặn xe bị xếp hai chuyến chồng thời gian, kể cả hai request đồng thời; không kết thúc/hủy phiên đang có tác vụ chưa được xử lý mà không thông báo rõ.
- QR cố định cho xe phải hiển thị đúng `maQrTinh` dạng GUID và có nút tải/in; quét trong mục 4 nhận đúng xe. Không coi QR xe là vé hoặc quyền đi xe.

### 7.2. Báo cáo và đối soát

- Dashboard cho chọn khoảng ngày/tuyến/nhóm đối tượng, mặc định 30 ngày; hiển thị lượt hợp lệ, vé phát hành, đơn và doanh thu **mock**, số tiền miễn giảm/trợ giá, số cảnh báo mới và hồ sơ chờ duyệt. Phân biệt ngày quét với ngày nhận đồng bộ; chỉ tính theo định nghĩa ghi rõ trên UI/báo cáo, không đếm sự kiện bị từ chối thành lượt.
- Hỗ trợ thống kê theo ngày/tháng và tuyến; bảng có trạng thái rỗng, lỗi, tải lại, phân trang server-side và tìm kiếm. Không giới hạn 200 bản rồi nói đó là toàn bộ. Tổng hợp lọc khớp danh sách. Cho xuất CSV có header UTF-8, escape dấu phẩy/newline và ngăn CSV formula injection ở các ô văn bản bắt đầu bằng `=`, `+`, `-`, `@`.
- Trang chi tiết cảnh báo cho thấy event/phiên/thiết bị/thời điểm, kết quả server và lý do; admin đặt `DangXuLy`, `DaXuLy` hoặc `BoQua`, nhập kết luận khi đóng. Truy vết người/lúc xử lý; không hiển thị secret/QR đầy đủ/CCCD không cần thiết.
- Vé: cho `TamKhoa → HoatDong` với lý do và nhật ký; không mở vé `ThuHoi` vĩnh viễn hay vé đã hết hạn như còn hợp lệ. Cập nhật trạng thái và audit trong transaction; gói offline chỉ nhận trạng thái mới sau tải gói. Tài khoản: khóa/mở theo quyền, ngăn tự khóa tài khoản admin đang dùng và ghi vết.
- Admin không sửa/xóa âm thầm giao dịch/lượt đã công nhận để làm đẹp số liệu. Nếu hỗ trợ hủy lượt, ghi lý do/người/thời gian và lịch sử; báo cáo nhất quán với trạng thái.

### 7.3. Nghiệm thu

- Test lộ trình: thứ tự trạm, trùng trạm, thời gian hiệu lực chồng lấn, lịch sử không bị sửa, rollback khi một trạm không hợp lệ.
- Test chuyến: xe không tồn tại/vô hiệu, giờ đảo ngược, hai request trùng lịch. Test dashboard với giao dịch mock thành công/thất bại/0 đồng, sự kiện quét từ chối, lệch múi giờ và ranh giới nửa đêm/tháng.
- Test admin/nhân viên/hành khách truy cập và thay đổi trạng thái; vé khóa/mở đúng điều kiện, audit có đủ thông tin. Kiểm tra giao diện desktop/mobile, phân trang, CSV, thông báo loading/error/empty.

## 8. P1 — Tải tệp minh chứng ưu tiên

- Hiện `HoSoHanhKhach.tsx` và `HoSoUuTien.duongDanMinhChung` nhận URL HTTP(S). Bổ sung chọn ảnh/PDF và tải trực tiếp **ngoài** lựa chọn URL, không phá hồ sơ URL cũ. Đây là lưu trữ demo local, không sử dụng dịch vụ cloud mà không có yêu cầu mới.
- Chỉ hành khách đã xác thực upload; cho phép JPEG/PNG/PDF, tối đa 5 MiB/tệp, kiểm tra extension, MIME và chữ ký file, tên lưu ngẫu nhiên server-side. Giới hạn số file và tốc độ phù hợp; không tin tên đường dẫn từ client. Nếu dùng Expo DocumentPicker/ImagePicker, xem tài liệu Expo SDK 58; xin quyền có giải thích và xử lý người dùng hủy.
- Lưu file ở thư mục cấu hình **ngoài web root**, không expose static public. Tạo bảng metadata tham chiếu người sở hữu, tên ngẫu nhiên, loại, size và thời gian; `duongDanMinhChung` cũ vẫn đọc được. Chỉ chủ hồ sơ hoặc admin được tải/xem qua endpoint có auth; admin web mở file bằng request có token, không dùng URL bearer công khai. Không log nội dung tệp/token.
- Tạo hồ sơ phải tham chiếu file do chính người dùng upload hoặc URL hợp lệ, không thể gắn file của người khác. Xử lý tệp upload nhưng hủy form/rollback bằng chính sách cleanup có giới hạn, không xóa tệp gắn với hồ sơ đã lưu. Từ chối file giả MIME/quá kích thước/path traversal.
- Nghiệm thu: upload/xem và gửi hồ sơ qua app; admin xem minh chứng sau đăng nhập; người thứ ba/ẩn danh bị chặn; URL cũ vẫn hiển thị; test tệp hỏng, sai loại, quá cỡ, lỗi mạng và cleanup.

## 9. P1 — Dựng CSDL demo, cache và cấu hình

- Thiết lập schema có phiên bản bằng EF Core migrations hoặc chiến lược SQL migrations có bảng lịch sử/kiểm tra thứ tự rõ ràng; chọn **một nguồn chính** và mô tả cách chuyển từ `01`/`02` hiện tại. Không chạy `EnsureCreated` để giả định đã migrate. Tạo DB test/demo riêng, không tự drop database hiện có; trước khi áp dụng schema lên `SmartBusDb` phải có bản sao và đường rollback được kiểm tra.
- Seed idempotent, theo khóa nghiệp vụ thay vì giả định identity 1/2/3: danh mục đối tượng/loại vé, hai tuyến, trạm và thứ tự, xe/QR, chuyến hoạt động, giá, thiết bị và ba vai trò. Dữ liệu test là giả; script không đưa password/private key thật vào repo. Tài khoản demo, cổng chạy và cách tạo secrets ghi trong README.
- Tạo script thiết lập user-secrets cho JWT, AES 32 byte và cặp ECDSA P-256 ổn định; chạy lần hai không thay khóa đang sử dụng. `tools/SmartBus.KeySetup` cần được rà soát và tài liệu hóa. Public key tương ứng phải được đăng ký an toàn; nếu xoay khóa phải duy trì xác minh vé cũ hoặc nêu rõ cách cấp lại.
- Redis hiện chỉ đăng ký connection, chưa có use case. Bổ sung cache **chỉ** cho danh sách tuyến và biểu giá với TTL 5 phút; invalidate sau sửa tuyến/giá và khi thay đổi trạm ảnh hưởng response. Redis không khả dụng → đọc SQL, không chặn mua/soát. Không dựa Redis làm nguồn duy nhất xác định vé đã dùng. Test cache hit, invalidation và fallback; nếu không có Redis trong demo, ghi rõ kết quả và lý do.
- Loại URL ngrok cố định khỏi `Client/services/api.ts`: cấu hình `EXPO_PUBLIC_API_BASE_URL`, có file ví dụ và chỉ dẫn Android emulator/điện thoại cùng LAN/tunnel HTTPS. Không commit URL tạm có tính cá nhân; báo lỗi khi thiếu cấu hình. Không làm lộ secret qua biến `EXPO_PUBLIC_*`. Admin dùng Vite proxy khi phát triển và reverse proxy `/api` khi triển khai.

## 10. Ma trận API đề xuất

Tên cụ thể có thể điều chỉnh theo quy ước hiện tại **nếu cập nhật đồng thời client, hợp đồng và test**, không tạo hai đường nghiệp vụ có quy tắc khác nhau.

| Endpoint/nhóm | Vai trò | Hành vi bắt buộc |
|---|---|---|
| `GET /api/VanHanh/quet-qr-dan-xe` | Hành khách đã đăng nhập cho luồng mua | Xác định xe/chuyến/tuyến; không phát vé. |
| `POST /api/Ve/tao-don-hang` | Hành khách | Kiểm tra chủ/giá/đối tượng; idempotent theo `maYeuCau`. |
| `POST /api/Ve/thanh-toan-gia-lap` | Chủ đơn, Demo/Development | Kết quả mock, giao dịch và phát hành nguyên tử. |
| `POST /api/SoatVe/tra-cuu-khan-cap` | Nhân viên, phiên của mình | Tìm, không ghi lượt. |
| `POST /api/SoatVe/xac-nhan-thu-cong` | Nhân viên, phiên của mình | Xác nhận có idempotency, chống trùng, audit. |
| `POST /api/Ve/khoa-ve`, endpoint mở khóa mới | Nhân viên/admin theo chính sách | Lý do, actor từ JWT, audit và transaction. |
| `POST /api/SoatVe/dong-bo-su-kien` | Chủ phiên/gói | Kiểm tra QR lại, ACK từng sự kiện idempotent. |
| `api/Admin/*` | Quản trị | Lộ trình/trạm, lịch trình, dữ liệu báo cáo và xử lý cảnh báo. |
| Upload/download minh chứng mới | Chủ hồ sơ hoặc admin theo thao tác | File riêng tư, kiểm tra quyền và loại/kích thước. |

Các lỗi nghiệp vụ phải rõ cho UI; không trả stack trace hoặc secret ra client. Các endpoint cũ đang trả HTTP 200 kèm `statusCode` trong body cần xử lý tương thích trong giai đoạn chuyển đổi; không làm app cũ coi lỗi là thành công.

## 11. Kiểm thử, nghiệm thu và bằng chứng

### 11.1. Kiểm thử tự động tối thiểu

- Chạy `dotnet test Server.Tests` từ `Project`; test SQL phải chỉ rõ connection test, tạo dữ liệu riêng và dọn đúng ID của test. Không mặc định chạy destructive test trên DB người dùng.
- Chạy `npx tsc --noEmit` và test TypeScript/JavaScript ở `Client`; bổ sung script test được tái chạy. Tối thiểu giữ `node --test tests/soatVeOffline.test.mjs` và thêm vectors/edge cases cho phần mới.
- Chạy `npm run build` ở `Admin`; bổ sung kiểm thử chức năng quan trọng ở mức phù hợp, gồm quyền, trạng thái rỗng, lỗi API và form gửi lặp. Khi có thay đổi giao diện, kiểm tra desktop/mobile bằng công cụ browser được môi trường hỗ trợ.
- Test auth: anonymous 401, sai role 403, thay userId không đọc được người khác, token của tài khoản bị khóa hoặc role cũ không dùng được; request mua/tra cứu/sửa đều thuộc đúng chủ.
- Test an toàn dữ liệu: JWT/private key không vào repo/log/UI; không in QR/TOTP; file minh chứng không truy cập được khi không có quyền; dữ liệu của user A không xuất hiện sau login user B.

### 11.2. Kiểm thử thủ công trên thiết bị

1. Admin đăng nhập, chuẩn bị tuyến–lộ trình–xe–chuyến và tài khoản, duyệt một hồ sơ ưu tiên.
2. Hành khách đăng ký/đăng nhập, mua vé lượt và vé tháng qua mock, thử thất bại/hủy, mua lại khi ví đã có vé; quét QR cố định trên xe mua nhanh.
3. Tải ví/gói khi online, tắt hoàn toàn Internet, quét QR hợp lệ/giả/hết hạn/sai tuyến/trùng; kiểm tra camera, âm thanh, FLAG_SECURE, độ sáng và lưu bền sau kill/restart trong giới hạn phiên.
4. Dùng hai thiết bị cùng offline nếu có, quét cùng vé; kết nối lại, sync và retry; chỉ một lượt được công nhận, cảnh báo được hiển thị trên Admin. Thử vé bị khóa sau khi tải gói.
5. Tra cứu khẩn cấp online, xác nhận/không xác nhận và thử trùng; xem lịch sử hành khách và báo cáo Admin. Kiểm tra dữ liệu không bị nhân đôi khi mất mạng sau commit.
6. Đo ít nhất 100 lần xác minh cục bộ trên thiết bị demo: ghi model, hệ điều hành, build, p50/p95/max từ **sau khi parse QR tới khi ghi kết quả cục bộ**, và ghi riêng thời gian camera decode. Mục tiêu p95 ≤500 ms là mục tiêu kiểm thử, chỉ báo đạt nếu số đo thực tế đạt; ghi điều kiện đo.

### 11.3. Định nghĩa hoàn thành

- Các acceptance criteria mục 4–10 đạt; build/test pass với output ghi lại; không có regression của luồng hiện tại.
- Có README từng thành phần, cách cấu hình secrets/DB/Redis/URL, seed, tài khoản demo giả, development build SQLCipher, kịch bản demo và kết quả/giới hạn thực tế.
- Nếu thiếu điện thoại/Android SDK hoặc không thể chạy phép đo, hoàn thành mọi phần có thể kiểm chứng và ghi rõ **chưa kiểm thử thiết bị/chưa đo**, không báo hoàn thành toàn bộ hoặc tự tạo số liệu.
- Đề cương/báo cáo phải mô tả thanh toán mock, secret TOTP trên thiết bị có kiểm soát, thời gian thu hồi trễ và rủi ro hai thiết bị offline. Không tuyên bố an toàn tuyệt đối hoặc loại bỏ gian lận triệt để.

## 12. Thứ tự khuyến nghị và bàn giao cho AI

1. Xác minh baseline build/test, schema thực tế, trạng thái Git và dữ liệu demo; không làm mất thay đổi chưa commit.
2. P0 giao dịch/idempotency và mock → mua nhanh QR → test SQL và mobile.
3. P0 tách tra cứu/xác nhận, quyền phiên, khóa vé/audit → test QR/tra cứu/đồng bộ cạnh tranh.
4. P0 phục hồi phiên/cache, bảo vệ màn hình, đồng bộ và test offline.
5. P1 Admin lộ trình/báo cáo/vé → tải minh chứng → migration/Redis/cấu hình.
6. Kiểm thử tự động, thiết bị thật, đo hiệu năng, tài liệu và review toàn bộ. Mỗi phần cần có kết quả kiểm chứng trước khi tích hợp tiếp.

Để bắt đầu, AI tiếp nhận nên đọc file này cùng mã và test hiện hành, lập checklist phụ thuộc theo kết quả kiểm tra, rồi triển khai tuần tự. Đây là một đặc tả bàn giao; không cần hỏi lại người dùng về thanh toán thật, chia app hay quyền test DB local đã được xác nhận.
