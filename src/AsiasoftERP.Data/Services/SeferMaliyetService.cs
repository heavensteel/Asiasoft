using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Tır / Lojistik sefer ve maliyet veritabanı servis sınıfı
/// </summary>
public class SeferMaliyetService
{
    /// <summary>
    /// Filtrelere göre sefer listesini getirir
    /// </summary>
    public async Task<List<SeferFisDto>> GetSeferlerAsync(DateTime start, DateTime end, string seferTuru = "TÜMÜ", string durumu = "TÜMÜ", string search = "")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                COALESCE(SIRANO, 0) AS SiraNo,
                COALESCE(SIRKOD, '1') AS Sirkod,
                COALESCE(FISNO, 0) AS FisNo,
                COALESCE(FISTUR, 753) AS FisTur,
                CASE WHEN TARIH IS NULL OR TARIH = '0000-00-00' THEN '2026-01-01' ELSE TARIH END AS Tarih,
                CASE WHEN ISLEMTARIHI IS NULL OR ISLEMTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE ISLEMTARIHI END AS IslemTarihi,
                CASE WHEN CIKISTARIHI IS NULL OR CIKISTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE CIKISTARIHI END AS CikisTarihi,
                CASE WHEN DONUSTARIHI IS NULL OR DONUSTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE DONUSTARIHI END AS DonusTarihi,
                COALESCE(SEFER_TURU, 'YURT DIŞI') AS SeferTuru,
                COALESCE(DURUMU, 'AÇIK') AS Durumu,
                COALESCE(FIRKOD_PLAKA, 0) AS FirkodPlaka,
                COALESCE(FIRMAADI_PLAKA, '') AS Plaka,
                COALESCE(FIRKOD_TER, 0) AS FirkodTer,
                COALESCE(FIRMAADI_TER, '') AS DorsePlaka,
                COALESCE(FIRKOD, 0) AS Firkod,
                COALESCE(FIRMAADI, '') AS SurucuAdi,
                COALESCE(CIKIS_NOKTASI, '') AS CikisNoktasi,
                COALESCE(VARIS_NOKTASI, '') AS VarisNoktasi,
                COALESCE(YUKLEME_BOSALTMA_YERI, '') AS YuklemeBosaltmaYeri,
                COALESCE(YUKCINSI, '') AS YukCinsi,
                COALESCE(CIKISKM, 0) AS CikisKm,
                COALESCE(DONUSKM, 0) AS DonusKm,
                COALESCE(CIKISLITRE, 0) AS CikisLitre,
                COALESCE(TOPLAM_ALINAN_LITRE, 0) AS ToplamAlinanLitre,
                COALESCE(SEFER_DONUS_KALAN_LITRE, 0) AS SeferDonusKalanLitre,
                COALESCE(SEFERBEDELI, 0) AS SeferBedeli,
                COALESCE(SEFER_TUTAR, 0) AS SeferTutar,
                COALESCE(SEFER_DOVIZ_TURU, 'EUR') AS SeferDovizTuru,
                COALESCE(DOVIZKUR, 1.0) AS DovizKur,
                COALESCE(ALINAN_TL_AVANS, 0) AS AlinanTlAvans,
                COALESCE(ALINAN_DOVIZ_AVANS, 0) AS AlinanDovizAvans,
                COALESCE(PRIMTUTARI_TL, 0) AS PrimTutariTl,
                COALESCE(TOPLAM_MASRAF_TL, 0) AS ToplamMasrafTl,
                COALESCE(TOPLAM_DOVIZ_MASRAF, 0) AS ToplamDovizMasraf,
                COALESCE(ACIKLAMA, '') AS Aciklama,
                COALESCE(KKULLANICI, '') AS Kullanici
            FROM sefer_fis
            WHERE TARIH IS NOT NULL AND TARIH != '0000-00-00' AND TARIH >= @StartDate AND TARIH <= @EndDate
        ";

        var parameters = new DynamicParameters();
        parameters.Add("StartDate", start.Date);
        parameters.Add("EndDate", end.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(seferTuru) && seferTuru != "TÜMÜ")
        {
            sql += " AND SEFER_TURU = @SeferTuru";
            parameters.Add("SeferTuru", seferTuru);
        }

