using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Data service providing live queries and updates for Ödeme Talep İşlemleri
/// Queries MariaDB table 'erp.bankaodemedetay'
/// </summary>
public class OdemeTalepService
{
    /// <summary>
    /// Retrieves payment requests matching the selected date range and tab filter
    /// </summary>
    public async Task<List<OdemeTalepDto>> GetOdemeTalepleriAsync(
        DateTime fromDate, 
        DateTime toDate, 
        string tabName, 
        string search = "", 
        string? sirKod = null)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("FromDate", fromDate.Date);
            parameters.Add("ToDate", toDate.Date);

            string whereClause = "WHERE FISTAR BETWEEN @FromDate AND @ToDate";

            // Only include real payment requests (sender is specified and positive amount)
            whereClause += " AND (GONDEREN IS NOT NULL OR KASAADI IS NOT NULL) AND (COALESCE(TALEP_TUTAR, 0) > 0 OR COALESCE(TUTAR, 0) > 0)";

            // Filter by company if specified
            if (!string.IsNullOrWhiteSpace(sirKod) && sirKod != "Tümü")
            {
                whereClause += " AND SIRKOD = @SirKod";
                parameters.Add("SirKod", sirKod);
            }

            // Tab-based status filter
            switch (tabName)
            {
                case "Devam Eden Talepler":
                    whereClause += " AND (COALESCE(DURUM, '') IN ('Devam Ediyor', 'Ongoing', '') OR COALESCE(DURUM, '') NOT IN ('Aktarildi', 'Onaylandı', 'RET'))";
                    break;
                case "Onaylanan Talepler":
                    whereClause += " AND (COALESCE(DURUM, '') IN ('Onaylandı', 'Aktarildi', 'Ödendi'))";
                    break;
                case "Ret Edilen Talepler":
                    whereClause += " AND (COALESCE(DURUM, '') = 'RET' OR COALESCE(DURUM, '') LIKE '%RET%')";
                    break;
                case "Genel Talep Listesi":
                default:
                    // No status restriction
                    break;
            }

