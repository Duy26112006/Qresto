using Microsoft.AspNetCore.Mvc;
using Qresto.Helpers;
using Qresto.Models;
using Qresto.Services;
using Qresto.ViewModels;

namespace Qresto.Controllers;

// Trang goi mon cua khach: /ban/{token}. Khach khong dang nhap, duoc nhan dien bang cookie GuestKey.
[Route("ban/{token}")]
public class GoiMonController : Controller
{
    private const string GuestCookie = "qresto_khach";

    private readonly DiningSessionService _sessions;
    private readonly MenuService _menu;
    private readonly OrderService _orders;
    private readonly GuestStatusService _status;

    public GoiMonController(DiningSessionService sessions, MenuService menu, OrderService orders,
        GuestStatusService status)
    {
        _sessions = sessions;
        _menu = menu;
        _orders = orders;
        _status = status;
    }

    private string GuestKey()
    {
        var key = Request.Cookies[GuestCookie];
        if (string.IsNullOrEmpty(key) || key.Length > 64)
        {
            key = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(GuestCookie, key, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.Now.AddDays(1)
            });
        }
        return key;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string token)
    {
        try
        {
            var ctx = await _sessions.EnterTableAsync(token, GuestKey());
            var vm = new GuestMenuViewModel
            {
                Table = ctx.Table,
                SessionId = ctx.ActiveSession?.Id,
                SessionStatus = ctx.ActiveSession?.Status,
                Turn = ctx.TurnVersion,
                Categories = await _menu.GetGuestMenuAsync()
            };
            return View(vm);
        }
        catch (BusinessException ex)
        {
            ViewBag.Message = ex.Message;
            return View("QrLoi");
        }
    }

    [HttpGet("trang-thai")]
    public async Task<IActionResult> TrangThai(string token, int? sessionId, int turn)
    {
        var dto = await _status.GetAsync(token, sessionId, turn, GuestKey());
        return Json(dto);
    }

    [HttpPost("gui-don")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> GuiDon(string token, [FromBody] SubmitOrderRequest req) =>
        Api(async () => await _orders.SubmitAsync(token, req, GuestKey()));

    // Boc loi nghiep vu thanh JSON co ma loi de JS xu ly
    private async Task<IActionResult> Api<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Json(new { ok = true, data = result });
        }
        catch (BusinessException ex)
        {
            Response.StatusCode = ex.Code switch
            {
                ErrorCodes.InvalidInput => 400,
                ErrorCodes.NoAccess or ErrorCodes.NotAllowed => 403,
                ErrorCodes.NotFound or ErrorCodes.TableNotFound => 404,
                _ => 409
            };
            return Json(new { ok = false, code = ex.Code, message = ex.Message, data = ex.Payload });
        }
    }
}
