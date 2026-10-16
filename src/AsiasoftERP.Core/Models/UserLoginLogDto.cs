using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// User login and session audit log DTO
/// </summary>
public class UserLoginLogDto
{
    public int Id { get; set; }
    public string KullaniciKodu { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public string SirKod { get; set; } = "1";
    public string SirketAdi { get; set; } = string.Empty;
    public string BilgisayarAdi { get; set; } = string.Empty;
    public string WindowsKullanici { get; set; } = string.Empty;
    public string IpAdresi { get; set; } = string.Empty;
    public string OturumTuru { get; set; } = "Konsol";
    public string RdpClientName { get; set; } = string.Empty;
    public DateTime GirisZamani { get; set; } = DateTime.Now;
    public DateTime? CikisZamani { get; set; }
    public string Durum { get; set; } = "GİRİŞ BAŞARILI";
    public string Detay { get; set; } = string.Empty;

    public bool IsSuccess => Durum.Contains("BAŞARILI", StringComparison.OrdinalIgnoreCase);
    public bool IsActiveSession => CikisZamani == null && IsSuccess;

    public string SureDisplay
    {
        get
        {
            if (CikisZamani.HasValue)
            {
                var diff = CikisZamani.Value - GirisZamani;
                if (diff.TotalHours >= 1) return $"{(int)diff.TotalHours} sa {diff.Minutes} dk";
                if (diff.TotalMinutes >= 1) return $"{(int)diff.TotalMinutes} dk";
                return $"{(int)diff.TotalSeconds} sn";
            }
            if (IsSuccess) return "🟢 Aktif Oturum";
            return "—";
        }
    }
}
