# Đặc tả phần 1 - Nền tảng, bàn, mã QR, phiên phục vụ và phân quyền

Phụ trách: **Nguyễn Khánh Duy**

Sơ đồ liên quan: [Kiến trúc](../so-do/01-kien-truc.png), [Use case bàn/QR/phiên](../so-do/11-use-case-ban-qr-phien.png) ,
[Trạng thái phiên](../so-do/05-trang-thai-phien.png), [Tuần tự quét QR](../so-do/07-tuan-tu-quet-qr.png), [ERD](../so-do/04-erd.png)

Mã nguồn chính: `Models/*`, `Data/AppDbContext.cs`, `Data/DbSeeder.cs`, `Data/AppClaimsFactory.cs`, `Services/DiningSessionService.cs`,
`Services/TableService.cs`, `Services/DbExtensions.cs`, `Services/StaffActor.cs`, `Services/StaffNotifier.cs`, `Hubs/StaffHub.cs`,
`Controllers/TaiKhoanController.cs`, `Controllers/BanController.cs`, `Controllers/StaffBaseController.cs`, `Views/Shared/*`, `Views/Ban/*`, `Views/TaiKhoan/*`.

## 1. Kiến trúc

Một ứng dụng ASP.NET Core MVC (.NET 10). Controller nhận yêu cầu và tạo `StaffActor` từ người đang đăng nhập, Service kiểm tra quy tắc và chạy
trong giao dịch, EF Core lưu vào file SQLite `App_Data/qresto.db`. ASP.NET Core Identity quản lý tài khoản nhân viên với 3 vai trò
`PhucVu`, `Bep`, `QuanLy`. Sau khi lưu thành công, Service gọi `IStaffNotifier` để SignalR báo cho màn hình nhân viên tải lại.
Màn hình khách không dùng SignalR mà lấy trạng thái mỗi 5 giây.

## 2. Danh sách use case

| Mã | Use case | Tác nhân | Ghi chú |
| --- | --- | --- | --- |
| UC-B1 | Quét QR, vào thực đơn của bàn | Khách | Không tạo phiên. Bàn đang có phiên thì cấp quyền xem phiên cho trình duyệt. |
| UC-B2 | Đăng nhập / đăng xuất | Phục vụ, Bếp, Quản lý | Tài khoản bị khóa không đăng nhập được. Sai 5 lần khóa tạm 5 phút. |
| UC-B3 | Đổi mật khẩu | Nhân viên | Ít nhất 6 ký tự, có chữ số. |
| UC-B4 | Xem sơ đồ bàn và chi tiết phiên | Phục vụ, Quản lý | Bàn trống / đang phục vụ / chờ thanh toán, tạm tính, số món chờ. |
| UC-B5 | Cho bàn gọi thêm | Phục vụ, Quản lý | Chỉ từ Chờ thanh toán về Đang phục vụ, trước khi thu tiền. |
| UC-B6 | Hủy phiên không thu tiền | Phục vụ, Quản lý | Bắt buộc lý do. Mọi dòng phải Đã hủy hoặc Bị từ chối, không còn yêu cầu hủy hay sự cố. |
| UC-B7 | Quản lý bàn và mã QR | Quản lý | Thêm, sửa, in QR, đổi QR, ngừng dùng/dùng lại. |

## 3. Đặc tả UC-B1 - Quét QR, vào thực đơn

| Mục | Nội dung |
| --- | --- |
| Tác nhân | Khách (không đăng nhập) |
| Kích hoạt | Khách quét QR dán trên bàn, mở `/ban/{QrToken}` |
| Tiền điều kiện | Không có |
| Hậu điều kiện | Trang thực đơn hiển thị đúng bàn. Không có phiên nào được tạo. |

**Luồng chính**

