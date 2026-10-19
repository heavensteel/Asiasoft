using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Alınan (Müşteri) ve Verilen (Tedarikçi) Sipariş Yönetimi Veritabanı Servisi
/// </summary>
public class SiparisService
{
    private static (string MasterTable, string DetailTable) GetTables(SiparisYonu yon) =>
        yon == SiparisYonu.Alinan
            ? ("alinansiparisana", "alinansiparisdetay")
            : ("verilensiparisana", "verilensiparisdetay");

    /// <summary>
    /// Sipariş listesini filtrelere göre getirir
    /// </summary>
    public async Task<List<SiparisDto>> GetSiparislerAsync(SiparisYonu yon, DateTime start, DateTime end, string durum = "TÜMÜ", string search = "")
    {
        var (masterTable, _) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var sql = $@"
            SELECT 
                COALESCE(IDNO, 0) AS IdNo,
                COALESCE(FISNO, 0) AS FisNo,
                COALESCE(FATNO, 0) AS FatNo,
                COALESCE(SIRKOD, '1') AS Sirkod,
                CASE WHEN TARIH IS NULL OR TARIH = '0000-00-00' THEN '2026-01-01' ELSE TARIH END AS Tarih,
                CASE WHEN ISLEMTARIHI IS NULL OR ISLEMTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE ISLEMTARIHI END AS IslemTarihi,
                CASE WHEN BASLAMATARIHI IS NULL OR BASLAMATARIHI = '0000-00-00' THEN NULL ELSE BASLAMATARIHI END AS BaslamaTarihi,
                CASE WHEN BITISTARIHI IS NULL OR BITISTARIHI = '0000-00-00' THEN NULL ELSE BITISTARIHI END AS BitisTarihi,
                COALESCE(DURUMU, 'SİPARİŞ ALINDI') AS Durumu,
                COALESCE(FIRKOD, '') AS Firkod,
                COALESCE(FIRMAADI, '') AS FirmaAdi,
                COALESCE(ADRESI, '') AS Adresi,
                COALESCE(SEHIR, '') AS Sehir,
                COALESCE(ULKE, '') AS Ulke,
                COALESCE(SATISSEKLI, 'DAP') AS SatisSekli,
                COALESCE(PARTINO, '') AS PartiNo,
                COALESCE(POZISYON, '1') AS Pozisyon,
                COALESCE(SIPARISIVEREN, '') AS SiparisiVeren,
                COALESCE(SIPARISIALAN, '') AS SiparisiAlan,
                COALESCE(PALETTOPLAM, 0) AS PaletToplam,
                COALESCE(KASATOPLAM, 0) AS KasaToplam,
                COALESCE(BURUTTOPLAM, 0) AS BurutToplam,
                COALESCE(NETTOPLAM, 0) AS NetToplam,
                COALESCE(TOPLAMTUTAR, 0) AS ToplamTutar,
                COALESCE(DOVIZTURU, 'EUR') AS DovizTuru,
                COALESCE(NAKLIYECI, '') AS Nakliyeci,
                COALESCE(ARACPLAKA, '') AS AracPlaka,
                COALESCE(ARACPLAKA2, '') AS AracPlaka2,
                COALESCE(SOFORADI, '') AS SoforAdi,
                COALESCE(SOFORTELEFON, '') AS SoforTelefon,
                COALESCE(FRIGODERECE, '+4°C') AS FrigoDerece,
                COALESCE(GUMRUK, '') AS Gumruk,
                COALESCE(GUMRUK_ADRES, '') AS GumrukAdres,
                COALESCE(BOSALTMA_ADRESI, '') AS BosaltmaAdresi,
                COALESCE(ACIKLAMA, '') AS Aciklama,
                COALESCE(KKULLANICI, '') AS Kullanici
            FROM {masterTable}
            WHERE TARIH IS NOT NULL AND TARIH != '0000-00-00' AND TARIH >= @StartDate AND TARIH <= @EndDate
        ";

        var parameters = new DynamicParameters();
        parameters.Add("StartDate", start.Date);
        parameters.Add("EndDate", end.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(durum) && durum != "TÜMÜ")
        {
            sql += " AND DURUMU = @Durum";
            parameters.Add("Durum", durum);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += @" AND (
                CAST(FISNO AS CHAR) LIKE @Search OR
                FIRMAADI LIKE @Search OR
                ULKE LIKE @Search OR
                SEHIR LIKE @Search OR
                ARACPLAKA LIKE @Search OR
                SOFORADI LIKE @Search
            )";
            parameters.Add("Search", $"%{search}%");
        }

