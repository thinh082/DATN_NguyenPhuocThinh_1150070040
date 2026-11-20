# SmartBus Admin

Web quản trị chạy cục bộ bằng React, TypeScript và Vite. Cần Node.js tương thích phiên bản Vite trong `package.json` và Backend SmartBus đã cấu hình SQL Server và user-secrets.

Từ thư mục `Project`, chạy Backend:

```powershell
dotnet run --project Server --launch-profile http
```

Trong một terminal khác:

```powershell
cd Admin
npm install
npm run dev
```

Mở địa chỉ Vite in ra trong terminal, đăng nhập bằng tài khoản có vai trò `QuanTriVien`. Trong môi trường phát triển, Vite chuyển các yêu cầu `/api` tới `http://localhost:5056`; sửa `vite.config.ts` nếu Backend chạy cổng khác. Bản `npm run build` cần reverse proxy `/api` trỏ tới Backend khi triển khai. Không nhúng khóa JWT, khóa ký hoặc mật khẩu vào web.

Web có tổng quan, danh sách và chỉnh sửa tuyến/trạm/xe, tạo và quản lý trạng thái chuyến, xem và sửa giá, quản lý tài khoản, duyệt hồ sơ ưu tiên, xem/khóa vé, xem đơn hàng, nhật ký và xử lý cảnh báo. Doanh thu hiển thị là từ thanh toán mô phỏng. Các danh sách giao dịch lấy tối đa 200 bản gần nhất.

Chưa có giao diện sắp xếp trạm trong lộ trình, tạo lộ trình mới, mở khóa vé, xuất báo cáo hoặc sửa các hồ sơ đã xét duyệt. Các phần đó cần API và giao diện bổ sung. Kiểm thử thực tế trên dữ liệu mẫu trước khi dùng cho demo.
