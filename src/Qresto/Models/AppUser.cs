using Microsoft.AspNetCore.Identity;

namespace Qresto.Models;

// Tai khoan nhan vien (phuc vu, bep, quan ly). Khach khong co tai khoan.
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";

    // Khong xoa nhan vien da co lich su, chi khoa lai
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
