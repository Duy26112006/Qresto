namespace Qresto.ViewModels;

// Du lieu trang khach lay dinh ky (polling) tu server
public class GuestStatusDto
{
    public bool Stale { get; set; }
    public string? Message { get; set; }

    public int? SessionId { get; set; }
    public string? SessionStatus { get; set; }
    public string? SessionStatusText { get; set; }
    public bool CanOrder { get; set; }

    public List<GuestItemDto> Items { get; set; } = new();
    public int Subtotal { get; set; }
    public int PendingTotal { get; set; }
    public List<string> OpenSupport { get; set; } = new();

    // tinh trang va gia moi nhat cua mon, de the mon tren trang cap nhat theo
    public List<MenuStateDto> Menu { get; set; } = new();
}

public class GuestItemDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string? Reason { get; set; }
    public bool Mine { get; set; }
    public bool CanRequestCancel { get; set; }
    public string? CancelRequestStatus { get; set; }
    public string Time { get; set; } = "";
}

public class MenuStateDto
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public int Price { get; set; }
}