        sql += " ORDER BY TARIH DESC, FISNO DESC;";

        var result = (await conn.QueryAsync<SiparisDto>(sql, parameters)).ToList();
        foreach (var r in result)
        {
            r.SiparisYonu = yon;
        }
        return result;
    }

    /// <summary>
    /// Sipariş KPI özet göstergelerini hesaplar
    /// </summary>
    public async Task<SiparisKpiDto> GetKpisAsync(SiparisYonu yon, DateTime start, DateTime end)
    {
        var (masterTable, _) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var sql = $@"
            SELECT 
                COUNT(*) AS ToplamSiparis,
                COALESCE(SUM(CASE WHEN DURUMU = 'SİPARİŞ ALINDI' OR DURUMU = '' OR DURUMU IS NULL THEN 1 ELSE 0 END), 0) AS BekleyenSiparis,
                COALESCE(SUM(CASE WHEN DURUMU = 'HAZIRLANIYOR' THEN 1 ELSE 0 END), 0) AS HazirlananSiparis,
                COALESCE(SUM(CASE WHEN DURUMU = 'YÜKLENDİ' OR DURUMU = 'SEVK EDİLDİ' OR DURUMU = 'TAMAMLANDI' THEN 1 ELSE 0 END), 0) AS SevkEdilenSiparis,
                COALESCE(SUM(PALETTOPLAM), 0) AS ToplamPalet,
                COALESCE(SUM(KASATOPLAM), 0) AS ToplamKasa,
                COALESCE(SUM(NETTOPLAM), 0) AS ToplamNetKg,
                COALESCE(SUM(CASE WHEN DOVIZTURU = 'EUR' THEN TOPLAMTUTAR ELSE 0 END), 0) AS ToplamEurTutar,
                COALESCE(SUM(CASE WHEN DOVIZTURU = 'USD' THEN TOPLAMTUTAR ELSE 0 END), 0) AS ToplamUsdTutar,
                COALESCE(SUM(CASE WHEN DOVIZTURU = 'TL' OR DOVIZTURU = 'TRY' THEN TOPLAMTUTAR ELSE 0 END), 0) AS ToplamTlTutar
            FROM {masterTable}
            WHERE TARIH IS NOT NULL AND TARIH != '0000-00-00' AND TARIH >= @StartDate AND TARIH <= @EndDate;
        ";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            StartDate = start.Date,
            EndDate = end.Date.AddDays(1).AddTicks(-1)
        });

        var kpi = new SiparisKpiDto();
        if (row != null)
        {
            kpi.ToplamSiparis = Convert.ToInt32(row.ToplamSiparis ?? 0);
            kpi.BekleyenSiparis = Convert.ToInt32(row.BekleyenSiparis ?? 0);
            kpi.HazirlananSiparis = Convert.ToInt32(row.HazirlananSiparis ?? 0);
            kpi.SevkEdilenSiparis = Convert.ToInt32(row.SevkEdilenSiparis ?? 0);
            kpi.ToplamPalet = Convert.ToDouble(row.ToplamPalet ?? 0);
            kpi.ToplamKasa = Convert.ToDouble(row.ToplamKasa ?? 0);
            kpi.ToplamNetKg = Convert.ToDouble(row.ToplamNetKg ?? 0);
            kpi.ToplamEurTutar = Convert.ToDouble(row.ToplamEurTutar ?? 0);
            kpi.ToplamUsdTutar = Convert.ToDouble(row.ToplamUsdTutar ?? 0);
            kpi.ToplamTlTutar = Convert.ToDouble(row.ToplamTlTutar ?? 0);
        }

        return kpi;
    }

    /// <summary>
    /// Sipariş kalem detaylarını getirir
    /// </summary>
    public async Task<List<SiparisDetayDto>> GetSiparisDetaylarAsync(SiparisYonu yon, double fisNo)
    {
        var (_, detailTable) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var sql = $@"
            SELECT 
                COALESCE(IDNO, 0) AS IdNo,
                COALESCE(FISNO, 0) AS FisNo,
                COALESCE(FATNO, 0) AS FatNo,
                COALESCE(SIRA, 0) AS Sira,
                COALESCE(SIRKOD, '1') AS Sirkod,
                CASE WHEN TARIH IS NULL OR TARIH = '0000-00-00' THEN '2026-01-01' ELSE TARIH END AS Tarih,
                COALESCE(MALKODU, '') AS MalKodu,
                COALESCE(MALADI, '') AS MalAdi,
                COALESCE(BOY, '') AS Boy,
                COALESCE(KALITE, '1.KALİTE') AS Kalite,
                COALESCE(KUTUKODU, '') AS KutuKodu,
                COALESCE(KUTUADI, '') AS KutuAdi,
                COALESCE(KUTUDARA, 0) AS KutuDara,
                COALESCE(KUTUKG, 0) AS KutuKg,
                COALESCE(PALETKODU, '') AS PaletKodu,
                COALESCE(PALETADI, '') AS PaletAdi,
                COALESCE(PALETDARA, 0) AS PaletDara,
                COALESCE(PALETSAYISI, 0) AS PaletSayisi,
                COALESCE(PARCASAYISI, 0) AS ParcaSayisi,
                COALESCE(BURUTKG, 0) AS BurutKg,
                COALESCE(NETKG, 0) AS NetKg,
                COALESCE(FIYATI, 0) AS Fiyati,
                COALESCE(DOVIZTURU, 'EUR') AS DovizTuru,
                COALESCE(TUTARI, 0) AS Tutari,
                COALESCE(AMBKODU, '') AS AmbKodu,
                COALESCE(AMBADI, '') AS AmbAdi,
                COALESCE(BIRIM, 'KG') AS Birim,
                COALESCE(KDV, 0) AS Kdv,
                COALESCE(DARA, 0) AS Dara
            FROM {detailTable}
            WHERE FISNO = @FisNo
            ORDER BY SIRA ASC, IDNO ASC;
        ";

        var list = (await conn.QueryAsync<SiparisDetayDto>(sql, new { FisNo = fisNo })).ToList();
        return list;
    }

    /// <summary>
    /// Tek bir siparişi ve detaylarını getirir
    /// </summary>
    public async Task<SiparisDto?> GetSiparisByIdAsync(SiparisYonu yon, double fisNo)
    {
        var (masterTable, _) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var sql = $@"
            SELECT 
                COALESCE(IDNO, 0) AS IdNo,
                COALESCE(FISNO, 0) AS FisNo,
                COALESCE(FATNO, 0) AS FatNo,
                COALESCE(SIRKOD, '1') AS Sirkod,
                CASE WHEN TARIH IS NULL OR TARIH = '0000-00-00' THEN '2026-01-01' ELSE TARIH END AS Tarih,
                CASE WHEN ISLEMTARIHI IS NULL OR ISLEMTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE ISLEMTARIHI END AS IslemTarihi,
                CASE WHEN BASLAMATARIHI IS NULL OR BASLAMATARIHI = '0000-00-00' THEN NULL ELSE BASLAMATARIHI END AS BaslamaTarihi,
                CASE WHEN BITISTARIHI IS NULL OR BITISTARIHI = '0000-00-00' THEN NULL ELSE BITISTARIHI END AS BitisTarihi,
                COALESCE(DURUMU, 'SİPARİŞ ALINDI') AS Durumu,
                COALESCE(FIRKOD, '') AS Firkod,
                COALESCE(FIRMAADI, '') AS FirmaAdi,
                COALESCE(ADRESI, '') AS Adresi,
                COALESCE(SEHIR, '') AS Sehir,
                COALESCE(ULKE, '') AS Ulke,
                COALESCE(SATISSEKLI, 'DAP') AS SatisSekli,
                COALESCE(PARTINO, '') AS PartiNo,
                COALESCE(POZISYON, '1') AS Pozisyon,
                COALESCE(SIPARISIVEREN, '') AS SiparisiVeren,
                COALESCE(SIPARISIALAN, '') AS SiparisiAlan,
                COALESCE(PALETTOPLAM, 0) AS PaletToplam,
                COALESCE(KASATOPLAM, 0) AS KasaToplam,
                COALESCE(BURUTTOPLAM, 0) AS BurutToplam,
                COALESCE(NETTOPLAM, 0) AS NetToplam,
                COALESCE(TOPLAMTUTAR, 0) AS ToplamTutar,
                COALESCE(DOVIZTURU, 'EUR') AS DovizTuru,
                COALESCE(NAKLIYECI, '') AS Nakliyeci,
                COALESCE(ARACPLAKA, '') AS AracPlaka,
                COALESCE(ARACPLAKA2, '') AS AracPlaka2,
                COALESCE(SOFORADI, '') AS SoforAdi,
                COALESCE(SOFORTELEFON, '') AS SoforTelefon,
                COALESCE(FRIGODERECE, '+4°C') AS FrigoDerece,
                COALESCE(GUMRUK, '') AS Gumruk,
                COALESCE(GUMRUK_ADRES, '') AS GumrukAdres,
                COALESCE(BOSALTMA_ADRESI, '') AS BosaltmaAdresi,
                COALESCE(ACIKLAMA, '') AS Aciklama,
                COALESCE(KKULLANICI, '') AS Kullanici
            FROM {masterTable}
            WHERE FISNO = @FisNo
            LIMIT 1;
        ";

        var dto = await conn.QueryFirstOrDefaultAsync<SiparisDto>(sql, new { FisNo = fisNo });
        if (dto != null)
        {
            dto.SiparisYonu = yon;
            dto.Detaylar = await GetSiparisDetaylarAsync(yon, fisNo);
        }

        return dto;
    }

    /// <summary>
    /// Sıradaki sipariş fiş numarasını üretir
    /// </summary>
    public async Task<double> GetNextFisNoAsync(SiparisYonu yon)
    {
        var (masterTable, _) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var sql = $"SELECT COALESCE(MAX(FISNO), 800) + 1 FROM {masterTable};";
        var next = await conn.ExecuteScalarAsync<double>(sql);
        return next;
    }

    /// <summary>
    /// Sipariş ana kaydını ve kalemlerini kaydeder
    /// </summary>
    public async Task<bool> SaveSiparisAsync(SiparisYonu yon, SiparisDto master, List<SiparisDetayDto> details)
    {
        var (masterTable, detailTable) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            if (master.FisNo <= 0)
            {
                master.FisNo = await GetNextFisNoAsync(yon);
                master.FatNo = master.FisNo;
            }

            // Toplamları detaylardan otomatik hesapla
            master.PaletToplam = details.Sum(d => d.PaletSayisi);
            master.KasaToplam = details.Sum(d => d.ParcaSayisi);
            master.BurutToplam = details.Sum(d => d.BurutKg);
            master.NetToplam = details.Sum(d => d.NetKg);
            master.ToplamTutar = details.Sum(d => d.Tutari);

            var exists = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM {masterTable} WHERE FISNO = @FisNo;",
                new { FisNo = master.FisNo }, trans) > 0;

            if (exists)
            {
                var updateSql = $@"
                    UPDATE {masterTable} SET
                        TARIH = @Tarih,
                        BASLAMATARIHI = @BaslamaTarihi,
                        BITISTARIHI = @BitisTarihi,
                        DURUMU = @Durumu,
                        FIRKOD = @Firkod,
                        FIRMAADI = @FirmaAdi,
                        ADRESI = @Adresi,
                        SEHIR = @Sehir,
                        ULKE = @Ulke,
                        SATISSEKLI = @SatisSekli,
                        PARTINO = @PartiNo,
                        PALETTOPLAM = @PaletToplam,
                        KASATOPLAM = @KasaToplam,
                        BURUTTOPLAM = @BurutToplam,
                        NETTOPLAM = @NetToplam,
                        TOPLAMTUTAR = @ToplamTutar,
                        DOVIZTURU = @DovizTuru,
                        ARACPLAKA = @AracPlaka,
                        ARACPLAKA2 = @AracPlaka2,
                        SOFORADI = @SoforAdi,
                        SOFORTELEFON = @SoforTelefon,
                        FRIGODERECE = @FrigoDerece,
                        GUMRUK_ADRES = @GumrukAdres,
                        BOSALTMA_ADRESI = @BosaltmaAdresi,
                        ACIKLAMA = @Aciklama,
                        DKULLANICI = @Kullanici
                    WHERE FISNO = @FisNo;
                ";

                await conn.ExecuteAsync(updateSql, master, trans);
            }
            else
            {
                var insertSql = $@"
                    INSERT INTO {masterTable} (
                        SIRKOD, FISNO, FATNO, TARIH, ISLEMTARIHI, BASLAMATARIHI, BITISTARIHI,
                        DURUMU, FIRKOD, FIRMAADI, ADRESI, SEHIR, ULKE, SATISSEKLI, PARTINO, POZISYON,
                        SIPARISIVEREN, SIPARISIALAN, PALETTOPLAM, KASATOPLAM, BURUTTOPLAM, NETTOPLAM,
                        TOPLAMTUTAR, DOVIZTURU, NAKLIYECI, ARACPLAKA, ARACPLAKA2, SOFORADI, SOFORTELEFON,
                        FRIGODERECE, GUMRUK_ADRES, BOSALTMA_ADRESI, ACIKLAMA, KKULLANICI, ISYERINO, YETKIKODU
                    ) VALUES (
                        @Sirkod, @FisNo, @FatNo, @Tarih, NOW(), @BaslamaTarihi, @BitisTarihi,
                        @Durumu, @Firkod, @FirmaAdi, @Adresi, @Sehir, @Ulke, @SatisSekli, @PartiNo, @Pozisyon,
                        @SiparisiVeren, @SiparisiAlan, @PaletToplam, @KasaToplam, @BurutToplam, @NetToplam,
                        @ToplamTutar, @DovizTuru, @Nakliyeci, @AracPlaka, @AracPlaka2, @SoforAdi, @SoforTelefon,
                        @FrigoDerece, @GumrukAdres, @BosaltmaAdresi, @Aciklama, @Kullanici, 1, 'MERKEZ'
                    );
                ";

                await conn.ExecuteAsync(insertSql, master, trans);
            }

            // Kalemleri güncelle
            await conn.ExecuteAsync($"DELETE FROM {detailTable} WHERE FISNO = @FisNo;", new { FisNo = master.FisNo }, trans);

            if (details.Count > 0)
            {
                var insertDetailSql = $@"
                    INSERT INTO {detailTable} (
                        SIRKOD, FISNO, FATNO, SIRA, TARIH, MALKODU, MALADI, BOY, KALITE,
                        KUTUKODU, KUTUADI, KUTUDARA, KUTUKG, PALETKODU, PALETADI, PALETDARA,
                        PALETSAYISI, PARCASAYISI, BURUTKG, NETKG, FIYATI, DOVIZTURU, TUTARI,
                        AMBKODU, AMBADI, BIRIM, KDV, DARA, FIRKOD, FIRMAADI, ULKE, DURUMU, KKULLANICI, ISYERINO, YETKIKODU
                    ) VALUES (
                        '1', @FisNo, @FatNo, @Sira, @Tarih, @MalKodu, @MalAdi, @Boy, @Kalite,
                        @KutuKodu, @KutuAdi, @KutuDara, @KutuKg, @PaletKodu, @PaletAdi, @PaletDara,
                        @PaletSayisi, @ParcaSayisi, @BurutKg, @NetKg, @Fiyati, @DovizTuru, @Tutari,
                        @AmbKodu, @AmbAdi, @Birim, @Kdv, @Dara, @Firkod, @FirmaAdi, @Ulke, @Durumu, @Kullanici, 1, 'MERKEZ'
                    );
                ";

                int sira = 1;
                foreach (var d in details)
                {
                    d.FisNo = master.FisNo;
                    d.FatNo = master.FisNo;
                    d.Sira = sira++;
                    d.Tarih = master.Tarih;

                    await conn.ExecuteAsync(insertDetailSql, new
                    {
                        d.FisNo,
                        d.FatNo,
                        d.Sira,
                        d.Tarih,
                        d.MalKodu,
                        d.MalAdi,
                        d.Boy,
                        d.Kalite,
                        d.KutuKodu,
                        d.KutuAdi,
                        d.KutuDara,
                        d.KutuKg,
                        d.PaletKodu,
                        d.PaletAdi,
                        d.PaletDara,
                        d.PaletSayisi,
                        d.ParcaSayisi,
                        d.BurutKg,
                        d.NetKg,
                        d.Fiyati,
                        d.DovizTuru,
                        d.Tutari,
                        d.AmbKodu,
                        d.AmbAdi,
                        d.Birim,
                        d.Kdv,
                        d.Dara,
                        Firkod = master.Firkod,
                        FirmaAdi = master.FirmaAdi,
                        Ulke = master.Ulke,
                        Durumu = master.Durumu,
                        Kullanici = master.Kullanici
                    }, trans);
                }
            }

            trans.Commit();
            return true;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Sipariş durumunu günceller (SİPARİŞ ALINDI, HAZIRLANIYOR, YÜKLENDİ, SEVK EDİLDİ vb.)
    /// </summary>
    public async Task<bool> UpdateDurumAsync(SiparisYonu yon, double fisNo, string yeniDurum)
    {
        var (masterTable, detailTable) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var sql = $@"
            UPDATE {masterTable} SET DURUMU = @Durum WHERE FISNO = @FisNo;
            UPDATE {detailTable} SET DURUMU = @Durum WHERE FISNO = @FisNo;
        ";

        var affected = await conn.ExecuteAsync(sql, new { FisNo = fisNo, Durum = yeniDurum });
        return affected > 0;
    }

    /// <summary>
    /// Sipariş ve detaylarını siler
    /// </summary>
    public async Task<bool> DeleteSiparisAsync(SiparisYonu yon, double fisNo)
    {
        var (masterTable, detailTable) = GetTables(yon);
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync($"DELETE FROM {detailTable} WHERE FISNO = @FisNo;", new { FisNo = fisNo }, trans);
            await conn.ExecuteAsync($"DELETE FROM {masterTable} WHERE FISNO = @FisNo;", new { FisNo = fisNo }, trans);
            trans.Commit();
            return true;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Müşteri / Tedarikçi F10 arama listesi
    /// </summary>
    public async Task<List<dynamic>> GetFirmalarLookupAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                FIRKOD AS FirKod,
                COALESCE(FIRMAADI, '') AS FirmaAdi,
                COALESCE(ADRES1, '') AS Adres,
                COALESCE(SEHIR, '') AS Sehir,
                COALESCE(ULKE, '') AS Ulke
            FROM firma
            WHERE SIRKOD = '1'
            ORDER BY FIRMAADI ASC
            LIMIT 500;
        ";

        var list = (await conn.QueryAsync<dynamic>(sql)).ToList();
        return list;
    }

    /// <summary>
    /// Narenciye ve meyve stok kartları F10 listesi
    /// </summary>
    public async Task<List<StokLookupDto>> GetStoklarLookupAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COALESCE(KODU, '') AS MalKodu,
                COALESCE(ADI, '') AS MalAdi,
                COALESCE(BIRIM, 'KG') AS Birim,
                COALESCE(SFIYAT1, 0) AS Fiyat,
                COALESCE(DOVKOD, 'EUR') AS Doviz
            FROM stok
            WHERE SIRKOD = '1'
            ORDER BY ADI ASC
            LIMIT 500;
        ";

        var list = (await conn.QueryAsync<StokLookupDto>(sql)).ToList();
        return list;
    }
}
