# Qresto - Nghiệp vụ và hướng triển khai

**Nhóm 8 - Đồ án thực tế công nghệ phần mềm**  
**Đề tài**: Xây dựng website gọi món tại bàn bằng mã QR và quản lý xử lý đơn hàng cho nhà hàng  
**Thành viên**: Nguyễn Khánh Duy, Nguyễn Tùng Duy, Võ Hữu Duy, Chương Thị Ngọc Duyên  

## PHẦN A. NGHIỆP VỤ HỆ THỐNG

### 1. Mục tiêu và phạm vi

Qresto hỗ trợ quy trình phục vụ tại một nhà hàng: khách quét QR để tự gọi món trên điện thoại, phục vụ kiểm tra đơn, bếp chế biến, phục vụ giao món, sau đó nhân viên thu tiền và kết thúc lượt phục vụ. Khách sử dụng trình duyệt, không cần cài ứng dụng hoặc đăng ký tài khoản.

Nhóm triển khai thực đơn, bàn và QR, giỏ hàng, gọi món, gọi bổ sung, xử lý món, yêu cầu hủy, hỗ trợ tại bàn, thanh toán, hóa đơn và thống kê. Nhà hàng, bàn và món ăn trong bản demo là dữ liệu mẫu.

Ngoài phạm vi đồ án: quản lý tồn kho, đặt bàn trước, giao hàng, chuyển hoặc gộp bàn, chia hóa đơn, hoàn tiền và tích hợp cổng thanh toán. Hệ thống chỉ ghi nhận nhân viên đã thu tiền mặt hoặc đã kiểm tra nhận chuyển khoản.

### 2. Vai trò và quyền xử lý

| Vai trò | Công việc chính |
| --- | --- |
| Khách hàng | Quét QR, xem thực đơn, thêm món và ghi chú, gửi đơn, gọi bổ sung, theo dõi món của bàn, yêu cầu hủy món mình gọi, gọi hỗ trợ và yêu cầu thanh toán. |
| Nhân viên phục vụ | Xác nhận hoặc từ chối món, xử lý yêu cầu hủy và hỗ trợ, hủy món trước khi chế biến, giải quyết sự cố theo quyền, xác nhận đã giao món, bắt đầu thanh toán và xác nhận đã thu tiền. |
| Nhân viên bếp | Xem món đã xác nhận, bắt đầu làm, báo món sẵn sàng, hoàn tác khi bấm nhầm, báo sự cố và cập nhật món tạm hết. Bếp không xác nhận giao món hoặc thu tiền. |
| Quản lý | Quản lý danh mục, món, giá, ảnh, bàn, QR và tài khoản; tra cứu lịch sử, hóa đơn, doanh thu; hỗ trợ công việc của phục vụ và bếp; hủy ngoại lệ đối với món đã bắt đầu chế biến nhưng chưa giao. |

Các quyền trên phải được kiểm tra ở máy chủ. Việc ẩn nút trên màn hình chỉ giúp người dùng thấy đúng thao tác của mình.

### 3. Bàn, phiên phục vụ, giỏ và đơn gọi món

- Bàn có một QR riêng. Quét QR Bàn 03 thì vào đúng thực đơn của Bàn 03.
- Phiên phục vụ là một lượt khách sử dụng bàn. Một bàn có nhiều phiên qua thời gian nhưng chỉ có tối đa một phiên đang hoạt động, gồm trạng thái Đang phục vụ hoặc Chờ thanh toán.
- Giỏ hàng là món khách đang chọn, chưa gửi. Mỗi trình duyệt có giỏ riêng.
- Đơn gọi món được tạo sau mỗi lần Gửi đơn thành công. Một phiên có thể có nhiều đơn, mỗi đơn có nhiều dòng món.
- Dòng món là một mục trong đơn, ví dụ 2 phần Gỏi cuốn, không đậu phộng. Số lượng, ghi chú và trạng thái được theo dõi theo từng dòng.
- Hóa đơn là bản ghi chính thức khi đã thu tiền. Mỗi phiên có tối đa một hóa đơn.

