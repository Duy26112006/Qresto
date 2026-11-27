namespace Qresto.Helpers;

public static class Roles
{
    public const string PhucVu = "PhucVu";
    public const string Bep = "Bep";
    public const string QuanLy = "QuanLy";

    // Dung trong [Authorize(Roles = ...)]
    public const string PhucVuHoacQuanLy = PhucVu + "," + QuanLy;
    public const string BepHoacQuanLy = Bep + "," + QuanLy;
    public const string TatCaNhanVien = PhucVu + "," + Bep + "," + QuanLy;

    public static readonly string[] All = { PhucVu, Bep, QuanLy };

    public static string TenHienThi(string role) => role switch
    {
        PhucVu => "Phục vụ",
        Bep => "Bếp",
        QuanLy => "Quản lý",
        _ => role
    };
}
