using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Master Puantaj Fişi Modeli (yevmiyefis tablosu karşılığı)
/// </summary>
public class PuantajFisDto
{
    public string SirKod { get; set; } = "1";
    public int FisNo { get; set; }
    public int FatNo { get; set; }
    public string? Uid { get; set; }
    public double? TgKodu { get; set; }
    public string? TgAdi { get; set; } = string.Empty; // İş Yeri / Şube (örn: MERKEZ AMELE, GRES.)
    public double? SaticiKodu { get; set; }
    public string? SaticiAdi { get; set; } = string.Empty;
    public double? CFirKod { get; set; }
    public string? CFirmaAdi { get; set; } = string.Empty;
    public string? BolgeKodu { get; set; }
    public string? BolgeAdi { get; set; } = string.Empty; // Servis Bölgesi (örn: BALLIÖZ, SEKI)
    public string? Ay { get; set; } = string.Empty; // EYLÜL, EKİM vs.
    public int Yil { get; set; } = DateTime.Today.Year;
    public DateTime? BTarih { get; set; }
    public DateTime? STarih { get; set; }
    
    // Sayısal ve Parasal Alanlar
    public decimal YevmiyeToplam { get; set; } // YTOPLAM (Toplam yevmiye günü)
    public decimal MesaiToplam { get; set; }   // MTOPLAM (Toplam mesai saati)
    public decimal YevmiyeTutari { get; set; } // YTUTAR (Toplam yevmiye tutarı)
    public decimal MesaiTutari { get; set; }   // MESAITUTAR (Toplam mesai tutarı)
    public decimal EkKazanc { get; set; }      // EKKAZANC (Prim, ikramiye vb.)
    public decimal ServisSayisi { get; set; }  // SERVIS_SAYISI (Servis tur/araç sayısı)
    public decimal ServisFiyati { get; set; }  // SERVIS_FIYAT (Servis birim ücreti)
    public decimal ServisTutari { get; set; }  // SERVIS_TUTARI (Servis toplam tutarı)
    public decimal KalanServisTutari { get; set; } // KALAN
    public decimal ToplamHakedis { get; set; } // YEVMIYE_TUTARI (İşçilik net toplam hakediş)
    public decimal Avans { get; set; }         // AVANS
    public decimal ServisAvansi { get; set; }  // AVANS2
    public string? Aciklama { get; set; }
    public string? KKullanici { get; set; }
    public string? DKullanici { get; set; }
}

/// <summary>
/// Detail Puantaj / Yevmiye İşçi Çetelesi Modeli (yevmiyehrk tablosu karşılığı)
/// </summary>
public class PuantajDetayDto
{
    public string SirKod { get; set; } = "1";
    public int FisNo { get; set; }
    public int Sira { get; set; } // SIRA (Tablo PK veya sıralama numarası)
    public int FatNo { get; set; }
    public double? FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty; // Amele / İşçi Adı Soyadı
    public string? Ay { get; set; }
    public int Yil { get; set; }

    // Günlük Yevmiyeler (Y1..Y31) - 1: Tam Gün, 0.5: Yarım Gün, null/0: Gelmedi
    public double? Y1 { get; set; }
    public double? Y2 { get; set; }
    public double? Y3 { get; set; }
    public double? Y4 { get; set; }
    public double? Y5 { get; set; }
    public double? Y6 { get; set; }
    public double? Y7 { get; set; }
    public double? Y8 { get; set; }
    public double? Y9 { get; set; }
    public double? Y10 { get; set; }
    public double? Y11 { get; set; }
    public double? Y12 { get; set; }
    public double? Y13 { get; set; }
    public double? Y14 { get; set; }
    public double? Y15 { get; set; }
    public double? Y16 { get; set; }
    public double? Y17 { get; set; }
    public double? Y18 { get; set; }
    public double? Y19 { get; set; }
    public double? Y20 { get; set; }
    public double? Y21 { get; set; }
    public double? Y22 { get; set; }
    public double? Y23 { get; set; }
    public double? Y24 { get; set; }
    public double? Y25 { get; set; }
    public double? Y26 { get; set; }
    public double? Y27 { get; set; }
    public double? Y28 { get; set; }
    public double? Y29 { get; set; }
    public double? Y30 { get; set; }
    public double? Y31 { get; set; }

    // Günlük Mesai Saatleri (M1..M31)
    public double? M1 { get; set; }
    public double? M2 { get; set; }
    public double? M3 { get; set; }
    public double? M4 { get; set; }
    public double? M5 { get; set; }
    public double? M6 { get; set; }
    public double? M7 { get; set; }
    public double? M8 { get; set; }
    public double? M9 { get; set; }
    public double? M10 { get; set; }
    public double? M11 { get; set; }
    public double? M12 { get; set; }
    public double? M13 { get; set; }
    public double? M14 { get; set; }
    public double? M15 { get; set; }
    public double? M16 { get; set; }
    public double? M17 { get; set; }
    public double? M18 { get; set; }
    public double? M19 { get; set; }
    public double? M20 { get; set; }
    public double? M21 { get; set; }
    public double? M22 { get; set; }
    public double? M23 { get; set; }
    public double? M24 { get; set; }
    public double? M25 { get; set; }
    public double? M26 { get; set; }
    public double? M27 { get; set; }
    public double? M28 { get; set; }
    public double? M29 { get; set; }
    public double? M30 { get; set; }
    public double? M31 { get; set; }

