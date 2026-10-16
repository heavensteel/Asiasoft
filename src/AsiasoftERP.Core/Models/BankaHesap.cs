using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public class BankaHesap
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string HesapKod { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string HesapAdi { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string BankaAdi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SubeAdi { get; set; }

    [MaxLength(26)]
    public string? Iban { get; set; }

    [MaxLength(30)]
    public string? HesapNumarasi { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bakiye { get; set; } = 0;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<BankaFis> Fisler { get; set; } = new List<BankaFis>();
}
