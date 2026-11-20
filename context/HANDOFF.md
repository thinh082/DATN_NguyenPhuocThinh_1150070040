# Bàn giao SmartBus

## Sản phẩm và phạm vi

- `Server/` là ASP.NET Core 8 với Entity Framework Core, SQL Server và Redis tùy chọn.
- `Client/` là Expo SDK 58 và React Native. App phân luồng hành khách và nhân viên. Máy nhân viên dùng SQLite với SQLCipher cho gói soát vé và lượt quét chờ đồng bộ.
- `Admin/` là React và Vite. `Server.Tests/` và `Client/tests/` chứa các bài kiểm thử. `tools/SmartBus.KeySetup/` chuẩn bị khóa local.
- Thanh toán chỉ mô phỏng thành công, thất bại và hủy. QR cố định dán trên xe dùng nhận diện phương tiện. QR trong ví có mã vé, TOTP SHA-256 8 số theo chu kỳ 30 giây và chữ ký ECDSA P-256.
- Màn camera nhân viên xử lý cục bộ, dù backend có endpoint xác thực trực tuyến riêng. Khi hai máy mất mạng cùng quét một vé lượt, máy chủ phát hiện trùng sau khi đồng bộ.

## Bắt đầu trên máy mới

1. Dùng [README](../README.md) để dựng SQL Server demo, khóa local và khởi động ba thành phần. Đổi IP API theo máy mới.
2. Đối chiếu yêu cầu với [đề cương chi tiết bản Word](../word/DATN_DeCuong_NguyenPhuocThinh_1150070040.docx) và [context tóm tắt đề cương](de_cuong_datn_context.md). Tài liệu [đặc tả bổ sung](spec/SMARTBUS_SPEC_CHUC_NANG_CON_THIEU.md) mô tả cả hiện trạng cũ và mục tiêu. Kiểm tra lại source trước khi kết luận thiếu.
3. Chạy thử mua vé mô phỏng, ví QR, soát ngoại tuyến và đồng bộ. Trên bản sao bàn giao đã kiểm tra build backend, 4 test backend về mật mã và xác thực QR ngoại tuyến, cài dependencies mobile/admin từ lockfile, typecheck mobile, test thuật toán offline và build admin. Báo cáo nghiệm thu cũ ghi 37 test backend pass. Chạy lại bộ test đầy đủ trên database demo của máy mới.
4. Lấy bằng chứng Android thật cho SQLCipher, camera, âm báo, chặn chụp màn hình, đổi mạng và quét cùng vé lượt trên hai máy. Đo thời gian xác thực và ghi mẫu đo, p95 trước khi kết luận về mốc 0,5 giây. Cổng mặc định mobile và backend khác nhau, dùng URL theo README.

## Báo cáo và việc còn mở

- Trong repo, mở [báo cáo Word 23 trang](../word/SmartBus%20-%20Khung%20bao%20cao%20DATN.docx) để viết tiếp và [file draw.io sáu tab](../word/SmartBus%20-%20Sequence%20QR%20va%20soat%20ve.drawio) để chỉnh sửa các sơ đồ Sequence. Tài liệu của khoa và bản PDF đề cương đang lưu nằm trong thư mục `word/` bên ngoài repo của gói chuyển máy.
- Mỗi tab draw.io có ghi chú mã nguồn và tài liệu liên quan ở cuối. Cách trình bày Sequence tham khảo giáo trình Trần Đình Quế [13]. Khi chèn hình vào Word, có thể ghi “Nguồn: Tác giả xây dựng từ mã nguồn SmartBus” và nêu tài liệu thuật toán liên quan. Số trích dẫn theo danh mục trong bản Word hiện tại.
- Chương 2 cần đưa hình sơ đồ vào Word và hoàn thiện Use Case, kiến trúc, Activity, ERD, Class. Sơ đồ Sequence đã có file riêng. Chương 3 cần ảnh giao diện và số liệu thiết bị thật.
- Bản PDF đề cương có tên đề tài khác tên trên khung Word. Sinh viên cần xác nhận bản đã duyệt và thống nhất tên trước khi xuất báo cáo nộp.
- Đã bổ sung phái sinh khóa **HKDF-SHA256** cho bí mật TOTP (`MatMaService.PhaiSinhBiMatTotp`), lưu tệp minh chứng qua **Cloudinary** (tùy chọn, fallback đĩa local), script nạp **134 tuyến xe buýt TP.HCM** theo Nghị quyết `09/2026/NQ-HĐND` (`Server/Database/04_seed_134_tuyen_NQ09_2026.sql`) và nâng cấp giao diện Mobile + Web Admin đồng bộ tone Xanh dương – Xám – Trắng. Dashboard và CSV vẫn còn khác cách áp dụng bộ lọc (tùy chọn sửa thêm nếu cần).
- Bộ nộp theo thông báo gồm 01 file `.zip` (PDF đề cương đã duyệt, báo cáo DOCX và PDF, slide PPTX/PDF, video thuyết trình & demo tối đa 15 phút, link GitHub có quyền truy cập), gửi `dklien@hcmunre.edu.vn` (Cc `tuthanhtri@hcmunre.edu.vn`) trước 23h00 ngày 22/11/2026. Xem chi tiết checklist tại [yêu cầu nộp đồ án](YEU_CAU_NOP_DO_AN.md).

