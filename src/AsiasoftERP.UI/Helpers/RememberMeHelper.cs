using Microsoft.Win32;

namespace AsiasoftERP.UI.Helpers;

/// <summary>
/// Stores/retrieves the Remember Me token in Windows Registry (per machine)
/// </summary>
public static class RememberMeHelper
{
    private const string RegistryKey = @"SOFTWARE\AsiasoftERP";
    private const string TokenValue = "RememberToken";
    private const string UsernameValue = "RememberUsername";

    public static void SaveToken(string username, string token)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
        key.SetValue(TokenValue, token);
        key.SetValue(UsernameValue, username);
    }

    public static (string username, string token) LoadToken()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
        if (key == null) return (string.Empty, string.Empty);

        var token = key.GetValue(TokenValue)?.ToString() ?? string.Empty;
        var username = key.GetValue(UsernameValue)?.ToString() ?? string.Empty;
        return (username, token);
    }

    public static void ClearToken()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
        if (key == null) return;
        key.DeleteValue(TokenValue, throwOnMissingValue: false);
        key.DeleteValue(UsernameValue, throwOnMissingValue: false);
    }

    public static bool HasSavedToken()
    {
        var (_, token) = LoadToken();
        return !string.IsNullOrEmpty(token);
    }
}
