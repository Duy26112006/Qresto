# Đặc tả phần 2 - Thực đơn, giỏ hàng và gửi đơn

Phụ trách: **Nguyễn Tùng Duy**

Sơ đồ liên quan: [Use case thực đơn và gọi món](../so-do/12-use-case-thuc-don-goi-mon.png), [Tuần tự gửi đơn](../so-do/08-tuan-tu-gui-don.png) ,
[Lược đồ lớp](../so-do/03-lop-tong-the.png)

Mã nguồn chính: `Services/OrderService.cs`, `Services/MenuService.cs`, `Services/GuestStatusService.cs`, `Controllers/GoiMonController.cs`,
`Controllers/DanhMucController.cs`, `Controllers/MonAnController.cs`, `Views/GoiMon/*`, `Views/DanhMuc/*`, `Views/MonAn/*`,
`wwwroot/js/goimon.js`, `wwwroot/css/khach.css`, `ViewModels/SubmitOrderRequest.cs`, `ViewModels/GuestStatusDto.cs`.

## 1. Danh sách use case

| Mã | Tên use case | Tác nhân |
| --- | --- | --- |
| UC01 | Xem thực đơn theo bàn | Khách |
| UC02 | Tìm kiếm món (không cần gõ dấu), lọc theo danh mục | Khách |
| UC05 | Quản lý giỏ hàng (thêm, sửa số lượng, xóa, ghi chú) | Khách |
| UC06 | Gửi đơn gọi món | Khách |
| UC07 | Gọi bổ sung | Khách |
| UC08 | Theo dõi món đã gọi và trạng thái món của bàn | Khách |
| UC10 | Quản lý danh mục | Quản lý |
| UC11 | Quản lý món ăn (giá, ảnh, mô tả, thời gian chế biến) | Quản lý |
| UC12 | Đổi tình trạng món (Đang bán / Tạm hết / Ngừng bán) | Bếp (Đang bán, Tạm hết), Quản lý (thêm Ngừng bán) |

## 1b. Ảnh món

- Quản lý tải ảnh khi thêm/sửa món: chỉ nhận `.jpg`, `.png`, `.webp`, tối đa 2MB, lưu vào `wwwroot/uploads/mon/` với tên ngẫu nhiên.
- Dữ liệu mẫu lấy ảnh có sẵn trong `wwwroot/images/mon/{ten-mon-khong-dau}.jpg` (`DbSeeder.FindImage`).
- Món chưa có ảnh thì thẻ món hiện ô màu với chữ cái đầu của tên món.

## 2. Giỏ hàng trên trình duyệt

- Giỏ lưu trong `localStorage` với khóa `qresto_gio_{token}`, mỗi trình duyệt một giỏ. Giỏ **không** có bảng trong CSDL.
- Bấm **Thêm +** lần đầu thì thẻ món đổi thành **− 1 +**. Giảm về 0 thì hiện lại **Thêm +**.
- Khi giỏ có món, thanh **Xem giỏ (n), tổng tiền** ghim ở đáy màn hình. `n` là tổng số phần.
- Giỏ mở từ dưới lên (offcanvas), sửa số lượng và ghi chú ngay trong giỏ. Đóng giỏ không làm trang cuộn lên đầu.
- Giới hạn phía trình duyệt: mỗi món 1-50 phần, tối đa 50 dòng, ghi chú 300 ký tự. Server vẫn kiểm tra lại.

## 3. Đặc tả UC06 - Gửi đơn gọi món

| Mục | Nội dung |
| --- | --- |
| Tác nhân chính | Khách (không đăng nhập) |
| Thành phần phối hợp | `DiningSessionService` (lấy phiên, cấp quyền), `IStaffNotifier` (báo phục vụ) |
| Kích hoạt | Khách bấm **Gửi đơn** trong giỏ |
| Tiền điều kiện | Trang thuộc một bàn hợp lệ, giỏ có ít nhất 1 dòng, bàn chưa có phiên hoặc phiên đang Đang phục vụ |
| Hậu điều kiện thành công | Có đúng 1 đơn mới gắn đúng phiên và đúng trình duyệt. Các dòng ở Chờ xác nhận, lưu tên và đơn giá tại lúc gửi. Trình duyệt có quyền với phiên. Giỏ được làm trống. Phục vụ nhận thông báo. |
| Hậu điều kiện thất bại | Không lưu đơn, không để lại phiên rỗng, giỏ giữ nguyên. |

