using System.ComponentModel.DataAnnotations;

namespace Qresto.Models;

// Cac enum luu xuong DB duoi dang chuoi (xem AppDbContext) cho de doc khi mo file SQLite.

public enum MenuItemStatus
{
    [Display(Name = "Đang bán")] DangBan,
    [Display(Name = "Tạm hết")] TamHet,
    [Display(Name = "Ngừng bán")] NgungBan
}

public enum SessionStatus
{
    DangPhucVu,
    ChoThanhToan,
    DaDong,
    DaHuy
}

public enum OrderItemStatus
{
    ChoXacNhan,
    ChoCheBien,
    DangCheBien,
    SanSangPhucVu,
    DaPhucVu,
    BiTuChoi,
    DaHuy
}

public enum CancelRequestStatus
{
    ChoDuyet,
    DaDuyet,
    KhongDuyet
}

public enum SupportType
{
    GoiNhanVien,
    XinDungCu
}

public enum SupportStatus
{
    DangMo,
    DaTiepNhan,
    HoanTat
}

public enum PaymentMethod
{
    TienMat,
    ChuyenKhoan
}

public enum ActorType
{
    Khach,
    NhanVien,
    HeThong
}
