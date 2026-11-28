using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Helpers;
using Qresto.Models;
using Qresto.ViewModels;

namespace Qresto.Services;

public class OrderService
{
    public const int MaxLines = 50;
    public const int MaxQuantity = 50;
    public const int MaxNoteLength = 300;

    private readonly AppDbContext _db;
    private readonly DiningSessionService _sessions;
    private readonly IStaffNotifier _notifier;

    public OrderService(AppDbContext db, DiningSessionService sessions, IStaffNotifier notifier)
    {
        _db = db;
        _sessions = sessions;
        _notifier = notifier;
    }

    public async Task<SubmitOrderResult> SubmitAsync(string qrToken, SubmitOrderRequest req, string guestKey)
    {
        Validate(req);
        string hash = ComputeHash(req.Items);

        SubmitOrderResult result;
        try
        {
            result = await _db.InTransactionAsync(() => SubmitInternalAsync(qrToken, req, guestKey, hash));
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Hai dien thoai cung tao phien, hoac cung ma gui den cung luc.
            // Ben thua thu lai 1 lan: luc nay phien (hoac don) da co nen se vao dung nhanh.
            result = await _db.InTransactionAsync(() => SubmitInternalAsync(qrToken, req, guestKey, hash));
        }

        if (!result.Duplicate)
        {
            var tableId = await _db.DiningSessions.Where(s => s.Id == result.SessionId)
                .Select(s => s.DiningTableId).FirstAsync();
            await _notifier.NotifyAsync("don-moi", tableId);
        }
        return result;
    }