**Quy tắc chốt:** Quét QR hoặc thêm món vào giỏ chưa tạo phiên. Hệ thống chỉ tạo phiên khi lưu thành công đơn đầu tiên. Không có thao tác nhân viên mở phiên thủ công trước khi khách gọi món.

### 4. Quy trình từ lúc khách quét QR đến lúc giao món

Bước 1 - Quét QR và xem thực đơn. Trang hiển thị tên bàn, danh mục, tên món, ảnh, giá và tình trạng bán. Khi bàn chưa có phiên, khách vẫn xem và gọi món ngay. Nếu QR không hợp lệ hoặc bàn ngừng sử dụng, trang báo lỗi và không nhận đơn.

Bước 2 - Chọn món và xem giỏ. Lần đầu bấm Thêm +, ngay trên thẻ món đổi thành − 1 +. Bấm thêm thì tăng số lượng; giảm về 0 thì hiện lại Thêm +. Khi giỏ có món, thanh Xem giỏ ghim ở đáy màn hình điện thoại. Ví dụ: Xem giỏ (2) · 60.000 đ. Số 2 là tổng số phần. Bấm vào thì giỏ mở từ dưới lên để xem từng món, sửa số lượng, thêm ghi chú và gửi đơn. Đóng giỏ xong khách vẫn ở vị trí đang xem thực đơn. Số lượng trên thẻ món và trong giỏ phải khớp nhau.

Bước 3 - Gửi đơn. Máy chủ kiểm tra bàn, phiên, tình trạng bán, giá và dữ liệu giỏ. Một đơn có tối đa 50 dòng, mỗi dòng từ 1 đến 50 phần, ghi chú tối đa 300 ký tự. Nếu chưa có phiên, máy chủ tạo phiên và lưu đơn đầu tiên cùng nhau. Nếu có phiên Đang phục vụ, đơn mới vào phiên đó. Nếu bàn Chờ thanh toán, hệ thống từ chối đơn mới và giữ giỏ. Món vừa hết hoặc giá vừa đổi thì báo khách kiểm tra lại trước khi gửi. Chỉ làm trống giỏ sau khi lưu đơn thành công.

Bước 4 - Phục vụ xác nhận. Đơn mới xuất hiện ở màn hình phục vụ. Nhân viên xử lý theo từng dòng: xác nhận để chuyển bếp, hoặc từ chối và ghi lý do. Trong một đơn, món này có thể được xác nhận còn món khác bị từ chối.

Bước 5 - Bếp chế biến, phục vụ giao món. Bếp thấy bàn, tên món, số lượng và ghi chú. Khi làm xong, bếp báo Sẵn sàng phục vụ. Sau khi mang món ra bàn, phục vụ bấm Đã giao món để dòng chuyển sang Đã phục vụ.

Luồng trạng thái dòng món: Chờ xác nhận -> Chờ chế biến -> Đang chế biến -> Sẵn sàng phục vụ -> Đã phục vụ.

Nhánh khác: Bị từ chối hoặc Đã hủy. Các dòng này vẫn lưu trong lịch sử và không tính tiền.

Bước 6 - Gọi bổ sung. Khi bàn còn Đang phục vụ, khách có thể gửi thêm nhiều lần. Mỗi lần tạo đơn mới trong cùng phiên, không ghi đè đơn trước.

Ví dụ: A gửi một phần Cơm sườn ở Bàn 03, B gửi hai ly Trà đào từ điện thoại khác. Bàn 03 có một phiên và hai đơn, kể cả khi hai khách gửi gần như cùng lúc. A và B đều xem được món đã gửi của bàn, nhưng giỏ chưa gửi của họ tách riêng. Mỗi người chỉ được yêu cầu hủy dòng do trình duyệt của mình đã gửi.

### 5. Hủy món, sự cố và hỗ trợ

