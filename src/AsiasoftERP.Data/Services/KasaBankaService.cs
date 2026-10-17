using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

public class KasaBankaService
{
    public async Task<List<KasaKartDto>> GetKasaKartlariAsync(string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                k.SIRKOD as Sirkod,
                k.KODU as Kodu,
                COALESCE(k.ADI, '') as Adi,
                COALESCE(k.DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(k.MUHKODU, '') as MuhKodu,
                COALESCE(t.ToplamGiris, 0) as ToplamGiris,
                COALESCE(o.ToplamCikis, 0) as ToplamCikis
            FROM kasatnm k
            LEFT JOIN (
                SELECT KASAKODU, SUM(COALESCE(TUTAR, 0)) as ToplamGiris 
                FROM tahsilatana 
                WHERE SIRKOD = @sirkod 
                GROUP BY KASAKODU
            ) t ON k.KODU = t.KASAKODU
            LEFT JOIN (
                SELECT KASAKODU, SUM(COALESCE(TUTAR, 0)) as ToplamCikis 
                FROM odemeana 
                WHERE SIRKOD = @sirkod 
                GROUP BY KASAKODU
            ) o ON k.KODU = o.KASAKODU
            WHERE k.SIRKOD = @sirkod
            ORDER BY CASE WHEN (COALESCE(t.ToplamGiris, 0) + COALESCE(o.ToplamCikis, 0)) > 0 THEN 0 ELSE 1 END, k.KODU ASC;";

        var rows = await conn.QueryAsync<KasaKartDto>(sql, new { sirkod });
        return rows.ToList();
    }

    public async Task<List<BankaHesapDto>> GetBankaHesaplariAsync(string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                b.SIRKOD as Sirkod,
                b.KODU as Kodu,
                COALESCE(b.ADI, '') as Adi,
                COALESCE(b.SUBEADI, '') as SubeAdi,
                COALESCE(b.HESAPNO, '') as HesapNo,
                COALESCE(b.IBAN, '') as Iban,
                COALESCE(b.DOVIZCINSI, 'TL') as DovizCinsi,
                COALESCE(b.MUHKODU, '') as MuhKodu,
                COALESCE(t.ToplamYatan, 0) as ToplamYatan,
                COALESCE(o.ToplamCekilen, 0) as ToplamCekilen
            FROM banka b
            LEFT JOIN (
                SELECT BANKAKODU, SUM(COALESCE(TUTAR, 0)) as ToplamYatan 
                FROM bankatahsilatdetay 
                WHERE SIRKOD = @sirkod AND BANKAKODU > 0
                GROUP BY BANKAKODU
            ) t ON b.KODU = t.BANKAKODU
            LEFT JOIN (
                SELECT BANKAKODU, SUM(COALESCE(TUTAR, 0)) as ToplamCekilen 
                FROM bankaodemedetay 
                WHERE SIRKOD = @sirkod AND BANKAKODU > 0
                GROUP BY BANKAKODU
            ) o ON b.KODU = o.BANKAKODU
            WHERE b.SIRKOD = @sirkod
            ORDER BY CASE WHEN (COALESCE(t.ToplamYatan, 0) + COALESCE(o.ToplamCekilen, 0)) > 0 THEN 0 ELSE 1 END, b.KODU ASC;";

        var rows = await conn.QueryAsync<BankaHesapDto>(sql, new { sirkod });
        return rows.ToList();
    }

    public async Task<List<FinansHareketDto>> GetKasaEkstreAsync(
        double kasaKodu, 
        DateTime start, 
        DateTime end, 
        string search = "", 
        string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        // 1. Devreden Bakiye (Start tarihinden önceki hareketler toplamı)
        const string devredenSql = @"
            SELECT 
                (SELECT COALESCE(SUM(TUTAR), 0) FROM tahsilatana WHERE SIRKOD = @sirkod AND KASAKODU = @kasaKodu AND FISTAR < @startDate) -
                (SELECT COALESCE(SUM(TUTAR), 0) FROM odemeana WHERE SIRKOD = @sirkod AND KASAKODU = @kasaKodu AND FISTAR < @startDate);";

        double devreden = await conn.ExecuteScalarAsync<double>(devredenSql, new { sirkod, kasaKodu, startDate = start.Date });

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = " AND (ACIKLAMA LIKE @pat OR SATICIADI LIKE @pat OR CAST(FISNO AS CHAR) LIKE @pat)";
        }

        // 2. Dönem İçi Hareketler (Tahsilat + Tediye)
        string sql = $@"
            SELECT 
                FISNO as FisNo,
                MAKBUZNO as MakbuzNo,
                FISTAR as FisTar,
                'Kasa Tahsilat' as IslemTipi,
                'Kasa' as HesapTuru,
                KASAKODU as HesapKodu,
                'KASA' as HesapAdi,
                SATICINO as FirKod,
                COALESCE(SATICIADI, '') as CariAdi,
                '' as BelgeNo,
                COALESCE(ACIKLAMA, '') as Aciklama,
                COALESCE(TUTAR, 0) as Borc,
                0.0 as Alacak,
                COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                COALESCE(USER, '') as User
            FROM tahsilatana
            WHERE SIRKOD = @sirkod 
              AND KASAKODU = @kasaKodu 
              AND FISTAR >= @startDate AND FISTAR <= @endDate
              {searchClause}

            UNION ALL

            SELECT 
                FISNO as FisNo,
                MAKBUZNO as MakbuzNo,
                FISTAR as FisTar,
                'Kasa Tediye' as IslemTipi,
                'Kasa' as HesapTuru,
                KASAKODU as HesapKodu,
                COALESCE(KASAADI, 'KASA') as HesapAdi,
                SATICINO as FirKod,
                COALESCE(SATICIADI, '') as CariAdi,
                '' as BelgeNo,
                COALESCE(ACIKLAMA, '') as Aciklama,
                0.0 as Borc,
                COALESCE(TUTAR, 0) as Alacak,
                COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                COALESCE(USER, '') as User
            FROM odemeana
            WHERE SIRKOD = @sirkod 
              AND KASAKODU = @kasaKodu 
              AND FISTAR >= @startDate AND FISTAR <= @endDate
              {searchClause}

            ORDER BY FisTar ASC, FisNo ASC;";

        var prms = new
        {
            sirkod,
            kasaKodu,
            startDate = start.Date,
            endDate = end.Date,
            pat = $"%{search.Trim()}%"
        };

        var rawRows = (await conn.QueryAsync<FinansHareketDto>(sql, prms)).ToList();

        // 3. Devreden Satırı ve Yürüyen Bakiye Hesaplama
        var result = new List<FinansHareketDto>();
        double runningBalance = devreden;

        result.Add(new FinansHareketDto
        {
            FisNo = 0,
            FisTar = start.Date,
            IslemTipi = "DEVİR",
            HesapTuru = "Kasa",
            HesapKodu = kasaKodu,
            Aciklama = "DÖNEM BAŞI DEVREDEN BAKİYE",
            Borc = devreden >= 0 ? devreden : 0,
            Alacak = devreden < 0 ? Math.Abs(devreden) : 0,
            Bakiye = runningBalance,
            User = "SİSTEM"
        });

        int sira = 1;
        foreach (var r in rawRows)
        {
            r.SiraNo = sira++;
            runningBalance += (r.Borc - r.Alacak);
            r.Bakiye = runningBalance;
            result.Add(r);
        }

        return result;
    }

