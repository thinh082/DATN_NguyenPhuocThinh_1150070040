# SmartBus Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Chưa bắt đầu triển khai trong lần lập kế hoạch này.

**Goal:** Bổ sung các chức năng còn thiếu theo đề cương, hoàn thành demo từ mua vé bằng thanh toán mock đến xác thực offline và đối soát trên Web Admin.

**Architecture:** Giữ ASP.NET Core API và SQL Server hiện có; dùng chung Expo app, phân màn hình hành khách/nhân viên theo vai trò. Thêm React Admin riêng. Thiết bị soát vé xác minh tại chỗ với gói dữ liệu đã tải; Backend kiểm tra lại sự kiện khi đồng bộ.

**Tech Stack:** .NET 8, EF Core, SQL Server, Expo 58, React Native, TypeScript, React/Vite, xUnit, Vitest; phiên bản package phải được xác minh khi thực hiện.

**Spec:** `context/de_cuong_datn_context.md` ở thư mục cha của Project và `docs/superpowers/specs/2026-11-13-smartbus-demo-design.md`. Các điều chỉnh dưới đây thay thế nội dung tương ứng của bản dự thảo cũ, là đề xuất kỹ thuật để review trước khi code.

## Global Constraints

- Người dùng xác nhận: thanh toán chỉ là mock. Không yêu cầu cổng ngoài, IPN hay simulator ký HMAC. Mock có thành công/thất bại/hủy, nhãn rõ ràng, chỉ bật trong Demo/Development.
- Giữ cấu trúc ID thực tế: `maVe` GUID; tuyến/xe/người dùng là int, không đổi tuyến sang GUID như dự thảo cũ.
- Giữ mã loại vé `VeLuot`/`VeThang`; payload mới có phiên bản. Chỉ vé demo mới sử dụng định dạng mới; vé cũ báo không hỗ trợ và có hướng dẫn cấp lại, không âm thầm coi hợp lệ.
- Mobile dùng Expo Router theo `Client/AGENTS.md`; đọc tài liệu đúng SDK 58 trước khi chạm API Expo. SQLCipher/native modules cần development build, phải xác minh bằng bước thử kỹ thuật trước luồng scanner.
- Private key chỉ trên server. ECDSA P-256/SHA-256, AES-GCM-256 với key 32 byte, nonce 12 byte, tag 16 byte. Secrets nằm trong user-secrets/environment.
- TOTP HMAC-SHA256, 8 chữ số, chu kỳ 30 giây, cửa sổ ±1 bước. Mỗi vé có secret riêng ít nhất 20 byte.
- Không xóa CSDL hiện có. Migration chạy trên CSDL demo riêng trước; giữ bản sao dữ liệu và hướng dẫn chuyển đổi.
- Vé lượt dùng một lần, hạn demo 24 giờ; vé tháng 30 ngày, chặn trùng cùng bước TOTP. Đây là quy tắc demo đề xuất, cần ghi rõ trong báo cáo.
- Gói offline hết hạn sau 24 giờ, có phiên bản và dấu thời gian server. Chỉ hỗ trợ vé đã tải; gói hết hạn phải từ chối xác thực offline.
- Hai thiết bị offline có thể cùng chấp nhận vé; đối soát phát hiện xung đột sau. Secret TOTP chia cho thiết bị được ủy quyền có rủi ro khi thiết bị bị chiếm quyền; không tuyên bố an toàn tuyệt đối.
- Redis: bổ sung cache danh sách tuyến/bảng giá và xóa cache khi sửa; không dùng cache làm nguồn quyết định vé đã dùng. Dapper không bắt buộc.

## Review Focus

