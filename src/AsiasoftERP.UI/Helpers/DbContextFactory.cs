using AsiasoftERP.Data;
using Microsoft.EntityFrameworkCore;

namespace AsiasoftERP.UI.Helpers;

/// <summary>
/// Creates AppDbContext instances with configured options
/// </summary>
public static class DbContextFactory
{
    private static DbContextOptions<AppDbContext>? _options;

    public static void Configure(string connectionString, bool useSqlite = true)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();

        if (useSqlite)
            builder.UseSqlite(connectionString);
        else
            builder.UseSqlServer(connectionString);

        _options = builder.Options;
    }

    public static AppDbContext Create()
    {
        if (_options == null)
            throw new InvalidOperationException("DbContextFactory not configured. Call Configure() first.");

        return new AppDbContext(_options);
    }

    /// <summary>
    /// Ensure database is created and seeded
    /// </summary>
    public static async Task EnsureDatabaseAsync()
    {
        using var db = Create();
        await db.Database.MigrateAsync();
    }
}
