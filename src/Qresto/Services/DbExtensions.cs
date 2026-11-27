using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Models;

namespace Qresto.Services;

public static class DbExtensions
{
    // Chay mot doan xu ly trong giao dich. Loi thi rollback va xoa cac thay doi dang theo doi
    // de lan thu lai (neu co) khong bi dinh du lieu cu.
    public static async Task<T> InTransactionAsync<T>(this AppDbContext db, Func<Task<T>> action)
    {
        if (db.Database.CurrentTransaction != null)
            return await action();

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await action();
            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            throw;
        }
    }

    public static async Task InTransactionAsync(this AppDbContext db, Func<Task> action)
    {
        await db.InTransactionAsync(async () =>
        {
            await action();
            return true;
        });
    }

    public static void AddLog(this AppDbContext db, string action, StaffActor? actor,
        int? sessionId = null, int? orderItemId = null,
        string? from = null, string? to = null, string? reason = null, string? guestKey = null)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            Action = action,
            DiningSessionId = sessionId,
            OrderItemId = orderItemId,
            FromStatus = from,
            ToStatus = to,
            Reason = reason,
            ActorType = actor != null ? ActorType.NhanVien : (guestKey != null ? ActorType.Khach : ActorType.HeThong),
            ActorId = actor?.Id ?? guestKey,
            ActorName = actor?.Name ?? (guestKey != null ? "Khách" : "Hệ thống"),
            At = DateTime.Now
        });
    }

    // Unique index bi vi pham (SQLite error 19 - constraint)
    public static bool IsUniqueViolation(this DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }
}
