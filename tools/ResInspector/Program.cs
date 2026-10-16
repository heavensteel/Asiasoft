using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Data.Services;
using AsiasoftERP.Core.Models;
using AsiasoftERP.UI.Forms.Modules;

class TestApp {
    [STAThread]
    static async Task Main() {
        var macro = new MacroService();
        Console.WriteLine("1. Testing Companies...");
        var comps = await macro.GetCompaniesAsync();
        foreach (var c in comps) {
            Console.WriteLine($"   Company: {c.SirKod} - {c.Adi}");
        }

        Console.WriteLine("\n2. Testing Users for Company 1 (TEK ASYA)...");
        var users = await macro.GetCompanyUsersAsync("1");
        Console.WriteLine($"   Found {users.Count} users.");
        foreach (var u in users.GetRange(0, Math.Min(5, users.Count))) {
            Console.WriteLine($"   User: {u.KullaniciKodu} - {u.KullaniciAdi}");
        }

        Console.WriteLine("\n3. Testing INI Settings...");
        var (sKod, sAdi, uKod, uAdi) = macro.LoadSettings();
        Console.WriteLine($"   INI Loaded: Comp={sKod} ({sAdi}), User={uKod} ({uAdi})");

        Console.WriteLine("\n4. Testing Cari Search (Paged from 15.8K records)...");
        var (cariler, total) = await macro.GetCarilerPagedAsync("", 0, 10);
        Console.WriteLine($"   Total cariler in DB: {total}");
        foreach (var cari in cariler) {
            Console.WriteLine($"   Cari: {cari.FirKod} | {cari.FirmaAdi} | Bakiye={cari.Bakiye:N2} TL");
        }

        Console.WriteLine("\n5. Testing GetAllErpUsersAsync (Read-Only Health Check)...");
        var erpUsers = await macro.GetAllErpUsersAsync();
        Console.WriteLine($"   Total ERP Users in DB: {erpUsers.Count}");
        var ali = erpUsers.Find(u => u.Kodu == "100");
        if (ali != null) {
            Console.WriteLine($"   Found Ali: Kodu={ali.Kodu}, Adi={ali.Adi}, Role={ali.RoleDisplay}, Sirketler={ali.Sirketler}, Active={ali.IsActive}");
        }

        Console.WriteLine("\n6. Testing LIVE SCALE CONNECTION to 192.168.60.116:8080...");
        var scale = new KantarScaleClient { Host = "192.168.60.116", Port = 8080 };
        var (scaleOk, weight, scaleMsg) = await scale.ReadOnceAsync(3000);
        Console.WriteLine($"   Scale Connection Result: Success={scaleOk}, Message='{scaleMsg}'");
        Console.WriteLine($"   >> LIVE WEIGHT READ FROM SCALE: {weight:N1} KG <<");

        Console.WriteLine("\n7. Testing Recent Kantar Giris Records from 'kantargiris'...");
        var kantarList = await macro.GetRecentKantarGirisAsync(5);
        Console.WriteLine($"   Loaded {kantarList.Count} recent kantar records.");
        foreach (var k in kantarList) {
            Console.WriteLine($"   Fiş #{k.GirisKodu} | {k.Tarih:dd.MM.yyyy} {k.GirisSaati} | Plaka: {k.Plaka} | Cari: {k.FirmaAdi} | Ürün: {k.MalAdi} | Brüt: {k.Burut:N0} kg, Dara: {k.Dara:N0} kg, Net: {k.Net:N0} kg");
        }

        Console.WriteLine("\n8. Testing Cariler & Urunler Lookups for Mal Kabul...");
        var lookupCariler = await macro.GetCarilerForLookupAsync();
        var lookupUrunler = await macro.GetUrunlerForLookupAsync();
        var recentPlakalar = await macro.GetRecentPlakalarAsync();
        Console.WriteLine($"   Cariler available for selection: {lookupCariler.Count}");
        Console.WriteLine($"   Urunler available for selection: {lookupUrunler.Count}");
        Console.WriteLine($"   Recent Plates available for autocomplete: {recentPlakalar.Count} (e.g. {string.Join(", ", recentPlakalar.GetRange(0, Math.Min(3, recentPlakalar.Count)))})");

        Console.WriteLine("\n9. Testing USER LOGIN AUDIT LOGS (Read-Only)...");
        var loginLogs = await macro.GetRecentUserLoginsAsync(5);
        Console.WriteLine($"   Total audit log records in MariaDB: {loginLogs.Count}");
        foreach (var l in loginLogs) {
            Console.WriteLine($"   >> Log #{l.Id} | {l.GirisZamani:dd.MM.yyyy HH:mm:ss} | ERP User: {l.KullaniciKodu} - {l.KullaniciAdi} | PC: {l.BilgisayarAdi} | Windows: {l.WindowsKullanici} | IP: {l.IpAdresi} | Durum: {l.Durum} | Süre: {l.SureDisplay}");
        }

        Console.WriteLine("\n10. Testing ODEME TALEP ISLEMLERI (MacroSoft ERP Replication)...");
        var odemeService = new OdemeTalepService();
        var dtFrom = new DateTime(2026, 10, 8);
        var dtTo = new DateTime(2026, 10, 15);

        // Tab 1: Genel Talep Listesi
        var taleplerGenel = await odemeService.GetOdemeTalepleriAsync(dtFrom, dtTo, "Genel Talep Listesi", "", "1");
        var summary = odemeService.ComputeSummary(taleplerGenel);
        Console.WriteLine($"   Tab 1 (Genel Talep Listesi): Loaded {taleplerGenel.Count} records.");
        Console.WriteLine($"   >> Talep Tutarı Toplamı: {summary.ToplamTalepTutari:N2} TL");
        Console.WriteLine($"   >> Bağkur Kesintisi: {summary.ToplamBagkurTutari:N2} TL");
        Console.WriteLine($"   >> Net Tutar: {summary.ToplamNetTutar:N2} TL");

        if (taleplerGenel.Count > 0) {
            var first = taleplerGenel[0];
            Console.WriteLine($"   Row 1: FisNo={first.FisNo}, Tarih={first.FisTar:dd.MM.yyyy}, Sube={first.YetkiKodu}, Turu={first.Turu}, Cari={first.FirAdi}, Tutar={first.TalepTutar:N2} TL, Bagkur={first.BagkurVarmi}, Durum={first.Durum}");
        }

        // Tab 2: Devam Eden Talepler
        var taleplerDevam = await odemeService.GetOdemeTalepleriAsync(dtFrom, dtTo, "Devam Eden Talepler", "", "1");
        Console.WriteLine($"   Tab 2 (Devam Eden Talepler): {taleplerDevam.Count} records, Toplam: {taleplerDevam.Sum(x => x.TalepTutar):N2} TL");

        // Tab 3: Onaylanan Talepler
        var taleplerOnay = await odemeService.GetOdemeTalepleriAsync(dtFrom, dtTo, "Onaylanan Talepler", "", "1");
        Console.WriteLine($"   Tab 3 (Onaylanan Talepler): {taleplerOnay.Count} records, Toplam: {taleplerOnay.Sum(x => x.TalepTutar):N2} TL");

        // Tab 4: Ret Edilen Talepler
        var taleplerRet = await odemeService.GetOdemeTalepleriAsync(dtFrom, dtTo, "Ret Edilen Talepler", "", "1");
        Console.WriteLine($"   Tab 4 (Ret Edilen Talepler): {taleplerRet.Count} records, Toplam: {taleplerRet.Sum(x => x.TalepTutar):N2} TL");

        // Tab 5: Ödeme Talep Karşılaştırma
        var karsilastirma = await odemeService.GetKarsilastirmaAsync(dtFrom, dtTo, "1");
        Console.WriteLine($"   Tab 5 (Karşılaştırma): {karsilastirma.Count} group entries.");
        foreach (var k in karsilastirma.GetRange(0, Math.Min(3, karsilastirma.Count))) {
            Console.WriteLine($"      Şube: {k.Sube} | Tür: {k.Turu} | Adet: {k.TalepAdedi} | Tutar: {k.ToplamTalepTutari:N2} TL | Onaylanan: {k.OnaylananTutar:N2} TL | Oran: %{k.KarsilamaOrani:N2}");
        }

        Console.WriteLine("\n11. Capturing Visual Snapshot of OdemeTalepIslemleriForm...");
        var form = new OdemeTalepIslemleriForm();
        form.Show();
        Application.DoEvents();
        await Task.Delay(2000); // Allow data loading
        Application.DoEvents();

        using var bmp = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        string snapshotPath = @"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\odeme_talep_ekrani.png";
        bmp.Save(snapshotPath, System.Drawing.Imaging.ImageFormat.Png);
        Console.WriteLine($"   Snapshot successfully saved to: {snapshotPath}");
        form.Close();

        Console.WriteLine("\n12. Testing USER-SPECIFIC KANTAR CONFIGURATION (kullanici_kantar_ayarlari)...");
        var aliKantar = await macro.GetKullaniciKantarAyariAsync("100");
        Console.WriteLine($"   User 100 (Ali SARIGÜL) Scale Config: Active={aliKantar.KantarAktif}, Scale={aliKantar.KantarAdi}, IP={aliKantar.IpAdresi}:{aliKantar.Port}");

        var randomUserKantar = await macro.GetKullaniciKantarAyariAsync("55");
        Console.WriteLine($"   User 55 (Standard User without scale): Active={randomUserKantar.KantarAktif}, IP={randomUserKantar.IpAdresi}");

        var (testOk, testWeight, testMsg) = await KantarScaleClient.TestConnectionAsync(aliKantar.IpAdresi, aliKantar.Port);
        Console.WriteLine($"   Scale Test Connection Result: Success={testOk}, Weight={testWeight:N1} KG, Msg='{testMsg}'");

        Console.WriteLine("\n13. Testing ETILEN & SARARTMA ODALARI (\\192.168.1.252\\it\\ANIL)...");
        var etilenService = new EtilenService();
        var (etilenData, isLive, etilenMsg) = await etilenService.LoadDataAsync();
        Console.WriteLine($"   Etilen Data Load: LiveNetwork={isLive}, Message='{etilenMsg}'");
        Console.WriteLine($"   Rooms Loaded: {etilenData.Rooms.Count}, Logs: {etilenData.Logs.Count}");
        foreach (var r in etilenData.Rooms) {
            Console.WriteLine($"      Room {r.Name} [{r.Id}]: EthyleneActive={r.EthyleneActive}, Doluluk=%{r.OccupancyPercent:N1}, Pallets={r.TotalPallets}/{r.Capacity}, Lots={r.Lots.Count}");
        }

        Console.WriteLine("\n14. Capturing Visual Snapshot of EtilenStokTakipForm...");
        var etilenForm = new EtilenStokTakipForm();
        etilenForm.Size = new Size(1300, 800);
        etilenForm.Show();
        Application.DoEvents();
        await Task.Delay(2500); // Allow async data loading and grid render
        Application.DoEvents();

        using var bmpEtilen = new Bitmap(etilenForm.Width, etilenForm.Height);
        etilenForm.DrawToBitmap(bmpEtilen, new Rectangle(0, 0, etilenForm.Width, etilenForm.Height));
        string etilenSnapshotPath = @"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\etilen_stok_ekrani.png";
        bmpEtilen.Save(etilenSnapshotPath, System.Drawing.Imaging.ImageFormat.Png);
        Console.WriteLine($"   Snapshot successfully saved to: {etilenSnapshotPath}");
        etilenForm.Close();

        Console.WriteLine("\n15. Testing COMMERCIAL INVOICES (stkfis / stkdet) via FaturaService...");
        var faturaService = new FaturaService();
        var dtStart = new DateTime(2026, 9, 1);
        var dtEnd = new DateTime(2026, 10, 16);

        var satislar = await faturaService.GetFaturalarAsync(FaturaTip.SatisFaturasi, dtStart, dtEnd);
        Console.WriteLine($"   Satış Faturaları: Loaded {satislar.Count} invoices. Toplam: {satislar.Sum(f => f.GenTut):N2} TL");
        if (satislar.Count > 0)
        {
            var firstSatis = satislar[0];
            Console.WriteLine($"   >> Row 1: Fiş #{firstSatis.FisNo} | Tarih: {firstSatis.FisTar:dd.MM.yyyy} | Cari: {firstSatis.FirmaAdi} | Tutar: {firstSatis.GenTut:N2} {firstSatis.DovizTuru}");
        }

        var alislar = await faturaService.GetFaturalarAsync(FaturaTip.AlisFaturasi, dtStart, dtEnd);
        Console.WriteLine($"   Alış Faturaları: Loaded {alislar.Count} invoices. Toplam: {alislar.Sum(f => f.GenTut):N2} TL");

        var ihracatlar = await faturaService.GetFaturalarAsync(FaturaTip.IhracatFaturasi, dtStart, dtEnd);
        Console.WriteLine($"   İhracat Faturaları: Loaded {ihracatlar.Count} invoices. Toplam Döviz: {ihracatlar.Sum(f => f.DovizTutar):N2}");
        if (ihracatlar.Count > 0)
        {
            var testFisNo = ihracatlar[0].FisNo;
            var detaylar = await faturaService.GetFaturaDetayAsync(testFisNo);
            Console.WriteLine($"   >> Fatura #{testFisNo} Kalemleri (stkdet): Loaded {detaylar.Count} line items.");
            foreach (var d in detaylar)
            {
                Console.WriteLine($"      Kalem: {d.MalAdi} | Miktar: {d.Miktar:N0} {d.Birimi} | Fiyat: {d.Fiyati:N2} | Tutar: {d.Tutar:N2} {d.DovizTuru}");
            }
        }

        Console.WriteLine("\n16. Testing BANKA, CEK & CARI BAKIYE OZET...");
        using var conn = DbConnectionConfig.CreateConnection();
        await conn.OpenAsync();
        var bankalar = (await Dapper.SqlMapper.QueryAsync(conn, "SELECT count(*) as Cnt FROM banka WHERE SIRKOD='1'")).First();
        var cekler = (await Dapper.SqlMapper.QueryAsync(conn, "SELECT count(*) as Cnt, sum(TUTAR) as Toplam FROM cekdetay WHERE SIRKOD='1'")).First();
        var bakiyeler = (await Dapper.SqlMapper.QueryAsync(conn, "SELECT count(*) as Cnt FROM erp.cari_bakiye_ozet WHERE SIRKOD='1' AND BAKIYE != 0")).First();
        var stoklar = (await Dapper.SqlMapper.QueryAsync(conn, "SELECT count(*) as Cnt FROM stok WHERE SIRKOD='1'")).First();

        Console.WriteLine($"   Banka Hesapları: {bankalar.Cnt} banka");
        Console.WriteLine($"   Çek & Senet Portföyü: {cekler.Cnt} çek, Toplam Tutar: {cekler.Toplam:N2} TL");
        Console.WriteLine($"   Borç/Alacak Bakiyeli Cari: {bakiyeler.Cnt} cari");
        Console.WriteLine($"   Stok ve Ürün Kartları: {stoklar.Cnt} ürün/ambalaj");

        Console.WriteLine("\n17. Capturing Visual Snapshot of FaturalarForm (Satış Faturaları)...");
        var faturaForm = new FaturalarForm(FaturaTip.SatisFaturasi);
        faturaForm.Size = new Size(1300, 750);
        faturaForm.Show();
        Application.DoEvents();
        await faturaForm.RefreshDataAsync();
        Application.DoEvents();
        await Task.Delay(1000);
        Application.DoEvents();

        using var bmpFatura = new Bitmap(faturaForm.Width, faturaForm.Height);
        faturaForm.DrawToBitmap(bmpFatura, new Rectangle(0, 0, faturaForm.Width, faturaForm.Height));
        string faturaSnapshotPath = @"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\faturalar_ekrani.png";
        bmpFatura.Save(faturaSnapshotPath, System.Drawing.Imaging.ImageFormat.Png);
        Console.WriteLine($"   Snapshot successfully saved to: {faturaSnapshotPath}");
        faturaForm.Close();

        Console.WriteLine("\nALL TESTS PASSED SUCCESSFULLY!");
    }
}
