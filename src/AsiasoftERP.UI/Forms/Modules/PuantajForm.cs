using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class PuantajForm : Form
{
    private readonly PuantajService _puantajService = new();
    private List<PuantajFisDto> _fisList = new();
    private List<PuantajDetayDto> _detayList = new();
    private PuantajFisDto? _selectedFis;
    private bool _isDirty = false;
    private static readonly CultureInfo TrCulture = new CultureInfo("tr-TR");

    // UI Controls
    private DateTimePicker _dtpBaslangic = null!;
    private DateTimePicker _dtpBitis = null!;
    private TextBox _txtSearch = null!;
    private Button _btnListele = null!;
    private Button _btnEkle = null!;
    private Button _btnDegistir = null!;
    private Button _btnSil = null!;
    private Button _btnExcelMaster = null!;
    private Button _btnCikis = null!;

    private SplitContainer _splitContainer = null!;
    private DataGridView _gridFisler = null!;
    private Panel _pnlMasterFooter = null!;
    private Label _lblMasterSummary = null!;

    // Detail UI Controls
    private Label _lblDetailTitle = null!;
    private Button _btnAmeleEkle = null!;
    private Button _btnAmeleCikar = null!;
    private Button _btnServisDagit = null!;
    private Button _btnKaydet = null!;
    private Button _btnExcelDetail = null!;
    private Button _btnYazdir = null!;
    private DataGridView _gridDetay = null!;
    private Panel _pnlDetailFooter = null!;
    private Label _lblDetailSummary = null!;

    public PuantajForm()
    {
        InitializeComponent();
        ConfigureGrids();
        Load += async (_, _) => await LoadFislerAsync();
    }

    private void InitializeComponent()
    {
        Text = "Puantaj Fişleri & Amele Puantaj Defteri";
        Size = new Size(1380, 850);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(241, 245, 249);
        Font = new Font("Segoe UI", 9f);

        // 1. TOP TOOLBAR PANEL
        var pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(226, 238, 250), // Soft classic blue tint like MacroSoft
            Padding = new Padding(12, 8, 12, 8)
        };
        pnlTopBar.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(190, 210, 235), 1f);
            e.Graphics.DrawLine(pen, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
        };

        var lblTarih = new Label
        {
            Text = "Tarih Aralığı :",
            AutoSize = true,
            Location = new Point(12, 16),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59)
        };

        // Default to current month or September 2026 (matching DB data)
        DateTime now = DateTime.Today;
        _dtpBaslangic = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(now.Year, now.Month, 1),
            Size = new Size(100, 26),
            Location = new Point(102, 12)
        };

        _dtpBitis = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)),
            Size = new Size(100, 26),
            Location = new Point(210, 12)
        };

        _btnListele = CreateToolbarButton("Listele", Color.FromArgb(37, 99, 235), Color.White, 320, 10, 75);
        _btnListele.Click += async (_, _) => await LoadFislerAsync();

        _btnEkle = CreateToolbarButton("Ekle", Color.FromArgb(16, 185, 129), Color.White, 402, 10, 65);
        _btnEkle.Click += async (_, _) => await OnYeniFisEkleAsync();

        _btnDegistir = CreateToolbarButton("Değiştir", Color.FromArgb(245, 158, 11), Color.White, 474, 10, 75);
        _btnDegistir.Click += async (_, _) => await OnFisDuzenleAsync();

        _btnSil = CreateToolbarButton("Sil", Color.FromArgb(239, 68, 68), Color.White, 556, 10, 55);
        _btnSil.Click += async (_, _) => await OnFisSilAsync();

        _btnExcelMaster = CreateToolbarButton("Excel Aktar", Color.FromArgb(13, 148, 136), Color.White, 618, 10, 90);
        _btnExcelMaster.Click += (_, _) => ExportGridToCsv(_gridFisler, "Puantaj_Fisleri");

        // Quick search box
        var lblSearch = new Label
        {
            Text = "🔍",
            AutoSize = true,
            Location = new Point(720, 16),
            Font = new Font("Segoe UI", 10f)
        };
        _txtSearch = new TextBox
        {
            Location = new Point(745, 13),
            Size = new Size(180, 25),
            PlaceholderText = "İş yeri, bölge, fiş no ara..."
        };
        _txtSearch.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await LoadFislerAsync();
            }
        };

        _btnCikis = CreateToolbarButton("Çıkış Esc", Color.FromArgb(100, 116, 139), Color.White, 0, 10, 85);
        _btnCikis.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnCikis.Location = new Point(pnlTopBar.Width - 95, 10);
        _btnCikis.Click += (_, _) => Close();

        pnlTopBar.Controls.AddRange(new Control[]
        {
            lblTarih, _dtpBaslangic, _dtpBitis,
            _btnListele, _btnEkle, _btnDegistir, _btnSil, _btnExcelMaster,
            lblSearch, _txtSearch, _btnCikis
        });

        // 2. SPLIT CONTAINER (Master on Top, Detail on Bottom)
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(203, 213, 225)
        };

        // ==================== MASTER PANEL (Panel 1) ====================
        _gridFisler = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            AutoGenerateColumns = false,
            Font = new Font("Segoe UI", 8.5f)
        };
        _gridFisler.SelectionChanged += async (_, _) => await OnFisSelectionChangedAsync();
        _gridFisler.CellDoubleClick += async (_, _) => await OnFisDuzenleAsync();

        _pnlMasterFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(10, 4, 10, 4)
        };
        _pnlMasterFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
            e.Graphics.DrawLine(pen, 0, 0, _pnlMasterFooter.Width, 0);
        };
        _lblMasterSummary = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            TextAlign = ContentAlignment.MiddleRight,
            Text = "Toplam Fiş: 0  |  Yevmiye: 0  |  Yevmiye Tutarı: 0,00 ₺  |  Servis: 0,00 ₺  |  Hak Ediş: 0,00 ₺  |  Avans: 0,00 ₺"
        };
        _pnlMasterFooter.Controls.Add(_lblMasterSummary);

        _splitContainer.Panel1.Controls.Add(_gridFisler);
        _splitContainer.Panel1.Controls.Add(_pnlMasterFooter);

        // ==================== DETAIL PANEL (Panel 2) ====================
        var pnlDetailHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(235, 243, 252),
            Padding = new Padding(10, 4, 10, 4)
        };
        pnlDetailHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(190, 210, 235), 1f);
            e.Graphics.DrawLine(pen, 0, pnlDetailHeader.Height - 1, pnlDetailHeader.Width, pnlDetailHeader.Height - 1);
        };

        _lblDetailTitle = new Label
        {
            Text = "Yevmiye Fiş Detayı (İşçi Çalışma ve Mesai Çetelesi)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize = true,
            Location = new Point(10, 8)
        };

        int bx = 450;
        _btnAmeleEkle = CreateDetailToolbarButton("➕ Amele Ekle", Color.FromArgb(16, 185, 129), ref bx, 105);
        _btnAmeleEkle.Click += OnAmeleEkleClick;

        _btnAmeleCikar = CreateDetailToolbarButton("🗑️ Çıkar", Color.FromArgb(239, 68, 68), ref bx, 75);
        _btnAmeleCikar.Click += OnAmeleCikarClick;

        _btnServisDagit = CreateDetailToolbarButton("🔄 Servis Dağıt & Hesapla", Color.FromArgb(99, 102, 241), ref bx, 160);
        _btnServisDagit.Click += (_, _) => RecalculateAndDistributeServis();

        _btnKaydet = CreateDetailToolbarButton("💾 Değişiklikleri Kaydet", Color.FromArgb(14, 165, 233), ref bx, 155);
        _btnKaydet.Click += async (_, _) => await SaveCurrentDetayAsync();

        _btnExcelDetail = CreateDetailToolbarButton("Excel Aktar", Color.FromArgb(13, 148, 136), ref bx, 85);
        _btnExcelDetail.Click += (_, _) => ExportGridToCsv(_gridDetay, "Puantaj_Cetele_Detay");

        _btnYazdir = CreateDetailToolbarButton("Yazdır", Color.FromArgb(79, 70, 229), ref bx, 75);
        _btnYazdir.Click += (_, _) => PrintPuantajReport();

        pnlDetailHeader.Controls.AddRange(new Control[]
        {
            _lblDetailTitle, _btnAmeleEkle, _btnAmeleCikar,
            _btnServisDagit, _btnKaydet, _btnExcelDetail, _btnYazdir
        });

        _gridDetay = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            EnableHeadersVisualStyles = false,
            AutoGenerateColumns = false,
            Font = new Font("Segoe UI", 8.5f)
        };
        _gridDetay.CellValueChanged += OnDetailCellValueChanged;
        _gridDetay.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_gridDetay.IsCurrentCellDirty)
                _gridDetay.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        _pnlDetailFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(10, 4, 10, 4)
        };
        _pnlDetailFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
            e.Graphics.DrawLine(pen, 0, 0, _pnlDetailFooter.Width, 0);
        };

        _lblDetailSummary = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleRight,
            Text = "Toplam İşçi: 0  |  Gün: 0.0  |  Yevmiye Tutarı: 0,00 ₺  |  Diğer (Servis): 0,00 ₺  |  Avans: 0,00 ₺  |  Net Hakediş: 0,00 ₺"
        };
        _pnlDetailFooter.Controls.Add(_lblDetailSummary);

        _splitContainer.Panel2.Controls.Add(_gridDetay);
        _splitContainer.Panel2.Controls.Add(_pnlDetailFooter);
        _splitContainer.Panel2.Controls.Add(pnlDetailHeader);

        Controls.Add(_splitContainer);
        Controls.Add(pnlTopBar);

        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            else if (e.KeyCode == Keys.F5) _ = LoadFislerAsync();
        };
    }

    private Button CreateToolbarButton(string text, Color bg, Color fg, int x, int y, int width)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, 30),
            Location = new Point(x, y),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private Button CreateDetailToolbarButton(string text, Color bg, ref int x, int width)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, 26),
            Location = new Point(x, 5),
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        x += width + 6;
        return btn;
    }

    private void ConfigureGrids()
    {
        // Double buffer grids for silky rendering
        var type = typeof(DataGridView);
        var prop = type.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        prop?.SetValue(_gridFisler, true, null);
        prop?.SetValue(_gridDetay, true, null);

        ConfigureMasterColumns();
        ConfigureDetailColumns();
    }

    private void ConfigureMasterColumns()
    {
        _gridFisler.Columns.Clear();
        _gridFisler.ColumnHeadersHeight = 32;
        _gridFisler.RowTemplate.Height = 24;

        // Custom styling colors matching MacroSoft
        var peachHeader = Color.FromArgb(255, 228, 214); // Peach tint
        var normalHeader = Color.FromArgb(238, 242, 246);

        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sira", HeaderText = "Sıra", Width = 45, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "TgAdi", HeaderText = "İş Yeri / Şube", Width = 140, ReadOnly = true });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "BolgeAdi", HeaderText = "Servis Bölgesi", Width = 140, ReadOnly = true });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "FisNo", HeaderText = "Fiş No", Width = 65, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "FatNo", HeaderText = "Belge No", Width = 65, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ay", HeaderText = "Ay", Width = 75, ReadOnly = true });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Yil", HeaderText = "Yıl", Width = 55, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "CFirKod", HeaderText = "Cari Kod", Width = 80, ReadOnly = true });
        _gridFisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "CFirmaAdi", HeaderText = "Cari Hesap Ünvanı", Width = 160, ReadOnly = true });

        // Peach highlighted total columns
        AddMasterCol("YevmiyeToplam", "Yevmiye Toplam", 100, peachHeader, "N1");
        AddMasterCol("MesaiToplam", "Mesai Toplam", 90, peachHeader, "N2");
        AddMasterCol("YevmiyeTutari", "Yevmiye Tutarı", 110, peachHeader, "N2");
        AddMasterCol("MesaiTutari", "Mesai Tutarı", 95, peachHeader, "N2");

        AddMasterCol("EkKazanc", "EK Kazanç", 90, normalHeader, "N2");
        AddMasterCol("ServisSayisi", "Servis Sayısı", 85, normalHeader, "N0");
        AddMasterCol("ServisFiyati", "Servis Fiyatı", 95, normalHeader, "N2");
        AddMasterCol("ServisTutari", "Servis Tutarı", 105, normalHeader, "N2");
        AddMasterCol("KalanServisTutari", "Kalan Servis Tutarı", 115, normalHeader, "N2");

        var colHakedis = AddMasterCol("ToplamHakedis", "Toplam Hak Ediş", 115, Color.FromArgb(220, 252, 231), "N2");
        colHakedis.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        AddMasterCol("Avans", "Avans", 95, normalHeader, "N2");
        AddMasterCol("ServisAvansi", "Servis Avansı", 95, normalHeader, "N2");

        // Cell painting to give selected row bold green appearance like MacroSoft
        _gridFisler.RowPrePaint += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _gridFisler.Rows.Count)
            {
                var row = _gridFisler.Rows[e.RowIndex];
                if (row.Selected)
                {
                    row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(22, 101, 52); // Forest green
                    row.DefaultCellStyle.SelectionForeColor = Color.White;
                }
            }
        };
    }

    private DataGridViewTextBoxColumn AddMasterCol(string name, string header, int width, Color headerBg, string format)
    {
        var col = new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            Width = width,
            ReadOnly = true,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = format }
        };
        col.HeaderCell.Style.BackColor = headerBg;
        col.HeaderCell.Style.ForeColor = Color.FromArgb(30, 41, 59);
        _gridFisler.Columns.Add(col);
        return col;
    }

    private void ConfigureDetailColumns()
    {
        _gridDetay.Columns.Clear();
        _gridDetay.ColumnHeadersHeight = 32;
        _gridDetay.RowTemplate.Height = 24;

        var peachHeader = Color.FromArgb(255, 228, 214);
        var creamHeader = Color.FromArgb(255, 243, 235);

        // 1. PINNED / FROZEN COLUMNS (User never loses worker's name!)
        var colSira = new DataGridViewTextBoxColumn
        {
            Name = "Sira",
            HeaderText = "Sıra",
            Width = 40,
            ReadOnly = true,
            Frozen = true,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.FromArgb(248, 250, 252) }
        };
        _gridDetay.Columns.Add(colSira);

        var colAmele = new DataGridViewTextBoxColumn
        {
            Name = "FirmaAdi",
            HeaderText = "Amele Adı Soyadı",
            Width = 180,
            Frozen = true,
            DefaultCellStyle = { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), BackColor = Color.FromArgb(248, 250, 252) }
        };
        _gridDetay.Columns.Add(colAmele);

        // 2. DAY COLUMNS 1 TO 31 (Pairs: Day Y & Day M)
        for (int d = 1; d <= 31; d++)
        {
            // Yevmiye Day Column (e.g. "1", "2"...)
            var colY = new DataGridViewTextBoxColumn
            {
                Name = $"Y{d}",
                HeaderText = d.ToString(),
                Width = 34,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            colY.HeaderCell.Style.BackColor = peachHeader;
            colY.HeaderCell.Style.ForeColor = Color.FromArgb(15, 23, 42);
            _gridDetay.Columns.Add(colY);

            // Mesai Column (e.g. "M1", "M2"...)
            var colM = new DataGridViewTextBoxColumn
            {
                Name = $"M{d}",
                HeaderText = $"M{d}",
                Width = 36,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.FromArgb(255, 251, 245) }
            };
            colM.HeaderCell.Style.BackColor = creamHeader;
            colM.HeaderCell.Style.ForeColor = Color.FromArgb(100, 116, 139);
            _gridDetay.Columns.Add(colM);
        }

        // 3. SUMMARY & PAYMENT COLUMNS
        AddDetailCol("MesaiToplam", "Mesai Toplam", 75, true, "N2", Color.FromArgb(241, 245, 249));
        AddDetailCol("MesaiUcreti", "Mesai Ücreti", 80, false, "N2");
        AddDetailCol("MesaiTutari", "Mesai Tutarı", 85, true, "N2", Color.FromArgb(241, 245, 249));

        AddDetailCol("ToplamGun", "Toplam Gün", 75, true, "N1", peachHeader);
        AddDetailCol("YevmiyeUcreti", "Yevmiye Ücreti", 85, false, "N2", peachHeader);
        AddDetailCol("YevmiyeTutari", "Yevmiye Tutarı", 95, true, "N2", peachHeader);

        AddDetailCol("DigerUcreti", "Diğer Ücreti", 80, false, "N2");
        AddDetailCol("DigerTutar", "Diğer Tutar", 85, true, "N2", Color.FromArgb(241, 245, 249));

        AddDetailCol("EkKazanc", "Ek Kazanç", 80, false, "N2");
        AddDetailCol("Avans", "Avans", 85, false, "N2");

        var colHakedis = AddDetailCol("ToplamHakedis", "Toplam Hakediş", 100, true, "N2", Color.FromArgb(220, 252, 231));
        colHakedis.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        var colYevmiyeci = new DataGridViewCheckBoxColumn
        {
            Name = "YevmiyeciMi",
            HeaderText = "YEVMİYECİ Mİ?",
            Width = 90
        };
        _gridDetay.Columns.Add(colYevmiyeci);
    }

    private DataGridViewTextBoxColumn AddDetailCol(string name, string header, int width, bool readOnly, string format, Color? bg = null)
    {
        var col = new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            Width = width,
            ReadOnly = readOnly,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = format }
        };
        if (bg.HasValue)
        {
            col.DefaultCellStyle.BackColor = bg.Value;
            col.HeaderCell.Style.BackColor = bg.Value;
        }
        _gridDetay.Columns.Add(col);
        return col;
    }

    // ==================== DATA LOADING ====================

    private async Task LoadFislerAsync()
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            _fisList = await _puantajService.GetPuantajFisleriAsync(_dtpBaslangic.Value, _dtpBitis.Value, _txtSearch.Text);

            _gridFisler.Rows.Clear();
            int sira = 1;
            foreach (var f in _fisList)
            {
                int r = _gridFisler.Rows.Add(
                    sira++,
                    f.TgAdi ?? string.Empty,
                    f.BolgeAdi ?? string.Empty,
                    f.FisNo,
                    f.FatNo,
                    f.Ay ?? string.Empty,
                    f.Yil,
                    f.CFirKod?.ToString() ?? string.Empty,
                    f.CFirmaAdi ?? string.Empty,
                    f.YevmiyeToplam,
                    f.MesaiToplam,
                    f.YevmiyeTutari,
                    f.MesaiTutari,
                    f.EkKazanc,
                    f.ServisSayisi,
                    f.ServisFiyati,
                    f.ServisTutari,
                    f.KalanServisTutari,
                    f.ToplamHakedis,
                    f.Avans,
                    f.ServisAvansi
                );
                _gridFisler.Rows[r].Tag = f;
            }

            UpdateMasterSummary();

            if (_gridFisler.Rows.Count > 0)
            {
                _gridFisler.Rows[0].Selected = true;
                _selectedFis = _gridFisler.Rows[0].Tag as PuantajFisDto;
                await LoadDetayAsync(_selectedFis?.FisNo ?? 0);
            }
            else
            {
                _selectedFis = null;
                _detayList.Clear();
                _gridDetay.Rows.Clear();
                UpdateDetailSummary();
                _lblDetailTitle.Text = "Yevmiye Fiş Detayı - [Kayıt Bulunamadı]";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Puantaj fişleri yüklenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private async Task OnFisSelectionChangedAsync()
    {
        if (_gridFisler.SelectedRows.Count == 0) return;

        if (_isDirty)
        {
            var res = MessageBox.Show("Mevcut puantaj detayında kaydedilmemiş değişiklikler var. Kaydetmek istiyor musunuz?",
                "Kayıt Onayı", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                await SaveCurrentDetayAsync();
            }
            else if (res == DialogResult.Cancel)
            {
                return;
            }
            _isDirty = false;
        }

        var f = _gridFisler.SelectedRows[0].Tag as PuantajFisDto;
        if (f != null && f != _selectedFis)
        {
            _selectedFis = f;
            await LoadDetayAsync(f.FisNo);
        }
    }

    private async Task LoadDetayAsync(int fisNo)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            if (_selectedFis != null)
            {
                _lblDetailTitle.Text = $"Yevmiye Fiş Detayı - [Fiş #{_selectedFis.FisNo} | {_selectedFis.TgAdi} | {_selectedFis.BolgeAdi} | {_selectedFis.Ay} {_selectedFis.Yil}]";
            }

            _detayList = await _puantajService.GetPuantajDetaylariAsync(fisNo);
            PopulateDetailGrid();
            UpdateDetailSummary();
            _isDirty = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fiş detayları yüklenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void PopulateDetailGrid()
    {
        _gridDetay.Rows.Clear();
        int sira = 1;

        foreach (var d in _detayList)
        {
            var rowValues = new List<object>
            {
                sira++,
                d.FirmaAdi
            };

            // Days 1..31
            for (int day = 1; day <= 31; day++)
            {
                var y = d.GetYevmiyeDay(day);
                var m = d.GetMesaiDay(day);
                rowValues.Add(y.HasValue && y.Value > 0 ? (object)y.Value.ToString(TrCulture) : "");
                rowValues.Add(m.HasValue && m.Value > 0 ? (object)m.Value.ToString(TrCulture) : "");
            }

            // Summary columns
            rowValues.Add(d.MesaiToplam);
            rowValues.Add(d.MesaiUcreti);
            rowValues.Add(d.MesaiTutari);
            rowValues.Add(d.ToplamGun);
            rowValues.Add(d.YevmiyeUcreti);
            rowValues.Add(d.YevmiyeTutari);
            rowValues.Add(d.DigerUcreti);
            rowValues.Add(d.DigerTutar);
            rowValues.Add(d.EkKazanc);
            rowValues.Add(d.Avans);
            rowValues.Add(d.ToplamHakedis);
            rowValues.Add(d.YevmiyeciMi);

            int r = _gridDetay.Rows.Add(rowValues.ToArray());
            _gridDetay.Rows[r].Tag = d;
        }
    }

    // ==================== EDITING & CALCULATIONS ====================

    private void OnDetailCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        var row = _gridDetay.Rows[e.RowIndex];
        var d = row.Tag as PuantajDetayDto;
        if (d == null) return;

        string colName = _gridDetay.Columns[e.ColumnIndex].Name;
        object? val = row.Cells[e.ColumnIndex].Value;

        // Check if day column
        if (colName.StartsWith("Y") && int.TryParse(colName.Substring(1), out int yDay) && yDay >= 1 && yDay <= 31)
        {
            d.SetYevmiyeDay(yDay, ParseDouble(val));
        }
        else if (colName.StartsWith("M") && int.TryParse(colName.Substring(1), out int mDay) && mDay >= 1 && mDay <= 31)
        {
            d.SetMesaiDay(mDay, ParseDouble(val));
        }
        else if (colName == "FirmaAdi")
        {
            d.FirmaAdi = val?.ToString() ?? string.Empty;
        }
        else if (colName == "YevmiyeUcreti")
        {
            d.YevmiyeUcreti = ParseDecimal(val);
        }
        else if (colName == "MesaiUcreti")
        {
            d.MesaiUcreti = ParseDecimal(val);
        }
        else if (colName == "DigerUcreti")
        {
            d.DigerUcreti = ParseDecimal(val);
        }
        else if (colName == "EkKazanc")
        {
            d.EkKazanc = ParseDecimal(val);
        }
        else if (colName == "Avans")
        {
            d.Avans = ParseDecimal(val);
        }
        else if (colName == "YevmiyeciMi")
        {
            d.YevmiyeciMi = val is bool b ? b : (val?.ToString() == "True");
        }

        // Recalculate row totals
        d.RecalculateTotals();

        // Update read-only cell values in the grid
        row.Cells["MesaiToplam"].Value = d.MesaiToplam;
        row.Cells["MesaiTutari"].Value = d.MesaiTutari;
        row.Cells["ToplamGun"].Value = d.ToplamGun;
        row.Cells["YevmiyeTutari"].Value = d.YevmiyeTutari;
        row.Cells["DigerTutar"].Value = d.DigerTutar;
        row.Cells["ToplamHakedis"].Value = d.ToplamHakedis;

        _isDirty = true;
        UpdateDetailSummary();
    }

    private double? ParseDouble(object? val)
    {
        if (val == null) return null;
        string s = val.ToString()?.Trim().Replace(',', '.') ?? "";
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) && d > 0)
            return d;
        return null;
    }

    private decimal ParseDecimal(object? val)
    {
        if (val == null) return 0;
        string s = val.ToString()?.Trim().Replace(',', '.') ?? "";
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
            return d;
        return 0;
    }

    private void RecalculateAndDistributeServis()
    {
        if (_selectedFis == null || _detayList.Count == 0) return;

        _puantajService.DistributeServisCost(_selectedFis, _detayList);
        PopulateDetailGrid();
        UpdateDetailSummary();
        _isDirty = true;
        MessageBox.Show($"Servis tutarı ({_selectedFis.ServisTutari:N2} ₺) çalışanların iş günlerine göre kişi başı gün başına {_detayList.FirstOrDefault()?.DigerUcreti:N2} ₺ olarak dağıtıldı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task SaveCurrentDetayAsync()
    {
        if (_selectedFis == null) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            await _puantajService.SavePuantajFisAsync(_selectedFis, _detayList);
            _isDirty = false;
            NotificationService.NotifySuccess("Puantaj Kaydedildi", $"Fiş #{_selectedFis.FisNo} detayları ve toplamları başarıyla güncellendi.");
            await LoadFislerAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void OnAmeleEkleClick(object? sender, EventArgs e)
    {
        if (_selectedFis == null)
        {
            MessageBox.Show("Lütfen önce bir puantaj fişi seçiniz veya oluşturunuz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new AmeleEkleDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var amele = dlg.Amele;
            amele.FisNo = _selectedFis.FisNo;
            amele.FatNo = _selectedFis.FatNo;
            amele.Ay = _selectedFis.Ay;
            amele.Yil = _selectedFis.Yil;
            amele.Sira = _detayList.Count + 1;
            amele.RecalculateTotals();

            _detayList.Add(amele);
            PopulateDetailGrid();
            UpdateDetailSummary();
            _isDirty = true;
        }
    }

    private void OnAmeleCikarClick(object? sender, EventArgs e)
    {
        if (_gridDetay.SelectedCells.Count == 0) return;
        int rowIndex = _gridDetay.SelectedCells[0].RowIndex;
        if (rowIndex < 0 || rowIndex >= _detayList.Count) return;

        var amele = _detayList[rowIndex];
        var res = MessageBox.Show($"'{amele.FirmaAdi}' isimli işçiyi bu puantaj fişinden çıkarmak istediğinize emin misiniz?",
            "İşçi Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            _detayList.RemoveAt(rowIndex);
            PopulateDetailGrid();
            UpdateDetailSummary();
            _isDirty = true;
        }
    }

    // ==================== CRUD ON MASTER ====================

    private async Task OnYeniFisEkleAsync()
    {
        using var dlg = new PuantajFisEditDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var fis = dlg.Fis;
                int newFisNo = await _puantajService.SavePuantajFisAsync(fis, new List<PuantajDetayDto>());
                NotificationService.NotifySuccess("Yeni Fiş Oluşturuldu", $"Fiş #{newFisNo} başarıyla oluşturuldu.");
                await LoadFislerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Puantaj fişi oluşturulurken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }

    private async Task OnFisDuzenleAsync()
    {
        if (_selectedFis == null)
        {
            MessageBox.Show("Lütfen düzenlemek istediğiniz fişi seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new PuantajFisEditDialog(_selectedFis);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                await _puantajService.SavePuantajFisAsync(dlg.Fis, _detayList);
                NotificationService.NotifySuccess("Fiş Güncellendi", $"Fiş #{dlg.Fis.FisNo} bilgileri güncellendi.");
                await LoadFislerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fiş güncellenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }

    private async Task OnFisSilAsync()
    {
        if (_selectedFis == null) return;

        var res = MessageBox.Show($"Fiş #{_selectedFis.FisNo} ({_selectedFis.TgAdi} - {_selectedFis.BolgeAdi}) ve ait tüm işçi çalışma kayıtları tamamen silinecektir.\nOnaylıyor musunuz?",
            "Fiş Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (res == DialogResult.Yes)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                await _puantajService.DeletePuantajFisAsync(_selectedFis.FisNo);
                NotificationService.NotifySuccess("Fiş Silindi", $"Fiş #{_selectedFis.FisNo} başarıyla silindi.");
                await LoadFislerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fiş silinirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }

    // ==================== SUMMARIES ====================

    private void UpdateMasterSummary()
    {
        int count = _fisList.Count;
        decimal yevmiye = _fisList.Sum(f => f.YevmiyeToplam);
        decimal yevmiyeTutari = _fisList.Sum(f => f.YevmiyeTutari);
        decimal servisTutari = _fisList.Sum(f => f.ServisTutari);
        decimal hakedis = _fisList.Sum(f => f.ToplamHakedis);
        decimal avans = _fisList.Sum(f => f.Avans);

        _lblMasterSummary.Text = $"Toplam Fiş: {count}  |  Yevmiye Gün: {yevmiye:N1}  |  Yevmiye Tutarı: {yevmiyeTutari:N2} ₺  |  Servis Tutarı: {servisTutari:N2} ₺  |  Toplam Hak Ediş: {hakedis:N2} ₺  |  Avans: {avans:N2} ₺";
    }

    private void UpdateDetailSummary()
    {
        int count = _detayList.Count;
        decimal gun = _detayList.Sum(d => d.ToplamGun);
        decimal yevmiyeTutari = _detayList.Sum(d => d.YevmiyeTutari);
        decimal digerTutar = _detayList.Sum(d => d.DigerTutar);
        decimal avans = _detayList.Sum(d => d.Avans);
        decimal hakedis = _detayList.Sum(d => d.ToplamHakedis);

        // Calculate daily counts for the 31 days footer string
        var dailyWorkers = new StringBuilder();
        int activeDays = 0;
        for (int day = 1; day <= 31; day++)
        {
            double dayTotal = _detayList.Sum(d => d.GetYevmiyeDay(day) ?? 0);
            if (dayTotal > 0) activeDays++;
        }

        _lblDetailSummary.Text = $"İşçi Sayısı: {count}  |  Çalışılan Gün Sayısı: {activeDays}  |  Toplam Yevmiye: {gun:N1}  |  Yevmiye Tutarı: {yevmiyeTutari:N2} ₺  |  Diğer (Servis): {digerTutar:N2} ₺  |  Avans: {avans:N2} ₺  |  NET HAKEDİŞ: {hakedis:N2} ₺";
    }

    // ==================== EXPORT & PRINT ====================

    private void ExportGridToCsv(DataGridView grid, string baseName)
    {
        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Dosyası (*.csv)|*.csv|Metin Dosyası (*.txt)|*.txt",
                FileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var sb = new StringBuilder();
            // Headers
            var visibleCols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
            sb.AppendLine(string.Join(";", visibleCols.Select(c => $"\"{c.HeaderText}\"")));

            // Rows
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var line = visibleCols.Select(c =>
                {
                    var val = row.Cells[c.Index].Value;
                    return val != null ? $"\"{val.ToString()?.Replace("\"", "\"\"")}\"" : "\"\"";
                });
                sb.AppendLine(string.Join(";", line));
            }

            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
            NotificationService.NotifySuccess("Excel/CSV Aktarıldı", $"Veriler '{Path.GetFileName(sfd.FileName)}' dosyasına kaydedildi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dışa aktarma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PrintPuantajReport()
    {
        if (_selectedFis == null || _detayList.Count == 0)
        {
            MessageBox.Show("Yazdırılacak puantaj verisi bulunmamaktadır.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var printDoc = new PrintDocument();
        printDoc.DefaultPageSettings.Landscape = true;
        printDoc.DocumentName = $"Puantaj_Fis_{_selectedFis.FisNo}";

        printDoc.PrintPage += (s, e) =>
        {
            var g = e.Graphics!;
            int left = 40;
            int top = 30;

            // Header
            using var fontTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
            using var fontSub = new Font("Segoe UI", 9f);
            using var fontBold = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var fontRegular = new Font("Segoe UI", 7.5f);

            g.DrawString("ASIASOFT ERP - İŞÇİ / AMELE AYLIK PUANTAJ ÇETELESİ", fontTitle, Brushes.Black, left, top);
            top += 25;

            string info = $"İş Yeri / Şube: {_selectedFis.TgAdi}   |   Bölge: {_selectedFis.BolgeAdi}   |   Dönem: {_selectedFis.Ay} {_selectedFis.Yil}   |   Fiş No: #{_selectedFis.FisNo}   |   Belge No: {_selectedFis.FatNo}";
            g.DrawString(info, fontSub, Brushes.Black, left, top);
            top += 25;

            // Mini Table Header
            int y = top;
            int colWidth = 22;
            int ameleWidth = 140;

            g.FillRectangle(Brushes.LightGray, left, y, 1050, 20);
            g.DrawRectangle(Pens.Black, left, y, 1050, 20);
            g.DrawString("Amele Adı Soyadı", fontBold, Brushes.Black, left + 4, y + 3);

            for (int d = 1; d <= 31; d++)
            {
                g.DrawString(d.ToString(), fontBold, Brushes.Black, left + ameleWidth + (d - 1) * colWidth + 4, y + 3);
            }
            g.DrawString("Gün", fontBold, Brushes.Black, left + ameleWidth + 31 * colWidth + 4, y + 3);
            g.DrawString("Yevmiye", fontBold, Brushes.Black, left + ameleWidth + 31 * colWidth + 35, y + 3);
            g.DrawString("Hakediş", fontBold, Brushes.Black, left + ameleWidth + 31 * colWidth + 90, y + 3);
            g.DrawString("İmza", fontBold, Brushes.Black, left + ameleWidth + 31 * colWidth + 150, y + 3);

            y += 20;

            // Worker Rows
            foreach (var w in _detayList)
            {
                if (y > e.MarginBounds.Bottom - 40) break; // Simple single page cutoff for preview

                g.DrawRectangle(Pens.LightGray, left, y, 1050, 18);
                g.DrawString(w.FirmaAdi, fontRegular, Brushes.Black, left + 4, y + 2);

                for (int d = 1; d <= 31; d++)
                {
                    var val = w.GetYevmiyeDay(d);
                    if (val.HasValue && val.Value > 0)
                    {
                        g.DrawString(val.Value == 1 ? "1" : "½", fontBold, Brushes.Black, left + ameleWidth + (d - 1) * colWidth + 6, y + 2);
                    }
                }

                g.DrawString(w.ToplamGun.ToString("N1"), fontBold, Brushes.Black, left + ameleWidth + 31 * colWidth + 4, y + 2);
                g.DrawString(w.YevmiyeTutari.ToString("N0") + " ₺", fontRegular, Brushes.Black, left + ameleWidth + 31 * colWidth + 35, y + 2);
                g.DrawString(w.ToplamHakedis.ToString("N0") + " ₺", fontBold, Brushes.Black, left + ameleWidth + 31 * colWidth + 90, y + 2);

                y += 18;
            }

            // Summary row
            y += 5;
            g.DrawString($"TOPLAM ÇALIŞAN: {_detayList.Count} Kişi  |  TOPLAM GÜN: {_detayList.Sum(x => x.ToplamGun):N1}  |  TOPLAM HAKEDİŞ: {_detayList.Sum(x => x.ToplamHakedis):N2} ₺",
                fontBold, Brushes.Black, left, y);

            e.HasMorePages = false;
        };

        using var ppd = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 1100,
            Height = 750,
            StartPosition = FormStartPosition.CenterParent
        };
        ppd.ShowDialog(this);
    }
}
