using Microsoft.AspNetCore.Mvc;
using Qresto.Helpers;

namespace Qresto.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Nhan vien da dang nhap thi vao thang man hinh cua minh
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(Roles.QuanLy)) return RedirectToAction("Index", "QuanLy");
            if (User.IsInRole(Roles.PhucVu)) return RedirectToAction("Index", "PhucVu");
            if (User.IsInRole(Roles.Bep)) return RedirectToAction("Index", "Bep");
        }
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
