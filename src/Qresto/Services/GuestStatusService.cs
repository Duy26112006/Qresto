using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Helpers;
using Qresto.Models;
using Qresto.ViewModels;

namespace Qresto.Services;

public class GuestStatusService
{
    private readonly AppDbContext _db;
    private readonly DiningSessionService _sessions;

    public GuestStatusService(AppDbContext db, DiningSessionService sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    private static GuestStatusDto StaleResult() => new()
    {
        Stale = true,
        Message = "Lượt phục vụ này đã kết thúc. Quét lại mã QR trên bàn (hoặc tải lại trang) để bắt đầu lượt mới."
    };

    public async Task<GuestStatusDto> GetAsync(string qrToken, int? pageSessionId, int pageTurn, string guestKey)
    {
        var table = await _db.DiningTables.AsNoTracking().FirstOrDefaultAsync(t => t.QrToken == qrToken);
        if (table == null || !table.IsActive)
            return new GuestStatusDto { Stale = true, Message = "Mã QR không hợp lệ hoặc bàn đang ngừng sử dụng." };

        var dto = new GuestStatusDto();
        var menu = await _db.MenuItems.AsNoTracking().ToListAsync();
        dto.Menu = menu.Select(m => new MenuStateDto { Id = m.Id, Status = m.Status.ToString(), Price = m.Price }).ToList();

        DiningSession? session;
        if (pageSessionId == null)
        {
            if (pageTurn != table.TurnVersion)
                return StaleResult();

            session = await _sessions.GetActiveSessionAsync(table.Id);
            if (session == null)
            {
                dto.CanOrder = true; // ban trong, gui don dau tien se tao phien
                return dto;
            }
            // Nguoi cung ban vua gui don dau tien -> trang nay vao chung phien
            await _sessions.GrantAccessAsync(session.Id, guestKey, save: true);
        }
        else
        {
            try
            {
                session = await _sessions.RequireGuestSessionAsync(qrToken, pageSessionId.Value, guestKey);
            }
            catch (BusinessException ex)
            {
                return new GuestStatusDto { Stale = true, Message = ex.Message };
            }
        }

        dto.SessionId = session.Id;
        dto.SessionStatus = session.Status.ToString();
        dto.SessionStatusText = session.Status.Text();
        dto.CanOrder = session.Status == SessionStatus.DangPhucVu;

        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.CancelRequests)
            .Where(o => o.DiningSessionId == session.Id)
            .OrderBy(o => o.CreatedAt).ToListAsync();

        foreach (var o in orders)
        {
            foreach (var i in o.Items.OrderBy(i => i.Id))
            {
                var lastCancel = i.CancelRequests.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
                bool mine = o.GuestKey == guestKey;
                dto.Items.Add(new GuestItemDto
                {
                    Id = i.Id,
                    OrderId = o.Id,
                    Name = i.ItemName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Note = i.Note,
                    Status = i.Status.ToString(),
                    StatusText = i.Status.Text(),
                    Reason = i.Status == OrderItemStatus.BiTuChoi ? i.RejectReason
                           : i.Status == OrderItemStatus.DaHuy ? i.CancelReason : null,
                    Mine = mine,
                    CancelRequestStatus = lastCancel?.Status.Text(),
                    CanRequestCancel = mine
                        && (i.Status == OrderItemStatus.ChoXacNhan || i.Status == OrderItemStatus.ChoCheBien)
                        && !i.CancelRequests.Any(c => c.Status == Models.CancelRequestStatus.ChoDuyet),
                    Time = o.CreatedAt.ToString("HH:mm")
                });
            }
        }

        var all = orders.SelectMany(o => o.Items).ToList();
        dto.Subtotal = all.Where(i => i.IsChargeable).Sum(i => i.LineTotal);
        dto.PendingTotal = all.Where(i => i.Status == OrderItemStatus.ChoXacNhan).Sum(i => i.LineTotal);
        var supports = await _db.SupportRequests.AsNoTracking()
            .Where(r => r.DiningSessionId == session.Id && r.Status != SupportStatus.HoanTat)
            .ToListAsync();
        dto.OpenSupport = supports.Select(r => r.Type.ToString()).ToList();
        return dto;
    }
}
