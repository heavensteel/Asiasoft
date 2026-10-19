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
/// Lojistik Nakliye ve Tır Sefer Maliyeti Yönetim Formu (erp.sefer_fis & erp.sefer_detay)
/// </summary>
public class SeferMaliyetForm : Form
{
    private readonly SeferMaliyetService _service = new();
    private List<SeferFisDto> _seferler = new();

    // KPI Kontrolleri
    private Label _lblKpiToplamSefer = null!;
    private Label _lblKpiToplamKm = null!;
    private Label _lblKpiToplamMasraf = null!;
    private Label _lblKpiNetKar = null!;

    // Filtre Kontrolleri
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private ComboBox _cmbSeferTuru = null!;
    private ComboBox _cmbDurumu = null!;
    private TextBox _txtSearch = null!;
    private Button _btnSearch = null!;

    // Eylem Butonları
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnKapat = null!;
    private Button _btnPrint = null!;
    private Button _btnDelete = null!;
    private Button _btnRefresh = null!;

    // Grid ve Durum
    private DataGridView _grid = null!;
    private Label _lblStatusInfo = null!;

    public SeferMaliyetForm()
    {
        this.DoubleBuffered = true;
        this.Text = "🚛 Nakliye & Tır Sefer Maliyeti";
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
            Text = "🚛",
            Font = new Font("Segoe UI", 20f),
            Location = new Point(16, 12),
            Size = new Size(40, 40)
        };

