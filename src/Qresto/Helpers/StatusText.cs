using Qresto.Models;

namespace Qresto.Helpers;

// Doi enum sang chu tieng Viet de hien thi
public static class StatusText
{
    public static string Text(this OrderItemStatus s) => s switch
    {
        OrderItemStatus.ChoXacNhan => "Chờ xác nhận",
        OrderItemStatus.ChoCheBien => "Chờ chế biến",
        OrderItemStatus.DangCheBien => "Đang chế biến",
        OrderItemStatus.SanSangPhucVu => "Sẵn sàng phục vụ",
        OrderItemStatus.DaPhucVu => "Đã phục vụ",
        OrderItemStatus.BiTuChoi => "Bị từ chối",
        OrderItemStatus.DaHuy => "Đã hủy",
        _ => s.ToString()
    };

    // class mau cho badge bootstrap
    public static string Badge(this OrderItemStatus s) => s switch
    {
        OrderItemStatus.ChoXacNhan => "bg-warning text-dark",
        OrderItemStatus.ChoCheBien => "bg-info text-dark",
        OrderItemStatus.DangCheBien => "bg-primary",
        OrderItemStatus.SanSangPhucVu => "bg-success",
        OrderItemStatus.DaPhucVu => "bg-secondary",
        _ => "bg-light text-muted border"
    };

    public static string Text(this SessionStatus s) => s switch
    {
        SessionStatus.DangPhucVu => "Đang phục vụ",
        SessionStatus.ChoThanhToan => "Chờ thanh toán",
        SessionStatus.DaDong => "Đã đóng",
        SessionStatus.DaHuy => "Đã hủy",
        _ => s.ToString()
    };

    public static string Text(this MenuItemStatus s) => s switch
    {
        MenuItemStatus.DangBan => "Đang bán",
        MenuItemStatus.TamHet => "Tạm hết",
        MenuItemStatus.NgungBan => "Ngừng bán",
        _ => s.ToString()
    };

    public static string Text(this CancelRequestStatus s) => s switch
    {
        CancelRequestStatus.ChoDuyet => "Chờ duyệt",
        CancelRequestStatus.DaDuyet => "Đã duyệt",
        CancelRequestStatus.KhongDuyet => "Không duyệt",
        _ => s.ToString()
    };

    public static string Text(this SupportType t) => t switch
    {
        SupportType.GoiNhanVien => "Gọi nhân viên",
        SupportType.XinDungCu => "Xin thêm dụng cụ",
        _ => t.ToString()
    };

    public static string Text(this SupportStatus s) => s switch
    {
        SupportStatus.DangMo => "Mới",
        SupportStatus.DaTiepNhan => "Đã tiếp nhận",
        SupportStatus.HoanTat => "Hoàn tất",
        _ => s.ToString()
    };

    public static string Text(this PaymentMethod m) => m switch
    {
        PaymentMethod.TienMat => "Tiền mặt",
        PaymentMethod.ChuyenKhoan => "Chuyển khoản",
        _ => m.ToString()
    };

    public static string Vnd(this int soTien) => soTien.ToString("#,##0").Replace(",", ".") + " đ";
}
