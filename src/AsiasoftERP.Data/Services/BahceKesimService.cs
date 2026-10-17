using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// High-performance Dapper-based Data Service for Orchard Purchase & Cutting Contracts
/// and harvest cost tracking connecting to 'sozlesmebahceana' and 'bahce_kesim_maliyet'.
/// </summary>
public class BahceKesimService
{
    public async Task<List<BahceSozlesmeDto>> GetSozlesmelerAsync(
        DateTime? start = null,
        DateTime? end = null,
        string? durum = null,
        string? search = null,
        int limit = 500)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var conditions = new List<string> { "s.SOZLESMENO > 0" };
        var p = new DynamicParameters();
        p.Add("Limit", limit);

        if (start.HasValue && end.HasValue)
        {
            conditions.Add("s.TARIH BETWEEN @Start AND @End");
            p.Add("Start", start.Value.Date);
            p.Add("End", end.Value.Date.AddDays(1).AddTicks(-1));
        }

        if (!string.IsNullOrWhiteSpace(durum) && durum != "Tümü")
        {
            conditions.Add("s.DURUMU = @Durum");
            p.Add("Durum", durum);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add(@"(
                s.FIRMAADI LIKE @Search OR 
                s.BAHCEADI LIKE @Search OR 
                s.MEVKI LIKE @Search OR 
                s.MALADI LIKE @Search OR 
                s.ADA LIKE @Search OR 
                s.PARSEL LIKE @Search OR 
                CAST(s.SOZLESMENO AS CHAR) LIKE @Search)");
            p.Add("Search", $"%{search}%");
        }

        string whereClause = string.Join(" AND ", conditions);

        string sql = $@"
            SELECT 
                s.SIRANO as SiraNo,
                s.SOZLESMENO as SozlesmeNo,
                COALESCE(s.TURU, 'TONAJ') as SozlesmeTuru,
                COALESCE(s.SOZLESMEADI, '') as SozlesmeAdi,
                s.TARIH as Tarih,
                COALESCE(s.BELGENO, '') as BelgeNo,
                COALESCE(s.FIRKOD, 0) as FirKod,
                COALESCE(s.FIRMAADI, '') as FirmaAdi,
                COALESCE(s.BAHCEADI, '') as BahceAdi,
                COALESCE(s.MEVKI, '') as Mevki,
                COALESCE(s.BOLGEKODU, 0) as BolgeKodu,
                COALESCE(s.BOLGEADI, '') as BolgeAdi,
                COALESCE(s.ADA, '') as Ada,
                COALESCE(s.PARSEL, '') as Parsel,
                COALESCE(s.DURUMU, 'Aktif') as Durumu,
                COALESCE(s.MALKODU, '') as MalKodu,
                COALESCE(s.MALADI, '') as MalAdi,
                COALESCE(s.BIRIM, 'KG') as Birim,
                COALESCE(s.MIKTAR, 0) as HedefKg,
                COALESCE(s.FIYAT, 0) as Fiyat,
                COALESCE(s.TUTAR, s.KABALAFIYAT, 0) as SozlesmeTutari,
                COALESCE(s.KABALAFIYAT, 0) as KabalaFiyat,
                COALESCE(s.TAH_BOX, 0) as TahBox,
                COALESCE(s.TAH_PALET, 0) as TahPalet,
                COALESCE(s.TAH_KASA, 0) as TahKasa,
                COALESCE(s.ACIKLAMA, '') as Aciklama,
                COALESCE(s.KKULLANICI, '') as Kullanici,
                COALESCE(km.GerceklesenKg, 0) as GerceklesenKg,
                COALESCE(km.ToplamMaliyet, 0) as GerceklesenMaliyet,
                COALESCE(km.SeferSayisi, 0) as SeferSayisi
            FROM sozlesmebahceana s
            LEFT JOIN (
                SELECT 
                    SOZLESMENO, 
                    SUM(NETKG) as GerceklesenKg, 
                    SUM(GENEL_TOPLAM) as ToplamMaliyet,
                    COUNT(DISTINCT IDNO) as SeferSayisi
                FROM bahce_kesim_maliyet 
                WHERE SOZLESMENO > 0 
                GROUP BY SOZLESMENO
            ) km ON s.SOZLESMENO = km.SOZLESMENO
            WHERE {whereClause}
            ORDER BY s.TARIH DESC, s.SIRANO DESC
            LIMIT @Limit;";

