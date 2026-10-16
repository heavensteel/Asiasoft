using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public enum MakbuzTip
{
    OdemeMakbuzu = 1,      // Payment receipt (outgoing)
    TahsilatMakbuzu = 2,   // Collection receipt (incoming)
    MustahsilMakbuzu = 3,  // Mustahsil receipt
    SmmMakbuzu = 4         // SMM receipt
}

/// <summary>
/// Ödeme/Tahsilat Makbuzu - Payment/Collection Receipt
/// </summary>
public class OdemeMakbuz
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string MakbuzNo { get; set; } = string.Empty;

    public MakbuzTip MakbuzTipi { get; set; } = MakbuzTip.OdemeMakbuzu;

    public DateTime MakbuzTarihi { get; set; } = DateTime.Today;

    public int CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizTutar { get; set; } = 0;

    [MaxLength(50)]
    public string? OdemeYontemi { get; set; }  // Nakit, Havale, Çek, Kredi Kartı

    [MaxLength(100)]
    public string? BankaAdi { get; set; }

    [MaxLength(50)]
    public string? HesapNo { get; set; }

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public bool IsKapali { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
}