        var lblTitle = new Label
        {
            Text = "Nakliye, Tır Seferleri ve Sefer Maliyeti Yönetimi (Canlı Veri)",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(62, 10),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Tır sefer kayıtları, KM sayaçları, mazot sarfiyatı (LT/100 KM), yurt içi/dışı yol giderleri ve net sefer kârlılık analizi",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(64, 34),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSub });

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

        tblKpis.Controls.Add(CreateKpiCard("TOPLAM SEFER", "0 Sefer", "Açık: 0  |  Kapalı: 0", Color.FromArgb(30, 58, 138), out _lblKpiToplamSefer), 0, 0);
        tblKpis.Controls.Add(CreateKpiCard("TOPLAM KM & SARFİYAT", "0 KM", "Ortalama: 0.00 LT/100 KM", Color.FromArgb(14, 116, 144), out _lblKpiToplamKm), 1, 0);
        tblKpis.Controls.Add(CreateKpiCard("TOPLAM SEFER MASRAFI", "0.00 TL", "Mazot, Otoyol, Gümrük, Harcırah", Color.FromArgb(185, 28, 28), out _lblKpiToplamMasraf), 2, 0);
        tblKpis.Controls.Add(CreateKpiCard("NET SEFER KÂR / ZARARI", "0.00 TL", "Toplam Navlun: 0.00 TL", Color.FromArgb(22, 101, 52), out _lblKpiNetKar), 3, 0);

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

        // Tarih Seçiciler
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

        // Sefer Türü Filtresi
        var lblTuru = new Label { Text = "Tür:", Location = new Point(285, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = ModernColors.Slate700 };
        _cmbSeferTuru = new ComboBox { Location = new Point(318, 12), Width = 105, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSeferTuru.Items.AddRange(new object[] { "TÜMÜ", "YURT DIŞI", "YURT İÇİ", "KATRA" });
        _cmbSeferTuru.SelectedIndex = 0;
        _cmbSeferTuru.SelectedIndexChanged += async (s, e) => await LoadDataAsync();

        // Durum Filtresi
        var lblDurum = new Label { Text = "Durum:", Location = new Point(432, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = ModernColors.Slate700 };
        _cmbDurumu = new ComboBox { Location = new Point(485, 12), Width = 95, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbDurumu.Items.AddRange(new object[] { "TÜMÜ", "AÇIK", "KAPALI" });
        _cmbDurumu.SelectedIndex = 0;
        _cmbDurumu.SelectedIndexChanged += async (s, e) => await LoadDataAsync();

        // Arama Kutusu
        _txtSearch = new TextBox
        {
            Location = new Point(590, 12),
            Width = 145,
            PlaceholderText = "Plaka, şoför, fiş..."
        };
        _txtSearch.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) await LoadDataAsync();
        };

        _btnSearch = new Button
        {
            Text = "Filtrele",
            Location = new Point(742, 11),
            Size = new Size(68, 28),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat
        };
        _btnSearch.FlatAppearance.BorderColor = ModernColors.Slate300;
        _btnSearch.Click += async (s, e) => await LoadDataAsync();

        // Eylem Butonları (Sağa Hizalı)
        _btnRefresh = CreateActionButton("🔄 Yenile", Color.FromArgb(241, 245, 249), ModernColors.Slate700, 75);
        _btnRefresh.Click += async (s, e) => await LoadDataAsync();

        _btnPrint = CreateActionButton("🖨️ A4 Yazdır", Color.FromArgb(30, 58, 138), Color.White, 95);
        _btnPrint.Click += (s, e) => PrintSelectedSefer();

        _btnKapat = CreateActionButton("🏁 Seferi Kapat", Color.FromArgb(14, 116, 144), Color.White, 105);
        _btnKapat.Click += async (s, e) => await KapatSelectedSeferAsync();

        _btnDelete = CreateActionButton("🗑️ Sil", Color.FromArgb(254, 242, 242), Color.Crimson, 65);
        _btnDelete.Click += async (s, e) => await DeleteSelectedSeferAsync();

        _btnEdit = CreateActionButton("✏️ Düzenle", Color.FromArgb(241, 245, 249), ModernColors.Slate800, 85);
        _btnEdit.Click += (s, e) => EditSelectedSefer();

        _btnNew = CreateActionButton("➕ Yeni Sefer", Color.FromArgb(22, 101, 52), Color.White, 105);
        _btnNew.Click += (s, e) => CreateNewSefer();

        // FlowLayoutPanel ile butonları sağa diz
        var pnlActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0)
        };
        pnlActions.Controls.AddRange(new Control[] { _btnNew, _btnEdit, _btnKapat, _btnPrint, _btnDelete, _btnRefresh });

        pnlToolbar.Controls.AddRange(new Control[] {
            lblDate, _dtpStart, lblDateSep, _dtpEnd,
            lblTuru, _cmbSeferTuru,
            lblDurum, _cmbDurumu,
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
        _grid.CellDoubleClick += (s, e) => EditSelectedSefer();

        // 5. ALT BİLGİ DURUM ÇUBUĞU
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
            Text = "Veriler yükleniyor...",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate600
        };
        pnlStatus.Controls.Add(_lblStatusInfo);

        // Form bileşenlerini ekle
        this.Controls.Add(_grid);
        this.Controls.Add(pnlStatus);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlHeader);
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "FisNoFormatted", HeaderText = "Fiş No", Width = 90, DataPropertyName = "FisNoFormatted" },
            new DataGridViewTextBoxColumn { Name = "TarihFormatted", HeaderText = "Tarih", Width = 85, DataPropertyName = "TarihFormatted" },
            new DataGridViewTextBoxColumn { Name = "Durumu", HeaderText = "Durum", Width = 95, DataPropertyName = "Durumu" },
            new DataGridViewTextBoxColumn { Name = "SeferTuru", HeaderText = "Tür", Width = 90, DataPropertyName = "SeferTuru" },
            new DataGridViewTextBoxColumn { Name = "Plaka", HeaderText = "Çekici Plaka", Width = 100, DataPropertyName = "Plaka" },
            new DataGridViewTextBoxColumn { Name = "DorsePlaka", HeaderText = "Dorse Plaka", Width = 95, DataPropertyName = "DorsePlaka" },
            new DataGridViewTextBoxColumn { Name = "SurucuAdi", HeaderText = "Sürücü / Lojistik", Width = 180, DataPropertyName = "SurucuAdi" },
            new DataGridViewTextBoxColumn { Name = "Guzergah", HeaderText = "Güzergah (Çıkış ➔ Varış)", Width = 180, DataPropertyName = "Guzergah" },
            new DataGridViewTextBoxColumn { Name = "YapilanKmFormatted", HeaderText = "Yapılan KM", Width = 95, DataPropertyName = "YapilanKmFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "TuketilenLitreFormatted", HeaderText = "Tüketim (LT)", Width = 95, DataPropertyName = "TuketilenLitreFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "YuzKmTuketimFormatted", HeaderText = "100 KM (LT)", Width = 95, DataPropertyName = "YuzKmTuketimFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "ToplamMasrafFormatted", HeaderText = "Sefer Masrafı", Width = 110, DataPropertyName = "ToplamMasrafFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "SeferBedeliFormatted", HeaderText = "Navlun Geliri", Width = 110, DataPropertyName = "SeferBedeliFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "NetKarZararFormatted", HeaderText = "Net Kâr/Zarar", Width = 115, DataPropertyName = "NetKarZararFormatted", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } }
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
            _lblStatusInfo.Text = "Sefer kayıtları yükleniyor...";

            var start = _dtpStart.Value;
            var end = _dtpEnd.Value;
            var seferTuru = _cmbSeferTuru.SelectedItem?.ToString() ?? "TÜMÜ";
            var durumu = _cmbDurumu.SelectedItem?.ToString() ?? "TÜMÜ";
            var search = _txtSearch.Text.Trim();

            _seferler = await _service.GetSeferlerAsync(start, end, seferTuru, durumu, search);
            var kpis = await _service.GetKpisAsync(start, end);

            // KPI'ları güncelle
            _lblKpiToplamSefer.Text = $"{kpis.ToplamSefer:N0} Sefer";
            _lblKpiToplamKm.Text = $"{kpis.ToplamKm:N0} KM";
            _lblKpiToplamMasraf.Text = $"{kpis.ToplamMasrafTl:N2} TL";
            _lblKpiNetKar.Text = $"{kpis.ToplamNetKarTl:N2} TL";

            // Grid'i bağla
            var displayList = _seferler.Select(s => new
            {
                Model = s,
                FisNoFormatted = $"SF-{(int)s.FisNo}",
                TarihFormatted = s.Tarih.ToString("dd.MM.yyyy"),
                s.Durumu,
                s.SeferTuru,
                s.Plaka,
                DorsePlaka = string.IsNullOrWhiteSpace(s.DorsePlaka) ? "-" : s.DorsePlaka,
                s.SurucuAdi,
                Guzergah = $"{s.CikisNoktasi} ➔ {s.VarisNoktasi}",
                YapilanKmFormatted = s.YapilanKm > 0 ? $"{s.YapilanKm:N0} KM" : "-",
                TuketilenLitreFormatted = s.TuketilenToplamLitre > 0 ? $"{s.TuketilenToplamLitre:N0} LT" : "-",
                YuzKmTuketimFormatted = s.YuzKmTuketim > 0 ? $"{s.YuzKmTuketim:N2} LT" : "-",
                ToplamMasrafFormatted = $"{s.ToplamMasrafTl:N2} TL",
                SeferBedeliFormatted = $"{s.SeferBedeli:N2} TL",
                NetKarZararFormatted = $"{s.NetKarZararTl:N2} TL"
            }).ToList();

            _grid.DataSource = displayList;
            _lblStatusInfo.Text = $"Toplam {_seferler.Count} sefer listelendi. (Açık: {_seferler.Count(s => s.Durumu == "AÇIK")} | Kapalı: {_seferler.Count(s => s.Durumu == "KAPALI")})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sefer kayıtları yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // 1. Durum Kolonu (Pill Badge)
        if (_grid.Columns["Durumu"] != null && e.ColumnIndex == _grid.Columns["Durumu"]!.Index)
        {
            e.PaintBackground(e.CellBounds, true);
            var durumStr = e.Value?.ToString() ?? "";
            bool isKapali = durumStr.Equals("KAPALI", StringComparison.OrdinalIgnoreCase);

            var badgeBg = isKapali ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 243, 199);
            var badgeFg = isKapali ? Color.FromArgb(22, 101, 52) : Color.FromArgb(180, 83, 9);
            var text = isKapali ? "KAPALI" : "AÇIK (YOLDA)";

            var badgeRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + (e.CellBounds.Height - 20) / 2, e.CellBounds.Width - 12, 20);

