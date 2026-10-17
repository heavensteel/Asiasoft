using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

public class IrsaliyeService
{
    public async Task<List<IrsaliyeStkDto>> GetSatisIrsaliyeleriAsync(
        DateTime start, 
        DateTime end, 
        string search = "", 
        string faturaDurumu = "Tümü", 
        string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string faturaCondition = faturaDurumu switch
        {
            "Açık İrsaliyeler" or "AcikIrsaliyeler" => "AND (FATNO IS NULL OR FATNO = 0)",
            "Faturalananlar" or "Faturalandı" => "AND (FATNO > 0)",
            _ => ""
        };

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = @" AND (
                FIRMAADI LIKE @searchPattern OR 
                COALESCE(GIBIRSNO, '') LIKE @searchPattern OR 
                COALESCE(PLAKA, '') LIKE @searchPattern OR 
                COALESCE(SOFORADI, '') LIKE @searchPattern OR 
                CAST(FISNO AS CHAR) LIKE @searchPattern OR 
                CAST(IRSNO AS CHAR) LIKE @searchPattern
            )";
        }

        string sql = $@"
            SELECT 
                f.SIRANO as SiraNo,
                COALESCE(f.SIRKOD, '1') as Sirkod,
                COALESCE(f.FISTUR, 222) as FisTur,
                COALESCE(f.FISNO, 0) as FisNo,
                COALESCE(f.IRSNO, 0) as IrsNo,
                COALESCE(f.GIBIRSNO, '') as GibIrsNo,
                f.FISTAR as FisTar,
                f.IRSTAR as IrsTar,
                f.SAAT as Saat,
                COALESCE(f.FIRKOD, 0) as FirKod,
                COALESCE(f.FIRMAADI, '') as FirmaAdi,
                COALESCE(f.FIRMAADRESI, '') as FirmaAdresi,
                COALESCE(f.VERGIDAIRESI, '') as VergiDairesi,
                COALESCE(f.VERGINUMARASI, '') as VergiNumarasi,
                COALESCE(f.SEHIR, '') as Sehir,
                COALESCE(f.ILCE, '') as Ilce,
                COALESCE(f.ULKE, 'TÜRKİYE') as Ulke,
                COALESCE(f.PLAKA, '') as Plaka,
                COALESCE(f.ARKAPLAKA, '') as ArkaPlaka,
                COALESCE(f.SOFORADI, '') as SoforAdi,
                COALESCE(f.SOFORSOYADI, '') as SoforSoyadi,
                COALESCE(f.SOFORTC, '') as SoforTc,
                COALESCE(f.SOFORTELEFON, '') as SoforTelefon,
                COALESCE(f.TASIYICIFIRMA, '') as TasiyiciFirma,
                COALESCE(f.TASIYICIFIRMA_VKN, '') as TasiyiciFirmaVkn,
                COALESCE(f.DEPOKODU, 9) as DepoKodu,
                COALESCE(f.DEPOADI, 'ANA DEPO') as DepoAdi,
                COALESCE(f.ACIKLAMA, '') as Aciklama,
                COALESCE(f.SIPARISNO, '') as SiparisNo,
                COALESCE(NULLIF(f.TOPLAMPALET, 0), SUM(d.PALET), 0) as ToplamPalet,
                COALESCE(NULLIF(f.TOPLAMKASA, 0), SUM(d.KASA), 0) as ToplamKasa,
                COALESCE(NULLIF(f.TOPLAMMIKTAR, 0), SUM(d.MIKTAR), 0) as ToplamMiktar,
                COALESCE(f.TOPLAMFIRE, 0) as ToplamFire,
                COALESCE(NULLIF(f.FISTUT, 0), SUM(d.TUTAR), 0) as FisTut,
                COALESCE(f.ISKTUT, 0) as IskTut,
                COALESCE(NULLIF(f.KDVTUT, 0), SUM(d.KDVTUT), 0) as KdvTut,
                COALESCE(NULLIF(f.GENTUT, 0), SUM(d.TUTAR) + SUM(d.KDVTUT), 0) as GenTut,
                COALESCE(f.DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(f.DOVIZTUTAR, 0) as DovizTutar,
                f.FATNO as FatNo,
                COALESCE(f.KAPANDI, '') as Kapandi,
                COALESCE(f.USER, '') as User
            FROM irsstkfis f
            LEFT JOIN irsstkdet d ON f.FISNO = d.FISNO AND f.FISTUR = d.FISTUR AND f.SIRKOD = d.SIRKOD
            WHERE f.SIRKOD = @sirkod 
              AND f.FISTUR = 222
              AND f.FISTAR >= @start AND f.FISTAR <= @end
              {faturaCondition}
              {searchClause.Replace("FIRMAADI", "f.FIRMAADI").Replace("GIBIRSNO", "f.GIBIRSNO").Replace("PLAKA", "f.PLAKA").Replace("SOFORADI", "f.SOFORADI").Replace("FISNO", "f.FISNO").Replace("IRSNO", "f.IRSNO")}
            GROUP BY f.SIRANO
            ORDER BY f.FISTAR DESC, f.FISNO DESC
            LIMIT 1000;";

        var prms = new
        {
            sirkod,
            start = start.Date,
            end = end.Date,
            searchPattern = $"%{search.Trim()}%"
        };

        var rows = await conn.QueryAsync<IrsaliyeStkDto>(sql, prms);
        return rows.ToList();
    }

    public async Task<List<IrsaliyeStkDto>> GetAlisIrsaliyeleriAsync(
        DateTime start, 
        DateTime end, 
        string search = "", 
        string faturaDurumu = "Tümü", 
        string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string faturaCondition = faturaDurumu switch
        {
            "Açık İrsaliyeler" or "AcikIrsaliyeler" => "AND (FATNO IS NULL OR FATNO = 0)",
            "Faturalananlar" or "Faturalandı" => "AND (FATNO > 0)",
            _ => ""
        };

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = @" AND (
                f.FIRMAADI LIKE @searchPattern OR 
                COALESCE(f.GIBIRSNO, '') LIKE @searchPattern OR 
                COALESCE(f.PLAKA, '') LIKE @searchPattern OR 
                COALESCE(f.SOFORADI, '') LIKE @searchPattern OR 
                CAST(f.FISNO AS CHAR) LIKE @searchPattern OR 
                CAST(f.IRSNO AS CHAR) LIKE @searchPattern
            )";
        }

        string sql = $@"
            SELECT 
                f.SIRANO as SiraNo,
                COALESCE(f.SIRKOD, '1') as Sirkod,
                COALESCE(f.FISTUR, 220) as FisTur,
                COALESCE(f.FISNO, 0) as FisNo,
                COALESCE(f.IRSNO, 0) as IrsNo,
                COALESCE(f.GIBIRSNO, '') as GibIrsNo,
                COALESCE(f.FISTAR, f.IRSTAR) as FisTar,
                COALESCE(f.IRSTAR, f.FISTAR) as IrsTar,
                f.SAAT as Saat,
                COALESCE(f.FIRKOD, 0) as FirKod,
                COALESCE(f.FIRMAADI, '') as FirmaAdi,
                COALESCE(f.FIRMAADRESI, '') as FirmaAdresi,
                COALESCE(f.VERGIDAIRESI, '') as VergiDairesi,
                COALESCE(f.VERGINUMARASI, '') as VergiNumarasi,
                COALESCE(f.SEHIR, '') as Sehir,
                COALESCE(f.ILCE, '') as Ilce,
                COALESCE(f.ULKE, 'TÜRKİYE') as Ulke,
                COALESCE(f.PLAKA, '') as Plaka,
                COALESCE(f.ARKAPLAKA, '') as ArkaPlaka,
                COALESCE(f.SOFORADI, '') as SoforAdi,
                COALESCE(f.SOFORSOYADI, '') as SoforSoyadi,
                COALESCE(f.SOFORTC, '') as SoforTc,
                COALESCE(f.SOFORTELEFON, '') as SoforTelefon,
                COALESCE(f.TASIYICIFIRMA, '') as TasiyiciFirma,
                COALESCE(f.TASIYICIFIRMA_VKN, '') as TasiyiciFirmaVkn,
                COALESCE(f.DEPOKODU, 9) as DepoKodu,
                COALESCE(f.DEPOADI, 'ANA DEPO') as DepoAdi,
                COALESCE(f.ACIKLAMA, '') as Aciklama,
                COALESCE(f.SIPARISNO, '') as SiparisNo,
                COALESCE(NULLIF(f.TOPLAMPALET, 0), SUM(d.PALET), 0) as ToplamPalet,
                COALESCE(NULLIF(f.TOPLAMKASA, 0), SUM(d.KASA), 0) as ToplamKasa,
                COALESCE(NULLIF(f.TOPLAMMIKTAR, 0), SUM(d.MIKTAR), 0) as ToplamMiktar,
                COALESCE(f.TOPLAMFIRE, 0) as ToplamFire,
                COALESCE(NULLIF(f.FISTUT, 0), SUM(d.TUTAR), 0) as FisTut,
                COALESCE(f.ISKTUT, 0) as IskTut,
                COALESCE(NULLIF(f.KDVTUT, 0), SUM(d.KDVTUT), 0) as KdvTut,
                COALESCE(NULLIF(f.GENTUT, 0), SUM(d.TUTAR) + SUM(d.KDVTUT), 0) as GenTut,
                COALESCE(f.DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(f.DOVIZTUTAR, 0) as DovizTutar,
                f.FATNO as FatNo,
                COALESCE(f.KAPANDI, '') as Kapandi,
                COALESCE(f.USER, '') as User
            FROM irsstkfis f
            LEFT JOIN irsstkdet d ON f.FISNO = d.FISNO AND f.FISTUR = d.FISTUR AND f.SIRKOD = d.SIRKOD
            WHERE f.SIRKOD = @sirkod 
              AND f.FISTUR = 220
              AND COALESCE(f.FISTAR, f.IRSTAR) >= @start AND COALESCE(f.FISTAR, f.IRSTAR) <= @end
              {faturaCondition}
              {searchClause}
            GROUP BY f.SIRANO
            ORDER BY COALESCE(f.FISTAR, f.IRSTAR) DESC, f.FISNO DESC
            LIMIT 1000;";

        var prms = new
        {
            sirkod,
            start = start.Date,
            end = end.Date,
            searchPattern = $"%{search.Trim()}%"
        };

        var rows = await conn.QueryAsync<IrsaliyeStkDto>(sql, prms);
        return rows.ToList();
    }

    public async Task<List<IrsaliyeDetayStkDto>> GetIrsaliyeDetayAsync(double fisNo, double fisTur = 222, string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                SIRANO as SiraNo,
                COALESCE(SIRKOD, '1') as Sirkod,
                COALESCE(FISTUR, 222) as FisTur,
                COALESCE(FISNO, 0) as FisNo,
                COALESCE(IRSNO, 0) as IrsNo,
                COALESCE(SIRA, 1) as Sira,
                COALESCE(MALKODU, '') as MalKodu,
                COALESCE(MALADI, '') as MalAdi,
                COALESCE(BIRIMI, 'KG') as Birimi,
                COALESCE(PALET, 0) as Palet,
                COALESCE(KASA, 0) as Kasa,
                COALESCE(BURUT, 0) as Burut,
                COALESCE(DARA, 0) as Dara,
                COALESCE(MIKTAR, 0) as Miktar,
                COALESCE(FIREORANI, 0) as FireOrani,
                COALESCE(FIRETOPLAMI, 0) as FireToplami,
                COALESCE(FIYATI, 0) as Fiyati,
                COALESCE(KDV, 1) as Kdv,
                COALESCE(KDVTUT, 0) as KdvTut,
                COALESCE(TUTAR, 0) as Tutar,
                COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(DOVIZFIYATI, 0) as DovizFiyati,
                COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                COALESCE(KUNYENO, '') as KunyeNo,
                COALESCE(BARKODU, 0) as Barkodu,
                COALESCE(PLAKA, '') as Plaka,
                COALESCE(ACIKLAMA, '') as Aciklama
            FROM irsstkdet
            WHERE SIRKOD = @sirkod 
              AND FISTUR = @fisTur 
              AND FISNO = @fisNo
            ORDER BY SIRA ASC, SIRANO ASC;";

        var rows = await conn.QueryAsync<IrsaliyeDetayStkDto>(sql, new { sirkod, fisTur, fisNo });
        return rows.ToList();
    }

    public async Task<double> GetNextFisNoAsync(double fisTur = 222, string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT COALESCE(MAX(FISNO), 0) + 1 
            FROM irsstkfis 
            WHERE SIRKOD = @sirkod AND FISTUR = @fisTur;";

        return await conn.ExecuteScalarAsync<double>(sql, new { sirkod, fisTur });
    }

    public async Task<double> SaveSatisIrsaliyesiAsync(IrsaliyeStkDto header, List<IrsaliyeDetayStkDto> lines)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            if (header.FisNo <= 0)
            {
                const string nextNoSql = @"
                    SELECT COALESCE(MAX(FISNO), 0) + 1 
                    FROM irsstkfis 
                    WHERE SIRKOD = @Sirkod AND FISTUR = @FisTur;";
                header.FisNo = await conn.ExecuteScalarAsync<double>(nextNoSql, new { header.Sirkod, header.FisTur }, trans);
                if (header.IrsNo <= 0) header.IrsNo = header.FisNo;
            }

            // Calculate Totals from Lines
            header.ToplamPalet = lines.Sum(l => l.Palet);
            header.ToplamKasa = lines.Sum(l => l.Kasa);
            header.ToplamMiktar = lines.Sum(l => l.Miktar);
            header.FisTut = lines.Sum(l => l.Tutar);
            header.KdvTut = lines.Sum(l => l.KdvTut);
            header.GenTut = header.FisTut + header.KdvTut - header.IskTut;

            if (header.SiraNo <= 0)
            {
                // INSERT HEADER
                const string insertHdrSql = @"
                    INSERT INTO irsstkfis (
                        SIRKOD, FISTUR, FISNO, IRSNO, GIBIRSNO, FISTAR, IRSTAR, SAAT,
                        FIRKOD, FIRMAADI, FIRMAADRESI, VERGIDAIRESI, VERGINUMARASI, SEHIR, ILCE, ULKE,
                        PLAKA, ARKAPLAKA, SOFORADI, SOFORSOYADI, SOFORTC, SOFORTELEFON, TASIYICIFIRMA, TASIYICIFIRMA_VKN,
                        DEPOKODU, DEPOADI, ACIKLAMA, SIPARISNO,
                        TOPLAMPALET, TOPLAMKASA, TOPLAMMIKTAR, TOPLAMFIRE,
                        FISTUT, ISKTUT, KDVTUT, GENTUT, DOVIZTURU, DOVIZTUTAR,
                        FATNO, KAPANDI, USER
                    ) VALUES (
                        @Sirkod, @FisTur, @FisNo, @IrsNo, @GibIrsNo, @FisTar, @IrsTar, @Saat,
                        @FirKod, @FirmaAdi, @FirmaAdresi, @VergiDairesi, @VergiNumarasi, @Sehir, @Ilce, @Ulke,
                        @Plaka, @ArkaPlaka, @SoforAdi, @SoforSoyadi, @SoforTc, @SoforTelefon, @TasiyiciFirma, @TasiyiciFirmaVkn,
                        @DepoKodu, @DepoAdi, @Aciklama, @SiparisNo,
                        @ToplamPalet, @ToplamKasa, @ToplamMiktar, @ToplamFire,
                        @FisTut, @IskTut, @KdvTut, @GenTut, @DovizTuru, @DovizTutar,
                        @FatNo, @Kapandi, @User
                    );
                    SELECT LAST_INSERT_ID();";

                header.SiraNo = await conn.ExecuteScalarAsync<double>(insertHdrSql, header, trans);
            }
            else
            {
                // UPDATE HEADER
                const string updateHdrSql = @"
                    UPDATE irsstkfis SET
                        GIBIRSNO = @GibIrsNo,
                        FISTAR = @FisTar,
                        IRSTAR = @IrsTar,
                        SAAT = @Saat,
                        FIRKOD = @FirKod,
                        FIRMAADI = @FirmaAdi,
                        FIRMAADRESI = @FirmaAdresi,
                        VERGIDAIRESI = @VergiDairesi,
                        VERGINUMARASI = @VergiNumarasi,
                        SEHIR = @Sehir,
                        ILCE = @Ilce,
                        ULKE = @Ulke,
                        PLAKA = @Plaka,
                        ARKAPLAKA = @ArkaPlaka,
                        SOFORADI = @SoforAdi,
                        SOFORSOYADI = @SoforSoyadi,
                        SOFORTC = @SoforTc,
                        SOFORTELEFON = @SoforTelefon,
                        TASIYICIFIRMA = @TasiyiciFirma,
                        TASIYICIFIRMA_VKN = @TasiyiciFirmaVkn,
                        DEPOKODU = @DepoKodu,
                        DEPOADI = @DepoAdi,
                        ACIKLAMA = @Aciklama,
                        SIPARISNO = @SiparisNo,
                        TOPLAMPALET = @ToplamPalet,
                        TOPLAMKASA = @ToplamKasa,
                        TOPLAMMIKTAR = @ToplamMiktar,
                        TOPLAMFIRE = @ToplamFire,
                        FISTUT = @FisTut,
                        ISKTUT = @IskTut,
                        KDVTUT = @KdvTut,
                        GENTUT = @GenTut,
                        DOVIZTURU = @DovizTuru,
                        DOVIZTUTAR = @DovizTutar,
                        FATNO = @FatNo,
                        KAPANDI = @Kapandi,
                        USER = @User
                    WHERE SIRANO = @SiraNo;";

                await conn.ExecuteAsync(updateHdrSql, header, trans);

                // Delete existing lines to re-insert cleanly
                const string deleteLinesSql = @"
                    DELETE FROM irsstkdet 
                    WHERE SIRKOD = @Sirkod AND FISTUR = @FisTur AND FISNO = @FisNo;";
                await conn.ExecuteAsync(deleteLinesSql, new { header.Sirkod, header.FisTur, header.FisNo }, trans);
            }

            // INSERT LINES
            const string insertLineSql = @"
                INSERT INTO irsstkdet (
                    SIRKOD, FISTUR, FISNO, IRSNO, SIRA,
                    MALKODU, MALADI, BIRIMI,
                    PALET, KASA, BURUT, DARA, MIKTAR, FIREORANI, FIRETOPLAMI,
                    FIYATI, KDV, KDVTUT, TUTAR,
                    DOVIZTURU, DOVIZFIYATI, DOVIZTUTAR,
                    KUNYENO, BARKODU, PLAKA, ACIKLAMA
                ) VALUES (
                    @Sirkod, @FisTur, @FisNo, @IrsNo, @Sira,
                    @MalKodu, @MalAdi, @Birimi,
                    @Palet, @Kasa, @Burut, @Dara, @Miktar, @FireOrani, @FireToplami,
                    @Fiyati, @Kdv, @KdvTut, @Tutar,
                    @DovizTuru, @DovizFiyati, @DovizTutar,
                    @KunyeNo, @Barkodu, @Plaka, @Aciklama
                );";

            int lineIndex = 1;
            foreach (var line in lines)
            {
                line.Sirkod = header.Sirkod;
                line.FisTur = header.FisTur;
                line.FisNo = header.FisNo;
                line.IrsNo = header.IrsNo;
                line.Sira = lineIndex++;
                line.Plaka = header.Plaka;

                await conn.ExecuteAsync(insertLineSql, line, trans);
            }

            trans.Commit();
            return header.FisNo;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public Task<double> SaveAlisIrsaliyesiAsync(IrsaliyeStkDto header, List<IrsaliyeDetayStkDto> lines)
    {
        header.FisTur = 220;
        return SaveSatisIrsaliyesiAsync(header, lines);
    }

    public Task<double> SaveIrsaliyeAsync(IrsaliyeStkDto header, List<IrsaliyeDetayStkDto> lines)
    {
        return SaveSatisIrsaliyesiAsync(header, lines);
    }

    public async Task DeleteIrsaliyeAsync(double siraNo, double fisNo, double fisTur = 222, string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            const string delDetSql = "DELETE FROM irsstkdet WHERE SIRKOD = @sirkod AND FISTUR = @fisTur AND FISNO = @fisNo;";
            await conn.ExecuteAsync(delDetSql, new { sirkod, fisTur, fisNo }, trans);

            const string delHdrSql = "DELETE FROM irsstkfis WHERE SIRANO = @siraNo;";
            await conn.ExecuteAsync(delHdrSql, new { siraNo }, trans);

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<bool> FaturayaDonusturAsync(double fisNo, double faturaNo, double fisTur = 222, string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            const string updateHdr = @"
                UPDATE irsstkfis 
                SET FATNO = @faturaNo, KAPANDI = 'E' 
                WHERE SIRKOD = @sirkod AND FISTUR = @fisTur AND FISNO = @fisNo;";
            await conn.ExecuteAsync(updateHdr, new { sirkod, fisTur, fisNo, faturaNo }, trans);

            const string updateDet = @"
                UPDATE irsstkdet 
                SET FATNO = @faturaNo 
                WHERE SIRKOD = @sirkod AND FISTUR = @fisTur AND FISNO = @fisNo;";
            await conn.ExecuteAsync(updateDet, new { sirkod, fisTur, fisNo, faturaNo }, trans);

            trans.Commit();
            return true;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<List<StokKartDto>> SearchStokKartlariAsync(string search = "", string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT 
                SIRA as Sira,
                COALESCE(KODU, '') as Kodu,
                COALESCE(ADI, '') as Adi,
                COALESCE(GRUPKODUADI, '') as GrupKoduAdi,
                COALESCE(BIRIM, 'KG') as Birim,
                COALESCE(MEVCUT, 0) as Mevcut,
                COALESCE(ALISFIYAT, 0) as AlisFiyat,
                COALESCE(SFIYAT1, 0) as SFiyat1,
                COALESCE(KDV, 1) as Kdv
            FROM stok
            WHERE SIRKOD = @sirkod AND LENGTH(TRIM(COALESCE(ADI, ''))) > 2 AND ADI NOT LIKE '***%' AND ADI NOT LIKE '////%'";

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (KODU LIKE @pat OR ADI LIKE @pat OR GRUPKODUADI LIKE @pat)";
        }

        sql += " ORDER BY CASE WHEN MEVCUT > 0 THEN 0 ELSE 1 END, ADI ASC LIMIT 200;";

        var rows = await conn.QueryAsync<StokKartDto>(sql, new { sirkod, pat = $"%{search.Trim()}%" });
        return rows.ToList();
    }
}
