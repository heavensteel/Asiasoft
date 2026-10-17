using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Service managing granular, user-specific module and page visibility permissions
/// stored in MariaDB table 'erp.kullanici_modul_yetkileri'.
/// </summary>
public class PermissionService
{
    public static async Task EnsureTableExistsAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                CREATE TABLE IF NOT EXISTS erp.kullanici_modul_yetkileri (
                    ID INT AUTO_INCREMENT PRIMARY KEY,
                    KULLANICI_KODU VARCHAR(50) NOT NULL,
                    KULLANICI_ADI VARCHAR(100) NOT NULL,
                    MODUL_KEY VARCHAR(50) NOT NULL,
                    MODUL_BASLIK VARCHAR(100) NOT NULL,
                    IZIN_VAR TINYINT(1) DEFAULT 1,
                    GUNCELLEME_TARIHI DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    UNIQUE KEY uq_kul_mod (KULLANICI_KODU, MODUL_KEY)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            await conn.ExecuteAsync(sql);
        }
        catch { }
    }

    /// <summary>
    /// Returns the set of module keys the user is explicitly authorized to view.
    /// If user has records in 'kullanici_modul_yetkileri', ONLY permitted modules are returned!
    /// </summary>
    public async Task<HashSet<string>> GetAllowedModuleKeysAsync(string userCode, string userName, bool isSuperAdmin = false)
    {
        await EnsureTableExistsAsync();

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string countSql = @"
            SELECT COUNT(*) 
            FROM erp.kullanici_modul_yetkileri 
            WHERE (KULLANICI_KODU = @UserCode OR KULLANICI_ADI = @UserName);";

        int totalConfigured = await conn.ExecuteScalarAsync<int>(countSql, new { UserCode = userCode, UserName = userName });

        if (totalConfigured > 0)
        {
            const string sql = @"
                SELECT MODUL_KEY 
                FROM erp.kullanici_modul_yetkileri 
                WHERE (KULLANICI_KODU = @UserCode OR KULLANICI_ADI = @UserName) AND IZIN_VAR = 1;";

            var keys = (await conn.QueryAsync<string>(sql, new { UserCode = userCode, UserName = userName })).ToList();
            return new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        }

        // If no records have been configured yet for this user in DB:
        // By default, grant standard set, or all if admin
        var defaultSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in AppModuleRegistry.AllModules)
        {
            // By default, everyone can see Home
            if (mod.Key == "Home") { defaultSet.Add(mod.Key); continue; }

            // Restrict admin system menus by default unless admin
            if (mod.Category == "SİSTEM" && !isSuperAdmin) continue;

            defaultSet.Add(mod.Key);
        }

        return defaultSet;
    }

    /// <summary>
    /// Retrieves all modules with their permission status for the given user.
    /// </summary>
    public async Task<List<UserModulePermissionDto>> GetUserPermissionsAsync(string userCode, string userName, bool isSuperAdmin = false)
    {
        await EnsureTableExistsAsync();

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string checkSql = "SELECT COUNT(*) FROM erp.kullanici_modul_yetkileri WHERE KULLANICI_KODU = @UserCode OR KULLANICI_ADI = @UserName;";
        int count = await conn.ExecuteScalarAsync<int>(checkSql, new { UserCode = userCode, UserName = userName });

        var allowedKeys = await GetAllowedModuleKeysAsync(userCode, userName, isSuperAdmin);

        var list = new List<UserModulePermissionDto>();
        foreach (var mod in AppModuleRegistry.AllModules)
        {
            list.Add(new UserModulePermissionDto
            {
                UserCode = userCode,
                UserName = userName,
                ModuleKey = mod.Key,
                ModuleTitle = mod.FullTitle,
                Category = mod.Category,
                HasPermission = allowedKeys.Contains(mod.Key)
            });
        }

        return list;
    }

    /// <summary>
    /// Saves or updates the permissions for a specific user into 'kullanici_modul_yetkileri'
    /// </summary>
    public async Task<bool> SaveUserPermissionsAsync(string userCode, string userName, Dictionary<string, bool> permissions)
    {
        await EnsureTableExistsAsync();

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            const string sql = @"
                INSERT INTO erp.kullanici_modul_yetkileri 
                    (KULLANICI_KODU, KULLANICI_ADI, MODUL_KEY, MODUL_BASLIK, IZIN_VAR, GUNCELLEME_TARIHI)
                VALUES 
                    (@UserCode, @UserName, @ModKey, @ModBaslik, @IzinVar, NOW())
                ON DUPLICATE KEY UPDATE 
                    IZIN_VAR = VALUES(IZIN_VAR),
                    KULLANICI_ADI = VALUES(KULLANICI_ADI),
                    GUNCELLEME_TARIHI = NOW();";

            foreach (var kvp in permissions)
            {
                var modDef = AppModuleRegistry.AllModules.FirstOrDefault(m => m.Key.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                string baslik = modDef?.FullTitle ?? kvp.Key;

                await conn.ExecuteAsync(sql, new
                {
                    UserCode = userCode,
                    UserName = userName,
                    ModKey = kvp.Key,
                    ModBaslik = baslik,
                    IzinVar = kvp.Value ? 1 : 0
                }, tran);
            }

            tran.Commit();
            return true;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Retrieves distinct users for permission assignment dropdown
    /// </summary>
    public async Task<List<CompanyUserDto>> GetAllUsersAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT DISTINCT
                CAST(KODU AS DOUBLE) as KullaniciKodu,
                ADI as KullaniciAdi,
                COALESCE(YETKI, 'S') as Yetki,
                CASE WHEN YETKI = 'A' THEN 1 ELSE 0 END as IsAdmin
            FROM user
            WHERE ADI IS NOT NULL AND ADI != ''
            ORDER BY ADI ASC;";

        var list = (await conn.QueryAsync<CompanyUserDto>(sql)).ToList();
        return list;
    }
}
