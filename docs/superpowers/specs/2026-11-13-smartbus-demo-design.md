# SmartBus — Spec hoàn thiện hệ thống demo bảo vệ đồ án

- Ngày: 2026-11-13
- Trạng thái: Bản dự thảo chờ phê duyệt
- Phương án đã chọn: A — hoàn thiện một luồng demo end-to-end
- Phạm vi: các phần còn thiếu hoặc đang lỗi trong `Client/`, `Server/` và Web Admin mới

## 1. Mục đích

Hoàn thiện hệ thống SmartBus để sinh viên có thể trình diễn một luồng nghiệp vụ hoàn chỉnh, có dữ liệu thật trong SQL Server và có kiểm thử chứng minh các tuyên bố chính của đề tài:

1. Hành khách đăng nhập và mua vé lượt hoặc vé tháng.
2. Hệ thống ghi nhận thanh toán mô phỏng qua Webhook/IPN hoặc xác nhận vé miễn phí.
3. Backend phát hành vé có chữ ký ECDSA.
4. Mobile hành khách lưu vé và tạo QR động bằng TOTP, kể cả khi mất mạng.
5. Nhân viên soát vé tải gói dữ liệu kiểm tra trước, sau đó tắt mạng và quét QR.
6. Thiết bị kiểm tra chữ ký, hiệu lực, TOTP và trạng thái vé; phản hồi âm thanh và lưu lượt quét cục bộ.
7. Khi có mạng, thiết bị đồng bộ nhật ký; Backend xác minh lại, chống trùng và ghi nhận cảnh báo.
8. Quản trị viên xem lượt đi, doanh thu và cảnh báo trên Web Admin.

Spec ưu tiên một bản demo ổn định, dễ cài đặt, dễ kiểm thử và giải thích đúng giới hạn, thay vì mở rộng thành hệ thống vận hành thương mại.

## 2. Hiện trạng và lý do phải hoàn thiện

Project đã có nền tảng ASP.NET Core 8, EF Core SQL Server, các entity và API cho tài khoản, vận hành, vé, soát vé và báo cáo. Client Expo/React Native đã có demo đăng nhập, xem tuyến, mua vé và ví vé.

Các điểm chặn luồng chính:

- Private key dùng ký vé không tương ứng public key dùng kiểm tra.
- Khóa AES hiện tại có độ dài không hợp lệ và nhãn thuật toán không khớp cài đặt.
- JWT chưa được áp dụng bằng middleware và `[Authorize]`; Client chưa lưu/gửi token.
- Ví vé chưa render QR; QR động đang được tạo lại bởi server khi tải ví.
- Chưa có giao diện soát vé, camera, âm thanh và lưu trữ offline.
- API đồng bộ còn tin tưởng kết quả từ thiết bị và thiếu idempotency theo sự kiện.
- Thanh toán đang tự đánh dấu thành công, chưa có Webhook/IPN.
- Chưa có Web Admin, EF Migrations và test first-party.
- Redis mới được đăng ký, chưa phục vụ một yêu cầu cụ thể của demo.

## 3. Mục tiêu, không mục tiêu và điều kiện thất bại

### 3.1. Mục tiêu bắt buộc

- Luồng demo tại mục 1 chạy end-to-end mà không cần sửa dữ liệu trực tiếp trong database.
- Vé phát hành phải xác minh được bằng public key tương ứng; vé bị sửa nội dung phải bị từ chối.
- Hành khách tạo được QR động offline sau khi đã tải vé.
- Nhân viên xác thực được offline sau khi đã tải gói kiểm tra.
- Đồng bộ lại cùng một batch không tạo thêm lượt đi hoặc cảnh báo trùng.
- Backend không chấp nhận kết quả “hợp lệ” chỉ vì thiết bị khai báo; phải kiểm tra lại dữ liệu QR.
- Có phân quyền thực thi cho ba vai trò: hành khách, nhân viên soát vé và quản trị viên.
- Có Web Admin đủ để chuẩn bị và theo dõi buổi demo.
- Có test tự động cho các tuyên bố bảo mật và nghiệp vụ quan trọng.
- Có dữ liệu mẫu, tài khoản demo, hướng dẫn chạy và kịch bản trình diễn.

