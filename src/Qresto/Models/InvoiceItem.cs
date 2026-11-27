namespace Qresto.Models;

// Ban sao dong mon luc chot tien
public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    // Chi de truy vet, co the null
    public int? OrderItemId { get; set; }
    public int? MenuItemId { get; set; }

    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public int LineTotal { get; set; }
}
