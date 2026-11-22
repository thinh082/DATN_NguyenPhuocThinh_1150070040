-- 07. Chuyen du lieu demo khong dau sang tieng Viet co dau (chi UPDATE theo dung gia tri cu, chay lai khong anh huong).
-- Chay: sqlcmd -S localhost -E -C -b -I -f i:65001 -d SmartBusDb -i "Server/Database/07_du_lieu_demo_co_dau.sql"
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;
UPDATE dbo.NguoiDung SET hoTen = N'Quản trị viên hệ thống' WHERE hoTen COLLATE Latin1_General_BIN = N'Quan Tri Vien He Thong';
UPDATE dbo.NguoiDung SET hoTen = N'Nhân viên soát vé 01' WHERE hoTen COLLATE Latin1_General_BIN = N'Nhan Vien Soat Ve 01';
UPDATE dbo.NguoiDung SET hoTen = N'Nguyễn Phước Thịnh' WHERE hoTen COLLATE Latin1_General_BIN = N'Nguyen Phuoc Thinh';
UPDATE dbo.ThietBiSoatVe SET tenThietBi = N'Máy soát vé xe 51B-123.45' WHERE tenThietBi COLLATE Latin1_General_BIN = N'May Soat Ve Xe 51B-123.45';
UPDATE dbo.TramDung SET tenTram = N'Trạm trung chuyển Bến Thành' WHERE tenTram COLLATE Latin1_General_BIN = N'Tram Trung Chuyen Ben Thanh';
UPDATE dbo.TramDung SET tenTram = N'Trường ĐH Tài nguyên và Môi trường' WHERE tenTram COLLATE Latin1_General_BIN = N'Truong DH Tai Nguyen va Moi Truong';
UPDATE dbo.TramDung SET tenTram = N'Bến xe Chợ Lớn' WHERE tenTram COLLATE Latin1_General_BIN = N'Ben Xe Cho Lon';
UPDATE dbo.TramDung SET tenTram = N'Bến xe Tân Vạn' WHERE tenTram COLLATE Latin1_General_BIN = N'Ben Xe Tan Van';
UPDATE dbo.TramDung SET diaChi = N'Đường Hàm Nghi, Quận 1, TP.HCM' WHERE diaChi COLLATE Latin1_General_BIN = N'Duong Ham Nghi, Quan 1, TP.HCM';
UPDATE dbo.TramDung SET diaChi = N'236 Lê Văn Sỹ, Quận Tân Bình, TP.HCM' WHERE diaChi COLLATE Latin1_General_BIN = N'236 Le Van Sy, Quan Tan Binh, TP.HCM';
UPDATE dbo.TramDung SET diaChi = N'Lê Quang Sung, Quận 5, TP.HCM' WHERE diaChi COLLATE Latin1_General_BIN = N'Le Quang Sung, Quan 5, TP.HCM';
UPDATE dbo.TramDung SET diaChi = N'Xã Bình Thắng, Dĩ An, Bình Dương' WHERE diaChi COLLATE Latin1_General_BIN = N'Xa Binh Thang, Di An, Binh Duong';
UPDATE dbo.LoTrinh SET tenLoTrinh = N'Bến Thành đi Chợ Lớn' WHERE tenLoTrinh COLLATE Latin1_General_BIN = N'Ben Thanh di Cho Lon';
UPDATE dbo.LoTrinh SET tenLoTrinh = N'Chợ Lớn đi Bến Thành' WHERE tenLoTrinh COLLATE Latin1_General_BIN = N'Cho Lon di Ben Thanh';
UPDATE dbo.LoTrinh SET tenLoTrinh = N'Chợ Lớn đi Tân Vạn' WHERE tenLoTrinh COLLATE Latin1_General_BIN = N'Cho Lon di Tan Van';
UPDATE dbo.LoTrinh SET tenLoTrinh = N'Tân Vạn đi Chợ Lớn' WHERE tenLoTrinh COLLATE Latin1_General_BIN = N'Tan Van di Cho Lon';
UPDATE dbo.TuyenXe SET tenTuyen = N'Bến Thành - Bến xe Chợ Lớn' WHERE tenTuyen COLLATE Latin1_General_BIN = N'Ben Thanh - BX Cho Lon';
UPDATE dbo.TuyenXe SET tenTuyen = N'Bến xe Chợ Lớn - Tân Vạn' WHERE tenTuyen COLLATE Latin1_General_BIN = N'BX Cho Lon - Tan Van';
UPDATE dbo.GiaVe SET canCuChinhSach = N'Vé lượt phổ thông toàn mạng' WHERE canCuChinhSach COLLATE Latin1_General_BIN = N'Ve luot pho thong toan mang';
UPDATE dbo.GiaVe SET canCuChinhSach = N'Trợ giá 4.000đ cho học sinh, sinh viên' WHERE canCuChinhSach COLLATE Latin1_General_BIN = N'Tro gia 4.000d cho hoc sinh sinh vien';
UPDATE dbo.GiaVe SET canCuChinhSach = N'Miễn 100% cho người cao tuổi từ 60 tuổi trở lên' WHERE canCuChinhSach COLLATE Latin1_General_BIN = N'Mien 100% cho nguoi cao tuoi tren 60 tuoi';
UPDATE dbo.GiaVe SET canCuChinhSach = N'Vé tháng toàn mạng lưới' WHERE canCuChinhSach COLLATE Latin1_General_BIN = N'Ve thang toan mang luoi';
UPDATE dbo.GiaVe SET canCuChinhSach = N'Vé tháng 1 tuyến (Tuyến 01)' WHERE canCuChinhSach COLLATE Latin1_General_BIN = N'Ve thang 1 tuyen (Tuyen 01)';
UPDATE dbo.GiaVe SET canCuChinhSach = N'Vé tháng 1 tuyến (Tuyến 150)' WHERE canCuChinhSach COLLATE Latin1_General_BIN = N'Ve thang 1 tuyen (Tuyen 150)';
UPDATE dbo.GiaVe SET canCuChinhSach = N'Dữ liệu demo kiểm thử QR trực tuyến - tuyến 01' WHERE canCuChinhSach COLLATE Latin1_General_BIN = N'Demo test QR Online - route 01';
UPDATE dbo.CanhBaoGianLan SET noiDung = N'Phát hiện lượt quét trùng khi đồng bộ.' WHERE noiDung COLLATE Latin1_General_BIN = N'Phat hien luot quet trung khi dong bo.';
UPDATE dbo.CanhBaoGianLan SET noiDung = N'Vé bị thu hồi sau khi tải gói ngoại tuyến.' WHERE noiDung COLLATE Latin1_General_BIN = N'Ve bi thu hoi sau khi tai goi ngoai tuyen.';
COMMIT;
GO
