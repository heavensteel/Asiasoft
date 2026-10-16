using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

public class PuantajService
{
    private static readonly CultureInfo TrCulture = new CultureInfo("tr-TR");

    public async Task<List<PuantajFisDto>> GetPuantajFisleriAsync(DateTime startDate, DateTime endDate, string? search = null)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = " AND (TGADI LIKE @p OR BOLGE_ADI LIKE @p OR CFIRMAADI LIKE @p OR CAST(FISNO AS CHAR) LIKE @p)";
        }

        string startAy = startDate.ToString("MMMM", TrCulture).ToUpper(TrCulture);
        string endAy = endDate.ToString("MMMM", TrCulture).ToUpper(TrCulture);

        string sql = $@"
            SELECT 
                SIRKOD as SirKod,
                CAST(FISNO AS SIGNED) as FisNo,
                CAST(COALESCE(FATNO, 0) AS SIGNED) as FatNo,
                UID as Uid,
                TGKODU as TgKodu,
                COALESCE(TGADI, '') as TgAdi,
                SATICIKODU as SaticiKodu,
                COALESCE(SATICIADI, '') as SaticiAdi,
                CFIRKOD as CFirKod,
                COALESCE(CFIRMAADI, '') as CFirmaAdi,
                BOLGE_KODU as BolgeKodu,
                COALESCE(BOLGE_ADI, '') as BolgeAdi,
                COALESCE(AY, '') as Ay,
                COALESCE(YIL, 0) as Yil,
                BTARIH as BTarih,
                STARIH as STarih,
                COALESCE(YTOPLAM, 0) as YevmiyeToplam,
                COALESCE(MTOPLAM, 0) as MesaiToplam,
                COALESCE(YTUTAR, 0) as YevmiyeTutari,
                COALESCE(MESAITUTAR, 0) as MesaiTutari,
                COALESCE(EKKAZANC, 0) as EkKazanc,
                COALESCE(SERVIS_SAYISI, 0) as ServisSayisi,
                COALESCE(SERVIS_FIYAT, 0) as ServisFiyati,
                COALESCE(SERVIS_TUTARI, 0) as ServisTutari,
                COALESCE(KALAN, 0) as KalanServisTutari,
                COALESCE(YEVMIYE_TUTARI, 0) as ToplamHakedis,
                COALESCE(AVANS, 0) as Avans,
                COALESCE(AVANS2, 0) as ServisAvansi,
                COALESCE(ACIKLAMA, '') as Aciklama
            FROM erp.yevmiyefis
            WHERE (
                (BTARIH <= @end AND STARIH >= @start)
                OR (BTARIH BETWEEN @start AND @end)
                OR (YIL BETWEEN @startYil AND @endYil AND (AY = @startAy OR AY = @endAy))
            )
            {searchClause}
            ORDER BY BTARIH DESC, FISNO DESC;";

        var param = new
        {
            start = startDate.Date,
            end = endDate.Date,
            startYil = startDate.Year,
            endYil = endDate.Year,
            startAy,
            endAy,
            p = $"%{search}%"
        };

        var list = (await conn.QueryAsync<PuantajFisDto>(sql, param)).ToList();
        return list;
    }

    public async Task<List<PuantajDetayDto>> GetPuantajDetaylariAsync(int fisNo)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT 
                SIRKOD as SirKod,
                CAST(FISNO AS SIGNED) as FisNo,
                CAST(SIRA AS SIGNED) as Sira,
                CAST(COALESCE(FATNO, 0) AS SIGNED) as FatNo,
                FIRKOD as FirKod,
                COALESCE(FIRMAADI, '') as FirmaAdi,
                COALESCE(AY, '') as Ay,
                COALESCE(YIL, 0) as Yil,
                Y1, Y2, Y3, Y4, Y5, Y6, Y7, Y8, Y9, Y10,
                Y11, Y12, Y13, Y14, Y15, Y16, Y17, Y18, Y19, Y20,
                Y21, Y22, Y23, Y24, Y25, Y26, Y27, Y28, Y29, Y30, Y31,
                M1, M2, M3, M4, M5, M6, M7, M8, M9, M10,
                M11, M12, M13, M14, M15, M16, M17, M18, M19, M20,
                M21, M22, M23, M24, M25, M26, M27, M28, M29, M30, M31,
                COALESCE(MTOPLAM, 0) as MesaiToplam,
                COALESCE(MFIYAT, 0) as MesaiUcreti,
                COALESCE(MTUTAR, 0) as MesaiTutari,
                COALESCE(TOPLAM, 0) as ToplamGun,
                COALESCE(FIYAT, 0) as YevmiyeUcreti,
                COALESCE(TUTAR, 0) as YevmiyeTutari,
                COALESCE(SERVIS_FIYAT, 0) as DigerUcreti,
                COALESCE(SERVIS_TUTARI, 0) as DigerTutar,
                COALESCE(EKKAZANC, 0) as EkKazanc,
                COALESCE(AVANS, 0) as Avans,
                CASE WHEN YEVMIYECIMI = 'True' OR YEVMIYECIMI = '1' THEN 1 ELSE 0 END as YevmiyeciMi
            FROM erp.yevmiyehrk
            WHERE FISNO = @fisNo
            ORDER BY SIRA ASC;";

        var list = (await conn.QueryAsync<PuantajDetayDto>(sql, new { fisNo })).ToList();

        // Calculate ToplamHakedis for each row
        foreach (var d in list)
        {
            d.RecalculateTotals();
        }

        return list;
    }

    public async Task<int> SavePuantajFisAsync(PuantajFisDto fis, List<PuantajDetayDto> detaylar)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // 1. Otomatik Toplamları Hesapla
            decimal totalYevmiye = detaylar.Sum(d => d.ToplamGun);
            decimal totalMesai = detaylar.Sum(d => d.MesaiToplam);
            decimal totalYevmiyeTutari = detaylar.Sum(d => d.YevmiyeTutari);
            decimal totalMesaiTutari = detaylar.Sum(d => d.MesaiTutari);
            decimal totalEkKazanc = detaylar.Sum(d => d.EkKazanc);
            decimal totalAvans = detaylar.Sum(d => d.Avans);

            fis.YevmiyeToplam = totalYevmiye;
            fis.MesaiToplam = totalMesai;
            fis.YevmiyeTutari = totalYevmiyeTutari;
            fis.MesaiTutari = totalMesaiTutari;
            fis.EkKazanc = totalEkKazanc;
            fis.Avans = totalAvans;
            fis.ServisTutari = fis.ServisSayisi * fis.ServisFiyati;
            fis.KalanServisTutari = fis.ServisTutari - fis.ServisAvansi;
            fis.ToplamHakedis = fis.YevmiyeTutari + fis.MesaiTutari + fis.ServisTutari + fis.EkKazanc - fis.Avans;

            // 2. Fiş Numarası Belirleme
            if (fis.FisNo <= 0)
            {
                int maxFisNo = await conn.ExecuteScalarAsync<int>(
                    "SELECT COALESCE(MAX(CAST(FISNO AS SIGNED)), 0) FROM erp.yevmiyefis", transaction: tx);
                fis.FisNo = maxFisNo + 1;
                if (fis.FatNo <= 0) fis.FatNo = fis.FisNo;
            }

            // 3. yevmiyefis tablosunu UPSERT et
            string upsertMasterSql = @"
                INSERT INTO erp.yevmiyefis (
                    SIRKOD, FISNO, FATNO, UID, TGKODU, TGADI, SATICIKODU, SATICIADI,
                    CFIRKOD, CFIRMAADI, BOLGE_KODU, BOLGE_ADI, AY, YIL, BTARIH, STARIH,
                    YTOPLAM, MTOPLAM, YTUTAR, MESAITUTAR, EKKAZANC,
                    SERVIS_SAYISI, SERVIS_FIYAT, SERVIS_TUTARI, KALAN,
                    YEVMIYE_TUTARI, AVANS, AVANS2, ACIKLAMA, KKULLANICI, DKULLANICI
                ) VALUES (
                    @SirKod, @FisNo, @FatNo, @Uid, @TgKodu, @TgAdi, @SaticiKodu, @SaticiAdi,
                    @CFirKod, @CFirmaAdi, @BolgeKodu, @BolgeAdi, @Ay, @Yil, @BTarih, @STarih,
                    @YevmiyeToplam, @MesaiToplam, @YevmiyeTutari, @MesaiTutari, @EkKazanc,
                    @ServisSayisi, @ServisFiyati, @ServisTutari, @KalanServisTutari,
                    @ToplamHakedis, @Avans, @ServisAvansi, @Aciklama, @KKullanici, @DKullanici
                )
                ON DUPLICATE KEY UPDATE
                    FATNO = VALUES(FATNO),
                    TGADI = VALUES(TGADI),
                    BOLGE_ADI = VALUES(BOLGE_ADI),
                    AY = VALUES(AY),
                    YIL = VALUES(YIL),
                    BTARIH = VALUES(BTARIH),
                    STARIH = VALUES(STARIH),
                    YTOPLAM = VALUES(YTOPLAM),
                    MTOPLAM = VALUES(MTOPLAM),
                    YTUTAR = VALUES(YTUTAR),
                    MESAITUTAR = VALUES(MESAITUTAR),
                    EKKAZANC = VALUES(EKKAZANC),
                    SERVIS_SAYISI = VALUES(SERVIS_SAYISI),
                    SERVIS_FIYAT = VALUES(SERVIS_FIYAT),
                    SERVIS_TUTARI = VALUES(SERVIS_TUTARI),
                    KALAN = VALUES(KALAN),
                    YEVMIYE_TUTARI = VALUES(YEVMIYE_TUTARI),
                    AVANS = VALUES(AVANS),
                    AVANS2 = VALUES(AVANS2),
                    ACIKLAMA = VALUES(ACIKLAMA),
                    DKULLANICI = VALUES(DKULLANICI);";

            await conn.ExecuteAsync(upsertMasterSql, fis, transaction: tx);

            // 4. Detayları güncelle: Mevcut detayları temizleyip yenilerini ekleyelim (ya da SIRA ile koru)
            await conn.ExecuteAsync("DELETE FROM erp.yevmiyehrk WHERE FISNO = @FisNo", new { fis.FisNo }, transaction: tx);

            string insertDetailSql = @"
                INSERT INTO erp.yevmiyehrk (
                    SIRKOD, FISNO, FATNO, AY, YIL, FIRKOD, FIRMAADI,
                    Y1, Y2, Y3, Y4, Y5, Y6, Y7, Y8, Y9, Y10,
                    Y11, Y12, Y13, Y14, Y15, Y16, Y17, Y18, Y19, Y20,
                    Y21, Y22, Y23, Y24, Y25, Y26, Y27, Y28, Y29, Y30, Y31,
                    M1, M2, M3, M4, M5, M6, M7, M8, M9, M10,
                    M11, M12, M13, M14, M15, M16, M17, M18, M19, M20,
                    M21, M22, M23, M24, M25, M26, M27, M28, M29, M30, M31,
                    MTOPLAM, MFIYAT, MTUTAR, TOPLAM, FIYAT, TUTAR,
                    SERVIS_FIYAT, SERVIS_TUTARI, EKKAZANC, AVANS, YEVMIYECIMI
                ) VALUES (
                    @SirKod, @FisNo, @FatNo, @Ay, @Yil, @FirKod, @FirmaAdi,
                    @Y1, @Y2, @Y3, @Y4, @Y5, @Y6, @Y7, @Y8, @Y9, @Y10,
                    @Y11, @Y12, @Y13, @Y14, @Y15, @Y16, @Y17, @Y18, @Y19, @Y20,
                    @Y21, @Y22, @Y23, @Y24, @Y25, @Y26, @Y27, @Y28, @Y29, @Y30, @Y31,
                    @M1, @M2, @M3, @M4, @M5, @M6, @M7, @M8, @M9, @M10,
                    @M11, @M12, @M13, @M14, @M15, @M16, @M17, @M18, @M19, @M20,
                    @M21, @M22, @M23, @M24, @M25, @M26, @M27, @M28, @M29, @M30, @M31,
                    @MesaiToplam, @MesaiUcreti, @MesaiTutari, @ToplamGun, @YevmiyeUcreti, @YevmiyeTutari,
                    @DigerUcreti, @DigerTutar, @EkKazanc, @Avans, @YevmiyeciStr
                );";

            foreach (var d in detaylar)
            {
                d.FisNo = fis.FisNo;
                d.FatNo = fis.FatNo;
                d.Ay = fis.Ay;
                d.Yil = fis.Yil;
                d.SirKod = fis.SirKod;
                d.RecalculateTotals();

                await conn.ExecuteAsync(insertDetailSql, new
                {
                    d.SirKod,
                    d.FisNo,
                    d.FatNo,
                    d.Ay,
                    d.Yil,
                    d.FirKod,
                    d.FirmaAdi,
                    d.Y1, d.Y2, d.Y3, d.Y4, d.Y5, d.Y6, d.Y7, d.Y8, d.Y9, d.Y10,
                    d.Y11, d.Y12, d.Y13, d.Y14, d.Y15, d.Y16, d.Y17, d.Y18, d.Y19, d.Y20,
                    d.Y21, d.Y22, d.Y23, d.Y24, d.Y25, d.Y26, d.Y27, d.Y28, d.Y29, d.Y30, d.Y31,
                    d.M1, d.M2, d.M3, d.M4, d.M5, d.M6, d.M7, d.M8, d.M9, d.M10,
                    d.M11, d.M12, d.M13, d.M14, d.M15, d.M16, d.M17, d.M18, d.M19, d.M20,
                    d.M21, d.M22, d.M23, d.M24, d.M25, d.M26, d.M27, d.M28, d.M29, d.M30, d.M31,
                    d.MesaiToplam,
                    d.MesaiUcreti,
                    d.MesaiTutari,
                    d.ToplamGun,
                    d.YevmiyeUcreti,
                    d.YevmiyeTutari,
                    d.DigerUcreti,
                    d.DigerTutar,
                    d.EkKazanc,
                    d.Avans,
                    YevmiyeciStr = d.YevmiyeciMi ? "True" : "False"
                }, transaction: tx);
            }

            tx.Commit();
            return fis.FisNo;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task DeletePuantajFisAsync(int fisNo)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync("DELETE FROM erp.yevmiyehrk WHERE FISNO = @fisNo", new { fisNo }, transaction: tx);
            await conn.ExecuteAsync("DELETE FROM erp.yevmiyefis WHERE FISNO = @fisNo", new { fisNo }, transaction: tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Servis toplam tutarını işçilerin çalışma gün sayısına oranla dağıtır
    /// </summary>
    public void DistributeServisCost(PuantajFisDto fis, List<PuantajDetayDto> detaylar)
    {
        decimal totalDays = detaylar.Sum(d => d.ToplamGun);
        decimal servisTutari = fis.ServisSayisi * fis.ServisFiyati;
        fis.ServisTutari = servisTutari;

        decimal unitRate = totalDays > 0 ? (servisTutari / totalDays) : 0;

        foreach (var d in detaylar)
        {
            d.DigerUcreti = Math.Round(unitRate, 2);
            d.DigerTutar = Math.Round(d.ToplamGun * unitRate, 2);
            d.RecalculateTotals();
        }
    }

    public async Task<List<string>> GetIsyerleriAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        var list = (await conn.QueryAsync<string>("SELECT DISTINCT TGADI FROM erp.yevmiyefis WHERE TGADI IS NOT NULL AND TGADI <> '' ORDER BY TGADI")).ToList();
        return list;
    }

    public async Task<List<string>> GetBolgelerAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        var list = (await conn.QueryAsync<string>("SELECT DISTINCT BOLGE_ADI FROM erp.yevmiyefis WHERE BOLGE_ADI IS NOT NULL AND BOLGE_ADI <> '' ORDER BY BOLGE_ADI")).ToList();
        return list;
    }

    public async Task<List<string>> GetAmeleRehberAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        var list = (await conn.QueryAsync<string>(
            "SELECT DISTINCT FIRMAADI FROM erp.yevmiyehrk WHERE FIRMAADI IS NOT NULL AND FIRMAADI <> '' ORDER BY FIRMAADI LIMIT 1000")).ToList();
        return list;
    }
}