| Tình huống | Quy tắc xử lý |
| --- | --- |
| Khách muốn hủy | Chỉ yêu cầu hủy toàn bộ dòng mình gọi khi còn Chờ xác nhận hoặc Chờ chế biến, kèm lý do. Khi yêu cầu chờ duyệt, tạm chặn xác nhận hoặc bắt đầu làm dòng đó. Duyệt thì chuyển Đã hủy; không duyệt thì tiếp tục xử lý. |
| Nhân viên cần hủy trực tiếp | Phục vụ hoặc quản lý có thể hủy trước khi bắt đầu chế biến. Sau khi bắt đầu chế biến, chỉ quản lý được hủy nếu món chưa giao. Mọi lần hủy phải ghi lý do. |
| Bếp không thể phục vụ món | Bếp báo sự cố kèm nội dung khi món đang ở bếp (Chờ chế biến, Đang chế biến hoặc Sẵn sàng phục vụ). Món giữ trạng thái hiện tại nhưng có cờ sự cố; khi còn cờ thì không bắt đầu làm, không báo xong và không giao món đó. Phục vụ hoặc quản lý xử lý để tiếp tục, hoặc hủy theo quyền. Chưa giải quyết sự cố thì không được chốt thu tiền. |
| Bếp bấm nhầm món đã sẵn sàng | Khi chưa giao, bếp hoặc quản lý có thể chuyển về Đang chế biến, kèm lý do. Khi đã giao thì không hoàn tác theo cách này. |
| Khách cần hỗ trợ tại bàn | Khách có thể gọi nhân viên hoặc xin thêm dụng cụ. Phục vụ tiếp nhận và hoàn tất. Không tạo thêm yêu cầu cùng loại nếu yêu cầu trước còn mở trong phiên. |

### 6. Tạm tính, thanh toán và kết thúc phiên

Tạm tính là tổng đơn giá lúc gửi đơn x số lượng của các dòng đã xác nhận và còn tính tiền. Món Chờ xác nhận, Bị từ chối hoặc Đã hủy không cộng vào tạm tính. Giá đã lưu trên đơn cũ không đổi khi quản lý cập nhật giá thực đơn. Tạm tính của bàn cũng khác tổng giỏ: tổng giỏ là món đang chọn, còn tạm tính là món đã được xác nhận trong phiên.

Bắt đầu thanh toán theo một trong hai cách: khách bấm Yêu cầu thanh toán, hoặc phục vụ/quản lý bấm Bắt đầu thanh toán. Cả hai đều chuyển bàn sang Chờ thanh toán và khóa gửi món mới cho cả bàn. Khách vẫn xem được món và tạm tính, gọi hỗ trợ hoặc yêu cầu hủy nếu còn đúng điều kiện. Nếu khách muốn gọi thêm trước khi trả tiền, phục vụ hoặc quản lý chuyển bàn về Đang phục vụ.

Xác nhận đã thu tiền chỉ thực hiện sau khi thực tế đã nhận tiền và thỏa các điều kiện:

- Phiên còn Chờ thanh toán, chưa có hóa đơn và có ít nhất một dòng Đã phục vụ.
- Các dòng còn lại đều đã phục vụ, đã hủy hoặc bị từ chối. Không còn yêu cầu hủy đang chờ duyệt hay sự cố chưa giải quyết.
- Phương thức thanh toán hợp lệ. Tổng tiền và phiên bản thanh toán trên màn hình vẫn khớp dữ liệu hiện tại.


Ví dụ, nếu một món còn Sẵn sàng phục vụ, màn hình phải báo còn món chưa giao và chưa bật nút xác nhận thu tiền. Phục vụ giao món, bấm Đã giao món, kiểm tra tổng và xác nhận sau khi nhận tiền.

Khi xác nhận thành công, hệ thống tạo hóa đơn, lưu tên món, số lượng, đơn giá, tổng tiền, phương thức, người và thời điểm thu. Phiên chuyển Đã đóng và bàn sẵn sàng cho lượt sau. Hai nhân viên thao tác cùng lúc cũng chỉ được tạo một hóa đơn.

Hủy phiên không thu tiền chỉ áp dụng khi toàn bộ dòng đã hủy hoặc bị từ chối, không còn khoản phải thu hoặc yêu cầu hủy/sự cố cần xử lý. Phục vụ hoặc quản lý nhập lý do, hệ thống chuyển phiên sang Đã hủy và không tạo hóa đơn. Trang của lượt khách cũ không được tự chuyển sang lượt mới để gửi đơn.