1. Gửi lại thao tác mua/thanh toán hoặc chạy đồng thời: không phát hành thêm vé; task 3.
2. App bị tắt hoặc mất mạng sau quét: nhật ký còn nguyên và retry không nhân đôi lượt; task 6–7.
3. Tài khoản khóa, token còn hạn, đổi vai trò hoặc truy cập vé người khác: API chặn ngay; task 2.
4. Đồng hồ lệch, app chạy nền, gói cũ và thu hồi sau tải: kết quả có lý do rõ; task 4–7.
5. QR lỗi định dạng/quá dài, sai tuyến, mật mã bị sửa: từ chối có kiểm soát, không crash; task 3 và 6.

## Phạm vi file và hợp đồng chung

- Giữ Controllers/Services/Models/Entities/Data hiện có, tách thêm service mật mã, gói offline và đồng bộ có trách nhiệm riêng.
- Tạo `Server.Tests/` cho unit/API tests; integration tests dùng SQL Server demo để kiểm tra unique index, transaction và cạnh tranh thực tế.
- Tạo `contracts/qr-vectors.json`, `contracts/api.md`: nguồn chung cho format QR, DTO, mã lỗi và vectors .NET/TypeScript.
- Client: `src/app/` chứa routes; `src/auth/`, `src/tickets/`, `src/scanner/`, `src/storage/`, `src/sync/` chứa logic; tái sử dụng `services/` đang có.
- Admin: `src/api/`, `src/auth/`, `src/pages/`; không truy cập trực tiếp CSDL.
- Payload ký: `SB1|maVe|maLoaiVe|tuyenXeId-or--|hieuLucUnix|hetHanUnix|maKhoa|nonceBase64Url`. GUID chữ thường dạng D, số thập phân invariant, thời gian giây UTC. QR thêm `|timeStep|totp|signatureBase64Url`, chữ ký DER. Không đưa thông tin cá nhân vào QR.
- `OfflinePackageDto`: packageVersion GUID, deviceId int, issuedAtUtc, expiresAtUtc, publicKeys[], tickets[], revocations[]. Server lưu snapshot/metadata để kiểm tra lại khi đồng bộ; chỉ cấp theo thiết bị/tuyến đã ủy quyền.
- `ScanEventDto`: maSuKien GUID, deviceId int, phienSoatVeId GUID, sequence long, packageVersion GUID, qrPayload string, scannedAtUtc, resultCode. Backend tự suy ra vé, timeStep và tuyến từ QR/phiên.
- `SyncResultDto`: batchId GUID, events[{maSuKien, status, reasonCode, luotDiId?}]. Chỉ ACK từng sự kiện đã lưu thành công; cùng ID mà nội dung khác trả conflict.
- Giới hạn đề xuất: QR tối đa 4096 ký tự, batch tối đa 100 sự kiện; kiểm tra phiên/thiết bị thuộc nhân viên hiện tại.

## Task 1: CSDL và môi trường demo có thể dựng lại

**Files:** sửa `Server/Data/SmartBusDbContext.cs`, `DuLieuKhoiTao.cs`, `Server/Database/01_khoi_tao_SmartBusDb.sql`; tạo `Server/Migrations/`, `Server.Tests/DatabaseTests.cs`, `scripts/setup-demo.ps1`, `.config/dotnet-tools.json`.

- [ ] Viết test seed hai lần không trùng; lưu được nhật ký thất bại không có veId; eventId/batchId trùng bị ràng buộc đúng.
- [ ] Chạy `dotnet test Server.Tests --filter DatabaseTests`, xác nhận fail trước khi bổ sung.
- [ ] Tạo migration trên DB demo mới, unique eventId và phát hành theo chi tiết đơn/soThuTu; ràng buộc chống replay ở DB, không chỉ AnyAsync. Seed đủ loại vé/đối tượng/giá trước dữ liệu phụ thuộc, tài khoản ba vai trò và chuyến hoạt động.
- [ ] Script tạo secrets, cặp khóa ký bền vững và cấu hình mẫu; seed chỉ public key tương ứng. Không tự tạo key mới mỗi request hoặc mỗi lần khởi động.
- [ ] Chạy migration, test seed và `dotnet build Server`; ghi lệnh dựng lại trong README. Review diff và commit riêng task.

