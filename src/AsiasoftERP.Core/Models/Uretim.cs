using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Paketleme Fişi - Packaging Record
/// </summary>
public class PaketlemeFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    [MaxLength(50)]
    public string? UrunTipi { get; set; }

    [MaxLength(50)]
    public string? Cesit { get; set; }

    [MaxLength(50)]
    public string? Kalite { get; set; }   // Quality class

    [MaxLength(50)]
    public string? OdaNo { get; set; }    // Room/section number

    [Column(TypeName = "decimal(18,4)")]
    public decimal GirisMiktar { get; set; } = 0;   // Input quantity

    [Column(TypeName = "decimal(18,4)")]
    public decimal CikisMiktar { get; set; } = 0;   // Output quantity

    [Column(TypeName = "decimal(18,4)")]
    public decimal Fire { get; set; } = 0;           // Wastage

    [Column(TypeName = "decimal(18,2)")]
    public decimal KolliSayisi { get; set; } = 0;   // Number of packages/colli

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaletSayisi { get; set; } = 0;

    [MaxLength(100)]
    public string? EtiketKod { get; set; }   // Label/mark code

    public bool IsOnaylandi { get; set; } = false;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Hazır Mal Fişi - Ready/Finished Goods Record
/// </summary>
public class HazirMalFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    public int? StokId { get; set; }
    public Stok? Stok { get; set; }

    [MaxLength(50)]
    public string? OdaNo { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Miktar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetKg { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal KolliSayisi { get; set; } = 0;

    [MaxLength(100)]
    public string? SertifikaNo { get; set; }

    [MaxLength(100)]
    public string? KunNo { get; set; }   // Künye/label number

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Puantaj Fişi - Timesheet/Payroll Record
/// </summary>
public class PuantajFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public DateTime FisTarihi { get; set; } = DateTime.Today;

    [Required, MaxLength(100)]
    public string IsciAdi { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TcNo { get; set; }

    [MaxLength(100)]
    public string? Bolum { get; set; }     // Department/section

    [MaxLength(50)]
    public string? Gorev { get; set; }     // Job title

    [Column(TypeName = "decimal(18,2)")]
    public decimal Saat { get; set; } = 0;        // Hours worked

    [Column(TypeName = "decimal(18,2)")]
    public decimal FazlaMesai { get; set; } = 0;   // Overtime hours

    [Column(TypeName = "decimal(18,4)")]
    public decimal SaatUcreti { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamUcret { get; set; } = 0;

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Beyanname - Customs Declaration
/// </summary>
public class Beyanname
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string BeyanNo { get; set; } = string.Empty;

    public DateTime BeyanTarihi { get; set; } = DateTime.Today;

    [MaxLength(10)]
    public string? BeyanTipi { get; set; }  // İhracat, İthalat

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "USD";

    [Column(TypeName = "decimal(18,2)")]
    public decimal BeyanDegeri { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamKg { get; set; } = 0;

    [MaxLength(100)]
    public string? GumrukMudurlugu { get; set; }

    [MaxLength(200)]
    public string? UlkePazar { get; set; }   // Destination country/market

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public bool IsKapatildi { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }

    public ICollection<BeyannameMasraf> Masraflar { get; set; } = new List<BeyannameMasraf>();
}

public class BeyannameMasraf
{
    public int Id { get; set; }
    public int BeyannameId { get; set; }
    public Beyanname? Beyanname { get; set; }

    [MaxLength(100)]
    public string MasrafTipi { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [MaxLength(200)]
    public string? Aciklama { get; set; }
}

/// <summary>
/// Üretim Planlama - Production Planning
/// </summary>
public class UretimPlan
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string PlanNo { get; set; } = string.Empty;

    public DateTime PlanTarihi { get; set; } = DateTime.Today;
    public DateTime? UretimTarihi { get; set; }

    [MaxLength(100)]
    public string? UrunTipi { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PlanlananMiktar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal GerceklesanMiktar { get; set; } = 0;

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    public bool IsTamamlandi { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// User Settings (ayarlar.ini equivalent stored in DB)
/// </summary>
public class UserSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(20)]
    public string SirketKodu { get; set; } = "1";

    [MaxLength(200)]
    public string SirketAdi { get; set; } = "TEK ASYA A.Ş.";

    // Window layout preferences (JSON)
    public string? WindowLayoutJson { get; set; }

    // Default filters
    public string? DefaultFiltersJson { get; set; }

    // Theme
    [MaxLength(50)]
    public string Theme { get; set; } = "Default";

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
