# Thử chức năng soát vé ngoại tuyến

Ứng dụng yêu cầu Android/iOS development build có SQLCipher; Expo Go không hỗ trợ SQLCipher. Sau khi cài dependency, chạy `npx expo run:android` từ `Client` để tạo và cài bản Android, hoặc dùng EAS development build. Cần máy Android hoặc thiết bị giả lập có camera.

Chạy script `Server/Database/02_goi_soat_ve.sql` trên database dự án trước khi dùng tính năng; script tạo các bảng bổ sung nếu chưa có. Script này đã được áp dụng cho database local trong phiên làm việc này.

Chạy Backend với môi trường Development và cấu hình user-secrets. Đăng nhập mobile bằng tài khoản nhân viên soát vé, tải danh sách thiết bị và chuyến, chọn một thiết bị và chuyến đang chạy, sau đó chọn **Bắt đầu phiên và tải gói offline**. Khi gói được lưu thành công, có thể ngắt mạng, cấp quyền camera và quét QR của vé đã nằm trong gói. Mỗi lần quét phải bấm lại nút Quét vé để tránh camera ghi lặp.

Gói hết hạn sau 24 giờ; vé phát hành sau lần tải gói không có trong dữ liệu offline. Vé lượt đã ghi nhận trước khi tải gói hoặc đã chấp nhận trên máy sẽ bị chặn khi quét lại. Hai thiết bị cùng offline không biết lượt quét của nhau.

Khi có mạng, chọn **Đồng bộ lượt quét**. Server xác minh lại chữ ký, TOTP tại thời điểm quét, tuyến và phiên bản gói; gửi lại cùng sự kiện không tạo thêm lượt. Thiết bị lưu trạng thái xác nhận và báo số lượt bị từ chối hoặc cảnh báo thu hồi. Lượt ghi bằng phiên bản cũ chưa có mã gói được giữ lại để đối soát thủ công. API đồng bộ cũ đã bị vô hiệu hóa.

Chưa có khôi phục đăng nhập khi mở app lại không có mạng. Cần kiểm thử trên development build và thiết bị thật trước khi kết luận âm thanh, camera, SQLCipher, thời gian xác thực hoặc khả năng hoạt động thực tế. Hai thiết bị cùng offline chỉ phát hiện trùng khi đồng bộ; thứ tự ghi nhận do server quyết định.

Kiểm thử tích hợp SQL Server tạo hai thiết bị và hai gói riêng, gửi cùng một vé lượt đồng thời, kiểm tra chỉ một lượt được chấp nhận và lượt còn lại được cảnh báo trùng. Các yêu cầu đồng bộ ngoại tuyến được tuần tự hóa bằng khóa giao dịch SQL Server, phù hợp quy mô demo; cần đo tải riêng nếu triển khai nhiều thiết bị.