### 3.2. Không mục tiêu

- Không thanh toán tiền thật hoặc phát hành ứng dụng thương mại.
- Không triển khai HSM, KMS, chứng thư số doanh nghiệp hoặc hạ tầng production.
- Không cam kết ngăn tuyệt đối replay giữa nhiều thiết bị cùng mất mạng.
- Không xây dựng hệ thống phân phối/thu hồi khóa nhiều lớp; chỉ hỗ trợ `keyId` và một khóa ký hoạt động cho demo.
- Không phát triển hai codebase mobile riêng; dùng chung Expo project và tách giao diện theo vai trò.
- Không mở rộng thành bản đồ, định vị GPS, tìm đường tối ưu hoặc quản lý vận hành ngoài đề cương.
- Không bắt buộc dùng Redis nếu không có use case cần thiết cho demo.

### 3.3. Điều kiện được xem là thất bại

- Demo chính phụ thuộc vào dữ liệu hard-code hoặc thao tác SQL thủ công.
- Ứng dụng báo thanh toán/phát hành vé thành công nhưng database không có giao dịch tương ứng.
- QR chỉ là chuỗi text, không quét được bằng camera.
- Chế độ offline chỉ đổi giao diện mà không xác minh mật mã tại thiết bị.
- Mọi endpoint đều truy cập được mà không cần đúng vai trò.
- Test chỉ kiểm tra trường hợp hợp lệ, không có vé giả, hết hạn, replay hoặc callback giả.
- Báo cáo tuyên bố “loại bỏ triệt để gian lận” hoặc “an toàn tuyệt đối”.

## 4. Các phương án đã cân nhắc

### Phương án A — Luồng demo end-to-end, đã chọn

Tận dụng Backend và Expo Client hiện có; sửa các lỗi nền trước, sau đó hoàn thiện từng lát cắt nghiệp vụ. Dùng payment simulator có Webhook/IPN ký HMAC, một mobile app phân vai trò, offline trên thiết bị đã chuẩn bị và Web Admin tối thiểu.

Ưu điểm: phù hợp tiến độ đồ án, kiểm thử được, giảm số thành phần phải vận hành và vẫn trình diễn đúng các ý tưởng chính.

### Phương án B — Gần với hệ thống thực tế

Tách hai mobile app, tích hợp cổng sandbox bên ngoài, hỗ trợ nhiều thiết bị offline, quản lý khóa nâng cao và đầy đủ Web Admin. Phương án này không được chọn vì khối lượng và rủi ro vượt mục tiêu demo bảo vệ.

## 5. Kiến trúc và công nghệ

### 5.1. Thành phần

- `Server/`: ASP.NET Core 8 Web API, EF Core, SQL Server.
- `Client/`: Expo SDK 58, React Native, TypeScript; dùng Expo Router cho điều hướng theo quy ước của project.
- `Admin/`: React + TypeScript + Vite cho Web Admin mới. Đây là lựa chọn thiết kế của Spec; không tái sử dụng chế độ `expo start --web` làm Admin.
- `Server` tests: xUnit cho unit/integration tests.
- Client tests: test thuần TypeScript cho canonical payload, TOTP, state machine vé và xử lý kết quả đồng bộ; test UI/e2e chỉ bổ sung nếu phù hợp toolchain hiện có.

### 5.2. Quy ước chung

