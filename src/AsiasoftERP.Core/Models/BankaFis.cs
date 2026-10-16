using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public enum BankaFisTip
{
    Giris = 1,      // Incoming
    Cikis = 2,      // Outgoing
    Virman = 3,     // Bank transfer
    DovizAlim = 4,  // Foreign currency purchase
    DovizSatim = 5  // Foreign currency sale
}

public class BankaFis
{
    public int Id { get; set; }

    public int BankaHesapId { get; set; }
    public BankaHesap? BankaHesap { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public BankaFisTip FisTip { get; set; } = BankaFisTip.Giris;

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,4)")]
    public decimal DovizKuru { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DovizTutar { get; set; } = 0;

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(100)]
    public string? GonderenAlici { get; set; }  // Sender/Receiver

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public bool IsOnaylandi { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}
