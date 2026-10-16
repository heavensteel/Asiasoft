using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// DTO representing a single Payment Request row in 'Ödeme Talep İşlemleri'
/// Mapped from table 'erp.bankaodemedetay'
/// </summary>
public class OdemeTalepDto
{
    public double IdNo { get; set; }
    public int Sira { get; set; }
    public string SirKod { get; set; } = "1";
    
    // Column 2: İş Yeri / Şube
    public string YetkiKodu { get; set; } = string.Empty;
    
    // Column 3: Grup Şirket
    public string GrupSirket { get; set; } = string.Empty;
    
    // Column 4: Gönderen Hesap
    public string GonderenHesap { get; set; } = string.Empty;
    
    // Column 5: Türü (AVANS, HAVALE, BANKADA NAKİT, ŞUBEDEN NAKİT, EFT...)
    public string Turu { get; set; } = string.Empty;
    
    // Column 6: Fiş No
    public double FisNo { get; set; }
    
    // Column 7: Tarih
    public DateTime? FisTar { get; set; }
    
    // Column 8: İşlem Tarihi
    public DateTime? IslemTarihi { get; set; }
    
    // Column 9: Kasa Adı
    public string KasaAdi { get; set; } = string.Empty;
    
    // Column 10: Cari Hesap Ünvanı
    public double FirKod { get; set; }
    public string FirAdi { get; set; } = string.Empty;
    
    // Column 11: Belge No / Fatura No
    public string BelgeNo { get; set; } = string.Empty;
    public string FaturaNo { get; set; } = string.Empty;
    public string BelgeVeyaFaturaNo => !string.IsNullOrWhiteSpace(FaturaNo) ? FaturaNo : BelgeNo;
    
    // Column 12: Bağkur Kesinti ? (HAYIR / EVET)
    public string BagkurVarmi { get; set; } = "HAYIR";
    
    // Column 13: Talep Tutarı
    public decimal TalepTutar { get; set; }
    
    // Column 14: Bağkur Kesinti Oranı%
    public decimal BagkurOrani { get; set; }
    
    // Column 15: Bağkur Kesinti Tutarı
    public decimal BagkurTutar { get; set; }
    
    // Calculated Net Amount
    public decimal NetTutar => TalepTutar - BagkurTutar;
    
    // Extra details
    public string Durum { get; set; } = string.Empty; // Aktarildi, Onaylandı, RET, Devam Ediyor
    public string Aciklama { get; set; } = string.Empty;
    public string BankaIban { get; set; } = string.Empty;
    public string Kullanici { get; set; } = string.Empty;
    public string OnayKullanici { get; set; } = string.Empty;
    public DateTime? OnayTarihi { get; set; }
    public string DovizTuru { get; set; } = "TL";
    public decimal DovizTutar { get; set; }
}

/// <summary>
/// Grouped comparative summary for Tab 5 ('Ödeme Talep Karşılaştırma')
/// </summary>
public class OdemeTalepKarsilastirmaDto
{
    public string Sube { get; set; } = string.Empty;
    public string Turu { get; set; } = string.Empty;
    public int TalepAdedi { get; set; }
    public decimal ToplamTalepTutari { get; set; }
    public decimal OnaylananTutar { get; set; }
    public decimal RetEdilenTutar { get; set; }
    public decimal DevamEdenTutar { get; set; }
    public decimal KarsilamaOrani => ToplamTalepTutari > 0 
        ? Math.Round((OnaylananTutar / ToplamTalepTutari) * 100m, 2) 
        : 0;
}

/// <summary>
/// Summary totals for 'Ödeme Talep İşlemleri'
/// </summary>
public class OdemeTalepSummary
{
    public int ToplamKayit { get; set; }
    public decimal ToplamTalepTutari { get; set; }
    public decimal ToplamBagkurTutari { get; set; }
    public decimal ToplamNetTutar => ToplamTalepTutari - ToplamBagkurTutari;
    public decimal OnaylananToplam { get; set; }
    public decimal RetToplam { get; set; }
    public decimal DevamEdenToplam { get; set; }
}
