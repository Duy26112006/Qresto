using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qresto.Helpers;
using Qresto.Services;

namespace Qresto.Controllers;

[Authorize(Roles = Roles.QuanLy)]
public class BanController : StaffBaseController
{
    private readonly TableService _tables;
    private readonly IConfiguration _config;

    public BanController(TableService tables, IConfiguration config)
    {
        _tables = tables;
        _config = config;
    }

    // Dia chi goc de tao link trong QR. Neu chua cau hinh thi lay theo dia chi dang truy cap.
    // Luu y: QR tro toi localhost thi dien thoai khong mo duoc, can dung IP may chay web hoac dia chi cong khai.
    private string BaseUrl()
    {
        var cfg = _config["Qresto:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(cfg))
            return cfg.TrimEnd('/');
        return $"{Request.Scheme}://{Request.Host}";
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.BaseUrl = BaseUrl();
        return View(await _tables.GetAllAsync());
    }

    // Trang in QR cho tat ca ban
    public async Task<IActionResult> InQr(int? id)
    {
        ViewBag.BaseUrl = BaseUrl();
        var tables = await _tables.GetAllAsync();
        if (id != null)
            tables = tables.Where(t => t.Id == id).ToList();
        return View(tables.Where(t => t.IsActive).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Them(string code, string name) =>
        RunAsync(() => _tables.CreateAsync(code ?? "", name ?? ""), "Đã thêm bàn.", Url.Action(nameof(Index)));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Sua(int id, string code, string name) =>
        RunAsync(() => _tables.UpdateAsync(id, code ?? "", name ?? ""), "Đã cập nhật bàn.", Url.Action(nameof(Index)));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DoiQr(int id) =>
        RunAsync(() => _tables.RegenerateQrAsync(id), "Đã tạo mã QR mới. Nhớ in lại QR cho bàn này.", Url.Action(nameof(Index)));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DoiTrangThai(int id, bool active) =>
        RunAsync(() => _tables.SetActiveAsync(id, active), active ? "Bàn đã dùng lại." : "Đã ngừng sử dụng bàn.", Url.Action(nameof(Index)));
}
