using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Service providing access to Tek Asya Tarım's Ethylene and Degreening Chamber System (Etilen & Sarartma Odaları)
/// Reads live from network path \\192.168.1.252\it\ANIL\etilen_stok_v7.json with offline local cache fallback
/// </summary>
public class EtilenService
{
    public static readonly string PrimaryServerPath = @"\\192.168.1.252\it\ANIL\etilen_stok_v7.json";
    public static readonly string DesktopExePath = @"\\192.168.1.252\it\ANIL\new.exe.etilen\TekAsyaEtilenStokTakip.exe";
    public static readonly string SharedDirectoryPath = @"\\192.168.1.252\it\ANIL";

    private static readonly string LocalCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TekAsyaTarim",
        "etilen_stok_v7_local_cache.json"
    );

    /// <summary>
    /// Loads the latest room status and lots from the network JSON database
    /// </summary>
    public async Task<(EtilenDatabaseRoot data, bool isLiveNetwork, string message)> LoadDataAsync()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // 1. Try reading live from network share
        try
        {
            if (File.Exists(PrimaryServerPath))
            {
                using var fs = new FileStream(PrimaryServerPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var root = await JsonSerializer.DeserializeAsync<EtilenDatabaseRoot>(fs, options);
                if (root != null)
                {
                    // Update local cache
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(LocalCachePath)!);
                        await File.WriteAllBytesAsync(LocalCachePath, await File.ReadAllBytesAsync(PrimaryServerPath));
                    }
                    catch { }

                    return (root, true, "🟢 Merkezi Sunucu Bağlı (Canlı Senkronize)");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EtilenService] Network read error: {ex.Message}");
        }

        // 2. Fallback to local cache
        try
        {
            if (File.Exists(LocalCachePath))
            {
                using var fs = new FileStream(LocalCachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var root = await JsonSerializer.DeserializeAsync<EtilenDatabaseRoot>(fs, options);
                if (root != null)
                {
                    return (root, false, "🟠 Çevrimdışı Mod (Yerel Önbellek Aktif)");
                }
            }
        }
        catch { }

        return (new EtilenDatabaseRoot(), false, "❌ Veritabanı okunamadı (Sunucuya ve yerel önbelleğe ulaşılamadı)");
    }

    /// <summary>
    /// Launches the standalone TekAsyaEtilenStokTakip.exe application
    /// </summary>
    public bool LaunchDesktopApp(out string error)
    {
        error = string.Empty;
        try
        {
            if (File.Exists(DesktopExePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DesktopExePath,
                    UseShellExecute = true
                });
                return true;
            }

            error = $"Etilen uygulaması bulunamadı: {DesktopExePath}";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Opens Windows Explorer pointing to the network share \\192.168.1.252\it\ANIL
    /// </summary>
    public void OpenNetworkFolder()
    {
        try
        {
            if (Directory.Exists(SharedDirectoryPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = SharedDirectoryPath,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }
}
