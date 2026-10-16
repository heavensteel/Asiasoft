using System;

namespace AsiasoftERP.Core.Models;

public class FaturaStkDto
{
    public double SiraIdNo { get; set; }
    public double FisNo { get; set; }
    public string GibFaturaNo { get; set; } = string.Empty;
    public DateTime? FisTar { get; set; }
    public DateTime? VadeTar { get; set; }
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string DovizTuru { get; set; } = "TL";
    public double FisTut { get; set; }
    public double IskTut { get; set; }
    public double KdvTut { get; set; }
    public double GenTut { get; set; }
    public double DovizTutar { get; set; }
    public string Tip { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;

    public string BelgeNoDisplay => !string.IsNullOrWhiteSpace(GibFaturaNo) ? GibFaturaNo : FisNo.ToString("N0");
}

public class FaturaDetayStkDto
{
    public double Sira { get; set; }
    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public string Birimi { get; set; } = "KG";
    public double Miktar { get; set; }
    public double Fiyati { get; set; }
    public double Kdv { get; set; }
    public double KdvTut { get; set; }
    public double Tutar { get; set; }
    public string DovizTuru { get; set; } = "TL";
    public double DovizFiyati { get; set; }
    public double DovizTutar { get; set; }
}
