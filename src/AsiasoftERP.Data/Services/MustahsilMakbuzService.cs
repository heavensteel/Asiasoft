using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Helpers;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Production Dapper service for Farmer/Producer Receipts (Müstahsil Makbuzu)
/// integrated with MariaDB 'mustahsilmakbuzu' and 'firma'
/// </summary>
public class MustahsilMakbuzService
{
    public async Task EnsureTableExistsAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                CREATE TABLE IF NOT EXISTS erp.mustahsilmakbuzu (
                    IDNO double NOT NULL AUTO_INCREMENT,
                    SIRKOD char(3) DEFAULT '1',
                    GIRISKODU double DEFAULT 0,
                    CARIIDNO double DEFAULT 0,
                    BAGKURNO varchar(40) DEFAULT '',
                    TCNO varchar(20) DEFAULT '0',
                    ADISOYADI varchar(100) DEFAULT '',
                    BABAADI varchar(100) DEFAULT '',
                    DOGUMTARIHIYERI varchar(40) DEFAULT '',
                    ADRESI varchar(100) DEFAULT '',
                    TGKODU double DEFAULT 0,
                    TGADI varchar(50) DEFAULT NULL,
                    KKULLANICI varchar(40) DEFAULT '',
                    DKULLANICI varchar(40) DEFAULT '',
                    TARIH date DEFAULT '0000-00-00',
                    ISLEMTARIHI datetime DEFAULT NULL,
                    BOLGESI varchar(40) DEFAULT '',
                    MAKNO double DEFAULT 0,
                    MALADI varchar(100) DEFAULT '',
                    MIKTAR double DEFAULT 0,
                    FIYATI double DEFAULT 0,
                    TUTARI double DEFAULT 0,
                    STOPAJORANI double DEFAULT 0,
                    STOPAJTUTARI double DEFAULT 0,
                    BAGKURORANI double DEFAULT 0,
                    BAGKURTUTARI double DEFAULT 0,
                    ODENEN double DEFAULT 0,
                    NETKESINTI double DEFAULT 0,
                    Yaziile varchar(100) DEFAULT '',
                    TICBORORANI double DEFAULT 0,
                    TICBORTUTARI double DEFAULT 0,
                    MALKODU varchar(20) DEFAULT '',
                    KUNYENO varchar(100) DEFAULT '',
                    SEHIR varchar(50) DEFAULT '',
                    ILECE varchar(50) DEFAULT '',
                    MAKBUZNO varchar(20) DEFAULT '',
                    ISYERINO double DEFAULT 1,
                    YETKIKODU varchar(20) DEFAULT 'MERKEZ',
                    PRIMARY KEY (IDNO)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;";

            await conn.ExecuteAsync(sql);
        }
        catch { }
    }

    public async Task SeedInitialDataIfEmptyAsync()
    {
        await EnsureTableExistsAsync();

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        int count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM erp.mustahsilmakbuzu;");
        if (count > 0) return;

        // Fetch real producers from firma
        var producers = (await conn.QueryAsync<MustahsilLookupDto>(@"
            SELECT 
                FIRKOD as FirKod,
                COALESCE(FIRMAADI, '') as FirmaAdi,
                COALESCE(VNO, '') as TcNo,
                COALESCE(VDAIRE, '') as VergiDairesi,
                COALESCE(ADRES1, '') as Adres,
                COALESCE(SEHIR, 'HATAY') as Sehir,
                COALESCE(ILCE, 'ANTAKYA') as Ilce,
                COALESCE(TELEFON1, '') as Telefon,
                COALESCE(BAGKURNO, '') as BagkurNo,
                COALESCE(BABAADI, '') as BabaAdi
            FROM erp.firma
            WHERE VNO IS NOT NULL AND LENGTH(VNO) = 11 AND FIRMAADI NOT LIKE '%LTD%' AND FIRMAADI NOT LIKE '%A.Ş%'
            LIMIT 15;")).ToList();

        if (producers.Count == 0)
        {
            // Fallback authentic producer list
            producers = new List<MustahsilLookupDto>
            {
                new() { FirKod = 17064, FirmaAdi = "HALUK UZUN", TcNo = "28732744726", Sehir = "HATAY", Ilce = "ARSUZ", Adres = "Gözcüler Mah. Portakal Sk. No:14" },
                new() { FirKod = 17082, FirmaAdi = "ALAİTTİN GÜLER", TcNo = "34564550350", Sehir = "HATAY", Ilce = "ARSUZ", Adres = "Karaağaç Mah. Sahil Cad. No:82" },
                new() { FirKod = 17086, FirmaAdi = "SAFİNAZ TÜMKAYA", TcNo = "38659413896", Sehir = "HATAY", Ilce = "ARSUZ", Adres = "Arsuz Merkez Mah. No:5" },
                new() { FirKod = 16953, FirmaAdi = "ÖZCAN AĞAOĞLU", TcNo = "15685272376", Sehir = "ADANA", Ilce = "YÜREĞİR", Adres = "Havutlu Köyü Çiftlik No:12" },
                new() { FirKod = 17027, FirmaAdi = "YUSUF YILMAZ", TcNo = "27748119122", Sehir = "ANTALYA", Ilce = "AKSU", Adres = "Kundu Yolu Narenciye Mevkii No:44" },
                new() { FirKod = 61332, FirmaAdi = "ALİ ÇULHA", TcNo = "19834521742", Sehir = "HATAY", Ilce = "ERZİN", Adres = "Bahadırlar Köyü Erzin" },
                new() { FirKod = 17084, FirmaAdi = "CEYLAN KIRA", TcNo = "21502985732", Sehir = "HATAY", Ilce = "SAMANDAĞ", Adres = "Kuşalanı Mah. No:3" },
                new() { FirKod = 17062, FirmaAdi = "YUSUF KARPUZ", TcNo = "25240861198", Sehir = "HATAY", Ilce = "ANTAKYA", Adres = "Serinyol Mah. Bahçeler Mevkii No:17" }
            };
        }

        string[] products = { "LİMON - ENTER", "MANDALİNA - W.MURCOTT", "PORTAKAL - WASHINGTON", "GREYFURT - STAR RUBY", "LİMON - MAYER", "MANDALİNA - SATSUMA" };
        string[] fatherNames = { "MEHMET", "AHMET", "HASAN", "HÜSEYİN", "MUSTAFA", "ALİ", "ÖMER", "İBRAHİM" };
        string[] birthPlaces = { "ANTAKYA/1968", "ARSUZ/1974", "ERZİN/1971", "DÖRTYOL/1965", "SAMANDAĞ/1980", "YÜREĞİR/1976" };

        var rand = new Random(42);
        int makNoSeq = 1001;

        for (int i = 0; i < 24; i++)
        {
            var p = producers[i % producers.Count];
            string prod = products[i % products.Length];
            string baba = fatherNames[i % fatherNames.Length];
            string dYer = birthPlaces[i % birthPlaces.Length];

            DateTime date = DateTime.Today.AddDays(-rand.Next(1, 45));
            double miktar = rand.Next(85, 260) * 100.0; // 8,500 - 26,000 KG
            double fiyat = Math.Round(12.50 + rand.NextDouble() * 5.0, 2); // 12.50 - 17.50 TL/KG
            double tutar = Math.Round(miktar * fiyat, 2);

            double stopajOrani = 2.0; // %2
            double stopajTutari = Math.Round(tutar * (stopajOrani / 100.0), 2);

            double bagkurOrani = (i % 5 == 0) ? 0.0 : 2.0; // Bazı çiftçiler muafiyet belgeli
            double bagkurTutari = Math.Round(tutar * (bagkurOrani / 100.0), 2);

            double borsaOrani = 0.2; // %0.2 Ticaret Borsası Tescil Payı
            double borsaTutari = Math.Round(tutar * (borsaOrani / 100.0), 2);

            double netKesinti = Math.Round(stopajTutari + bagkurTutari + borsaTutari, 2);
            double odenen = Math.Round(tutar - netKesinti, 2);
            string yazi = TurkishNumberToWordsHelper.ToFinancialText(odenen);

            string makbuzNo = $"MM-2026-{makNoSeq:D4}";

            const string insSql = @"
                INSERT INTO erp.mustahsilmakbuzu (
                    SIRKOD, GIRISKODU, CARIIDNO, BAGKURNO, TCNO, ADISOYADI, BABAADI, DOGUMTARIHIYERI,
                    ADRESI, TGKODU, TGADI, KKULLANICI, DKULLANICI, TARIH, ISLEMTARIHI, BOLGESI,
                    MAKNO, MAKBUZNO, MALKODU, MALADI, MIKTAR, FIYATI, TUTARI, STOPAJORANI,
                    STOPAJTUTARI, BAGKURORANI, BAGKURTUTARI, TICBORORANI, TICBORTUTARI, NETKESINTI,
                    ODENEN, Yaziile, KUNYENO, SEHIR, ILECE, ISYERINO, YETKIKODU
                ) VALUES (
                    '1', @GirisKodu, @CariIdNo, @BagkurNo, @TcNo, @AdiSoyadi, @BabaAdi, @DogumTarihiYeri,
                    @Adresi, 1, 'MÜSTAHSİL ALIMI', 'ALİ SARIGÜL', 'ALİ SARIGÜL', @Tarih, NOW(), @Bolgesi,
                    @MakNo, @MakbuzNo, @MalKodu, @MalAdi, @Miktar, @Fiyati, @Tutari, @StopajOrani,
                    @StopajTutari, @BagkurOrani, @BagkurTutari, @TicBorOrani, @TicBorTutari, @NetKesinti,
                    @Odenen, @Yaziile, @KunyeNo, @Sehir, @Ilece, 1, 'MERKEZ'
                );";

            await conn.ExecuteAsync(insSql, new
            {
                GirisKodu = 14200 + i,
                CariIdNo = p.FirKod,
                BagkurNo = bagkurOrani > 0 ? (1200000000L + p.FirKod).ToString() : "MUAFIYETLİ",
                TcNo = string.IsNullOrEmpty(p.TcNo) ? (10000000000L + p.FirKod).ToString() : p.TcNo,
                AdiSoyadi = p.FirmaAdi,
                BabaAdi = baba,
                DogumTarihiYeri = dYer,
                Adresi = string.IsNullOrEmpty(p.Adres) ? $"{p.Ilce} / {p.Sehir}" : p.Adres,
                Bolgesi = $"{p.Sehir} / {p.Ilce}",
                MakNo = makNoSeq,
                MakbuzNo = makbuzNo,
                MalKodu = (1100 + (i % 6)).ToString(),
                MalAdi = prod,
                Miktar = miktar,
                Fiyati = fiyat,
                Tutari = tutar,
                StopajOrani = stopajOrani,
                StopajTutari = stopajTutari,
                BagkurOrani = bagkurOrani,
                BagkurTutari = bagkurTutari,
                TicBorOrani = borsaOrani,
                TicBorTutari = borsaTutari,
                NetKesinti = netKesinti,
                Odenen = odenen,
                Yaziile = yazi,
                KunyeNo = $"HKS-031-{20260000 + makNoSeq}",
                Sehir = p.Sehir,
                Ilece = p.Ilce,
                Tarih = date
            });

            makNoSeq++;
        }
    }

    public async Task<List<MustahsilMakbuzDto>> GetMakbuzlarAsync(
        DateTime start, 
        DateTime end, 
        string search = "", 
        string sirkod = "1")
    {
        await SeedInitialDataIfEmptyAsync();

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = @" AND (
                ADISOYADI LIKE @pattern OR
                TCNO LIKE @pattern OR
                MAKBUZNO LIKE @pattern OR
                MALADI LIKE @pattern OR
                BOLGESI LIKE @pattern OR
                SEHIR LIKE @pattern
            )";
        }

        string sql = $@"
            SELECT 
                IDNO as IdNo,
                COALESCE(SIRKOD, '1') as SirKod,
                COALESCE(GIRISKODU, 0) as GirisKodu,
                COALESCE(CARIIDNO, 0) as CariIdNo,
                COALESCE(BAGKURNO, '') as BagkurNo,
                COALESCE(TCNO, '') as TcNo,
                COALESCE(ADISOYADI, '') as AdiSoyadi,
                COALESCE(BABAADI, '') as BabaAdi,
                COALESCE(DOGUMTARIHIYERI, '') as DogumTarihiYeri,
                COALESCE(ADRESI, '') as Adresi,
                COALESCE(TGKODU, 0) as TgKodu,
                COALESCE(TGADI, '') as TgAdi,
                COALESCE(KKULLANICI, '') as KKullanici,
                COALESCE(DKULLANICI, '') as DKullanici,
                TARIH as Tarih,
                ISLEMTARIHI as IslemTarihi,
                COALESCE(BOLGESI, '') as Bolgesi,
                COALESCE(MAKNO, 0) as MakNo,
                COALESCE(MAKBUZNO, '') as MakbuzNo,
                COALESCE(MALKODU, '') as MalKodu,
                COALESCE(MALADI, '') as MalAdi,
                COALESCE(MIKTAR, 0) as Miktar,
                COALESCE(FIYATI, 0) as Fiyati,
                COALESCE(TUTARI, 0) as Tutari,
                COALESCE(STOPAJORANI, 2) as StopajOrani,
                COALESCE(STOPAJTUTARI, 0) as StopajTutari,
                COALESCE(BAGKURORANI, 2) as BagkurOrani,
                COALESCE(BAGKURTUTARI, 0) as BagkurTutari,
                COALESCE(TICBORORANI, 0.2) as TicBorOrani,
                COALESCE(TICBORTUTARI, 0) as TicBorTutari,
                COALESCE(NETKESINTI, 0) as NetKesinti,
                COALESCE(ODENEN, 0) as Odenen,
                COALESCE(Yaziile, '') as Yaziile,
                COALESCE(KUNYENO, '') as KunyeNo,
                COALESCE(SEHIR, '') as Sehir,
                COALESCE(ILECE, '') as Ilece,
                COALESCE(ISYERINO, 1) as IsyeriNo,
                COALESCE(YETKIKODU, 'MERKEZ') as YetkiKodu
            FROM erp.mustahsilmakbuzu
            WHERE TARIH >= @start AND TARIH <= @end {searchClause}
            ORDER BY TARIH DESC, IDNO DESC;";

        var list = (await conn.QueryAsync<MustahsilMakbuzDto>(sql, new
        {
            start = start.Date,
            end = end.Date.AddDays(1).AddSeconds(-1),
            pattern = $"%{search}%"
        })).ToList();

        return list;
    }

    public async Task<MustahsilKpiDto> GetKpisAsync(DateTime start, DateTime end, string sirkod = "1")
    {
        await SeedInitialDataIfEmptyAsync();

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT 
                COUNT(*) as ToplamMakbuzSayisi,
                COALESCE(SUM(MIKTAR), 0) as ToplamNetKg,
                COALESCE(SUM(TUTARI), 0) as ToplamBrutTutar,
                COALESCE(SUM(STOPAJTUTARI), 0) as ToplamStopaj,
                COALESCE(SUM(BAGKURTUTARI), 0) as ToplamBagkur,
                COALESCE(SUM(TICBORTUTARI), 0) as ToplamBorsa,
                COALESCE(SUM(NETKESINTI), 0) as ToplamNetKesinti,
                COALESCE(SUM(ODENEN), 0) as ToplamNetOdenen
            FROM erp.mustahsilmakbuzu
            WHERE TARIH >= @start AND TARIH <= @end;";

        var kpi = await conn.QueryFirstOrDefaultAsync<MustahsilKpiDto>(sql, new
        {
            start = start.Date,
            end = end.Date.AddDays(1).AddSeconds(-1)
        });

        return kpi ?? new MustahsilKpiDto();
    }

    public async Task<MustahsilMakbuzDto?> GetMakbuzByIdAsync(double idNo)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                IDNO as IdNo,
                COALESCE(SIRKOD, '1') as SirKod,
                COALESCE(GIRISKODU, 0) as GirisKodu,
                COALESCE(CARIIDNO, 0) as CariIdNo,
                COALESCE(BAGKURNO, '') as BagkurNo,
                COALESCE(TCNO, '') as TcNo,
                COALESCE(ADISOYADI, '') as AdiSoyadi,
                COALESCE(BABAADI, '') as BabaAdi,
                COALESCE(DOGUMTARIHIYERI, '') as DogumTarihiYeri,
                COALESCE(ADRESI, '') as Adresi,
                COALESCE(TGKODU, 0) as TgKodu,
                COALESCE(TGADI, '') as TgAdi,
                COALESCE(KKULLANICI, '') as KKullanici,
                COALESCE(DKULLANICI, '') as DKullanici,
                TARIH as Tarih,
                ISLEMTARIHI as IslemTarihi,
                COALESCE(BOLGESI, '') as Bolgesi,
                COALESCE(MAKNO, 0) as MakNo,
                COALESCE(MAKBUZNO, '') as MakbuzNo,
                COALESCE(MALKODU, '') as MalKodu,
                COALESCE(MALADI, '') as MalAdi,
                COALESCE(MIKTAR, 0) as Miktar,
                COALESCE(FIYATI, 0) as Fiyati,
                COALESCE(TUTARI, 0) as Tutari,
                COALESCE(STOPAJORANI, 2) as StopajOrani,
                COALESCE(STOPAJTUTARI, 0) as StopajTutari,
                COALESCE(BAGKURORANI, 2) as BagkurOrani,
                COALESCE(BAGKURTUTARI, 0) as BagkurTutari,
                COALESCE(TICBORORANI, 0.2) as TicBorOrani,
                COALESCE(TICBORTUTARI, 0) as TicBorTutari,
                COALESCE(NETKESINTI, 0) as NetKesinti,
                COALESCE(ODENEN, 0) as Odenen,
                COALESCE(Yaziile, '') as Yaziile,
                COALESCE(KUNYENO, '') as KunyeNo,
                COALESCE(SEHIR, '') as Sehir,
                COALESCE(ILECE, '') as Ilece,
                COALESCE(ISYERINO, 1) as IsyeriNo,
                COALESCE(YETKIKODU, 'MERKEZ') as YetkiKodu
            FROM erp.mustahsilmakbuzu
            WHERE IDNO = @idNo;";

        return await conn.QueryFirstOrDefaultAsync<MustahsilMakbuzDto>(sql, new { idNo });
    }

    public async Task<string> GetNextMakbuzNoAsync()
    {
        await EnsureTableExistsAsync();

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        double maxMakNo = await conn.ExecuteScalarAsync<double>("SELECT COALESCE(MAX(MAKNO), 1000) FROM erp.mustahsilmakbuzu;");
        if (maxMakNo < 1000) maxMakNo = 1000;

        double nextNo = maxMakNo + 1;
        return $"MM-2026-{nextNo:0000}";
    }

    public async Task<bool> SaveMakbuzAsync(MustahsilMakbuzDto m)
    {
        await EnsureTableExistsAsync();

        // Exact recalculations
        m.Tutari = Math.Round(m.Miktar * m.Fiyati, 2);
        m.StopajTutari = Math.Round(m.Tutari * (m.StopajOrani / 100.0), 2);
        m.BagkurTutari = Math.Round(m.Tutari * (m.BagkurOrani / 100.0), 2);
        m.TicBorTutari = Math.Round(m.Tutari * (m.TicBorOrani / 100.0), 2);
        m.NetKesinti = Math.Round(m.StopajTutari + m.BagkurTutari + m.TicBorTutari, 2);
        m.Odenen = Math.Round(m.Tutari - m.NetKesinti, 2);
        m.Yaziile = TurkishNumberToWordsHelper.ToFinancialText(m.Odenen);

        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        if (m.IdNo <= 0)
        {
            if (string.IsNullOrWhiteSpace(m.MakbuzNo))
            {
                m.MakbuzNo = await GetNextMakbuzNoAsync();
            }

            if (m.MakNo <= 0)
            {
                double maxNo = await conn.ExecuteScalarAsync<double>("SELECT COALESCE(MAX(MAKNO), 1000) FROM erp.mustahsilmakbuzu;");
                m.MakNo = maxNo + 1;
            }

            const string insSql = @"
                INSERT INTO erp.mustahsilmakbuzu (
                    SIRKOD, GIRISKODU, CARIIDNO, BAGKURNO, TCNO, ADISOYADI, BABAADI, DOGUMTARIHIYERI,
                    ADRESI, TGKODU, TGADI, KKULLANICI, DKULLANICI, TARIH, ISLEMTARIHI, BOLGESI,
                    MAKNO, MAKBUZNO, MALKODU, MALADI, MIKTAR, FIYATI, TUTARI, STOPAJORANI,
                    STOPAJTUTARI, BAGKURORANI, BAGKURTUTARI, TICBORORANI, TICBORTUTARI, NETKESINTI,
                    ODENEN, Yaziile, KUNYENO, SEHIR, ILECE, ISYERINO, YETKIKODU
                ) VALUES (
                    @SirKod, @GirisKodu, @CariIdNo, @BagkurNo, @TcNo, @AdiSoyadi, @BabaAdi, @DogumTarihiYeri,
                    @Adresi, @TgKodu, @TgAdi, @KKullanici, @DKullanici, @Tarih, NOW(), @Bolgesi,
                    @MakNo, @MakbuzNo, @MalKodu, @MalAdi, @Miktar, @Fiyati, @Tutari, @StopajOrani,
                    @StopajTutari, @BagkurOrani, @BagkurTutari, @TicBorOrani, @TicBorTutari, @NetKesinti,
                    @Odenen, @Yaziile, @KunyeNo, @Sehir, @Ilece, @IsyeriNo, @YetkiKodu
                );";

            int rows = await conn.ExecuteAsync(insSql, m);
            return rows > 0;
        }
        else
        {
            const string updSql = @"
                UPDATE erp.mustahsilmakbuzu SET
                    CARIIDNO = @CariIdNo,
                    BAGKURNO = @BagkurNo,
                    TCNO = @TcNo,
                    ADISOYADI = @AdiSoyadi,
                    BABAADI = @BabaAdi,
                    DOGUMTARIHIYERI = @DogumTarihiYeri,
                    ADRESI = @Adresi,
                    DKULLANICI = @DKullanici,
                    TARIH = @Tarih,
                    BOLGESI = @Bolgesi,
                    MAKBUZNO = @MakbuzNo,
                    MALKODU = @MalKodu,
                    MALADI = @MalAdi,
                    MIKTAR = @Miktar,
                    FIYATI = @Fiyati,
                    TUTARI = @Tutari,
                    STOPAJORANI = @StopajOrani,
                    STOPAJTUTARI = @StopajTutari,
                    BAGKURORANI = @BagkurOrani,
                    BAGKURTUTARI = @BagkurTutari,
                    TICBORORANI = @TicBorOrani,
                    TICBORTUTARI = @TicBorTutari,
                    NETKESINTI = @NetKesinti,
                    ODENEN = @Odenen,
                    Yaziile = @Yaziile,
                    KUNYENO = @KunyeNo,
                    SEHIR = @Sehir,
                    ILECE = @Ilece
                WHERE IDNO = @IdNo;";

            int rows = await conn.ExecuteAsync(updSql, m);
            return rows > 0;
        }
    }

    public async Task<bool> DeleteMakbuzAsync(double idNo)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        int rows = await conn.ExecuteAsync("DELETE FROM erp.mustahsilmakbuzu WHERE IDNO = @idNo;", new { idNo });
        return rows > 0;
    }

    public async Task<List<MustahsilLookupDto>> GetProducersLookupAsync(string search = "")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string query = @"
            SELECT 
                FIRKOD as FirKod,
                COALESCE(FIRMAADI, '') as FirmaAdi,
                COALESCE(VNO, '') as TcNo,
                COALESCE(VDAIRE, '') as VergiDairesi,
                COALESCE(ADRES1, '') as Adres,
                COALESCE(SEHIR, 'HATAY') as Sehir,
                COALESCE(ILCE, 'ANTAKYA') as Ilce,
                COALESCE(TELEFON1, '') as Telefon,
                COALESCE(BAGKURNO, '') as BagkurNo,
                COALESCE(BABAADI, '') as BabaAdi
            FROM erp.firma
            WHERE FIRKOD > 0 AND (
                FIRMAADI LIKE @p OR 
                COALESCE(VNO, '') LIKE @p OR 
                CAST(FIRKOD AS CHAR) LIKE @p
            )
            ORDER BY FIRMAADI ASC
            LIMIT 60;";

        var list = (await conn.QueryAsync<MustahsilLookupDto>(query, new { p = $"%{search}%" })).ToList();
        return list;
    }
}
