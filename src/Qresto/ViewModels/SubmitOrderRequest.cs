namespace Qresto.ViewModels;

// Du lieu trinh duyet gui len khi bam Gui don
public class SubmitOrderRequest
{
    public string ClientRequestId { get; set; } = "";

    // Phien ma trang dang hien thi, null neu luc tai trang ban chua co phien
    public int? PageSessionId { get; set; }

    // Luot cua ban luc tai trang (DiningTable.TurnVersion)
    public int PageTurn { get; set; }

    public List<CartLine> Items { get; set; } = new();
}

public class CartLine
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }

    // Gia khach dang thay, chi de so sanh. Server khong dung gia nay de luu.
    public int Price { get; set; }
}

public class SubmitOrderResult
{
    public int OrderId { get; set; }
    public int SessionId { get; set; }
    public bool Duplicate { get; set; }
    public int ItemCount { get; set; }
}
