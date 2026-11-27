namespace Qresto.Models;

public class SupportRequest
{
    public int Id { get; set; }
    public int DiningSessionId { get; set; }
    public DiningSession? DiningSession { get; set; }

    public SupportType Type { get; set; }
    public string? Note { get; set; }
    public SupportStatus Status { get; set; } = SupportStatus.DangMo;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? HandledById { get; set; }
}
