using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Forms;
using AsiasoftERP.UI.Forms.Modules;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log"), $"[CRASH] {DateTime.Now}: {e.ExceptionObject}\n"); } catch { }
        };

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log"), $"[THREAD ERROR] {DateTime.Now}: {e.Exception}\n"); } catch { }
        };

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();

            // Setup Turkish codepages & culture
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var culture = new System.Globalization.CultureInfo("tr-TR");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            // Configure local database connection for Dapper and MariaDB
            DbConnectionConfig.ConnectionString = "Server=127.0.0.1;Port=3306;Database=erp;User ID=root;Password=;AllowUserVariables=true;Pooling=true;MinPoolSize=5;MaxPoolSize=100;ConnectionTimeout=15;CharSet=utf8mb4;";

            if (args.Length > 0 && args[0] == "--snapshot-notifications")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                var main = new MainForm();
                main.StartPosition = FormStartPosition.Manual;
                main.Location = new Point(0, 0);
                main.Size = new Size(1440, 900);
                main.Show();
                for (int i = 0; i < 20; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                // Add a fresh notification
                NotificationService.NotifySuccess(
                    "Kantar Tartımı Tamamlandı",
                    "33 ABN 421 plakalı araç 24.850 KG tartım fişi kesildi.",
                    "MalKabul",
                    showToast: false);

                // Open notification dropdown
                var dropdown = new NotificationDropdown();
                dropdown.AutoCloseOnDeactivate = false;
                dropdown.StartPosition = FormStartPosition.Manual;
                dropdown.Location = new Point(1030, 60);
                dropdown.Show();
                for (int i = 0; i < 15; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                // Capture Dropdown Form
                using (var bmpDrop = new Bitmap(dropdown.Width, dropdown.Height))
                {
                    dropdown.DrawToBitmap(bmpDrop, new Rectangle(0, 0, dropdown.Width, dropdown.Height));
                    bmpDrop.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\bildirimler_dropdown_ekrani.png");
                }

                // Capture Toast Form
                var toastItem = new NotificationItem
                {
                    Title = "Yükleme Programı Onaylandı",
                    Message = "Kuveyt & Romanya sevkiyat yükleme planı onaylandı.",
                    Type = NotificationType.Success,
                    Timestamp = DateTime.Now,
                    ModuleKey = "YuklemeProgrami"
                };
                var toast = new ToastNotificationForm(toastItem);
                toast.StartPosition = FormStartPosition.Manual;
                toast.Location = new Point(1050, 780);
                toast.Show();
                for (int i = 0; i < 15; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpToast = new Bitmap(toast.Width, toast.Height))
                {
                    toast.DrawToBitmap(bmpToast, new Rectangle(0, 0, toast.Width, toast.Height));
                    bmpToast.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\bildirim_toast_ekrani.png");
                }

                // Full screen capture with dropdown & toast overlay
                using (var fullBmp = new Bitmap(main.Width, main.Height))
                {
                    using (var g = Graphics.FromImage(fullBmp))
                    {
                        main.DrawToBitmap(fullBmp, new Rectangle(0, 0, main.Width, main.Height));

                        using (var dropBmp = new Bitmap(dropdown.Width, dropdown.Height))
                        {
                            dropdown.DrawToBitmap(dropBmp, new Rectangle(0, 0, dropdown.Width, dropdown.Height));
                            g.DrawImage(dropBmp, dropdown.Location.X - main.Location.X, dropdown.Location.Y - main.Location.Y);
                        }

                        using (var toastBmp = new Bitmap(toast.Width, toast.Height))
                        {
                            toast.DrawToBitmap(toastBmp, new Rectangle(0, 0, toast.Width, toast.Height));
                            g.DrawImage(toastBmp, toast.Location.X - main.Location.X, toast.Location.Y - main.Location.Y);
                        }
                    }
                    fullBmp.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\bildirimler_ve_toast_ekrani.png");
                }

                toast.Close();
                dropdown.Close();
                main.Close();
                Console.WriteLine("SNAPSHOT_NOTIFICATIONS_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-kantar-vpn")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                var dlg = new KantarAyarlariDialog("100", "Ali SARIGÜL");
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(100, 80);
                dlg.Show();
                for (int i = 0; i < 20; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmp = new Bitmap(dlg.Width, dlg.Height))
                {
                    dlg.DrawToBitmap(bmp, new Rectangle(0, 0, dlg.Width, dlg.Height));
                    bmp.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\kantar_ayarlari_vpn_ekrani.png");
                }

                dlg.Close();
                Console.WriteLine("SNAPSHOT_KANTAR_VPN_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-malkabul-dual")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                // Emulate central DB at 192.168.1.250 and branch scale at 192.168.45.116
                DbConnectionConfig.Server = "192.168.1.250";

                var form = new MalKabulForm();
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(50, 50);
                form.Size = new Size(1320, 840);
                form.Show();

                // Emulate live weight stream from 192.168.45.116
                var scaleClient = KantarScaleClient.Instance;
                scaleClient.LokalIp = "192.168.60.116";
                scaleClient.VpnIp = "192.168.45.116";
                scaleClient.Mode = "VPN";

                // Set status text to illustrate live dual channel
                var lblScaleStatus = form.Controls.Find("_lblScaleStatus", true).OfType<Label>().FirstOrDefault();
                if (lblScaleStatus != null)
                {
                    lblScaleStatus.Text = "🟢 192.168.45.116:8080 (Dörtyol via FortiVPN) Canlı";
                    lblScaleStatus.ForeColor = Color.FromArgb(21, 128, 61);
                    lblScaleStatus.BackColor = Color.FromArgb(220, 252, 231);
                }

                var lblDbStatus = form.Controls.Find("_lblDbStatus", true).OfType<Label>().FirstOrDefault();
                if (lblDbStatus != null)
                {
                    lblDbStatus.Text = "🗄️ Veritabanı: 192.168.1.250 (Canlı Eşzamanlı)";
                }

                var lblLiveWeight = form.Controls.Find("_lblLiveWeight", true).OfType<Label>().FirstOrDefault();
                if (lblLiveWeight != null)
                {
                    lblLiveWeight.Text = "24.850 KG";
                }

                var lblLiveTag = form.Controls.Find("_lblLiveTag", true).OfType<Label>().FirstOrDefault();
                if (lblLiveTag != null)
                {
                    lblLiveTag.Text = "● CANLI AKIŞ: 192.168.45.116:8080 via FortiVPN | EŞZAMANLI KAYIT: 192.168.1.250";
                }

                for (int i = 0; i < 20; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                    bmp.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\kantar_ve_db_eszamanli_akis.png");
                }

                form.Close();
                Console.WriteLine("SNAPSHOT_MALKABUL_DUAL_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-yukleme")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                var main = new MainForm();
                main.StartPosition = FormStartPosition.Manual;
                main.Location = new Point(0, 0);
                main.Size = new Size(1440, 900);
                main.Show();
                Application.DoEvents();

                main.OpenModuleTab("YuklemeProgrami", "🚚 Yükleme Programı", () => new YuklemeProgramiForm());
                for (int i = 0; i < 20; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(50); }

                using (var bmp = new Bitmap(main.Width, main.Height))
                {
                    main.DrawToBitmap(bmp, new Rectangle(0, 0, main.Width, main.Height));
                    bmp.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\yukleme_programi_list_ekrani.png");
                }

                // Find the DataGridView inside YuklemeProgramiForm
                var yuklemeTab = main.Controls.Find("_tabControl", true).OfType<TabControl>().FirstOrDefault()?.SelectedTab;
                var yuklemeGrid = yuklemeTab?.Controls.OfType<YuklemeProgramiForm>().FirstOrDefault()?.Controls.Find("_grid", true).OfType<DataGridView>().FirstOrDefault();

                if (yuklemeGrid == null)
                {
                    // Fallback search in all child controls
                    yuklemeGrid = main.Controls.OfType<Control>().SelectMany(c => GetAllChildren(c)).OfType<DataGridView>().FirstOrDefault();
                }

                if (yuklemeGrid != null)
                {
                    // Open GridParametersDialog snapshot
                    var paramDlg = new GridParametersDialog(yuklemeGrid, "YuklemeProgramiForm", "GridYukleme");
                    paramDlg.StartPosition = FormStartPosition.Manual;
                    paramDlg.Location = new Point(100, 80);
                    paramDlg.Size = new Size(820, 620);
                    paramDlg.Show();
                    for (int i = 0; i < 15; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(40); }

                    using (var bmpParam = new Bitmap(paramDlg.Width, paramDlg.Height))
                    {
                        paramDlg.DrawToBitmap(bmpParam, new Rectangle(0, 0, paramDlg.Width, paramDlg.Height));
                        bmpParam.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\grid_parameters_dialog_ekrani.png");
                    }
                    paramDlg.Close();

                    // Open ColumnFilterPopup snapshot
                    var colDurum = yuklemeGrid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText.Contains("Durum")) ?? yuklemeGrid.Columns[0];
                    var filterPopup = new GridColumnFilterPopup(colDurum.Name, colDurum.HeaderText, new List<string> { "Onaylandı", "Taslak", "Yüklendi" }, new HashSet<string> { "Onaylandı" });
                    filterPopup.StartPosition = FormStartPosition.Manual;
                    filterPopup.Location = new Point(300, 200);
                    filterPopup.Show();
                    for (int i = 0; i < 15; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(40); }

                    using (var bmpFilter = new Bitmap(filterPopup.Width, filterPopup.Height))
                    {
                        filterPopup.DrawToBitmap(bmpFilter, new Rectangle(0, 0, filterPopup.Width, filterPopup.Height));
                        bmpFilter.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\grid_column_filter_popup_ekrani.png");
                    }
                    filterPopup.Close();
                }

                main.Close();
                Console.WriteLine("SNAPSHOT_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-3x3-grid")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                var svc = new YuklemeProgramiService();
                var p33 = new YuklemeProgramiDto
                {
                    ProgramNo = "YP-2026-3X3-01",
                    Tarih = DateTime.Today,
                    Baslik = "KÖRFEZ & ORTADOĞU 9'LU MULTI-SEVKİYAT",
                    SatirSayisi = 3,
                    SutunSayisi = 3,
                    Hazirlayan = "Ali SARIGÜL",
                    Onaylayan = "İHRACAT BÖLÜMÜ",
                    Durum = "Onaylandı"
                };

                string[] destinations = new[]
                {
                    "KUVEYT / AL MASSAR", "MASKAT / BIN MAYEZ", "KATAR / KBM",
                    "DUBAİ / FRESHCO", "CİDDE / AL AMOUD", "RİYAD / TAMIMI",
                    "BAHREYN / LULU", "AMMAN / ZALATIMO", "ERBİL / KARWAN"
                };

                string[] products = new[]
                {
                    "ELMA GRANNY SMITH : 15 PLT\r\nELMA GALA : 11 PLT",
                    "LİMON ENTER : 16 PLT\r\nNAR METRO CHEF : 10 PLT",
                    "MAND. PRİMASOL : 18 PLT\r\nGREYF. STAR : 8 PLT",
                    "PORTAKAL WASH. : 26 PLT",
                    "LİMON MAYER : 20 PLT\r\nNAR : 6 PLT",
                    "ELMA FUJİ : 14 PLT\r\nELMA GOLDEN : 12 PLT",
                    "MAND. SATSUMA : 26 PLT",
                    "GREYFURT RUBY : 26 PLT",
                    "LİMON LAMAS : 26 PLT"
                };

                for (int i = 0; i < 9; i++)
                {
                    int r = (i / 3) + 1;
                    int c = (i % 3) + 1;
                    p33.Sutunlar.Add(new YuklemeSutunDto
                    {
                        SatirNo = r,
                        SutunNo = c,
                        HucreIndex = i + 1,
                        Baslik = destinations[i],
                        UrunlerMetin = products[i],
                        TalimatlarMetin = "TEKASYA TIR SURİYE ÜZERİ\r\nBRÜT: 25.000 KG GEÇMEYECEK\r\nTERMOKİNG : +2 DERECE",
                        PartilerMetin = $"PARTİ NO: 10-{1680 + i}\r\nPLAKA : 31 KAA {100 + i}",
                        ToplamPalet = 26
                    });
                }

                p33.ToplamPalet = p33.Sutunlar.Sum(s => s.ToplamPalet);
                Task.Run(async () => await svc.SaveAsync(p33)).GetAwaiter().GetResult();

                // 1. Render high-res A4 preview to PNG
                using (var bmpA4 = YuklemeProgramiReportRenderer.RenderToBitmap(p33, 1200, 1695))
                {
                    bmpA4.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\yukleme_programi_3x3_a4_render.png");
                }

                // 2. Open EditDialog in 3x3 mode and capture UI window
                var dlg = new YuklemeProgramiEditDialog(p33);
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(50, 40);
                dlg.Size = new Size(1420, 880);
                dlg.Show();
                dlg.SetPreviewWidthRatio(0.48);
                dlg.PerformLayout();
                for (int i = 0; i < 30; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpDlg = new Bitmap(dlg.Width, dlg.Height))
                {
                    dlg.DrawToBitmap(bmpDlg, new Rectangle(0, 0, dlg.Width, dlg.Height));
                    bmpDlg.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\yukleme_programi_3x3_ekrani.png");
                }
                dlg.Close();

                Console.WriteLine("3X3_SNAPSHOT_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-whatsapp")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                var svc = new YuklemeProgramiService();
                var list = Task.Run(async () => await svc.GetAllAsync()).GetAwaiter().GetResult();
                var p = list.FirstOrDefault(x => x.ProgramNo == "YP-2026-3X3-01") ?? list.FirstOrDefault();
                YuklemeProgramiDto fullProg;
                if (p != null)
                {
                    fullProg = Task.Run(async () => await svc.GetByIdAsync(p.Id)).GetAwaiter().GetResult() ?? p;
                }
                else
                {
                    fullProg = new YuklemeProgramiDto
                    {
                        ProgramNo = "YP-2026-003",
                        Tarih = DateTime.Today,
                        Baslik = "KUVEYT & KATAR SEVKİYATI",
                        SatirSayisi = 1,
                        SutunSayisi = 2,
                        ToplamPalet = 52,
                        Hazirlayan = "Ali SARIGÜL",
                        Onaylayan = "İHRACAT BÖLÜMÜ",
                        Durum = "Onaylandı"
                    };
                }

                // 1. Capture WhatsAppShareDialog
                var waDlg = new WhatsAppShareDialog(fullProg);
                waDlg.StartPosition = FormStartPosition.Manual;
                waDlg.Location = new Point(120, 80);
                waDlg.Show();
                for (int i = 0; i < 25; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpWa = new Bitmap(waDlg.Width, waDlg.Height))
                {
                    waDlg.DrawToBitmap(bmpWa, new Rectangle(0, 0, waDlg.Width, waDlg.Height));
                    bmpWa.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\whatsapp_paylasim_dialog_ekrani.png");
                }
                waDlg.Close();

                // 2. Capture YuklemeProgramiForm (List screen with WhatsApp button)
                var formList = new YuklemeProgramiForm();
                formList.StartPosition = FormStartPosition.Manual;
                formList.Location = new Point(40, 40);
                formList.Size = new Size(1300, 750);
                formList.Show();
                for (int i = 0; i < 25; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpList = new Bitmap(formList.Width, formList.Height))
                {
                    formList.DrawToBitmap(bmpList, new Rectangle(0, 0, formList.Width, formList.Height));
                    bmpList.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\yukleme_programi_whatsapp_list_ekrani.png");
                }
                formList.Close();

                // 3. Recapture 3x3 EditDialog with WhatsApp buttons visible
                var editDlg = new YuklemeProgramiEditDialog(fullProg);
                editDlg.StartPosition = FormStartPosition.Manual;
                editDlg.Location = new Point(40, 40);
                editDlg.Size = new Size(1420, 880);
                editDlg.Show();
                editDlg.SetPreviewWidthRatio(0.48);
                editDlg.PerformLayout();
                for (int i = 0; i < 30; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpEdit = new Bitmap(editDlg.Width, editDlg.Height))
                {
                    editDlg.DrawToBitmap(bmpEdit, new Rectangle(0, 0, editDlg.Width, editDlg.Height));
                    bmpEdit.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\yukleme_programi_3x3_ekrani.png");
                }
                editDlg.Close();

                Console.WriteLine("WHATSAPP_SNAPSHOT_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-satis-irsaliye")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                var svc = new IrsaliyeService();

                // 1. Capture SatisIrsaliyeForm (List Screen with Live MariaDB Data)
                var formList = new SatisIrsaliyeForm();
                formList.StartPosition = FormStartPosition.Manual;
                formList.Location = new Point(30, 30);
                formList.Size = new Size(1360, 780);
                formList.Show();
                for (int i = 0; i < 35; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpList = new Bitmap(formList.Width, formList.Height))
                {
                    formList.DrawToBitmap(bmpList, new Rectangle(0, 0, formList.Width, formList.Height));
                    bmpList.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\satis_irsaliye_liste_ekrani.png");
                }
                formList.Close();

                // 2. Fetch a real waybill (e.g. FISNO 8948 or most recent)
                var irsList = Task.Run(async () => await svc.GetSatisIrsaliyeleriAsync(DateTime.Today.AddDays(-120), DateTime.Today)).GetAwaiter().GetResult();
                var sampleIrs = irsList.FirstOrDefault(x => x.FisNo == 8948) ?? irsList.FirstOrDefault();
                if (sampleIrs == null)
                {
                    sampleIrs = new IrsaliyeStkDto
                    {
                        FisNo = 8950,
                        IrsNo = 8950,
                        FisTar = DateTime.Today,
                        IrsTar = DateTime.Today,
                        FirmaAdi = "SAVUKDURAN TARIM ÜRÜNLERİ LTD. ŞTİ.",
                        FirKod = 30048,
                        Plaka = "31 AVL 103",
                        ArkaPlaka = "31 KAA 205",
                        SoforAdi = "Süleyman",
                        SoforSoyadi = "Bıyıklı",
                        SoforTc = "34567891234",
                        SoforTelefon = "0532 555 12 34",
                        DepoAdi = "ANA DEPO",
                        ToplamPalet = 4,
                        ToplamKasa = 355,
                        ToplamMiktar = 3602.24,
                        FisTut = 86453.76,
                        KdvTut = 864.54,
                        GenTut = 87318.30
                    };
                }

                var sampleLines = Task.Run(async () => await svc.GetIrsaliyeDetayAsync(sampleIrs.FisNo)).GetAwaiter().GetResult();
                if (sampleLines.Count == 0)
                {
                    sampleLines = new List<IrsaliyeDetayStkDto>
                    {
                        new() { Sira = 1, MalKodu = "1817", MalAdi = "ELMA - GALA", Birimi = "KG", Palet = 1, Kasa = 85, Burut = 742, Dara = 52.06, Miktar = 689.94, Fiyati = 24.50, Kdv = 1, KdvTut = 169.04, Tutar = 16903.53, KunyeNo = "010-8948-1" },
                        new() { Sira = 2, MalKodu = "1817", MalAdi = "ELMA - GALA", Birimi = "KG", Palet = 1, Kasa = 80, Burut = 1068, Dara = 62.61, Miktar = 1005.39, Fiyati = 24.50, Kdv = 1, KdvTut = 246.32, Tutar = 24632.06, KunyeNo = "010-8948-2" },
                        new() { Sira = 3, MalKodu = "1804", MalAdi = "MANDALİNA - PRIMASOL", Birimi = "KG", Palet = 1, Kasa = 110, Burut = 1193.5, Dara = 62.49, Miktar = 1131.01, Fiyati = 22.00, Kdv = 1, KdvTut = 248.82, Tutar = 24882.22, KunyeNo = "010-8948-3" },
                        new() { Sira = 4, MalKodu = "2306", MalAdi = "NEKTARİN - SWET LEYDİ", Birimi = "KG", Palet = 1, Kasa = 80, Burut = 827, Dara = 50.16, Miktar = 776.84, Fiyati = 25.80, Kdv = 1, KdvTut = 200.42, Tutar = 20042.47, KunyeNo = "010-8948-4" }
                    };
                }

                // 3. Render A4 Sevk İrsaliyesi High-Res Document
                using (var bmpA4 = SatisIrsaliyeReportRenderer.RenderToBitmap(sampleIrs, sampleLines, 1240, 1754))
                {
                    bmpA4.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\satis_irsaliye_a4_baski.png");
                }

                // 4. Capture SatisIrsaliyeEditDialog
                var editDlg = new SatisIrsaliyeEditDialog(sampleIrs);
                editDlg.StartPosition = FormStartPosition.Manual;
                editDlg.Location = new Point(40, 40);
                editDlg.Size = new Size(1340, 840);
                editDlg.Show();
                for (int i = 0; i < 30; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpEdit = new Bitmap(editDlg.Width, editDlg.Height))
                {
                    editDlg.DrawToBitmap(bmpEdit, new Rectangle(0, 0, editDlg.Width, editDlg.Height));
                    bmpEdit.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\satis_irsaliye_duzenle_ekrani.png");
                }
                editDlg.Close();

                // 5. Capture StokRehberForm
                var stokDlg = new StokRehberForm();
                stokDlg.StartPosition = FormStartPosition.Manual;
                stokDlg.Location = new Point(120, 90);
                stokDlg.Size = new Size(950, 600);
                stokDlg.Show();
                for (int i = 0; i < 25; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpStok = new Bitmap(stokDlg.Width, stokDlg.Height))
                {
                    stokDlg.DrawToBitmap(bmpStok, new Rectangle(0, 0, stokDlg.Width, stokDlg.Height));
                    bmpStok.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\stok_rehber_dialog_ekrani.png");
                }
                stokDlg.Close();

                Console.WriteLine("SATIS_IRSALIYE_SNAPSHOT_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-alis-irsaliye")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                var svc = new IrsaliyeService();

                // 1. Capture AlisIrsaliyeForm (List Screen with Live MariaDB Data)
                var formList = new AlisIrsaliyeForm();
                formList.StartPosition = FormStartPosition.Manual;
                formList.Location = new Point(30, 30);
                formList.Size = new Size(1360, 780);
                formList.Show();
                for (int i = 0; i < 35; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpList = new Bitmap(formList.Width, formList.Height))
                {
                    formList.DrawToBitmap(bmpList, new Rectangle(0, 0, formList.Width, formList.Height));
                    bmpList.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\alis_irsaliye_liste_ekrani.png");
                }
                formList.Close();

                // 2. Fetch a real Alış İrsaliyesi (e.g. FISNO 5608 or most recent with lines)
                var irsList = Task.Run(async () => await svc.GetAlisIrsaliyeleriAsync(DateTime.Today.AddDays(-120), DateTime.Today)).GetAwaiter().GetResult();
                var sampleIrs = irsList.FirstOrDefault(x => x.FisNo == 5608) ?? irsList.FirstOrDefault();
                if (sampleIrs == null)
                {
                    sampleIrs = new IrsaliyeStkDto
                    {
                        FisNo = 5608,
                        FisTur = 220,
                        IrsNo = 2025000005079,
                        GibIrsNo = "ABY2026000000058",
                        FisTar = DateTime.Today,
                        IrsTar = DateTime.Today,
                        FirmaAdi = "AHMET BEYAN",
                        FirKod = 61008,
                        VergiDairesi = "ANTAKYA V.D.",
                        VergiNumarasi = "1234567890",
                        Plaka = "31 AHM 450",
                        ArkaPlaka = "31 BB 920",
                        SoforAdi = "Ahmet",
                        SoforSoyadi = "Beyan",
                        SoforTc = "28495018274",
                        SoforTelefon = "0533 456 78 90",
                        TasiyiciFirma = "ÖZ BEYAN LOJİSTİK LTD. ŞTİ.",
                        DepoAdi = "AMBALAJ VE KASA DEPOSU",
                        ToplamPalet = 0,
                        ToplamKasa = 0,
                        ToplamMiktar = 3660,
                        FisTut = 69540,
                        KdvTut = 13908,
                        GenTut = 83448,
                        FatNo = 14120782
                    };
                }

                var sampleLines = Task.Run(async () => await svc.GetIrsaliyeDetayAsync(sampleIrs.FisNo, 220)).GetAwaiter().GetResult();
                if (sampleLines.Count == 0)
                {
                    sampleLines = new List<IrsaliyeDetayStkDto>
                    {
                        new() { Sira = 1, FisTur = 220, MalKodu = "1410", MalAdi = "30*50*10 PLASTİK KASA MAVİ", Birimi = "ADET", Miktar = 3660, Fiyati = 19.00, Kdv = 20, KdvTut = 13908.00, Tutar = 69540.00, Aciklama = "Parti No: AB-2026-09" },
                        new() { Sira = 2, FisTur = 220, MalKodu = "1111", MalAdi = "100*120 GEMİ PALETİ", Birimi = "ADET", Miktar = 250, Fiyati = 145.00, Kdv = 20, KdvTut = 7250.00, Tutar = 36250.00, Aciklama = "ISPM-15 Isıl İşlemli" },
                        new() { Sira = 3, FisTur = 220, MalKodu = "1689", MalAdi = "100*120 PALET ŞAPKASI", Birimi = "ADET", Miktar = 500, Fiyati = 8.50, Kdv = 20, KdvTut = 850.00, Tutar = 4250.00, Aciklama = "Polietilen Şeffaf" }
                    };
                }

                // 3. Render A4 Mal Kabul & Alış İrsaliyesi High-Res Document
                using (var bmpA4 = AlisIrsaliyeReportRenderer.RenderToBitmap(sampleIrs, sampleLines, 1240, 1754))
                {
                    bmpA4.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\alis_irsaliye_a4_baski.png");
                }

                // 4. Capture AlisIrsaliyeEditDialog
                var editDlg = new AlisIrsaliyeEditDialog(sampleIrs);
                editDlg.StartPosition = FormStartPosition.Manual;
                editDlg.Location = new Point(40, 40);
                editDlg.Size = new Size(1340, 840);
                editDlg.Show();
                for (int i = 0; i < 30; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpEdit = new Bitmap(editDlg.Width, editDlg.Height))
                {
                    editDlg.DrawToBitmap(bmpEdit, new Rectangle(0, 0, editDlg.Width, editDlg.Height));
                    bmpEdit.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\alis_irsaliye_duzenle_ekrani.png");
                }
                editDlg.Close();

                Console.WriteLine("ALIS_IRSALIYE_SNAPSHOT_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-kasa-banka")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                // 1. Capture KasaBankaYonetimForm Tab 0 (Finans Hareketleri & KPIs)
                var formMain = new KasaBankaYonetimForm(0);
                formMain.StartPosition = FormStartPosition.Manual;
                formMain.Location = new Point(30, 30);
                formMain.Size = new Size(1360, 780);
                formMain.Show();
                for (int i = 0; i < 35; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpGenel = new Bitmap(formMain.Width, formMain.Height))
                {
                    formMain.DrawToBitmap(bmpGenel, new Rectangle(0, 0, formMain.Width, formMain.Height));
                    bmpGenel.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\kasa_banka_genel_ekrani.png");
                }
                formMain.Close();

                // 2. Capture KasaBankaYonetimForm Tab 1 (Kasa Kartları & Ekstre)
                var formKasa = new KasaBankaYonetimForm(1);
                formKasa.StartPosition = FormStartPosition.Manual;
                formKasa.Location = new Point(30, 30);
                formKasa.Size = new Size(1360, 780);
                formKasa.Show();
                for (int i = 0; i < 35; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpKasa = new Bitmap(formKasa.Width, formKasa.Height))
                {
                    formKasa.DrawToBitmap(bmpKasa, new Rectangle(0, 0, formKasa.Width, formKasa.Height));
                    bmpKasa.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\kasa_ekstre_ekrani.png");
                }
                formKasa.Close();

                // 3. Capture KasaBankaYonetimForm Tab 2 (Banka Hesapları & Ekstre)
                var formBanka = new KasaBankaYonetimForm(2);
                formBanka.StartPosition = FormStartPosition.Manual;
                formBanka.Location = new Point(30, 30);
                formBanka.Size = new Size(1360, 780);
                formBanka.Show();
                for (int i = 0; i < 35; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpBanka = new Bitmap(formBanka.Width, formBanka.Height))
                {
                    formBanka.DrawToBitmap(bmpBanka, new Rectangle(0, 0, formBanka.Width, formBanka.Height));
                    bmpBanka.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\banka_ekstre_ekrani.png");
                }
                formBanka.Close();

                // 4. Sample Voucher & Dialog Snapshot
                var sampleFis = new FinansFisGirisDto
                {
                    FisNo = 982,
                    MakbuzNo = 982,
                    FisTar = DateTime.Today,
                    Saat = DateTime.Now.TimeOfDay,
                    IslemTipi = FinansIslemTipi.KasaTahsilat,
                    KasaKodu = 4,
                    KasaAdi = "MERKEZ KASA TL",
                    FirKod = 30048,
                    CariAdi = "SAVUKDURAN TARIM ÜRÜNLERİ LTD. ŞTİ.",
                    VergiDairesi = "ANTAKYA V.D.",
                    VergiNo = "7540291834",
                    CariAdresi = "Karaali Mah. Antakya / HATAY",
                    Tutar = 89800.00,
                    DovizTuru = "TL",
                    DovizKur = 1.0,
                    BelgeNo = "THS-2026-004",
                    Aciklama = "SÖZLEŞMELİ İŞÇİLİK VE NARENCİYE ALIM TAHSİLATI",
                    YaziIle = "SeksenDokuzBinSekizYüz Türk Lirası",
                    User = "Ali SARIGÜL"
                };

                // Capture KasaBankaFisDialog
                var dlg = new KasaBankaFisDialog(sampleFis);
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(100, 70);
                dlg.Show();
                for (int i = 0; i < 25; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpDlg = new Bitmap(dlg.Width, dlg.Height))
                {
                    dlg.DrawToBitmap(bmpDlg, new Rectangle(0, 0, dlg.Width, dlg.Height));
                    bmpDlg.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\kasa_banka_fis_dialog_ekrani.png");
                }
                dlg.Close();

                // 5. Render A4 Makbuz & Dekont High-Res Document
                using (var bmpA4 = KasaBankaReportRenderer.RenderToBitmap(sampleFis, 1240, 1754))
                {
                    bmpA4.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\kasa_banka_a4_makbuz.png");
                }

                Console.WriteLine("KASA_BANKA_SNAPSHOT_COMPLETED");
                return;
            }

            if (args.Length > 0 && args[0] == "--snapshot-cavus-servis")
            {
                AppState.ActiveCompany = new CompanyDto { SirKod = 1, Adi = "TEK ASYA A.Ş." };
                AppState.ActiveUser = new CompanyUserDto { KullaniciKodu = 100, KullaniciAdi = "Ali SARIGÜL", IsAdmin = true, Yetki = "A" };

                // 1. Main Management Form
                var form = new CavusKantarServisForm();
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(0, 0);
                form.Size = new Size(1360, 780);
                form.Show();
                for (int i = 0; i < 40; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpGenel = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bmpGenel, new Rectangle(0, 0, form.Width, form.Height));
                    bmpGenel.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\cavus_servis_genel_ekrani.png");
                }

                // 2. Filter Pending (Bekleyenler)
                var cboDurum = GetAllChildren(form).OfType<ComboBox>().FirstOrDefault(c => c.Items.Contains("⚠️ Çavuş Bekleyenler"));
                if (cboDurum != null)
                {
                    cboDurum.SelectedIndex = 2; // Çavuş Bekleyenler
                    form.RefreshDataAsync().GetAwaiter().GetResult();
                    for (int i = 0; i < 30; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                    using (var bmpBekleyen = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmpBekleyen, new Rectangle(0, 0, form.Width, form.Height));
                        bmpBekleyen.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\cavus_servis_bekleyenler_ekrani.png");
                    }
                }
                form.Close();

                // 3. Sample Match & Dialog Snapshot
                var sampleEslesme = new CavusKantarEslesmeDto
                {
                    IdNo = 18450,
                    GirisKodu = 14211,
                    Tarih = DateTime.Today,
                    GirisSaati = "20:38",
                    Plaka = "31 GY 606",
                    SoforAdi = "UMUT OKAYA",
                    UrunAdi = "LİMON - ENTER",
                    MustahsilAdi = "MELAGRO TARIM SANAYİ VE TİC. LTD. ŞTİ.",
                    Bolge = "ADANA İÇİ",
                    NetKg = 12401,
                    CavusKodu = 35062,
                    CavusAdi = "ENVER AYVAZ",
                    ServisSayisi = 1,
                    ServisFiyati = 3310,
                    ServisTutari = 3310,
                    ToplamNakliye = 14300,
                    Onay = true,
                    Aciklama = "Melagro narenciye hasadı 1. servis seferi mutabakatı"
                };

                var dlg = new CavusKantarEslesmeDialog(sampleEslesme);
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(100, 70);
                dlg.Show();
                for (int i = 0; i < 25; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                using (var bmpDlg = new Bitmap(dlg.Width, dlg.Height))
                {
                    dlg.DrawToBitmap(bmpDlg, new Rectangle(0, 0, dlg.Width, dlg.Height));
                    bmpDlg.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\cavus_servis_dialog_ekrani.png");
                }
                dlg.Close();

                // 4. Sample A4 Vector Hakediş Tutanağı Snapshot
                var sampleList = new List<CavusKantarEslesmeDto>
                {
                    sampleEslesme,
                    new CavusKantarEslesmeDto
                    {
                        IdNo = 18449,
                        GirisKodu = 14210,
                        Tarih = DateTime.Today.AddDays(-1),
                        GirisSaati = "20:35",
                        Plaka = "31 SH 374",
                        SoforAdi = "NAZIM TEMİZ",
                        UrunAdi = "LİMON - ENTER",
                        MustahsilAdi = "MELAGRO TARIM SANAYİ VE TİC. LTD. ŞTİ.",
                        Bolge = "ADANA İÇİ",
                        NetKg = 12121,
                        CavusKodu = 35062,
                        CavusAdi = "ENVER AYVAZ",
                        ServisSayisi = 1,
                        ServisFiyati = 3310,
                        ServisTutari = 3310,
                        ToplamNakliye = 14020,
                        Onay = true
                    },
                    new CavusKantarEslesmeDto
                    {
                        IdNo = 18452,
                        GirisKodu = 14213,
                        Tarih = DateTime.Today.AddDays(-2),
                        GirisSaati = "22:43",
                        Plaka = "31 SY 720",
                        SoforAdi = "MURAT AKÇAY",
                        UrunAdi = "LİMON - VOLKA MARIA",
                        MustahsilAdi = "HASAN KODAL",
                        Bolge = "ADANA",
                        NetKg = 11701,
                        CavusKodu = 56776,
                        CavusAdi = "SEFA KÜÇÜK",
                        ServisSayisi = 1,
                        ServisFiyati = 4500,
                        ServisTutari = 4500,
                        ToplamNakliye = 13600,
                        Onay = true
                    },
                    new CavusKantarEslesmeDto
                    {
                        IdNo = 18448,
                        GirisKodu = 14209,
                        Tarih = DateTime.Today.AddDays(-2),
                        GirisSaati = "20:32",
                        Plaka = "31 AOT 430",
                        SoforAdi = "KADİR TEMİZKAN",
                        UrunAdi = "LİMON - ENTER",
                        MustahsilAdi = "NURİ SERGEN BÜYÜKYEL",
                        Bolge = "ADANA",
                        NetKg = 10741,
                        CavusKodu = 35099,
                        CavusAdi = "HAYDAR ERTEM",
                        ServisSayisi = 1,
                        ServisFiyati = 3900,
                        ServisTutari = 3900,
                        ToplamNakliye = 12620,
                        Onay = true
                    },
                    new CavusKantarEslesmeDto
                    {
                        IdNo = 18447,
                        GirisKodu = 14208,
                        Tarih = DateTime.Today.AddDays(-3),
                        GirisSaati = "19:18",
                        Plaka = "31 ZK 711",
                        SoforAdi = "MEHMET EMİN TEMİZKAN",
                        UrunAdi = "LİMON - ENTER",
                        MustahsilAdi = "NURİ SERGEN BÜYÜKYEL",
                        Bolge = "ADANA",
                        NetKg = 10281,
                        CavusKodu = 61234,
                        CavusAdi = "ABDULHADİ ERTEM",
                        ServisSayisi = 1,
                        ServisFiyati = 3900,
                        ServisTutari = 3900,
                        ToplamNakliye = 12180,
                        Onay = true
                    }
                };

                using (var bmpA4 = CavusKantarReportRenderer.RenderToBitmap(sampleList, "ENVER AYVAZ", "ADANA BÖLGESİ", DateTime.Today.AddDays(-30), DateTime.Today, 1240, 1754))
                {
                    bmpA4.Save(@"C:\Users\PC\.gemini\antigravity\brain\f01c8499-00c7-4c14-92e1-12b9c47dce57\cavus_servis_a4_hakedis.png");
                }

                Console.WriteLine("CAVUS_SERVIS_SNAPSHOT_COMPLETED");
                return;
            }

            // Launch authentic MacroSoft Login Form
            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log"), $"[MAIN CATCH] {DateTime.Now}: {ex}\n"); } catch { }
        }
    }

    private static IEnumerable<Control> GetAllChildren(Control root)
    {
        foreach (Control c in root.Controls)
        {
            yield return c;
            foreach (var child in GetAllChildren(c))
                yield return child;
        }
    }
}