            // Quick search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                whereClause += @" AND (
                    FIRADI LIKE @Search OR 
                    CAST(FISNO AS CHAR) LIKE @Search OR 
                    BELGENO LIKE @Search OR 
                    FATURANO LIKE @Search OR 
                    YETKIKODU LIKE @Search OR 
                    TURU LIKE @Search OR 
                    KASAADI LIKE @Search OR 
                    ACIKLAMA LIKE @Search
                )";
                parameters.Add("Search", $"%{search.Trim()}%");
            }

            string sql = $@"
                SELECT 
                    IDNO as IdNo,
                    SIRKOD as SirKod,
                    COALESCE(YETKIKODU, '') as YetkiKodu,
                    COALESCE(KASAADI, SIRKETADI, 'TEK ASYA') as GrupSirket,
                    COALESCE(GONDEREN, 'TEK ASYA') as GonderenHesap,
                    COALESCE(TURU, '') as Turu,
                    COALESCE(FISNO, 0) as FisNo,
                    CASE WHEN FISTAR IS NOT NULL AND FISTAR > '1970-01-01' THEN FISTAR ELSE NULL END as FisTar,
                    CASE WHEN ISLEMTARIHI IS NOT NULL AND ISLEMTARIHI > '1970-01-01' THEN ISLEMTARIHI ELSE NULL END as IslemTarihi,
                    COALESCE(C_KASAADI, '') as KasaAdi,
                    COALESCE(FIRKOD, 0) as FirKod,
                    COALESCE(FIRADI, '') as FirAdi,
                    COALESCE(BELGENO, '') as BelgeNo,
                    COALESCE(FATURANO, '') as FaturaNo,
                    COALESCE(BAGKUR_VARMI, 'HAYIR') as BagkurVarmi,
                    CAST(CASE WHEN COALESCE(TALEP_TUTAR, 0) > 0 THEN TALEP_TUTAR ELSE COALESCE(TUTAR, 0) END AS DECIMAL(18,2)) as TalepTutar,
                    CAST(COALESCE(BAGKUR_ORANI, 0) AS DECIMAL(18,2)) as BagkurOrani,
                    CAST(COALESCE(BAGKUR_TUTAR, 0) AS DECIMAL(18,2)) as BagkurTutar,
                    COALESCE(DURUM, '') as Durum,
                    COALESCE(ACIKLAMA, '') as Aciklama,
                    COALESCE(BANKAIBAN, '') as BankaIban,
                    COALESCE(KULLANICI, '') as Kullanici,
                    COALESCE(ONAYKULLANICI, '') as OnayKullanici,
                    CASE WHEN ONAYTARIHI IS NOT NULL AND ONAYTARIHI > '1970-01-01' THEN ONAYTARIHI ELSE NULL END as OnayTarihi,
                    COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                    CAST(COALESCE(DOVIZTUTAR, 0) AS DECIMAL(18,2)) as DovizTutar
                FROM bankaodemedetay
                {whereClause}
                ORDER BY FISNO ASC, IDNO ASC;";

            var list = (await conn.QueryAsync<OdemeTalepDto>(sql, parameters)).ToList();

            // Assign sequential Sıra numbers
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Sira = i + 1;
            }

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OdemeTalepService] Error: {ex}");
            return new List<OdemeTalepDto>();
        }
    }

    /// <summary>
    /// Computes summary statistics for the current query
    /// </summary>
    public OdemeTalepSummary ComputeSummary(List<OdemeTalepDto> items)
    {
        var summary = new OdemeTalepSummary
        {
            ToplamKayit = items.Count,
            ToplamTalepTutari = items.Sum(x => x.TalepTutar),
            ToplamBagkurTutari = items.Sum(x => x.BagkurTutar),
            OnaylananToplam = items.Where(x => x.Durum == "Onaylandı" || x.Durum == "Aktarildi" || x.Durum == "Ödendi").Sum(x => x.TalepTutar),
            RetToplam = items.Where(x => x.Durum.ToUpperInvariant().Contains("RET")).Sum(x => x.TalepTutar),
            DevamEdenToplam = items.Where(x => x.Durum == "Devam Ediyor" || x.Durum == "Ongoing" || string.IsNullOrWhiteSpace(x.Durum)).Sum(x => x.TalepTutar)
        };
        return summary;
    }

    /// <summary>
    /// Retrieves comparative summary statistics grouped by Branch (Şube) and Payment Type (Türü) for Tab 5
    /// </summary>
    public async Task<List<OdemeTalepKarsilastirmaDto>> GetKarsilastirmaAsync(
        DateTime fromDate, 
        DateTime toDate, 
        string? sirKod = null)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("FromDate", fromDate.Date);
            parameters.Add("ToDate", toDate.Date);

            string whereClause = "WHERE FISTAR BETWEEN @FromDate AND @ToDate AND (GONDEREN IS NOT NULL OR KASAADI IS NOT NULL) AND (COALESCE(TALEP_TUTAR, 0) > 0 OR COALESCE(TUTAR, 0) > 0)";

            if (!string.IsNullOrWhiteSpace(sirKod) && sirKod != "Tümü")
            {
                whereClause += " AND SIRKOD = @SirKod";
                parameters.Add("SirKod", sirKod);
            }

            string sql = $@"
                SELECT 
                    COALESCE(YETKIKODU, 'MERKEZ') as Sube,
                    COALESCE(TURU, 'DİĞER') as Turu,
                    COUNT(*) as TalepAdedi,
                    CAST(SUM(CASE WHEN COALESCE(TALEP_TUTAR, 0) > 0 THEN TALEP_TUTAR ELSE COALESCE(TUTAR, 0) END) AS DECIMAL(18,2)) as ToplamTalepTutari,
                    CAST(SUM(CASE WHEN COALESCE(DURUM, '') IN ('Onaylandı', 'Aktarildi', 'Ödendi') THEN (CASE WHEN COALESCE(TALEP_TUTAR, 0) > 0 THEN TALEP_TUTAR ELSE COALESCE(TUTAR, 0) END) ELSE 0 END) AS DECIMAL(18,2)) as OnaylananTutar,
                    CAST(SUM(CASE WHEN COALESCE(DURUM, '') LIKE '%RET%' THEN (CASE WHEN COALESCE(TALEP_TUTAR, 0) > 0 THEN TALEP_TUTAR ELSE COALESCE(TUTAR, 0) END) ELSE 0 END) AS DECIMAL(18,2)) as RetEdilenTutar,
                    CAST(SUM(CASE WHEN COALESCE(DURUM, '') IN ('Devam Ediyor', 'Ongoing', '') THEN (CASE WHEN COALESCE(TALEP_TUTAR, 0) > 0 THEN TALEP_TUTAR ELSE COALESCE(TUTAR, 0) END) ELSE 0 END) AS DECIMAL(18,2)) as DevamEdenTutar
                FROM bankaodemedetay
                {whereClause}
                GROUP BY COALESCE(YETKIKODU, 'MERKEZ'), COALESCE(TURU, 'DİĞER')
                ORDER BY ToplamTalepTutari DESC;";

            var list = (await conn.QueryAsync<OdemeTalepKarsilastirmaDto>(sql, parameters)).ToList();
            return list;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OdemeTalepService] Error: {ex.Message}");
            return new List<OdemeTalepKarsilastirmaDto>();
        }
    }

    /// <summary>
    /// Updates the status of an existing payment request (e.g. Onayla, Ret, Devam Ediyor)
    /// </summary>
    public async Task<bool> UpdateDurumAsync(double idNo, string newDurum, string currentUser)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE bankaodemedetay 
                SET DURUM = @NewDurum, 
                    ONAYKULLANICI = @CurrentUser, 
                    ONAYTARIHI = NOW() 
                WHERE IDNO = @IdNo;";

            int affected = await conn.ExecuteAsync(sql, new { IdNo = idNo, NewDurum = newDurum, CurrentUser = currentUser });
            return affected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OdemeTalepService] Update Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves or updates a payment request row
    /// </summary>
    public async Task<bool> SaveOdemeTalepAsync(OdemeTalepDto item)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            if (item.IdNo > 0)
            {
                // Update
                const string updateSql = @"
                    UPDATE bankaodemedetay SET
                        YETKIKODU = @YetkiKodu,
                        TURU = @Turu,
                        FIRKOD = @FirKod,
                        FIRADI = @FirAdi,
                        TALEP_TUTAR = @TalepTutar,
                        BAGKUR_VARMI = @BagkurVarmi,
                        BAGKUR_ORANI = @BagkurOrani,
                        BAGKUR_TUTAR = @BagkurTutar,
                        ACIKLAMA = @Aciklama,
                        BANKAIBAN = @BankaIban,
                        C_KASAADI = @KasaAdi,
                        FATURANO = @FaturaNo,
                        DURUM = @Durum
                    WHERE IDNO = @IdNo;";

                int rows = await conn.ExecuteAsync(updateSql, item);
                return rows > 0;
            }
            else
            {
                // Insert new request
                const string insertSql = @"
                    INSERT INTO bankaodemedetay (
                        SIRKOD, FISNO, FISTAR, ISLEMTARIHI, YETKIKODU, KASAADI, GONDEREN,
                        TURU, FIRKOD, FIRADI, TALEP_TUTAR, BAGKUR_VARMI, BAGKUR_ORANI,
                        BAGKUR_TUTAR, ACIKLAMA, BANKAIBAN, C_KASAADI, FATURANO, DURUM, KULLANICI
                    ) VALUES (
                        @SirKod, @FisNo, @FisTar, NOW(), @YetkiKodu, @GrupSirket, @GonderenHesap,
                        @Turu, @FirKod, @FirAdi, @TalepTutar, @BagkurVarmi, @BagkurOrani,
                        @BagkurTutar, @Aciklama, @BankaIban, @KasaAdi, @FaturaNo, @Durum, @Kullanici
                    );";

                int rows = await conn.ExecuteAsync(insertSql, item);
                return rows > 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OdemeTalepService] Save Error: {ex.Message}");
            return false;
        }
    }
}
