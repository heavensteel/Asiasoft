using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Model representing a single row in the Cari Hesap Ekstresi (Account Statement)
/// </summary>
public class CariEkstreRow
{
    public long SiraNo { get; set; }
    public DateTime? Tarih { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public string Turu { get; set; } = string.Empty;
    public string BelgeNo { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    
    // Financials
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
    public string BakiyeDurumu { get; set; } = string.Empty; // "(B)" or "(A)"
    
    // Foreign Currency
    public string DovizTuru { get; set; } = string.Empty;
    public decimal DovizBorc { get; set; }
    public decimal DovizAlacak { get; set; }
    public decimal DovizBakiye { get; set; }
    
    // Additional Details
    public string Kullanici { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public double Miktar { get; set; }
    public string Plaka { get; set; } = string.Empty;
    
    public bool IsDevir { get; set; }
}

/// <summary>
/// Filter criteria for Cari Hesap Ekstresi
/// </summary>
public class CariEkstreFilter
{
    public double FirKod { get; set; }
    public DateTime BaslangicTarihi { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);
    public DateTime BitisTarihi { get; set; } = DateTime.Now.Date;
    public bool SadeceBakiyeliler { get; set; } = false;
    public string? DovizTuru { get; set; }
}

/// <summary>
/// Summary totals for Cari Hesap Ekstresi
/// </summary>
public class CariEkstreSummary
{
    public decimal DevirBorc { get; set; }
    public decimal DevirAlacak { get; set; }
    public decimal DevirBakiye => DevirBorc - DevirAlacak;
    
    public decimal DonemBorc { get; set; }
    public decimal DonemAlacak { get; set; }
    public decimal DonemFark => DonemBorc - DonemAlacak;

    public decimal GenelBorc => DevirBorc + DonemBorc;
    public decimal GenelAlacak => DevirAlacak + DonemAlacak;
    public decimal GenelBakiye => GenelBorc - GenelAlacak;
    
    public string GenelBakiyeDurumu => GenelBakiye > 0 ? "BORÇLU (B)" : GenelBakiye < 0 ? "ALACAKLI (A)" : "BAKİYESİZ (0)";
    public int ToplamKayitSayisi { get; set; }
}

/// <summary>
/// DTO for quick Cari search and autocomplete picker
/// </summary>
public class CariLookupDto
{
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string? Unvan { get; set; }
    public string? VergiDairesi { get; set; }
    public string? VergiNo { get; set; }
    public string? Telefon { get; set; }
    public string? Sehir { get; set; }
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye => Borc - Alacak;
    public string BakiyeDurumu => Bakiye > 0 ? "(B)" : Bakiye < 0 ? "(A)" : "(0)";
}
