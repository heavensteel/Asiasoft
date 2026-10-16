using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public class Stok
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string StokKod { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string StokAdi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? StokGrubu { get; set; }

    [MaxLength(100)]
    public string? StokAlGrubu { get; set; }

    [MaxLength(20)]
    public string Birim { get; set; } = "ADET";

    [Column(TypeName = "decimal(18,4)")]
    public decimal Miktar { get; set; } = 0;      // Current stock

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinMiktar { get; set; } = 0;   // Minimum stock level

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaxMiktar { get; set; } = 0;   // Maximum stock level

    [Column(TypeName = "decimal(18,4)")]
    public decimal AlisFiyat { get; set; } = 0;   // Purchase price

    [Column(TypeName = "decimal(18,4)")]
    public decimal SatisFiyat { get; set; } = 0;  // Sales price

    [Column(TypeName = "decimal(18,2)")]
    public decimal KdvYuzde { get; set; } = 20;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<StokHareket> Hareketler { get; set; } = new List<StokHareket>();
}
