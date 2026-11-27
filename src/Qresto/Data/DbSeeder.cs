using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qresto.Helpers;
using Qresto.Models;
using Qresto.Services;

namespace Qresto.Data;

// Du lieu mau de ca nhom chay thu. Chi them khi bang con trong.
public static class DbSeeder
{
    public const string DemoPassword = "qresto123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await AddUserAsync(userManager, "quanly", "Nguyễn Văn Quản", Roles.QuanLy);
        await AddUserAsync(userManager, "phucvu1", "Trần Thị Mai", Roles.PhucVu);
        await AddUserAsync(userManager, "phucvu2", "Lê Văn Hùng", Roles.PhucVu);
        await AddUserAsync(userManager, "bep1", "Phạm Văn Bếp", Roles.Bep);

        if (!await db.DiningTables.AnyAsync())
        {
            for (int i = 1; i <= 10; i++)
            {
                db.DiningTables.Add(new DiningTable
                {
                    Code = $"B{i:00}",
                    Name = $"Bàn {i:00}",
                    QrToken = TableService.NewQrToken()
                });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.Categories.AnyAsync())
        {
            var khaiVi = new Category { Name = "Khai vị", DisplayOrder = 1 };
            var monChinh = new Category { Name = "Món chính", DisplayOrder = 2 };
            var comMi = new Category { Name = "Cơm - Mì - Bún", DisplayOrder = 3 };
            var doUong = new Category { Name = "Đồ uống", DisplayOrder = 4 };
            var trangMieng = new Category { Name = "Tráng miệng", DisplayOrder = 5 };
            db.Categories.AddRange(khaiVi, monChinh, comMi, doUong, trangMieng);
            await db.SaveChangesAsync();

            db.MenuItems.AddRange(
                Item(khaiVi, "Gỏi cuốn tôm thịt", 35000, 8, "2 cuốn, kèm nước chấm tương đậu phộng"),
                Item(khaiVi, "Chả giò rế", 45000, 10, "Chiên giòn, ăn kèm rau sống"),
                Item(khaiVi, "Khoai tây chiên", 30000, 7, null),
                Item(khaiVi, "Gỏi ngó sen tôm thịt", 65000, 10, null),
                Item(monChinh, "Gà nướng mật ong", 120000, 20, "Nửa con"),
                Item(monChinh, "Cá lóc kho tộ", 95000, 18, null),
                Item(monChinh, "Bò lúc lắc", 135000, 15, "Kèm khoai tây chiên"),
                Item(monChinh, "Rau muống xào tỏi", 40000, 6, null),
                Item(comMi, "Cơm sườn nướng", 55000, 12, "Sườn cốt lết, trứng ốp la"),
                Item(comMi, "Cơm chiên hải sản", 65000, 12, null),
                Item(comMi, "Mì xào bò", 60000, 12, null),
                Item(comMi, "Bún thịt nướng", 50000, 10, null),
                Item(doUong, "Trà đào cam sả", 30000, 4, null),
                Item(doUong, "Cà phê sữa đá", 25000, 3, null),
                Item(doUong, "Nước cam ép", 35000, 4, null),
                Item(doUong, "Nước suối", 10000, 1, null),
                Item(trangMieng, "Chè khúc bạch", 30000, 3, null),
                Item(trangMieng, "Bánh flan", 20000, 2, null)
            );
            await db.SaveChangesAsync();
        }
    }

    // Thu muc anh mau: wwwroot/images/mon/{ten-khong-dau}.jpg, vd goi-cuon-tom-thit.jpg
    public static string WebRoot { get; set; } = "";

    private static MenuItem Item(Category c, string name, int price, int prep, string? desc) => new()
    {
        CategoryId = c.Id,
        Name = name,
        Price = price,
        PrepTimeMinutes = prep,
        Description = desc,
        Status = MenuItemStatus.DangBan,
        ImageUrl = FindImage(name)
    };

    public static string Slug(string name)
    {
        var s = name.ToLowerInvariant().Replace("đ", "d").Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
    }

    private static string? FindImage(string name)
    {
        foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".webp" })
        {
            var file = Slug(name) + ext;
            if (File.Exists(Path.Combine(WebRoot, "images", "mon", file)))
                return "/images/mon/" + file;
        }
        return null;
    }

    private static async Task AddUserAsync(UserManager<AppUser> um, string userName, string fullName, string role)
    {
        if (await um.FindByNameAsync(userName) != null)
            return;
        var user = new AppUser { UserName = userName, FullName = fullName, IsActive = true };
        var result = await um.CreateAsync(user, DemoPassword);
        if (result.Succeeded)
            await um.AddToRoleAsync(user, role);
    }
}