- API trả lỗi nghiệp vụ nhất quán, có mã lỗi để Client hiển thị thông báo tiếng Việt.
- Mọi thao tác phát hành vé, xác nhận thanh toán, khóa vé và đồng bộ phải có transaction/idempotency phù hợp.
- Không ghi access token, private key, TOTP secret, CCCD hoặc payload vé đầy đủ vào log.
- Secrets phía server đọc từ environment hoặc .NET user-secrets; không commit giá trị thật.
- Các package Expo phải được kiểm tra với tài liệu đúng SDK 58 trước khi cài đặt/cài đặt code.

## 6. Xác thực, phân quyền và dữ liệu demo

### 6.1. Backend

- Bật JWT authentication và authorization; áp dụng `[Authorize]` theo vai trò cho endpoint.
- Đăng ký công khai luôn tạo vai trò hành khách, bỏ khả năng Client tự truyền vai trò nhân viên/quản trị viên.
- Chỉ quản trị viên tạo tài khoản nhân viên và đổi vai trò.
- Thay SHA-256 salt cố định bằng ASP.NET Core `PasswordHasher<TUser>` để có salt ngẫu nhiên và thuật toán băm mật khẩu phù hợp.
- JWT secret tối thiểu 32 byte ngẫu nhiên, nạp từ cấu hình bí mật; không có fallback hard-code.
- Token có issuer, audience, expiry và claim vai trò; demo dùng access token ngắn hạn, không bổ sung refresh token nếu chưa cần.
- Khóa/mở tài khoản phải có tác dụng ở request tiếp theo; tối thiểu kiểm tra trạng thái tài khoản khi cấp token và tại endpoint nhạy cảm.

### 6.2. Client và Admin

- Mobile lưu access token trong Expo SecureStore; Admin dùng cơ chế lưu token phù hợp và tự đăng xuất khi hết phiên.
- Axios/fetch tự gắn `Authorization: Bearer ...`; xóa token khi đăng xuất.
- Điều hướng sau đăng nhập theo vai trò; ẩn giao diện không thuộc quyền nhưng vẫn dựa vào Backend để bảo vệ dữ liệu.
- Có màn hình đăng ký, đăng nhập, hồ sơ và trạng thái hồ sơ ưu tiên.

### 6.3. Dữ liệu mẫu

Seed idempotent tối thiểu:

- Một quản trị viên, một nhân viên soát vé, một hành khách phổ thông và một hành khách ưu tiên đã duyệt.
- Hai tuyến, trạm theo thứ tự, hai xe có QR cố định và chuyến đang hoạt động.
- Nhóm đối tượng, loại vé, giá vé lượt/tháng và giá ưu tiên/miễn phí.
- Public key của khóa ký demo; không seed private key vào database hoặc source.
- Tài khoản mật khẩu demo được ghi trong hướng dẫn chạy, chỉ dùng dữ liệu giả.

## 7. Thanh toán mô phỏng và phát hành vé

### 7.1. Payment simulator

Simulator thay thế cổng thật chỉ trong môi trường Development/Demo nhưng phải đi qua state machine rõ ràng:

```text
Tạo đơn Pending
→ Mobile hiển thị trang thanh toán mô phỏng
→ Hành khách chọn thành công hoặc thất bại
→ Simulator tạo sự kiện và ký HMAC-SHA256
→ Backend nhận Webhook/IPN
→ Kiểm tra chữ ký, eventId, đơn hàng và số tiền
→ Lưu sự kiện idempotent
→ Chỉ phát hành vé khi thanh toán thành công
→ Mobile tải lại trạng thái đơn/ví vé
```

- Webhook/IPN có `eventId`, `orderId`, `amount`, `status`, `timestamp` và chữ ký HMAC.
- Callback giả, sai số tiền, quá hạn hoặc lặp `eventId` phải bị từ chối/xử lý idempotent.
- Một đơn chỉ phát hành vé một lần dù IPN được gửi lại nhiều lần.
- Endpoint simulator chỉ bật bằng cấu hình Demo/Development và phải hiển thị nhãn “Mô phỏng thanh toán”.
- Không gọi simulator là “tích hợp MoMo thật” trong giao diện hoặc báo cáo.