## Task 2: Xác thực, phân quyền và quyền sở hữu

**Files:** sửa `Server/Program.cs`, `Services/XacThucService.cs`, `NguoiDungService.cs`, toàn bộ `Controllers/`; tạo `Server/Security/AccountStatusHandler.cs`, `Server.Tests/AuthTests.cs`.

**Interface:** JWT claims userId/role; controller lấy userId từ token thay vì tin userId gửi lên. Hành khách chỉ xem/mua vé, lịch sử và hồ sơ của mình; nhân viên soát vé/tra cứu; admin quản trị/báo cáo.

- [ ] Test anonymous→401, sai role→403, đổi userId không đọc được vé người khác; đăng ký truyền Admin vẫn tạo HanhKhach; khóa tài khoản khiến token hiện tại bị từ chối.
- [ ] Chạy `dotnet test Server.Tests --filter AuthTests`, xác nhận fail.
- [ ] Bật JWT middleware/Authorize và kiểm tra trạng thái tài khoản mỗi request được bảo vệ; dùng PasswordHasher thay salt cố định, có chính sách chuyển hash demo cũ; bảo vệ cả endpoint nộp/duyệt hồ sơ.
- [ ] Chạy lại AuthTests và build; review/commit.

## Task 3: Mật mã, phát hành vé và thanh toán mock nguyên tử

**Files:** sửa `Services/MatMaService.cs`, `VeService.cs`, `Models/VeDto.cs`, `Controllers/VeController.cs`; tạo `Services/SigningKeyService.cs`, `Models/QrPayload.cs`, `contracts/qr-vectors.json`, `Server.Tests/CryptoTests.cs`, `TicketIssuanceTests.cs`.

**Interface:** `POST /api/ve/mock-thanh-toan` nhận donHangId, requestId GUID, ketQua (ThanhCong/ThatBai/Huy); server tự tính tiền và kiểm tra chủ đơn. Cập nhật `contracts/api.md` theo route prefix thực tế trước khi code.

- [ ] Test vectors ký/xác minh, sai key, sửa payload, AES sửa tag; TOTP biên ±1/±2 và đầu chu kỳ.
- [ ] Test hai request thanh toán thành công cùng đơn chỉ có một bộ vé; fail/hủy không phát vé; đơn 0 đồng ghi giao dịch; lỗi phát hành rollback cả thanh toán; endpoint mock bị tắt ngoài Demo/Development.
- [ ] Chạy tests xác nhận fail, rồi triển khai key bền vững, canonical payload, mock có trạng thái và transaction/idempotency. Không nhận số tiền client làm căn cứ.
- [ ] Test request đồng thời bằng SQL Server, chạy toàn bộ test crypto/phát hành, review/commit.

## Task 4: Mobile xác thực và ví QR offline

**Files:** sửa `Client/index.ts`, `App.tsx`, `app.json`, `package.json`, `services/api.ts`, `services/xacThucService.ts`, `services/veService.ts`; tạo `src/app/_layout.tsx`, routes auth/passenger/staff, `src/auth/session.ts`, `src/tickets/qr.ts`, `src/storage/ticketStore.ts`, `src/tickets/qr.test.ts`.

**Interface:** ví vé chủ tài khoản trả staticPayload/signature và secret qua TLS; secret lưu SecureStore, dữ liệu vé cache theo userId; `createQr(ticket, unixSeconds): string` dùng vectors task 3.