**Dữ liệu gửi lên** (`POST /ban/{token}/gui-don`, JSON, có antiforgery token trong header):
`ClientRequestId`, `PageSessionId` (null nếu lúc tải trang bàn chưa có phiên), `PageTurn`, `Items[] {MenuItemId, Quantity, Note, Price}`.
`Price` chỉ để so sánh, server không dùng giá này để lưu.

**Luồng chính**

| Bước | Thực hiện | Hành động |
| --- | --- | --- |
| 1 | Khách | Bấm Gửi đơn. |
| 2 | Trình duyệt | Nếu nội dung giỏ giống lần gửi trước (bị mất phản hồi) thì dùng lại `ClientRequestId` cũ, khác thì sinh mã mới. Khóa nút trong lúc gửi. |
| 3 | Server | Kiểm tra dữ liệu: 1-50 dòng, số lượng 1-50, ghi chú <= 300 ký tự. |
| 4 | Server | Mở giao dịch (SQLite `BEGIN IMMEDIATE`, các yêu cầu ghi chạy lần lượt). |
| 5 | Server | Tìm đơn có cùng `ClientRequestId`. Nếu có: cùng trình duyệt và cùng nội dung thì trả lại đơn cũ (Duplicate), khác thì từ chối. |
| 6 | Server | Đối chiếu trang với phiên hiện tại của bàn theo bảng ở mục 4. |
| 7 | Server | Đọc món từ CSDL: phải Đang bán, danh mục đang hiện, giá bằng giá khách thấy. |
| 8 | Server | Bàn chưa có phiên thì tạo phiên Đang phục vụ. Cấp quyền cho trình duyệt nếu chưa có. |
| 9 | Server | Tạo `Order` và các `OrderItem` ở Chờ xác nhận, lưu `ItemName`, `UnitPrice` từ server. Tăng `PaymentVersion`. Ghi lịch sử. |
| 10 | Server | Commit, sau đó báo "đơn mới" qua SignalR. |
| 11 | Trình duyệt | Xóa giỏ, cập nhật `PageSessionId`, báo gửi thành công. |

**Luồng ngoại lệ** (server trả `{ ok:false, code, message, data }`)

| Mã | Điều kiện | HTTP | Trình duyệt xử lý |
| --- | --- | --- | --- |
| InvalidInput | Giỏ trống, số lượng/ghi chú sai | 400 | Báo lỗi, giữ giỏ |
| TableNotFound | QR sai, bàn ngừng dùng | 404 | Báo lỗi |
| StaleSession | Trang thuộc lượt khách đã kết thúc | 409 | Khóa gửi, hướng dẫn quét lại QR |
| SessionClosed | Bàn đang Chờ thanh toán | 409 | Báo tạm dừng gọi món, giữ giỏ |
| NoAccess | Đúng phiên nhưng trình duyệt mất quyền (xóa cookie) | 403 | Nhờ nhân viên hỗ trợ |
| ItemUnavailable | Có món vừa hết hoặc ngừng bán | 409 | Tô đỏ món đó trong giỏ |
| PriceChanged | Giá vừa đổi | 409 | Cập nhật giá mới trong giỏ, khách xem lại rồi gửi lại (mã gửi mới) |
| DuplicateMismatch | Cùng mã gửi nhưng nội dung khác | 409 | Báo kiểm tra lại giỏ |
| (lỗi mạng) | Mất kết nối | - | Giữ giỏ và mã gửi, bấm gửi lại không tạo đơn trùng |