### 7.2. Nghiệp vụ vé

- Vé lượt: hiệu lực theo quy tắc demo 24 giờ và chỉ được ghi nhận một lượt đi hợp lệ.
- Vé tháng: hiệu lực 30 ngày, cho nhiều lượt nhưng chặn lặp trong cùng bước TOTP.
- Vé ưu tiên đủ điều kiện miễn phí: bỏ qua thanh toán nhưng vẫn tạo bản ghi giao dịch 0 đồng và phát hành vé.
- Đơn thất bại/hủy không phát hành vé.
- Phát hành vé, lưu chữ ký và lưu bí mật TOTP phải là một giao dịch nghiệp vụ nguyên tử.

## 8. Thiết kế mật mã và Dynamic QR

### 8.1. Quản lý khóa

- ECDSA P-256 với SHA-256; chữ ký DER được mã hóa Base64URL trong QR.
- Private key chỉ nằm ở Backend, đọc từ user-secrets/environment. Startup phải báo lỗi rõ nếu thiếu khóa; không tự tạo khóa tạm khác sau mỗi lần chạy.
- Public key có `keyId`, thuật toán, trạng thái và được lưu trong database để phát cho thiết bị soát vé.
- Cung cấp script/lệnh thiết lập demo tạo cặp khóa một lần và ghi vào secret store cục bộ; không commit private key.
- AES-GCM-256 bảo vệ TOTP secret trong database: khóa 32 byte từ secret store, nonce ngẫu nhiên 12 byte cho mỗi lần mã hóa, authentication tag 16 byte.

### 8.2. Canonical payload

Dùng chuỗi UTF-8 có phiên bản, không ký JSON không chuẩn hóa. Các trường GUID/số/Base64URL không chứa dấu phân cách.

Phần vé cố định được ký:

```text
SB1|ticketId|ticketType|routeId|validFromUnix|validToUnix|keyId|nonce
```

QR động:

```text
<staticPayload>|timeStep|totp|signatureBase64Url
```

- `ticketType`: mã số do Backend quy định.
- `routeId`: GUID hoặc `-` nếu vé áp dụng không theo tuyến.
- `validFromUnix` và `validToUnix`: giây UTC.
- `nonce`: tối thiểu 16 byte ngẫu nhiên, Base64URL.
- `timeStep = floor(unixTime / 30)`.
- TOTP: HMAC-SHA256, 8 chữ số; chấp nhận bước hiện tại và ±1 bước để bù lệch đồng hồ.
- Backend và Client dùng chung test vectors để bảo đảm ký/xác minh và TOTP cho kết quả giống nhau.

### 8.3. Phân phối bí mật vé

- Mỗi vé có TOTP secret ngẫu nhiên tối thiểu 20 byte.
- Mobile hành khách nhận secret qua API đã xác thực, lưu trong SecureStore và cache dữ liệu vé để tạo QR offline.
- Thiết bị nhân viên được cấp quyền tải gói kiểm tra gồm public key, dữ liệu vé tối thiểu, TOTP secret và trạng thái thu hồi khi tải.
- Gói offline của nhân viên lưu trong SQLite có SQLCipher; khóa database nằm trong SecureStore. Nếu SDK hiện tại không hỗ trợ cấu hình này, phải chọn giải pháp mã hóa tương đương trước khi triển khai, không lưu secret dạng plaintext.
- Private key ECDSA không bao giờ xuất hiện trên mobile/Admin.
- Gói offline có `packageVersion` và `expiresAt`; hết hạn yêu cầu tải lại khi online.

### 8.4. Mobile hành khách

- Ví vé tải và cache vé khi online; chỉ những vé đã cache mới mở được offline.
- QR do mobile tự tạo lại mỗi 30 giây, không gọi API cho mỗi chu kỳ.
- Render QR thật bằng thư viện tương thích Expo; không hiển thị payload thay QR.
- Bật chống chụp màn hình trên nền tảng hỗ trợ, tăng sáng khi mở vé và khôi phục độ sáng cũ khi thoát.
- Hiển thị trạng thái online/offline, hạn dùng vé và chu kỳ mã; không cho quét vé đã khóa/hết hạn như thể còn hợp lệ.
- Có đồng bộ ví khi mở app online để nhận trạng thái thu hồi mới.

