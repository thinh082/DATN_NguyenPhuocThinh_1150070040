# Context đề cương đồ án tốt nghiệp

## 1. Thông tin chung

- Trường: Đại học Tài nguyên và Môi trường TP.HCM.
- Khoa: Công nghệ Thông tin.
- Sinh viên: Nguyễn Phước Thịnh.
- MSSV: 1150070040.
- Lớp: 11_ĐH_TMĐT.
- Khóa: 2022–2026.
- Giảng viên hướng dẫn: ThS. Từ Thanh Trí.
- Tên đề tài: **Xây dựng hệ thống xác thực và quản lý lượt đi xe buýt sử dụng mã QR động, chữ ký số và cơ chế xác thực ngoại tuyến**.
- Kế hoạch thực hiện: 08/2026–12/2026, chia thành 15 tuần.
- Nguồn: `DATN_DeCuong_NguyenPhuocThinh_1150070040.docx`.

## 2. Bối cảnh và vấn đề cần giải quyết

Hệ thống hướng đến hiện đại hóa hoạt động xe buýt công cộng trong bối cảnh TP.HCM áp dụng chính sách trợ giá sâu hoặc miễn phí vé. Trọng tâm chuyển từ thu tiền vé thương mại sang:

- Xác thực quyền đi xe của hành khách.
- Kiểm soát số lượt hành khách thực tế.
- Minh bạch dữ liệu phục vụ đối soát và thanh quyết toán trợ giá.

Các hạn chế của vé giấy và mã QR tĩnh:

- Không định danh rõ đối tượng ưu tiên như học sinh, sinh viên và hành khách lớn tuổi.
- Có thể chụp màn hình và chia sẻ vé cho nhiều hành khách.
- Có nguy cơ khai khống sản lượng hành khách để trục lợi trợ giá.
- Việc soát vé phụ thuộc vào kết nối mạng trong khi 4G/5G có thể không ổn định trên một số tuyến.

Yêu cầu quan trọng: thiết bị soát vé phải xác thực ngoại tuyến nhanh, mục tiêu đề cương nêu là dưới 0,5 giây, và bảo đảm an toàn dữ liệu.

## 3. Cơ sở khoa học

- **ECDSA:** Backend dùng khóa riêng để ký dữ liệu vé; thiết bị soát vé dùng khóa công khai để kiểm tra tính toàn vẹn và nguồn gốc, kể cả khi ngoại tuyến.
- **TOTP:** Tạo thành phần mã thay đổi theo chu kỳ giai đoạn nhằm hạn chế tái sử dụng ảnh chụp QR.
- **HMAC-SHA256 và kỹ thuật phái sinh khóa:** Hỗ trợ tạo và kiểm tra mã động mà không cần liên lạc liên tục với máy chủ.
- **Chống phát lại:** Kết hợp dữ liệu vé có chữ ký, mã thay đổi theo giai đoạn và cơ chế phát hiện lượt quét trùng lặp.
- **Đồng bộ ngoại tuyến:** Thiết bị lưu tạm kết quả soát vé khi mất mạng, đồng bộ và đối soát khi có kết nối trở lại.
- **Dữ liệu:** Cơ sở dữ liệu quan hệ bảo đảm giao dịch ACID; Redis hỗ trợ bộ nhớ đệm và kiểm soát truy cập đồng giai đoạn.

## 4. Mục tiêu

### Mục tiêu tổng quát

Thiết kế và xây dựng hệ sinh thái phần mềm xác thực quyền đi xe, quản lý lượt đi xe buýt, định danh hành khách, ghi nhận lượt đi minh bạch, hạn chế gian lận và hỗ trợ xác thực ngoại tuyến.

### Mục tiêu cụ thể

1. Nghiên cứu ECDSA, TOTP, kỹ thuật phái sinh khóa và giải pháp chống tấn công phát lại.
2. Khảo sát quy trình cấp quyền đi xe cho hành khách phổ thông, học sinh/sinh viên và hành khách lớn tuổi.
3. Thiết kế kiến trúc Client–Server, RESTful API và cơ sở dữ liệu quan hệ chuẩn hóa.
4. Xây dựng Backend, ứng dụng hành khách, ứng dụng soát vé và Web Admin Dashboard.
5. Kiểm thử chữ ký số, tốc độ tạo/quét QR động, xác thực khi mất mạng và khả năng chịu tải.

