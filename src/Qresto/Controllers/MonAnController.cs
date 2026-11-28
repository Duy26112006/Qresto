using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Qresto.Helpers;
using Qresto.Models;
using Qresto.Services;

namespace Qresto.Controllers;

[Authorize(Roles = Roles.QuanLy)]
public class MonAnController : StaffBaseController
{
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSize = 2 * 1024 * 1024;

    private readonly MenuService _menu;
    private readonly IWebHostEnvironment _env;

    public MonAnController(MenuService menu, IWebHostEnvironment env)
    {
        _menu = menu;
        _env = env;
    }

    public async Task<IActionResult> Index(int? danhMuc, string? q)
    {
        ViewBag.DanhMuc = danhMuc;
        ViewBag.Q = q;
        ViewBag.Categories = await _menu.GetCategoriesAsync();
        return View(await _menu.GetItemsAsync(danhMuc, q));
    }

    public async Task<IActionResult> Sua(int? id)
    {
        MenuItem item;
        if (id == null)
            item = new MenuItem { PrepTimeMinutes = 10 };
        else
            item = await _menu.GetItemAsync(id.Value) ?? new MenuItem();
        await LoadCategories();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(MenuItem model, IFormFile? anh)
    {
        try
        {
            if (anh != null && anh.Length > 0)
                model.ImageUrl = await SaveImageAsync(anh);
            else
                model.ImageUrl = null; // giu anh cu

            await _menu.SaveItemAsync(model);
            TempData["ThongBao"] = "Đã lưu món " + model.Name + ".";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategories();
            if (model.Id != 0)
                model.ImageUrl = (await _menu.GetItemAsync(model.Id))?.ImageUrl;
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DoiTinhTrang(int id, MenuItemStatus status) =>
        RunAsync(() => _menu.ChangeStatusAsync(id, status, Actor), "Đã đổi tình trạng món.", Url.Action(nameof(Index)));

    private async Task LoadCategories()
    {
        var cats = await _menu.GetCategoriesAsync();
        ViewBag.CategoryList = new SelectList(cats, "Id", "Name");
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext))
            throw new BusinessException(ErrorCodes.InvalidInput, "Ảnh chỉ nhận file .jpg, .png hoặc .webp.");
        if (file.Length > MaxImageSize)
            throw new BusinessException(ErrorCodes.InvalidInput, "Ảnh tối đa 2MB.");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "mon");
        Directory.CreateDirectory(folder);
        var fileName = Guid.NewGuid().ToString("N") + ext;
        await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
        {
            await file.CopyToAsync(stream);
        }
        return "/uploads/mon/" + fileName;
    }
}
