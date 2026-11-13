# Ôn vấn đáp giữa kỳ Qresto

Cả nhóm tập kể cùng một ví dụ: khách quét QR bàn 05 → gửi món → hệ thống tự tạo phiên → phục vụ xác nhận → bếp chế biến → phục vụ giao → khách yêu cầu thanh toán → nhân viên xác nhận đã thu tiền → hệ thống lập hóa đơn và đóng phiên.

## Nguyễn Khánh Duy — kiến trúc, bàn, QR và phiên

1. **Quét QR có mở phiên không?** Không. QR nhận diện bàn và mở thực đơn. Đơn đầu tiên hợp lệ được lưu thành công mới tạo phiên; lưu lỗi không để lại phiên rỗng.
2. **Hai điện thoại gọi cùng lúc?** Máy chủ kiểm tra phiên khi ghi đơn và bảo đảm mỗi bàn chỉ có một phiên đang hoạt động. Đơn hợp lệ của máy thứ hai vào cùng phiên, kể cả máy đó đã mở thực đơn trước khi có phiên.
3. **Tại sao MVC, SQLite và SignalR?** MVC tổ chức phần nhận yêu cầu, xử lý và hiển thị. SQLite lưu dữ liệu phía máy chủ. SignalR báo thay đổi sau khi lưu; khi kết nối lại, màn hình đọc trạng thái mới nhất từ máy chủ.
4. **Cookie cũ có gọi cho lượt khách sau không?** Không. Quyền gắn với đúng phiên; sau khi phiên kết thúc khách phải vào lại bàn để hệ thống cấp ngữ cảnh hợp lệ.

## Nguyễn Tùng Duy — thực đơn, giỏ và gửi đơn

1. **Giỏ có phải một lớp lưu trong cơ sở dữ liệu không?** Giỏ là dữ liệu tạm của trình duyệt. Chỉ khi gửi thành công mới lưu đơn và dòng món.
2. **Giá hoặc tình trạng món đổi trước khi gửi?** Máy chủ kiểm tra lại, trả thay đổi để khách xác nhận hoặc sửa giỏ. Chưa lưu đơn và chưa tạo phiên nếu lần gửi thất bại.
3. **Bấm gửi hai lần hoặc mất mạng thì sao?** Thử lại cùng mã gửi. Nếu yêu cầu của đúng khách và cùng nội dung đã được lưu thì trả lại kết quả cũ. Cùng mã nhưng nội dung khác phải từ chối.
4. **Gọi bổ sung có sửa đơn cũ không?** Không. Mỗi lần gửi tạo đơn mới trong cùng phiên đang phục vụ.

## Võ Hữu Duy — phục vụ, bếp, hủy và hỗ trợ

1. **Bếp nhận món khi nào?** Sau khi phục vụ xác nhận dòng món. Bếp cập nhật từng dòng để các món trong một đơn có thể tiến triển khác nhau.
2. **Bếp hết món sau khi xác nhận?** Ghi sự cố trên dòng món và báo phục vụ/quản lý. Đây là cờ sự cố, không thêm trạng thái chế biến. Chặn giao hoặc đánh dấu sẵn sàng khi sự cố chưa xử lý.
3. **Ai được hủy món?** Phục vụ hoặc quản lý có thể hủy theo quyền trước chế biến. Đã chế biến nhưng chưa giao thì chỉ quản lý xử lý hủy ngoại lệ. Món đã giao không hỗ trợ hủy.
4. **Bấm nhầm sẵn sàng có quay lại được không?** Bếp hoặc quản lý được hoàn tác về Đang chế biến nếu chưa giao và có lý do; giữ lịch sử và không đặt lại đồng hồ chờ.

## Chương Thị Ngọc Duyên — thanh toán, hóa đơn, báo cáo

1. **Chờ thanh toán đã có hóa đơn chưa?** Chưa. Khách xem tạm tính và không gửi món mới. Phục vụ hoặc quản lý có thể cho gọi tiếp trước khi thu tiền.
2. **Tổng tiền thay đổi trong lúc thao tác thì sao?** Máy chủ đối chiếu tổng tiền và phiên bản thanh toán. Nếu lệch, yêu cầu tải lại trước khi xác nhận.
3. **Hai nhân viên cùng xác nhận có sinh hai hóa đơn không?** Mỗi phiên chỉ có tối đa một hóa đơn. Kiểm tra và ghi hóa đơn, đóng phiên và thu hồi quyền khách trong cùng giao dịch.
4. **Sửa menu có làm đổi doanh thu cũ không?** Không. Chi tiết hóa đơn giữ bản sao tên món, số lượng và đơn giá. Doanh thu lấy từ hóa đơn đã thanh toán theo thời điểm thu tiền.

Khi vấn đáp, chỉ nói đây là cách hệ thống được thiết kế nếu tính năng chưa được chạy hoặc kiểm thử.