## 5. Tác nhân và chức năng chính

### Hành khách

- Đăng ký, đăng nhập và xác thực thông tin.
- Tìm kiếm tuyến và lộ trình.
- Mua vé lượt hoặc vé tháng.
- Quét mã QR cố định trên xe để nhận diện tuyến và mua vé nhanh.
- Quản lý vé điện tử.
- Hiển thị QR động khi lên xe.

### Nhân viên soát vé

- Quét QR và kiểm tra vé trực tuyến hoặc ngoại tuyến.
- Nhận phản hồi hợp lệ/không hợp lệ bằng âm thanh.
- Tra cứu khẩn cấp bằng CCCD hoặc số điện thoại khi điện thoại hành khách hết pin/gặp sự cố.
- Khóa vé và ghi nhận trường hợp nghi ngờ gian lận.
- Lưu kết quả cục bộ khi mất mạng và đồng bộ sau.

### Quản trị viên vận hành

- Quản lý tuyến xe, lộ trình, trạm dừng, xe và lịch trình.
- Quản lý tài khoản nhân viên và phân quyền.
- Theo dõi lượt đi và doanh thu theo ngày/tháng.
- Đối soát dữ liệu từ thiết bị ngoại tuyến.
- Xem cảnh báo quét trùng và xử lý vé/tài khoản nghi ngờ gian lận.

## 6. Phạm vi chức năng

1. Xác thực và quản lý tài khoản, phân quyền ba vai trò.
2. Quản lý tuyến xe, trạm dừng, lịch trình, giai đoạn hoạt động và giá vé.
3. Đặt vé lượt/vé tháng, đóng gói dữ liệu và ký số ECDSA từ Backend.
4. Mua vé nhanh bằng cách quét QR cố định trên xe.
5. Hiển thị Dynamic QR theo chu kỳ TOTP.
6. Chặn chụp/quay màn hình bằng `FLAG_SECURE` trên nền tảng hỗ trợ và tăng độ sáng khi mở vé.
7. Quét QR bằng camera, xác minh chữ ký, kiểm tra hạn dùng và phản hồi âm thanh.
8. Xử lý sự cố bằng tra cứu CCCD/số điện thoại.
9. Lưu vết, khóa vé/tài khoản và đối soát gian lận sau đồng bộ.
10. Dashboard giám sát lượt đi, doanh thu và cảnh báo quét trùng lặp.

## 7. Công nghệ dự kiến

- Backend: C#, ASP.NET Core Web API.
- ORM/truy cập dữ liệu: Entity Framework Core và/hoặc Dapper.
- Cơ sở dữ liệu: SQL Server.
- Cache: Redis.
- Web Admin: JavaScript, ReactJS.
- Mobile: TypeScript, React Native.
- Công cụ: Visual Studio, SQL Server Management Studio.
- Thanh toán: tích hợp cổng thanh toán thử nghiệm, xử lý Webhook/IPN.

## 8. Giới hạn đề tài

- Tập trung vào xe buýt đô thị với hai loại vé chính: vé lượt và vé tháng.
- Khi mất mạng kéo dài, ưu tiên lưu dữ liệu tạm trên thiết bị để hành khách tiếp tục lên xe.
- Việc đối soát, phát hiện xung đột và khóa tài khoản có thể thực hiện sau khi thiết bị có mạng trở lại.
- Chấp nhận rủi ro trong khoảng giai đoạn chưa đồng bộ; cần thiết kế nhật ký và quy trình xử lý minh bạch.

## 9. Kiểm thử và kết quả mong đợi

### Kiểm thử

- Kiểm thử chức năng các luồng mua vé, phát hành vé và soát vé.
- Kiểm tra chữ ký hợp lệ, vé giả mạo và vé quá hạn.
- Kiểm tra QR động, chống chụp màn hình và phát hiện quét trùng.
- Kiểm tra xác thực khi ngắt Internet hoàn toàn.
- Kiểm tra đồng bộ dữ liệu sau khi có mạng.
- Đo tốc độ tạo/quét QR, giai đoạn xác thực và khả năng chịu tải.

### Sản phẩm