    public async Task<List<FinansHareketDto>> GetBankaEkstreAsync(
        double bankaKodu, 
        DateTime start, 
        DateTime end, 
        string search = "", 
        string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        // 1. Devreden Bakiye
        const string devredenSql = @"
            SELECT 
                (SELECT COALESCE(SUM(TUTAR), 0) FROM bankatahsilatdetay WHERE SIRKOD = @sirkod AND BANKAKODU = @bankaKodu AND FISTAR < @startDate) -
                (SELECT COALESCE(SUM(TUTAR), 0) FROM bankaodemedetay WHERE SIRKOD = @sirkod AND BANKAKODU = @bankaKodu AND FISTAR < @startDate);";

        double devreden = await conn.ExecuteScalarAsync<double>(devredenSql, new { sirkod, bankaKodu, startDate = start.Date });

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = " AND (ACIKLAMA LIKE @pat OR FIRADI LIKE @pat OR CAST(FISNO AS CHAR) LIKE @pat)";
        }

        // 2. Dönem İçi Hareketler
        string sql = $@"
            SELECT 
                FISNO as FisNo,
                MAKBUZNO as MakbuzNo,
                FISTAR as FisTar,
                'Gelen Havale' as IslemTipi,
                'Banka' as HesapTuru,
                BANKAKODU as HesapKodu,
                COALESCE(BANKAADI, '') as HesapAdi,
                FIRKOD as FirKod,
                COALESCE(FIRADI, '') as CariAdi,
                COALESCE(BELGENO, '') as BelgeNo,
                COALESCE(ACIKLAMA, '') as Aciklama,
                COALESCE(TUTAR, 0) as Borc,
                0.0 as Alacak,
                COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                COALESCE(USER, '') as User
            FROM bankatahsilatdetay
            WHERE SIRKOD = @sirkod 
              AND BANKAKODU = @bankaKodu 
              AND FISTAR >= @startDate AND FISTAR <= @endDate
              {searchClause}

