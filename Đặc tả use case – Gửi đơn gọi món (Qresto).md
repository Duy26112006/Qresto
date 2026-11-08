# Đặc tả use case – Gửi đơn gọi món (Qresto)

Phụ trách: Nguyễn Tùng Duy · Nhóm 8 – Đồ án thực tế công nghệ phần mềm

## 1. Danh sách use case phần Thực đơn và Gọi món

| Mã | Tên use case | Tác nhân |
| --- | --- | --- |
| UC01 | Quét QR, xem thực đơn theo bàn | Khách hàng |
| UC02 | Xem chi tiết món | Khách hàng |
| UC03 | Tìm kiếm món | Khách hàng |
| UC04 | Lọc theo danh mục | Khách hàng |
| UC05 | Quản lý giỏ hàng (thêm, sửa số lượng, xóa, ghi chú) | Khách hàng |
| UC06 | Gửi đơn gọi món | Khách hàng |
| UC07 | Gọi bổ sung | Khách hàng |
| UC08 | Xem món đã gọi của phiên | Khách hàng |
| UC09 | Theo dõi trạng thái món | Khách hàng |
| UC10 | Quản lý danh mục | Quản lý |
| UC11 | Quản lý món ăn | Quản lý |
| UC12 | Đổi tình trạng món (Đang bán / Tạm hết / Ngừng bán) | Nhân viên bếp, Quản lý |

Tài liệu này đặc tả chi tiết UC06 và UC07, kèm các trường hợp món hết, đổi giá và gửi trùng.

## 2. Đặc tả UC06 – Gửi đơn gọi món

| Mục | Nội dung |
| --- | --- |
| Mã, tên | UC06 – Gửi đơn gọi món |
| Tác nhân chính | Khách hàng (không cần đăng nhập) |
| Thành phần phối hợp | DiningSessionService (phần Khánh Duy) để lấy hoặc tạo phiên; SignalR để báo cho phục vụ |
| Mô tả | Khách gửi giỏ hàng hiện tại. Hệ thống kiểm tra lại toàn bộ ở server, lấy hoặc tạo phiên của bàn, rồi lưu thành một đơn mới gồm các dòng món ở trạng thái Chờ xác nhận. |
| Kích hoạt | Khách bấm «Gửi gọi món» trên trang giỏ hàng |
| Tiền điều kiện | Khách đang ở trang thực đơn của một bàn hợp lệ. Giỏ có ít nhất 1 dòng. Bàn chưa có phiên (sẽ được tạo) hoặc phiên đang ở trạng thái Đang phục vụ. |
| Hậu điều kiện (thành công) | Có đúng 1 đơn mới gắn đúng phiên và đúng trình duyệt gửi. Mỗi dòng món ở trạng thái Chờ xác nhận, đơn giá lấy từ server tại thời điểm gửi. Phiên đã tồn tại (tạo nếu bàn chưa có) và trình duyệt có quyền truy cập phiên. Giỏ được làm trống. Phục vụ nhận thông báo đơn mới. |
| Hậu điều kiện (thất bại) | Không có đơn nào được lưu. Không tạo phiên rỗng. Giỏ của khách giữ nguyên để chỉnh sửa hoặc gửi lại. |

### 2.1. Luồng chính

| Bước | Thực hiện bởi | Hành động |
| --- | --- | --- |
| 1 | Khách | Bấm «Gửi gọi món». |
| 2 | Trình duyệt | Kiểm tra nhanh giỏ không trống, sinh mã chống gửi trùng (ClientRequestId) nếu chưa có, gửi yêu cầu kèm danh sách {món, số lượng, ghi chú, giá đang hiển thị} và cookie truy cập. |
| 3 | Server | Kiểm tra dữ liệu: giỏ không trống, số lượng từ 1 đến 50, ghi chú tối đa 300 ký tự. |
| 4 | Server | Mở giao dịch. Tra phiên đang hoạt động của bàn và quyền truy cập của trình duyệt. Nếu có phiên thì phiên phải ở trạng thái Đang phục vụ. |
| 5 | Server | Nếu phiên đã có, kiểm tra ClientRequestId chưa được ghi nhận trong phiên. |
| 6 | Server | Đọc các món từ cơ sở dữ liệu. Mỗi món phải ở trạng thái Đang bán và giá hiện tại phải bằng giá khách đang thấy. |
| 7 | Server | Lấy hoặc tạo phiên của bàn, cấp quyền truy cập cho trình duyệt nếu chưa có (gọi DiningSessionService, cùng giao dịch). |
| 8 | Server | Tạo đơn và các dòng món ở trạng thái Chờ xác nhận, lưu đơn giá hiện tại của server vào từng dòng. |
| 9 | Server | Lưu thay đổi và chốt giao dịch. |
| 10 | Server | Sau khi chốt, phát thông báo «đơn mới» tới nhóm Phục vụ qua SignalR. |
| 11 | Server | Trả kết quả thành công kèm mã đơn và các dòng món, kèm cookie truy cập nếu trình duyệt vừa được cấp quyền ở bước 7. |
| 12 | Trình duyệt | Xóa giỏ, hiển thị đơn trong «Món đã gọi», báo gửi thành công. |

