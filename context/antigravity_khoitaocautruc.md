# NHẬT KÝ KHỞI TẠO CẤU TRÚC HỆ THỐNG (SERVER & CLIENT)

> **Dự án:** Xây dựng hệ thống xác thực và quản lý lượt đi xe buýt sử dụng mã QR động, chữ ký số và cơ chế xác thực ngoại tuyến  
> **Sinh viên:** Nguyễn Phước Thịnh (1150070040)  
> **Thời gian khởi tạo:** 25/10/2026  
> **Trạng thái:** Đã dựng sườn hoàn chỉnh, build thành công 0 Warning / 0 Error.

---

## 1. Cấu trúc thư mục tổng thể

```
Project/
├── DATN_DeCuong_NguyenPhuocThinh_1150070040.docx
├── context/
│   └── antigravity_khoitaocautruc.md       <-- (File tài liệu này)
├── Server/                                 <-- ASP.NET Core 8 Web API
│   ├── Commons/
│   │   └── KetQuaPhanHoi.cs               <-- Chuẩn hóa response { data, statusCode, message }
│   ├── Controllers/
│   │   └── NguoiDungController.cs         <-- Chỉ dùng HttpGet & HttpPost, gọi qua Service
│   ├── Models/
│   │   └── NguoiDungDto.cs                <-- DTO đối tượng phức tạp
│   ├── Services/
│   │   └── NguoiDungService.cs            <-- Interface ở trên, Service ở dưới, cùng 1 file
│   ├── appsettings.json                   <-- Cấu hình SQL Server & chờ Redis Online
│   ├── Program.cs                         <-- Cấu hình CORS AllowAll, DI, Redis, Swagger
│   └── Server.csproj
└── Client/                                 <-- React Native Expo Go (TypeScript)
    ├── App.tsx                            <-- Màn hình demo phong cách Trắng - Xanh - Xám
    ├── constants/
    │   └── colors.ts                      <-- Bảng màu chủ đạo: Trắng, Xanh dương, Xám
    ├── services/
    │   ├── api.ts                         <-- Cấu hình Axios instance & URL backend
    │   └── nguoiDungService.ts            <-- Logic gọi các endpoint backend
    ├── package.json                       <-- Đã cài @expo/ngrok & script start:tunnel
    └── tsconfig.json
```

---

## 2. Quy tắc lập trình trên Server (ASP.NET Core 8)

Hệ thống Backend được thiết kế tuân thủ nghiêm ngặt các quy tắc sau:

1. **Quy ước đặt tên (Naming Conventions):**
   * Tên hàm và tên biến ưu tiên hoàn toàn bằng **tiếng Việt không dấu** (ví dụ: `layDanhSach`, `layChiTiet`, `luuNguoiDung`, `khoaTaiKhoan`, `hoTen`, `soDienThoai`...).
   * Không sử dụng chú thích rườm rà `/// <summary>` và `#region`.

2. **Cấu trúc Controller - Service:**
   * Controller gọi Interface của Service tương ứng.
   * **Interface và Service được viết chung trong cùng 1 file `.cs`** (Interface nằm ở trên, Service class triển khai nằm ở dưới).
   * **Gộp hàm Thêm và Sửa:** Tất cả các thao tác lưu dữ liệu được viết trong 1 hàm duy nhất (`luuNguoiDung`):
     * Nếu `id == 0`: Thực hiện nghiệp vụ Thêm mới (Insert).
     * Nếu `id != 0`: Thực hiện nghiệp vụ Cập nhật (Update).

3. **Giao thức & Phản hồi (HTTP Methods & Response):**
   * **Chỉ sử dụng `[HttpGet]` và `[HttpPost]`**, không dùng PUT, DELETE, PATCH.
   * Mọi API đều trả về dạng `Ok(KetQuaPhanHoi.ThanhCong(...))` hoặc `Ok(KetQuaPhanHoi.ThatBai(...))` với cấu trúc JSON chuẩn:
     ```json
     {
       "data": { ... },
       "statusCode": 200, // hoặc 500, 404, 400
       "message": "Thông báo kết quả thực thi"
     }
     ```
   * Với các hàm đơn giản: Trả về kiểu `dynamic` / anonymous object.
   * Với các hàm phức tạp: Sử dụng Model/DTO (như `NguoiDungDto`).