1. Trình duyệt gửi `GET /ban/{token}`. Nếu chưa có cookie `qresto_khach` thì server tạo một `GuestKey` ngẫu nhiên (hạn 1 ngày).
2. `DiningSessionService.EnterTableAsync` tìm bàn theo `QrToken`.
3. Nếu bàn đang có phiên hoạt động, cấp `GuestSessionAccess` cho cặp (phiên, GuestKey) nếu chưa có. Người ngồi cùng bàn quét QR là xem được món chung của bàn.
4. Trang trả về thực đơn kèm `PageSessionId` (phiên đang hiển thị, có thể rỗng) và `PageTurn` (= `DiningTable.TurnVersion`).
5. Trang khách gọi `GET /ban/{token}/trang-thai` mỗi 5 giây để cập nhật món của bàn, tạm tính và tình trạng món.

**Luồng ngoại lệ**

- QR sai hoặc bàn đã ngừng sử dụng: hiện trang "Không mở được thực đơn", không nhận đơn.

## 4. Phiên phục vụ và quy tắc "lượt khách"

- Phiên được tạo **chỉ khi lưu thành công đơn đầu tiên**, trong cùng giao dịch với đơn (xem đặc tả phần 2). Lưu lỗi thì không có phiên rỗng.
- Mỗi bàn tối đa một phiên ở trạng thái `DangPhucVu` hoặc `ChoThanhToan`. Ràng buộc nằm ở cơ sở dữ liệu:
  `UNIQUE(DiningTableId) WHERE Status IN ('DangPhucVu','ChoThanhToan')` (`IX_DiningSessions_OnePerTable`).
- Khi phiên **đóng** (thu tiền) hoặc **hủy**, `EndSessionAsync` sẽ:
  - ghi `RevokedAt` cho mọi quyền khách của phiên, trang cũ không thao tác được nữa;
  - đóng các yêu cầu hỗ trợ còn mở;
  - tăng `DiningTable.TurnVersion` thêm 1.
- `TurnVersion` dùng để chặn trường hợp: trang mở lúc bàn trống của lượt trước, lượt đó đã thanh toán, khách mới vừa tạo phiên mới,
  trang cũ không được gửi đơn vào phiên mới. Trang cũ có `PageTurn` nhỏ hơn `TurnVersion` nên bị từ chối `StaleSession`.

## 5. Phân quyền

| Thao tác | Khách | Phục vụ | Bếp | Quản lý |
| --- | --- | --- | --- | --- |
| Quét QR, xem thực đơn, gửi đơn | Có | - | - | - |
| Đăng nhập, đổi mật khẩu | - | Có | Có | Có |
| Màn hình phục vụ, chi tiết phiên, thu tiền | - | Có | - | Có |
| Màn hình bếp, đổi tình trạng còn/hết món | - | - | Có | Có |
| Quản lý thực đơn, bàn, QR, nhân viên, báo cáo | - | - | - | Có |

Kiểm tra quyền ở hai lớp: `[Authorize(Roles = ...)]` trên controller và kiểm tra lại trong Service qua `StaffActor`
(`IsWaiter`, `IsKitchen`, `IsManager`). Quản lý được coi là vừa phục vụ vừa bếp. Khi khóa tài khoản hoặc đổi vai trò, hệ thống đổi
security stamp nên phiên đăng nhập cũ hết hiệu lực trong khoảng 1 phút.

## 6. Bàn và mã QR

- `QrToken` là chuỗi 12 ký tự ngẫu nhiên, duy nhất. Link trong QR: `{PublicBaseUrl}/ban/{QrToken}`.
- Nếu chưa cấu hình `Qresto:PublicBaseUrl` thì lấy theo địa chỉ đang truy cập. Mở web bằng `localhost` thì màn hình quản lý cảnh báo
  vì điện thoại không mở được QR trỏ tới localhost.
- Đổi QR và ngừng sử dụng bàn chỉ làm được khi bàn không có phiên hoạt động.

## 7. Dữ liệu mẫu (`DbSeeder`)

- Tài khoản (mật khẩu `qresto123`): `quanly`, `phucvu1`, `phucvu2`, `bep1`.
- 10 bàn `B01`-`B10`, 5 danh mục, 18 món.
