namespace Qresto.Models;

public class DiningTable
{
    public int Id { get; set; }

    // Ma ngan de nhan vien goi, vd B03
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    // Chuoi ngau nhien nam trong link QR. Doi QR = sinh chuoi moi, QR cu het hieu luc.
    public string QrToken { get; set; } = "";

    public bool IsActive { get; set; } = true;

    // Tang len 1 moi khi mot luot khach ket thuc (dong hoac huy phien).
    // Trang thuc don ghi lai gia tri nay luc tai de biet trang do thuoc luot nao.
    public int TurnVersion { get; set; }

    public List<DiningSession> Sessions { get; set; } = new();
}