            UNION ALL

            SELECT 
                FISNO as FisNo,
                MAKBUZNO as MakbuzNo,
                FISTAR as FisTar,
                'Gönderilen Havale' as IslemTipi,
                'Banka' as HesapTuru,
                BANKAKODU as HesapKodu,
                COALESCE(BANKAADI, '') as HesapAdi,
                FIRKOD as FirKod,
                COALESCE(FIRADI, '') as CariAdi,
                COALESCE(BELGENO, '') as BelgeNo,
                COALESCE(ACIKLAMA, '') as Aciklama,
                0.0 as Borc,
                COALESCE(TUTAR, 0) as Alacak,
                COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                COALESCE(USER, '') as User
            FROM bankaodemedetay
            WHERE SIRKOD = @sirkod 
              AND BANKAKODU = @bankaKodu 
              AND FISTAR >= @startDate AND FISTAR <= @endDate
              {searchClause}

            ORDER BY FisTar ASC, FisNo ASC;";

        var prms = new
        {
            sirkod,
            bankaKodu,
            startDate = start.Date,
            endDate = end.Date,
            pat = $"%{search.Trim()}%"
        };

        var rawRows = (await conn.QueryAsync<FinansHareketDto>(sql, prms)).ToList();

        var result = new List<FinansHareketDto>();
        double runningBalance = devreden;

        result.Add(new FinansHareketDto
        {
            FisNo = 0,
            FisTar = start.Date,
            IslemTipi = "DEVİR",
            HesapTuru = "Banka",
            HesapKodu = bankaKodu,
            Aciklama = "DÖNEM BAŞI DEVREDEN MEVDUAT",
            Borc = devreden >= 0 ? devreden : 0,
            Alacak = devreden < 0 ? Math.Abs(devreden) : 0,
            Bakiye = runningBalance,
            User = "SİSTEM"
        });

        int sira = 1;
        foreach (var r in rawRows)
        {
            r.SiraNo = sira++;
            runningBalance += (r.Borc - r.Alacak);
            r.Bakiye = runningBalance;
            result.Add(r);
        }