## 4. Bảng đối chiếu phiên ở bước 6

`PageTurn` là `DiningTable.TurnVersion` lúc tải trang. `TurnVersion` tăng mỗi khi một lượt khách kết thúc.

| PageSessionId | Phiên hiện tại của bàn | Kết quả |
| --- | --- | --- |
| null | Chưa có, `PageTurn` = `TurnVersion` | Tạo phiên, cấp quyền |
| null | Chưa có, `PageTurn` khác | StaleSession |
| null | Đang phục vụ, `PageTurn` = `TurnVersion` | Vào phiên hiện tại, cấp quyền (máy thứ hai không cần quét lại) |
| null | Có phiên nhưng `PageTurn` khác | StaleSession |
| = phiên hiện tại | Đang phục vụ, có quyền | Tiếp tục |
| = phiên hiện tại | Đang phục vụ, mất quyền | NoAccess |
| null (đúng lượt) hoặc = phiên hiện tại | Chờ thanh toán | SessionClosed |
| khác phiên hiện tại | bất kỳ | StaleSession |

## 5. Ba trường hợp trọng tâm

**Món hết.** Bếp đặt Tạm hết hoặc quản lý Ngừng bán trong lúc khách đang chọn. Bước 7 phát hiện, từ chối cả yêu cầu (không lưu một phần),
trả danh sách món. Trang khách cũng tự đổi nút thành "Tạm hết" sau lần lấy trạng thái kế tiếp.

**Đổi giá.** Quản lý sửa giá sau khi khách đã thêm món vào giỏ. Bước 7 so giá, trả `PriceChanged` kèm giá mới. Trình duyệt cập nhật giá trong giỏ
và hiện "Giá đổi từ ... thành ...". Vì nội dung giỏ đã đổi nên lần gửi sau dùng mã mới. Các đơn đã gửi trước đó giữ nguyên giá cũ.

**Gửi trùng.** `Orders.ClientRequestId` là UNIQUE. Bấm đúp hoặc gửi lại sau khi mất mạng đều dùng cùng mã nên chỉ có một đơn. Hai điện thoại
gửi đơn đầu tiên gần như cùng lúc: giao dịch chạy lần lượt, máy sau thấy phiên đã có và vào chung phiên. Nếu vẫn va chạm ràng buộc một phiên/bàn
thì bên thua thử lại một lần.

## 6. UC07 - Gọi bổ sung

Dùng chung luồng UC06. Phiên đã có nên không tạo phiên. Mỗi lần gửi tạo một đơn mới, đơn cũ giữ nguyên. Khi bàn Chờ thanh toán phải nhờ nhân viên
chuyển về Đang phục vụ mới gọi thêm được.

## 7. UC08 - Theo dõi món của bàn

`GET /ban/{token}/trang-thai?sessionId=&turn=` trả về: trạng thái phiên, danh sách dòng món của cả bàn (đánh dấu dòng "bạn gọi"), lý do từ chối/hủy,
trạng thái yêu cầu hủy, tạm tính, phần chờ xác nhận, loại hỗ trợ đang mở và tình trạng/giá mới nhất của các món.
Nếu phiên đã đóng hoặc hủy thì trả `stale = true` và trang khóa mọi thao tác.

## 8. Quy tắc nghiệp vụ

- BR1: số lượng mỗi dòng từ 1 đến 50; BR2: ghi chú tối đa 300 ký tự; tối đa 50 dòng mỗi đơn.
- BR3: đơn giá và tên món lưu vào dòng tại thời điểm gửi thành công.
- BR4: giá, tình trạng món và quyền luôn kiểm tra ở server.
- BR5: mỗi lần gửi tạo một đơn mới trong phiên.
- BR6: dòng mới ở Chờ xác nhận, chưa cộng vào tạm tính.
- BR7: không tạo phiên rỗng.
- BR8: phiên Chờ thanh toán không nhận đơn mới.
- BR9: trang của lượt khách cũ không gửi được vào lượt mới.
