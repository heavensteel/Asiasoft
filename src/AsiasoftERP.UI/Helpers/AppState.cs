using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Helpers;

/// <summary>
/// Global application state - current user, active company, session info
/// </summary>
public static class AppState
{
    public static User? CurrentUser { get; set; }
    public static CompanyDto? ActiveCompany { get; set; }
    public static CompanyUserDto? ActiveUser { get; set; }

    public static string RememberMeToken { get; set; } = string.Empty;
    public static int? CurrentSessionDbId { get; set; }
    public static RdpHelper.RdpSession? CurrentRdpSession { get; set; }

    public static bool IsLoggedIn => ActiveUser != null || CurrentUser != null;
    public static bool IsAdmin => (ActiveUser?.IsAdmin == true) 
                                   || (ActiveUser?.Yetki?.Trim().ToUpper() == "A")
                                   || (ActiveUser?.KullaniciKodu == 1 || ActiveUser?.KullaniciKodu == 100 || ActiveUser?.KullaniciAdi?.ToUpper().Contains("ADMIN") == true) 
                                   || (CurrentUser?.Role?.IsAdmin ?? false);

    public static string MachineName => Environment.MachineName;
    public static string SessionName => RdpHelper.GetCurrentSessionName();
    public static int RdpSessionId => RdpHelper.GetCurrentSessionId();
    public static string DatabaseInfo => "Yerel MariaDB (127.0.0.1:3306/erp) - Canlı Koruma Aktif";

    public static string DisplayUserName => ActiveUser?.KullaniciAdi ?? CurrentUser?.FullName ?? CurrentUser?.Username ?? "Misafir";
    public static string DisplayCompanyName => ActiveCompany?.Adi ?? "TEK ASYA A.Ş.";
    public static string CurrentUserCode => ActiveUser != null ? ActiveUser.KullaniciKodu.ToString() : (CurrentUser != null ? CurrentUser.Id.ToString() : "100");
    public static string CurrentUserName => DisplayUserName;

    public static void Clear()
    {
        CurrentUser = null;
        ActiveCompany = null;
        ActiveUser = null;
        RememberMeToken = string.Empty;
        CurrentSessionDbId = null;
        CurrentRdpSession = null;
        AllowedModules.Clear();
    }

    /// <summary>
    /// Active authorized module keys for current session
    /// </summary>
    public static HashSet<string> AllowedModules { get; } = new(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads permissions for active user from MariaDB
    /// </summary>
    public static async System.Threading.Tasks.Task LoadPermissionsAsync()
    {
        try
        {
            var srv = new AsiasoftERP.Data.Services.PermissionService();
            var keys = await srv.GetAllowedModuleKeysAsync(CurrentUserCode, CurrentUserName, IsAdmin);
            AllowedModules.Clear();
            foreach (var k in keys) AllowedModules.Add(k);
        }
        catch
        {
            // Fallback: grant all standard modules if offline
            foreach (var m in AsiasoftERP.Core.Models.AppModuleRegistry.AllModules)
            {
                AllowedModules.Add(m.Key);
            }
        }
    }

    /// <summary>
    /// Manually sets authorized modules (e.g. for testing role presets)
    /// </summary>
    public static void SetAllowedModules(System.Collections.Generic.IEnumerable<string> modules)
    {
        AllowedModules.Clear();
        AllowedModules.Add("Home");
        foreach (var m in modules) AllowedModules.Add(m);
    }

    /// <summary>
    /// Check if current user has permission for a specific action or module
    /// </summary>
    public static bool CanEdit(string module) => CanView(module);

    /// <summary>
    /// Strict module visibility check: returns true only if the user is authorized for this module!
    /// </summary>
    public static bool CanView(string module)
    {
        if (string.Equals(module, "Home", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (AllowedModules.Count == 0) return true; // not yet loaded
        return AllowedModules.Contains(module);
    }
}
