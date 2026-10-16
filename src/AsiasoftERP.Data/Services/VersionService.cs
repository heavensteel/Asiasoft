using AsiasoftERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AsiasoftERP.Data.Services;

public class VersionService
{
    private readonly AppDbContext _db;
    public const string CurrentVersion = "1.0.0";

    public VersionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<VersionInfo>> GetAllVersionsAsync()
    {
        return await _db.VersionInfos
            .Include(v => v.Changes)
            .OrderByDescending(v => v.ReleaseDate)
            .ToListAsync();
    }

    public async Task<VersionInfo?> GetCurrentVersionAsync()
    {
        return await _db.VersionInfos
            .Include(v => v.Changes)
            .FirstOrDefaultAsync(v => v.Version == CurrentVersion);
    }

    public async Task<VersionInfo> AddVersionAsync(string version, string title,
        string releaseNotes, bool isCritical, string releasedBy,
        List<(ChangeType type, string desc, string? module)> changes)
    {
        var versionInfo = new VersionInfo
        {
            Version = version,
            Title = title,
            ReleaseNotes = releaseNotes,
            IsCritical = isCritical,
            ReleasedBy = releasedBy,
            ReleaseDate = DateTime.Now
        };

        foreach (var (type, desc, module) in changes)
        {
            versionInfo.Changes.Add(new VersionChange
            {
                ChangeType = type,
                Description = desc,
                Module = module
            });
        }

        _db.VersionInfos.Add(versionInfo);
        await _db.SaveChangesAsync();
        return versionInfo;
    }
}
