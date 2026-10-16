using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Bahçe - Garden/Orchard
/// </summary>
public class Bahce
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string BahceKod { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string BahceAdi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? UreticiAdi { get; set; }

    [MaxLength(100)]
    public string? Konum { get; set; }    // Location

    [MaxLength(50)]
    public string? KoyMahalle { get; set; }

    [MaxLength(50)]
    public string? Ilce { get; set; }

    [MaxLength(50)]
    public string? Il { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Alan { get; set; } = 0;     // Area (dekar/hectare)

    [MaxLength(100)]
    public string? UrunTipi { get; set; }      // Citrus, Grape, etc.

    [MaxLength(100)]
    public string? Cesit { get; set; }         // Variety

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    public ICollection<BahceSozlesme> Sozlesmeler { get; set; } = new List<BahceSozlesme>();
    public ICollection<BahceKesimFisi> KesimFisleri { get; set; } = new List<BahceKesimFisi>();
}

/// <summary>
/// Bahçe Sözleşmesi - Garden Contract
/// </summary>
public class BahceSozlesme
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string SozlesmeNo { get; set; } = string.Empty;

    public int BahceId { get; set; }
    public Bahce? Bahce { get; set; }

    public DateTime SozlesmeTarihi { get; set; } = DateTime.Today;
    public DateTime? KesimBaslangic { get; set; }
    public DateTime? KesimBitis { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } = 0;   // Price per kg

    [Column(TypeName = "decimal(18,4)")]
    public decimal TahminiTon { get; set; } = 0;   // Estimated tonnage

    [Column(TypeName = "decimal(18,2)")]
    public decimal Avans { get; set; } = 0;         // Advance payment

    [MaxLength(1000)]
    public string? Kosullar { get; set; }

    public bool IsAktif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Bahçe Kesim Fişi - Harvesting Record
/// </summary>
public class BahceKesimFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public int BahceId { get; set; }
    public Bahce? Bahce { get; set; }

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    [MaxLength(50)]
    public string? UrunTipi { get; set; }

    [MaxLength(50)]
    public string? Cesit { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BrutKg { get; set; } = 0;    // Gross kg

    [Column(TypeName = "decimal(18,4)")]
    public decimal Dara { get; set; } = 0;      // Tare

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetKg { get; set; } = 0;     // Net kg

    [Column(TypeName = "decimal(18,2)")]
    public decimal KasaSayisi { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [MaxLength(100)]
    public string? KalibreNo { get; set; }      // Caliber

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public bool IsOnaylandi { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Kantar Fişi - Weighbridge Record
/// </summary>
public class KantarFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    [MaxLength(20)]
    public string? AracPlaka { get; set; }

    public int? AracId { get; set; }
    public Arac? Arac { get; set; }

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal GirisTartim { get; set; } = 0;   // First weighing (gross)

    [Column(TypeName = "decimal(18,4)")]
    public decimal CikisTartim { get; set; } = 0;   // Second weighing (tare)

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetKg { get; set; } = 0;         // Net weight

    [MaxLength(100)]
    public string? MalCinsi { get; set; }            // Product type

    public bool IsOnaylandi { get; set; } = false;

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Mal Kabul Fişi - Goods Acceptance Record
/// </summary>
public class MalKabulFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    public int? BahceId { get; set; }
    public Bahce? Bahce { get; set; }

    public int? KantarFisiId { get; set; }
    public KantarFisi? KantarFisi { get; set; }

    [MaxLength(50)]
    public string? UrunTipi { get; set; }

    [MaxLength(50)]
    public string? Cesit { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BrutKg { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetKg { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal KasaSayisi { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    public bool IsOnaylandi { get; set; } = false;
    public DateTime? OnayTarihi { get; set; }
    public int? OnaylayanUserId { get; set; }

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}
