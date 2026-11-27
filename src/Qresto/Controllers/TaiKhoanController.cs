using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Qresto.Models;

namespace Qresto.Controllers;

public class LoginViewModel
{
    [Required(ErrorMessage = "Nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class TaiKhoanController : Controller
{
    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;

    public TaiKhoanController(SignInManager<AppUser> signIn, UserManager<AppUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpGet]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _users.FindByNameAsync(model.UserName.Trim());
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError("", user == null ? "Sai tên đăng nhập hoặc mật khẩu." : "Tài khoản đã bị khóa.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Đăng nhập sai nhiều lần, vui lòng thử lại sau 5 phút.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangXuat()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(DangNhap));
    }

    public IActionResult KhongCoQuyen()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult DoiMatKhau()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _users.GetUserAsync(User);
        if (user == null)
            return RedirectToAction(nameof(DangNhap));

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Mật khẩu hiện tại không đúng hoặc mật khẩu mới chưa hợp lệ (ít nhất 6 ký tự, có chữ số).");
            return View(model);
        }

        await _signIn.RefreshSignInAsync(user);
        TempData["ThongBao"] = "Đã đổi mật khẩu.";
        return RedirectToAction("Index", "Home");
    }
}