- [ ] Xác minh tài liệu Expo 58 và dependency QR/crypto/router; ghi phiên bản chọn trong README, cài qua `expo install` khi phù hợp.
- [ ] Viết/chạy test fail cho vectors TypeScript, thay mã tại biên 30 giây và resume từ nền; không phụ thuộc countdown để tính timeStep.
- [ ] Thêm Router, đăng ký/đăng nhập, SecureStore token, interceptor bearer và xử lý 401; logout xóa token/cache nhạy cảm, đổi tài khoản không lộ vé cũ.
- [ ] Render QR thật, tạo mã không gọi API theo chu kỳ; hiển thị hết hạn/khóa/offline, mua mock thành công/thất bại/hủy. Tách vùng mua vé để vẫn mua được khi ví đã có vé.
- [ ] Khôi phục độ sáng và trạng thái screen-capture khi đóng vé; kiểm thử vé cache mở khi tắt mạng và mất mạng giữa lúc mua không báo thành công sai.
- [ ] Bổ sung scripts test/lint/typecheck; chạy `npm test`, `npm run lint`, `npm run typecheck` tại Client; kiểm tra QR bằng thiết bị thật, review/commit.

## Task 5: Gói xác thực ngoại tuyến có phiên bản

**Files:** sửa `Server/Models/SoatVeDto.cs`, `Controllers/SoatVeController.cs`; tạo `Services/OfflinePackageService.cs`, `Entities/GoiNgoaiTuyen.cs`, `Server.Tests/OfflinePackageTests.cs`; cập nhật DbContext/migration.

- [ ] Test gói chỉ cấp cho nhân viên/thiết bị được phép; đủ public key và secret vé thuộc phạm vi; version khác nhau, hết hạn 24 giờ; tải không mất bản thu hồi khi client chưa ACK.
- [ ] Chạy test fail, triển khai `CreatePackageAsync(deviceId, userId): Task<OfflinePackageDto>`; lưu snapshot trạng thái/hiệu lực và phiên bản, không log hoặc trả secret cho Admin.
- [ ] Phân biệt ngày phát hành gói và ACK nhận gói, thay việc tăng mốc thu hồi ngay lúc GET như hiện tại.
- [ ] Chạy tests với khóa cũ còn vé hợp lệ và khóa mới; giữ khả năng kiểm tra vé đã phát hành khi xoay khóa. Review/commit.

## Task 6: Scanner offline và lưu nhật ký bền vững

**Files:** tạo `Client/src/scanner/verify.ts`, `verify.test.ts`, `src/storage/scannerDb.ts`, `src/app/staff/scan.tsx`, `prepare.tsx`; sửa `Client/services/soatVeService.ts`, `app.json`; thêm assets âm thanh thực tế.

**Interface:** `verifyQr(payload, package, trip, nowUtc): VerificationResult`; `saveScan(event): Promise<void>` ghi SQLite transaction trước thông báo thành công.

- [ ] Thử development build trên thiết bị: SQLite mã hóa/SQLCipher, khóa SecureStore, camera, ECDSA DER. Chỉ chuyển tiếp khi đọc/ghi và mở lại sau kill app thành công; nếu SQLCipher không hỗ trợ, mã hóa AES-GCM từng bản ghi nhạy cảm với key SecureStore và kiểm tra không có plaintext secret trong DB.
- [ ] Test fail cho sai chữ ký/TOTP/tuyến, hết hạn, gói hết hạn, vé không có trong gói, QR quá dài/sai cấu trúc, vé lượt đã dùng và vé tháng trùng timeStep.
- [ ] Tạo màn chuẩn bị thiết bị/chuyến, tải gói; camera debounce chống callback lặp, chọn đúng phiên, xác minh hoàn toàn tại máy; âm thanh chỉ sau transaction thành công.
- [ ] Kiểm tra tắt app mở lại vẫn chặn vé đã quét và còn hàng đợi; lỗi ghi đĩa không báo chấp nhận. Chạy test/lint/typecheck, review/commit.

## Task 7: Đồng bộ, chống trùng và đối soát

**Files:** sửa `Server/Services/SoatVeService.cs`, `Models/SoatVeDto.cs`, `Entities/NhatKyQuetVe.cs`, `LuotDi.cs`; tạo `Services/ScanVerificationService.cs`, `SyncService.cs`, `Server.Tests/SyncTests.cs`, `Client/src/sync/queue.ts`, `queue.test.ts`; migration index cạnh tranh.

