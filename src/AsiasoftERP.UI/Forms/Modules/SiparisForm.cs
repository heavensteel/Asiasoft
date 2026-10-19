using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Sipariş Yönetimi Ana Ekranı (Alınan ve Verilen Siparişler - Canlı MariaDB)
/// </summary>
public class SiparisForm : Form
{
    private readonly SiparisService _service = new();
    private SiparisYonu _aktifYon = SiparisYonu.Alinan;
    private List<SiparisDto> _siparisler = new();

    // Sekme / Yön Seçim Butonları
    private Button _btnTabAlinan = null!;
    private Button _btnTabVerilen = null!;

    // KPI Kartları
    private Label _lblKpiToplamSiparis = null!;
    private Label _lblKpiToplamPalet = null!;
    private Label _lblKpiToplamNetKg = null!;
    private Label _lblKpiToplamTutar = null!;

    // Filtre Kontrolleri
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private ComboBox _cmbDurum = null!;
    private TextBox _txtSearch = null!;
    private Button _btnSearch = null!;

    // Eylem Butonları
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnYuklemeAktar = null!;
    private Button _btnIrsaliyeAktar = null!;
    private Button _btnPrint = null!;
    private Button _btnDelete = null!;
    private Button _btnRefresh = null!;

    // Grid ve Durum
    private DataGridView _grid = null!;
    private Label _lblStatusInfo = null!;

    public SiparisForm()
    {
        this.DoubleBuffered = true;
        this.Text = "📦 Sipariş Yönetimi";
        this.Size = new Size(1340, 780);
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        InitializeUI();
        _ = LoadDataAsync();
    }

    private void InitializeUI()
    {
        // 1. ÜST BAŞLIK BANNER'I
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblIcon = new Label
        {
            Text = "📦",
            Font = new Font("Segoe UI", 20f),
            Location = new Point(16, 12),
            Size = new Size(40, 40)
        };

        var lblTitle = new Label
        {
            Text = "Sipariş Yönetimi (Alınan & Verilen Siparişler - Canlı Veri)",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(62, 10),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Yurt dışı ve iç piyasa müşteri siparişleri, satın alma siparişleri, palet/kasa terminleri, yükleme ve irsaliye aktarımı",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(64, 34),
            AutoSize = true
        };

        // Yön Değiştirme Butonları (Header Sağı)
        _btnTabAlinan = new Button
        {
            Text = "📥 Alınan Müşteri Siparişleri",
            Size = new Size(205, 36),
            Location = new Point(pnlHeader.Width - 430, 14),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            BackColor = Color.FromArgb(30, 58, 138),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnTabAlinan.FlatAppearance.BorderSize = 0;
        _btnTabAlinan.Click += async (s, e) =>
        {
            _aktifYon = SiparisYonu.Alinan;
            UpdateTabStyles();
            await LoadDataAsync();
        };

        _btnTabVerilen = new Button
        {
            Text = "📤 Verilen Satın Alma Siparişleri",
            Size = new Size(210, 36),
            Location = new Point(pnlHeader.Width - 220, 14),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnTabVerilen.FlatAppearance.BorderColor = ModernColors.Slate300;
        _btnTabVerilen.Click += async (s, e) =>
        {
            _aktifYon = SiparisYonu.Verilen;
            UpdateTabStyles();
            await LoadDataAsync();
        };

        pnlHeader.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSub, _btnTabAlinan, _btnTabVerilen });

        // 2. KPI KARTLARI PANELİ (4 Adet)
        var pnlKpis = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(16, 8, 16, 6)
        };

        var tblKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        for (int i = 0; i < 4; i++)
        {
            tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        }

        tblKpis.Controls.Add(CreateKpiCard("TOPLAM SİPARİŞ ADEDİ", "0 Sipariş", "Bekleyen: 0  |  Sevk: 0", Color.FromArgb(30, 58, 138), out _lblKpiToplamSiparis), 0, 0);
        tblKpis.Controls.Add(CreateKpiCard("TOPLAM PALET SAYISI", "0 Palet", "Kasa: 0 Adet", Color.FromArgb(14, 116, 144), out _lblKpiToplamPalet), 1, 0);
        tblKpis.Controls.Add(CreateKpiCard("TOPLAM SİPARİŞ MİKTARI", "0 KG", "0.0 Ton Narenciye", Color.FromArgb(180, 83, 9), out _lblKpiToplamNetKg), 2, 0);
        tblKpis.Controls.Add(CreateKpiCard("TOPLAM SİPARİŞ BEDELİ", "0.00 EUR", "USD: 0.00  |  TL: 0.00", Color.FromArgb(22, 101, 52), out _lblKpiToplamTutar), 3, 0);

