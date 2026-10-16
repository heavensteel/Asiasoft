using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Araç - Vehicle/Truck
/// </summary>
public class Arac
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Plaka { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? AracTipi { get; set; }  // TIR, Kamyon, Kamyonet, etc.

    [MaxLength(100)]
    public string? SuruculAdi { get; set; }

    [MaxLength(50)]
    public string? SuruculTelefon { get; set; }

    [MaxLength(100)]
    public string? NakliyeFirma { get; set; }

    [MaxLength(20)]
    public string? DorseNo { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<SeferFisi> Seferler { get; set; } = new List<SeferFisi>();
}

public enum SeferDurum
{
    Planli = 0,
    Yukleniyor = 1,
    Yolda = 2,
    Teslim = 3,
    Tamamlandi = 4,
    Iptal = 5
}

/// <summary>
/// Sefer Fişi - Trip/Journey record
/// </summary>
public class SeferFisi
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FisNo { get; set; } = string.Empty;

    public DateTime FisTarihi { get; set; } = DateTime.Today;
    public DateTime? CikisTarihi { get; set; }
    public DateTime? VarisTarihi { get; set; }

    public int AracId { get; set; }
    public Arac? Arac { get; set; }

    [MaxLength(100)]
    public string? CikiNokta { get; set; }

    [MaxLength(100)]
    public string? VarisNokta { get; set; }

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }   // Nakliyeci

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Navlon { get; set; } = 0;    // Freight cost

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Mesafe { get; set; } = 0;    // Distance km

    [Column(TypeName = "decimal(18,2)")]
    public decimal MazotMiktar { get; set; } = 0;

    public SeferDurum Durum { get; set; } = SeferDurum.Planli;

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Yükleme Emri - Loading Order
/// </summary>
public class YuklemeEmri
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string EmriNo { get; set; } = string.Empty;

    public DateTime EmriTarihi { get; set; } = DateTime.Today;
    public DateTime? YuklemeTarihi { get; set; }

    public int? AracId { get; set; }
    public Arac? Arac { get; set; }

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(100)]
    public string? VarisYeri { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamKg { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetKg { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaletToplam { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal KasaToplam { get; set; } = 0;

    public bool IsOnaylandi { get; set; } = false;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}

/// <summary>
/// Aylık TIR Durum Raporu - Monthly TIR Status
/// (Links to SeferFisi with aggregate data)
/// </summary>
public class TirMaliyeti
{
    public int Id { get; set; }

    public int SeferFisiId { get; set; }
    public SeferFisi? SeferFisi { get; set; }

    [MaxLength(50)]
    public string GiderTipi { get; set; } = string.Empty;  // Mazot, Geçiş, Bakım, etc.

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [MaxLength(200)]
    public string? Aciklama { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Today;
}