- [ ] Test fail: gửi lại batch/event không tăng lượt; cùng ID nội dung khác conflict; thiết bị khai hợp lệ nhưng QR sai bị từ chối; kết quả thiết bị từ chối không tự biến thành lượt hợp lệ; cạnh tranh hai thiết bị chỉ một lượt và có cảnh báo.
- [ ] Backend xác minh QR gốc tại scannedAtUtc, phiên/tuyến, packageVersion và snapshot; timeStep khớp cửa sổ, thời gian không vượt nhận quá 5 phút và nằm trong hạn gói. Đồng bộ sau gói hết hạn vẫn được xử lý nếu thời điểm quét hợp lệ trong gói.
- [ ] Chốt lượt hợp lệ theo thứ tự server xử lý; thu hồi sau snapshot lưu lượt kèm STALE_REVOCATION, thu hồi trước snapshot từ chối. Dùng transaction/unique index và trả kết quả từng event, kể cả batch chỉ thành công một phần.
- [ ] Client ACK từng event; retry backoff khi lỗi mạng; event lỗi nghiệp vụ giữ để tra cứu, không retry vô hạn. Online và offline dùng chung quy tắc xác minh/chống trùng.
- [ ] Test kill app sau server commit trước ACK, mất mạng giữa batch, phát lại vé tháng cùng bước; chạy integration test SQL Server và test Client, review/commit.

## Task 8: Nghiệp vụ hành khách và tra cứu khẩn cấp

**Files:** sửa `XacThucService.cs`, `VanHanhService.cs`, `SoatVeService.cs`, controllers/models tương ứng; tạo `Server.Tests/PassengerFlowsTests.cs`, `Client/src/app/passenger/profile.tsx`, `routes.tsx`, `history.tsx`, `quick-buy.tsx`, `Client/src/app/staff/emergency.tsx`.

- [ ] Test fail: hồ sơ chưa duyệt không mua giá ưu tiên bằng cách đổi giaVeId; nhân viên tra cứu chỉ trả thông tin tối thiểu; xác nhận thủ công vẫn chặn trùng và ghi người thực hiện.
- [ ] Thêm cập nhật hồ sơ, nộp hồ sơ ưu tiên/trạng thái duyệt; server kiểm tra điều kiện giá theo đối tượng/hạn hồ sơ.
- [ ] Thêm tìm tuyến, chi tiết trạm theo thứ tự, lịch sử từ API của chính người dùng; bỏ lịch sử hard-code.
- [ ] Quét QR cố định bằng camera, server tìm xe/chuyến đang chạy, xác nhận tuyến rồi tái sử dụng luồng mua mock; xe không có chuyến báo lỗi rõ.
- [ ] Tra cứu CCCD/SĐT chỉ online, ghi lượt thủ công có audit; chạy tests/build/lint/typecheck, review/commit.

## Task 9: API quản trị đầy đủ và cache Redis

**Files:** sửa `VanHanhController.cs`, `NguoiDungController.cs`, `BaoCaoController.cs`, services/models tương ứng; tạo `Services/ReferenceCacheService.cs`, `Server.Tests/AdminOperationsTests.cs`, `ReportTests.cs`.

- [ ] Test fail: thứ tự trạm, lịch trình chồng lấn/giờ sai, xe đã có chuyến, giá âm; nhân viên không được sửa giá hoặc duyệt hồ sơ; đổi giá không sửa số tiền đơn đã tạo.
- [ ] Bổ sung quản lý lộ trình/thứ tự trạm, lịch trình/chuyến, giá/loại vé/đối tượng, thiết bị và phân công nhân viên; tạo/khóa tài khoản, duyệt hồ sơ, khóa/mở vé có audit.
- [ ] Bổ sung danh sách đơn/thanh toán mock, nhật ký và tình trạng sync; cập nhật trạng thái/kết luận cảnh báo; báo cáo theo ngày/tháng/tuyến/đối tượng, thống nhất UTC lưu trữ và ngày địa phương khi lọc.
- [ ] Cache tuyến/bảng giá Redis TTL 5 phút, invalidation khi sửa; Redis tắt thì đọc DB. Test sửa giá không trả cache cũ và Redis mất kết nối không chặn mua/soát vé.
- [ ] Test báo cáo đối chiếu dữ liệu mẫu có đơn thất bại, vé miễn phí và lượt bị từ chối; chạy tests/build, review/commit.

