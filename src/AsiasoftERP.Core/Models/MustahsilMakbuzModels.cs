using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// DTO representing an official Farmer/Producer Purchase Receipt (Müstahsil Makbuzu)
/// according to V.U.K. Madde 235 and stored in MariaDB 'mustahsilmakbuzu'.
/// </summary>
public class MustahsilMakbuzDto
{
    public double IdNo { get; set; }
    public string SirKod { get; set; } = "1";
    public double GirisKodu { get; set; }
    public double CariIdNo { get; set; }
    public string BagkurNo { get; set; } = string.Empty;
    public string TcNo { get; set; } = string.Empty;
    public string AdiSoyadi { get; set; } = string.Empty;
    public string BabaAdi { get; set; } = string.Empty;
    public string DogumTarihiYeri { get; set; } = string.Empty;
    public string Adresi { get; set; } = string.Empty;
    public double TgKodu { get; set; }
    public string TgAdi { get; set; } = string.Empty;
    public string KKullanici { get; set; } = string.Empty;
    public string DKullanici { get; set; } = string.Empty;
    public DateTime Tarih { get; set; } = DateTime.Today;
    public DateTime? IslemTarihi { get; set; } = DateTime.Now;
    public string Bolgesi { get; set; } = string.Empty;
    public double MakNo { get; set; }
    public string MakbuzNo { get; set; } = string.Empty;
    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public double Miktar { get; set; } // Net KG
    public double Fiyati { get; set; } // Birim Fiyat TL/KG
    public double Tutari { get; set; } // Brüt Tutar = Miktar * Fiyat
    public double StopajOrani { get; set; } = 2.0; // Standart Borsa Tescilli %2 Stopaj
    public double StopajTutari { get; set; }
    public double BagkurOrani { get; set; } = 2.0; // Standart %2 Bağkur
    public double BagkurTutari { get; set; }
    public double TicBorOrani { get; set; } = 0.2; // Ticaret Borsası Tescil Payı %0.2
    public double TicBorTutari { get; set; }
    public double NetKesinti { get; set; } // Stopaj + Bagkur + TicBor
    public double Odenen { get; set; } // Net Ödenen = Tutari - NetKesinti
    public string Yaziile { get; set; } = string.Empty;
    public string KunyeNo { get; set; } = string.Empty; // HKS Künye No
    public string Sehir { get; set; } = string.Empty;
    public string Ilece { get; set; } = string.Empty;
    public double IsyeriNo { get; set; } = 1;
    public string YetkiKodu { get; set; } = "MERKEZ";

    // Formatted Display Helpers
    public string TarihFormatted => Tarih.ToString("dd.MM.yyyy");
    public string MiktarFormatted => Miktar.ToString("N0") + " KG";
    public string FiyatFormatted => Fiyati.ToString("N2") + " TL";
    public string BrutTutarFormatted => Tutari.ToString("N2") + " TL";
    public string StopajFormatted => StopajTutari.ToString("N2") + " TL";
    public string BagkurFormatted => BagkurTutari.ToString("N2") + " TL";
    public string BorsaFormatted => TicBorTutari.ToString("N2") + " TL";
    public string NetKesintiFormatted => NetKesinti.ToString("N2") + " TL";
    public string OdenenFormatted => Odenen.ToString("N2") + " TL";
}

/// <summary>
/// Producer lookup DTO for farmers / müstahsiller with 11-digit TC Kimlik No
/// </summary>
public class MustahsilLookupDto
{
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string TcNo { get; set; } = string.Empty;
    public string VergiDairesi { get; set; } = string.Empty;
    public string Adres { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string BagkurNo { get; set; } = string.Empty;
    public string BabaAdi { get; set; } = string.Empty;

    public string DisplayText => $"{TcNo} - {FirmaAdi} ({Ilce}/{Sehir})";
}

/// <summary>
/// Dashboard KPI Summary for Müstahsil Makbuzları
/// </summary>
public class MustahsilKpiDto
{
    public int ToplamMakbuzSayisi { get; set; }
    public double ToplamNetKg { get; set; }
    public double ToplamBrutTutar { get; set; }
    public double ToplamStopaj { get; set; }
    public double ToplamBagkur { get; set; }
    public double ToplamBorsa { get; set; }
    public double ToplamNetKesinti { get; set; }
    public double ToplamNetOdenen { get; set; }

    public string ToplamTonFormatted => (ToplamNetKg / 1000.0).ToString("N1") + " Ton";
    public string BrutHacimFormatted => ToplamBrutTutar.ToString("N0") + " TL";
    public string NetOdenenFormatted => ToplamNetOdenen.ToString("N0") + " TL";
    public string ToplamKesintiFormatted => ToplamNetKesinti.ToString("N0") + " TL";
    public string StopajFormatted => ToplamStopaj.ToString("N0") + " TL";
    public string BagkurFormatted => ToplamBagkur.ToString("N0") + " TL";
}
