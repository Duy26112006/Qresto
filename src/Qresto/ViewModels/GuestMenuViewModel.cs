using Qresto.Models;

namespace Qresto.ViewModels;

public class GuestMenuViewModel
{
    public DiningTable Table { get; set; } = null!;
    public int? SessionId { get; set; }
    public SessionStatus? SessionStatus { get; set; }
    public int Turn { get; set; }
    public List<Category> Categories { get; set; } = new();
}

public class GuestCancelInput
{
    public int SessionId { get; set; }
    public int OrderItemId { get; set; }
    public string Reason { get; set; } = "";
}

public class GuestSupportInput
{
    public int SessionId { get; set; }
    public string Type { get; set; } = "";
    public string? Note { get; set; }
}

public class GuestSessionInput
{
    public int SessionId { get; set; }
}
