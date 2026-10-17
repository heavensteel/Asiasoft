using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// DTO representing the unified weighbridge (Kantar), harvest crew leader (Çavuş)
/// and transport shuttle/freight (Servis) matching record.
/// Joined from 'kantargirisana', 'cavusservis' and 'cavusbolgetanimi'.
/// </summary>
public class CavusKantarEslesmeDto
{
    public int IdNo { get; set; }
    public int GirisKodu { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Today;
    public string GirisSaati { get; set; } = string.Empty;
    public string Plaka { get; set; } = string.Empty;
    public string SoforAdi { get; set; } = string.Empty;
    public string UrunAdi { get; set; } = string.Empty;
    public string MustahsilAdi { get; set; } = string.Empty;
    public string Bolge { get; set; } = string.Empty;
    public double NetKg { get; set; }

    // Primary Çavuş (Crew Leader)
    public double CavusKodu { get; set; }
    public string CavusAdi { get; set; } = string.Empty;

    // Secondary Çavuş (if combined crews)
    public double CavusKodu2 { get; set; }
    public string CavusAdi2 { get; set; } = string.Empty;
    public double CavusYevmiye2 { get; set; }
    public double CavusServis2 { get; set; }

    // Servis & Transport Shuttle
    public double ServisSayisi { get; set; }
    public double ServisFiyati { get; set; }
    public double ServisTutari { get; set; }
    public double ToplamNakliye { get; set; }

    // Approval & Status
    public bool Onay { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string OnayKullanici { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;

    // Formatted display helpers
    public string TarihFormatted => Tarih.ToString("dd.MM.yyyy");
    public string NetKgFormatted => NetKg.ToString("N0") + " KG";
    public string ServisTutariFormatted => ServisTutari > 0 ? ServisTutari.ToString("N2") + " TL" : "-";

    public string EslesmeDurumu
    {
        get
        {
            if (Onay) return "Tam Eşleşti";
            if (CavusKodu > 0 && !string.IsNullOrWhiteSpace(CavusAdi) && ServisTutari > 0) return "Onay Bekliyor";
            if (CavusKodu <= 0 && string.IsNullOrWhiteSpace(CavusAdi)) return "Çavuş Eksik";
            if (ServisTutari <= 0) return "Servis Eksik";
            return "Eşleşmedi";
        }
    }
}

/// <summary>
/// Region route and standard service unit pricing for each Çavuş
/// Stored in MariaDB table 'erp.cavusbolgetanimi'
/// </summary>
public class CavusBolgeTanimDto
{
    public double IdNo { get; set; }
    public string SirKod { get; set; } = "1";
    public double FirKod { get; set; }
    public string CavusAdi { get; set; } = string.Empty;
    public double BolgeKodu { get; set; }
    public string BolgeAdi { get; set; } = string.Empty;
    public double BirimServisFiyati { get; set; }
    public double IsyeriNo { get; set; }
    public string YetkiKodu { get; set; } = string.Empty;

    public string BirimFiyatFormatted => BirimServisFiyati.ToString("N2") + " TL";
}

/// <summary>
/// Çavuş Lookup item for dropdowns and combo boxes
/// </summary>
public class CavusLookupItemDto
{
    public double FirKod { get; set; }
    public string CavusAdi { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrEmpty(CavusAdi) ? $"#{FirKod}" : $"{CavusAdi} (#{FirKod})";
}

/// <summary>
/// Region / Route Lookup item
/// </summary>
public class BolgeLookupItemDto
{
    public double BolgeKodu { get; set; }
    public string BolgeAdi { get; set; } = string.Empty;
    public double VarsayilanFiyat { get; set; }

    public override string ToString() => VarsayilanFiyat > 0 ? $"{BolgeAdi} ({VarsayilanFiyat:N0} TL)" : BolgeAdi;
}

/// <summary>
/// Top Dashboard KPI Summary for Çavuş - Kantar - Servis reconciliation
/// </summary>
public class CavusKantarKpiDto
{
    public int ToplamSefer { get; set; }
    public double ToplamNetKg { get; set; }
    public int EslesenSefer { get; set; }
    public int BekleyenSefer { get; set; }
    public double ToplamServisSayisi { get; set; }
    public double ToplamServisMaliyeti { get; set; }

    public double EslesmeOrani => ToplamSefer > 0 ? (double)EslesenSefer / ToplamSefer * 100.0 : 0.0;
    public double ToplamNetTon => ToplamNetKg / 1000.0;
}