## 9. Ứng dụng soát vé và xác thực offline

### 9.1. Chuẩn bị trước demo

- Nhân viên đăng nhập, chọn thiết bị/tuyến và tải gói offline mới nhất.
- App hiển thị phiên bản gói, lúc tải, hạn dùng và số vé có thể kiểm tra.
- Demo offline chỉ được cam kết với vé đã có trong gói; vé mới phát hành sau phiên bản gói sẽ bị từ chối với lý do “không có dữ liệu offline”, không được đoán là hợp lệ.

### 9.2. Luồng quét

1. Camera đọc QR và parse canonical payload.
2. Kiểm tra phiên bản, định dạng và độ dài từng trường.
3. Tìm public key theo `keyId` và xác minh chữ ký trên phần vé cố định.
4. Kiểm tra vé có trong gói, trạng thái khóa/thu hồi và hạn dùng.
5. Nếu vé có `routeId` cụ thể, so sánh với tuyến của chuyến nhân viên đã chọn; từ chối khi sai tuyến. Vé có `routeId = -` được dùng trên mọi tuyến trong phạm vi demo.
6. Kiểm tra `timeStep` trong cửa sổ cho phép và TOTP bằng secret của vé.
7. Kiểm tra trùng cục bộ: vé lượt đã dùng hoặc vé tháng đã quét trong cùng `timeStep`.
8. Ghi sự kiện vào SQLite trong một transaction rồi mới hiển thị/phát âm thanh kết quả.

Kết quả tối thiểu: hợp lệ, sai chữ ký, sai TOTP, hết hạn/chưa hiệu lực, sai tuyến, vé bị khóa, đã sử dụng, quét trùng, không có dữ liệu offline và QR không đúng định dạng.

### 9.3. Nhật ký sự kiện

Mỗi lần quét có:

- `eventId` UUID do thiết bị tạo, duy nhất và idempotent.
- `deviceId`, `ticketId`, `scannedAtUtc`, `timeStep`, `packageVersion`.
- Kết quả, mã lý do và payload QR gốc để Backend kiểm tra lại.
- `routeId`/`tripId` nếu nhân viên đã chọn chuyến; bắt buộc khi xác nhận lượt đi hợp lệ.

Không ghi CCCD hoặc dữ liệu cá nhân không cần thiết vào nhật ký quét.

### 9.4. Đồng bộ

- Gửi theo batch có `batchId`; từng sự kiện được xử lý và trả kết quả riêng, không làm hỏi cả batch vì một bản trùng.
- Backend tra `eventId` trước; nếu đã xử lý thì trả lại kết quả cũ.
- Backend xác minh lại chữ ký, TOTP tại `scannedAtUtc/timeStep`, hạn dùng tại `scannedAtUtc`, tuyến của chuyến và trạng thái vé từ payload gốc; không tin trường kết quả từ thiết bị. `scannedAtUtc` phải thuộc bước TOTP được chấp nhận, nằm trong hạn dùng của gói offline và không vượt quá lúc server nhận quá 5 phút.
- Với sự kiện offline, trạng thái thu hồi được đối chiếu tại phiên bản gói `packageVersion`. Nếu vé bị thu hồi sau khi tạo gói, lượt quét đã chấp nhận offline vẫn được lưu kèm cảnh báo `STALE_REVOCATION`; không xóa ngược lượt đi nhưng phải hiển thị để Admin xử lý. Quét online luôn dùng trạng thái hiện tại.
- Vé lượt: sự kiện hợp lệ đầu tiên được Backend xử lý là lượt đi được chấp nhận; các sự kiện đồng bộ sau tạo cảnh báo replay. Thứ tự này dựa vào thứ tự xử lý của server, không chọn lại theo `scannedAtUtc`; mọi timestamp vẫn được giữ để điều tra.
- Vé tháng: cho nhiều lượt nhưng cùng `ticketId + timeStep` chỉ được ghi một lượt; xung đột giữa thiết bị tạo cảnh báo.
- Thiết bị chỉ đánh dấu sự kiện đã đồng bộ khi nhận xác nhận tương ứng; retry an toàn.
- Sau đồng bộ, tải lại trạng thái vé/cảnh báo liên quan để cập nhật gói offline.

