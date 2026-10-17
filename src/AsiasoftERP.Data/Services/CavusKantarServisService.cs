using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// High-performance Dapper-based Data Service for Çavuş, Kantar and Servis
/// reconciliation, matching, route tariff lookup and batch verification.
/// </summary>
public class CavusKantarServisService
{
    /// <summary>
    /// Fetches weighbridge (Kantar) receipts joined with Çavuş & Servis allocations.
    /// </summary>
    public async Task<List<CavusKantarEslesmeDto>> GetEslesmelerAsync(
        DateTime start, 
        DateTime end, 
        string? cavus = null, 
        string? durumFilter = null, 
        string? search = null, 
        int limit = 500)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var conditions = new List<string> { "k.GIRISTARIHI BETWEEN @Start AND @End" };
        var p = new DynamicParameters();
        p.Add("Start", start.Date);
        p.Add("End", end.Date.AddDays(1).AddTicks(-1));
        p.Add("Limit", limit);

        if (!string.IsNullOrWhiteSpace(cavus) && cavus != "TÜMÜ")
        {
            conditions.Add("(k.CAVUSADI = @Cavus OR cs.FIRMAADI = @Cavus)");
            p.Add("Cavus", cavus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add(@"(
                k.PLAKA LIKE @Search OR 
                k.ADISOYADI LIKE @Search OR 
                k.MALADI LIKE @Search OR 
                k.FIRMAADI LIKE @Search OR 
                k.CAVUSADI LIKE @Search OR 
                cs.FIRMAADI LIKE @Search OR
                CAST(k.GIRISKODU AS CHAR) LIKE @Search)");
            p.Add("Search", $"%{search}%");
        }

        if (durumFilter == "TamEslesen")
        {
            conditions.Add(@"((k.ONAY = 1 OR cs.ONAY = 1) OR 
                ((k.CAVUSKODU > 0 OR cs.FIRKOD > 0 OR k.CAVUSADI != '') AND (k.SERVISKONTROL > 0 OR cs.SERVIS_TUTARI > 0)))");
        }
        else if (durumFilter == "CavusBekleyen")
        {
            conditions.Add(@"(
                (k.CAVUSKODU <= 0 OR k.CAVUSKODU IS NULL) AND 
                (k.CAVUSADI IS NULL OR k.CAVUSADI = '') AND 
                (cs.FIRMAADI IS NULL OR cs.FIRMAADI = ''))");
        }
        else if (durumFilter == "ServisBekleyen")
        {
            conditions.Add(@"(
                (k.SERVISKONTROL <= 0 OR k.SERVISKONTROL IS NULL) AND 
                (cs.SERVIS_TUTARI <= 0 OR cs.SERVIS_TUTARI IS NULL))");
        }

        string whereClause = string.Join(" AND ", conditions);

        string sql = $@"
            SELECT 
                k.IDNO as IdNo,
                k.GIRISKODU as GirisKodu,
                k.GIRISTARIHI as Tarih,
                COALESCE(k.GIRISSAATI, '') as GirisSaati,
                COALESCE(k.PLAKA, '') as Plaka,
                COALESCE(k.ADISOYADI, '') as SoforAdi,
                COALESCE(k.MALADI, '') as UrunAdi,
                COALESCE(k.FIRMAADI, '') as MustahsilAdi,
                COALESCE(NULLIF(k.BOLGEADI, ''), cs.BOLGE, '') as Bolge,
                COALESCE(k.NET, 0) as NetKg,
                COALESCE(NULLIF(k.CAVUSKODU, 0), cs.FIRKOD, 0) as CavusKodu,
                COALESCE(NULLIF(k.CAVUSADI, ''), cs.FIRMAADI, '') as CavusAdi,
                COALESCE(k.CAVUSKODU2, 0) as CavusKodu2,
                COALESCE(k.CAVUSADI2, '') as CavusAdi2,
                COALESCE(k.CAVUSYEVMIYE2, 0) as CavusYevmiye2,
                COALESCE(k.CAVUSSERVIS2, 0) as CavusServis2,
                COALESCE(cs.SERVIS_SAYISI, CASE WHEN k.SERVISKONTROL > 0 THEN 1 ELSE 0 END) as ServisSayisi,
                COALESCE(cs.SERVIS_FIYATI, 0) as ServisFiyati,
                COALESCE(NULLIF(cs.SERVIS_TUTARI, 0), k.SERVISKONTROL, 0) as ServisTutari,
                COALESCE(cs.TOPLAMNAKLIYE, 0) as ToplamNakliye,
                CASE WHEN (k.ONAY = 1 OR cs.ONAY = 1) OR ((k.CAVUSKODU > 0 OR cs.FIRKOD > 0 OR k.CAVUSADI != '') AND (k.SERVISKONTROL > 0 OR cs.SERVIS_TUTARI > 0)) THEN 1 ELSE 0 END as Onay,
                k.ONAYTARIH as OnayTarihi,
                COALESCE(k.ONAYKULLANICI, cs.ONAYKULLANICI, '') as OnayKullanici,
                COALESCE(k.ACIKLAMA, cs.ACIKLAMA, '') as Aciklama
            FROM kantargirisana k
            LEFT JOIN cavusservis cs ON k.GIRISKODU = cs.GIRISKODU
            WHERE {whereClause}
            ORDER BY k.GIRISTARIHI DESC, k.IDNO DESC
            LIMIT @Limit;";

        var result = await conn.QueryAsync<CavusKantarEslesmeDto>(sql, p);
        return result.ToList();
    }

