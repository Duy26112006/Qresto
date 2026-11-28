using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qresto.Helpers;
using Qresto.Services;

namespace Qresto.Controllers;

[Authorize(Roles = Roles.QuanLy)]
public class DanhMucController : StaffBaseController
{
    private readonly MenuService _menu;

    public DanhMucController(MenuService menu)
    {
        _menu = menu;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _menu.GetCategoriesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Luu(int? id, string name, int displayOrder, bool isActive = true) =>
        RunAsync(() => _menu.SaveCategoryAsync(id, name, displayOrder, isActive),
            "Đã lưu danh mục.", Url.Action(nameof(Index)));
}
