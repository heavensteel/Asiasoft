using System;

namespace AsiasoftERP.Core.Models;

public class StokKartDto
{
    public double Sira { get; set; }
    public string Kodu { get; set; } = string.Empty;
    public string Adi { get; set; } = string.Empty;
    public string GrupKoduAdi { get; set; } = string.Empty;
    public string Birim { get; set; } = "KG";
    public double Mevcut { get; set; }
    public double AlisFiyat { get; set; }
    public double SFiyat1 { get; set; }
    public double Kdv { get; set; }
}
