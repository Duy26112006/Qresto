using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Qresto.Models;

namespace Qresto.Data;

// Them ho ten vao cookie dang nhap de hien thi va ghi lich su ma khong phai truy van lai
public class AppClaimsFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public AppClaimsFactory(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options) : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("FullName", user.FullName));
        return identity;
    }
}
