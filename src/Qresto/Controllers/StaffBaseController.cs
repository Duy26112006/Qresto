using Microsoft.AspNetCore.Mvc;
using Qresto.Helpers;
using Qresto.Services;

namespace Qresto.Controllers;

// Controller chung cho cac man hinh nhan vien.
// Nut thao tac gui bang fetch (xem staff.js) thi tra JSON, khong co JS thi quay lai trang truoc.
public abstract class StaffBaseController : Controller
{
    protected StaffActor Actor => StaffActor.FromUser(User);

    protected bool IsAjax => Request.Headers["X-Requested-With"] == "fetch";

    protected async Task<IActionResult> RunAsync(Func<Task> action, string successMessage, string? returnUrl = null)
    {
        try
        {
            await action();
            return Done(true, successMessage, returnUrl);
        }
        catch (BusinessException ex)
        {
            return Done(false, ex.Message, returnUrl);
        }
    }

    protected IActionResult Done(bool ok, string message, string? returnUrl)
    {
        if (IsAjax)
            return Json(new { ok, message });

        TempData[ok ? "ThongBao" : "Loi"] = message;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri)
            && uri.Host == Request.Host.Host)
            return Redirect(uri.PathAndQuery);

        return RedirectToAction("Index", "Home");
    }
}