### 9.5. Giới hạn phải trình bày

- Hai thiết bị cùng offline không thể biết lượt quét của nhau ngay lập tức; hệ thống phát hiện khi đồng bộ.
- Đồng hồ thiết bị sai quá cửa sổ TOTP có thể làm vé hợp lệ bị từ chối; demo dùng giờ tự động.
- Thu hồi vé khi thiết bị offline chỉ có tác dụng sau lần cập nhật gói tiếp theo.
- Tra cứu CCCD/SĐT là chức năng online; không cache dữ liệu cá nhân rộng trên thiết bị offline.

## 10. Các chức năng mobile còn lại

### 10.1. Mua nhanh bằng QR trên xe

- QR cố định chứa mã xe và nonce định danh, không chứa quyền phát hành vé.
- Hành khách quét bằng camera; app gọi Backend xác thực mã xe và lấy tuyến/chuyến hiện tại.
- Hành khách xác nhận tuyến, loại vé rồi đi vào cùng luồng đơn hàng/thanh toán ở mục 7.
- Nếu xe không có chuyến hoạt động, hiển thị lỗi nghiệp vụ rõ ràng.

### 10.2. Tra cứu khẩn cấp

- Nhân viên tra bằng CCCD hoặc số điện thoại khi online.
- Chỉ trả vé còn hiệu lực và thông tin tối thiểu cần đối chiếu.
- Xác nhận ghi lượt đi thủ công có kiểm tra trùng và lưu nhân viên thực hiện.
- Không cho endpoint công khai tra danh sách tài khoản.

### 10.3. Lịch sử và tuyến

- Bỏ lịch sử chuyến đi hard-code; lấy từ API lượt đi của chính hành khách.
- Có tìm kiếm tuyến cơ bản và xem trạm theo thứ tự; không làm tìm đường nâng cao.

## 11. Web Admin tối thiểu

### 11.1. Đăng nhập và tổng quan

- Đăng nhập quản trị viên, route guard và xử lý hết phiên.
- Dashboard có bộ lọc ngày/tuyến: số lượt đi, số vé phát hành, doanh thu, số cảnh báo.
- Biểu đồ chỉ bổ sung nếu dữ liệu và tiến độ cho phép; bảng số liệu là bắt buộc.

### 11.2. Quản lý demo

- CRUD tuyến, trạm, xe; quản lý thứ tự trạm trên tuyến.
- Quản lý chuyến đang chạy và QR cố định của xe.
- Xem giá vé; sửa giá nếu cần cho kịch bản demo.
- Tạo/khóa tài khoản nhân viên; duyệt hồ sơ ưu tiên.
- Xem đơn hàng và trạng thái thanh toán simulator.

### 11.3. Vé và gian lận

- Tra cứu vé, khóa/mở khóa vé và khóa tài khoản theo quyền.
- Danh sách lượt quét online/offline và trạng thái đồng bộ.
- Danh sách cảnh báo, chi tiết sự kiện liên quan và cập nhật trạng thái xử lý/kết luận.
- Không hiển thị private key hoặc TOTP secret trên Admin.

## 12. Database, migrations và Redis

