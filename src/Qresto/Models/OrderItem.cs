namespace Qresto.Models;

// Dong mon trong don, vd "2 phan Goi cuon - khong dau phong"
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    // Luu lai ten va gia luc goi, sua thuc don sau nay khong anh huong
    public string ItemName { get; set; } = "";
    public int UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }

    public OrderItemStatus Status { get; set; } = OrderItemStatus.ChoXacNhan;

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? ServedAt { get; set; }

    public string? RejectReason { get; set; }
    public string? CancelReason { get; set; }

    // Su co la co tren dong mon, khong phai mot trang thai rieng
    public bool HasIncident { get; set; }
    public string? IncidentNote { get; set; }
    public DateTime? IncidentReportedAt { get; set; }

    // Phien ban dong, tang moi lan cap nhat. Dung de phat hien 2 nguoi cung sua.
    public int Version { get; set; }

    public List<CancelRequest> CancelRequests { get; set; } = new();

    public int LineTotal => UnitPrice * Quantity;

    public bool IsChargeable =>
        Status == OrderItemStatus.ChoCheBien || Status == OrderItemStatus.DangCheBien ||
        Status == OrderItemStatus.SanSangPhucVu || Status == OrderItemStatus.DaPhucVu;
}
