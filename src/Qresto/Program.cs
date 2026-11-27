using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Hubs;
using Qresto.Models;
using Qresto.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== Co so du lieu SQLite =====
// File qresto.db nam trong thu muc App_Data cua project (khong dua len git)
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var connStr = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connStr))
    connStr = "Data Source=" + Path.Combine(dataDir, "qresto.db");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connStr));

// ===== Dang nhap nhan vien =====
builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
    {
        opt.Password.RequiredLength = 6;
        opt.Password.RequireDigit = true;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail = false;
        opt.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppClaimsFactory>();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/TaiKhoan/DangNhap";
    opt.LogoutPath = "/TaiKhoan/DangXuat";
    opt.AccessDeniedPath = "/TaiKhoan/KhongCoQuyen";
    opt.ExpireTimeSpan = TimeSpan.FromHours(12);
});

// Tai khoan bi khoa hoac doi vai tro thi cookie cu het han nhanh
builder.Services.Configure<SecurityStampValidatorOptions>(opt => opt.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddAntiforgery(opt => opt.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// ===== Service nghiep vu =====
builder.Services.AddScoped<IStaffNotifier, StaffNotifier>();
builder.Services.AddScoped<TableService>();
builder.Services.AddScoped<DiningSessionService>();

var app = builder.Build();

// Tao CSDL va du lieu mau khi chay lan dau
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.GetMigrations().Any())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
    DbSeeder.WebRoot = app.Environment.WebRootPath;
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<StaffHub>("/hubs/nhanvien");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// de project test dung WebApplicationFactory neu can
public partial class Program { }
