# Qresto - Website gọi món tại bàn bằng mã QR

Đồ án thực tế Công nghệ phần mềm - **Nhóm 8**

Khách quét mã QR trên bàn để xem thực đơn và gọi món ngay trên điện thoại, không cần cài app hay đăng ký.
Phục vụ xác nhận món, bếp chế biến, phục vụ giao món, nhân viên thu tiền và hệ thống lập hóa đơn.

| Thành viên | Phần phụ trách |
| --- | --- |
| Nguyễn Khánh Duy | Nền tảng, bàn và mã QR, phiên phục vụ, đăng nhập và phân quyền |
| Nguyễn Tùng Duy | Thực đơn, giỏ hàng, gửi đơn và gọi bổ sung |
| Võ Hữu Duy | Màn hình phục vụ và bếp, hủy món, sự cố, hỗ trợ tại bàn |
| Chương Thị Ngọc Duyên | Thanh toán, hóa đơn, báo cáo doanh thu, tài khoản nhân viên |

## Công nghệ

ASP.NET Core MVC (.NET 10, C#), Razor + Bootstrap 5, Entity Framework Core + SQLite, ASP.NET Core Identity, SignalR, xUnit

## Chạy thử

Yêu cầu: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/Qresto
```

Mở địa chỉ in ra trên màn hình (mặc định http://localhost:5037). Lần chạy đầu tự tạo file CSDL `src/Qresto/App_Data/qresto.db` và dữ liệu mẫu.
Muốn làm lại dữ liệu từ đầu thì tắt web, xóa thư mục `App_Data` rồi chạy lại.

Ảnh món mẫu nằm ở `src/Qresto/wwwroot/images/mon/`, tên file là tên món viết không dấu (ví dụ `goi-cuon-tom-thit.jpg`).
Dữ liệu mẫu tự gắn ảnh khi tạo CSDL lần đầu. Ảnh quản lý tải lên sau này lưu ở `wwwroot/uploads/` (không đưa lên git).
Ảnh mẫu chỉ dùng minh họa cho đồ án học tập.

Tài khoản mẫu (mật khẩu `qresto123`):

| Tài khoản | Vai trò |
| --- | --- |
| `quanly` | Quản lý |
| `phucvu1`, `phucvu2` | Phục vụ |
| `bep1` | Bếp |

### Thử bằng điện thoại trong cùng Wi-Fi

1. Chạy với profile `lan` để web nhận kết nối từ máy khác:
   ```bash
   dotnet run --project src/Qresto --launch-profile lan
   ```
2. Xem IP của máy (Windows: `ipconfig`, macOS/Linux: `ifconfig`), ví dụ `192.168.1.5`.
3. Trên máy tính, mở `http://192.168.1.5:5037`, đăng nhập `quanly` -> **Quản lý -> Bàn và mã QR -> In QR**. QR sẽ trỏ tới IP này.
   Có thể cố định địa chỉ trong QR bằng `Qresto:PublicBaseUrl` trong `src/Qresto/appsettings.json`.
4. Dùng điện thoại quét QR. Nếu không vào được, kiểm tra tường lửa Windows đã cho phép cổng 5037.

QR trỏ tới `localhost` hoặc IP nội bộ thì chỉ dùng được trong mạng đó. Muốn người ở mạng khác dùng thì cần đưa web lên một địa chỉ công khai
và in lại QR theo địa chỉ đó.

### Chạy test

```bash
dotnet test
```

## Luồng demo nhanh

1. Mở 3 cửa sổ: điện thoại (hoặc Chrome F12 -> chế độ thiết bị di động) mở link QR của **Bàn 03**; một cửa sổ đăng nhập `phucvu1`; một cửa sổ ẩn danh đăng nhập `bep1`.
2. Khách thêm món, ghi chú, **Gửi đơn** -> màn hình phục vụ hiện món mới.
3. Phục vụ **Xác nhận** -> bếp **Bắt đầu làm** -> **Xong** -> phục vụ **Đã giao món**.
4. Khách bấm **Yêu cầu thanh toán** -> phục vụ vào **Tính tiền** -> **Xác nhận đã thu tiền** -> hóa đơn.
5. Quét lại QR Bàn 03: bàn đã sẵn sàng cho lượt khách mới.

## Cấu trúc thư mục

```
src/Qresto/
  Models/        lớp dữ liệu và enum trạng thái
  Data/          AppDbContext (ràng buộc, index), dữ liệu mẫu
  Services/      quy tắc nghiệp vụ: gửi đơn, phiên, xử lý món, thanh toán, báo cáo
  Controllers/   nhận thao tác, kiểm tra quyền, gọi service
  Views/         giao diện khách, phục vụ, bếp, quản lý
  Hubs/          SignalR cho màn hình nhân viên
  wwwroot/       css, js (goimon.js cho khách, staff.js cho nhân viên)
tests/Qresto.Tests/   test nghiệp vụ chạy trên SQLite thật
docs/                 nghiệp vụ, đặc tả từng phần, sơ đồ, kiểm thử
```

## Migration

Project tự tạo bảng bằng `EnsureCreated` khi chưa có migration. Khi cần migration:

```bash
dotnet tool install --global dotnet-ef
dotnet add src/Qresto package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet ef migrations add KhoiTao --project src/Qresto --output-dir Data/Migrations
```

Sau khi có migration, lúc khởi động project sẽ dùng `Migrate()`. Nhớ xóa `App_Data/qresto.db` cũ được tạo bằng `EnsureCreated` trước khi chạy lại.

## Ngoài phạm vi

Tồn kho, đặt bàn trước, giao hàng, chuyển/gộp bàn, chia hóa đơn, hoàn tiền, cổng thanh toán online.
Hệ thống chỉ ghi nhận nhân viên đã thu tiền mặt hoặc đã kiểm tra nhận chuyển khoản.
