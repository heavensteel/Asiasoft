using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsiasoftERP.Core.Models;

public class FaturaDetay
{
    public int Id { get; set; }

    public int FaturaId { get; set; }
    public Fatura? Fatura { get; set; }

    public int? StokId { get; set; }
    public Stok? Stok { get; set; }

    [MaxLength(200)]
    public string UrunAdi { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? UrunKod { get; set; }

    [MaxLength(10)]
    public string Birim { get; set; } = "ADET";

    [Column(TypeName = "decimal(18,4)")]
    public decimal Miktar { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal IskontoYuzde { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal IskontoTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal KdvYuzde { get; set; } = 20;

    [Column(TypeName = "decimal(18,2)")]
    public decimal KdvTutar { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; } = 0;

    public int SiraNo { get; set; } = 1;
}