    // Toplamlar ve Ücretler
    public decimal MesaiToplam { get; set; }  // MTOPLAM
    public decimal MesaiUcreti { get; set; }  // MFIYAT (Saatlik mesai ücreti)
    public decimal MesaiTutari { get; set; }  // MTUTAR (MesaiToplam * MesaiUcreti)
    public decimal ToplamGun { get; set; }    // TOPLAM (Y1..Y31 toplam gün)
    public decimal YevmiyeUcreti { get; set; } // FIYAT (Günlük yevmiye ücreti)
    public decimal YevmiyeTutari { get; set; } // TUTAR (ToplamGun * YevmiyeUcreti)
    public decimal DigerUcreti { get; set; }  // SERVIS_FIYAT (Günlük servis payı)
    public decimal DigerTutar { get; set; }   // SERVIS_TUTARI (ToplamGun * DigerUcreti)
    public decimal EkKazanc { get; set; }     // EKKAZANC
    public decimal Avans { get; set; }        // AVANS
    public decimal ToplamHakedis { get; set; } // (YevmiyeTutari + MesaiTutari + DigerTutar + EkKazanc) - Avans
    public bool YevmiyeciMi { get; set; } = true; // YEVMIYECIMI

    public double? GetYevmiyeDay(int day) => day switch
    {
        1 => Y1, 2 => Y2, 3 => Y3, 4 => Y4, 5 => Y5, 6 => Y6, 7 => Y7, 8 => Y8, 9 => Y9, 10 => Y10,
        11 => Y11, 12 => Y12, 13 => Y13, 14 => Y14, 15 => Y15, 16 => Y16, 17 => Y17, 18 => Y18, 19 => Y19, 20 => Y20,
        21 => Y21, 22 => Y22, 23 => Y23, 24 => Y24, 25 => Y25, 26 => Y26, 27 => Y27, 28 => Y28, 29 => Y29, 30 => Y30,
        31 => Y31, _ => null
    };

    public void SetYevmiyeDay(int day, double? value)
    {
        switch (day)
        {
            case 1: Y1 = value; break; case 2: Y2 = value; break; case 3: Y3 = value; break; case 4: Y4 = value; break;
            case 5: Y5 = value; break; case 6: Y6 = value; break; case 7: Y7 = value; break; case 8: Y8 = value; break;
            case 9: Y9 = value; break; case 10: Y10 = value; break; case 11: Y11 = value; break; case 12: Y12 = value; break;
            case 13: Y13 = value; break; case 14: Y14 = value; break; case 15: Y15 = value; break; case 16: Y16 = value; break;
            case 17: Y17 = value; break; case 18: Y18 = value; break; case 19: Y19 = value; break; case 20: Y20 = value; break;
            case 21: Y21 = value; break; case 22: Y22 = value; break; case 23: Y23 = value; break; case 24: Y24 = value; break;
            case 25: Y25 = value; break; case 26: Y26 = value; break; case 27: Y27 = value; break; case 28: Y28 = value; break;
            case 29: Y29 = value; break; case 30: Y30 = value; break; case 31: Y31 = value; break;
        }
    }

    public double? GetMesaiDay(int day) => day switch
    {
        1 => M1, 2 => M2, 3 => M3, 4 => M4, 5 => M5, 6 => M6, 7 => M7, 8 => M8, 9 => M9, 10 => M10,
        11 => M11, 12 => M12, 13 => M13, 14 => M14, 15 => M15, 16 => M16, 17 => M17, 18 => M18, 19 => M19, 20 => M20,
        21 => M21, 22 => M22, 23 => M23, 24 => M24, 25 => M25, 26 => M26, 27 => M27, 28 => M28, 29 => M29, 30 => M30,
        31 => M31, _ => null
    };

    public void SetMesaiDay(int day, double? value)
    {
        switch (day)
        {
            case 1: M1 = value; break; case 2: M2 = value; break; case 3: M3 = value; break; case 4: M4 = value; break;
            case 5: M5 = value; break; case 6: M6 = value; break; case 7: M7 = value; break; case 8: M8 = value; break;
            case 9: M9 = value; break; case 10: M10 = value; break; case 11: M11 = value; break; case 12: M12 = value; break;
            case 13: M13 = value; break; case 14: M14 = value; break; case 15: M15 = value; break; case 16: M16 = value; break;
            case 17: M17 = value; break; case 18: M18 = value; break; case 19: M19 = value; break; case 20: M20 = value; break;
            case 21: M21 = value; break; case 22: M22 = value; break; case 23: M23 = value; break; case 24: M24 = value; break;
            case 25: M25 = value; break; case 26: M26 = value; break; case 27: M27 = value; break; case 28: M28 = value; break;
            case 29: M29 = value; break; case 30: M30 = value; break; case 31: M31 = value; break;
        }
    }

    /// <summary>
    /// Satır toplamlarını (gün, mesai, tutar, hakediş) otomatik olarak yeniden hesaplar
    /// </summary>
    public void RecalculateTotals()
    {
        double gunSum = 0;
        for (int i = 1; i <= 31; i++)
        {
            var v = GetYevmiyeDay(i);
            if (v.HasValue && v.Value > 0) gunSum += v.Value;
        }
        ToplamGun = (decimal)gunSum;

        double mesaiSum = 0;
        for (int i = 1; i <= 31; i++)
        {
            var m = GetMesaiDay(i);
            if (m.HasValue && m.Value > 0) mesaiSum += m.Value;
        }
        MesaiToplam = (decimal)mesaiSum;

        YevmiyeTutari = ToplamGun * YevmiyeUcreti;
        MesaiTutari = MesaiToplam * MesaiUcreti;
        DigerTutar = ToplamGun * DigerUcreti;

        ToplamHakedis = (YevmiyeTutari + MesaiTutari + DigerTutar + EkKazanc) - Avans;
    }
}
