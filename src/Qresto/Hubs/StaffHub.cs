using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Qresto.Helpers;

namespace Qresto.Hubs;

[Authorize(Roles = Roles.TatCaNhanVien)]
public class StaffHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        foreach (var role in Roles.All)
        {
            if (Context.User?.IsInRole(role) == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
        }
        await base.OnConnectedAsync();
    }
}