## Task 10: Web Admin

**Files:** tạo `Admin/package.json`, cấu hình Vite/TypeScript, `src/api/client.ts`, `src/auth/session.ts`, `src/pages/{Login,Dashboard,Routes,Stops,Vehicles,Trips,Fares,Users,PriorityProfiles,Tickets,Orders,Scans,Alerts}.tsx`, `src/pages/admin.test.tsx`.

- [ ] Tạo skeleton cùng scripts test/lint/typecheck/build, đăng nhập admin, route guard, xử lý token hết hạn; API vẫn là lớp bảo vệ cuối.
- [ ] Viết/chạy tests fail cho sai quyền, lọc ngày, danh sách rỗng/lỗi mạng và gửi form lặp.
- [ ] Xây các trang quản trị trên API task 9, QR cố định tải/in được; bảng dashboard số lượt/vé/doanh thu/cảnh báo, phân biệt doanh thu mock; xem đối soát và kết luận xử lý.
- [ ] Chạy `npm test`, `npm run lint`, `npm run typecheck`, `npm run build` tại Admin; dùng Orca browser kiểm tra layout và luồng dữ liệu thật, review/commit.

## Task 11: Nghiệm thu, hiệu năng và bàn giao

**Files:** tạo `README.md`, `docs/demo-checklist.md`, `docs/test-results.md`, `docs/security-and-offline.md`, `scripts/verify.ps1`; cập nhật `contracts/api.md` và cấu hình mẫu.

- [ ] Dựng từ checkout sạch bằng README; chạy build/test Server, test/lint/typecheck Client và Admin/build Admin. Không coi test EF InMemory là bằng chứng transaction SQL Server.
- [ ] Chạy demo: admin chuẩn bị chuyến → mua mock thành công/thất bại/hủy/miễn phí → QR đổi offline → nhân viên tải gói/tắt mạng/quét → chặn replay → mở mạng/retry sync → Admin đối soát.
- [ ] Kiểm tra quét QR sửa, vé hết hạn/sai tuyến, hai thiết bị offline, thu hồi trước/sau gói, tra cứu khẩn cấp và mua nhanh bằng QR xe.
- [ ] Đo ít nhất 100 lượt xác minh cục bộ; ghi thiết bị/build, p50/p95/max. Mục tiêu p95 ≤500 ms từ QR đã parse đến khi lưu kết quả; ghi riêng thời gian camera decode, không tuyên bố đạt khi chưa đo.
- [ ] Ghi số test và kết quả thực tế, giới hạn TOTP chia sẻ secret, replay đa thiết bị, đồng hồ và thu hồi; tài khoản demo và hướng dẫn development build. Review toàn bộ diff, commit tài liệu nghiệm thu.

## Thứ tự và điều kiện chuyển bước

1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11. Mỗi task chỉ đánh dấu xong khi tiêu chí test của task có bằng chứng; task cần phần cứng phải ghi rõ phần còn chờ nếu chưa có thiết bị, không báo hoàn thành giả.

Mốc A (task 1–4): đăng nhập, mua mock và QR động offline. Mốc B (5–7): soát vé offline và sync chống trùng. Mốc C (8–10): đủ nghiệp vụ phụ và Web Admin. Mốc D (11): bản demo có số liệu kiểm chứng.

Khi thực hiện, dùng nhánh/worktree riêng nếu cần cách ly; commit theo task sau kiểm tra. Kế hoạch này chỉ tạo tài liệu, chưa sửa code sản phẩm và chưa xác nhận thiết kế dự thảo đã được phê duyệt.