        if (!string.IsNullOrWhiteSpace(durumu) && durumu != "TÜMÜ")
        {
            sql += " AND DURUMU = @Durumu";
            parameters.Add("Durumu", durumu);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += @" AND (
                CAST(FISNO AS CHAR) LIKE @Search OR
                FIRMAADI_PLAKA LIKE @Search OR
                FIRMAADI_TER LIKE @Search OR
                FIRMAADI LIKE @Search OR
                CIKIS_NOKTASI LIKE @Search OR
                VARIS_NOKTASI LIKE @Search
            )";
            parameters.Add("Search", $"%{search}%");
        }

        sql += " ORDER BY TARIH DESC, FISNO DESC;";

        var result = (await conn.QueryAsync<SeferFisDto>(sql, parameters)).ToList();
        return result;
    }

    /// <summary>
    /// Dashboard KPI özet verilerini hesaplar
    /// </summary>
    public async Task<SeferKpiDto> GetKpisAsync(DateTime start, DateTime end)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COUNT(*) AS ToplamSefer,
                COALESCE(SUM(CASE WHEN DURUMU = 'AÇIK' THEN 1 ELSE 0 END), 0) AS AcikSefer,
                COALESCE(SUM(CASE WHEN DURUMU = 'KAPALI' THEN 1 ELSE 0 END), 0) AS KapaliSefer,
                COALESCE(SUM(CASE WHEN DONUSKM > CIKISKM THEN (DONUSKM - CIKISKM) ELSE 0 END), 0) AS ToplamKm,
                COALESCE(SUM(TOPLAM_MASRAF_TL), 0) AS ToplamMasrafTl,
                COALESCE(SUM(SEFERBEDELI), 0) AS ToplamNavlunTl,
                COALESCE(SUM(SEFERBEDELI - TOPLAM_MASRAF_TL - CASE WHEN PRIMTUTARI_TL > 0 THEN PRIMTUTARI_TL ELSE 0 END), 0) AS ToplamNetKarTl,
                COALESCE(SUM(CASE WHEN (CIKISLITRE + TOPLAM_ALINAN_LITRE) >= SEFER_DONUS_KALAN_LITRE 
                                  THEN (CIKISLITRE + TOPLAM_ALINAN_LITRE - SEFER_DONUS_KALAN_LITRE) 
                                  ELSE 0 END), 0) AS ToplamLitre
            FROM sefer_fis
            WHERE TARIH IS NOT NULL AND TARIH != '0000-00-00' AND TARIH >= @StartDate AND TARIH <= @EndDate;
        ";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            StartDate = start.Date,
            EndDate = end.Date.AddDays(1).AddTicks(-1)
        });

        var kpi = new SeferKpiDto();
        if (row != null)
        {
            kpi.ToplamSefer = Convert.ToInt32(row.ToplamSefer ?? 0);
            kpi.AcikSefer = Convert.ToInt32(row.AcikSefer ?? 0);
            kpi.KapaliSefer = Convert.ToInt32(row.KapaliSefer ?? 0);
            kpi.ToplamKm = Convert.ToDouble(row.ToplamKm ?? 0);
            kpi.ToplamMasrafTl = Convert.ToDouble(row.ToplamMasrafTl ?? 0);
            kpi.ToplamNavlunTl = Convert.ToDouble(row.ToplamNavlunTl ?? 0);
            kpi.ToplamNetKarTl = Convert.ToDouble(row.ToplamNetKarTl ?? 0);

            var totalLitre = Convert.ToDouble(row.ToplamLitre ?? 0);
            if (kpi.ToplamKm > 0 && totalLitre > 0)
            {
                kpi.Ortalama100KmSarfiyat = Math.Round((totalLitre / kpi.ToplamKm) * 100, 2);
            }
        }

        return kpi;
    }

    /// <summary>
    /// Sefer detay masraf kayıtlarını getirir
    /// </summary>
    public async Task<List<SeferDetayDto>> GetSeferDetaylarAsync(double fisNo)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COALESCE(SIRANO, 0) AS SiraNo,
                COALESCE(SIRKOD, '1') AS Sirkod,
                COALESCE(FISNO, 0) AS FisNo,
                COALESCE(FISTUR, 753) AS FisTur,
                CASE WHEN TARIH IS NULL OR TARIH = '0000-00-00' THEN '2026-01-01' ELSE TARIH END AS Tarih,
                COALESCE(HIZMETKODU, '') AS HizmetKodu,
                COALESCE(HIZMETAADI, '') AS HizmetAdi,
                COALESCE(BIRIM, 'TL') AS Birim,
                COALESCE(TUTAR, 0) AS Tutar,
                COALESCE(DOVIZTUTARI, 0) AS DovizTutari,
                COALESCE(DOVIZ_TL_KARSILIGI, 0) AS DovizTlKarsiligi,
                COALESCE(ACIKLAMA, '') AS Aciklama,
                COALESCE(KKULLANICI, '') AS Kullanici
            FROM sefer_detay
            WHERE FISNO = @FisNo
            ORDER BY TARIH ASC, SIRANO ASC;
        ";

        var list = (await conn.QueryAsync<SeferDetayDto>(sql, new { FisNo = fisNo })).ToList();
        return list;
    }

    /// <summary>
    /// Tek bir sefer kartını ve detaylarını getirir
    /// </summary>
    public async Task<SeferFisDto?> GetSeferByIdAsync(double fisNo)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COALESCE(SIRANO, 0) AS SiraNo,
                COALESCE(SIRKOD, '1') AS Sirkod,
                COALESCE(FISNO, 0) AS FisNo,
                COALESCE(FISTUR, 753) AS FisTur,
                CASE WHEN TARIH IS NULL OR TARIH = '0000-00-00' THEN '2026-01-01' ELSE TARIH END AS Tarih,
                CASE WHEN ISLEMTARIHI IS NULL OR ISLEMTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE ISLEMTARIHI END AS IslemTarihi,
                CASE WHEN CIKISTARIHI IS NULL OR CIKISTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE CIKISTARIHI END AS CikisTarihi,
                CASE WHEN DONUSTARIHI IS NULL OR DONUSTARIHI = '0000-00-00 00:00:00' THEN NULL ELSE DONUSTARIHI END AS DonusTarihi,
                COALESCE(SEFER_TURU, 'YURT DIŞI') AS SeferTuru,
                COALESCE(DURUMU, 'AÇIK') AS Durumu,
                COALESCE(FIRKOD_PLAKA, 0) AS FirkodPlaka,
                COALESCE(FIRMAADI_PLAKA, '') AS Plaka,
                COALESCE(FIRKOD_TER, 0) AS FirkodTer,
                COALESCE(FIRMAADI_TER, '') AS DorsePlaka,
                COALESCE(FIRKOD, 0) AS Firkod,
                COALESCE(FIRMAADI, '') AS SurucuAdi,
                COALESCE(CIKIS_NOKTASI, '') AS CikisNoktasi,
                COALESCE(VARIS_NOKTASI, '') AS VarisNoktasi,
                COALESCE(YUKLEME_BOSALTMA_YERI, '') AS YuklemeBosaltmaYeri,
                COALESCE(YUKCINSI, '') AS YukCinsi,
                COALESCE(CIKISKM, 0) AS CikisKm,
                COALESCE(DONUSKM, 0) AS DonusKm,
                COALESCE(CIKISLITRE, 0) AS CikisLitre,
                COALESCE(TOPLAM_ALINAN_LITRE, 0) AS ToplamAlinanLitre,
                COALESCE(SEFER_DONUS_KALAN_LITRE, 0) AS SeferDonusKalanLitre,
                COALESCE(SEFERBEDELI, 0) AS SeferBedeli,
                COALESCE(SEFER_TUTAR, 0) AS SeferTutar,
                COALESCE(SEFER_DOVIZ_TURU, 'EUR') AS SeferDovizTuru,
                COALESCE(DOVIZKUR, 1.0) AS DovizKur,
                COALESCE(ALINAN_TL_AVANS, 0) AS AlinanTlAvans,
                COALESCE(ALINAN_DOVIZ_AVANS, 0) AS AlinanDovizAvans,
                COALESCE(PRIMTUTARI_TL, 0) AS PrimTutariTl,
                COALESCE(TOPLAM_MASRAF_TL, 0) AS ToplamMasrafTl,
                COALESCE(TOPLAM_DOVIZ_MASRAF, 0) AS ToplamDovizMasraf,
                COALESCE(ACIKLAMA, '') AS Aciklama,
                COALESCE(KKULLANICI, '') AS Kullanici
            FROM sefer_fis
            WHERE FISNO = @FisNo
            LIMIT 1;
        ";

        var dto = await conn.QueryFirstOrDefaultAsync<SeferFisDto>(sql, new { FisNo = fisNo });
        if (dto != null)
        {
            dto.Detaylar = await GetSeferDetaylarAsync(fisNo);
        }

        return dto;
    }

    /// <summary>
    /// Sıradaki fiş numarasını üretir
    /// </summary>
    public async Task<double> GetNextFisNoAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT COALESCE(MAX(FISNO), 2200) + 1 FROM sefer_fis;";
        var next = await conn.ExecuteScalarAsync<double>(sql);
        return next;
    }

    /// <summary>
    /// Sefer ve detay masraflarını kaydeder (Insert / Update)
    /// </summary>
    public async Task<bool> SaveSeferAsync(SeferFisDto master, List<SeferDetayDto> details)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            if (master.FisNo <= 0)
            {
                master.FisNo = await GetNextFisNoAsync();
            }

            // Toplam masrafları hesapla
            master.ToplamMasrafTl = details.Sum(d => d.Tutar);
            master.ToplamDovizMasraf = details.Sum(d => d.DovizTutari);

            // Mevcut kayıt var mı?
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sefer_fis WHERE FISNO = @FisNo;",
                new { FisNo = master.FisNo }, trans) > 0;

            if (exists)
            {
                const string updateSql = @"
                    UPDATE sefer_fis SET
                        TARIH = @Tarih,
                        SEFER_TURU = @SeferTuru,
                        DURUMU = @Durumu,
                        FIRKOD_PLAKA = @FirkodPlaka,
                        FIRMAADI_PLAKA = @Plaka,
                        FIRKOD_TER = @FirkodTer,
                        FIRMAADI_TER = @DorsePlaka,
                        FIRKOD = @Firkod,
                        FIRMAADI = @SurucuAdi,
                        CIKIS_NOKTASI = @CikisNoktasi,
                        VARIS_NOKTASI = @VarisNoktasi,
                        YUKLEME_BOSALTMA_YERI = @YuklemeBosaltmaYeri,
                        YUKCINSI = @YukCinsi,
                        CIKISKM = @CikisKm,
                        DONUSKM = @DonusKm,
                        YAPILANKM = @YapilanKm,
                        CIKISLITRE = @CikisLitre,
                        TOPLAM_ALINAN_LITRE = @ToplamAlinanLitre,
                        SEFER_DONUS_KALAN_LITRE = @SeferDonusKalanLitre,
                        TUKETILEN_TOPLAM_LITRE = @TuketilenToplamLitre,
                        SEFERBEDELI = @SeferBedeli,
                        SEFER_TUTAR = @SeferTutar,
                        SEFER_DOVIZ_TURU = @SeferDovizTuru,
                        DOVIZKUR = @DovizKur,
                        ALINAN_TL_AVANS = @AlinanTlAvans,
                        ALINAN_DOVIZ_AVANS = @AlinanDovizAvans,
                        PRIMTUTARI_TL = @PrimTutariTl,
                        TOPLAM_MASRAF_TL = @ToplamMasrafTl,
                        TOPLAM_DOVIZ_MASRAF = @ToplamDovizMasraf,
                        ACIKLAMA = @Aciklama,
                        KKULLANICI = @Kullanici
                    WHERE FISNO = @FisNo;
                ";

                await conn.ExecuteAsync(updateSql, master, trans);
            }
            else
            {
                const string insertSql = @"
                    INSERT INTO sefer_fis (
                        SIRKOD, FISNO, FISTUR, ISLEMTARIHI, TARIH, CIKISTARIHI,
                        SEFER_TURU, DURUMU, FIRKOD_PLAKA, FIRMAADI_PLAKA, FIRKOD_TER, FIRMAADI_TER,
                        FIRKOD, FIRMAADI, CIKIS_NOKTASI, VARIS_NOKTASI, YUKLEME_BOSALTMA_YERI, YUKCINSI,
                        CIKISKM, DONUSKM, YAPILANKM, CIKISLITRE, TOPLAM_ALINAN_LITRE, SEFER_DONUS_KALAN_LITRE,
                        TUKETILEN_TOPLAM_LITRE, SEFERBEDELI, SEFER_TUTAR, SEFER_DOVIZ_TURU, DOVIZKUR,
                        ALINAN_TL_AVANS, ALINAN_DOVIZ_AVANS, PRIMTUTARI_TL, TOPLAM_MASRAF_TL, TOPLAM_DOVIZ_MASRAF,
                        ACIKLAMA, KKULLANICI, ISYERINO, YETKIKODU
                    ) VALUES (
                        @Sirkod, @FisNo, @FisTur, NOW(), @Tarih, @Tarih,
                        @SeferTuru, @Durumu, @FirkodPlaka, @Plaka, @FirkodTer, @DorsePlaka,
                        @Firkod, @SurucuAdi, @CikisNoktasi, @VarisNoktasi, @YuklemeBosaltmaYeri, @YukCinsi,
                        @CikisKm, @DonusKm, @YapilanKm, @CikisLitre, @ToplamAlinanLitre, @SeferDonusKalanLitre,
                        @TuketilenToplamLitre, @SeferBedeli, @SeferTutar, @SeferDovizTuru, @DovizKur,
                        @AlinanTlAvans, @AlinanDovizAvans, @PrimTutariTl, @ToplamMasrafTl, @ToplamDovizMasraf,
                        @Aciklama, @Kullanici, 68, 'NAKLİYE'
                    );
                ";

                await conn.ExecuteAsync(insertSql, master, trans);
            }

            // Detayları yeniden kaydet
            await conn.ExecuteAsync("DELETE FROM sefer_detay WHERE FISNO = @FisNo;", new { FisNo = master.FisNo }, trans);

            if (details.Count > 0)
            {
                const string insertDetailSql = @"
                    INSERT INTO sefer_detay (
                        SIRKOD, FISNO, FISTUR, TARIH, HIZMETKODU, HIZMETAADI,
                        BIRIM, TUTAR, DOVIZTUTARI, DOVIZ_TL_KARSILIGI, ACIKLAMA,
                        KKULLANICI, ISYERINO, YETKIKODU, FIRKOD, FIRMAADI, FIRKOD_PLAKA, FIRMAADI_PLAKA
                    ) VALUES (
                        '1', @FisNo, 753, @Tarih, @HizmetKodu, @HizmetAdi,
                        @Birim, @Tutar, @DovizTutari, @DovizTlKarsiligi, @Aciklama,
                        @Kullanici, 68, 'NAKLİYE', @Firkod, @SurucuAdi, @FirkodPlaka, @Plaka
                    );
                ";

                foreach (var d in details)
                {
                    await conn.ExecuteAsync(insertDetailSql, new
                    {
                        FisNo = master.FisNo,
                        Tarih = d.Tarih,
                        HizmetKodu = string.IsNullOrWhiteSpace(d.HizmetKodu) ? "100" : d.HizmetKodu,
                        HizmetAdi = d.HizmetAdi,
                        Birim = d.Birim,
                        Tutar = d.Tutar,
                        DovizTutari = d.DovizTutari,
                        DovizTlKarsiligi = d.DovizTlKarsiligi,
                        Aciklama = d.Aciklama,
                        Kullanici = master.Kullanici,
                        Firkod = master.Firkod,
                        SurucuAdi = master.SurucuAdi,
                        FirkodPlaka = master.FirkodPlaka,
                        Plaka = master.Plaka
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
    /// Açık olan bir seferi kapatır (Dönüş KM ve yakıt kaydedilir)
    /// </summary>
    public async Task<bool> KapatSeferAsync(double fisNo, double donusKm, double kalanLitre)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        var master = await GetSeferByIdAsync(fisNo);
        if (master == null) return false;

        master.DonusKm = donusKm;
        master.SeferDonusKalanLitre = kalanLitre;
        master.Durumu = "KAPALI";
        master.DonusTarihi = DateTime.Now;

        const string sql = @"
            UPDATE sefer_fis SET
                DONUSKM = @DonusKm,
                YAPILANKM = @YapilanKm,
                SEFER_DONUS_KALAN_LITRE = @SeferDonusKalanLitre,
                TUKETILEN_TOPLAM_LITRE = @TuketilenToplamLitre,
                DONUSTARIHI = NOW(),
                DURUMU = 'KAPALI'
            WHERE FISNO = @FisNo;
        ";

        var affected = await conn.ExecuteAsync(sql, master);
        return affected > 0;
    }

    /// <summary>
    /// Seferi ve detaylarını siler
    /// </summary>
    public async Task<bool> DeleteSeferAsync(double fisNo)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync("DELETE FROM sefer_detay WHERE FISNO = @FisNo;", new { FisNo = fisNo }, trans);
            await conn.ExecuteAsync("DELETE FROM sefer_fis WHERE FISNO = @FisNo;", new { FisNo = fisNo }, trans);
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
    /// Araçlar tablosundan F10 seçim rehberi için araç listesini çeker
    /// </summary>
    public async Task<List<AracLookupDto>> GetAraclarLookupAsync()
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                COALESCE(ARAC_KODU, SIRAIDNO) AS AracKodu,
                COALESCE(PLAKA, '') AS Plaka,
                COALESCE(DORSE_PLAKA, '') AS DorsePlaka,
                COALESCE(MARKA, '') AS Marka,
                COALESCE(MODEL, '') AS Model,
                COALESCE(TIPI, '') AS Tipi,
                COALESCE(ARAC_SEFER_TIPI, 'YURT DIŞI') AS AracSeferTipi,
                COALESCE(SFIRMAADI, '') AS SurucuAdi,
                COALESCE(SFIRKOD, 0) AS SurucuKodu
            FROM araclar
            WHERE PLAKA IS NOT NULL AND PLAKA != ''
            ORDER BY PLAKA ASC;
        ";

        var list = (await conn.QueryAsync<AracLookupDto>(sql)).ToList();
        return list;
    }

    /// <summary>
    /// Sık kullanılan standart masraf kalemleri listesi
    /// </summary>
    public List<string> GetStandartMasrafTurleri()
    {
        return new List<string>
        {
            "E-100 BOX İLE BULGARİSTAN YOL ÜCRETİ",
            "ROMANYA VİGNETTA FAZLASI",
            "GESTAŞ ÇANAKKALE FERİBOT ÜCRETİ",
            "T1 ÜCRETİ",
            "DKV GİDER ÜCRETİ",
            "KAPİTAN ANDREEVO SAHA ÜCRETİ",
            "KAPIKULE MESAİ ÜCRETİ",
            "DKV KART İLE BULGARİSTAN KÖPRÜ",
            "SEFER DIŞI HARCANAN YAKIT",
            "OMV KART İLE ROMANYA VİGNETTA",
            "DKV KART İLE BULGARİSTAN DOZVOLA",
            "ŞÖFÖR YURT DIŞI SEFER HARCIRAHi",
            "ŞÖFÖR YURT İÇİ YÜKLEME PRİMİ",
            "TURK ELEKTRONİK KART İLE ANALİZ ÖDEMESİ",
            "BULGARİSTAN ANALİZ NAKİT ÖDEME",
            "KAPIKULE MAZOT CEZASI / HARCI",
            "TR DOZVOLA ÜCRETİ",
            "DEMRE HAL GİRİŞ ÜCRETİ",
            "OTOBAN VE KÖPRÜ GEÇİŞ ÜCRETİ (HGS)",
            "YOL VE YEMEK GİDERLERİ"
        };
    }
}
