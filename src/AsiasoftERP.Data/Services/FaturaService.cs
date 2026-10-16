using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

public class FaturaService
{
    public async Task<List<FaturaStkDto>> GetFaturalarAsync(FaturaTip tip, DateTime start, DateTime end, string search = "", string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string typeCondition = tip switch
        {
            FaturaTip.SatisFaturasi => "(FISTUR IN (20, 120, 224, 226) OR TIP LIKE '%SATIŞ%')",
            FaturaTip.AlisFaturasi => "(FISTUR IN (10, 110) OR TIP = 'ALIŞ')",
            FaturaTip.IhracatFaturasi => "(FISTUR IN (750, 752, 754) OR TIP LIKE '%İHRACAT%' OR TIP LIKE '%YURT DIŞI%')",
            _ => "1=1"
        };

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = " AND (FIRMAADI LIKE @searchPattern OR GIBFATURANO LIKE @searchPattern OR CAST(FISNO AS CHAR) LIKE @searchPattern)";
        }

        string sql = $@"
            SELECT 
                SIRAIDNO as SiraIdNo,
                COALESCE(FISNO, 0) as FisNo,
                COALESCE(GIBFATURANO, '') as GibFaturaNo,
                FISTAR as FisTar,
                VADETAR as VadeTar,
                COALESCE(FIRKOD, 0) as FirKod,
                COALESCE(FIRMAADI, '') as FirmaAdi,
                COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(FISTUT, 0) as FisTut,
                COALESCE(ISKTUT, 0) as IskTut,
                COALESCE(KDVTUT, 0) as KdvTut,
                COALESCE(GENTUT, 0) as GenTut,
                COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                COALESCE(TIP, '') as Tip,
                COALESCE(ACIKLAMA, '') as Aciklama
            FROM stkfis
            WHERE SIRKOD = @sirkod 
              AND FISTAR >= @start AND FISTAR <= @end
              AND {typeCondition}
              {searchClause}
            ORDER BY FISTAR DESC, SIRAIDNO DESC
            LIMIT 500;";

        var prms = new
        {
            sirkod,
            start = start.Date,
            end = end.Date,
            searchPattern = $"%{search}%"
        };

        var result = await conn.QueryAsync<FaturaStkDto>(sql, prms);
        return result.ToList();
    }

    public async Task<List<FaturaDetayStkDto>> GetFaturaDetayAsync(double fisNo, string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COALESCE(SIRA, 0) as Sira,
                COALESCE(MALKODU, '') as MalKodu,
                COALESCE(MALADI, '') as MalAdi,
                COALESCE(BIRIMI, 'KG') as Birimi,
                COALESCE(MIKTAR, 0) as Miktar,
                COALESCE(FIYATI, 0) as Fiyati,
                COALESCE(KDV, 0) as Kdv,
                COALESCE(KDVTUT, 0) as KdvTut,
                COALESCE(TUTAR, 0) as Tutar,
                COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(DOVIZFIYATI, 0) as DovizFiyati,
                COALESCE(DOVIZTUTAR, 0) as DovizTutar
            FROM stkdet
            WHERE FISNO = @fisNo AND SIRKOD = @sirkod
            ORDER BY SIRA ASC;";

        var result = await conn.QueryAsync<FaturaDetayStkDto>(sql, new { fisNo, sirkod });
        return result.ToList();
    }
}