        return result;
    }

    public async Task<List<FinansHareketDto>> GetSonFinansHareketleriAsync(
        DateTime start, 
        DateTime end, 
        string islemFiltre = "Tümü", 
        string search = "", 
        string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string searchClause = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchClause = " AND (ACIKLAMA LIKE @pat OR CariAdi LIKE @pat OR CAST(FisNo AS CHAR) LIKE @pat)";
        }

        string islemClause = islemFiltre switch
        {
            "Kasa Tahsilat" => "WHERE IslemTipi = 'Kasa Tahsilat'",
            "Kasa Tediye" => "WHERE IslemTipi = 'Kasa Tediye'",
            "Gelen Havale" or "Banka Tahsilat" => "WHERE IslemTipi = 'Gelen Havale'",
            "Gönderilen Havale" or "Banka Tediye" => "WHERE IslemTipi = 'Gönderilen Havale'",
            _ => ""
        };

        string sql = $@"
            SELECT * FROM (
                SELECT 
                    FISNO as FisNo,
                    MAKBUZNO as MakbuzNo,
                    FISTAR as FisTar,
                    'Kasa Tahsilat' as IslemTipi,
                    'Kasa' as HesapTuru,
                    KASAKODU as HesapKodu,
                    'MERKEZ KASA' as HesapAdi,
                    SATICINO as FirKod,
                    COALESCE(SATICIADI, '') as CariAdi,
                    '' as BelgeNo,
                    COALESCE(ACIKLAMA, '') as Aciklama,
                    COALESCE(TUTAR, 0) as Borc,
                    0.0 as Alacak,
                    COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                    COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                    COALESCE(USER, '') as User
                FROM tahsilatana
                WHERE SIRKOD = @sirkod AND FISTAR >= @start AND FISTAR <= @end

                UNION ALL

                SELECT 
                    FISNO as FisNo,
                    MAKBUZNO as MakbuzNo,
                    FISTAR as FisTar,
                    'Kasa Tediye' as IslemTipi,
                    'Kasa' as HesapTuru,
                    KASAKODU as HesapKodu,
                    COALESCE(KASAADI, 'MERKEZ KASA') as HesapAdi,
                    SATICINO as FirKod,
                    COALESCE(SATICIADI, '') as CariAdi,
                    '' as BelgeNo,
                    COALESCE(ACIKLAMA, '') as Aciklama,
                    0.0 as Borc,
                    COALESCE(TUTAR, 0) as Alacak,
                    COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                    COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                    COALESCE(USER, '') as User
                FROM odemeana
                WHERE SIRKOD = @sirkod AND FISTAR >= @start AND FISTAR <= @end

                UNION ALL

                SELECT 
                    FISNO as FisNo,
                    MAKBUZNO as MakbuzNo,
                    FISTAR as FisTar,
                    'Gelen Havale' as IslemTipi,
                    'Banka' as HesapTuru,
                    BANKAKODU as HesapKodu,
                    COALESCE(BANKAADI, 'BANKA') as HesapAdi,
                    FIRKOD as FirKod,
                    COALESCE(FIRADI, '') as CariAdi,
                    COALESCE(BELGENO, '') as BelgeNo,
                    COALESCE(ACIKLAMA, '') as Aciklama,
                    COALESCE(TUTAR, 0) as Borc,
                    0.0 as Alacak,
                    COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                    COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                    COALESCE(USER, '') as User
                FROM bankatahsilatdetay
                WHERE SIRKOD = @sirkod AND FISTAR >= @start AND FISTAR <= @end

                UNION ALL

                SELECT 
                    FISNO as FisNo,
                    MAKBUZNO as MakbuzNo,
                    FISTAR as FisTar,
                    'Gönderilen Havale' as IslemTipi,
                    'Banka' as HesapTuru,
                    BANKAKODU as HesapKodu,
                    COALESCE(BANKAADI, 'BANKA') as HesapAdi,
                    FIRKOD as FirKod,
                    COALESCE(FIRADI, '') as CariAdi,
                    COALESCE(BELGENO, '') as BelgeNo,
                    COALESCE(ACIKLAMA, '') as Aciklama,
                    0.0 as Borc,
                    COALESCE(TUTAR, 0) as Alacak,
                    COALESCE(DOVIZTURU, 'TL') as DovizTuru,
                    COALESCE(DOVIZTUTAR, 0) as DovizTutar,
                    COALESCE(USER, '') as User
                FROM bankaodemedetay
                WHERE SIRKOD = @sirkod AND FISTAR >= @start AND FISTAR <= @end
            ) u
            {islemClause}
            {searchClause}
            ORDER BY FisTar DESC, FisNo DESC
            LIMIT 1000;";

        var prms = new
        {
            sirkod,
            start = start.Date,
            end = end.Date,
            pat = $"%{search.Trim()}%"
        };

        var rows = await conn.QueryAsync<FinansHareketDto>(sql, prms);
        return rows.ToList();
    }

    public async Task<double> GetNextFisNoAsync(string tableName, string sirkod = "1")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string sql = $"SELECT COALESCE(MAX(FISNO), 0) + 1 FROM {tableName} WHERE SIRKOD = @sirkod;";
        return await conn.ExecuteScalarAsync<double>(sql, new { sirkod });
    }

    public async Task<double> SaveFinansFisiAsync(FinansFisGirisDto dto)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        try
        {
            if (dto.IslemTipi == FinansIslemTipi.KasaTahsilat)
            {
                if (dto.FisNo <= 0) dto.FisNo = await conn.ExecuteScalarAsync<double>("SELECT COALESCE(MAX(FISNO), 0) + 1 FROM tahsilatana WHERE SIRKOD = @Sirkod;", dto, trans);
                if (dto.MakbuzNo <= 0) dto.MakbuzNo = dto.FisNo;

                const string sqlHdr = @"
                    INSERT INTO tahsilatana (
                        SIRKOD, FISNO, MAKBUZNO, FISTAR, TURU, USER,
                        SATICINO, SATICIADI, ACIKLAMA, TUTAR, KASAKODU, DOVIZTURU, DOVIZKUR, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, @FisTar, 'Kasa Tahsilat', @User,
                        @FirKod, @CariAdi, @Aciklama, @Tutar, @KasaKodu, @DovizTuru, @DovizKur, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlHdr, dto, trans);

                const string sqlDet = @"
                    INSERT INTO tahsilatdetay (
                        SIRKOD, FISNO, MAKBUZNO, SIRA, FISTAR, TURU,
                        FIRKOD, FIRADI, TUTAR, ACIKLAMA, SATICIKODU, SATICIADI,
                        BELGENO, USER, KASAKODU, KASAADI, DOVIZTURU, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, 1, @FisTar, 'Kasa Tahsilat',
                        @FirKod, @CariAdi, @Tutar, @Aciklama, @FirKod, @CariAdi,
                        @BelgeNo, @User, @KasaKodu, @KasaAdi, @DovizTuru, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlDet, dto, trans);
            }
            else if (dto.IslemTipi == FinansIslemTipi.KasaTediye)
            {
                if (dto.FisNo <= 0) dto.FisNo = await conn.ExecuteScalarAsync<double>("SELECT COALESCE(MAX(FISNO), 0) + 1 FROM odemeana WHERE SIRKOD = @Sirkod;", dto, trans);
                if (dto.MakbuzNo <= 0) dto.MakbuzNo = dto.FisNo;

                const string sqlHdr = @"
                    INSERT INTO odemeana (
                        SIRKOD, FISNO, MAKBUZNO, FISTAR, TURU, USER,
                        SATICINO, SATICIADI, ACIKLAMA, TUTAR, KASAKODU, KASAADI, DOVIZTURU, DOVIZKUR, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, @FisTar, 'Kasa Tediye', @User,
                        @FirKod, @CariAdi, @Aciklama, @Tutar, @KasaKodu, @KasaAdi, @DovizTuru, @DovizKur, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlHdr, dto, trans);

                const string sqlDet = @"
                    INSERT INTO odemedetay (
                        SIRKOD, FISNO, MAKBUZNO, SIRA, FISTAR, TURU,
                        FIRKOD, FIRADI, TUTAR, ACIKLAMA, SATICIKODU, SATICIADI,
                        BELGENO, USER, KASAKODU, KASAADI, DOVIZTURU, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, 1, @FisTar, 'Kasa Tediye',
                        @FirKod, @CariAdi, @Tutar, @Aciklama, @FirKod, @CariAdi,
                        @BelgeNo, @User, @KasaKodu, @KasaAdi, @DovizTuru, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlDet, dto, trans);
            }
            else if (dto.IslemTipi == FinansIslemTipi.BankaTahsilat)
            {
                if (dto.FisNo <= 0) dto.FisNo = await conn.ExecuteScalarAsync<double>("SELECT COALESCE(MAX(FISNO), 0) + 1 FROM bankatahsilatana WHERE SIRKOD = @Sirkod;", dto, trans);
                if (dto.MakbuzNo <= 0) dto.MakbuzNo = dto.FisNo;

                const string sqlHdr = @"
                    INSERT INTO bankatahsilatana (
                        SIRKOD, FISNO, MAKBUZNO, FISTAR, TURU, USER,
                        SATICINO, SATICIADI, BANKAKODU, BANKAADI, ACIKLAMA, TUTAR, DOVIZTURU, DOVIZKUR, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, @FisTar, 'Gelen Havale', @User,
                        @FirKod, @CariAdi, @BankaKodu, @BankaAdi, @Aciklama, @Tutar, @DovizTuru, @DovizKur, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlHdr, dto, trans);

                const string sqlDet = @"
                    INSERT INTO bankatahsilatdetay (
                        SIRKOD, FISNO, MAKBUZNO, SIRA, FISTAR, TURU,
                        FIRKOD, FIRADI, BANKAKODU, BANKAADI, TUTAR, ACIKLAMA, BELGENO, USER, DOVIZTURU, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, 1, @FisTar, 'Gelen Havale',
                        @FirKod, @CariAdi, @BankaKodu, @BankaAdi, @Tutar, @Aciklama, @BelgeNo, @User, @DovizTuru, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlDet, dto, trans);
            }
            else if (dto.IslemTipi == FinansIslemTipi.BankaTediye)
            {
                if (dto.FisNo <= 0) dto.FisNo = await conn.ExecuteScalarAsync<double>("SELECT COALESCE(MAX(FISNO), 0) + 1 FROM bankaodemeana WHERE SIRKOD = @Sirkod;", dto, trans);
                if (dto.MakbuzNo <= 0) dto.MakbuzNo = dto.FisNo;

                const string sqlHdr = @"
                    INSERT INTO bankaodemeana (
                        SIRKOD, FISNO, MAKBUZNO, FISTAR, TURU, USER,
                        SATICINO, SATICIADI, BANKAKODU, BANKAADI, ACIKLAMA, TUTAR, DOVIZTURU, DOVIZKUR, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, @FisTar, 'Gönderilen Havale', @User,
                        @FirKod, @CariAdi, @BankaKodu, @BankaAdi, @Aciklama, @Tutar, @DovizTuru, @DovizKur, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlHdr, dto, trans);

                const string sqlDet = @"
                    INSERT INTO bankaodemedetay (
                        SIRKOD, FISNO, MAKBUZNO, SIRA, FISTAR, TURU,
                        FIRKOD, FIRADI, BANKAKODU, BANKAADI, TUTAR, ACIKLAMA, BELGENO, USER, DOVIZTURU, DOVIZTUTAR, Yaziile
                    ) VALUES (
                        @Sirkod, @FisNo, @MakbuzNo, 1, @FisTar, 'Gönderilen Havale',
                        @FirKod, @CariAdi, @BankaKodu, @BankaAdi, @Tutar, @Aciklama, @BelgeNo, @User, @DovizTuru, @DovizTutar, @YaziIle
                    );";
                await conn.ExecuteAsync(sqlDet, dto, trans);
            }

            trans.Commit();
            return dto.FisNo;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }
}