            using var brushBg = new SolidBrush(badgeBg);
            using var brushFg = new SolidBrush(badgeFg);
            using var path = GetRoundedRectangle(badgeRect, 10);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brushBg, path);

            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(text, font, brushFg, badgeRect, sf);

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

    private SeferFisDto? GetSelectedSefer()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        var row = _grid.SelectedRows[0];
        dynamic? item = row.DataBoundItem;
        return item?.Model as SeferFisDto;
    }

    private void CreateNewSefer()
    {
        using var dlg = new SeferMaliyetEditDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync();
        }
    }

    private void EditSelectedSefer()
    {
        var selected = GetSelectedSefer();
        if (selected == null)
        {
            MessageBox.Show("Lütfen düzenlemek için bir sefer seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SeferMaliyetEditDialog(selected);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync();
        }
    }

    private async Task KapatSelectedSeferAsync()
    {
        var selected = GetSelectedSefer();
        if (selected == null)
        {
            MessageBox.Show("Lütfen kapatmak istediğiniz seferi seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (selected.Durumu == "KAPALI")
        {
            MessageBox.Show("Bu sefer zaten kapatılmış durumdadır.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new Form
        {
            Text = $"Seferi Kapat — SF-{(int)selected.FisNo}",
            Size = new Size(380, 240),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl1 = new Label { Text = $"Çıkış KM: {selected.CikisKm:N0} KM", Location = new Point(25, 20), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        var lbl2 = new Label { Text = "Dönüş KM:", Location = new Point(25, 55), AutoSize = true };
        var numKm = new NumericUpDown { Location = new Point(140, 52), Width = 120, Maximum = 9999999, Value = (decimal)(selected.DonusKm > 0 ? selected.DonusKm : selected.CikisKm + 1000) };

        var lbl3 = new Label { Text = "Kalan Yakıt (LT):", Location = new Point(25, 95), AutoSize = true };
        var numLt = new NumericUpDown { Location = new Point(140, 92), Width = 120, Maximum = 99999, Value = (decimal)selected.SeferDonusKalanLitre };

        var btnOk = new Button { Text = "Seferi Kapat", Location = new Point(140, 145), Size = new Size(100, 32), BackColor = Color.FromArgb(14, 116, 144), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnCancel = new Button { Text = "İptal", Location = new Point(245, 145), Size = new Size(80, 32), FlatStyle = FlatStyle.Flat };

        btnOk.Click += async (s, e) =>
        {
            if (numKm.Value <= (decimal)selected.CikisKm)
            {
                MessageBox.Show("Dönüş KM'si çıkış KM'sinden büyük olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await _service.KapatSeferAsync(selected.FisNo, (double)numKm.Value, (double)numLt.Value);
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        btnCancel.Click += (s, e) => dlg.Close();

        dlg.Controls.AddRange(new Control[] { lbl1, lbl2, numKm, lbl3, numLt, btnOk, btnCancel });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await LoadDataAsync();
        }
    }

    private void PrintSelectedSefer()
    {
        var selected = GetSelectedSefer();
        if (selected == null)
        {
            MessageBox.Show("Lütfen yazdırmak için bir sefer seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _ = Task.Run(async () =>
        {
            var full = await _service.GetSeferByIdAsync(selected.FisNo);
            this.Invoke(new Action(() =>
            {
                if (full != null)
                {
                    SeferMaliyetEditDialog.PrintSeferRaporu(full);
                }
            }));
        });
    }

    private async Task DeleteSelectedSeferAsync()
    {
        var selected = GetSelectedSefer();
        if (selected == null)
        {
            MessageBox.Show("Lütfen silmek istediğiniz seferi seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"SF-{(int)selected.FisNo} numaralı seferi ({selected.Plaka} - {selected.SurucuAdi}) ve tüm masraf detaylarını silmek istediğinize emin misiniz?",
            "Sefer Silme Onayı",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            await _service.DeleteSeferAsync(selected.FisNo);
            await LoadDataAsync();
        }
    }
}
