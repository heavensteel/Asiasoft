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
