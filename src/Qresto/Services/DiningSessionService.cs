using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Helpers;
using Qresto.Models;

namespace Qresto.Services;

// Ngu canh cua trang thuc don khi khach quet QR
public class TableContext
{
    public DiningTable Table { get; set; } = null!;
    public DiningSession? ActiveSession { get; set; }
    public int TurnVersion { get; set; }
}

public class DiningSessionService
{
    private readonly AppDbContext _db;
    private readonly IStaffNotifier _notifier;

    public DiningSessionService(AppDbContext db, IStaffNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public Task<DiningSession?> GetActiveSessionAsync(int tableId)
    {
        return _db.DiningSessions.FirstOrDefaultAsync(s => s.DiningTableId == tableId
            && (s.Status == SessionStatus.DangPhucVu || s.Status == SessionStatus.ChoThanhToan));
    }

    // Khach quet QR. Khong tao phien. Neu ban dang co phien thi cap quyen xem cho trinh duyet nay
    // (khach ngoi cung ban quet QR tren ban la duoc xem mon chung).
    public async Task<TableContext> EnterTableAsync(string qrToken, string guestKey)
    {
        var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.QrToken == qrToken);
        if (table == null || !table.IsActive)
            throw new BusinessException(ErrorCodes.TableNotFound, "Mã QR không hợp lệ hoặc bàn đang ngừng sử dụng.");

        var session = await GetActiveSessionAsync(table.Id);
        if (session != null)
            await GrantAccessAsync(session.Id, guestKey, save: true);

        return new TableContext { Table = table, ActiveSession = session, TurnVersion = table.TurnVersion };
    }

