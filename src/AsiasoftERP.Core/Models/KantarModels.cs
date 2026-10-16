using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// DTO representing Mal Kabul / Kantar Tartım record from table 'kantargiris' and 'kantargirisana'
/// </summary>
public class KantarTartimDto
{
    public int IdNo { get; set; }
    public int GirisKodu { get; set; }
    public string SirKod { get; set; } = "1";
    public DateTime Tarih { get; set; } = DateTime.Today;
    public string GirisSaati { get; set; } = DateTime.Now.ToString("HH:mm");
    public string CikisSaati { get; set; } = DateTime.Now.ToString("HH:mm");
    public string Plaka { get; set; } = string.Empty;
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public decimal Burut { get; set; } // 1. Tartım / Brüt KG
    public decimal Dara { get; set; }  // 2. Tartım / Dara KG
    public decimal Net => Burut - Dara; // Net KG
    public decimal Firesiz => Net;
    public double DepoKodu { get; set; } = 1;
    public string DepoAdi { get; set; } = "MERKEZ";
    public string SoforAdi { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string KantarAdi { get; set; } = "TEKASYA MERKEZ KANTARI-ANTAKYA";
    public string Kullanici { get; set; } = string.Empty;
    public DateTime IslemTarihi { get; set; } = DateTime.Now;
}

/// <summary>
/// Lookup item for Autocomplete / ComboBox selections
/// </summary>
public class LookupItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrEmpty(Code) ? Name : $"{Code} - {Name}";
}

/// <summary>
/// Enterprise weighbridge definition with multi-IP (Lokal + FortiVPN / Şube) routing
/// Stored in MariaDB table 'erp.kantar_tanimlari'
/// </summary>
public class KantarTanimDto
{
    public int Id { get; set; }
    public string KantarKodu { get; set; } = "MERKEZ";
    public string KantarAdi { get; set; } = "Tek Asya Merkez Kantarı (Antakya)";
    public string SubeAdi { get; set; } = "Merkez Fabrika & Mal Kabul";
    public string SubnetPrefix { get; set; } = "192.168.60.";
    public string LokalIp { get; set; } = "192.168.60.116";
    public int LokalPort { get; set; } = 8080;
    public string VpnIp { get; set; } = "192.168.45.116";
    public int VpnPort { get; set; } = 8080;
    public string IndikatorModeli { get; set; } = "Tunaylar / Generic TCP";
    public string BaglantiModu { get; set; } = "Auto"; // 'Auto', 'Lokal', 'VPN'
    public bool Aktif { get; set; } = true;
    public string? Aciklama { get; set; }

    public override string ToString() => $"{KantarAdi} ({SubeAdi})";
}

/// <summary>
/// User-specific Scale connection settings
/// Stored in MariaDB table 'kullanici_kantar_ayarlari'
/// </summary>
public class KantarAyarDto
{
    public string KullaniciKodu { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public bool KantarAktif { get; set; } = false;
    public string KantarAdi { get; set; } = "Tek Asya Merkez Kantarı";
    public string SubeKodu { get; set; } = "MERKEZ";

    // Primary Local Network (Varsayılan 192.168.60.* bloğu)
    public string LokalIp { get; set; } = "192.168.60.116";
    public int LokalPort { get; set; } = 8080;

    // FortiClient VPN / Şube Network (192.168.45.* veya 192.168.31.*)
    public string VpnIp { get; set; } = "192.168.45.116";
    public int VpnPort { get; set; } = 8080;

    // Connection Mode: 'Auto' (Akıllı Lokal & FortiVPN Otomatik Geçiş), 'Lokal', 'VPN'
    public string BaglantiModu { get; set; } = "Auto";

    public int TimeoutMs { get; set; } = 3000;

    // Backwards-compatible accessors
    public string IpAdresi
    {
        get => !string.IsNullOrEmpty(LokalIp) ? LokalIp : "192.168.60.116";
        set
        {
            LokalIp = value;
            if (string.IsNullOrEmpty(VpnIp)) VpnIp = value;
        }
    }

    public int Port
    {
        get => LokalPort > 0 ? LokalPort : 8080;
        set
        {
            LokalPort = value;
            if (VpnPort <= 0) VpnPort = value;
        }
    }
}

/// <summary>
/// Diagnostic info about local PC's active network interfaces & FortiVPN connection
/// </summary>
public class NetworkDiagnosticInfo
{
    public List<string> LocalIps { get; set; } = new();
    public bool HasFortiVpn { get; set; } = false;
    public string ActiveSubnet { get; set; } = "Bilinmiyor";
    public string DetectedAdapter { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