    private static void Validate(SubmitOrderRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientRequestId) || req.ClientRequestId.Length > 64)
            throw new BusinessException(ErrorCodes.InvalidInput, "Yêu cầu không hợp lệ, vui lòng tải lại trang.");
        if (req.Items == null || req.Items.Count == 0)
            throw new BusinessException(ErrorCodes.InvalidInput, "Giỏ hàng đang trống.");
        if (req.Items.Count > MaxLines)
            throw new BusinessException(ErrorCodes.InvalidInput, $"Mỗi đơn tối đa {MaxLines} dòng món.");

        foreach (var line in req.Items)
        {
            if (line.Quantity < 1 || line.Quantity > MaxQuantity)
                throw new BusinessException(ErrorCodes.InvalidInput, $"Số lượng mỗi món phải từ 1 đến {MaxQuantity}.",
                    new { menuItemId = line.MenuItemId });
            if (line.Note != null && line.Note.Length > MaxNoteLength)
                throw new BusinessException(ErrorCodes.InvalidInput, $"Ghi chú tối đa {MaxNoteLength} ký tự.",
                    new { menuItemId = line.MenuItemId });
        }
    }

    // Noi dung gio duoc chuan hoa roi bam SHA256, de so sanh khi gui lai cung ma
    public static string ComputeHash(List<CartLine> items)
    {
        var text = string.Join(";", items
            .Select(i => $"{i.MenuItemId}|{i.Quantity}|{(i.Note ?? "").Trim()}")
            .OrderBy(s => s, StringComparer.Ordinal));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    private async Task<SubmitOrderResult> SubmitInternalAsync(string qrToken, SubmitOrderRequest req, string guestKey, string hash)
    {
        var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.QrToken == qrToken);
        if (table == null || !table.IsActive)
            throw new BusinessException(ErrorCodes.TableNotFound, "Mã QR không hợp lệ hoặc bàn đang ngừng sử dụng.");

        // 1. Gui trung: cung ma da duoc luu thi tra lai don cu
        var existed = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.ClientRequestId == req.ClientRequestId);
        if (existed != null)
        {
            if (existed.GuestKey != guestKey || existed.ContentHash != hash)
                throw new BusinessException(ErrorCodes.DuplicateMismatch,
                    "Yêu cầu gửi lại không khớp với đơn đã lưu. Vui lòng kiểm tra lại giỏ.");
            return new SubmitOrderResult
            {
                OrderId = existed.Id,
                SessionId = existed.DiningSessionId,
                Duplicate = true,
                ItemCount = existed.Items.Count
            };
        }

        // 2. Doi chieu phien cua trang voi phien hien tai cua ban
        var session = await _sessions.GetActiveSessionAsync(table.Id);
        if (session == null)
        {
            if (req.PageSessionId != null || req.PageTurn != table.TurnVersion)
                throw new BusinessException(ErrorCodes.StaleSession,
                    "Lượt phục vụ trước đã kết thúc. Vui lòng quét lại mã QR trên bàn.");
        }
        else
        {
            if (req.PageSessionId == null)
            {
                // trang mo luc ban con trong, nguoi khac cung ban vua tao phien -> duoc vao phien do
                if (req.PageTurn != table.TurnVersion)
                    throw new BusinessException(ErrorCodes.StaleSession,
                        "Lượt phục vụ trước đã kết thúc. Vui lòng quét lại mã QR trên bàn.");
            }
            else if (req.PageSessionId != session.Id)
            {
                throw new BusinessException(ErrorCodes.StaleSession,
                    "Lượt phục vụ trước đã kết thúc. Vui lòng quét lại mã QR trên bàn.");
            }

            if (session.Status == SessionStatus.ChoThanhToan)
                throw new BusinessException(ErrorCodes.SessionClosed,
                    "Bàn đang chờ thanh toán nên tạm dừng gọi món. Gọi nhân viên nếu bạn muốn gọi thêm.");

            if (req.PageSessionId != null && !await _sessions.HasAccessAsync(session.Id, guestKey))
                throw new BusinessException(ErrorCodes.NoAccess,
                    "Trình duyệt chưa được cấp quyền cho bàn này. Vui lòng nhờ nhân viên hỗ trợ.");
        }

        // 3. Kiem tra mon va gia o server
        var ids = req.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menu = await _db.MenuItems.Include(m => m.Category)
            .Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id);

        var unavailable = ids.Where(id => !menu.ContainsKey(id)
            || menu[id].Status != MenuItemStatus.DangBan
            || menu[id].Category?.IsActive != true).ToList();
        if (unavailable.Count > 0)
        {
            var names = unavailable.Where(menu.ContainsKey).Select(id => menu[id].Name);
            throw new BusinessException(ErrorCodes.ItemUnavailable,
                "Có món vừa hết hoặc ngừng bán: " + string.Join(", ", names) + ". Vui lòng bỏ món đó rồi gửi lại.",
                new { items = unavailable });
        }

        var changed = req.Items.Where(i => menu[i.MenuItemId].Price != i.Price)
            .Select(i => new { menuItemId = i.MenuItemId, oldPrice = i.Price, newPrice = menu[i.MenuItemId].Price })
            .DistinctBy(x => x.menuItemId).ToList();
        if (changed.Count > 0)
            throw new BusinessException(ErrorCodes.PriceChanged,
                "Giá một số món vừa thay đổi. Vui lòng xem lại giỏ rồi bấm gửi lần nữa.", new { items = changed });

        // 4. Lay hoac tao phien. Tao phien va luu don cung mot giao dich nen loi thi khong de lai phien rong.
        if (session == null)
        {
            session = new DiningSession
            {
                DiningTableId = table.Id,
                Status = SessionStatus.DangPhucVu,
                StartedAt = DateTime.Now
            };
            _db.DiningSessions.Add(session);
            await _db.SaveChangesAsync();
            _db.AddLog("TaoPhien", null, sessionId: session.Id, to: nameof(SessionStatus.DangPhucVu), guestKey: guestKey);
        }

        await _sessions.GrantAccessAsync(session.Id, guestKey, save: false);

        var order = new Order
        {
            DiningSessionId = session.Id,
            GuestKey = guestKey,
            ClientRequestId = req.ClientRequestId,
            ContentHash = hash,
            CreatedAt = DateTime.Now
        };
        foreach (var line in req.Items)
        {
            var m = menu[line.MenuItemId];
            order.Items.Add(new OrderItem
            {
                MenuItemId = m.Id,
                ItemName = m.Name,
                UnitPrice = m.Price,
                Quantity = line.Quantity,
                Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
                Status = OrderItemStatus.ChoXacNhan
            });
        }
        _db.Orders.Add(order);
        session.PaymentVersion++;
        await _db.SaveChangesAsync();

        _db.AddLog("GuiDon", null, sessionId: session.Id, reason: $"Đơn #{order.Id}, {order.Items.Count} dòng", guestKey: guestKey);
        await _db.SaveChangesAsync();

        return new SubmitOrderResult
        {
            OrderId = order.Id,
            SessionId = session.Id,
            Duplicate = false,
            ItemCount = order.Items.Count
        };
    }
}
