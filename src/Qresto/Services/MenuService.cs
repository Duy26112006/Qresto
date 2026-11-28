using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Helpers;
using Qresto.Models;

namespace Qresto.Services;

public class MenuService
{
    private readonly AppDbContext _db;
    private readonly IStaffNotifier _notifier;

    public MenuService(AppDbContext db, IStaffNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    // Thuc don cho khach: danh muc dang dung, mon chua ngung ban (mon tam het van hien nhung khong dat duoc)
    public async Task<List<Category>> GetGuestMenuAsync()
    {
        var cats = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();
        var items = await _db.MenuItems.AsNoTracking()
            .Where(m => m.Status != MenuItemStatus.NgungBan)
            .OrderBy(m => m.Name)
            .ToListAsync();
        foreach (var c in cats)
            c.MenuItems = items.Where(m => m.CategoryId == c.Id).ToList();
        return cats.Where(c => c.MenuItems.Count > 0).ToList();
    }

    public Task<List<Category>> GetCategoriesAsync()
    {
        return _db.Categories.Include(c => c.MenuItems)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
    }

    public Task<List<MenuItem>> GetItemsAsync(int? categoryId = null, string? keyword = null)
    {
        var q = _db.MenuItems.Include(m => m.Category).AsQueryable();
        if (categoryId != null)
            q = q.Where(m => m.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(m => m.Name.Contains(keyword.Trim()));
        return q.OrderBy(m => m.Category!.DisplayOrder).ThenBy(m => m.Name).ToListAsync();
    }

    public Task<MenuItem?> GetItemAsync(int id) =>
        _db.MenuItems.Include(m => m.Category).FirstOrDefaultAsync(m => m.Id == id);

    public async Task SaveCategoryAsync(int? id, string name, int displayOrder, bool isActive)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0)
            throw new BusinessException(ErrorCodes.InvalidInput, "Tên danh mục không được để trống.");
        if (await _db.Categories.AnyAsync(c => c.Name == name && c.Id != (id ?? 0)))
            throw new BusinessException(ErrorCodes.InvalidInput, "Tên danh mục đã tồn tại.");

        Category cat;
        if (id == null)
        {
            cat = new Category();
            _db.Categories.Add(cat);
        }
        else
        {
            cat = await _db.Categories.FindAsync(id.Value)
                ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy danh mục.");
        }
        cat.Name = name;
        cat.DisplayOrder = displayOrder;
        cat.IsActive = isActive;
        await _db.SaveChangesAsync();
        await _notifier.NotifyAsync("thuc-don");
    }

    public async Task<MenuItem> SaveItemAsync(MenuItem input)
    {
        input.Name = (input.Name ?? "").Trim();
        if (input.Name.Length == 0)
            throw new BusinessException(ErrorCodes.InvalidInput, "Tên món không được để trống.");
        if (input.Price <= 0)
            throw new BusinessException(ErrorCodes.InvalidInput, "Giá món phải lớn hơn 0.");
        if (input.PrepTimeMinutes <= 0)
            throw new BusinessException(ErrorCodes.InvalidInput, "Thời gian chế biến phải lớn hơn 0.");
        if (!await _db.Categories.AnyAsync(c => c.Id == input.CategoryId))
            throw new BusinessException(ErrorCodes.InvalidInput, "Vui lòng chọn danh mục.");

        MenuItem item;
        if (input.Id == 0)
        {
            item = new MenuItem();
            _db.MenuItems.Add(item);
        }
        else
        {
            item = await _db.MenuItems.FindAsync(input.Id)
                ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy món.");
        }

        // Doi gia chi anh huong don gui sau nay, dong mon cu da luu UnitPrice rieng
        item.Name = input.Name;
        item.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        item.Price = input.Price;
        item.CategoryId = input.CategoryId;
        item.PrepTimeMinutes = input.PrepTimeMinutes;
        item.Status = input.Status;
        if (input.ImageUrl != null)
            item.ImageUrl = input.ImageUrl;

        await _db.SaveChangesAsync();
        await _notifier.NotifyAsync("thuc-don");
        return item;
    }

    // Bep chi doi qua lai Dang ban / Tam het. Quan ly duoc them Ngung ban.
    public async Task ChangeStatusAsync(int menuItemId, MenuItemStatus status, StaffActor actor)
    {
        if (!actor.IsKitchen)
            throw new BusinessException(ErrorCodes.NotAllowed, "Bạn không có quyền đổi tình trạng món.");
        if (status == MenuItemStatus.NgungBan && !actor.IsManager)
            throw new BusinessException(ErrorCodes.NotAllowed, "Chỉ quản lý được ngừng bán món.");

        var item = await _db.MenuItems.FindAsync(menuItemId)
            ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy món.");
        if (item.Status == MenuItemStatus.NgungBan && !actor.IsManager)
            throw new BusinessException(ErrorCodes.NotAllowed, "Món đã ngừng bán, chỉ quản lý được mở lại.");

        if (item.Status != status)
        {
            _db.AddLog("DoiTinhTrangMon", actor, from: item.Status.ToString(), to: status.ToString(), reason: item.Name);
            item.Status = status;
            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync("thuc-don");
        }
    }
}
