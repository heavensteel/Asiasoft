using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Sipariş Yönü: Alınan (Müşteri Satış Siparişi) veya Verilen (Tedarikçi Satın Alma Siparişi)
/// </summary>
public enum SiparisYonu
{
    Alinan = 1,  // alinansiparisana / alinansiparisdetay
    Verilen = 2  // verilensiparisana / verilensiparisdetay
}

/// <summary>
/// Sipariş Ana Kaydı (alinansiparisana & verilensiparisana)
/// </summary>
public class SiparisDto
{
    public double IdNo { get; set; }
    public double FisNo { get; set; }
    public double FatNo { get; set; }
    public string Sirkod { get; set; } = "1";

    public DateTime Tarih { get; set; } = DateTime.Today;
    public DateTime? IslemTarihi { get; set; }
    public DateTime? BaslamaTarihi { get; set; } // Yükleme / Sevk başlangıç
    public DateTime? BitisTarihi { get; set; }   // Teslimat termin tarihi
    public DateTime? AnalizTarihi { get; set; }

    public SiparisYonu SiparisYonu { get; set; } = SiparisYonu.Alinan;
    public string Durumu { get; set; } = "SİPARİŞ ALINDI"; // SİPARİŞ ALINDI, HAZIRLANIYOR, YÜKLENDİ, SEVK EDİLDİ, TAMAMLANDI, İPTAL

    // Cari / Müşteri / Tedarikçi
    public string Firkod { get; set; } = string.Empty;
    public string FirmaAdi { get; set; } = string.Empty;
    public string Adresi { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Ulke { get; set; } = string.Empty;

    // Teslimat & Ticari Koşullar
    public string SatisSekli { get; set; } = "DAP"; // DAP, FOB, CIF, CPT, EXW
    public string PartiNo { get; set; } = string.Empty;
    public string Pozisyon { get; set; } = "1";
    public string SiparisiVeren { get; set; } = string.Empty;
    public string SiparisiAlan { get; set; } = string.Empty;

    // Lojistik & Araç
    public string Nakliyeci { get; set; } = string.Empty;
    public string AracPlaka { get; set; } = string.Empty;
    public string AracPlaka2 { get; set; } = string.Empty; // Dorse
    public string SoforAdi { get; set; } = string.Empty;
    public string SoforTelefon { get; set; } = string.Empty;
    public string FrigoDerece { get; set; } = "+4°C";

    // Adres & Gümrük Detayları
    public string Gumruk { get; set; } = string.Empty;
    public string GumrukAdres { get; set; } = string.Empty;
    public string BosaltmaAdresi { get; set; } = string.Empty;

    // Miktar ve Tutarlar
    public double PaletToplam { get; set; }
    public double KasaToplam { get; set; }
    public double BurutToplam { get; set; }
    public double NetToplam { get; set; } // Net KG

    public double ToplamTutar { get; set; }
    public string DovizTuru { get; set; } = "EUR";
    public double DovizKuru { get; set; } = 55.10;
    public double ToplamTutarTl => ToplamTutar * (DovizKuru > 0 ? DovizKuru : 1.0);

    public string Aciklama { get; set; } = string.Empty;
    public string Aciklama2 { get; set; } = string.Empty;
    public string Kullanici { get; set; } = string.Empty;

    // Detay Listesi
    public List<SiparisDetayDto> Detaylar { get; set; } = new();
}

/// <summary>
/// Sipariş Kalem Detayı (alinansiparisdetay & verilensiparisdetay)
/// </summary>
public class SiparisDetayDto
{
    public double IdNo { get; set; }
    public double FisNo { get; set; }
    public double FatNo { get; set; }
    public double Sira { get; set; }
    public string Sirkod { get; set; } = "1";
    public DateTime Tarih { get; set; } = DateTime.Today;

    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public string Boy { get; set; } = string.Empty;        // Örn: 58MM+, 57MM+, 1-2 Boy
    public string Kalite { get; set; } = "1.KALİTE";       // 1.KALİTE, EKSTRA, SANAYİ

    public string KutuKodu { get; set; } = string.Empty;
    public string KutuAdi { get; set; } = string.Empty;    // Örn: 30*40*18 PLASTİK KASA MAVİ
    public double KutuDara { get; set; }
    public double KutuKg { get; set; }

    public string PaletKodu { get; set; } = string.Empty;
    public string PaletAdi { get; set; } = string.Empty;   // Örn: 80*120 EPAL PALET
    public double PaletDara { get; set; }

    public double PaletSayisi { get; set; }
    public double ParcaSayisi { get; set; }                 // Kasa / Kutu Sayısı

    public double BurutKg { get; set; }
    public double NetKg { get; set; }
    public double Fiyati { get; set; }
    public string DovizTuru { get; set; } = "EUR";
    public double Tutari { get; set; }

    public string AmbKodu { get; set; } = string.Empty;
    public string AmbAdi { get; set; } = string.Empty;     // Örn: 30*40 ELASTİK MAVİ FİLE
    public string Birim { get; set; } = "KG";              // KG, KASA, ADET, PALET
    public double Kdv { get; set; } = 0;
    public double Dara { get; set; }
}

/// <summary>
/// Sipariş Yönetimi Dashboard KPI Kartları Modeli
/// </summary>
public class SiparisKpiDto
{
    public int ToplamSiparis { get; set; }
    public int BekleyenSiparis { get; set; }
    public int HazirlananSiparis { get; set; }
    public int SevkEdilenSiparis { get; set; }

    public double ToplamPalet { get; set; }
    public double ToplamKasa { get; set; }
    public double ToplamNetKg { get; set; }

    public double ToplamEurTutar { get; set; }
    public double ToplamUsdTutar { get; set; }
    public double ToplamTlTutar { get; set; }
}

/// <summary>
/// Ürün ve Stok Seçim Arama DTO (F10)
/// </summary>
public class StokLookupDto
{
    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public string Birim { get; set; } = "KG";
    public double Fiyat { get; set; }
    public string Doviz { get; set; } = "TL";
    public double StokMiktari { get; set; }
}
