using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Comprehensive DTO representing an ERP user from table 'user' and company mapping from 'sirketkullanicilari'
/// </summary>
public class ErpUserDto
{
    public string Kodu { get; set; } = string.Empty;
    public string Adi { get; set; } = string.Empty;
    public string Sifre { get; set; } = string.Empty;
    public string Yetki { get; set; } = "S"; // 'A' = Admin, 'S' = Standard, 'N' = Terminal/Restricted
    public string Durum { get; set; } = "EVET"; // 'EVET' = Aktif, 'HAYIR' = Pasif
    public string OzelKodu { get; set; } = string.Empty;

    // Company assignments summary and raw codes
    public string Sirketler { get; set; } = string.Empty;
    public string SirketKodlariRaw { get; set; } = string.Empty;
    public List<string> SirketKodlari { get; set; } = new();

    // Module permission flags
    public bool Cari { get; set; } = true;
    public bool Stok { get; set; } = true;
    public bool Alis { get; set; } = true;
    public bool Satis { get; set; } = true;
    public bool PerakendeIade { get; set; } = true;
    public bool Perakende { get; set; } = true;
    public bool Fis { get; set; } = true;
    public bool Ekstre { get; set; } = true;
    public bool BorcAlacak { get; set; } = true;
    public bool Kasa { get; set; } = true;
    public bool AlisIade { get; set; } = true;
    public bool SatisIade { get; set; } = true;
    public bool TahsilatGirisi { get; set; } = true;
    public bool OdemeGirisi { get; set; } = true;
    public bool MusteriCekGirisi { get; set; } = true;
    public bool MusteriSenetGirisi { get; set; } = true;
    public bool FirmaCekCikisi { get; set; } = true;
    public bool BorcDekontuGirisi { get; set; } = true;
    public bool AlacakDekontuGirisi { get; set; } = true;
    public bool BakiyeGorGirisi { get; set; } = true;
    public bool FiyatGorGiris { get; set; } = true;
    public bool BakiyeGoster { get; set; } = true;
    public bool GunSonuRaporu { get; set; } = true;
    public bool IsletmeDurumRaporu { get; set; } = true;
    public bool SatisOzeti { get; set; } = true;
    public bool SatisKarlilik { get; set; } = true;
    public bool SenetRaporu { get; set; } = true;
    public bool MusteriCekRaporu { get; set; } = true;
    public bool FirmaCekRaporu { get; set; } = true;
    public bool DepoBazindaStokRaporu { get; set; } = true;
    public bool StokGirisCikisRaporu { get; set; } = true;
    public bool AylikAnaliz { get; set; } = true;

    // Display helpers
    public bool IsAdmin => Yetki?.Trim().ToUpper() == "A";
    public bool IsActive => Durum?.Trim().ToUpper() == "EVET";

    public string RoleDisplay => Yetki?.Trim().ToUpper() switch
    {
        "A" => "👑 Yönetici",
        "N" => "💻 Terminal",
        _ => "👤 Standart"
    };

    public string StatusDisplay => IsActive ? "🟢 Aktif" : "🔴 Pasif";
}
