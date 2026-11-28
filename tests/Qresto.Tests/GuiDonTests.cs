using Microsoft.EntityFrameworkCore;
using Qresto.Helpers;
using Qresto.Models;

namespace Qresto.Tests;

public class GuiDonTests : IDisposable
{
    private readonly TestDb _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task GuiDonDauTien_TaoPhienVaDon_DongChoXacNhan()
    {
        using var s = _t.NewServices();
        var req = TestDb.Request(null, 0, (_t.ComSuon, 1, 55000));

        var result = await s.Orders.SubmitAsync(_t.TableToken, req, "khach-a");

        var session = _t.Session(result.SessionId);
        Assert.Equal(SessionStatus.DangPhucVu, session.Status);
        var item = _t.Item(_t.ItemIdsOfOrder(result.OrderId).Single());
        Assert.Equal(OrderItemStatus.ChoXacNhan, item.Status);
        Assert.Equal(55000, item.UnitPrice);
        Assert.Contains("don-moi", s.Notifier.Sent);
    }

    [Fact]
    public async Task GoiBoSung_TaoDonMoiTrongCungPhien()
    {
        int sessionId;
        using (var s = _t.NewServices())
            sessionId = (await s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.ComSuon, 1, 55000)), "khach-a")).SessionId;

        using (var s = _t.NewServices())
        {
            var r2 = await s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(sessionId, 0, (_t.TraDao, 2, 30000)), "khach-a");
            Assert.Equal(sessionId, r2.SessionId);
            Assert.Equal(2, await s.Db.Orders.CountAsync());
            Assert.Equal(1, await s.Db.DiningSessions.CountAsync());
        }
    }

    [Fact]
    public async Task HaiDienThoaiGuiGanNhuCungLuc_MotPhienHaiDon()
    {
        var reqA = TestDb.Request(null, 0, (_t.ComSuon, 1, 55000));
        var reqB = TestDb.Request(null, 0, (_t.TraDao, 2, 30000));

        using var sa = _t.NewServices();
        using var sb = _t.NewServices();
        var results = await Task.WhenAll(
            sa.Orders.SubmitAsync(_t.TableToken, reqA, "khach-a"),
            sb.Orders.SubmitAsync(_t.TableToken, reqB, "khach-b"));

        Assert.Equal(results[0].SessionId, results[1].SessionId);

        // Gui lai cung ma cua A (mat phan hoi) khong tao don thu ba
        using var sc = _t.NewServices();
        var again = await sc.Orders.SubmitAsync(_t.TableToken, reqA, "khach-a");
        Assert.True(again.Duplicate);
        Assert.Equal(results[0].OrderId, again.OrderId);
        Assert.Equal(2, await sc.Db.Orders.CountAsync());
        Assert.Equal(1, await sc.Db.DiningSessions.CountAsync());
    }

    [Fact]
    public async Task CungMaGuiNhungKhacNoiDung_TuChoi()
    {
        var req = TestDb.Request(null, 0, (_t.ComSuon, 1, 55000));
        using (var s = _t.NewServices())
            await s.Orders.SubmitAsync(_t.TableToken, req, "khach-a");

        req.Items[0].Quantity = 3;
        using (var s = _t.NewServices())
        {
            var ex = await Assert.ThrowsAsync<BusinessException>(() => s.Orders.SubmitAsync(_t.TableToken, req, "khach-a"));
            Assert.Equal(ErrorCodes.DuplicateMismatch, ex.Code);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task SoLuongNgoaiKhoang_InvalidInput(int qty)
    {
        using var s = _t.NewServices();
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.ComSuon, qty, 55000)), "khach-a"));
        Assert.Equal(ErrorCodes.InvalidInput, ex.Code);
        Assert.Equal(0, await s.Db.DiningSessions.CountAsync());
    }

    [Fact]
    public async Task GhiChuQua300KyTu_InvalidInput()
    {
        using var s = _t.NewServices();
        var req = TestDb.Request(null, 0, (_t.ComSuon, 1, 55000));
        req.Items[0].Note = new string('a', 301);
        var ex = await Assert.ThrowsAsync<BusinessException>(() => s.Orders.SubmitAsync(_t.TableToken, req, "khach-a"));
        Assert.Equal(ErrorCodes.InvalidInput, ex.Code);
    }

    [Fact]
    public async Task MonVuaHet_KhongLuuGi()
    {
        using (var s = _t.NewServices())
            await s.Menu.ChangeStatusAsync(_t.ComSuon, MenuItemStatus.TamHet, TestDb.Bep);

        using (var s = _t.NewServices())
        {
            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.ComSuon, 1, 55000), (_t.TraDao, 1, 30000)), "khach-a"));
            Assert.Equal(ErrorCodes.ItemUnavailable, ex.Code);
        }

        using var check = _t.NewContext();
        Assert.Equal(0, await check.Orders.CountAsync());
        Assert.Equal(0, await check.DiningSessions.CountAsync()); // khong de lai phien rong
    }

    [Fact]
    public async Task GiaVuaDoi_BaoKhach_GuiLaiVoiGiaMoiThiLuuGiaMoi()
    {
        using (var db = _t.NewContext())
        {
            var m = await db.MenuItems.FirstAsync(x => x.Id == _t.ComSuon);
            m.Price = 60000;
            await db.SaveChangesAsync();
        }

        using (var s = _t.NewServices())
        {
            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.ComSuon, 1, 55000)), "khach-a"));
            Assert.Equal(ErrorCodes.PriceChanged, ex.Code);
        }

        using (var s = _t.NewServices())
        {
            var r = await s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.ComSuon, 1, 60000)), "khach-a");
            Assert.Equal(60000, _t.Item(_t.ItemIdsOfOrder(r.OrderId).Single()).UnitPrice);
        }
    }

    [Fact]
    public async Task SuaGiaThucDon_KhongDoiGiaDonCu()
    {
        int orderId;
        using (var s = _t.NewServices())
            orderId = (await s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.ComSuon, 1, 55000)), "khach-a")).OrderId;

        using (var s = _t.NewServices())
        {
            var m = await s.Menu.GetItemAsync(_t.ComSuon);
            m!.Price = 99000;
            await s.Menu.SaveItemAsync(m);
        }

        Assert.Equal(55000, _t.Item(_t.ItemIdsOfOrder(orderId).Single()).UnitPrice);
    }

    [Fact]
    public async Task DienThoaiThuHai_MoTrangLucBanTrong_VanVaoDuocPhienDangPhucVu()
    {
        // B mo trang luc ban chua co phien (PageSessionId = null, turn = 0)
        int sessionId;
        using (var s = _t.NewServices())
            sessionId = (await s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.ComSuon, 1, 55000)), "khach-a")).SessionId;

        using (var s = _t.NewServices())
        {
            var r = await s.Orders.SubmitAsync(_t.TableToken, TestDb.Request(null, 0, (_t.TraDao, 1, 30000)), "khach-b");
            Assert.Equal(sessionId, r.SessionId);
            Assert.True(await s.Sessions.HasAccessAsync(sessionId, "khach-b"));
        }
    }
}