4. **Cơ chế CORS:**
   * Cấu hình chính sách `AllowAll` (`AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader`) đảm bảo mọi Client từ Web, Mobile Expo, máy ảo, điện thoại thật hay qua ngrok đều kết nối thông suốt mà không gặp lỗi CORS.

5. **Cơ sở dữ liệu & Redis:**
   * Đã cài sẵn các package EF Core 8 (`Microsoft.EntityFrameworkCore.SqlServer`, `Tools`, `Design`) sẵn sàng chờ AI khác cung cấp cấu trúc SQL để chạy lệnh `dotnet ef dbcontext scaffold`.
   * Cấu hình Redis trong `appsettings.json` sẵn sàng nhận chuỗi kết nối Online sau này.

---

## 3. Kiến trúc Client (React Native Expo TypeScript)

1. **Hệ màu chủ đạo (`constants/colors.ts`):**
   * **Màu trắng:** `#FFFFFF` (nền card, bề mặt, chữ trên nền đậm).
   * **Màu xanh dương:** `#0066CC` (màu nhận diện chủ đạo cho hệ thống giao thông công cộng), `#004C99` (dark), `#E6F0FA` (light/badge).
   * **Màu xám:** `#F5F7FA` (nền app), `#E2E8F0` (viền border), `#4A5568` / `#718096` (chữ phụ).

2. **Cấu hình Service gọi API (`services/`):**
   * `api.ts`: Cấu hình Axios instance với `baseURL`, timeout, header và interceptor tự động bắt lỗi hoặc chuẩn hóa kiểu `PhanHoiApi<T>`.
   * `nguoiDungService.ts`: Các hàm `layDanhSachNguoiDung()`, `layChiTietNguoiDung(id)`, `luuNguoiDung(thongTin)`, `khoaTaiKhoan(id, trangThai)`.

3. **Chế độ Tunnel (Expo Go):**
   * Đã cài đặt `@expo/ngrok` trong `devDependencies`.
   * Tạo sẵn lệnh chạy nhanh trong `package.json`:
     ```bash
     npm run start:tunnel
     ```
   * Giúp quét mã QR từ điện thoại bằng ứng dụng Expo Go qua mạng 4G hoặc khác mạng Wi-Fi với máy tính.

4. **Các thư viện phần cứng & mật mã đã cài đặt:**
   * `expo-camera`: Quét mã QR xe buýt tốc độ cao.
   * `expo-screen-capture`: Bật cờ `FLAG_SECURE` ngăn chặn chụp/quay màn hình vé.
   * `expo-brightness`: Tự động tăng sáng màn hình khi xuất mã QR.
   * `expo-sqlite` & `expo-secure-store`: Bộ nhớ ngoại tuyến và lưu trữ khóa mật mã an toàn.
   * `@noble/curves` & `otplib`: Hỗ trợ xác thực chữ ký số ECDSA và sinh mã TOTP.

---

## 4. Hướng dẫn chạy thử nghiệm

### Khởi chạy Server (.NET 8 Web API):
```powershell
cd "d:\mon hoc\DoAnTN\Thinh\Project\Server"
dotnet run
```
* Swagger UI: Mở trình duyệt truy cập `http://localhost:5000/swagger` (hoặc cổng hiển thị trên console).

### Khởi chạy Client (React Native Expo):
```powershell
cd "d:\mon hoc\DoAnTN\Thinh\Project\Client"

# Chạy thông thường (mạng nội bộ LAN):
npm start

# Chạy qua Tunnel (bất kỳ mạng nào, 4G, điện thoại ngoài):
npm run start:tunnel
```
Sau đó mở ứng dụng **Expo Go** trên điện thoại và quét mã QR hiển thị trên màn hình terminal.
