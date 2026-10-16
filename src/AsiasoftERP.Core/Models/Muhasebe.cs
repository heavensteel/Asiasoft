using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Yevmiye Fişi - Journal Entry
/// </summary>
public class YevmiyeFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    [MaxLength(100)]
    public string FisTipi { get; set; } = "YEVMİYE";  // YEVMİYE, MAHSUP, KARMA

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamBorc { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamAlacak { get; set; } = 0;

    public bool IsOnaylandi { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }

    public ICollection<YevmiyeKalemi> Kalemler { get; set; } = new List<YevmiyeKalemi>();
}

public class YevmiyeKalemi
{
    public int Id { get; set; }
    public int YevmiyeFisiId { get; set; }
    public YevmiyeFisi? YevmiyeFisi { get; set; }

    [MaxLength(20)]
    public string HesapKodu { get; set; } = string.Empty;

    [MaxLength(200)]
    public string HesapAdi { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Borc { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Alacak { get; set; } = 0;

    [MaxLength(500)]
    public string? Aciklama { get; set; }
    public int SiraNo { get; set; } = 1;
}

/// <summary>
/// Alacak/Borç Dekontu - Receivable/Payable receipt
/// </summary>
public enum DekontTip
{
    BorcDekontu = 1,
    AlacakDekontu = 2,
    VirmanDekontu = 3
}

public class Dekont
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string DekontNo { get; set; } = string.Empty;

    public DekontTip DekontTipi { get; set; } = DekontTip.AlacakDekontu;

    public DateTime DekontTarihi { get; set; } = DateTime.Today;

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizTutar { get; set; } = 0;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public bool IsKapali { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Banka Yatanlar Listesi - Bankaya Yatan Ödemeler
/// </summary>
public class BankayaYatan
{
    public int Id { get; set; }

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    public int? BankaHesapId { get; set; }
    public BankaHesap? BankaHesap { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
