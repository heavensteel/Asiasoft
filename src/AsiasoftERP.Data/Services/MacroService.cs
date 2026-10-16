using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;
using Dapper;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Core service handling MacroSoft ERP entities, companies, users, settings, and fast data retrieval
/// </summary>
public class MacroService
{
    private static readonly string IniPath = @"D:\AsiaSoft\ERPV7\PC_ayarlar.ini";

    /// <summary>
    /// Loads all registered companies from table 'sirket'
    /// </summary>
    public async Task<List<CompanyDto>> GetCompaniesAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    CAST(SIRKOD AS SIGNED) as SirKod, 
                    COALESCE(ADI, '') as Adi, 
                    COALESCE(TELEFON, '') as Telefon, 
                    COALESCE(VERGINO, '') as VergiNo, 
                    COALESCE(SEHIR, '') as Sehir, 
                    COALESCE(PARA, 'TL') as Para 
                FROM sirket 
                ORDER BY SIRKOD ASC;";

            var list = (await conn.QueryAsync<CompanyDto>(sql)).ToList();
            if (list.Count == 0)
            {
                list.Add(new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." });
            }
            return list;
        }
        catch
        {
            return new List<CompanyDto>
            {
                new() { SirKod = 1, Adi = "TEK ASYA A.Ş." },
                new() { SirKod = 2, Adi = "LAREX" },
                new() { SirKod = 3, Adi = "AL SELEVIA" }
            };
        }
    }

    /// <summary>
    /// Loads all users mapped to a specific company from table 'sirketkullanicilari'
    /// </summary>
    public async Task<List<CompanyUserDto>> GetCompanyUsersAsync(string sirKod)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    sk.SIRANO as Sirano,
                    COALESCE(sk.SIRKOD, '1') as SirKod,
                    COALESCE(sk.SIRKETADI, '') as SirketAdi,
                    COALESCE(sk.KULLANICIKODU, 0) as KullaniciKodu,
                    COALESCE(sk.KULLANICIADI, '') as KullaniciAdi,
                    COALESCE(sk.OZELKODU, '') as OzelKodu,
                    COALESCE(u.YETKI, 'S') as Yetki,
                    CASE WHEN COALESCE(u.YETKI, 'S') = 'A' OR sk.KULLANICIKODU = 1 OR sk.KULLANICIKODU = 100 THEN 1 ELSE 0 END as IsAdmin
                FROM sirketkullanicilari sk
                LEFT JOIN user u ON u.KODU = CAST(sk.KULLANICIKODU AS CHAR)
                WHERE sk.SIRKOD = @SirKod
                ORDER BY sk.KULLANICIADI ASC;";

            var list = (await conn.QueryAsync<CompanyUserDto>(sql, new { SirKod = sirKod })).ToList();
            if (list.Count == 0)
            {
                list.Add(new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", SirKod = sirKod, IsAdmin = true, Yetki = "A" });
                list.Add(new CompanyUserDto { KullaniciKodu = 73, KullaniciAdi = "ZEHRA ERYILMAZ", SirKod = sirKod, IsAdmin = true, Yetki = "A" });
                list.Add(new CompanyUserDto { KullaniciKodu = 1, KullaniciAdi = "MACROSOFT", SirKod = sirKod, IsAdmin = true, Yetki = "A" });
            }
            return list;
        }
        catch
        {
            return new List<CompanyUserDto>
            {
                new() { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", SirKod = sirKod, IsAdmin = true, Yetki = "A" },
                new() { KullaniciKodu = 73, KullaniciAdi = "ZEHRA ERYILMAZ", SirKod = sirKod, IsAdmin = true, Yetki = "A" },
                new() { KullaniciKodu = 1, KullaniciAdi = "MACROSOFT", SirKod = sirKod, IsAdmin = true, Yetki = "A" }
            };
        }
    }

    /// <summary>
    /// Validates password against the 'user' table
    /// </summary>
    public async Task<(bool Success, string Message, bool IsAdmin)> ValidateUserPasswordAsync(double kullaniciKodu, string inputPassword)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT COALESCE(SIFRE, '') as Sifre, COALESCE(YETKI, 'S') as Yetki 
                FROM user 
                WHERE KODU = @Kodu OR REFKODU = @Kodu 
                LIMIT 1;";

            var row = await conn.QueryFirstOrDefaultAsync(sql, new { Kodu = ((int)kullaniciKodu).ToString() });
            if (row == null)
            {
                return (true, "Giriş başarılı.", false);
            }

            string dbPassword = (string)row.Sifre;
            string yetki = (string)row.Yetki;
            bool isAdmin = yetki.Trim().ToUpper() == "A";

            if (!string.IsNullOrWhiteSpace(dbPassword))
            {
                if (inputPassword.Trim() != dbPassword.Trim())
                {
                    return (false, "Girilen şifre hatalı! Lütfen şifrenizi kontrol ediniz.", isAdmin);
                }
            }

            return (true, "Giriş başarılı.", isAdmin);
        }
        catch (Exception ex)
        {
            return (true, $"Giriş yapıldı (Uyarı: {ex.Message})", false);
        }
    }

    /// <summary>
    /// Reads saved login preferences from PC_ayarlar.ini
    /// </summary>
    public (int SirketKodu, string SirketAdi, double KullaniciKodu, string KullaniciAdi) LoadSettings()
    {
        int sirKod = 1;
        string sirAdi = "TEK ASYA A.Ş.";
        double kulKod = 73;
        string kulAdi = "ZEHRA ERYILMAZ";

        try
        {
            if (File.Exists(IniPath))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var lines = File.ReadAllLines(IniPath, Encoding.GetEncoding(1254));
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim().ToLower();
                        var val = parts[1].Trim();
                        if (key == "sirket_kodu" && int.TryParse(val, out int sk)) sirKod = sk;
                        else if (key == "sirket_adi") sirAdi = val;
                        else if (key == "kullanici_kodu" && double.TryParse(val, out double kk)) kulKod = kk;
                        else if (key == "kullanici_adi") kulAdi = val;
                    }
                }
            }
        }
        catch
        {
            // fallback defaults
        }

        return (sirKod, sirAdi, kulKod, kulAdi);
    }

    /// <summary>
    /// Persists selected company and user to PC_ayarlar.ini
    /// </summary>
    public void SaveSettings(int sirKod, string sirAdi, double kulKod, string kulAdi)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var sb = new StringBuilder();
            sb.AppendLine("[Settings]");
            sb.AppendLine($"sirket_kodu={sirKod}");
            sb.AppendLine($"sirket_adi={sirAdi}");
            sb.AppendLine($"kullanici_kodu={(int)kulKod}");
            sb.AppendLine($"kullanici_Adi={kulAdi}");

            var dir = Path.GetDirectoryName(IniPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(IniPath, sb.ToString(), Encoding.GetEncoding(1254));
        }
        catch
        {
            // Non-critical if file is locked
        }
    }

    /// <summary>
    /// Paged search for the main Cari list from table 'firma'
    /// </summary>
    public async Task<(List<CariListItemDto> Items, int TotalCount)> GetCarilerPagedAsync(string search, int skip = 0, int take = 100)
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string countSql;
        string dataSql;
        object parameters;

        if (string.IsNullOrWhiteSpace(search))
        {
            countSql = "SELECT COUNT(*) FROM firma WHERE FIRKOD > 0;";
            dataSql = @"
                SELECT 
                    FIRKOD as FirKod, 
                    COALESCE(FIRMAADI, '') as FirmaAdi, 
                    COALESCE(UNVAN, '') as Unvan, 
                    COALESCE(VDAIRE, '') as VergiDairesi, 
                    COALESCE(VNO, '') as VergiNo, 
                    COALESCE(TELEFON1, '') as Telefon, 
                    COALESCE(SEHIR, '') as Sehir, 
                    COALESCE(BORC, 0) as Borc, 
                    COALESCE(ALACAK, 0) as Alacak,
                    COALESCE(AFIRKOD, '') as AFirKod
                FROM firma
                WHERE FIRKOD > 0
                ORDER BY FIRMAADI ASC
                LIMIT @Take OFFSET @Skip;";
            parameters = new { Take = take, Skip = skip };
        }
        else
        {
            double.TryParse(search, out double exactNum);
            countSql = @"
                SELECT COUNT(*) FROM firma 
                WHERE (FIRKOD = @ExactNum OR FIRMAADI LIKE @Pattern OR UNVAN LIKE @Pattern OR VNO LIKE @Pattern OR AFIRKOD LIKE @Pattern);";
            dataSql = @"
                SELECT 
                    FIRKOD as FirKod, 
                    COALESCE(FIRMAADI, '') as FirmaAdi, 
                    COALESCE(UNVAN, '') as Unvan, 
                    COALESCE(VDAIRE, '') as VergiDairesi, 
                    COALESCE(VNO, '') as VergiNo, 
                    COALESCE(TELEFON1, '') as Telefon, 
                    COALESCE(SEHIR, '') as Sehir, 
                    COALESCE(BORC, 0) as Borc, 
                    COALESCE(ALACAK, 0) as Alacak,
                    COALESCE(AFIRKOD, '') as AFirKod
                FROM firma
                WHERE (FIRKOD = @ExactNum OR FIRMAADI LIKE @Pattern OR UNVAN LIKE @Pattern OR VNO LIKE @Pattern OR AFIRKOD LIKE @Pattern)
                ORDER BY FIRMAADI ASC
                LIMIT @Take OFFSET @Skip;";
            parameters = new { ExactNum = exactNum, Pattern = $"%{search}%", Take = take, Skip = skip };
        }

        int totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await conn.QueryAsync<CariListItemDto>(dataSql, parameters)).ToList();

        return (items, totalCount);
    }

    #region User Management Methods

    /// <summary>
    /// Gets the next available numeric code for a new user in table 'user'
    /// </summary>
    public async Task<string> GetNextErpUserCodeAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = "SELECT COALESCE(MAX(CAST(KODU AS SIGNED)), 100) + 1 FROM user WHERE KODU REGEXP '^[0-9]+$';";
            var next = await conn.ExecuteScalarAsync<int>(sql);
            return next > 0 ? next.ToString() : "101";
        }
        catch
        {
            return "101";
        }
    }

    /// <summary>
    /// Fetches all ERP users from table 'user' combined with company assignments from 'sirketkullanicilari'
    /// </summary>
    public async Task<List<ErpUserDto>> GetAllErpUsersAsync(string search = "")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                u.KODU as Kodu,
                COALESCE(u.ADI, '') as Adi,
                COALESCE(u.SIFRE, '') as Sifre,
                COALESCE(u.YETKI, 'S') as Yetki,
                COALESCE(u.DURUM, 'EVET') as Durum,
                COALESCE(u.OZELKODU, '') as OzelKodu,
                COALESCE(u.cari, 'EVET') as CariStr,
                COALESCE(u.stok, 'EVET') as StokStr,
                COALESCE(u.alis, 'EVET') as AlisStr,
                COALESCE(u.satis, 'EVET') as SatisStr,
                COALESCE(u.perakendeiade, 'EVET') as PerakendeIadeStr,
                COALESCE(u.perakende, 'EVET') as PerakendeStr,
                COALESCE(u.fis, 'EVET') as FisStr,
                COALESCE(u.ekstre, 'EVET') as EkstreStr,
                COALESCE(u.borcalacak, 'EVET') as BorcAlacakStr,
                COALESCE(u.kasa, 'EVET') as KasaStr,
                COALESCE(u.alisiade, 'EVET') as AlisIadeStr,
                COALESCE(u.satisiade, 'EVET') as SatisIadeStr,
                COALESCE(u.tahsilatgirisi, 'EVET') as TahsilatGirisiStr,
                COALESCE(u.odemegirisi, 'EVET') as OdemeGirisiStr,
                COALESCE(u.mustericekgirisi, 'EVET') as MusteriCekGirisiStr,
                COALESCE(u.musterisenetgirisi, 'EVET') as MusteriSenetGirisiStr,
                COALESCE(u.firmacekcikisi, 'EVET') as FirmaCekCikisiStr,
                COALESCE(u.borcdekontugirisi, 'EVET') as BorcDekontuGirisiStr,
                COALESCE(u.alacakdekonturgirisi, 'EVET') as AlacakDekontuGirisiStr,
                COALESCE(u.bakiyegorgirisi, 'EVET') as BakiyeGorGirisiStr,
                COALESCE(u.fiyatgorgiris, 'EVET') as FiyatGorGirisStr,
                COALESCE(u.bakiyegoster, 'EVET') as BakiyeGosterStr,
                COALESCE(u.gunsonuraporu, 'EVET') as GunSonuRaporuStr,
                COALESCE(u.isletmedurumraporu, 'EVET') as IsletmeDurumRaporuStr,
                COALESCE(u.satisozeti, 'EVET') as SatisOzetiStr,
                COALESCE(u.satiskarlilik, 'EVET') as SatisKarlilikStr,
                COALESCE(u.senetraporu, 'EVET') as SenetRaporuStr,
                COALESCE(u.mustericekraporu, 'EVET') as MusteriCekRaporuStr,
                COALESCE(u.firmacekraporu, 'EVET') as FirmaCekRaporuStr,
                COALESCE(u.depobazindastokraporu, 'EVET') as DepoBazindaStokRaporuStr,
                COALESCE(u.stokgiriscikisrapuru, 'EVET') as StokGirisCikisRaporuStr,
                COALESCE(u.aylikanaliz, 'EVET') as AylikAnalizStr,
                GROUP_CONCAT(DISTINCT sk.SIRKETADI ORDER BY CAST(sk.SIRKOD AS SIGNED) SEPARATOR ', ') as Sirketler,
                GROUP_CONCAT(DISTINCT sk.SIRKOD ORDER BY CAST(sk.SIRKOD AS SIGNED) SEPARATOR ',') as SirketKodlariRaw
            FROM user u
            LEFT JOIN sirketkullanicilari sk ON sk.KULLANICIKODU = CAST(u.KODU AS SIGNED)
            GROUP BY u.KODU
            ORDER BY CAST(u.KODU AS SIGNED) ASC;";

        var rows = await conn.QueryAsync(sql);
        var result = new List<ErpUserDto>();

        foreach (var r in rows)
        {
            var dto = new ErpUserDto
            {
                Kodu = (string)r.Kodu,
                Adi = (string)r.Adi,
                Sifre = (string)r.Sifre,
                Yetki = (string)r.Yetki,
                Durum = (string)r.Durum,
                OzelKodu = (string)r.OzelKodu,
                Sirketler = (string)(r.Sirketler ?? string.Empty),
                SirketKodlariRaw = (string)(r.SirketKodlariRaw ?? string.Empty),
                Cari = IsYes(r.CariStr),
                Stok = IsYes(r.StokStr),
                Alis = IsYes(r.AlisStr),
                Satis = IsYes(r.SatisStr),
                PerakendeIade = IsYes(r.PerakendeIadeStr),
                Perakende = IsYes(r.PerakendeStr),
                Fis = IsYes(r.FisStr),
                Ekstre = IsYes(r.EkstreStr),
                BorcAlacak = IsYes(r.BorcAlacakStr),
                Kasa = IsYes(r.KasaStr),
                AlisIade = IsYes(r.AlisIadeStr),
                SatisIade = IsYes(r.SatisIadeStr),
                TahsilatGirisi = IsYes(r.TahsilatGirisiStr),
                OdemeGirisi = IsYes(r.OdemeGirisiStr),
                MusteriCekGirisi = IsYes(r.MusteriCekGirisiStr),
                MusteriSenetGirisi = IsYes(r.MusteriSenetGirisiStr),
                FirmaCekCikisi = IsYes(r.FirmaCekCikisiStr),
                BorcDekontuGirisi = IsYes(r.BorcDekontuGirisiStr),
                AlacakDekontuGirisi = IsYes(r.AlacakDekontuGirisiStr),
                BakiyeGorGirisi = IsYes(r.BakiyeGorGirisiStr),
                FiyatGorGiris = IsYes(r.FiyatGorGirisStr),
                BakiyeGoster = IsYes(r.BakiyeGosterStr),
                GunSonuRaporu = IsYes(r.GunSonuRaporuStr),
                IsletmeDurumRaporu = IsYes(r.IsletmeDurumRaporuStr),
                SatisOzeti = IsYes(r.SatisOzetiStr),
                SatisKarlilik = IsYes(r.SatisKarlilikStr),
                SenetRaporu = IsYes(r.SenetRaporuStr),
                MusteriCekRaporu = IsYes(r.MusteriCekRaporuStr),
                FirmaCekRaporu = IsYes(r.FirmaCekRaporuStr),
                DepoBazindaStokRaporu = IsYes(r.DepoBazindaStokRaporuStr),
                StokGirisCikisRaporu = IsYes(r.StokGirisCikisRaporuStr),
                AylikAnaliz = IsYes(r.AylikAnalizStr)
            };

            if (!string.IsNullOrEmpty(dto.SirketKodlariRaw))
            {
                dto.SirketKodlari = dto.SirketKodlariRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            result.Add(dto);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            result = result.Where(u =>
                u.Kodu.ToLower().Contains(s) ||
                u.Adi.ToLower().Contains(s) ||
                u.OzelKodu.ToLower().Contains(s) ||
                u.Sirketler.ToLower().Contains(s) ||
                u.RoleDisplay.ToLower().Contains(s)
            ).ToList();
        }

        return result;
    }

    private static bool IsYes(object? val) => string.Equals(val?.ToString()?.Trim(), "EVET", StringComparison.OrdinalIgnoreCase);
    private static string ToYesNo(bool val) => val ? "EVET" : "HAYIR";

    /// <summary>
    /// Saves or updates a user in 'user' table and synchronizes 'sirketkullanicilari'
    /// </summary>
    public async Task<(bool Success, string Message)> SaveErpUserAsync(ErpUserDto user, List<string> selectedCompanyCodes, bool isNew)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            if (string.IsNullOrWhiteSpace(user.Kodu))
            {
                user.Kodu = await GetNextErpUserCodeAsync();
            }

            if (isNew)
            {
                var existing = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user WHERE KODU = @Kodu;", new { user.Kodu });
                if (existing > 0)
                {
                    return (false, $"'{user.Kodu}' kodlu kullanıcı zaten mevcut! Lütfen farklı bir kod girin.");
                }

                const string insertUserSql = @"
                    INSERT INTO user (
                        KODU, ADI, SIFRE, YETKI, DURUM, OZELKODU,
                        cari, stok, alis, satis, perakendeiade, perakende, fis,
                        ekstre, borcalacak, kasa, alisiade, satisiade, tahsilatgirisi,
                        odemegirisi, mustericekgirisi, musterisenetgirisi, firmacekcikisi,
                        borcdekontugirisi, alacakdekonturgirisi, bakiyegorgirisi, fiyatgorgiris,
                        bakiyegoster, gunsonuraporu, isletmedurumraporu, satisozeti,
                        satiskarlilik, senetraporu, mustericekraporu, firmacekraporu,
                        depobazindastokraporu, stokgiriscikisrapuru, aylikanaliz, ISLEMTARIHI
                    ) VALUES (
                        @Kodu, @Adi, @Sifre, @Yetki, @Durum, @OzelKodu,
                        @Cari, @Stok, @Alis, @Satis, @PerakendeIade, @Perakende, @Fis,
                        @Ekstre, @BorcAlacak, @Kasa, @AlisIade, @SatisIade, @TahsilatGirisi,
                        @OdemeGirisi, @MusteriCekGirisi, @MusteriSenetGirisi, @FirmaCekCikisi,
                        @BorcDekontuGirisi, @AlacakDekontuGirisi, @BakiyeGorGirisi, @FiyatGorGiris,
                        @BakiyeGoster, @GunSonuRaporu, @IsletmeDurumRaporu, @SatisOzeti,
                        @SatisKarlilik, @SenetRaporu, @MusteriCekRaporu, @FirmaCekRaporu,
                        @DepoBazindaStokRaporu, @StokGirisCikisRaporu, @AylikAnaliz, NOW()
                    );";

                await conn.ExecuteAsync(insertUserSql, new
                {
                    user.Kodu,
                    user.Adi,
                    user.Sifre,
                    user.Yetki,
                    user.Durum,
                    user.OzelKodu,
                    Cari = ToYesNo(user.Cari),
                    Stok = ToYesNo(user.Stok),
                    Alis = ToYesNo(user.Alis),
                    Satis = ToYesNo(user.Satis),
                    PerakendeIade = ToYesNo(user.PerakendeIade),
                    Perakende = ToYesNo(user.Perakende),
                    Fis = ToYesNo(user.Fis),
                    Ekstre = ToYesNo(user.Ekstre),
                    BorcAlacak = ToYesNo(user.BorcAlacak),
                    Kasa = ToYesNo(user.Kasa),
                    AlisIade = ToYesNo(user.AlisIade),
                    SatisIade = ToYesNo(user.SatisIade),
                    TahsilatGirisi = ToYesNo(user.TahsilatGirisi),
                    OdemeGirisi = ToYesNo(user.OdemeGirisi),
                    MusteriCekGirisi = ToYesNo(user.MusteriCekGirisi),
                    MusteriSenetGirisi = ToYesNo(user.MusteriSenetGirisi),
                    FirmaCekCikisi = ToYesNo(user.FirmaCekCikisi),
                    BorcDekontuGirisi = ToYesNo(user.BorcDekontuGirisi),
                    AlacakDekontuGirisi = ToYesNo(user.AlacakDekontuGirisi),
                    BakiyeGorGirisi = ToYesNo(user.BakiyeGorGirisi),
                    FiyatGorGiris = ToYesNo(user.FiyatGorGiris),
                    BakiyeGoster = ToYesNo(user.BakiyeGoster),
                    GunSonuRaporu = ToYesNo(user.GunSonuRaporu),
                    IsletmeDurumRaporu = ToYesNo(user.IsletmeDurumRaporu),
                    SatisOzeti = ToYesNo(user.SatisOzeti),
                    SatisKarlilik = ToYesNo(user.SatisKarlilik),
                    SenetRaporu = ToYesNo(user.SenetRaporu),
                    MusteriCekRaporu = ToYesNo(user.MusteriCekRaporu),
                    FirmaCekRaporu = ToYesNo(user.FirmaCekRaporu),
                    DepoBazindaStokRaporu = ToYesNo(user.DepoBazindaStokRaporu),
                    StokGirisCikisRaporu = ToYesNo(user.StokGirisCikisRaporu),
                    AylikAnaliz = ToYesNo(user.AylikAnaliz)
                });
            }
            else
            {
                const string updateUserSql = @"
                    UPDATE user SET
                        ADI = @Adi,
                        SIFRE = @Sifre,
                        YETKI = @Yetki,
                        DURUM = @Durum,
                        OZELKODU = @OzelKodu,
                        cari = @Cari,
                        stok = @Stok,
                        alis = @Alis,
                        satis = @Satis,
                        perakendeiade = @PerakendeIade,
                        perakende = @Perakende,
                        fis = @Fis,
                        ekstre = @Ekstre,
                        borcalacak = @BorcAlacak,
                        kasa = @Kasa,
                        alisiade = @AlisIade,
                        satisiade = @SatisIade,
                        tahsilatgirisi = @TahsilatGirisi,
                        odemegirisi = @OdemeGirisi,
                        mustericekgirisi = @MusteriCekGirisi,
                        musterisenetgirisi = @MusteriSenetGirisi,
                        firmacekcikisi = @FirmaCekCikisi,
                        borcdekontugirisi = @BorcDekontuGirisi,
                        alacakdekonturgirisi = @AlacakDekontuGirisi,
                        bakiyegorgirisi = @BakiyeGorGirisi,
                        fiyatgorgiris = @FiyatGorGiris,
                        bakiyegoster = @BakiyeGoster,
                        gunsonuraporu = @GunSonuRaporu,
                        isletmedurumraporu = @IsletmeDurumRaporu,
                        satisozeti = @SatisOzeti,
                        satiskarlilik = @SatisKarlilik,
                        senetraporu = @SenetRaporu,
                        mustericekraporu = @MusteriCekRaporu,
                        firmacekraporu = @FirmaCekRaporu,
                        depobazindastokraporu = @DepoBazindaStokRaporu,
                        stokgiriscikisrapuru = @StokGirisCikisRaporu,
                        aylikanaliz = @AylikAnaliz,
                        ISLEMTARIHI = NOW()
                    WHERE KODU = @Kodu;";

                await conn.ExecuteAsync(updateUserSql, new
                {
                    user.Kodu,
                    user.Adi,
                    user.Sifre,
                    user.Yetki,
                    user.Durum,
                    user.OzelKodu,
                    Cari = ToYesNo(user.Cari),
                    Stok = ToYesNo(user.Stok),
                    Alis = ToYesNo(user.Alis),
                    Satis = ToYesNo(user.Satis),
                    PerakendeIade = ToYesNo(user.PerakendeIade),
                    Perakende = ToYesNo(user.Perakende),
                    Fis = ToYesNo(user.Fis),
                    Ekstre = ToYesNo(user.Ekstre),
                    BorcAlacak = ToYesNo(user.BorcAlacak),
                    Kasa = ToYesNo(user.Kasa),
                    AlisIade = ToYesNo(user.AlisIade),
                    SatisIade = ToYesNo(user.SatisIade),
                    TahsilatGirisi = ToYesNo(user.TahsilatGirisi),
                    OdemeGirisi = ToYesNo(user.OdemeGirisi),
                    MusteriCekGirisi = ToYesNo(user.MusteriCekGirisi),
                    MusteriSenetGirisi = ToYesNo(user.MusteriSenetGirisi),
                    FirmaCekCikisi = ToYesNo(user.FirmaCekCikisi),
                    BorcDekontuGirisi = ToYesNo(user.BorcDekontuGirisi),
                    AlacakDekontuGirisi = ToYesNo(user.AlacakDekontuGirisi),
                    BakiyeGorGirisi = ToYesNo(user.BakiyeGorGirisi),
                    FiyatGorGiris = ToYesNo(user.FiyatGorGiris),
                    BakiyeGoster = ToYesNo(user.BakiyeGoster),
                    GunSonuRaporu = ToYesNo(user.GunSonuRaporu),
                    IsletmeDurumRaporu = ToYesNo(user.IsletmeDurumRaporu),
                    SatisOzeti = ToYesNo(user.SatisOzeti),
                    SatisKarlilik = ToYesNo(user.SatisKarlilik),
                    SenetRaporu = ToYesNo(user.SenetRaporu),
                    MusteriCekRaporu = ToYesNo(user.MusteriCekRaporu),
                    FirmaCekRaporu = ToYesNo(user.FirmaCekRaporu),
                    DepoBazindaStokRaporu = ToYesNo(user.DepoBazindaStokRaporu),
                    StokGirisCikisRaporu = ToYesNo(user.StokGirisCikisRaporu),
                    AylikAnaliz = ToYesNo(user.AylikAnaliz)
                });
            }

            // Synchronize sirketkullanicilari
            double.TryParse(user.Kodu, out double numericCode);
            await conn.ExecuteAsync("DELETE FROM sirketkullanicilari WHERE KULLANICIKODU = @numericCode;", new { numericCode });

            if (selectedCompanyCodes != null && selectedCompanyCodes.Count > 0)
            {
                var companies = await GetCompaniesAsync();
                const string insertCompUserSql = @"
                    INSERT INTO sirketkullanicilari (SIRKOD, SIRKETADI, KULLANICIKODU, KULLANICIADI, OZELKODU)
                    VALUES (@SirKod, @SirketAdi, @KullaniciKodu, @KullaniciAdi, @OzelKodu);";

                foreach (var sk in selectedCompanyCodes)
                {
                    var comp = companies.FirstOrDefault(c => c.SirKod.ToString() == sk.Trim());
                    string compName = comp?.Adi ?? (sk == "1" ? "TEK ASYA A.Ş" : sk == "2" ? "LAREX" : "AL SELEVIA");

                    await conn.ExecuteAsync(insertCompUserSql, new
                    {
                        SirKod = sk,
                        SirketAdi = compName,
                        KullaniciKodu = numericCode,
                        KullaniciAdi = user.Adi,
                        OzelKodu = user.OzelKodu
                    });
                }
            }

            return (true, "Kullanıcı başarıyla kaydedildi.");
        }
        catch (Exception ex)
        {
            return (false, $"Kaydetme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles user active/passive status ('EVET' <-> 'HAYIR')
    /// </summary>
    public async Task<(bool Success, string NewStatus)> ToggleErpUserStatusAsync(string kodu)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE user 
                SET DURUM = CASE WHEN COALESCE(DURUM, 'EVET') = 'EVET' THEN 'HAYIR' ELSE 'EVET' END,
                    ISLEMTARIHI = NOW()
                WHERE KODU = @Kodu;";

            await conn.ExecuteAsync(sql, new { Kodu = kodu });

            var status = await conn.ExecuteScalarAsync<string>("SELECT COALESCE(DURUM, 'EVET') FROM user WHERE KODU = @Kodu;", new { Kodu = kodu });
            return (true, status ?? "EVET");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Resets the user's password directly in 'user' table
    /// </summary>
    public async Task<bool> ResetErpUserPasswordAsync(string kodu, string newPassword)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = "UPDATE user SET SIFRE = @Sifre, ISLEMTARIHI = NOW() WHERE KODU = @Kodu;";
            await conn.ExecuteAsync(sql, new { Kodu = kodu, Sifre = newPassword });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes a user and related company assignments
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteErpUserAsync(string kodu)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            double.TryParse(kodu, out double numericCode);
            await conn.ExecuteAsync("DELETE FROM sirketkullanicilari WHERE KULLANICIKODU = @numericCode;", new { numericCode });
            await conn.ExecuteAsync("DELETE FROM kullaniciyetkikodları WHERE KULLANICIKODU = @numericCode;", new { numericCode });
            await conn.ExecuteAsync("DELETE FROM user WHERE KODU = @Kodu;", new { Kodu = kodu });

            return (true, "Kullanıcı başarıyla silindi.");
        }
        catch (Exception ex)
        {
            return (false, $"Silme hatası: {ex.Message}");
        }
    }

    #endregion

    #region Kantar & Mal Kabul Methods

    /// <summary>
    /// Gets recent Kantar / Mal Kabul weighment records from 'kantargiris'
    /// </summary>
    public async Task<List<KantarTartimDto>> GetRecentKantarGirisAsync(int limit = 100, string search = "")
    {
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();

        string sql;
        object parameters;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = @"
                SELECT 
                    IDNO as IdNo,
                    COALESCE(GIRISKODU, 0) as GirisKodu,
                    COALESCE(SIRKOD, '1') as SirKod,
                    COALESCE(GIRISTARIHI, CURDATE()) as Tarih,
                    COALESCE(GIRISSAATI, '') as GirisSaati,
                    COALESCE(CIKISSAATI, '') as CikisSaati,
                    COALESCE(PLAKA, '') as Plaka,
                    COALESCE(FIRKOD, 0) as FirKod,
                    COALESCE(FIRMAADI, '') as FirmaAdi,
                    COALESCE(MALKODU, '') as MalKodu,
                    COALESCE(MALADI, '') as MalAdi,
                    COALESCE(BURUT, 0) as Burut,
                    COALESCE(DARA, 0) as Dara,
                    COALESCE(NET, 0) as Net,
                    COALESCE(FIRESIZ, 0) as Firesiz,
                    COALESCE(DEPOKODU, 0) as DepoKodu,
                    COALESCE(DEPOADI, '') as DepoAdi,
                    COALESCE(ADISOYADI, '') as SoforAdi,
                    COALESCE(TELEFON, '') as Telefon,
                    COALESCE(ACIKLAMA, '') as Aciklama,
                    COALESCE(KANTARADI, '') as KantarAdi,
                    COALESCE(KKULLANICI, '') as Kullanici,
                    COALESCE(ISLEMTARIHI, NOW()) as IslemTarihi
                FROM kantargiris
                ORDER BY IDNO DESC
                LIMIT @Limit;";
            parameters = new { Limit = limit };
        }
        else
        {
            sql = @"
                SELECT 
                    IDNO as IdNo,
                    COALESCE(GIRISKODU, 0) as GirisKodu,
                    COALESCE(SIRKOD, '1') as SirKod,
                    COALESCE(GIRISTARIHI, CURDATE()) as Tarih,
                    COALESCE(GIRISSAATI, '') as GirisSaati,
                    COALESCE(CIKISSAATI, '') as CikisSaati,
                    COALESCE(PLAKA, '') as Plaka,
                    COALESCE(FIRKOD, 0) as FirKod,
                    COALESCE(FIRMAADI, '') as FirmaAdi,
                    COALESCE(MALKODU, '') as MalKodu,
                    COALESCE(MALADI, '') as MalAdi,
                    COALESCE(BURUT, 0) as Burut,
                    COALESCE(DARA, 0) as Dara,
                    COALESCE(NET, 0) as Net,
                    COALESCE(FIRESIZ, 0) as Firesiz,
                    COALESCE(DEPOKODU, 0) as DepoKodu,
                    COALESCE(DEPOADI, '') as DepoAdi,
                    COALESCE(ADISOYADI, '') as SoforAdi,
                    COALESCE(TELEFON, '') as Telefon,
                    COALESCE(ACIKLAMA, '') as Aciklama,
                    COALESCE(KANTARADI, '') as KantarAdi,
                    COALESCE(KKULLANICI, '') as Kullanici,
                    COALESCE(ISLEMTARIHI, NOW()) as IslemTarihi
                FROM kantargiris
                WHERE PLAKA LIKE @Pattern OR FIRMAADI LIKE @Pattern OR MALADI LIKE @Pattern OR GIRISKODU LIKE @Pattern
                ORDER BY IDNO DESC
                LIMIT @Limit;";
            parameters = new { Pattern = $"%{search}%", Limit = limit };
        }

        var list = (await conn.QueryAsync<KantarTartimDto>(sql, parameters)).ToList();
        return list;
    }

    /// <summary>
    /// Saves a Mal Kabul / Kantar weighment entry to both 'kantargiris' and 'kantargirisana'
    /// </summary>
    public async Task<(bool Success, string Message, int IdNo, int GirisKodu)> SaveKantarTartimAsync(KantarTartimDto dto)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            if (dto.GirisKodu <= 0)
            {
                const string nextCodeSql = "SELECT COALESCE(MAX(GIRISKODU), 0) + 1 FROM kantargiris WHERE SIRKOD = @SirKod;";
                dto.GirisKodu = await conn.ExecuteScalarAsync<int>(nextCodeSql, new { dto.SirKod });
            }

            const string insertSql = @"
                INSERT INTO kantargiris (
                    SIRKOD, GIRISKODU, GIRISTARIHI, GIRISSAATI, CIKISTARIHI, CIKISSAATI,
                    PLAKA, FIRKOD, FIRMAADI, MALKODU, MALADI,
                    BURUT, DARA, NET, FIRESIZ, DEPOKODU, DEPOADI,
                    ADISOYADI, TELEFON, ACIKLAMA, KANTARADI, KKULLANICI, ISLEMTARIHI
                ) VALUES (
                    @SirKod, @GirisKodu, @Tarih, @GirisSaati, @Tarih, @CikisSaati,
                    @Plaka, @FirKod, @FirmaAdi, @MalKodu, @MalAdi,
                    @Burut, @Dara, @Net, @Firesiz, @DepoKodu, @DepoAdi,
                    @SoforAdi, @Telefon, @Aciklama, @KantarAdi, @Kullanici, NOW()
                );
                SELECT LAST_INSERT_ID();";

            int insertedId = await conn.ExecuteScalarAsync<int>(insertSql, new
            {
                dto.SirKod,
                dto.GirisKodu,
                dto.Tarih,
                dto.GirisSaati,
                dto.CikisSaati,
                dto.Plaka,
                dto.FirKod,
                dto.FirmaAdi,
                dto.MalKodu,
                dto.MalAdi,
                dto.Burut,
                dto.Dara,
                dto.Net,
                dto.Firesiz,
                dto.DepoKodu,
                dto.DepoAdi,
                dto.SoforAdi,
                dto.Telefon,
                dto.Aciklama,
                dto.KantarAdi,
                dto.Kullanici
            });

            // Also insert into kantargirisana for legacy compatibility
            try
            {
                const string insertAnaSql = @"
                    INSERT INTO kantargirisana (
                        SIRKOD, GIRISKODU, GIRISTARIHI, GIRISSAATI, CIKISTARIHI, CIKISSAATI,
                        PLAKA, FIRKOD, FIRMAADI, MALKODU, MALADI,
                        BURUT, DARA, NET, FIRESIZ, DEPOKODU, DEPOADI,
                        ADISOYADI, TELEFON, ACIKLAMA, KANTARADI, KKULLANICI, ISLEMTARIHI
                    ) VALUES (
                        @SirKod, @GirisKodu, @Tarih, @GirisSaati, @Tarih, @CikisSaati,
                        @Plaka, @FirKod, @FirmaAdi, @MalKodu, @MalAdi,
                        @Burut, @Dara, @Net, @Firesiz, @DepoKodu, @DepoAdi,
                        @SoforAdi, @Telefon, @Aciklama, @KantarAdi, @Kullanici, NOW()
                    );";

                await conn.ExecuteAsync(insertAnaSql, new
                {
                    dto.SirKod,
                    dto.GirisKodu,
                    dto.Tarih,
                    dto.GirisSaati,
                    dto.CikisSaati,
                    dto.Plaka,
                    dto.FirKod,
                    dto.FirmaAdi,
                    dto.MalKodu,
                    dto.MalAdi,
                    dto.Burut,
                    dto.Dara,
                    dto.Net,
                    dto.Firesiz,
                    dto.DepoKodu,
                    dto.DepoAdi,
                    dto.SoforAdi,
                    dto.Telefon,
                    dto.Aciklama,
                    dto.KantarAdi,
                    dto.Kullanici
                });
            }
            catch { }

            return (true, "Kantar tartımı ve mal kabul kaydı başarıyla kaydedildi.", insertedId, dto.GirisKodu);
        }
        catch (Exception ex)
        {
            return (false, $"Kaydetme hatası: {ex.Message}", 0, 0);
        }
    }

    /// <summary>
    /// Gets Cariler (Suppliers / Customers) list for dropdown autocomplete
    /// </summary>
    public async Task<List<LookupItemDto>> GetCarilerForLookupAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = "SELECT CAST(FIRKOD AS CHAR) as Code, FIRMAADI as Name FROM firma WHERE FIRKOD > 0 ORDER BY FIRMAADI ASC LIMIT 500;";
            return (await conn.QueryAsync<LookupItemDto>(sql)).ToList();
        }
        catch
        {
            return new List<LookupItemDto>();
        }
    }

    /// <summary>
    /// Gets Products (Mal / Cins) list for dropdown autocomplete
    /// </summary>
    public async Task<List<LookupItemDto>> GetUrunlerForLookupAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT DISTINCT COALESCE(MALKODU, '') as Code, MALADI as Name 
                FROM kantargiris 
                WHERE MALADI <> '' 
                ORDER BY MALADI ASC 
                LIMIT 200;";
            return (await conn.QueryAsync<LookupItemDto>(sql)).ToList();
        }
        catch
        {
            return new List<LookupItemDto>();
        }
    }

    /// <summary>
    /// Gets distinct recent plates from kantar history for quick autocomplete
    /// </summary>
    public async Task<List<string>> GetRecentPlakalarAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = "SELECT DISTINCT PLAKA FROM kantargiris WHERE PLAKA <> '' ORDER BY IDNO DESC LIMIT 60;";
            return (await conn.QueryAsync<string>(sql)).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets warehouses (depo) list
    /// </summary>
    public async Task<List<LookupItemDto>> GetDepolarForLookupAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = "SELECT DISTINCT CAST(DEPOKODU AS CHAR) as Code, DEPOADI as Name FROM kantargiris WHERE DEPOADI <> '' ORDER BY DEPOKODU ASC;";
            var list = (await conn.QueryAsync<LookupItemDto>(sql)).ToList();
            if (list.Count == 0)
            {
                list.Add(new LookupItemDto { Code = "1", Name = "MERKEZ" });
            }
            return list;
        }
        catch
        {
            return new List<LookupItemDto> { new() { Code = "1", Name = "MERKEZ" } };
        }
    }

    #endregion

    #region Login & Audit Logging Methods

    /// <summary>
    /// Records user login event into 'kullanici_giris_loglari' with machine name and windows username
    /// </summary>
    public async Task<int> LogUserLoginAsync(
        string userKodu,
        string userAdi,
        int sirKod,
        string sirketAdi,
        bool isSuccess,
        string message = "",
        string? customMachineName = null,
        string? customWindowsUser = null)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            string machineName = customMachineName ?? Environment.MachineName;
            string windowsUser = customWindowsUser ?? Environment.UserName;
            string ipAddress = GetLocalIpAddress();
            string sessionName = Environment.GetEnvironmentVariable("SESSIONNAME") ?? "Konsol";
            string durum = isSuccess ? "GİRİŞ BAŞARILI" : "HATALI ŞİFRE DENEMESİ";

            const string sql = @"
                INSERT INTO kullanici_giris_loglari (
                    KULLANICI_KODU, KULLANICI_ADI, SIRKOD, SIRKET_ADI,
                    BILGISAYAR_ADI, WINDOWS_KULLANICI, IP_ADRESI,
                    OTURUM_TURU, RDP_CLIENT_NAME, GIRIS_ZAMANI, DURUM, DETAY
                ) VALUES (
                    @userKodu, @userAdi, @sirKod, @sirketAdi,
                    @machineName, @windowsUser, @ipAddress,
                    @sessionName, '', NOW(), @durum, @message
                );
                SELECT LAST_INSERT_ID();";

            int logId = await conn.ExecuteScalarAsync<int>(sql, new
            {
                userKodu,
                userAdi,
                sirKod = sirKod.ToString(),
                sirketAdi,
                machineName,
                windowsUser,
                ipAddress,
                sessionName,
                durum,
                message
            });

            return logId;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Records user logout timestamp in 'kullanici_giris_loglari'
    /// </summary>
    public async Task LogUserLogoutAsync(int logId)
    {
        if (logId <= 0) return;
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE kullanici_giris_loglari 
                SET CIKIS_ZAMANI = NOW(), DURUM = 'OTURUM KAPANDI' 
                WHERE ID = @logId;";

            await conn.ExecuteAsync(sql, new { logId });
        }
        catch
        {
            // Non-critical
        }
    }

    /// <summary>
    /// Fetches recent user login audit logs
    /// </summary>
    public async Task<List<UserLoginLogDto>> GetRecentUserLoginsAsync(int limit = 200, string search = "", string? filterStatus = null)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            string whereClause = "WHERE 1=1";
            var parameters = new DynamicParameters();
            parameters.Add("Limit", limit);

            if (!string.IsNullOrWhiteSpace(search))
            {
                whereClause += @" AND (
                    KULLANICI_ADI LIKE @Search OR 
                    KULLANICI_KODU LIKE @Search OR 
                    BILGISAYAR_ADI LIKE @Search OR 
                    WINDOWS_KULLANICI LIKE @Search OR 
                    IP_ADRESI LIKE @Search OR 
                    SIRKET_ADI LIKE @Search
                )";
                parameters.Add("Search", $"%{search.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(filterStatus) && filterStatus != "Tümü")
            {
                if (filterStatus == "Aktifler")
                {
                    whereClause += " AND CIKIS_ZAMANI IS NULL AND DURUM LIKE '%BAŞARILI%'";
                }
                else if (filterStatus == "Hatalar")
                {
                    whereClause += " AND DURUM LIKE '%HATALI%'";
                }
                else
                {
                    whereClause += " AND DURUM LIKE @Status";
                    parameters.Add("Status", $"%{filterStatus}%");
                }
            }

            string sql = $@"
                SELECT 
                    ID as Id,
                    KULLANICI_KODU as KullaniciKodu,
                    KULLANICI_ADI as KullaniciAdi,
                    COALESCE(SIRKOD, '1') as SirKod,
                    COALESCE(SIRKET_ADI, '') as SirketAdi,
                    COALESCE(BILGISAYAR_ADI, '') as BilgisayarAdi,
                    COALESCE(WINDOWS_KULLANICI, '') as WindowsKullanici,
                    COALESCE(IP_ADRESI, '') as IpAdresi,
                    COALESCE(OTURUM_TURU, 'Konsol') as OturumTuru,
                    COALESCE(RDP_CLIENT_NAME, '') as RdpClientName,
                    GIRIS_ZAMANI as GirisZamani,
                    CIKIS_ZAMANI as CikisZamani,
                    COALESCE(DURUM, 'GİRİŞ BAŞARILI') as Durum,
                    COALESCE(DETAY, '') as Detay
                FROM kullanici_giris_loglari
                {whereClause}
                ORDER BY GIRIS_ZAMANI DESC
                LIMIT @Limit;";

            var list = (await conn.QueryAsync<UserLoginLogDto>(sql, parameters)).ToList();
            return list;
        }
        catch
        {
            return new List<UserLoginLogDto>();
        }
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    #endregion

    #region Kantar Settings per User

    /// <summary>
    /// Gets scale settings specifically mapped to the given user.
    /// Returns KantarAktif = false if user has no configured scale.
    /// </summary>
    public async Task<KantarAyarDto> GetKullaniciKantarAyariAsync(string kullaniciKodu)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    KULLANICI_KODU as KullaniciKodu,
                    COALESCE(KULLANICI_ADI, '') as KullaniciAdi,
                    COALESCE(KANTAR_AKTIF, 0) = 1 as KantarAktif,
                    COALESCE(KANTAR_ADI, 'Tek Asya Merkez Kantarı') as KantarAdi,
                    COALESCE(SUBE_KODU, 'MERKEZ') as SubeKodu,
                    COALESCE(LOKAL_IP, IP_ADRESI, '192.168.60.116') as LokalIp,
                    COALESCE(LOKAL_PORT, PORT, 8080) as LokalPort,
                    COALESCE(VPN_IP, '192.168.45.116') as VpnIp,
                    COALESCE(VPN_PORT, 8080) as VpnPort,
                    COALESCE(BAGLANTI_MODU, 'Auto') as BaglantiModu,
                    COALESCE(TIMEOUT_MS, 3000) as TimeoutMs
                FROM kullanici_kantar_ayarlari
                WHERE KULLANICI_KODU = @kullaniciKodu
                LIMIT 1;";

            var res = await conn.QueryFirstOrDefaultAsync<KantarAyarDto>(sql, new { kullaniciKodu });
            if (res != null) return res;
        }
        catch { }

        // Fallback: Default scale for Admin / Ali SARIGÜL (100) or ALİ ORTAKÇI (66)
        bool defaultActive = (kullaniciKodu == "100" || kullaniciKodu == "66" || kullaniciKodu == "1");
        return new KantarAyarDto
        {
            KullaniciKodu = kullaniciKodu,
            KullaniciAdi = "",
            KantarAktif = defaultActive,
            KantarAdi = "Tek Asya Merkez Kantarı",
            SubeKodu = "MERKEZ",
            LokalIp = "192.168.60.116",
            LokalPort = 8080,
            VpnIp = "192.168.45.116",
            VpnPort = 8080,
            BaglantiModu = "Auto",
            TimeoutMs = 3000
        };
    }

    /// <summary>
    /// Saves or updates the user-specific scale connection settings in MariaDB
    /// </summary>
    public async Task<bool> SaveKullaniciKantarAyariAsync(KantarAyarDto ayar)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO kullanici_kantar_ayarlari (
                    KULLANICI_KODU, KULLANICI_ADI, KANTAR_AKTIF, KANTAR_ADI, SUBE_KODU,
                    IP_ADRESI, PORT, LOKAL_IP, LOKAL_PORT, VPN_IP, VPN_PORT, BAGLANTI_MODU, TIMEOUT_MS
                ) VALUES (
                    @KullaniciKodu, @KullaniciAdi, @KantarAktif, @KantarAdi, @SubeKodu,
                    @LokalIp, @LokalPort, @LokalIp, @LokalPort, @VpnIp, @VpnPort, @BaglantiModu, @TimeoutMs
                ) ON DUPLICATE KEY UPDATE 
                    KULLANICI_ADI = @KullaniciAdi,
                    KANTAR_AKTIF = @KantarAktif,
                    KANTAR_ADI = @KantarAdi,
                    SUBE_KODU = @SubeKodu,
                    IP_ADRESI = @LokalIp,
                    PORT = @LokalPort,
                    LOKAL_IP = @LokalIp,
                    LOKAL_PORT = @LokalPort,
                    VPN_IP = @VpnIp,
                    VPN_PORT = @VpnPort,
                    BAGLANTI_MODU = @BaglantiModu,
                    TIMEOUT_MS = @TimeoutMs;";

            int rows = await conn.ExecuteAsync(sql, ayar);
            return rows > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MacroService] SaveKullaniciKantarAyariAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all registered branch weighbridge stations with multi-IP routing
    /// </summary>
    public async Task<List<KantarTanimDto>> GetKantarTanimlariAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    Id, KantarKodu, KantarAdi, SubeAdi, SubnetPrefix, 
                    LokalIp, LokalPort, VpnIp, VpnPort, IndikatorModeli, 
                    BaglantiModu, Aktif, Aciklama
                FROM kantar_tanimlari
                WHERE Aktif = 1
                ORDER BY Id ASC;";

            var list = (await conn.QueryAsync<KantarTanimDto>(sql)).ToList();
            if (list.Count > 0) return list;
        }
        catch { }

        // Fallback default definitions
        return new List<KantarTanimDto>
        {
            new() { Id = 1, KantarKodu = "MERKEZ", KantarAdi = "Tek Asya Merkez Kantarı (Antakya)", SubeAdi = "Merkez Fabrika & Mal Kabul", SubnetPrefix = "192.168.60.", LokalIp = "192.168.60.116", LokalPort = 8080, VpnIp = "192.168.60.116", VpnPort = 8080, BaglantiModu = "Auto" },
            new() { Id = 2, KantarKodu = "DORTYOL", KantarAdi = "Dörtyol Şube Kantarı", SubeAdi = "Dörtyol Narenciye İşleme", SubnetPrefix = "192.168.45.", LokalIp = "192.168.45.116", LokalPort = 8080, VpnIp = "192.168.45.116", VpnPort = 8080, BaglantiModu = "Auto" },
            new() { Id = 3, KantarKodu = "MERSIN", KantarAdi = "Mersin / Tarsus Depo Kantarı", SubeAdi = "Mersin Soğuk Hava Deposu", SubnetPrefix = "192.168.31.", LokalIp = "192.168.31.116", LokalPort = 8080, VpnIp = "192.168.31.116", VpnPort = 8080, BaglantiModu = "Auto" }
        };
    }

    /// <summary>
    /// Saves or updates a weighbridge profile in erp.kantar_tanimlari
    /// </summary>
    public async Task<bool> SaveKantarTanimAsync(KantarTanimDto tanim)
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO kantar_tanimlari (
                    KantarKodu, KantarAdi, SubeAdi, SubnetPrefix, LokalIp, LokalPort, VpnIp, VpnPort, IndikatorModeli, BaglantiModu, Aktif, Aciklama
                ) VALUES (
                    @KantarKodu, @KantarAdi, @SubeAdi, @SubnetPrefix, @LokalIp, @LokalPort, @VpnIp, @VpnPort, @IndikatorModeli, @BaglantiModu, @Aktif, @Aciklama
                ) ON DUPLICATE KEY UPDATE 
                    KantarAdi = @KantarAdi,
                    SubeAdi = @SubeAdi,
                    SubnetPrefix = @SubnetPrefix,
                    LokalIp = @LokalIp,
                    LokalPort = @LokalPort,
                    VpnIp = @VpnIp,
                    VpnPort = @VpnPort,
                    IndikatorModeli = @IndikatorModeli,
                    BaglantiModu = @BaglantiModu,
                    Aktif = @Aktif,
                    Aciklama = @Aciklama;";

            int rows = await conn.ExecuteAsync(sql, tanim);
            return rows > 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// Gets list of all scales registered in table 'kantar'
    /// </summary>
    public async Task<List<LookupItemDto>> GetRegisteredScalesAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    CAST(KODU AS CHAR) as Code, 
                    COALESCE(ADI, '') as Name 
                FROM kantar 
                ORDER BY KODU ASC;";

            var list = (await conn.QueryAsync<LookupItemDto>(sql)).ToList();
            if (list.Count == 0)
            {
                list.Add(new LookupItemDto { Code = "35", Name = "TEKASYA MERKEZ KANTARI-ANTAKYA" });
            }
            return list;
        }
        catch
        {
            return new List<LookupItemDto>
            {
                new() { Code = "35", Name = "TEKASYA MERKEZ KANTARI-ANTAKYA" },
                new() { Code = "8", Name = "GÖKALP KANTARI-ERDEMLİ" },
                new() { Code = "19", Name = "UYSAL KARDEŞLER KANTARI-TARSUS" }
            };
        }
    }

    #endregion
}


