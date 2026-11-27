namespace Qresto.Models;

public class CancelRequest
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public string GuestKey { get; set; } = "";
    public string Reason { get; set; } = "";
    public CancelRequestStatus Status { get; set; } = CancelRequestStatus.ChoDuyet;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? HandledAt { get; set; }
    public string? HandledById { get; set; }
    public string? HandleNote { get; set; }
}
