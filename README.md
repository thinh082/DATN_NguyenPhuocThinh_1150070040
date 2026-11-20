# SmartBus

Ứng dụng vé xe buýt có hai vai trò mobile là hành khách và nhân viên soát vé. Quản trị viên dùng web. Luồng chính là mua vé mô phỏng, hiển thị QR động, soát vé khi mất mạng và đồng bộ lượt quét.

## Công cụ trên Windows

- Git, .NET SDK 8, Node.js 22.13+ trong dòng 22 và npm.
- SQL Server 2019 trở lên, cùng SSMS hoặc `sqlcmd`. Bản mặc định dùng SQL Server local và Windows Authentication.
- Expo Go để thử màn hình mobile được hỗ trợ. Để thử soát vé ngoại tuyến, cần bản Android có SQLCipher. Build Android local cần JDK và Android SDK phù hợp Expo SDK 58. Có thể dùng EAS Build nếu đã cấu hình dự án EAS.
- Redis tùy chọn. Ứng dụng đọc từ SQL Server khi bộ nhớ đệm không khả dụng.

## Dựng database demo sạch

Trong SSMS, chạy theo thứ tự `Server/Database/01_khoi_tao_SmartBusDb.sql`, `02_goi_soat_ve.sql`, `03_tep_minh_chung.sql` và (tùy chọn nạp đủ 134 tuyến xe buýt TP.HCM theo Nghị quyết 09/2026/NQ-HĐND) `04_seed_134_tuyen_NQ09_2026.sql`. Script đầu tạo database. Khi chạy các script sau, chọn database `SmartBusDb` trong SSMS.

Nếu có `sqlcmd`, chạy từ gốc repo với `-f i:65001` để đọc các tệp SQL dưới dạng UTF-8 và giữ đúng dấu tiếng Việt trong dữ liệu mẫu.

```powershell
sqlcmd -S localhost -E -C -b -f i:65001 -i "Server/Database/01_khoi_tao_SmartBusDb.sql"
sqlcmd -S localhost -E -C -b -f i:65001 -d SmartBusDb -i "Server/Database/02_goi_soat_ve.sql"
sqlcmd -S localhost -E -C -b -f i:65001 -d SmartBusDb -i "Server/Database/03_tep_minh_chung.sql"
sqlcmd -S localhost -E -C -b -f i:65001 -d SmartBusDb -i "Server/Database/04_seed_134_tuyen_NQ09_2026.sql"
```

Nếu tên SQL Server trên máy đích khác, thay `-S localhost` trong các lệnh SQL và cài lại chuỗi kết nối. Ví dụ dưới đây dùng instance `SQLEXPRESS` và Windows Authentication.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=SmartBusDb;Trusted_Connection=True;TrustServerCertificate=True" --project Server
```

Từ gốc repo `Project`, chạy công cụ để tạo khóa JWT, khóa mã hóa bí mật TOTP và khóa ký ECDSA cho dữ liệu demo. Công cụ lưu khóa riêng trong .NET user-secrets và đăng ký khóa công khai trong DB.

```powershell
dotnet run --project tools/SmartBus.KeySetup
```

## Chạy ứng dụng

Mở ba terminal tại gốc repo. Giữ backend ở cổng 5056 để khớp proxy Vite.

**Backend**

```powershell
dotnet run --project Server --launch-profile http -- --urls "http://0.0.0.0:5056"
```

Swagger ở `http://localhost:5056/swagger`. Nếu điện thoại truy cập qua LAN, dùng mạng riêng tin cậy và chỉ mở quyền qua Windows Firewall trên profile mạng riêng khi cần.

Để lưu tệp minh chứng mới trên Cloudinary, đặt ba khóa cấu hình backend sau bằng .NET user-secrets ở môi trường Development hoặc biến môi trường tương ứng khi triển khai.

| Khóa cấu hình | Biến môi trường |
| --- | --- |
| `Storage:Cloudinary:CloudName` | `Storage__Cloudinary__CloudName` |
| `Storage:Cloudinary:ApiKey` | `Storage__Cloudinary__ApiKey` |
| `Storage:Cloudinary:ApiSecret` | `Storage__Cloudinary__ApiSecret` |

Khởi động lại API sau khi cấu hình. Cloudinary lưu tệp ở chế độ `authenticated`. Chủ tệp và quản trị viên tải tệp qua API SmartBus. Tệp đã lưu trên đĩa vẫn được đọc từ thư mục cũ. Môi trường demo chưa cấu hình Cloudinary tiếp tục lưu tệp mới trên đĩa.

**Web admin**