- Thêm EF Core Migrations làm nguồn quản lý schema chính; không dùng `EnsureCreatedAsync` cho database demo mới.
- Không tự động xóa database hiện có. Nếu schema cũ khó migrate, tạo database riêng `SmartBusDemoDb` và hướng dẫn đổi connection string.
- Đồng bộ ràng buộc giữa SQL script và EF model, đặc biệt `NhatKyQuetVe.veId` nullable cho lượt quét lỗi.
- Thêm unique/index cần thiết cho `eventId`, `batchId`, `eventId` thanh toán và quy tắc chống replay.
- Seed đầy đủ `NhomDoiTuong`, `LoaiVe` trước `GiaVe` và các dữ liệu phụ thuộc.
- Redis không nằm trong đường demo bắt buộc. Có thể giữ cấu hình hiện tại nhưng loại khỏi yêu cầu khởi động; chỉ dùng cho rate limit/cache nếu có test và lý do cụ thể.
- Dapper không bắt buộc vì EF Core đã đáp ứng phạm vi demo.

## 13. Kiểm thử và tiêu chí nghiệm thu

### 13.1. Test Backend bắt buộc

- ECDSA: ký/xác minh thành công; sửa bất kỳ trường nào hoặc dùng sai public key đều thất bại.
- AES-GCM: giải mã đúng; sửa ciphertext/tag/nonce thất bại; nonce không dùng lại theo thiết kế.
- TOTP: test vectors giống giữa Backend và TypeScript; cửa sổ ±1 đúng; mã ngoài cửa sổ bị từ chối.
- Auth: anonymous bị chặn; sai vai trò bị chặn; đăng ký không thể tự nâng quyền; tài khoản khóa không dùng được.
- Payment: callback đúng được nhận; sai chữ ký/số tiền bị từ chối; lặp `eventId` không phát thêm vé.
- Vé: vé lượt chỉ dùng một lần; vé tháng chặn cùng `timeStep`; hết hạn/khóa/sai tuyến bị từ chối.
- Sync: retry cùng event/batch idempotent; payload giả dù kèm kết quả “valid” vẫn bị từ chối; xung đột hai thiết bị tạo cảnh báo; sự kiện đồng bộ trước được giữ là lượt hợp lệ; thu hồi sau phiên bản gói tạo `STALE_REVOCATION`.
- API báo cáo khớp dữ liệu giao dịch/lượt đi mẫu.

### 13.2. Test Client và kiểm thử thiết bị

- TypeScript tạo canonical payload và TOTP khớp test vectors Backend.
- QR thay đổi đúng chu kỳ mà không gọi mạng.
- Vé đã cache mở offline; vé chưa cache báo rõ không khả dụng.
- Scanner xử lý được các trạng thái ở mục 9.2 và lưu sự kiện trước khi báo thành công.
- Chống chụp màn hình/tăng sáng được kiểm tra trên Android thật; ghi rõ giới hạn nếu chạy nền tảng khác.
- Đo 100 lượt xác minh cục bộ trên thiết bị demo: p95 từ lúc QR đã parse đến khi có kết quả ≤ 500 ms, không tính thao tác đưa camera vào mã; ghi model thiết bị và kết quả thực tế.
- Lint, typecheck, build/test của từng project phải chạy thành công trước khi nhận hoàn thành.

### 13.3. Kịch bản nghiệm thu end-to-end

1. Admin đăng nhập và xác nhận tuyến/xe/chuyến mẫu.
2. Hành khách mua vé lượt bằng simulator thành công.
3. Hành khách ưu tiên nhận vé 0 đồng.
4. Mobile hiển thị QR thay đổi; tắt mạng vẫn tạo mã từ vé đã cache.
5. Nhân viên tải gói offline, tắt mạng và quét vé hợp lệ.
6. Quét lại ngay để thấy cảnh báo trùng; quét QR bị sửa và vé hết hạn để thấy từ chối.
7. Tra CCCD/SĐT online để trình diễn trường hợp điện thoại hành khách hết pin.
8. Bật mạng, đồng bộ và gửi lại batch để chứng minh không nhân đôi lượt đi.
9. Admin xem lượt đi/doanh thu/cảnh báo và cập nhật kết quả xử lý.
10. Mua nhanh bằng QR trên xe như luồng phụ nếu luồng chính đã ổn định.