- Ứng dụng di động cho hành khách.
- Ứng dụng soát vé cho nhân viên.
- Web điều hành cho quản trị viên.
- Backend RESTful API, SQL Server và Redis.

### Thực tiễn

- Giảm phụ thuộc vào vé giấy.
- Hỗ trợ kiểm soát lượt đi và đối soát trợ giá.
- Hạn chế chia sẻ vé và giả mạo vé.
- Duy trì hoạt động soát vé trong điều kiện mạng không ổn định.

## 10. Kế hoạch 15 tuần

1. Hoàn thiện tên đề tài, mục tiêu và phạm vi.
2. Thu thập tài liệu, nghiên cứu ECDSA/TOTP và khảo sát nghiệp vụ.
3. Phân tích yêu cầu chức năng, phi chức năng và nhóm vai trò.
4. Thiết kế kiến trúc, luồng bảo mật vé và cơ sở dữ liệu.
5. Thiết kế giao diện mobile hành khách, mobile soát vé và Web Admin.
6. Xây dựng Backend, xác thực tài khoản và kết nối dữ liệu.
7. Cài đặt quản lý tuyến, trạm, lịch trình và phân quyền.
8. Cài đặt ký số, mã vé động và bộ nhớ đệm.
9. Tích hợp thanh toán thử nghiệm và Webhook/IPN.
10. Xây dựng ứng dụng hành khách.
11. Xây dựng ứng dụng soát vé và xác thực ngoại tuyến.
12. Xây dựng Web Admin và báo cáo.
13. Kiểm thử toàn hệ thống, mất mạng và đồng bộ.
14. Phân tích kết quả, sửa lỗi, tối ưu và hoàn thiện báo cáo.
15. Hoàn tất báo cáo, slide và bản chạy thử để bảo vệ.

## 11. Bố cục báo cáo dự kiến

- Phần mở đầu.
- Chương 1: Tổng quan tài liệu.
  - Bài toán quản lý vé giao thông công cộng.
  - ECDSA.
  - TOTP và Dynamic QR Code.
  - Công nghệ phát triển hệ thống.
- Chương 2: Phương pháp thực hiện.
  - Phân tích yêu cầu.
  - Use Case.
  - Kiến trúc và luồng dữ liệu bảo mật.
  - Cơ sở dữ liệu.
  - Giao diện ứng dụng.
- Chương 3: Cài đặt thực nghiệm.
  - Môi trường triển khai.
  - Các phân hệ chức năng.
  - Kiểm thử bảo mật và hiệu năng ngoại tuyến.
- Kết luận.
- Tài liệu tham khảo.

## 12. Tài liệu tham khảo chính

1. D. M’Raihi và cộng sự, *TOTP: Time-Based One-Time Password Algorithm*, RFC 6238, IETF, 2011: https://datatracker.ietf.org/doc/html/rfc6238
2. NIST, *Digital Signature Standard (DSS)*, FIPS PUB 186-5, 2023: https://csrc.nist.gov/publications/detail/fips/186-5/final
3. Microsoft Learn, *ASP.NET Core documentation*: https://learn.microsoft.com/en-us/aspnet/core/
4. Microsoft Learn, *SQL Server Technical Documentation*: https://learn.microsoft.com/en-us/sql/sql-server/

## 13. Điểm cần bổ sung hoặc hiệu chỉnh trong đề cương

- Mục 6.2 ghi hệ sinh thái có ba thành phần nhưng chỉ liệt kê ứng dụng soát vé và Web điều hành; cần bổ sung ứng dụng di động dành cho hành khách.
- Cần làm rõ thiết kế mật mã cụ thể khi triển khai: dữ liệu nào được ký, thành phần nào thay đổi theo TOTP, cách thiết bị ngoại tuyến kiểm tra TOTP mà không lộ bí mật dùng chung, và cách phân phối/thu hồi khóa công khai.
- Cần định nghĩa chi tiết chiến lược chống quét trùng khi nhiều thiết bị cùng ngoại tuyến, định danh lượt quét, xử lý xung đột và thứ tự đồng bộ.
- Các mục tiêu như “dưới 0,5 giây”, “loại bỏ triệt để” cần được chuyển thành tiêu chí đo lường và kết luận phù hợp với kết quả thực nghiệm.
