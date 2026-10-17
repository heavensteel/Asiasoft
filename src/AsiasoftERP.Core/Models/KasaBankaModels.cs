using System;

namespace AsiasoftERP.Core.Models;

public enum FinansIslemTipi
{
    KasaTahsilat,       // Müşteriden nakit tahsilat (Kasa Borç, Cari Alacak)
    KasaTediye,         // Nakit ödeme (Kasa Alacak, Cari Borç)
    BankaTahsilat,      // Gelen havale/EFT (Banka Borç, Cari Alacak)
    BankaTediye,        // Gönderilen havale/EFT (Banka Alacak, Cari Borç)
    Virman              // Kasalar veya bankalar arası virman
}

public class KasaKartDto
{
    public string Sirkod { get; set; } = "1";
    public double Kodu { get; set; }
    public string Adi { get; set; } = string.Empty;
    public string DovizTuru { get; set; } = "TL";
    public string MuhKodu { get; set; } = string.Empty;

    // Canlı Hesaplanan Bakiyeler
    public double ToplamGiris { get; set; }
    public double ToplamCikis { get; set; }
    public double Bakiye => ToplamGiris - ToplamCikis;

    public string BakiyeDisplay => $"{Bakiye:N2} {DovizTuru}";
}

public class BankaHesapDto
{
    public string Sirkod { get; set; } = "1";
    public double Kodu { get; set; }
    public string Adi { get; set; } = string.Empty;
    public string SubeAdi { get; set; } = string.Empty;
    public string HesapNo { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string DovizCinsi { get; set; } = "TL";
    public string MuhKodu { get; set; } = string.Empty;

    // Canlı Hesaplanan Bakiyeler
    public double ToplamYatan { get; set; }
    public double ToplamCekilen { get; set; }
    public double Bakiye => ToplamYatan - ToplamCekilen;

    public string BakiyeDisplay => $"{Bakiye:N2} {DovizCinsi}";
    public string IbanFormatted => string.IsNullOrWhiteSpace(Iban) ? "-" : Iban;
}

public class FinansHareketDto
{
    public double SiraNo { get; set; }
    public string Sirkod { get; set; } = "1";
    public double FisNo { get; set; }
    public double MakbuzNo { get; set; }
    public DateTime? FisTar { get; set; }
    public TimeSpan? Saat { get; set; }
    
    public string IslemTipi { get; set; } = "Kasa Tahsilat"; // Kasa Tahsilat, Kasa Tediye, Gelen Havale, Gönderilen Havale, Virman
    public string HesapTuru { get; set; } = "Kasa";          // Kasa / Banka
    public double HesapKodu { get; set; }
    public string HesapAdi { get; set; } = string.Empty;

    public double FirKod { get; set; }
    public string CariAdi { get; set; } = string.Empty;
    public string BelgeNo { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;

    public double Borc { get; set; }    // Tahsilat / Yatan (Giriş)
    public double Alacak { get; set; }  // Tediye / Çekilen (Çıkış)
    public double Bakiye { get; set; }  // Yürüyen Bakiye

    public string DovizTuru { get; set; } = "TL";
    public double DovizTutar { get; set; }
    public double DovizKur { get; set; } = 1.0;

    public string User { get; set; } = string.Empty;

    public string TarihFormatted => FisTar?.ToString("dd.MM.yyyy") ?? "";
    public string SaatFormatted => Saat.HasValue ? Saat.Value.ToString(@"hh\:mm") : "00:00";
    public string NetTutarDisplay => Borc > 0 ? $"+{Borc:N2}" : $"-{Alacak:N2}";
}

public class FinansFisGirisDto
{
    public string Sirkod { get; set; } = "1";
    public double FisNo { get; set; }
    public double MakbuzNo { get; set; }
    public DateTime FisTar { get; set; } = DateTime.Today;
    public TimeSpan Saat { get; set; } = DateTime.Now.TimeOfDay;

    public FinansIslemTipi IslemTipi { get; set; } = FinansIslemTipi.KasaTahsilat;

    // Kasa / Banka Seçimi
    public double KasaKodu { get; set; } = 4; // Varsayılan Merkez TL
    public string KasaAdi { get; set; } = "MERKEZ TL";

    public double BankaKodu { get; set; } = 2; // Varsayılan QNB Finansbank
    public string BankaAdi { get; set; } = "QNB FİNANSBANK";

    // Cari Seçimi
    public double FirKod { get; set; }
    public string CariAdi { get; set; } = string.Empty;
    public string VergiDairesi { get; set; } = string.Empty;
    public string VergiNo { get; set; } = string.Empty;
    public string CariAdresi { get; set; } = string.Empty;

    // Tutar & Para Birimi
    public double Tutar { get; set; }
    public string DovizTuru { get; set; } = "TL";
    public double DovizKur { get; set; } = 1.0;
    public double DovizTutar { get; set; }

    public string BelgeNo { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string YaziIle { get; set; } = string.Empty;
    public string User { get; set; } = "Ali SARIGÜL";
}