    /// <summary>
    /// Returns the distinct list of Çavuş (Harvest Crew Leaders)
    /// </summary>
    public async Task<List<CavusLookupItemDto>> GetCavuslarAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT DISTINCT 
                FIRKOD as FirKod, 
                FIRMAADI as CavusAdi 
            FROM (
                SELECT DISTINCT cbt.FIRKOD, f.FIRMAADI 
                FROM cavusbolgetanimi cbt 
                JOIN firma f ON cbt.FIRKOD = f.FIRKOD 
                WHERE cbt.FIRKOD > 0 AND f.FIRMAADI IS NOT NULL
                UNION
                SELECT DISTINCT CAVUSKODU as FIRKOD, CAVUSADI as FIRMAADI 
                FROM kantargirisana 
                WHERE CAVUSKODU > 0 AND CAVUSADI != ''
                UNION
                SELECT DISTINCT FIRKOD, FIRMAADI 
                FROM cavusservis 
                WHERE FIRKOD > 0 AND FIRMAADI != ''
            ) t
            ORDER BY CavusAdi ASC;";

        var result = await conn.QueryAsync<CavusLookupItemDto>(sql);
        return result.ToList();
    }

    /// <summary>
    /// Returns Region / Route list and standard pricing
    /// </summary>
    public async Task<List<BolgeLookupItemDto>> GetBolgelerAsync(double? firKod = null)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string sql;
        object p;

        if (firKod.HasValue && firKod.Value > 0)
        {
            sql = @"
                SELECT 
                    BOLGEKODU as BolgeKodu, 
                    BOLGEADI as BolgeAdi, 
                    COALESCE(SERVIS, 0) as VarsayilanFiyat 
                FROM cavusbolgetanimi 
                WHERE FIRKOD = @FirKod
                UNION
                SELECT 
                    KODU as BolgeKodu, 
                    ACIKLAMA as BolgeAdi, 
                    0 as VarsayilanFiyat 
                FROM servis_bolge
                ORDER BY BolgeAdi ASC;";
            p = new { FirKod = firKod.Value };
        }
        else
        {
            sql = @"
                SELECT DISTINCT
                    BOLGEKODU as BolgeKodu, 
                    BOLGEADI as BolgeAdi, 
                    MAX(COALESCE(SERVIS, 0)) as VarsayilanFiyat 
                FROM cavusbolgetanimi 
                GROUP BY BOLGEKODU, BOLGEADI
                UNION
                SELECT 
                    KODU as BolgeKodu, 
                    ACIKLAMA as BolgeAdi, 
                    0 as VarsayilanFiyat 
                FROM servis_bolge
                ORDER BY BolgeAdi ASC;";
            p = new { };
        }

        var result = await conn.QueryAsync<BolgeLookupItemDto>(sql, p);
        return result.ToList();
    }

    /// <summary>
    /// Saves or updates the match between Kantar receipt, Çavuş and Servis.
    /// Atomically updates both 'kantargirisana' and 'cavusservis'.
    /// </summary>
    public async Task<bool> SaveEslesmeAsync(CavusKantarEslesmeDto dto, string aktifKullanici)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            int onayInt = dto.Onay ? 1 : 0;

            // 1. Update kantargirisana
            const string sqlKantar = @"
                UPDATE kantargirisana SET 
                    CAVUSKODU = @CavusKodu,
                    CAVUSADI = @CavusAdi,
                    CAVUSKODU2 = @CavusKodu2,
                    CAVUSADI2 = @CavusAdi2,
                    CAVUSYEVMIYE2 = @CavusYevmiye2,
                    CAVUSSERVIS2 = @CavusServis2,
                    BOLGEADI = @Bolge,
                    SERVISKONTROL = @ServisTutari,
                    ONAY = @OnayInt,
                    ONAYTARIH = CURDATE(),
                    ONAYKULLANICI = @AktifKullanici,
                    ACIKLAMA = @Aciklama
                WHERE GIRISKODU = @GirisKodu OR IDNO = @IdNo;";

            await conn.ExecuteAsync(sqlKantar, new
            {
                dto.CavusKodu,
                dto.CavusAdi,
                dto.CavusKodu2,
                dto.CavusAdi2,
                dto.CavusYevmiye2,
                dto.CavusServis2,
                dto.Bolge,
                dto.ServisTutari,
                OnayInt = onayInt,
                AktifKullanici = aktifKullanici,
                dto.Aciklama,
                dto.GirisKodu,
                dto.IdNo
            }, tran);

            // 2. Check if cavusservis has entry
            const string checkSql = "SELECT COUNT(*) FROM cavusservis WHERE GIRISKODU = @GirisKodu;";
            int exists = await conn.ExecuteScalarAsync<int>(checkSql, new { dto.GirisKodu }, tran);

            if (exists > 0)
            {
                const string updateCsSql = @"
                    UPDATE cavusservis SET 
                        PLAKA = @Plaka,
                        SOFORUNADI = @SoforAdi,
                        MALADI = @UrunAdi,
                        FIRKOD = @CavusKodu,
                        FIRMAADI = @CavusAdi,
                        BOLGE = @Bolge,
                        NETYUK = @NetKg,
                        SERVIS_SAYISI = @ServisSayisi,
                        SERVIS_FIYATI = @ServisFiyati,
                        SERVIS_TUTARI = @ServisTutari,
                        TOPLAMNAKLIYE = @ToplamNakliye,
                        ONAY = @OnayInt,
                        ONAYTARIH = CURDATE(),
                        ONAYKULLANICI = @AktifKullanici,
                        ACIKLAMA = @Aciklama,
                        DKULLANICI = @AktifKullanici
                    WHERE GIRISKODU = @GirisKodu;";

                await conn.ExecuteAsync(updateCsSql, new
                {
                    dto.Plaka,
                    dto.SoforAdi,
                    dto.UrunAdi,
                    dto.CavusKodu,
                    dto.CavusAdi,
                    dto.Bolge,
                    dto.NetKg,
                    dto.ServisSayisi,
                    dto.ServisFiyati,
                    dto.ServisTutari,
                    dto.ToplamNakliye,
                    OnayInt = onayInt,
                    AktifKullanici = aktifKullanici,
                    dto.Aciklama,
                    dto.GirisKodu
                }, tran);
            }
            else
            {
                const string insertCsSql = @"
                    INSERT INTO cavusservis 
                    (FISNO, SIRKOD, ISLEMTARIHI, GIRISTARIHI, GIRISKODU, PLAKA, SOFORUNADI, MALADI, FIRKOD, FIRMAADI, BOLGE, NETYUK, SERVIS_SAYISI, SERVIS_FIYATI, SERVIS_TUTARI, TOPLAMNAKLIYE, ONAY, ONAYTARIH, ONAYKULLANICI, ACIKLAMA, KKULLANICI, DKULLANICI)
                    VALUES
                    (@GirisKodu, '1', NOW(), @Tarih, @GirisKodu, @Plaka, @SoforAdi, @UrunAdi, @CavusKodu, @CavusAdi, @Bolge, @NetKg, @ServisSayisi, @ServisFiyati, @ServisTutari, @ToplamNakliye, @OnayInt, CURDATE(), @AktifKullanici, @Aciklama, @AktifKullanici, @AktifKullanici);";

                await conn.ExecuteAsync(insertCsSql, new
                {
                    dto.GirisKodu,
                    dto.Tarih,
                    dto.Plaka,
                    dto.SoforAdi,
                    dto.UrunAdi,
                    dto.CavusKodu,
                    dto.CavusAdi,
                    dto.Bolge,
                    dto.NetKg,
                    dto.ServisSayisi,
                    dto.ServisFiyati,
                    dto.ServisTutari,
                    dto.ToplamNakliye,
                    OnayInt = onayInt,
                    AktifKullanici = aktifKullanici,
                    dto.Aciklama
                }, tran);
            }

            tran.Commit();
            return true;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Bulk approve matches for selected weighbridge receipt codes
    /// </summary>
    public async Task<int> TopluOnaylaAsync(List<int> girisKodlari, string aktifKullanici)
    {
        if (girisKodlari == null || girisKodlari.Count == 0) return 0;

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            const string sqlKantar = @"
                UPDATE kantargirisana SET 
                    ONAY = 1, 
                    ONAYTARIH = CURDATE(), 
                    ONAYKULLANICI = @AktifKullanici 
                WHERE GIRISKODU IN @Kodlar;";

            const string sqlServis = @"
                UPDATE cavusservis SET 
                    ONAY = 1, 
                    ONAYTARIH = CURDATE(), 
                    ONAYKULLANICI = @AktifKullanici 
                WHERE GIRISKODU IN @Kodlar;";

            int updated = await conn.ExecuteAsync(sqlKantar, new { Kodlar = girisKodlari, AktifKullanici = aktifKullanici }, tran);
            await conn.ExecuteAsync(sqlServis, new { Kodlar = girisKodlari, AktifKullanici = aktifKullanici }, tran);

            tran.Commit();
            return updated;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Returns regional route service definitions for Çavuşlar ('cavusbolgetanimi')
    /// </summary>
    public async Task<List<CavusBolgeTanimDto>> GetCavusBolgeTanimlariAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                cbt.IDNO as IdNo,
                cbt.SIRKOD as SirKod,
                cbt.FIRKOD as FirKod,
                COALESCE(f.FIRMAADI, '') as CavusAdi,
                cbt.BOLGEKODU as BolgeKodu,
                COALESCE(cbt.BOLGEADI, '') as BolgeAdi,
                COALESCE(cbt.SERVIS, 0) as BirimServisFiyati,
                COALESCE(cbt.ISYERINO, 0) as IsyeriNo,
                COALESCE(cbt.YETKIKODU, '') as YetkiKodu
            FROM cavusbolgetanimi cbt
            LEFT JOIN firma f ON cbt.FIRKOD = f.FIRKOD
            ORDER BY cbt.BOLGEADI ASC, f.FIRMAADI ASC;";

        var result = await conn.QueryAsync<CavusBolgeTanimDto>(sql);
        return result.ToList();
    }

    /// <summary>
    /// Saves or updates a Çavuş route service fee definition
    /// </summary>
    public async Task<bool> SaveCavusBolgeTanimAsync(CavusBolgeTanimDto dto)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        if (dto.IdNo > 0)
        {
            const string sql = @"
                UPDATE cavusbolgetanimi SET 
                    FIRKOD = @FirKod, 
                    BOLGEKODU = @BolgeKodu, 
                    BOLGEADI = @BolgeAdi, 
                    SERVIS = @BirimServisFiyati 
                WHERE IDNO = @IdNo;";
            await conn.ExecuteAsync(sql, dto);
        }
        else
        {
            const string sql = @"
                INSERT INTO cavusbolgetanimi (SIRKOD, FIRKOD, BOLGEKODU, BOLGEADI, SERVIS, ISYERINO, YETKIKODU)
                VALUES ('1', @FirKod, @BolgeKodu, @BolgeAdi, @BirimServisFiyati, 0, 'MERKEZ');";
            await conn.ExecuteAsync(sql, dto);
        }

        return true;
    }

    /// <summary>
    /// Calculates dashboard KPI metrics for the period
    /// </summary>
    public async Task<CavusKantarKpiDto> GetKpiSummaryAsync(DateTime start, DateTime end)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COUNT(*) as ToplamSefer,
                COALESCE(SUM(k.NET), 0) as ToplamNetKg,
                COALESCE(SUM(CASE WHEN (k.ONAY = 1 OR cs.ONAY = 1) OR ((k.CAVUSKODU > 0 OR cs.FIRKOD > 0) AND (k.SERVISKONTROL > 0 OR cs.SERVIS_TUTARI > 0)) THEN 1 ELSE 0 END), 0) as EslesenSefer,
                COALESCE(SUM(CASE WHEN (k.CAVUSKODU <= 0 AND (cs.FIRKOD IS NULL OR cs.FIRKOD <= 0)) OR (k.SERVISKONTROL <= 0 AND (cs.SERVIS_TUTARI IS NULL OR cs.SERVIS_TUTARI <= 0)) THEN 1 ELSE 0 END), 0) as BekleyenSefer,
                COALESCE(SUM(COALESCE(cs.SERVIS_SAYISI, CASE WHEN k.SERVISKONTROL > 0 THEN 1 ELSE 0 END)), 0) as ToplamServisSayisi,
                COALESCE(SUM(COALESCE(NULLIF(cs.SERVIS_TUTARI, 0), k.SERVISKONTROL, 0)), 0) as ToplamServisMaliyeti
            FROM kantargirisana k
            LEFT JOIN cavusservis cs ON k.GIRISKODU = cs.GIRISKODU
            WHERE k.GIRISTARIHI BETWEEN @Start AND @End;";

        var p = new { Start = start.Date, End = end.Date.AddDays(1).AddTicks(-1) };
        var kpi = await conn.QueryFirstOrDefaultAsync<CavusKantarKpiDto>(sql, p);
        return kpi ?? new CavusKantarKpiDto();
    }
}