### 2.2. Luồng thay thế và ngoại lệ

| Mã | Tại bước | Điều kiện | Xử lý của hệ thống | Khách thấy |
| --- | --- | --- | --- | --- |
| A1 | 3 | Giỏ trống, số lượng ngoài 1–50 hoặc ghi chú quá 300 ký tự | Không lưu gì. Trả InvalidInput (HTTP 400). | Thông báo lỗi ngay trên dòng sai. Giỏ giữ nguyên. |
| A2 | 4 | Phiên đang Chờ thanh toán hoặc đã kết thúc | Hủy giao dịch. Trả SessionClosed (HTTP 409). | Báo bàn đang yêu cầu thanh toán nên tạm dừng gọi món, gọi nhân viên nếu muốn gọi thêm. |
| A3 | 4 | Cookie không thuộc phiên hiện tại hoặc đã hết hiệu lực | Hủy giao dịch. Trả NoAccess (HTTP 403). | Hướng dẫn quét lại QR trên bàn. |
| A4 | 5 hoặc 9 | Gửi trùng: ClientRequestId đã có trong phiên | Không tạo thêm đơn. Trả Duplicate kèm mã đơn đã tạo. | Thấy đơn đã gửi như bình thường, không bị gọi hai lần. |
| A5 | 6 | Có món Tạm hết hoặc Ngừng bán | Hủy giao dịch. Trả ItemUnavailable kèm danh sách món. | Món hết được đánh dấu, khách bỏ món đó rồi gửi lại. |
| A6 | 6 | Giá hiện tại khác giá khách đang thấy | Hủy giao dịch. Trả PriceChanged kèm giá mới. | Giỏ cập nhật giá mới, khách xem lại tổng tiền và bấm gửi lại để xác nhận. |
| A7 | Bất kỳ | Mất kết nối hoặc lỗi hệ thống | Hủy giao dịch, không báo thành công khi chưa lưu. | Thông báo rõ lỗi, giữ giỏ. Khi có mạng, khách gửi lại. |

### 2.3. Ba trường hợp trọng tâm

**Món hết**

- Tình huống: bếp đổi món sang Tạm hết (hoặc quản lý Ngừng bán) trong lúc khách đang chọn.
- Phát hiện: bước 6, trạng thái món trong cơ sở dữ liệu khác Đang bán. Không tin trạng thái hiển thị ở trình duyệt.
- Xử lý: từ chối cả yêu cầu, không lưu một phần. Trả danh sách các món không đặt được.
- Giao diện: tô dấu món hết trong giỏ, giữ nguyên các món còn lại. Khách bỏ món hết rồi gửi lại.

**Đổi giá**

- Tình huống: quản lý sửa giá sau khi khách đã thêm món vào giỏ.
- Phát hiện: bước 6, trình duyệt gửi kèm giá đang hiển thị (chỉ để so sánh), server so với giá hiện tại. Trình duyệt không bao giờ quyết định giá bán.
- Xử lý: từ chối yêu cầu, trả giá mới. Chỉ khi khách gửi lại sau khi đã thấy giá mới thì đơn mới được tạo, với đơn giá lấy từ server tại lúc đó.
- Giao diện: hiển thị giá cũ và giá mới, yêu cầu khách xác nhận lại.

**Gửi trùng**

- Tình huống: bấm đúp, mạng chậm nên bấm lại, hoặc trình duyệt gửi lại yêu cầu.
- Phát hiện: mã ClientRequestId do trình duyệt sinh khi bắt đầu gửi. Có ràng buộc duy nhất theo cặp (phiên, ClientRequestId) trong cơ sở dữ liệu nên hai yêu cầu đồng thời cũng chỉ một yêu cầu ghi được.
- Xử lý: yêu cầu thứ hai trả về đơn đã có (Duplicate), không tạo đơn mới.
- Quy ước mã: gửi lại sau lỗi mạng thì giữ nguyên mã. Gửi lại sau khi khách đã sửa giỏ vì món hết hoặc đổi giá thì sinh mã mới.
- Với đơn đầu tiên của bàn chưa có phiên: hai yêu cầu cùng lúc cùng cố tạo phiên, chỉ một phiên được tạo (ràng buộc một phiên hoạt động mỗi bàn). Yêu cầu bên thua đọc lại phiên và đơn đã tạo.

