# Đặc tả: điều kiện chốt hóa đơn và cách tính doanh thu

Phụ trách: Chương Thị Ngọc Duyên. Phối hợp: Khánh Duy (đóng phiên), Hữu Duy (trạng thái dòng món).

## 1. Cách tính tạm tính
- Tiền tệ là VNĐ, số nguyên. Giá hiển thị là giá cuối, chưa cộng thuế hay phí phục vụ.
- Đơn giá được lưu vào dòng món tại thời điểm gửi đơn thành công.
- Tạm tính = tổng (số lượng × đơn giá đã lưu) của các dòng **đã được xác nhận và chưa bị hủy**.
- Dòng Chờ xác nhận hiển thị riêng, chưa cộng tiền. Dòng Bị từ chối hoặc Đã hủy không tính tiền.
- Khi phiên ở trạng thái Chờ thanh toán, hệ thống chỉ hiển thị bảng tạm tính, chưa tạo hóa đơn.

## 2. Điều kiện để chốt thanh toán
Server kiểm tra **trong cùng một giao dịch**. Chỉ cần một điều kiện sai thì không ghi nhận thanh toán.

| # | Điều kiện |
|---|-----------|
| 1 | Phiên đang ở trạng thái Chờ thanh toán |
| 2 | Phiên chưa có hóa đơn đã thanh toán |
| 3 | Mọi dòng món đều ở trạng thái Đã phục vụ, Đã hủy hoặc Bị từ chối |
| 4 | Không còn yêu cầu hủy ở trạng thái Chờ duyệt |
| 5 | Không còn sự cố dòng món chưa giải quyết |
| 6 | Tổng tiền và phiên bản dữ liệu thanh toán do giao diện gửi lên khớp dữ liệu hiện tại |

Khi không khớp: không ghi nhận, tải lại dữ liệu và báo
"Thông tin thanh toán đã thay đổi, vui lòng kiểm tra lại."

## 3. Phiên bản dữ liệu thanh toán
- SQLite không có `rowversion` tự sinh như SQL Server, nên dùng cột `PhienBanThanhToan` (số nguyên) trên phiên, **do ứng dụng tự tăng**.
- Tăng 1 mỗi khi có thay đổi ảnh hưởng nội dung hoặc điều kiện thanh toán: dòng món được xác nhận, đổi trạng thái, bị từ chối hoặc hủy; yêu cầu hủy được tạo hoặc xử lý; sự cố được báo hoặc giải quyết; phiên được mở lại để gọi thêm.
- Khi mở màn hình thanh toán, server trả về tổng tiền và phiên bản. Khi xác nhận thu tiền, giao diện gửi lại hai giá trị này để server so sánh.

## 4. Quy trình chốt (một giao dịch, thành công hoặc thất bại cùng nhau)
1. Bắt đầu giao dịch.
2. Kiểm tra 6 điều kiện ở mục 2.
3. Tạo hóa đơn: phiên, tổng tiền, phương thức (tiền mặt hoặc chuyển khoản), người xác nhận, thời điểm thanh toán.
4. Sao chép chi tiết hóa đơn từ các dòng đã phục vụ và được tính tiền: tên món, số lượng, đơn giá, thành tiền.
5. Gọi `DiningSessionService` đóng phiên: trạng thái Đã đóng, kết thúc quyền truy cập của khách, trả bàn về trống.
6. Lưu giao dịch. Nếu bất kỳ bước nào lỗi thì hoàn tác toàn bộ.

Không để xảy ra tình trạng đã ghi nhận thu tiền nhưng phiên vẫn mở, hoặc ngược lại.

## 5. Chống chốt trùng
- Hai nhân viên cùng bấm xác nhận: chỉ một thao tác thành công.
- Bảo đảm bằng 2 lớp: kiểm tra "chưa có hóa đơn" trong giao dịch, và **unique index** trên `HoaDon.PhienPhucVuId`.
- Người bấm sau nhận thông báo dữ liệu đã thay đổi.

## 6. Chi tiết hóa đơn
- Là **bản sao** tại thời điểm thanh toán. Nội dung và số tiền lấy từ bản sao, không đọc lại từ menu.
- `DongMonId` chỉ để truy vết (cho phép NULL). Sửa giá hoặc tên món sau đó không làm đổi hóa đơn cũ.
- Thành tiền = số lượng × đơn giá. Tổng tiền hóa đơn = tổng thành tiền các chi tiết.
- Hóa đơn đã thanh toán không sửa, không xóa.

## 7. Cách tính doanh thu
- Chỉ tính **hóa đơn đã thanh toán**. Phiên Đã hủy không có hóa đơn nên không tính.
- Doanh thu tính theo **thời điểm xác nhận thanh toán** (`ThoiDiemThanhToan`), không theo thời điểm gọi món.
- Theo ngày: tổng tiền các hóa đơn có thời điểm thanh toán trong ngày đó. Theo tháng: tương tự trong tháng.
- Tổng doanh thu phải khớp tổng các hóa đơn tương ứng.

## 8. Món bán chạy
- Cộng số lượng của các chi tiết hóa đơn thuộc hóa đơn đã thanh toán, nhóm theo món, sắp xếp giảm dần.
- Chỉ tính món đã phục vụ vì chi tiết hóa đơn chỉ chứa các dòng đã phục vụ.
- Điểm cần thống nhất với Tùng Duy: nhóm theo món gốc (qua `DongMonId`) hay theo tên bản sao. Nếu đổi tên món giữa chừng thì hai cách cho kết quả khác nhau.

## 9. Quản lý tài khoản nhân viên
- Quản lý tạo tài khoản, gán vai trò (Phục vụ, Bếp, Quản lý) và khóa tài khoản.
- Dữ liệu có lịch sử thì ngừng hoạt động thay vì xóa, để không mất liên kết với hóa đơn và lịch sử xử lý.
- Quyền kiểm tra ở server, không chỉ ẩn nút trên giao diện.

## 10. Tình huống cần kiểm thử
- Yêu cầu thanh toán khóa gọi thêm cho cả bàn.
- Không chốt được khi còn món chưa hoàn tất, yêu cầu hủy chờ duyệt hoặc sự cố tồn.
- Tổng tiền hoặc phiên bản thay đổi trước khi xác nhận thu tiền.
- Hai nhân viên cùng xác nhận thanh toán một lúc.
- Thanh toán thành công tạo đúng một hóa đơn, bàn trở lại trống.
- Sửa giá menu không làm đổi hóa đơn đã chốt.
- Doanh thu và món bán chạy khớp các hóa đơn đã thanh toán.
- Người không có quyền không vào được trang thống kê và quản lý nhân viên.
