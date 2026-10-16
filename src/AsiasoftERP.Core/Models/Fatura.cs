using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public enum FaturaTip
{
    AlisFaturasi = 1,
    SatisFaturasi = 2,
    IhracatFaturasi = 3,
    AlisSevkiyati = 4,
    SatisSevkiyati = 5,
    Konsinyasyon = 6
}

public class Fatura
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;    // Document number

    public FaturaTip FaturaTipi { get; set; } = FaturaTip.SatisFaturasi;

    public DateTime FaturaTarihi { get; set; } = DateTime.Today;
    public DateTime? VadeTarihi { get; set; }

    public int CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,4)")]
    public decimal DovizKuru { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal FisTutar { get; set; } = 0;      // Gross total (FISTUT)

    [Column(TypeName = "decimal(18,2)")]
    public decimal IskontaTutar { get; set; } = 0;  // Discount amount (ISKTUT)

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetFisTutar { get; set; } = 0;   // Net total (NETFISTUT)

    [Column(TypeName = "decimal(18,2)")]
    public decimal KdvTutar { get; set; } = 0;       // VAT amount (KDVTUT)

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtvToplami { get; set; } = 0;     // Special consumption tax (OTVTOPLAMI)

    [Column(TypeName = "decimal(18,2)")]
    public decimal TevkifatTutar { get; set; } = 0;  // Withholding tax (TEVKIFATTURARI)

    [Column(TypeName = "decimal(18,2)")]
    public decimal GenelToplam { get; set; } = 0;    // Grand total (GENTUT)

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizTutar { get; set; } = 0;     // Foreign currency total (DOVIZTUTAR)

    [MaxLength(10)]
    public string? OdemeSekli { get; set; }  // Payment method

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public bool IsOdendi { get; set; } = false;
    public bool IsKapali { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }

    // Navigation
    public ICollection<FaturaDetay> Detaylar { get; set; } = new List<FaturaDetay>();
}
