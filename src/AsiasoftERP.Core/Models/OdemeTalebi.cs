using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public enum OdemeDurum
{
    Bekliyor = 0,
    Onaylandi = 1,
    Odendi = 2,
    Reddedildi = 3,
    Iptal = 4
}

/// <summary>
/// Ödeme Talebi - Payment Request
/// </summary>
public class OdemeTalebi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string TalepNo { get; set; } = string.Empty;

    public DateTime TalepTarihi { get; set; } = DateTime.Today;
    public DateTime? VadeTarihi { get; set; }

    public int CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OdenenTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bakiye { get; set; } = 0;  // Remaining

    public OdemeDurum Durum { get; set; } = OdemeDurum.Bekliyor;

    [MaxLength(10)]
    public string? IsyeriKod { get; set; }  // Department/branch

    [MaxLength(50)]
    public string? OdemeYontemi { get; set; }  // Payment method

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
