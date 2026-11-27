namespace Qresto.Models;

// Moi lan khach bam Gui don thanh cong tao ra 1 Order
public class Order
{
    public int Id { get; set; }
    public int DiningSessionId { get; set; }
    public DiningSession? DiningSession { get; set; }

    // Trinh duyet da gui don, dung de kiem tra quyen yeu cau huy
    public string GuestKey { get; set; } = "";

    // Ma chong gui trung do trinh duyet sinh ra
    public string ClientRequestId { get; set; } = "";

    // Dau van tay noi dung gio, de phat hien cung ma nhung khac noi dung
    public string ContentHash { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<OrderItem> Items { get; set; } = new();
}
