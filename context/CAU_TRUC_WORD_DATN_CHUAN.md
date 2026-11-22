# Cấu trúc & Phong cách viết Báo cáo ĐATN SmartBus (Chuẩn HCMUNRE + Style ĐACN)

> Kết hợp quy chuẩn Khoa CNTT (`word/CauTrucWordDATN/Hướng dẫn trình bày Báo cáo ĐATN năm học 2026 - 2027.docx`) và phong cách trình bày, bố cục, văn phong thực tế của sinh viên từ `word/NguyenPhuocThinh_1150070040_DoAnChuyenNganh.pdf`.

## 1. Quy chuẩn hình thức (giữ đúng style ĐACN + chuẩn Khoa 2026–2027)
- **Khổ giấy**: A4, lề Trên `2.0 cm`, Dưới `2.0 cm`, Trái `3.0 cm`, Phải `2.0 cm`.
- **Font**: `Times New Roman`, nội dung `13pt`, giãn dòng `1.5 lines`; tiêu đề chương `14–16pt` in đậm in hoa.
- **Trang bìa & bìa trong**: Viền đôi, logo HCMUNRE ở giữa, `ĐỒ ÁN TỐT NGHIỆP — NGÀNH CÔNG NGHỆ THÔNG TIN`, tên đề tài khớp Đề cương đã duyệt, GVHD: `ThS. Từ Thanh Trí`, SVTH: `Nguyễn Phước Thịnh`, MSSV: `1150070040`, Lớp: `11_ĐH_TMĐT`, Khóa: `2022–2026`, `TP. Hồ Chí Minh, tháng 11 năm 2026`.
- **Header (từ Mở đầu trở đi)**: Tên đề tài (canh trái) | `ThS. Từ Thanh Trí` (canh phải) + đường kẻ mảnh.
- **Footer (từ Mở đầu trở đi)**: `Sinh viên thực hiện: Nguyễn Phước Thịnh` (canh trái) | `Trang X` (canh phải) + đường kẻ mảnh.
- **Sơ đồ UML**: Vẽ **đen – trắng (không màu)** chuẩn học thuật (giống bản ĐACN cũ); cuối mỗi hình/sơ đồ/bảng có chú thích in nghiêng + ghi nguồn.
- **Lưu ý khắc phục từ bản ĐACN cũ**: Không để trang trắng thừa; kiểm tra đánh số mục/hình liên tục; có **≥ 10 tài liệu tham khảo** (chuẩn IEEE, xếp cuối cùng sau Phụ lục).

## 2. Cấu trúc chi tiết từng phần

### Phần đầu *(không đánh số trang)*
1. Trang bìa & Trang bìa trong
2. LỜI CẢM ƠN *(cảm ơn Trường, Khoa CNTT, ThS. Từ Thanh Trí)*
3. LỜI CAM ĐOAN
4. NHẬN XÉT CỦA GIẢNG VIÊN HƯỚNG DẪN *(không ghi điểm)*
5. NHẬN XÉT CỦA GIẢNG VIÊN PHẢN BIỆN *(không ghi điểm)*
6. LỊCH LÀM VIỆC *(bảng 15 tuần theo Đề cương đã duyệt)*
7. TÓM TẮT ĐỒ ÁN *(≤ 1 trang A4: Bối cảnh – Mục tiêu & phạm vi – Phương pháp & công nghệ – Kết quả đạt được – Ý nghĩa & hướng phát triển)*
8. MỤC LỤC
9. DANH MỤC SƠ ĐỒ, HÌNH
10. DANH MỤC BẢNG
11. KÝ HIỆU CỤM TỪ VIẾT TẮT *(bảng 2 cột: API, CCCD, CSDL, ECDSA, HKDF, JWT, QR, SQLCipher, TOTP, TTL, UI/UX, UTC)*

