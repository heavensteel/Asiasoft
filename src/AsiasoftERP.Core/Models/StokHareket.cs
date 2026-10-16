using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public enum StokHareketTip
{
    Giris = 1,      // Stock in
    Cikis = 2,      // Stock out
    Devir = 3,      // Transfer
    Sayim = 4       // Inventory count
}

public class StokHareket
{
    public int Id { get; set; }

    public int StokId { get; set; }
    public Stok? Stok { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public StokHareketTip HareketTip { get; set; } = StokHareketTip.Giris;

    public DateTime HareketTarihi { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Miktar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,4)")]
    public decimal DovizKuru { get; set; } = 1;

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    public int? FaturaId { get; set; }

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}
