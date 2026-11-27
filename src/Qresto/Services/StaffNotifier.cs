using Microsoft.AspNetCore.SignalR;
using Qresto.Helpers;
using Qresto.Hubs;

namespace Qresto.Services;

public interface IStaffNotifier
{
    // loai: "don-moi", "mon", "ho-tro", "huy", "phien", "thuc-don"
    Task NotifyAsync(string loai, int? tableId = null);
}

public class StaffNotifier : IStaffNotifier
{
    private readonly IHubContext<StaffHub> _hub;
    private readonly ILogger<StaffNotifier> _logger;

    public StaffNotifier(IHubContext<StaffHub> hub, ILogger<StaffNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyAsync(string loai, int? tableId = null)
    {
        try
        {
            // Chi gui cho nhom nhan vien. Du lieu that thi man hinh tu tai lai tu server.
            await _hub.Clients.Groups(Roles.PhucVu, Roles.Bep, Roles.QuanLy)
                .SendAsync("capNhat", new { loai, banId = tableId });
        }
        catch (Exception ex)
        {
            // Thong bao loi khong duoc lam hong thao tac da luu
            _logger.LogWarning(ex, "Khong gui duoc thong bao SignalR");
        }
    }
}