### Nội dung chính *(bắt đầu đánh số Trang 1 từ Mở đầu)*
- **MỞ ĐẦU**: Bối cảnh chuyển đổi số giao thông công cộng TP.HCM & **Nghị quyết 09/2026/NQ-HĐND** (hỗ trợ 100% giá vé trên 134 tuyến từ 01/07–31/12/2026, bắt buộc xác thực lượt đi điện tử giai đoạn 2 từ 01/10/2026 để quyết toán trợ giá); mục tiêu, phương pháp, đối tượng/phạm vi và bố cục báo cáo.
- **CHƯƠNG 1. TỔNG QUAN TÀI LIỆU**
  - `1.1.` Tổng quan bài toán xác thực và quản lý lượt đi xe buýt công cộng
  - `1.2.` Tổng quan các giải pháp liên quan (vé giấy, QR tĩnh, thẻ từ NFC, ứng dụng giao thông hiện hành như MultiGo / MuniMobile)
  - `1.3.` Sự khác biệt của đề tài SmartBus & **So sánh với các hệ thống hiện có** *(3 bảng giống style ĐACN: Bảng 1.1 Tính năng chính, Bảng 1.2 UI/UX, Bảng 1.3 Kỹ thuật & Bảo mật + Nhận xét & Kết luận so sánh)*
  - `1.4.` Cơ sở lý thuyết *(Kiến trúc Client–Server & 3-layer; Chữ ký số bất đối xứng ECDSA P-256; Phái sinh khóa HKDF-SHA256 & mã động TOTP HMAC-SHA256 8 số 30s; Chống phát lại Anti-Replay & xác thực ngoại tuyến SQLCipher)*
  - `1.5.` Công nghệ và kỹ thuật sử dụng *(C# ASP.NET Core 8, EF Core, TypeScript React Native + Expo SDK 58, React + Vite, SQL Server, Redis Cache, SQLCipher, Cloudinary)*
- **CHƯƠNG 2. PHƯƠNG PHÁP THỰC HIỆN**
  - `2.1.` Phân tích yêu cầu hệ thống *(Yêu cầu chức năng theo 3 vai trò: Hành khách – Nhân viên soát vé – Quản trị viên; Yêu cầu phi chức năng: bảo mật, ngoại tuyến < 0,5s, đồng bộ chống trùng)*
  - `2.2.` Lựa chọn kiến trúc và công nghệ *(Lý do chọn ASP.NET Core 8, React Native/Expo, SQL Server, SQLCipher, Redis, Cloudinary)*
  - `2.3.` Thuật toán mật mã và các thách thức kỹ thuật *(Thiết kế payload `SMARTBUS:v1:{maVe}:{TOTP}:{ECDSA}`, phái sinh khóa HKDF-SHA256, dung sai đồng hồ ±1 bước 30s, xử lý tranh chấp vé khi 2 máy mất mạng cùng quét 1 vé lượt, `FLAG_SECURE`)*
- **CHƯƠNG 3. CÀI ĐẶT THỬ NGHIỆM, KẾT QUẢ VÀ ĐÁNH GIÁ**
  - `3.1.` Phân tích và thiết kế hệ thống *(sơ đồ đen–trắng chuẩn UML)*:
    - `3.1.1.` Sơ đồ Use Case tổng quát & Bảng đặc tả Use Case chính *(mua vé/phát hành vé ký số, quét QR động ngoại tuyến, đồng bộ & phát hiện quét trùng, tra cứu khẩn cấp CCCD)*
    - `3.1.2.` Sơ đồ Activity *(luồng mua vé & phát hành QR động; luồng soát vé ngoại tuyến & đồng bộ)*
    - `3.1.3.` Sơ đồ Sequence *(6 sơ đồ từ `word/SmartBus - Sequence QR va soat ve.drawio`)*
    - `3.1.4.` Sơ đồ lớp (Class Diagram) & Sơ đồ triển khai (Deployment Diagram)
    - `3.1.5.` Sơ đồ cơ sở dữ liệu (Database Diagram trích từ SSMS) & Lược đồ quan hệ `SmartBusDb`
  - `3.2.` Môi trường cài đặt và công cụ thực hiện *(Môi trường phần mềm, phần cứng, thiết bị Android thật Nubia A36 & máy ảo, công cụ phát triển)*
  - `3.3.` Cài đặt các thành phần chính *(SQL Server + 134 tuyến NQ 09/2026, ASP.NET Core API + Swagger, React Native + Expo + SQLCipher, Web Admin, Redis & Cloudinary)*
  - `3.4.` Quá trình thực hiện và các giải pháp áp dụng *(theo mẫu ĐACN: Mục tiêu → Giải pháp → Triển khai → Kết quả cho từng mảng: UI/UX, Backend & mật mã ECDSA/HKDF/TOTP, Soát vé ngoại tuyến SQLCipher, Đồng bộ chống quét trùng, Bảo mật JWT & `FLAG_SECURE`)*
  - `3.5.` Các khó khăn, thách thức và cách khắc phục
  - `3.6.` Kết quả đạt được & Minh họa giao diện thực tế *(Mobile Hành khách, Ví QR động, Quét QR dán trên xe, Mobile Nhân viên soát vé, Web Admin)*
  - `3.7.` Kiểm thử, đánh giá và phân tích kết quả *(Bảng kết quả kiểm thử tự động Backend/Mobile/Admin, kết quả thực nghiệm Android thật & số đo thời gian xác thực, đánh giá mục tiêu, hiệu năng, UX/UI và hạn chế)*
- **KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN** *(đủ 7 mục giống ĐACN: Đánh giá kết quả, Kiến thức lý thuyết, Kỹ năng thực hành, Kinh nghiệm rút ra, Hạn chế, Hướng phát triển, Kết luận)*
- **PHỤ LỤC** *(hướng dẫn cài đặt & chạy nhanh, trích đoạn mã nguồn trọng tâm)*
- **DANH MỤC TÀI LIỆU THAM KHẢO** *(≥ 10 tài liệu chuẩn IEEE, chia Tiếng Việt / Tiếng Anh, có Nghị quyết `09/2026/NQ-HĐND`)*
