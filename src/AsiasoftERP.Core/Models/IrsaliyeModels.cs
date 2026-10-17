using System;

namespace AsiasoftERP.Core.Models;

public enum IrsaliyeFaturaDurumu
{
    Tumu,
    AcikIrsaliyeler, // Faturalanmamış
    Faturalananlar   // FATNO > 0
}

/// <summary>
/// Satış ve Sevk İrsaliyesi Üst Bilgi DTO (irsstkfis tablosu ile birebir eşleşir)
/// </summary>
public class IrsaliyeStkDto
{
    public double SiraNo { get; set; }
    public string Sirkod { get; set; } = "1";
    public double FisTur { get; set; } = 222; // 222 = Satış İrsaliyesi
    public double FisNo { get; set; }
    public double IrsNo { get; set; }
    public string GibIrsNo { get; set; } = string.Empty; // E-İrsaliye Belge No
    public DateTime? FisTar { get; set; } = DateTime.Today;
    public DateTime? IrsTar { get; set; } = DateTime.Today;
    public TimeSpan? Saat { get; set; }

    // Cari Bilgileri
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string FirmaAdresi { get; set; } = string.Empty;
    public string VergiDairesi { get; set; } = string.Empty;
    public string VergiNumarasi { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Ulke { get; set; } = "TÜRKİYE";

    // Nakliye ve Araç Bilgileri
    public string Plaka { get; set; } = string.Empty;        // Çekici Plaka
    public string ArkaPlaka { get; set; } = string.Empty;    // Dorse Plaka
    public string SoforAdi { get; set; } = string.Empty;
    public string SoforSoyadi { get; set; } = string.Empty;
    public string SoforTc { get; set; } = string.Empty;
    public string SoforTelefon { get; set; } = string.Empty;
    public string TasiyiciFirma { get; set; } = string.Empty;
    public string TasiyiciFirmaVkn { get; set; } = string.Empty;

    // Depo & Sevkiyat
    public double DepoKodu { get; set; } = 9;
    public string DepoAdi { get; set; } = "ANA DEPO";
    public string Aciklama { get; set; } = string.Empty;
    public string SiparisNo { get; set; } = string.Empty;

    // Miktar & Kasa Toplamları
    public double ToplamPalet { get; set; }
    public double ToplamKasa { get; set; }
    public double ToplamMiktar { get; set; } // Net KG
    public double ToplamFire { get; set; }

    // Parasal Toplamlar
    public double FisTut { get; set; }       // Ara toplam
    public double IskTut { get; set; }       // İskonto tutarı
    public double KdvTut { get; set; }       // KDV tutarı
    public double GenTut { get; set; }       // Genel toplam
    public string DovizTuru { get; set; } = "TL";
    public double DovizTutar { get; set; }
    public double DovizKur { get; set; } = 1.0;

    // Fatura Bağlantısı
    public double? FatNo { get; set; }
    public string Kapandi { get; set; } = string.Empty;
    public string User { get; set; } = "Ali SARIGÜL";

    // Yardımcı Özellikler
    public string BelgeNoDisplay => !string.IsNullOrWhiteSpace(GibIrsNo) 
        ? GibIrsNo 
        : (IrsNo > 0 ? IrsNo.ToString("N0") : FisNo.ToString("N0"));

    public string SoforTamAdi => $"{SoforAdi} {SoforSoyadi}".Trim();

    public bool IsFaturalandi => FatNo.HasValue && FatNo.Value > 0;

    public string DurumDisplay => IsFaturalandi 
        ? $"Faturalandı (#{FatNo:N0})" 
        : "Açık İrsaliye (Bekliyor)";

    public DateTime? BelgeTarihi => FisTar ?? IrsTar;
    public string FisTarFormatted => BelgeTarihi?.ToString("dd.MM.yyyy") ?? "";
    public string TarihFormatted => BelgeTarihi?.ToString("dd.MM.yyyy") ?? "";
    public string SevkSaatFormatted => Saat.HasValue ? Saat.Value.ToString(@"hh\:mm") : "00:00";
}

/// <summary>
/// Satış ve Sevk İrsaliyesi Kalem Detay DTO (irsstkdet tablosu ile birebir eşleşir)
/// </summary>
public class IrsaliyeDetayStkDto
{
    public double SiraNo { get; set; }
    public string Sirkod { get; set; } = "1";
    public double FisTur { get; set; } = 222;
    public double FisNo { get; set; }
    public double IrsNo { get; set; }
    public double Sira { get; set; }

    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public string Birimi { get; set; } = "KG";

    // Narenciye & Yaş Meyve Ambalaj Ölçüleri
    public double Palet { get; set; }
    public double Kasa { get; set; }
    public double Burut { get; set; }   // Brüt KG
    public double Dara { get; set; }    // Kasa/Palet Darası KG
    public double Miktar { get; set; }  // Net KG (Burut - Dara)
    public double FireOrani { get; set; }
    public double FireToplami { get; set; }

    // Fiyatlandırma
    public double Fiyati { get; set; }
    public double Kdv { get; set; } = 1;
    public double KdvTut { get; set; }
    public double Tutar { get; set; }

    // Döviz
    public string DovizTuru { get; set; } = "TL";
    public double DovizFiyati { get; set; }
    public double DovizTutar { get; set; }

    // HKS & Takip
    public string KunyeNo { get; set; } = string.Empty;
    public double Barkodu { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
}
