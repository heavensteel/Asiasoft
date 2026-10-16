using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// MacroSoft Company representation from table 'sirket'
/// </summary>
public class CompanyDto
{
    public int SirKod { get; set; }
    public string Adi { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string VergiNo { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Para { get; set; } = "TL";

    public override string ToString() => $"{SirKod} - {Adi}";
}

/// <summary>
/// MacroSoft User representation from table 'sirketkullanicilari'
/// </summary>
public class CompanyUserDto
{
    public double Sirano { get; set; }
    public string SirKod { get; set; } = "1";
    public string SirketAdi { get; set; } = string.Empty;
    public double KullaniciKodu { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public string OzelKodu { get; set; } = string.Empty;
    public string Yetki { get; set; } = "S";
    public bool IsAdmin { get; set; } = false;

    public override string ToString() => $"{(int)KullaniciKodu} - {KullaniciAdi}";
}

/// <summary>
/// Representation of Cari item for the main Cari list from table 'firma'
/// </summary>
public class CariListItemDto
{
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string Unvan { get; set; } = string.Empty;
    public string VergiDairesi { get; set; } = string.Empty;
    public string VergiNo { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye => Borc - Alacak;
    public string AFirKod { get; set; } = string.Empty;
}
