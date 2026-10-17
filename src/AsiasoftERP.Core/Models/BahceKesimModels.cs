using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// DTO representing an Orchard Purchase & Cutting Contract from 'sozlesmebahceana'
/// and aggregated harvest costs from 'bahce_kesim_maliyet'.
/// </summary>
public class BahceSozlesmeDto
{
    public double SiraNo { get; set; }
    public double SozlesmeNo { get; set; }
    public string SozlesmeTuru { get; set; } = "TONAJ"; // 'TONAJ' (KG birim fiyatı) veya 'KABALA' (götürü sabit tutar)
    public string SozlesmeAdi { get; set; } = string.Empty;
    public DateTime Tarih { get; set; } = DateTime.Today;
    public string BelgeNo { get; set; } = string.Empty;

    // Bahçe Sahibi / Müstahsil Cari
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string BahceAdi { get; set; } = string.Empty;
    public string Mevki { get; set; } = string.Empty;
    public double BolgeKodu { get; set; }
    public string BolgeAdi { get; set; } = string.Empty;

    // Tapu & Parsel Bilgileri
    public string Ada { get; set; } = string.Empty;
    public string Parsel { get; set; } = string.Empty;
    public string Durumu { get; set; } = "Aktif"; // 'Aktif', 'Tamamlandı', 'İptal'

    // Ürün & Rekolte Şartları
    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public string Birim { get; set; } = "KG";
    public double HedefKg { get; set; }
    public double Fiyat { get; set; }
    public double SozlesmeTutari { get; set; }
    public double KabalaFiyat { get; set; }

    // Tahmini ambalaj
    public double TahBox { get; set; }
    public double TahPalet { get; set; }
    public double TahKasa { get; set; }

    // Gerçekleşen Kesim Bilgileri (From 'bahce_kesim_maliyet')
    public double GerceklesenKg { get; set; }
    public double GerceklesenMaliyet { get; set; }
    public int SeferSayisi { get; set; }

    public string Aciklama { get; set; } = string.Empty;
    public string Kullanici { get; set; } = string.Empty;

    // Display Helpers
    public string TarihFormatted => Tarih.ToString("dd.MM.yyyy");
    public string HedefTonFormatted => HedefKg > 0 ? (HedefKg / 1000.0).ToString("N1") + " Ton" : (SozlesmeTuru == "KABALA" ? "KABALA" : "-");
    public string GerceklesenTonFormatted => (GerceklesenKg / 1000.0).ToString("N1") + " Ton";
    public string SozlesmeTutariFormatted => SozlesmeTutari > 0 ? SozlesmeTutari.ToString("N0") + " TL" : (KabalaFiyat > 0 ? KabalaFiyat.ToString("N0") + " TL" : "-");
    public string GerceklesenMaliyetFormatted => GerceklesenMaliyet > 0 ? GerceklesenMaliyet.ToString("N0") + " TL" : "-";
    
    public double BirimKgMaliyeti => GerceklesenKg > 0 ? GerceklesenMaliyet / GerceklesenKg : Fiyat;
    public string BirimKgMaliyetFormatted => BirimKgMaliyeti > 0 ? BirimKgMaliyeti.ToString("N2") + " TL/KG" : "-";

    public double TamamlanmaYuzdesi => HedefKg > 0 ? Math.Min(100.0, (GerceklesenKg / HedefKg) * 100.0) : (GerceklesenKg > 0 ? 100.0 : 0.0);
    public string TamamlanmaFormatted => $"%{TamamlanmaYuzdesi:0.0}";
}

/// <summary>
/// Individual harvest cutting trip detail from 'bahce_kesim_maliyet'
/// </summary>
public class BahceKesimSeferDto
{
    public double IdNo { get; set; }
    public double SozlesmeNo { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Today;
    public string Plaka { get; set; } = string.Empty;
    public string Sofor { get; set; } = string.Empty;
    public string Bolge { get; set; } = string.Empty;
    public string KesimCavusu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public double Kasa { get; set; }
    public double NetKg { get; set; }
    public double Fiyat { get; set; }
    public double MalTutari { get; set; }
    public double YevmiyeTutari { get; set; }
    public double ServisTutari { get; set; }
    public double NakliyeTutari { get; set; }
    public double ExperTutari { get; set; }
    public double GenelToplam { get; set; }
    public double KgMaliyet { get; set; }

    public string TarihFormatted => Tarih.ToString("dd.MM.yyyy");
    public string NetKgFormatted => NetKg.ToString("N0") + " KG";
    public string GenelToplamFormatted => GenelToplam.ToString("N2") + " TL";
    public string KgMaliyetFormatted => KgMaliyet.ToString("N2") + " TL";
}

/// <summary>
/// Dashboard KPI Summary for Orchard Contracts & Harvest Operations
/// </summary>
public class BahceKpiDto
{
    public int ToplamSozlesme { get; set; }
    public int AktifSozlesme { get; set; }
    public double ToplamTaahhutTon { get; set; }
    public double ToplamGerceklesenTon { get; set; }
    public double ToplamSozlesmeHacmi { get; set; }
    public double ToplamGerceklesenMaliyet { get; set; }

    public double OrtalamaKgMaliyet => ToplamGerceklesenTon > 0 ? (ToplamGerceklesenMaliyet / (ToplamGerceklesenTon * 1000.0)) : 0.0;
}
