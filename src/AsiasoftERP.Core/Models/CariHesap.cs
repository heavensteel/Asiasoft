using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Cari Hesap - Current Account (Customer/Supplier)
/// </summary>
public class CariHesap
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string CariKod { get; set; } = string.Empty;   // Account Code

    [Required, MaxLength(200)]
    public string CariAdi { get; set; } = string.Empty;   // Account Name

    [MaxLength(10)]
    public string CariTip { get; set; } = "M";  // M=Müşteri, T=Tedarikçi, MT=İkisi de

    [MaxLength(200)]
    public string? Adres { get; set; }
    [MaxLength(50)]
    public string? Sehir { get; set; }
    [MaxLength(50)]
    public string? Ulke { get; set; }
    [MaxLength(20)]
    public string? Telefon { get; set; }
    [MaxLength(20)]
    public string? Fax { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    [MaxLength(20)]
    public string? VergiNo { get; set; }       // Tax Number
    [MaxLength(100)]
    public string? VergiDairesi { get; set; }  // Tax Office

    [Column(TypeName = "decimal(18,2)")]
    public decimal KrediLimiti { get; set; } = 0;

    [MaxLength(5)]
    public string? DovizTipi { get; set; } = "TRY";  // Currency type

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bakiye { get; set; } = 0;  // Balance (calculated)

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizBakiye { get; set; } = 0;  // Foreign currency balance

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? TemsilciAdi { get; set; }  // Sales rep

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    // Navigation
    public ICollection<Fatura> Faturalar { get; set; } = new List<Fatura>();
    public ICollection<CariEkstrem> Ekstreler { get; set; } = new List<CariEkstrem>();
}
