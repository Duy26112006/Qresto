using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Qresto.Models;

namespace Qresto.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<DiningSession> DiningSessions => Set<DiningSession>();
    public DbSet<GuestSessionAccess> GuestSessionAccesses => Set<GuestSessionAccess>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CancelRequest> CancelRequests => Set<CancelRequest>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        });

        builder.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<MenuItem>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.ImageUrl).HasMaxLength(300);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.CategoryId, x.Status });
            e.ToTable(t => t.HasCheckConstraint("CK_MenuItems_Price", "Price > 0"));
            e.HasOne(x => x.Category).WithMany(c => c.MenuItems)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DiningTable>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.QrToken).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.QrToken).IsUnique();
        });

        builder.Entity<DiningSession>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CancelReason).HasMaxLength(300);
            e.HasOne(x => x.DiningTable).WithMany(t => t.Sessions)
                .HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.Restrict);

            // Moi ban chi co toi da 1 phien dang hoat dong.
            // Hai dien thoai gui don dau tien cung luc thi chi 1 ben tao duoc phien.
            e.HasIndex(x => x.DiningTableId)
                .IsUnique()
                .HasFilter("\"Status\" IN ('DangPhucVu', 'ChoThanhToan')")
                .HasDatabaseName("IX_DiningSessions_OnePerTable");
        });

        builder.Entity<GuestSessionAccess>(e =>
        {
            e.Property(x => x.GuestKey).HasMaxLength(64).IsRequired();
            e.HasIndex(x => new { x.DiningSessionId, x.GuestKey }).IsUnique();
            e.HasOne(x => x.DiningSession).WithMany(s => s.Accesses)
                .HasForeignKey(x => x.DiningSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(e =>
        {
            e.Property(x => x.GuestKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.ClientRequestId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            // Ma gui do trinh duyet sinh (Guid) nen danh unique toan bang
            e.HasIndex(x => x.ClientRequestId).IsUnique();
            e.HasOne(x => x.DiningSession).WithMany(s => s.Orders)
                .HasForeignKey(x => x.DiningSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.ItemName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Note).HasMaxLength(300);
            e.Property(x => x.RejectReason).HasMaxLength(300);
            e.Property(x => x.CancelReason).HasMaxLength(300);
            e.Property(x => x.IncidentNote).HasMaxLength(300);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.Status);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_OrderItems_Quantity", "Quantity BETWEEN 1 AND 50");
                t.HasCheckConstraint("CK_OrderItems_UnitPrice", "UnitPrice >= 0");
            });
            e.HasOne(x => x.Order).WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MenuItem).WithMany()
                .HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CancelRequest>(e =>
        {
            e.Property(x => x.GuestKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(300).IsRequired();
            e.Property(x => x.HandleNote).HasMaxLength(300);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.OrderItem).WithMany(i => i.CancelRequests)
                .HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SupportRequest>(e =>
        {
            e.Property(x => x.Note).HasMaxLength(200);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.DiningSession).WithMany(s => s.SupportRequests)
                .HasForeignKey(x => x.DiningSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Invoice>(e =>
        {
            e.Property(x => x.TableName).HasMaxLength(50);
            e.Property(x => x.CashierName).HasMaxLength(100);
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            // Lop bao ve thu hai: du 2 nhan vien bam cung luc cung chi co 1 hoa don
            e.HasIndex(x => x.DiningSessionId).IsUnique();
            e.HasIndex(x => x.PaidAt);
            e.HasOne(x => x.DiningSession).WithOne(s => s.Invoice)
                .HasForeignKey<Invoice>(x => x.DiningSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InvoiceItem>(e =>
        {
            e.Property(x => x.ItemName).HasMaxLength(150).IsRequired();
            e.HasOne(x => x.Invoice).WithMany(i => i.Items)
                .HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ActivityLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.FromStatus).HasMaxLength(20);
            e.Property(x => x.ToStatus).HasMaxLength(20);
            e.Property(x => x.Reason).HasMaxLength(300);
            e.Property(x => x.ActorName).HasMaxLength(100);
            e.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.DiningSessionId);
            e.HasIndex(x => x.OrderItemId);
        });
    }
}