### 2.4. Quy tắc nghiệp vụ liên quan

- BR1: số lượng mỗi dòng từ 1 đến 50.
- BR2: ghi chú tối đa 300 ký tự.
- BR3: đơn giá lưu vào dòng món tại thời điểm gửi thành công, sửa menu sau đó không đổi đơn đã gửi.
- BR4: giá, trạng thái món và quyền truy cập luôn kiểm tra ở server.
- BR5: mỗi lần gửi tạo một đơn mới trong phiên, các đơn trước giữ nguyên.
- BR6: dòng mới ở trạng thái Chờ xác nhận, chưa cộng vào tạm tính.
- BR7: không tạo phiên rỗng. Tạo phiên và lưu đơn thành công hoặc thất bại cùng nhau.
- BR8: khi phiên đang Chờ thanh toán không nhận đơn mới.

## 3. Đặc tả UC07 – Gọi bổ sung

| Mục | Nội dung |
| --- | --- |
| Quan hệ | Kế thừa UC06, dùng cùng luồng kiểm tra và lưu |
| Kích hoạt | Khách bấm «Gọi thêm món», hoặc quét lại QR nếu đã đóng trang |
| Tiền điều kiện | Phiên đang ở trạng thái Đang phục vụ, trình duyệt có quyền truy cập |
| Khác UC06 | Phiên đã tồn tại nên bước tạo phiên chỉ là lấy phiên hiện có. Mỗi lần gửi tạo một đơn mới, các đơn trước giữ nguyên. |
| Hậu điều kiện | Có thêm một đơn trong cùng phiên, thanh toán chung với các đơn trước |
| Ngoại lệ | Phiên Chờ thanh toán thì không gọi thêm được. Nhân viên phải mở lại phục vụ trước. |

## 4. Tình huống kiểm thử liên quan

| Mã | Tình huống | Thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-06-01 | Gửi thành công, bàn chưa có phiên | Quét QR, thêm món, gửi | Bàn tự có phiên, có 1 đơn, dòng Chờ xác nhận, giỏ trống |
| TC-06-02 | Giỏ trống | Bấm gửi khi giỏ trống | InvalidInput, không tạo đơn, không tạo phiên |
| TC-06-03 | Số lượng, ghi chú sai | Số lượng 0, 51 hoặc ghi chú 301 ký tự | InvalidInput, giỏ giữ nguyên |
| TC-06-04 | Món vừa hết | Bếp đặt Tạm hết, khách gửi món đó | ItemUnavailable, không lưu đơn nào |
| TC-06-05 | Đổi giá | Quản lý sửa giá, khách gửi với giá cũ | PriceChanged, khách xác nhận lại mới tạo đơn với giá mới |
| TC-06-06 | Gửi nhiều lần | Bấm gửi nhiều lần hoặc gửi cùng ClientRequestId hai lần | Chỉ một đơn được tạo |
| TC-06-07 | Hai điện thoại cùng bàn | Hai điện thoại gửi gần như đồng thời | Hai đơn riêng trong cùng một phiên |
| TC-06-08 | Phiên Chờ thanh toán | Yêu cầu thanh toán rồi gửi món | SessionClosed, không tạo đơn |
| TC-06-09 | Gọi bổ sung | Gửi lần hai sau khi đơn đầu đã xác nhận | Đơn mới trong cùng phiên, đơn cũ giữ nguyên |
| TC-06-10 | Lỗi giữa chừng | Tắt mạng hoặc gây lỗi khi lưu | Không đơn nào được lưu, không có phiên rỗng, giỏ giữ nguyên |

## 5. Điểm cần thống nhất với nhóm

- Khánh Duy: chữ ký hàm lấy hoặc tạo phiên, và dùng chung một giao dịch cơ sở dữ liệu với OrderService.
- Khánh Duy: tài liệu nghiệp vụ mục 6.2 còn ghi «chưa có phiên thì khách xem menu nhưng chưa gửi món», mâu thuẫn với luồng mới (phiên tự tạo khi gửi đơn đầu tiên).
- Hữu Duy: thống nhất tên trạng thái dòng món và các cột sự cố trong bảng dòng món trước khi tạo migration.
