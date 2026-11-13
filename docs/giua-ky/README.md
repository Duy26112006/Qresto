# Thiết kế giữa kỳ Qresto Nhóm 8

Qresto là website gọi món tại bàn bằng mã QR và quản lý đơn hàng. Bộ thiết kế giữa kỳ gồm ba phần:

| Nội dung | Bản xem | Tệp chỉnh sửa |
| --- | --- | --- |
| Kiến trúc hệ thống | [PNG](so-do/Qresto_Kien_Truc_He_Thong.png) | [Draw.io](so-do/Qresto_Kien_Truc_He_Thong.drawio) |
| Lược đồ use case tổng thể | [PNG](so-do/Qresto_Use_Case_Tong_The.png) | [Draw.io](so-do/Qresto_Use_Case_Tong_The.drawio) |
| Lược đồ lớp tổng thể | [PNG](so-do/Qresto_Lop_Tong_The.png) | [Draw.io](so-do/Qresto_Lop_Tong_The.drawio) |

## Kiến trúc

Khách và nhân viên sử dụng trình duyệt. Qresto dùng ASP.NET Core MVC, EF Core và SQLite trên cùng máy chủ. ASP.NET Core Identity xác thực và phân quyền nhân viên; SignalR báo thay đổi sau khi lưu thành công.

## Quy tắc nối các phần

- Quét QR nhận diện bàn, hiển thị thực đơn và không tạo phiên. Đơn đầu tiên lưu thành công mới tạo phiên. Đơn tiếp theo dùng phiên đang phục vụ; phục vụ xác nhận món trước khi bếp nhận.
- Hai điện thoại mở menu khi chưa có phiên đều có thể đặt món. Sau khi đơn thứ nhất tạo phiên, đơn hợp lệ thứ hai tham gia phiên đó. Quyền gắn với một phiên đã đóng không tự chuyển sang phiên mới. Khi mất phản hồi, thử lại cùng mã gửi; chỉ trả đơn cũ nếu cùng người gửi và cùng nội dung.
- Đang phục vụ và Chờ thanh toán đều là phiên đang hoạt động. Chờ thanh toán khóa gọi thêm; khách vẫn có thể xem tạm tính, gọi hỗ trợ hoặc yêu cầu hủy hợp lệ. Phục vụ hoặc quản lý có thể cho gọi tiếp trước khi thu tiền.
- Bếp cập nhật từng dòng món. Sự cố là cờ và thông tin trên dòng, không phải trạng thái chế biến mới. Sau khi bắt đầu chế biến, chỉ quản lý được hủy ngoại lệ trước khi giao.
- Trước khi thu tiền, đối chiếu tổng tiền, phiên bản thanh toán, trạng thái dòng món và các yêu cầu/sự cố còn mở. Tạo hóa đơn, lưu chi tiết, đóng phiên và thu hồi quyền truy cập trong cùng một giao dịch.
- Phiên có tối đa một hóa đơn. Chi tiết hóa đơn giữ bản sao tên món, số lượng, đơn giá và thành tiền; không đọc lại giá menu để tính hóa đơn cũ. Phiên không có món tính tiền có thể hủy nếu đủ điều kiện nghiệp vụ.

## Nguồn phần việc trên GitHub

| Thành viên | Nội dung | Nhánh hoặc PR |
| --- | --- | --- |
| Nguyễn Khánh Duy | Kiến trúc, bàn, QR, phiên và quyền truy cập | [PR #7](https://github.com/Duy26112006/Qresto/pull/7) |
| Nguyễn Tùng Duy | Thực đơn, giỏ và gửi đơn | [PR #4](https://github.com/Duy26112006/Qresto/pull/4) |
| Võ Hữu Duy | Phục vụ, bếp, hủy món, hỗ trợ và sự cố | [Nhánh sơ đồ](https://github.com/Duy26112006/Qresto/tree/sodo/huuduy_service) |
| Chương Thị Ngọc Duyên | Thanh toán, hóa đơn, thống kê và nhân viên | [PR #5](https://github.com/Duy26112006/Qresto/pull/5) |

Tên chung: `DiningSession` là phiên phục vụ, `OrderItem` là dòng món, `PaymentVersion` là phiên bản dữ liệu thanh toán. `GuestSessionAccess` gắn một định danh trình duyệt với một phiên. Lược đồ lớp mô tả các lớp chính; giỏ hàng là dữ liệu tạm của trình duyệt.

Bộ tài liệu này là kết quả thiết kế giữa kỳ, chưa phải bản triển khai chạy được.
