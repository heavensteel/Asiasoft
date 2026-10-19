using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Tır / Kamyon sefer kartı ve maliyet ana kaydı (erp.sefer_fis)
/// </summary>
public class SeferFisDto
{
    public double SiraNo { get; set; }
    public string Sirkod { get; set; } = "1";
    public double FisNo { get; set; }
    public double FisTur { get; set; } = 753;
    public DateTime Tarih { get; set; } = DateTime.Today;
    public DateTime? IslemTarihi { get; set; }
    public DateTime? CikisTarihi { get; set; }
    public DateTime? DonusTarihi { get; set; }

    public string SeferTuru { get; set; } = "YURT DIŞI"; // YURT İÇİ, YURT DIŞI, KATRA
    public string Durumu { get; set; } = "AÇIK"; // AÇIK (Yolda), KAPALI (Tamamlandı)

    // Araç & Personel Bilgileri
    public double FirkodPlaka { get; set; }
    public string Plaka { get; set; } = string.Empty; // FIRMAADI_PLAKA (Çekici)
    public double FirkodTer { get; set; }
    public string DorsePlaka { get; set; } = string.Empty; // FIRMAADI_TER (Dorse / Termokin)
    public double Firkod { get; set; }
    public string SurucuAdi { get; set; } = string.Empty; // FIRMAADI (Şoför veya Taşeron Firma)

    // Güzergah & Yük
    public string CikisNoktasi { get; set; } = "ANTAKYA";
    public string VarisNoktasi { get; set; } = string.Empty;
    public string YuklemeBosaltmaYeri { get; set; } = string.Empty;
    public string YukCinsi { get; set; } = "NARENCİYE";

    // KM ve Yakıt (Mazot) Hesabı
    public double CikisKm { get; set; }
    public double DonusKm { get; set; }
    public double YapilanKm => (DonusKm > CikisKm && CikisKm > 0) ? (DonusKm - CikisKm) : 0;

    public double CikisLitre { get; set; }
    public double ToplamAlinanLitre { get; set; }
    public double SeferDonusKalanLitre { get; set; }
    public double TuketilenToplamLitre => (CikisLitre + ToplamAlinanLitre) >= SeferDonusKalanLitre
        ? (CikisLitre + ToplamAlinanLitre - SeferDonusKalanLitre)
        : 0;

    public double YuzKmTuketim => YapilanKm > 0
        ? Math.Round((TuketilenToplamLitre / YapilanKm) * 100, 2)
        : 0;

    // Maliyet & Finans
    public double SeferBedeli { get; set; } // Anlaşılan Navlun (TL)
    public double SeferTutar { get; set; } // Dövizli Navlun
    public string SeferDovizTuru { get; set; } = "EUR";
    public double DovizKur { get; set; } = 1.0;

    public double AlinanTlAvans { get; set; }
    public double AlinanDovizAvans { get; set; }
    public double PrimTutariTl { get; set; } // Şoför Sefer Primi

    public double ToplamMasrafTl { get; set; } // Detay masraflar toplamı TL
    public double ToplamDovizMasraf { get; set; }

    public double AvansMasrafFarkiTl => (AlinanTlAvans + (AlinanDovizAvans * (DovizKur > 0 ? DovizKur : 1))) - ToplamMasrafTl;

    public double NetKarZararTl => SeferBedeli - (ToplamMasrafTl + (PrimTutariTl > 0 ? PrimTutariTl : 0));

    public string Aciklama { get; set; } = string.Empty;
    public string Kullanici { get; set; } = string.Empty;

    // Detay listesi (Dialog ve Rapor için)
    public List<SeferDetayDto> Detaylar { get; set; } = new();
}

/// <summary>
/// Sefer esnasında gerçekleşen masraf kalemleri (erp.sefer_detay)
/// </summary>
public class SeferDetayDto
{
    public double SiraNo { get; set; }
    public string Sirkod { get; set; } = "1";
    public double FisNo { get; set; }
    public double FisTur { get; set; } = 753;
    public DateTime Tarih { get; set; } = DateTime.Today;

    public string HizmetKodu { get; set; } = string.Empty;
    public string HizmetAdi { get; set; } = string.Empty;
    public string Birim { get; set; } = "TL"; // TL, EUR, USD
    public double Tutar { get; set; } // TL tutarı
    public double DovizTutari { get; set; } // Döviz tutarı
    public double DovizTlKarsiligi { get; set; }

    public string Aciklama { get; set; } = string.Empty;
    public string Kullanici { get; set; } = string.Empty;
}

/// <summary>
/// Araç & Dorse & Şoför F10 Arama DTO (erp.araclar)
/// </summary>
public class AracLookupDto
{
    public double AracKodu { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string DorsePlaka { get; set; } = string.Empty;
    public string Marka { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Tipi { get; set; } = string.Empty;
    public string AracSeferTipi { get; set; } = string.Empty; // YURT İÇİ, YURT DIŞI
    public string SurucuAdi { get; set; } = string.Empty;
    public double SurucuKodu { get; set; }

    public string DisplayText => $"{Plaka} {(string.IsNullOrWhiteSpace(DorsePlaka) ? "" : "/ " + DorsePlaka)} - {SurucuAdi} ({Marka})";
}

/// <summary>
/// Sefer Dashboard KPI Özet DTO
/// </summary>
public class SeferKpiDto
{
    public int ToplamSefer { get; set; }
    public int AcikSefer { get; set; }
    public int KapaliSefer { get; set; }
    public double ToplamKm { get; set; }
    public double Ortalama100KmSarfiyat { get; set; }
    public double ToplamMasrafTl { get; set; }
    public double ToplamNavlunTl { get; set; }
    public double ToplamNetKarTl { get; set; }
}