## 14. Bộ cài đặt và tài liệu bàn giao

- File cấu hình mẫu không chứa secret thật.
- Script/lệnh tạo signing key, AES key, JWT secret và IPN secret vào user-secrets/environment.
- Migration và seed một lệnh cho database demo mới.
- Hướng dẫn chạy Server, Client, Admin và test; ghi yêu cầu Android development build nếu Expo Go không hỗ trợ module.
- Tài khoản demo, dữ liệu mẫu và checklist trước buổi bảo vệ.
- Kịch bản demo có bản dự phòng: dữ liệu đã seed, ảnh chụp màn hình và video ngắn; không dùng dự phòng để che giấu lỗi khi chấm chức năng.
- Cập nhật báo cáo: kiến trúc payload, quản lý khóa, offline/sync, kết quả test, hiệu năng đo được và giới hạn thực tế.

## 15. Thứ tự hoàn thiện theo rủi ro

1. Chốt schema, migrations, seed, secrets và test harness.
2. Sửa mật mã, canonical payload và auth; chứng minh phát hành/xác minh vé bằng test.
3. Hoàn thiện payment simulator và phát hành vé idempotent.
4. Hoàn thiện ví vé QR offline của hành khách.
5. Hoàn thiện scanner offline, SQLite và đồng bộ idempotent.
6. Hoàn thiện Web Admin và các luồng phụ.
7. Chạy nghiệm thu trên thiết bị thật, đo hiệu năng, sửa lỗi và đóng gói tài liệu demo.

Mỗi bước phải giữ được luồng đã hoàn thành trước đó; không xây toàn bộ giao diện trước khi hai điểm chặn mật mã và auth được giải quyết.

## 16. Quyết định do Spec đưa ra

Các lựa chọn sau là đề xuất thiết kế để bạn kiểm tra khi phê duyệt, không phải yêu cầu đã có từ trước:

- Dùng chung Expo app và điều hướng theo vai trò thay vì tách hai app.
- Web Admin mới dùng React + TypeScript + Vite.
- Payment simulator ký HMAC và gửi Webhook/IPN thật trong hệ thống, nhưng không kết nối cổng ngoài.
- Canonical payload dạng `SB1|...`; TOTP 8 số/30 giây, chấp nhận ±1 bước.
- Phân phối TOTP secret theo vé cho thiết bị soát vé được ủy quyền; bảo vệ gói offline bằng SQLCipher và SecureStore.
- Vé lượt dùng một lần; vé tháng chặn trùng theo `timeStep`.
- Redis và Dapper không nằm trong phạm vi bắt buộc của demo.
- Mục tiêu hiệu năng đo p95 ≤ 500 ms sau khi QR đã parse, trên thiết bị demo được ghi rõ cấu hình.

## 17. Phần để đội ngũ triển khai quyết định

Đội ngũ triển khai có thể tự chọn mà không cần hỏi lại, miễn không thay đổi nghiệp vụ và tiêu chí nghiệm thu:

- Cấu trúc file/component cụ thể theo quy ước hiện có.
- Thư viện render QR, biểu tượng, UI và biểu đồ tương thích SDK/project.
- Tên biến, DTO chi tiết và cách chia service nội bộ.
- Chiến lược cache nhỏ cho API báo cáo nếu thật sự cần.
- Số lượng bản ghi seed bổ sung ngoài tối thiểu.
- Công cụ test UI/e2e nếu bổ sung ngoài các test bắt buộc.

Mọi thay đổi liên quan thuật toán, định dạng payload, cách phân phối secret, state machine thanh toán hoặc quy tắc replay phải cập nhật Spec và test vectors trước khi triển khai.
