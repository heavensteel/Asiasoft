using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Cari Hesap Ekstresi - Current account statement entry
/// </summary>
public class CariEkstrem
{
    public int Id { get; set; }

    public int CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Today;

    [MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    [MaxLength(100)]
    public string IslemTipi { get; set; } = string.Empty;  // Fatura, Ödeme, Tahsilat, etc.

    [MaxLength(500)]
    public string Aciklama { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Borc { get; set; } = 0;    // Debit

    [Column(TypeName = "decimal(18,2)")]
    public decimal Alacak { get; set; } = 0;  // Credit

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bakiye { get; set; } = 0;  // Running balance

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizBorc { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizAlacak { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizBakiye { get; set; } = 0;

    public int? ReferansId { get; set; }  // Related record Id
    public string? ReferansTip { get; set; }  // "Fatura", "Odeme", etc.

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
