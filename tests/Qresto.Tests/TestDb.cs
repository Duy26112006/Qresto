using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Helpers;
using Qresto.Models;
using Qresto.Services;

namespace Qresto.Tests;

public class FakeNotifier : IStaffNotifier
{
    public List<string> Sent { get; } = new();

    public Task NotifyAsync(string loai, int? tableId = null)
    {
        lock (Sent) Sent.Add(loai);
        return Task.CompletedTask;
    }
}

// Moi test dung mot file SQLite rieng de co the mo nhieu context cung luc (gia lap 2 dien thoai, 2 nhan vien)
public class TestDb : IDisposable
{
    public string FilePath { get; }
    private readonly string _conn;

    public string TableToken { get; private set; } = "";
    public int TableId { get; private set; }
    public int ComSuon { get; private set; }   // 55.000
    public int TraDao { get; private set; }    // 30.000
    public int GoiCuon { get; private set; }   // 35.000

    public static readonly StaffActor PhucVu = new("pv1", "Phục vụ 1", new[] { Roles.PhucVu });
    public static readonly StaffActor PhucVu2 = new("pv2", "Phục vụ 2", new[] { Roles.PhucVu });
    public static readonly StaffActor Bep = new("bep1", "Bếp 1", new[] { Roles.Bep });
    public static readonly StaffActor QuanLy = new("ql1", "Quản lý", new[] { Roles.QuanLy });

    public TestDb()
    {
        FilePath = Path.Combine(Path.GetTempPath(), "qresto_test_" + Guid.NewGuid().ToString("N") + ".db");
        _conn = $"Data Source={FilePath};Pooling=False";
        using var db = NewContext();
        db.Database.EnsureCreated();

        var table = new DiningTable { Code = "B03", Name = "Bàn 03", QrToken = "token-ban-03" };
        db.DiningTables.Add(table);
        var cat = new Category { Name = "Món chính", DisplayOrder = 1 };
        var drink = new Category { Name = "Đồ uống", DisplayOrder = 2 };
        db.Categories.AddRange(cat, drink);
        db.SaveChanges();

        var com = new MenuItem { CategoryId = cat.Id, Name = "Cơm sườn", Price = 55000, PrepTimeMinutes = 10 };
        var tra = new MenuItem { CategoryId = drink.Id, Name = "Trà đào", Price = 30000, PrepTimeMinutes = 3 };
        var goi = new MenuItem { CategoryId = cat.Id, Name = "Gỏi cuốn", Price = 35000, PrepTimeMinutes = 5 };
        db.MenuItems.AddRange(com, tra, goi);
        db.SaveChanges();

        TableToken = table.QrToken;
        TableId = table.Id;
        ComSuon = com.Id;
        TraDao = tra.Id;
        GoiCuon = goi.Id;
    }

    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        return new AppDbContext(options);
    }

    // Bo service dung chung mot context, giong mot request
    public Svc NewServices()
    {
        return new Svc(NewContext());
    }

    public void Dispose()
    {
        try { File.Delete(FilePath); } catch { }
    }

    public class Svc : IDisposable
    {
        public AppDbContext Db { get; }
        public FakeNotifier Notifier { get; } = new();
        public DiningSessionService Sessions { get; }

        public Svc(AppDbContext db)
        {
            Db = db;
            Sessions = new DiningSessionService(db, Notifier);
        }

        public void Dispose() => Db.Dispose();
    }

    public DiningSession Session(int id)
    {
        using var db = NewContext();
        return db.DiningSessions.AsNoTracking().First(s => s.Id == id);
    }
}
