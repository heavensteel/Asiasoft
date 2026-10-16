using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public enum IrsaliyeTip
{
    AlisIrsaliyesi = 1,
    SatisIrsaliyesi = 2,
    DepoTransfer = 3
}

/// <summary>
/// İrsaliye - Delivery/Dispatch Note
/// </summary>
public class Irsaliye
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string IrsaliyeNo { get; set; } = string.Empty;

    public IrsaliyeTip IrsaliyeTipi { get; set; } = IrsaliyeTip.SatisIrsaliyesi;

    public DateTime IrsaliyeTarihi { get; set; } = DateTime.Today;

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamKg { get; set; } = 0;

    public int? FaturaId { get; set; }   // Linked invoice

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public bool IsKapali { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }

    public ICollection<IrsaliyeDetay> Detaylar { get; set; } = new List<IrsaliyeDetay>();
}

public class IrsaliyeDetay
{
    public int Id { get; set; }
    public int IrsaliyeId { get; set; }
    public Irsaliye? Irsaliye { get; set; }
    public int? StokId { get; set; }
    public Stok? Stok { get; set; }

    [MaxLength(200)]
    public string UrunAdi { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Birim { get; set; } = "ADET";

    [Column(TypeName = "decimal(18,4)")]
    public decimal Miktar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;
    public int SiraNo { get; set; } = 1;
}

public enum SiparisTip
{
    AlinanSiparis = 1,
    VerilenSiparis = 2,
    OnSiparis = 3
}

/// <summary>
/// Sipariş Fişi - Order
/// </summary>
public class SiparisFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string SiparisNo { get; set; } = string.Empty;

    public SiparisTip SiparisTipi { get; set; } = SiparisTip.AlinanSiparis;

    public DateTime SiparisTarihi { get; set; } = DateTime.Today;
    public DateTime? TeslimTarihi { get; set; }

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizTutar { get; set; } = 0;

    public bool IsKapalildi { get; set; } = false;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }

    public ICollection<SiparisDetay> Detaylar { get; set; } = new List<SiparisDetay>();
}

public class SiparisDetay
{
    public int Id { get; set; }
    public int SiparisFisiId { get; set; }
    public SiparisFisi? SiparisFisi { get; set; }
    public int? StokId { get; set; }
    public Stok? Stok { get; set; }

    [MaxLength(200)]
    public string UrunAdi { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Birim { get; set; } = "ADET";

    [Column(TypeName = "decimal(18,4)")]
    public decimal Miktar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal TeslimMiktar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;
    public int SiraNo { get; set; } = 1;
}

/// <summary>
/// Sözleşme - Contract
/// </summary>
public enum SozlesmeTip
{
    AlisSozlesmesi = 1,
    SatisSozlesmesi = 2,
    BahceSozlesmesi = 3,
    NakliyeSozlesmesi = 4
}

public class Sozlesme
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string SozlesmeNo { get; set; } = string.Empty;

    public SozlesmeTip SozlesmeTipi { get; set; } = SozlesmeTip.SatisSozlesmesi;

    public DateTime SozlesmeTarihi { get; set; } = DateTime.Today;
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal SozlesmeTutari { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Miktar { get; set; } = 0;

    [MaxLength(10)]
    public string Birim { get; set; } = "KG";

    [MaxLength(1000)]
    public string? Kosullar { get; set; }

    public bool IsAktif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}