### Quy tắc bổ sung (nhóm chốt khi triển khai)

Các điểm dưới đây tài liệu ban đầu chưa nói rõ, nhóm thống nhất khi viết code:

1. **Người ngồi cùng bàn.** Khi bàn đã có phiên, ai quét QR của bàn cũng được xem món chung của bàn và gửi thêm món vào phiên đó. Giỏ của mỗi người vẫn riêng.
2. **Lượt khách của bàn.** Mỗi bàn có số lượt, tăng 1 khi một phiên đóng hoặc hủy. Trang khách ghi lại số lượt lúc mở. Trang của lượt cũ (kể cả trang mở lúc bàn còn trống) không gửi được đơn vào lượt mới, khách phải quét lại QR.
3. **Mất quyền trình duyệt.** Nếu khách xóa cookie hoặc đổi trình duyệt giữa chừng thì quét lại QR là vào lại được phiên đang mở; món cũ do trình duyệt trước gửi thì không yêu cầu hủy từ trình duyệt mới được, phải nhờ nhân viên.
4. **Tình trạng món.** Đang bán: khách gọi được. Tạm hết: vẫn hiện trên thực đơn nhưng không gọi được, bếp hoặc quản lý bật/tắt. Ngừng bán: ẩn khỏi thực đơn, chỉ quản lý đổi.
5. **Hủy trong lúc yêu cầu hủy đang chờ.** Nếu nhân viên từ chối hoặc hủy trực tiếp một món đang có yêu cầu hủy chờ duyệt thì yêu cầu đó coi như đã xử lý.
6. **Phiên bản thanh toán.** Mọi thay đổi ảnh hưởng tới tiền hoặc điều kiện chốt (gửi đơn, đổi trạng thái món, yêu cầu hủy, sự cố, chuyển trạng thái phiên) đều làm tăng phiên bản thanh toán của phiên. Màn hình tính tiền thấy lệch phiên bản thì phải tải lại.
7. **Phiên không có món đã phục vụ.** Không xác nhận thu tiền được; nếu mọi món đã hủy hoặc bị từ chối thì dùng "Hủy phiên không thu tiền".
8. **Kết thúc phiên.** Khi đóng hoặc hủy phiên, các yêu cầu hỗ trợ còn mở được đóng luôn và quyền của các trình duyệt khách bị thu hồi.
9. **Bàn và QR.** Chỉ đổi mã QR hoặc ngừng sử dụng bàn khi bàn không có phiên đang hoạt động. Đổi QR thì QR cũ hết hiệu lực, phải in lại.
10. **Tài khoản nhân viên.** Không xóa tài khoản đã làm việc, chỉ khóa. Quản lý không tự khóa hoặc tự bỏ quyền quản lý của mình. Khóa hoặc đổi vai trò thì phiên đăng nhập cũ hết hiệu lực.
11. **Báo cáo.** Món bán chạy cộng theo món gốc, nên đổi tên món giữa chừng vẫn tính chung một món. Hóa đơn và doanh thu không đổi khi sửa giá hoặc tên món sau này.
12. **Cảnh báo chờ lâu.** Màn hình bếp tô đỏ món đã quá thời gian chế biến dự kiến của món cộng thêm 5 phút, tính từ lúc phục vụ xác nhận.

## PHẦN B. HƯỚNG TRIỂN KHAI

### 7. Công nghệ và cách tổ chức dự án

Qresto triển khai dưới dạng một ứng dụng web ASP.NET Core MVC, dùng C#/.NET 10. Giao diện dùng Razor, Bootstrap, CSS và JavaScript. Dữ liệu lưu trong SQLite qua Entity Framework Core; máy thành viên không cần cài máy chủ SQL Server. ASP.NET Core Identity quản lý đăng nhập và phân quyền nhân viên. SignalR báo thay đổi cho màn hình nhân viên; màn hình khách định kỳ lấy trạng thái mới từ máy chủ.

