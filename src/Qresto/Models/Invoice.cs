namespace Qresto.Models;

// Hoa don chi tao khi nhan vien xac nhan da thu tien. Moi phien toi da 1 hoa don.
public class Invoice
{
    public int Id { get; set; }
    public int DiningSessionId { get; set; }
    public DiningSession? DiningSession { get; set; }

    public string TableName { get; set; } = "";
    public int Total { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.Now;

    public string CashierId { get; set; } = "";
    public string CashierName { get; set; } = "";

    public List<InvoiceItem> Items { get; set; } = new();
}
