using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

public class YuklemeProgramiService
{
    public async Task<List<YuklemeProgramiDto>> GetAllAsync(string search = "")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = " WHERE (BASLIK LIKE @p OR PROGRAM_NO LIKE @p OR HAZIRLAYAN LIKE @p)";
        }

        string sql = $@"
            SELECT 
                ID,
                PROGRAM_NO as ProgramNo,
                TARIH,
                BASLIK,
                COALESCE(SUTUN_SAYISI, 1) as SutunSayisi,
                COALESCE(SATIR_SAYISI, 1) as SatirSayisi,
                TOPLAM_PALET as ToplamPalet,
                HAZIRLAYAN,
                ONAYLAYAN,
                COALESCE(FORKLIFT_OPERATOR, '') as ForkliftOperator,
                DURUM,
                NOTLAR,
                GUNCELLEME_TARIHI as GuncellemeTarihi
            FROM erp.yukleme_programi_ana
            {searchClause}
            ORDER BY TARIH DESC, ID DESC;";

        var list = (await conn.QueryAsync<YuklemeProgramiDto>(sql, new { p = $"%{search}%" })).ToList();
        return list;
    }

    public async Task<YuklemeProgramiDto?> GetByIdAsync(int id)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sqlProg = @"
            SELECT 
                ID,
                PROGRAM_NO as ProgramNo,
                TARIH,
                BASLIK,
                COALESCE(SUTUN_SAYISI, 1) as SutunSayisi,
                COALESCE(SATIR_SAYISI, 1) as SatirSayisi,
                TOPLAM_PALET as ToplamPalet,
                HAZIRLAYAN,
                ONAYLAYAN,
                COALESCE(FORKLIFT_OPERATOR, '') as ForkliftOperator,
                DURUM,
                NOTLAR,
                GUNCELLEME_TARIHI as GuncellemeTarihi
            FROM erp.yukleme_programi_ana
            WHERE ID = @id;";

        var prog = await conn.QueryFirstOrDefaultAsync<YuklemeProgramiDto>(sqlProg, new { id });
        if (prog == null) return null;

        const string sqlSutun = @"
            SELECT 
                ID,
                PROGRAM_ID as ProgramId,
                COALESCE(SUTUN_NO, 1) as SutunNo,
                COALESCE(SATIR_NO, 1) as SatirNo,
                BASLIK,
                URUNLER_METIN as UrunlerMetin,
                TALIMATLAR_METIN as TalimatlarMetin,
                PARTILER_METIN as PartilerMetin,
                TARIHLER_METIN as TarihlerMetin,
                TOPLAM_PALET as ToplamPalet
            FROM erp.yukleme_programi_sutun
            WHERE PROGRAM_ID = @id
            ORDER BY SATIR_NO ASC, SUTUN_NO ASC, ID ASC;";

        var sutunlar = (await conn.QueryAsync<YuklemeSutunDto>(sqlSutun, new { id })).ToList();
        prog.Sutunlar = sutunlar;
        return prog;
    }

    public async Task<int> SaveAsync(YuklemeProgramiDto prog)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        using var tran = await conn.BeginTransactionAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(prog.ProgramNo))
            {
                prog.ProgramNo = await GenerateNextProgramNoAsync();
            }

            // Calculate total pallets across all columns
            prog.ToplamPalet = prog.Sutunlar.Sum(s => s.ToplamPalet);

            int progId;
            if (prog.Id == 0)
            {
                const string sqlInsert = @"
                    INSERT INTO erp.yukleme_programi_ana 
                        (PROGRAM_NO, TARIH, BASLIK, SUTUN_SAYISI, SATIR_SAYISI, TOPLAM_PALET, HAZIRLAYAN, ONAYLAYAN, FORKLIFT_OPERATOR, DURUM, NOTLAR)
                    VALUES 
                        (@ProgramNo, @Tarih, @Baslik, @SutunSayisi, @SatirSayisi, @ToplamPalet, @Hazirlayan, @Onaylayan, @ForkliftOperator, @Durum, @Notlar);
                    SELECT LAST_INSERT_ID();";

                progId = await conn.ExecuteScalarAsync<int>(sqlInsert, prog, tran);
                prog.Id = progId;
            }
            else
            {
                progId = prog.Id;
                const string sqlUpdate = @"
                    UPDATE erp.yukleme_programi_ana SET
                        PROGRAM_NO = @ProgramNo,
                        TARIH = @Tarih,
                        BASLIK = @Baslik,
                        SUTUN_SAYISI = @SutunSayisi,
                        SATIR_SAYISI = @SatirSayisi,
                        TOPLAM_PALET = @ToplamPalet,
                        HAZIRLAYAN = @Hazirlayan,
                        ONAYLAYAN = @Onaylayan,
                        FORKLIFT_OPERATOR = @ForkliftOperator,
                        DURUM = @Durum,
                        NOTLAR = @Notlar
                    WHERE ID = @Id;";

                await conn.ExecuteAsync(sqlUpdate, prog, tran);

                // Delete old columns
                await conn.ExecuteAsync("DELETE FROM erp.yukleme_programi_sutun WHERE PROGRAM_ID = @progId;", new { progId }, tran);
            }

            // Insert columns
            const string sqlInsSutun = @"
                INSERT INTO erp.yukleme_programi_sutun
                    (PROGRAM_ID, SUTUN_NO, SATIR_NO, BASLIK, URUNLER_METIN, TALIMATLAR_METIN, PARTILER_METIN, TARIHLER_METIN, TOPLAM_PALET)
                VALUES
                    (@ProgramId, @SutunNo, @SatirNo, @Baslik, @UrunlerMetin, @TalimatlarMetin, @PartilerMetin, @TarihlerMetin, @ToplamPalet);";

            foreach (var s in prog.Sutunlar)
            {
                s.ProgramId = progId;
                await conn.ExecuteAsync(sqlInsSutun, s, tran);
            }

            await tran.CommitAsync();
            return progId;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        int affected = await conn.ExecuteAsync("DELETE FROM erp.yukleme_programi_ana WHERE ID = @id;", new { id });
        return affected > 0;
    }

    public async Task<string> GenerateNextProgramNoAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string prefix = $"YP-{DateTime.Today:yyyy-MMdd}-";
        const string sql = "SELECT COUNT(*) FROM erp.yukleme_programi_ana WHERE PROGRAM_NO LIKE @p;";
        int count = await conn.ExecuteScalarAsync<int>(sql, new { p = $"{prefix}%" });
        return $"{prefix}{(count + 1):D2}";
    }

    // ================= FORKLİFT OPERATÖRLERİ CRUD =================

    public async Task<List<ForkliftOperatorDto>> GetForkliftOperatorlerAsync(bool onlyActive = false)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string where = onlyActive ? "WHERE DURUM = 'Aktif'" : "";
        string sql = $@"
            SELECT 
                ID,
                AD_SOYAD as AdSoyad,
                COALESCE(TELEFON, '') as Telefon,
                COALESCE(EHLIYET_NO, '') as EhliyetNo,
                COALESCE(FORKLIFT_KODU, '') as ForkliftKodu,
                COALESCE(VARDIYA, '1. Vardiya (Gündüz)') as Vardiya,
                COALESCE(DURUM, 'Aktif') as Durum,
                COALESCE(ACIKLAMA, '') as Aciklama,
                COALESCE(KAYIT_TARIHI, NOW()) as KayitTarihi
            FROM erp.forklift_operatorler
            {where}
            ORDER BY AD_SOYAD ASC;";

        var list = (await conn.QueryAsync<ForkliftOperatorDto>(sql)).ToList();
        return list;
    }

    public async Task<int> SaveForkliftOperatorAsync(ForkliftOperatorDto op)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        if (op.Id == 0)
        {
            const string sql = @"
                INSERT INTO erp.forklift_operatorler 
                    (AD_SOYAD, TELEFON, EHLIYET_NO, FORKLIFT_KODU, VARDIYA, DURUM, ACIKLAMA, KAYIT_TARIHI)
                VALUES 
                    (@AdSoyad, @Telefon, @EhliyetNo, @ForkliftKodu, @Vardiya, @Durum, @Aciklama, NOW());
                SELECT LAST_INSERT_ID();";
            int newId = await conn.ExecuteScalarAsync<int>(sql, op);
            op.Id = newId;
            return newId;
        }
        else
        {
            const string sql = @"
                UPDATE erp.forklift_operatorler SET
                    AD_SOYAD = @AdSoyad,
                    TELEFON = @Telefon,
                    EHLIYET_NO = @EhliyetNo,
                    FORKLIFT_KODU = @ForkliftKodu,
                    VARDIYA = @Vardiya,
                    DURUM = @Durum,
                    ACIKLAMA = @Aciklama
                WHERE ID = @Id;";
            await conn.ExecuteAsync(sql, op);
            return op.Id;
        }
    }

    public async Task<bool> DeleteForkliftOperatorAsync(int id)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        int affected = await conn.ExecuteAsync("DELETE FROM erp.forklift_operatorler WHERE ID = @id;", new { id });
        return affected > 0;
    }
}
