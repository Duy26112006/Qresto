namespace Qresto.Models;

// Lich su xu ly: ai lam gi, luc nao, ly do gi
public class ActivityLog
{
    public int Id { get; set; }
    public int? DiningSessionId { get; set; }
    public int? OrderItemId { get; set; }

    public string Action { get; set; } = "";
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Reason { get; set; }

    public ActorType ActorType { get; set; }
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }

    public DateTime At { get; set; } = DateTime.Now;
}
