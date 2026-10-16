using AsiasoftERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AsiasoftERP.Data.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int? userId, string module, string action,
        string description, object? oldValues = null, object? newValues = null,
        string? machineName = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Module = module,
            Action = action,
            Description = description,
            OldValues = oldValues != null ? JsonConvert.SerializeObject(oldValues) : null,
            NewValues = newValues != null ? JsonConvert.SerializeObject(newValues) : null,
            MachineName = machineName ?? Environment.MachineName,
            Timestamp = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync(
        string? module = null, int? userId = null,
        DateTime? from = null, DateTime? to = null,
        int take = 1000)
    {
        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();

        if (!string.IsNullOrEmpty(module))
            query = query.Where(a => a.Module == module);
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId);
        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        return await query.OrderByDescending(a => a.Timestamp).Take(take).ToListAsync();
    }
}
