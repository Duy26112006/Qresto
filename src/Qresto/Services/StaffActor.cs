using System.Security.Claims;
using Qresto.Helpers;

namespace Qresto.Services;

// Thong tin nhan vien dang thao tac, controller tao ra tu User roi truyen xuong service.
// Service tu kiem tra quyen lan nua chu khong chi dua vao viec an nut.
public record StaffActor(string Id, string Name, IReadOnlyCollection<string> RoleList)
{
    public bool IsManager => RoleList.Contains(Roles.QuanLy);
    public bool IsWaiter => RoleList.Contains(Roles.PhucVu) || IsManager;
    public bool IsKitchen => RoleList.Contains(Roles.Bep) || IsManager;

    public static StaffActor FromUser(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var name = user.FindFirstValue("FullName") ?? user.Identity?.Name ?? "";
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        return new StaffActor(id, name, roles);
    }
}
