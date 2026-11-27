using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Qresto.Data;
using Qresto.Helpers;
using Qresto.Models;

namespace Qresto.Services;

public class TableService
{
    private readonly AppDbContext _db;

    public TableService(AppDbContext db)
    {
        _db = db;
    }

    public static string NewQrToken()
    {
        // 12 ky tu chu thuong + so, du kho doan
        const string chars = "abcdefghijkmnpqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    public Task<List<DiningTable>> GetAllAsync()
    {
        return _db.DiningTables.OrderBy(t => t.Code).ToListAsync();
    }

    public Task<DiningTable?> GetAsync(int id) => _db.DiningTables.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<DiningTable> CreateAsync(string code, string name)
    {
        code = code.Trim().ToUpper();
        name = name.Trim();
        if (code.Length == 0 || name.Length == 0)
            throw new BusinessException(ErrorCodes.InvalidInput, "Mã bàn và tên bàn không được để trống.");
        if (await _db.DiningTables.AnyAsync(t => t.Code == code))
            throw new BusinessException(ErrorCodes.InvalidInput, $"Mã bàn {code} đã tồn tại.");

        var table = new DiningTable { Code = code, Name = name, QrToken = NewQrToken() };
        _db.DiningTables.Add(table);
        await _db.SaveChangesAsync();
        return table;
    }

    public async Task UpdateAsync(int id, string code, string name)
    {
        var table = await GetAsync(id) ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy bàn.");
        code = code.Trim().ToUpper();
        if (code.Length == 0 || name.Trim().Length == 0)
            throw new BusinessException(ErrorCodes.InvalidInput, "Mã bàn và tên bàn không được để trống.");
        if (await _db.DiningTables.AnyAsync(t => t.Code == code && t.Id != id))
            throw new BusinessException(ErrorCodes.InvalidInput, $"Mã bàn {code} đã tồn tại.");

        table.Code = code;
        table.Name = name.Trim();
        await _db.SaveChangesAsync();
    }

    // Doi QR: QR cu in tren ban se khong dung duoc nua
    public async Task RegenerateQrAsync(int id)
    {
        var table = await GetAsync(id) ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy bàn.");
        bool dangPhucVu = await _db.DiningSessions.AnyAsync(s => s.DiningTableId == id
            && (s.Status == SessionStatus.DangPhucVu || s.Status == SessionStatus.ChoThanhToan));
        if (dangPhucVu)
            throw new BusinessException(ErrorCodes.Conflict, "Bàn đang có khách, đổi QR sau khi kết thúc lượt phục vụ.");

        table.QrToken = NewQrToken();
        await _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        var table = await GetAsync(id) ?? throw new BusinessException(ErrorCodes.NotFound, "Không tìm thấy bàn.");
        if (!active)
        {
            bool dangPhucVu = await _db.DiningSessions.AnyAsync(s => s.DiningTableId == id
                && (s.Status == SessionStatus.DangPhucVu || s.Status == SessionStatus.ChoThanhToan));
            if (dangPhucVu)
                throw new BusinessException(ErrorCodes.Conflict, "Bàn đang có phiên phục vụ nên chưa thể ngừng sử dụng.");
        }
        table.IsActive = active;
        await _db.SaveChangesAsync();
    }
}
