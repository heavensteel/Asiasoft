using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public enum CekDurum
{
    Portfoy = 0,     // In portfolio
    BankayaVerildi = 1,
    TahsilEdildi = 2,
    Cirkolandi = 3,  // Endorsed
    Karsilıksız = 4, // Bounced
    Iptal = 5
}

public enum CekTip
{
    MusteriCek = 1,   // Customer check
    FirmaCek = 2      // Company check
}

public class Cek
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string CekNo { get; set; } = string.Empty;

    public CekTip CekTipi { get; set; } = CekTip.MusteriCek;

    public DateTime DuzenlemeTarihi { get; set; } = DateTime.Today;
    public DateTime VadeTarihi { get; set; } = DateTime.Today;

    public int? CariHesapId { get; set; }
    public CariHesap? CariHesap { get; set; }

    [MaxLength(100)]
    public string? Keside { get; set; }    // Drawer
    [MaxLength(100)]
    public string? Muhatap { get; set; }   // Drawee bank

    [MaxLength(5)]
    public string DovizTipi { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; } = 0;

    public CekDurum Durum { get; set; } = CekDurum.Portfoy;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
}
