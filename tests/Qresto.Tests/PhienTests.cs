using Microsoft.EntityFrameworkCore;
using Qresto.Helpers;
using Qresto.Models;

namespace Qresto.Tests;

public class PhienTests : IDisposable
{
    private readonly TestDb _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task QuetQr_KhongTaoPhien()
    {
        using var s = _t.NewServices();
        var ctx = await s.Sessions.EnterTableAsync(_t.TableToken, "khach-a");

        Assert.Null(ctx.ActiveSession);
        Assert.Equal(0, await s.Db.DiningSessions.CountAsync());
    }

    [Fact]
    public async Task QrSai_BaoLoi()
    {
        using var s = _t.NewServices();
        var ex = await Assert.ThrowsAsync<BusinessException>(() => s.Sessions.EnterTableAsync("khong-co", "khach-a"));
        Assert.Equal(ErrorCodes.TableNotFound, ex.Code);
    }

    [Fact]
    public async Task BanNgungSuDung_KhongMoDuocThucDon()
    {
        using (var db = _t.NewContext())
        {
            var table = await db.DiningTables.FirstAsync(t => t.Id == _t.TableId);
            table.IsActive = false;
            await db.SaveChangesAsync();
        }

        using var s = _t.NewServices();
        var ex = await Assert.ThrowsAsync<BusinessException>(() => s.Sessions.EnterTableAsync(_t.TableToken, "khach-a"));
        Assert.Equal(ErrorCodes.TableNotFound, ex.Code);
    }
}