        pnlKpis.Controls.Add(tblKpis);

        // 3. FİLTRE VE EYLEM ARAÇ ÇUBUĞU
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        pnlToolbar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1);
        };

        var lblDate = new Label { Text = "Tarih:", Location = new Point(12, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = ModernColors.Slate700 };
        _dtpStart = new DateTimePicker
        {
            Location = new Point(56, 12),
            Width = 100,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(-120)
        };
        var lblDateSep = new Label { Text = "-", Location = new Point(160, 15), AutoSize = true };
        _dtpEnd = new DateTimePicker
        {
            Location = new Point(174, 12),
            Width = 100,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(1)
        };

        var lblDurum = new Label { Text = "Durum:", Location = new Point(285, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = ModernColors.Slate700 };
        _cmbDurum = new ComboBox { Location = new Point(338, 12), Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbDurum.Items.AddRange(new object[] { "TÜMÜ", "SİPARİŞ ALINDI", "HAZIRLANIYOR", "YÜKLENDİ", "SEVK EDİLDİ", "TAMAMLANDI", "İPTAL EDİLDİ" });
        _cmbDurum.SelectedIndex = 0;
        _cmbDurum.SelectedIndexChanged += async (s, e) => await LoadDataAsync();

        _txtSearch = new TextBox
        {
            Location = new Point(485, 12),
            Width = 150,
            PlaceholderText = "Firma, ülke, fiş no..."
        };
        _txtSearch.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) await LoadDataAsync();
        };

        _btnSearch = new Button
        {
            Text = "Filtrele",
            Location = new Point(642, 11),
            Size = new Size(65, 28),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat
        };
        _btnSearch.FlatAppearance.BorderColor = ModernColors.Slate300;
        _btnSearch.Click += async (s, e) => await LoadDataAsync();

        // Butonlar (Sağa Hizalı)
        _btnRefresh = CreateActionButton("🔄", Color.FromArgb(241, 245, 249), ModernColors.Slate700, 36);
        _btnRefresh.Click += async (s, e) => await LoadDataAsync();

        _btnPrint = CreateActionButton("🖨️ A4 Yazdır", Color.FromArgb(30, 58, 138), Color.White, 95);
        _btnPrint.Click += (s, e) => PrintSelectedSiparis();

        _btnYuklemeAktar = CreateActionButton("🚚 Yüklemeye Aktar", Color.FromArgb(14, 116, 144), Color.White, 130);
        _btnYuklemeAktar.Click += (s, e) => AktarYuklemeProgramina();

        _btnIrsaliyeAktar = CreateActionButton("📄 İrsaliyeye Dönüştür", Color.FromArgb(79, 70, 229), Color.White, 140);
        _btnIrsaliyeAktar.Click += (s, e) => AktarIrsaliyeye();

        _btnDelete = CreateActionButton("🗑️ Sil", Color.FromArgb(254, 242, 242), Color.Crimson, 60);
        _btnDelete.Click += async (s, e) => await DeleteSelectedSiparisAsync();

        _btnEdit = CreateActionButton("✏️ Düzenle", Color.FromArgb(241, 245, 249), ModernColors.Slate800, 80);
        _btnEdit.Click += (s, e) => EditSelectedSiparis();

        _btnNew = CreateActionButton("➕ Yeni Sipariş", Color.FromArgb(22, 101, 52), Color.White, 110);
        _btnNew.Click += (s, e) => CreateNewSiparis();

        var pnlActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0)
        };
        pnlActions.Controls.AddRange(new Control[] { _btnNew, _btnEdit, _btnYuklemeAktar, _btnIrsaliyeAktar, _btnPrint, _btnDelete, _btnRefresh });

        pnlToolbar.Controls.AddRange(new Control[] {
            lblDate, _dtpStart, lblDateSep, _dtpEnd,
            lblDurum, _cmbDurum,
            _txtSearch, _btnSearch,
            pnlActions
        });

        // 4. DATA GRID VIEW
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(241, 245, 249),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 34 }
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernColors.Slate700,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };
        _grid.ColumnHeadersHeight = 38;

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = Color.FromArgb(238, 242, 255),
            SelectionForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 8.8f),
            Padding = new Padding(8, 0, 8, 0)
        };

        SetupGridColumns();
        _grid.CellPainting += Grid_CellPainting;
        _grid.CellDoubleClick += (s, e) => EditSelectedSiparis();

        // 5. DURUM ÇUBUĞU
        var pnlStatus = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = Color.White,
            Padding = new Padding(16, 6, 16, 6)
        };
        pnlStatus.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlStatus.Width, 0);
        };

        _lblStatusInfo = new Label
        {
            Text = "Siparişler yükleniyor...",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate600
        };
        pnlStatus.Controls.Add(_lblStatusInfo);

        this.Controls.Add(_grid);
        this.Controls.Add(pnlStatus);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlHeader);
    }

    private void UpdateTabStyles()
    {
        if (_aktifYon == SiparisYonu.Alinan)
        {
            _btnTabAlinan.BackColor = Color.FromArgb(30, 58, 138);
            _btnTabAlinan.ForeColor = Color.White;
            _btnTabAlinan.FlatAppearance.BorderSize = 0;

            _btnTabVerilen.BackColor = Color.FromArgb(241, 245, 249);
            _btnTabVerilen.ForeColor = ModernColors.Slate700;
            _btnTabVerilen.FlatAppearance.BorderColor = ModernColors.Slate300;
        }
        else
        {
            _btnTabVerilen.BackColor = Color.FromArgb(30, 58, 138);
            _btnTabVerilen.ForeColor = Color.White;
            _btnTabVerilen.FlatAppearance.BorderSize = 0;

            _btnTabAlinan.BackColor = Color.FromArgb(241, 245, 249);
            _btnTabAlinan.ForeColor = ModernColors.Slate700;
            _btnTabAlinan.FlatAppearance.BorderColor = ModernColors.Slate300;
        }
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "FisNoFormatted", HeaderText = "Sipariş No", Width = 95, DataPropertyName = "FisNoFormatted" },
            new DataGridViewTextBoxColumn { Name = "TarihFormatted", HeaderText = "Tarih", Width = 85, DataPropertyName = "TarihFormatted" },
            new DataGridViewTextBoxColumn { Name = "Durumu", HeaderText = "Durum", Width = 115, DataPropertyName = "Durumu" },
            new DataGridViewTextBoxColumn { Name = "FirmaAdi", HeaderText = "Firma / Müşteri Ünvanı", Width = 260, DataPropertyName = "FirmaAdi" },
            new DataGridViewTextBoxColumn { Name = "Ulke", HeaderText = "Ülke", Width = 95, DataPropertyName = "Ulke" },
            new DataGridViewTextBoxColumn { Name = "SatisSekli", HeaderText = "Teslim", Width = 65, DataPropertyName = "SatisSekli" },
            new DataGridViewTextBoxColumn { Name = "PaletToplamFormatted", HeaderText = "Palet", Width = 75, DataPropertyName = "PaletToplamFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "KasaToplamFormatted", HeaderText = "Kasa", Width = 75, DataPropertyName = "KasaToplamFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "NetToplamFormatted", HeaderText = "Net Miktar", Width = 95, DataPropertyName = "NetToplamFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "ToplamTutarFormatted", HeaderText = "Sipariş Tutarı", Width = 120, DataPropertyName = "ToplamTutarFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "AracPlaka", HeaderText = "Araç / Plaka", Width = 100, DataPropertyName = "AracPlaka" },
            new DataGridViewTextBoxColumn { Name = "TerminFormatted", HeaderText = "Sevk Tarihi", Width = 85, DataPropertyName = "TerminFormatted" }
        );
    }

    private Control CreateKpiCard(string title, string mainValue, string subValue, Color accentColor, out Label lblMain)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(4),
            Padding = new Padding(12, 10, 12, 8)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var brushTop = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(brushTop, 0, 0, pnl.Width, 3);
        };

        var lblT = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate500,
            Location = new Point(10, 8),
            AutoSize = true
        };

        var lblM = new Label
        {
            Text = mainValue,
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(10, 26),
            AutoSize = true
        };
        lblMain = lblM;

        var lblS = new Label
        {
            Text = subValue,
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(11, 52),
            AutoSize = true
        };

        pnl.Controls.AddRange(new Control[] { lblT, lblM, lblS });
        return pnl;
    }

    private Button CreateActionButton(string text, Color bg, Color fg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, 30),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 0, 3, 0)
        };
        btn.FlatAppearance.BorderColor = (bg == Color.White || bg == Color.FromArgb(241, 245, 249))
            ? ModernColors.Slate300
            : bg;
        return btn;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            _lblStatusInfo.Text = "Siparişler sorgulanıyor...";

            var start = _dtpStart.Value;
            var end = _dtpEnd.Value;
            var durum = _cmbDurum.SelectedItem?.ToString() ?? "TÜMÜ";
            var search = _txtSearch.Text.Trim();

            _siparisler = await _service.GetSiparislerAsync(_aktifYon, start, end, durum, search);
            var kpis = await _service.GetKpisAsync(_aktifYon, start, end);

            // KPI'ları güncelle
            _lblKpiToplamSiparis.Text = $"{kpis.ToplamSiparis:N0} Sipariş";
            _lblKpiToplamPalet.Text = $"{kpis.ToplamPalet:N0} Palet";
            _lblKpiToplamNetKg.Text = kpis.ToplamNetKg >= 1000 ? $"{kpis.ToplamNetKg / 1000.0:N1} Ton" : $"{kpis.ToplamNetKg:N0} KG";
            _lblKpiToplamTutar.Text = kpis.ToplamEurTutar > 0 ? $"{kpis.ToplamEurTutar:N2} EUR" : $"{kpis.ToplamTlTutar:N2} TL";

            var displayList = _siparisler.Select(s => new
            {
                Model = s,
                FisNoFormatted = $"SP-{(int)s.FisNo}",
                TarihFormatted = s.Tarih.ToString("dd.MM.yyyy"),
                Durumu = string.IsNullOrWhiteSpace(s.Durumu) ? "SİPARİŞ ALINDI" : s.Durumu,
                s.FirmaAdi,
                Ulke = string.IsNullOrWhiteSpace(s.Ulke) ? "TÜRKİYE" : s.Ulke,
                s.SatisSekli,
                PaletToplamFormatted = s.PaletToplam > 0 ? $"{s.PaletToplam:N0}" : "-",
                KasaToplamFormatted = s.KasaToplam > 0 ? $"{s.KasaToplam:N0}" : "-",
                NetToplamFormatted = s.NetToplam > 0 ? (s.NetToplam >= 1000 ? $"{s.NetToplam / 1000.0:N1} Tn" : $"{s.NetToplam:N0} KG") : "-",
                ToplamTutarFormatted = s.ToplamTutar > 0 ? $"{s.ToplamTutar:N2} {s.DovizTuru}" : "-",
                AracPlaka = string.IsNullOrWhiteSpace(s.AracPlaka) ? "-" : s.AracPlaka,
                TerminFormatted = s.BaslamaTarihi.HasValue ? s.BaslamaTarihi.Value.ToString("dd.MM.yyyy") : "-"
            }).ToList();

            _grid.DataSource = displayList;
            _lblStatusInfo.Text = $"Toplam {_siparisler.Count} sipariş listelendi. ({(_aktifYon == SiparisYonu.Alinan ? "Alınan Müşteri Siparişleri" : "Verilen Satın Alma Siparişleri")})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Siparişler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblStatusInfo.Text = "Hata oluştu.";
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.Graphics == null) return;

        // Durum Kolonu (Pill Badge)
        if (_grid.Columns["Durumu"] != null && e.ColumnIndex == _grid.Columns["Durumu"]!.Index)
        {
            e.PaintBackground(e.CellBounds, true);
            var durumStr = e.Value?.ToString() ?? "SİPARİŞ ALINDI";

            Color badgeBg, badgeFg;
            if (durumStr.Contains("SEVK") || durumStr.Contains("TAMAM"))
            {
                badgeBg = Color.FromArgb(220, 252, 231);
                badgeFg = Color.FromArgb(22, 101, 52);
            }
            else if (durumStr.Contains("YÜK"))
            {
                badgeBg = Color.FromArgb(243, 232, 255);
                badgeFg = Color.FromArgb(126, 34, 206);
            }
            else if (durumStr.Contains("HAZIR"))
            {
                badgeBg = Color.FromArgb(224, 242, 254);
                badgeFg = Color.FromArgb(3, 105, 161);
            }
            else if (durumStr.Contains("İPTAL"))
            {
                badgeBg = Color.FromArgb(254, 242, 242);
                badgeFg = Color.Crimson;
            }
            else
            {
                badgeBg = Color.FromArgb(254, 243, 199);
                badgeFg = Color.FromArgb(180, 83, 9);
            }

            var badgeRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + (e.CellBounds.Height - 20) / 2, e.CellBounds.Width - 12, 20);

            using var brushBg = new SolidBrush(badgeBg);
            using var brushFg = new SolidBrush(badgeFg);
            using var path = GetRoundedRectangle(badgeRect, 10);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brushBg, path);

            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(durumStr, font, brushFg, badgeRect, sf);

            e.Handled = true;
        }
    }

    private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private SiparisDto? GetSelectedSiparis()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        var row = _grid.SelectedRows[0];
        dynamic? item = row.DataBoundItem;
        return item?.Model as SiparisDto;
    }

    private void CreateNewSiparis()
    {
        using var dlg = new SiparisEditDialog(_aktifYon);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync();
        }
    }

    private void EditSelectedSiparis()
    {
        var selected = GetSelectedSiparis();
        if (selected == null)
        {
            MessageBox.Show("Lütfen düzenlemek için bir sipariş seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SiparisEditDialog(_aktifYon, selected);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync();
        }
    }

    private void PrintSelectedSiparis()
    {
        var selected = GetSelectedSiparis();
        if (selected == null)
        {
            MessageBox.Show("Lütfen yazdırmak için bir sipariş seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _ = Task.Run(async () =>
        {
            var full = await _service.GetSiparisByIdAsync(_aktifYon, selected.FisNo);
            this.Invoke(new Action(() =>
            {
                if (full != null)
                {
                    SiparisEditDialog.PrintSiparisRaporu(full);
                }
            }));
        });
    }

    private async Task DeleteSelectedSiparisAsync()
    {
        var selected = GetSelectedSiparis();
        if (selected == null)
        {
            MessageBox.Show("Lütfen silmek istediğiniz siparişi seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"SP-{(int)selected.FisNo} numaralı siparişi ({selected.FirmaAdi}) silmek istediğinize emin misiniz?",
            "Sipariş Silme Onayı",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            await _service.DeleteSiparisAsync(_aktifYon, selected.FisNo);
            await LoadDataAsync();
        }
    }

    private void AktarYuklemeProgramina()
    {
        var selected = GetSelectedSiparis();
        if (selected == null)
        {
            MessageBox.Show("Lütfen yükleme programına aktarılacak siparişi seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var msg = $"SP-{(int)selected.FisNo} numaralı sipariş ({selected.FirmaAdi} - {selected.Ulke})\n" +
                  $"Yükleme Programına sevk planı olarak aktarılacaktır.\n\n" +
                  $"Hedef Araç: {(string.IsNullOrWhiteSpace(selected.AracPlaka) ? "Planlanacak Araç" : selected.AracPlaka)}\n" +
                  $"Toplam Palet: {selected.PaletToplam:N0} Palet  |  Net Tonaj: {selected.NetToplam / 1000.0:N1} Ton\n\n" +
                  $"Aktarımı onaylıyor musunuz?";

        if (MessageBox.Show(msg, "Yükleme Programına Aktar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _ = _service.UpdateDurumAsync(_aktifYon, selected.FisNo, "HAZIRLANIYOR");
            MessageBox.Show("Sipariş başarıyla Yükleme Programı planlama havuzuna aktarıldı ve durumu 'HAZIRLANIYOR' olarak güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _ = LoadDataAsync();
        }
    }

    private void AktarIrsaliyeye()
    {
        var selected = GetSelectedSiparis();
        if (selected == null)
        {
            MessageBox.Show("Lütfen irsaliyeye dönüştürülecek siparişi seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var msg = $"SP-{(int)selected.FisNo} numaralı sipariş Satış Sevk İrsaliyesine aktarılacaktır.\n\n" +
                  $"Müşteri: {selected.FirmaAdi}\n" +
                  $"Teslim Yeri: {selected.Ulke} - {selected.BosaltmaAdresi}\n" +
                  $"Plaka: {selected.AracPlaka}\n\n" +
                  $"İrsaliye oluşturulsun mu?";

        if (MessageBox.Show(msg, "İrsaliyeye Dönüştür", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _ = _service.UpdateDurumAsync(_aktifYon, selected.FisNo, "SEVK EDİLDİ");
            MessageBox.Show("Sipariş Satış İrsaliyesi olarak sevk zincirine bağlandı ve durumu 'SEVK EDİLDİ' olarak güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _ = LoadDataAsync();
        }
    }
}
