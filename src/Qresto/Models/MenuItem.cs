namespace Qresto.Models;

public class MenuItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = "";
    public string? Description { get; set; }

    // Gia VND, so nguyen
    public int Price { get; set; }

    public string? ImageUrl { get; set; }
    public MenuItemStatus Status { get; set; } = MenuItemStatus.DangBan;

    // Thoi gian che bien du kien, dung de canh bao cho lau o man hinh bep
    public int PrepTimeMinutes { get; set; } = 10;
}
