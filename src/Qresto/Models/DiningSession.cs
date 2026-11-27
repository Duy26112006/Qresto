namespace Qresto.Models;

// Phien phuc vu = mot luot khach ngoi ban
public class DiningSession
{
    public int Id { get; set; }
    public int DiningTableId { get; set; }
    public DiningTable? DiningTable { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.DangPhucVu;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? EndedAt { get; set; }

    // Tang moi khi co thay doi anh huong toi tien hoac dieu kien thanh toan.
    // Man hinh thu tien gui lai so nay de server biet du lieu chua bi doi.
    public int PaymentVersion { get; set; }

    public string? CancelReason { get; set; }
    public string? EndedById { get; set; }

    public List<Order> Orders { get; set; } = new();
    public List<GuestSessionAccess> Accesses { get; set; } = new();
    public List<SupportRequest> SupportRequests { get; set; } = new();
    public Invoice? Invoice { get; set; }

    public bool IsActive => Status == SessionStatus.DangPhucVu || Status == SessionStatus.ChoThanhToan;
}