```powershell
cd Admin
npm ci
npm run dev
```

Mở `http://localhost:5173`.

**Mobile bằng Expo Go**

```powershell
cd Client
npm ci
$env:EXPO_PUBLIC_API_BASE_URL = "http://IP_MAY_CHU:5056"
npx expo start --go
```

Thay `IP_MAY_CHU` bằng IPv4 Wi-Fi của máy mới, mở cùng mạng với điện thoại, sau đó quét QR của Expo. Có thể kiểm tra bằng cách mở `http://IP_MAY_CHU:5056/swagger` trên điện thoại. Android emulator truy cập máy chủ Windows qua `10.0.2.2`. Cấu hình mặc định trong mobile dùng cổng 5000, vì vậy phải đặt URL trên khi API chạy cổng 5056.

**Soát vé ngoại tuyến trên Android**

Giữ biến URL API như trên. Sau khi cài SDK và bật USB debugging, từ `Client` chạy `npx expo run:android --device` để cài bản Android có SQLCipher. Chọn thiết bị và chuyến đang chạy, tải gói sau khi đã phát hành vé, ngắt mạng trên máy nhân viên rồi quét. Kết nối lại để đồng bộ. Xem [hướng dẫn soát vé](docs/soat-ve-offline.md) và [ghi chú bàn giao](context/HANDOFF.md).

## Dữ liệu và kiểm thử

Backend seed dữ liệu mẫu khi khởi động. Các tài khoản sau chỉ dùng trong môi trường demo local, đều có mật khẩu `123456`.

| Vai trò | Tài khoản |
| --- | --- |
| Quản trị | `admin@smartbus.vn` |
| Hành khách | `thinh@smartbus.vn` |
| Nhân viên | `soatve01@smartbus.vn` |

QR xe demo chứa mã `11111111-1111-1111-1111-111111111111`. Có thể mở và in QR xe từ trang phương tiện trên web quản trị.

```powershell
# Tại gốc repo. Test tích hợp cần SQL Server demo.
dotnet test Server.Tests
# Tại Client, sau npm ci.
npx tsc --noEmit
node --test tests/soatVeOffline.test.mjs
# Tại Admin, sau npm ci.
npm run build
```

Chạy bộ test backend trên database demo dành cho kiểm thử. Một số bài tích hợp ghi SQL Server. Không dùng database chứa dữ liệu thật.

## Trạng thái tiến độ hiện tại

- **Đã hoàn thành (Code & CSDL)**:
  - Backend ASP.NET Core 8 + SQL Server + Redis tùy chọn, ký số ECDSA P-256, TOTP HMAC-SHA256 8 chữ số chu kỳ 30s kết hợp phái sinh khóa **HKDF-SHA256**.
  - Lưu trữ tệp minh chứng hồ sơ ưu tiên hỗ trợ **Cloudinary** (chế độ `authenticated`, tự fallback lưu đĩa local khi chưa cấu hình).
  - Nạp đủ **134 tuyến xe buýt TP.HCM** theo Nghị quyết `09/2026/NQ-HĐND` (`Server/Database/04_seed_134_tuyen_NQ09_2026.sql`).
  - Nâng cấp đồng bộ giao diện **Mobile (`Client/`)** và **Web Admin (`Admin/`)** theo bộ màu **Xanh dương – Xám – Trắng**.
  - Đầy đủ luồng: đăng ký/đăng nhập/phân quyền, hồ sơ ưu tiên, mua vé mô phỏng, quét QR dán trên xe mua nhanh, ví QR động (`FLAG_SECURE` + tăng độ sáng), soát vé ngoại tuyến SQLCipher + âm báo + đồng bộ phát hiện quét trùng, tra cứu khẩn cấp CCCD/SĐT và khóa vé.
- **Đang hoàn thiện cho đợt nộp bài (trước 23h00 ngày 22/11/2026)**:
  - Lấy bằng chứng & số liệu thực nghiệm trên thiết bị Android thật (SQLCipher, camera, âm báo, `FLAG_SECURE`, đo thời gian xác thực / p95).
  - Hoàn thiện các sơ đồ Chương 2, chèn ảnh giao diện/số liệu Chương 3 vào báo cáo Word & xuất PDF.
  - Chuẩn bị slide thuyết trình và quay video thuyết trình + demo (tối đa 15 phút).
  - Chi tiết bàn giao xem tại [context/HANDOFF.md](context/HANDOFF.md) và checklist nộp bài tại [context/YEU_CAU_NOP_DO_AN.md](context/YEU_CAU_NOP_DO_AN.md).

