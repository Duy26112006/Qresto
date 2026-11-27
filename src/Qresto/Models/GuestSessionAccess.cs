namespace Qresto.Models;

// Quyen cua mot trinh duyet (GuestKey trong cookie) doi voi mot phien.
// Khi phien dong hoac huy thi RevokedAt duoc ghi, trang cu khong thao tac duoc nua.
public class GuestSessionAccess
{
    public int Id { get; set; }
    public int DiningSessionId { get; set; }
    public DiningSession? DiningSession { get; set; }

    public string GuestKey { get; set; } = "";
    public DateTime IssuedAt { get; set; } = DateTime.Now;
    public DateTime? RevokedAt { get; set; }
}