| Vị trí trong dự án | Nội dung |
| --- | --- |
| src/Qresto/Models, src/Qresto/Data | Các lớp dữ liệu, liên kết bảng, ràng buộc, migration và dữ liệu mẫu. |
| src/Qresto/Services | Quy tắc gửi đơn, tự tạo phiên, chuyển trạng thái món, hủy và thanh toán. |
| src/Qresto/Controllers | Nhận thao tác từ giao diện, kiểm tra ngữ cảnh và quyền, gọi phần xử lý nghiệp vụ. |
| src/Qresto/Views, src/Qresto/wwwroot | Màn hình khách, phục vụ, bếp, quản lý; CSS, JavaScript và ảnh. |
| docs, tests | Tài liệu nghiệp vụ, sơ đồ, mô hình dữ liệu, hướng dẫn và kiểm thử. |

### 8. Chia triển khai thành bốn phần để làm song song

| Phần | Việc cần triển khai | Kết quả để ghép với phần khác |
| --- | --- | --- |
| 1. Nền tảng, bàn và phân quyền | Chuẩn hóa dữ liệu, tạo tài khoản theo vai trò, quản lý bàn và QR; mở đúng trang thực đơn theo QR. Chuẩn bị dữ liệu mẫu để cả nhóm chạy được. | QR xác định đúng bàn, dữ liệu mẫu dùng chung, tài khoản phục vụ/bếp/quản lý vào đúng màn hình. |
| 2. Thực đơn, giỏ và gửi món | Tìm/lọc thực đơn, nút − số lượng +, thanh Xem giỏ, giỏ mở từ dưới, ghi chú, gửi đơn, xem trạng thái. Xử lý gửi trùng, giá đổi và gọi bổ sung. | Đơn hợp lệ lưu vào đúng phiên. Dòng mới hiện ở màn hình phục vụ với trạng thái Chờ xác nhận. |
| 3. Phục vụ, bếp và hỗ trợ | Danh sách món theo bàn; xác nhận, từ chối, chế biến, giao; xử lý hủy, sự cố và gọi hỗ trợ. | Dòng món cập nhật đúng trạng thái, có lịch sử, khách xem được thay đổi và phần thanh toán biết dòng nào được tính tiền. |
| 4. Thanh toán, hóa đơn và thống kê | Hai cách bắt đầu tính tiền, hiển thị lý do chưa thể thu, xác nhận thu, mở lại phục vụ, hủy phiên không thu tiền, lịch sử hóa đơn và báo cáo. | Đóng đúng phiên, tạo đúng một hóa đơn, số liệu thống kê khớp hóa đơn đã thanh toán và bàn dùng lại được. |

Nhóm thống nhất trước tên trạng thái, quyền, tên trường và dữ liệu truyền giữa các phần. Ví dụ, màn hình bếp nhận dòng món đã xác nhận; phần thanh toán đọc các dòng đã lưu trong cùng phiên. Mỗi phần có thể dùng dữ liệu mẫu trong lúc phát triển, nhưng khi ghép phải dùng chung dữ liệu của dự án.

### 9. Những quy tắc cần bảo đảm trong code

1. Một phiên đang hoạt động mỗi bàn. Tạo phiên, lưu đơn đầu tiên và các dòng món trong cùng một giao dịch. Đặt ràng buộc ở cơ sở dữ liệu để hai điện thoại gửi cùng lúc không tạo hai phiên.
2. Không tạo đơn trùng khi mất mạng. Mỗi lần gửi có một mã yêu cầu. Nếu mất phản hồi, giữ giỏ và gửi lại cùng mã. Máy chủ trả đơn đã lưu nếu yêu cầu đó đã được xử lý. Cùng mã nhưng khác nội dung thì từ chối.
3. Tách giỏ riêng khỏi danh sách món chung. Giỏ lưu ở trình duyệt. Máy chủ lưu đơn đã gửi và dùng định danh khách để kiểm tra quyền yêu cầu hủy. Mã QR xác định bàn; quyền truy cập phiên cũ hết hiệu lực khi đóng hoặc hủy phiên.
4. Kiểm tra trạng thái tại lúc thao tác. Khi bếp hoặc phục vụ cập nhật dòng món, máy chủ kiểm tra vai trò, trạng thái và phiên bản dòng. Nếu người khác vừa thay đổi món thì tải lại rồi xử lý tiếp.
5. Chốt tiền và đóng phiên cùng nhau. Lúc thu tiền, kiểm tra lại tổng, PaymentVersion, các dòng món và yêu cầu chưa xử lý. Tạo hóa đơn, đóng phiên và thu hồi quyền khách trong cùng giao dịch. Cơ sở dữ liệu có ràng buộc mỗi phiên chỉ lập một hóa đơn.
6. Lưu giá và lịch sử để đối chiếu. Dòng món lưu tên và đơn giá tại thời điểm gọi. Chi tiết hóa đơn lưu bản sao dữ liệu khi chốt tiền. Doanh thu lấy theo thời điểm thanh toán, không lấy theo lúc gửi đơn.