        var result = await conn.QueryAsync<BahceSozlesmeDto>(sql, p);
        return result.ToList();
    }

    public async Task<List<BahceKesimSeferDto>> GetKesimSeferleriAsync(double? sozlesmeNo = null, int limit = 300)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string whereClause = sozlesmeNo.HasValue && sozlesmeNo.Value > 0 
            ? "WHERE SOZLESMENO = @SozlesmeNo" 
            : "WHERE SOZLESMENO > 0";

        string sql = $@"
            SELECT 
                IDNO as IdNo,
                SOZLESMENO as SozlesmeNo,
                TARIH as Tarih,
                COALESCE(CAST(PLAKA AS CHAR), '') as Plaka,
                COALESCE(SOFOR, '') as Sofor,
                COALESCE(BOLGE, '') as Bolge,
                COALESCE(KESIM_CAVUSU_CAVUSU, '') as KesimCavusu,
                COALESCE(MALADI, '') as MalAdi,
                COALESCE(KASA, 0) as Kasa,
                COALESCE(NETKG, 0) as NetKg,
                COALESCE(FIYAT, 0) as Fiyat,
                COALESCE(MAL_TUTARI, 0) as MalTutari,
                COALESCE(YEVMIYE_TUTARI, 0) as YevmiyeTutari,
                COALESCE(SERVIS_TUTARI, 0) as ServisTutari,
                COALESCE(NAKLIYE_TUTARI, 0) as NakliyeTutari,
                COALESCE(EXPER_TUTARI, 0) as ExperTutari,
                COALESCE(GENEL_TOPLAM, 0) as GenelToplam,
                COALESCE(KG_MALIYET, 0) as KgMaliyet
            FROM bahce_kesim_maliyet
            {whereClause}
            ORDER BY TARIH DESC, IDNO DESC
            LIMIT @Limit;";

        var result = await conn.QueryAsync<BahceKesimSeferDto>(sql, new { SozlesmeNo = sozlesmeNo, Limit = limit });
        return result.ToList();
    }

    public async Task<double> GetNextSozlesmeNoAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        const string sql = "SELECT COALESCE(MAX(SOZLESMENO), 1000) + 1 FROM sozlesmebahceana;";
        return await conn.ExecuteScalarAsync<double>(sql);
    }

    public async Task<bool> SaveSozlesmeAsync(BahceSozlesmeDto dto, string user)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            if (dto.SiraNo > 0)
            {
                const string updateSql = @"
                    UPDATE sozlesmebahceana SET
                        TURU = @SozlesmeTuru,
                        SOZLESMEADI = @SozlesmeAdi,
                        TARIH = @Tarih,
                        BELGENO = @BelgeNo,
                        FIRKOD = @FirKod,
                        FIRMAADI = @FirmaAdi,
                        BAHCEADI = @BahceAdi,
                        MEVKI = @Mevki,
                        ADA = @Ada,
                        PARSEL = @Parsel,
                        MALKODU = @MalKodu,
                        MALADI = @MalAdi,
                        MIKTAR = @HedefKg,
                        FIYAT = @Fiyat,
                        TUTAR = @SozlesmeTutari,
                        KABALAFIYAT = @KabalaFiyat,
                        TAH_BOX = @TahBox,
                        TAH_PALET = @TahPalet,
                        TAH_KASA = @TahKasa,
                        DURUMU = @Durumu,
                        ACIKLAMA = @Aciklama,
                        DKULLANICI = @User
                    WHERE SIRANO = @SiraNo;";

                await conn.ExecuteAsync(updateSql, new
                {
                    dto.SozlesmeTuru,
                    dto.SozlesmeAdi,
                    dto.Tarih,
                    dto.BelgeNo,
                    dto.FirKod,
                    dto.FirmaAdi,
                    dto.BahceAdi,
                    dto.Mevki,
                    dto.Ada,
                    dto.Parsel,
                    dto.MalKodu,
                    dto.MalAdi,
                    dto.HedefKg,
                    dto.Fiyat,
                    dto.SozlesmeTutari,
                    dto.KabalaFiyat,
                    dto.TahBox,
                    dto.TahPalet,
                    dto.TahKasa,
                    dto.Durumu,
                    dto.Aciklama,
                    User = user,
                    dto.SiraNo
                }, tran);
            }
            else
            {
                if (dto.SozlesmeNo <= 0)
                {
                    dto.SozlesmeNo = await GetNextSozlesmeNoAsync();
                }

                const string insertSql = @"
                    INSERT INTO sozlesmebahceana (
                        SIRKOD, TURU, SOZLESMENO, SOZLESMEADI, TARIH, ISLEMTARIHI, BELGENO,
                        FIRKOD, FIRMAADI, BAHCEADI, MEVKI, ADA, PARSEL,
                        MALKODU, MALADI, BIRIM, MIKTAR, FIYAT, TUTAR, KABALAFIYAT,
                        TAH_BOX, TAH_PALET, TAH_KASA, DURUMU, ACIKLAMA, KKULLANICI, DKULLANICI
                    ) VALUES (
                        '1', @SozlesmeTuru, @SozlesmeNo, @SozlesmeAdi, @Tarih, NOW(), @BelgeNo,
                        @FirKod, @FirmaAdi, @BahceAdi, @Mevki, @Ada, @Parsel,
                        @MalKodu, @MalAdi, @Birim, @HedefKg, @Fiyat, @SozlesmeTutari, @KabalaFiyat,
                        @TahBox, @TahPalet, @TahKasa, @Durumu, @Aciklama, @User, @User
                    );";

                await conn.ExecuteAsync(insertSql, new
                {
                    dto.SozlesmeTuru,
                    dto.SozlesmeNo,
                    dto.SozlesmeAdi,
                    dto.Tarih,
                    dto.BelgeNo,
                    dto.FirKod,
                    dto.FirmaAdi,
                    dto.BahceAdi,
                    dto.Mevki,
                    dto.Ada,
                    dto.Parsel,
                    dto.MalKodu,
                    dto.MalAdi,
                    dto.Birim,
                    dto.HedefKg,
                    dto.Fiyat,
                    dto.SozlesmeTutari,
                    dto.KabalaFiyat,
                    dto.TahBox,
                    dto.TahPalet,
                    dto.TahKasa,
                    dto.Durumu,
                    dto.Aciklama,
                    User = user
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

    public async Task<BahceKpiDto> GetKpiSummaryAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COUNT(*) as ToplamSozlesme,
                COALESCE(SUM(CASE WHEN DURUMU = 'Aktif' THEN 1 ELSE 0 END), 0) as AktifSozlesme,
                COALESCE(SUM(MIKTAR) / 1000.0, 0) as ToplamTaahhutTon,
                COALESCE(SUM(COALESCE(TUTAR, KABALAFIYAT)), 0) as ToplamSozlesmeHacmi
            FROM sozlesmebahceana
            WHERE SOZLESMENO > 0;";

        var kpi = await conn.QueryFirstOrDefaultAsync<BahceKpiDto>(sql) ?? new BahceKpiDto();

        const string sqlMaliyet = @"
            SELECT 
                COALESCE(SUM(NETKG) / 1000.0, 0) as ToplamGerceklesenTon,
                COALESCE(SUM(GENEL_TOPLAM), 0) as ToplamGerceklesenMaliyet
            FROM bahce_kesim_maliyet
            WHERE SOZLESMENO > 0;";

        var maliyet = await conn.QueryFirstOrDefaultAsync<BahceKpiDto>(sqlMaliyet);
        if (maliyet != null)
        {
            kpi.ToplamGerceklesenTon = maliyet.ToplamGerceklesenTon;
            kpi.ToplamGerceklesenMaliyet = maliyet.ToplamGerceklesenMaliyet;
        }

        return kpi;
    }
}
