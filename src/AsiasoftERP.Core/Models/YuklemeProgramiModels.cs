using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

public class YuklemeProgramiDto
{
    public int Id { get; set; }
    public string ProgramNo { get; set; } = string.Empty;
    public DateTime Tarih { get; set; } = DateTime.Today;
    public string Baslik { get; set; } = string.Empty;
    public int SutunSayisi { get; set; } = 1;
    public int SatirSayisi { get; set; } = 1;
    public int ToplamPalet { get; set; }
    public string Hazirlayan { get; set; } = "Ali SARIGÜL";
    public string Onaylayan { get; set; } = "İHRACAT BÖLÜMÜ";
    public string ForkliftOperator { get; set; } = string.Empty;
    public string Durum { get; set; } = "Onaylandı";
    public string? Notlar { get; set; }
    public DateTime GuncellemeTarihi { get; set; } = DateTime.Now;

    public List<YuklemeSutunDto> Sutunlar { get; set; } = new();

    public int ToplamHucreSayisi => Math.Max(1, SatirSayisi) * Math.Max(1, SutunSayisi);
    public string GridDuzen => $"{Math.Max(1, SatirSayisi)}x{Math.Max(1, SutunSayisi)}";
}

public class YuklemeSutunDto
{
    public int Id { get; set; }
    public int ProgramId { get; set; }
    public int SutunNo { get; set; } = 1;
    public int SatirNo { get; set; } = 1;
    public int HucreIndex { get; set; } = 1;
    public string Baslik { get; set; } = string.Empty;
    public string UrunlerMetin { get; set; } = string.Empty;
    public string TalimatlarMetin { get; set; } = string.Empty;
    public string PartilerMetin { get; set; } = string.Empty;
    public string TarihlerMetin { get; set; } = string.Empty;
    public int ToplamPalet { get; set; }
}

/// <summary>
/// Yükleme operasyonlarını yürüten fabrika/depo forklift operatörü
/// </summary>
public class ForkliftOperatorDto
{
    public int Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string EhliyetNo { get; set; } = string.Empty;
    public string ForkliftKodu { get; set; } = string.Empty;
    public string Vardiya { get; set; } = "1. Vardiya (Gündüz)";
    public string Durum { get; set; } = "Aktif";
    public string Aciklama { get; set; } = string.Empty;
    public DateTime KayitTarihi { get; set; } = DateTime.Now;

    public string DisplayText => string.IsNullOrWhiteSpace(ForkliftKodu)
        ? AdSoyad
        : $"{AdSoyad} ({ForkliftKodu})";
}