### 10. Thứ tự ghép, chạy thử và bàn giao

1. Chạy bộ khung chung: cài .NET 10 SDK, chạy dotnet run --project src/Qresto từ thư mục repo; dùng địa chỉ ứng dụng in ra hoặc cấu hình trong README.md. Kiểm tra tài khoản mẫu, bàn, QR và thực đơn.
2. Ghép luồng chính trước: QR -> chọn món -> gửi đơn và tạo phiên -> phục vụ xác nhận -> bếp làm -> phục vụ giao -> thu tiền -> xem hóa đơn.
3. Ghép ngoại lệ sau: gọi thêm, hai điện thoại cùng một bàn, hủy, sự cố, mất mạng, món đổi giá, chuyển về phục vụ và dùng lại bàn sau khi đóng phiên.
4. Kiểm tra bằng đúng thiết bị demo: dùng một điện thoại hoặc màn hình hẹp cho khách, một trình duyệt riêng cho phục vụ và bếp. Thử nội bộ bằng cùng Wi‑Fi với QR trỏ tới IP máy chạy web. Khi cần người dùng mạng khác, dùng địa chỉ web công khai và tạo QR theo địa chỉ đó; QR trỏ tới localhost hoặc IP nội bộ không dùng được từ mạng khác.
5. Đồng bộ trên GitHub: mỗi người làm nhánh riêng, mở pull request và nêu phần đã chạy thử. Nhóm kiểm tra rồi mới ghép vào main. Tài liệu và sơ đồ phải khớp code; không đưa cơ sở dữ liệu cá nhân hoặc thông tin đăng nhập thật lên repo.

| Kiểm thử trước khi bàn giao | Kết quả phải đạt |
| --- | --- |
| Cuộn xuống giữa thực đơn, thêm cùng một món hai lần, mở rồi đóng giỏ | Thấy − 2 + ngay trên thẻ, xem được đúng món trong giỏ, đóng giỏ không nhảy lên đầu trang. Chưa bấm Gửi đơn thì phục vụ chưa nhận món. |
| Hai điện thoại gửi đơn đầu tiên gần như đồng thời | Một phiên của bàn, hai đơn hợp lệ. Thử lại cùng mã của một đơn không tạo đơn thứ ba. |
| Một khách thử yêu cầu hủy món của người khác | Hệ thống từ chối, danh sách món chung vẫn xem được. |
| Phục vụ bắt đầu thanh toán khi còn món chưa giao | Bàn chờ thanh toán và khóa gọi thêm. Giao diện báo lý do, chưa cho xác nhận đã thu. |
| Hai nhân viên cùng xác nhận sau khi giao đủ món | Chỉ có một hóa đơn, tổng tiền đúng và phiên đã đóng. |
| Quét lại QR sau khi phiên kết thúc | Khách mới xem được thực đơn và gửi đơn đầu tiên cho phiên mới. Trang cũ không thao tác được vào lượt mới. |


**Tiêu chí hoàn thành:** Cả nhóm chạy được một lượt phục vụ từ quét QR đến hóa đơn trên cùng bản tích hợp. Khi báo tiến độ, ghi rõ mục nào đã thiết kế, mục nào đã tích hợp vào bản đang chạy, và mục nào đã được kiểm thử. Bản nghiệp vụ này là căn cứ để đối chiếu, không thay cho kết quả chạy thử của từng phiên bản.