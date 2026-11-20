# Bàn giao SmartBus

## Sản phẩm và phạm vi

- `Server/` là ASP.NET Core 8 với Entity Framework Core, SQL Server và Redis tùy chọn.
- `Client/` là Expo SDK 58 và React Native. App phân luồng hành khách và nhân viên. Máy nhân viên dùng SQLite với SQLCipher cho gói soát vé và lượt quét chờ đồng bộ.
- `Admin/` là React và Vite. `Server.Tests/` và `Client/tests/` chứa các bài kiểm thử. `tools/SmartBus.KeySetup/` chuẩn bị khóa local.
- Thanh toán chỉ mô phỏng thành công, thất bại và hủy. QR cố định dán trên xe dùng nhận diện phương tiện. QR trong ví có mã vé, TOTP SHA-256 8 số theo chu kỳ 30 giây và chữ ký ECDSA P-256.
- Màn camera nhân viên xử lý cục bộ, dù backend có endpoint xác thực trực tuyến riêng. Khi hai máy mất mạng cùng quét một vé lượt, máy chủ phát hiện trùng sau khi đồng bộ.

## Bắt đầu trên máy mới

1. Dùng [README](../README.md) để dựng SQL Server demo, khóa local và khởi động ba thành phần. Đổi IP API theo máy mới.
2. Đối chiếu yêu cầu với [context đề cương](de_cuong_datn_context.md). Tài liệu [đặc tả bổ sung](spec/SMARTBUS_SPEC_CHUC_NANG_CON_THIEU.md) mô tả cả hiện trạng cũ và mục tiêu. Kiểm tra lại source trước khi kết luận thiếu.
3. Chạy thử mua vé mô phỏng, ví QR, soát ngoại tuyến và đồng bộ. Trên bản sao bàn giao đã kiểm tra build backend, 4 test backend về mật mã và xác thực QR ngoại tuyến, cài dependencies mobile/admin từ lockfile, typecheck mobile, test thuật toán offline và build admin. Báo cáo nghiệm thu cũ ghi 37 test backend pass. Chạy lại bộ test đầy đủ trên database demo của máy mới.
4. Lấy bằng chứng Android thật cho SQLCipher, camera, âm báo, chặn chụp màn hình, đổi mạng và quét cùng vé lượt trên hai máy. Đo thời gian xác thực và ghi mẫu đo, p95 trước khi kết luận về mốc 0,5 giây. Cổng mặc định mobile và backend khác nhau, dùng URL theo README.

## Báo cáo và việc còn mở

- Trong gói chuyển máy, `../word/` tính từ thư mục repo chứa bản Word 23 trang, file draw.io sáu tab và tài liệu của khoa. Dùng `SmartBus - Khung bao cao DATN.docx` làm bản viết tiếp.
- Chương 2 cần đưa hình sơ đồ vào Word và hoàn thiện Use Case, kiến trúc, Activity, ERD, Class. Sơ đồ Sequence đã có file riêng. Chương 3 cần ảnh giao diện và số liệu thiết bị thật.
- Bản PDF đề cương có tên đề tài khác tên trên khung Word. Sinh viên cần xác nhận bản đã duyệt và thống nhất tên trước khi xuất báo cáo nộp.
- Dashboard và CSV còn khác cách áp dụng bộ lọc. Mã TOTP hiện dùng bí mật ngẫu nhiên theo vé. Đối chiếu với đề cương nếu bản duyệt yêu cầu dẫn xuất khóa.
- Bộ nộp theo thông báo gồm PDF đề cương đã duyệt, báo cáo DOCX và PDF, slide, video thuyết trình và demo tối đa 15 phút, link GitHub có quyền truy cập. Hạn nộp được cung cấp là trước 23h00 ngày 22/11/2026.