    public async Task GrantAccessAsync(int sessionId, string guestKey, bool save)
    {
        var access = await _db.GuestSessionAccesses
            .FirstOrDefaultAsync(a => a.DiningSessionId == sessionId && a.GuestKey == guestKey);
        if (access == null)
        {
            // co the da Add trong cung context nhung chua save
            bool daThem = _db.ChangeTracker.Entries<GuestSessionAccess>()
                .Any(e => e.Entity.DiningSessionId == sessionId && e.Entity.GuestKey == guestKey);
            if (!daThem)
                _db.GuestSessionAccesses.Add(new GuestSessionAccess { DiningSessionId = sessionId, GuestKey = guestKey });
        }
        if (save)
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation())
            {
                // hai tab cung quet mot luc, ben kia da cap roi
                _db.ChangeTracker.Clear();
            }
        }
    }

    public async Task<bool> HasAccessAsync(int sessionId, string guestKey)
    {
        return await _db.GuestSessionAccesses.AnyAsync(a => a.DiningSessionId == sessionId
            && a.GuestKey == guestKey && a.RevokedAt == null);
    }

    // Kiem tra trang cua khach con dung phien va con quyen. Dung cho moi thao tac cua khach sau khi da co phien.
    public async Task<DiningSession> RequireGuestSessionAsync(string qrToken, int sessionId, string guestKey)
    {
        var session = await _db.DiningSessions.Include(s => s.DiningTable)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null || session.DiningTable == null || session.DiningTable.QrToken != qrToken)
            throw new BusinessException(ErrorCodes.StaleSession, "Trang không còn hợp lệ. Vui lòng quét lại mã QR trên bàn.");

        if (!session.IsActive)
            throw new BusinessException(ErrorCodes.StaleSession, "Lượt phục vụ này đã kết thúc. Vui lòng quét lại mã QR trên bàn.");

        if (!await HasAccessAsync(session.Id, guestKey))
            throw new BusinessException(ErrorCodes.NoAccess, "Trình duyệt chưa được cấp quyền cho bàn này. Vui lòng nhờ nhân viên hỗ trợ.");

        return session;
    }

    // Phuc vu/quan ly cho ban goi them truoc khi thu tien
    public async Task ReopenForOrderingAsync(int sessionId, StaffActor actor)
    {
        if (!actor.IsWaiter)
            throw new BusinessException(ErrorCodes.NotAllowed, "Bạn không có quyền thực hiện thao tác này.");

        int tableId = await _db.InTransactionAsync(async () =>
        {
            var session = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == sessionId)
                ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy phiên phục vụ.");

            if (session.Status != SessionStatus.ChoThanhToan)
                throw new BusinessException(ErrorCodes.Conflict, "Chỉ chuyển về Đang phục vụ khi bàn đang chờ thanh toán.");

            session.Status = SessionStatus.DangPhucVu;
            session.PaymentVersion++;
            _db.AddLog("MoLaiPhucVu", actor, sessionId: session.Id,
                from: nameof(SessionStatus.ChoThanhToan), to: nameof(SessionStatus.DangPhucVu));
            await _db.SaveChangesAsync();
            return session.DiningTableId;
        });

        await _notifier.NotifyAsync("phien", tableId);
    }

    // Huy phien khong thu tien: moi dong da huy hoac bi tu choi
    public async Task CancelSessionAsync(int sessionId, string reason, StaffActor actor)
    {
        if (!actor.IsWaiter)
            throw new BusinessException(ErrorCodes.NotAllowed, "Bạn không có quyền thực hiện thao tác này.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessException(ErrorCodes.InvalidInput, "Vui lòng nhập lý do hủy phiên.");

        int tableId = await _db.InTransactionAsync(async () =>
        {
            var session = await _db.DiningSessions.Include(s => s.DiningTable)
                .FirstOrDefaultAsync(s => s.Id == sessionId)
                ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy phiên phục vụ.");

            if (!session.IsActive)
                throw new BusinessException(ErrorCodes.Conflict, "Phiên đã kết thúc.");

            var items = await _db.OrderItems.Where(i => i.Order!.DiningSessionId == sessionId).ToListAsync();
            if (items.Any(i => i.Status != OrderItemStatus.DaHuy && i.Status != OrderItemStatus.BiTuChoi))
                throw new BusinessException(ErrorCodes.Conflict, "Còn món phải xử lý hoặc phải thu tiền nên chưa thể hủy phiên.");

            bool conYeuCauHuy = await _db.CancelRequests.AnyAsync(c => c.OrderItem!.Order!.DiningSessionId == sessionId
                && c.Status == CancelRequestStatus.ChoDuyet);
            if (conYeuCauHuy || items.Any(i => i.HasIncident))
                throw new BusinessException(ErrorCodes.Conflict, "Còn yêu cầu hủy hoặc sự cố chưa xử lý.");

            session.Status = SessionStatus.DaHuy;
            session.CancelReason = reason.Trim();
            await EndSessionAsync(session, actor);
            _db.AddLog("HuyPhien", actor, sessionId: session.Id, to: nameof(SessionStatus.DaHuy), reason: reason.Trim());
            await _db.SaveChangesAsync();
            return session.DiningTableId;
        });

        await _notifier.NotifyAsync("phien", tableId);
    }

    // Dung chung khi dong phien (thanh toan) hoac huy phien. Goi trong giao dich cua nguoi goi.
    public async Task EndSessionAsync(DiningSession session, StaffActor actor)
    {
        session.EndedAt = DateTime.Now;
        session.EndedById = actor.Id;
        session.PaymentVersion++;

        var accesses = await _db.GuestSessionAccesses
            .Where(a => a.DiningSessionId == session.Id && a.RevokedAt == null).ToListAsync();
        foreach (var a in accesses)
            a.RevokedAt = DateTime.Now;

        // yeu cau ho tro con mo thi ket thuc luon
        var supports = await _db.SupportRequests
            .Where(r => r.DiningSessionId == session.Id && r.Status != SupportStatus.HoanTat).ToListAsync();
        foreach (var r in supports)
        {
            r.Status = SupportStatus.HoanTat;
            r.CompletedAt = DateTime.Now;
        }

        var table = session.DiningTable ?? await _db.DiningTables.FirstAsync(t => t.Id == session.DiningTableId);
        table.TurnVersion++;
    }
}
