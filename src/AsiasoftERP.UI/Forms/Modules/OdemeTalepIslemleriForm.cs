using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Ödeme Talep İşlemleri Modülü
/// Pixel-accurate modern replication of MacroSoft ERP Payment Request System
/// Features 5 status tabs, date filters, exact column layout, custom cell styling, and summary statistics
/// </summary>
public class OdemeTalepIslemleriForm : Form
{
    private readonly OdemeTalepService _service = new();

    // Filter controls
    private DateTimePicker _dtpBaslangic = null!;
    private DateTimePicker _dtpBitis = null!;
    private Button _btnListele = null!;
    private Button _btnEkle = null!;
    private Button _btnDegistir = null!;
    private Button _btnIncele = null!;
    private Button _btnCikis = null!;
    private TextBox _txtArama = null!;
    private ComboBox _cmbSirketFiltre = null!;
    private Button _btnYazdir = null!;
    private Button _btnExcel = null!;

    // Tab buttons
    private Panel _pnlTabs = null!;
    private Button _tabGenel = null!;
    private Button _tabDevam = null!;
    private Button _tabOnaylanan = null!;
    private Button _tabRet = null!;
    private Button _tabKarsilastirma = null!;
    private string _activeTab = "Genel Talep Listesi";

    // Grids
    private Panel _pnlGridContainer = null!;
    private DataGridView _gridMain = null!;
    private DataGridView _gridKarsilastirma = null!;

    // Footer summary controls
    private Label _lblKayitSayisi = null!;
    private Label _lblSeciliBilgi = null!;
    private Label _lblToplamTalep = null!;
    private Label _lblToplamBagkur = null!;
    private Label _lblToplamNet = null!;

    // Data cache
    private List<OdemeTalepDto> _allRows = new();
    private List<OdemeTalepDto> _filteredRows = new();
    private List<OdemeTalepKarsilastirmaDto> _karsilastirmaRows = new();

    public OdemeTalepIslemleriForm()
    {
        InitializeComponents();
        this.Load += async (_, _) => await LoadDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "Ödeme Talep İşlemleri";
        this.Size = new Size(1380, 820);
        this.MinimumSize = new Size(1100, 650);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(241, 245, 249);
        this.KeyPreview = true;

        this.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.F5) await LoadDataAsync();
            if (e.KeyCode == Keys.F2) OpenDetayDialog(isNew: true);
            if (e.KeyCode == Keys.F3) OpenDetayDialog(isNew: false, isReadOnly: false);
            if (e.KeyCode == Keys.F4) OpenDetayDialog(isNew: false, isReadOnly: true);
            if (e.KeyCode == Keys.Escape) this.Close();
        };

        // 1. Top Command Panel
        var pnlTop = CreateTopPanel();

        // 2. Tab Strip Panel
        _pnlTabs = CreateTabStrip();

        // 3. Sub Toolbar Panel (Search + Export)
        var pnlSubTool = CreateSubToolbar();

        // 4. Center Grid Area
        _pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 4, 12, 4),
            BackColor = Color.FromArgb(241, 245, 249)
        };

        CreateMainGrid();
        CreateKarsilastirmaGrid();
        _pnlGridContainer.Controls.Add(_gridMain);
        _pnlGridContainer.Controls.Add(_gridKarsilastirma);

        // 5. Footer Summary Bar
        var pnlFooter = CreateFooterPanel();

        // Assemble controls in correct docking order
        this.Controls.Add(_pnlGridContainer);
        this.Controls.Add(pnlSubTool);
        this.Controls.Add(_pnlTabs);
        this.Controls.Add(pnlTop);
        this.Controls.Add(pnlFooter);
    }

    #region UI Construction

    private Panel CreateTopPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        // Left: Module Icon & Date Range
        var lblModuleIcon = new Label
        {
            Text = "₺",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 185, 129),
            Size = new Size(32, 28),
            Location = new Point(12, 14),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblDateTitle = new Label
        {
            Text = "Tarih Aralığı:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(52, 18),
            AutoSize = true
        };

        // Initialize with default date range matching MacroSoft ERP screenshot
        _dtpBaslangic = new DateTimePicker
        {
            Location = new Point(142, 15),
            Width = 115,
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(2026, 10, 8),
            Font = new Font("Segoe UI", 9.5f)
        };

        var lblSep = new Label
        {
            Text = "-",
            Location = new Point(262, 17),
            AutoSize = true,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        _dtpBitis = new DateTimePicker
        {
            Location = new Point(279, 15),
            Width = 115,
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(2026, 10, 15),
            Font = new Font("Segoe UI", 9.5f)
        };

        // Center / Action Buttons
        int btnX = 405;

        _btnListele = CreateActionButton("Listele (F5)", Color.FromArgb(15, 23, 42), Color.White, btnX, 12, 95);
        _btnListele.Click += async (_, _) => await LoadDataAsync();
        btnX += 103;

        _btnEkle = CreateActionButton("+ Ekle (F2)", Color.FromArgb(16, 185, 129), Color.White, btnX, 12, 90);
        _btnEkle.Click += (_, _) => OpenDetayDialog(isNew: true);
        btnX += 98;

        _btnDegistir = CreateActionButton("Değiştir (F3)", Color.FromArgb(2, 132, 199), Color.White, btnX, 12, 100);
        _btnDegistir.Click += (_, _) => OpenDetayDialog(isNew: false, isReadOnly: false);
        btnX += 108;

        _btnIncele = CreateActionButton("İncele (F4)", Color.FromArgb(100, 116, 139), Color.White, btnX, 12, 95);
        _btnIncele.Click += (_, _) => OpenDetayDialog(isNew: false, isReadOnly: true);
        btnX += 103;

        _btnCikis = CreateActionButton("Çıkış", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), btnX, 12, 80);
        _btnCikis.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnCikis.Click += (_, _) => this.Close();

        // Company filter on far right
        var lblSirket = new Label
        {
            Text = "Şirket:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnl.Width - 250, 19),
            AutoSize = true
        };

        _cmbSirketFiltre = new ComboBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnl.Width - 195, 15),
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cmbSirketFiltre.Items.AddRange(new object[] { "Tümü", "1 - TEK ASYA A.Ş.", "2 - LAREX", "3 - AL SELEVIA" });
        _cmbSirketFiltre.SelectedIndex = 1; // Default to TEK ASYA
        _cmbSirketFiltre.SelectedIndexChanged += async (_, _) => await LoadDataAsync();

        pnl.Controls.AddRange(new Control[]
        {
            lblModuleIcon, lblDateTitle, _dtpBaslangic, lblSep, _dtpBitis,
            _btnListele, _btnEkle, _btnDegistir, _btnIncele, _btnCikis,
            lblSirket, _cmbSirketFiltre
        });

        // Bottom separator line
        var sepLine = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };
        pnl.Controls.Add(sepLine);

        return pnl;
    }

    private Button CreateActionButton(string text, Color bg, Color fg, int x, int y, int width)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 36),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = bg == Color.White || bg.R > 230 ? 1 : 0;
        return btn;
    }

    private Panel CreateTabStrip()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(15, 6, 15, 0)
        };

        int tabX = 15;
        _tabGenel = CreateTabButton("Genel Talep Listesi", ref tabX, true);
        _tabDevam = CreateTabButton("Devam Eden Talepler", ref tabX, false);
        _tabOnaylanan = CreateTabButton("Onaylanan Talepler", ref tabX, false);
        _tabRet = CreateTabButton("Ret Edilen Talepler", ref tabX, false);
        _tabKarsilastirma = CreateTabButton("Ödeme Talep Karşılaştırma", ref tabX, false);

        pnl.Controls.AddRange(new Control[] { _tabGenel, _tabDevam, _tabOnaylanan, _tabRet, _tabKarsilastirma });

        var sepLine = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(203, 213, 225)
        };
        pnl.Controls.Add(sepLine);

        return pnl;
    }

    private Button CreateTabButton(string text, ref int x, bool isActive)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 6),
            Size = new Size(185, 36),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, isActive ? FontStyle.Bold : FontStyle.Regular),
            BackColor = isActive ? Color.White : Color.FromArgb(241, 245, 249),
            ForeColor = isActive ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139),
            Cursor = Cursors.Hand,
            Tag = text
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += async (s, _) => await OnTabClicked((Button)s!);

        x += 190;
        return btn;
    }

    private async Task OnTabClicked(Button clickedTab)
    {
        foreach (var ctrl in _pnlTabs.Controls)
        {
            if (ctrl is Button btn)
            {
                bool isSelected = (btn == clickedTab);
                btn.BackColor = isSelected ? Color.White : Color.FromArgb(241, 245, 249);
                btn.ForeColor = isSelected ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139);
                btn.Font = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        _activeTab = clickedTab.Tag?.ToString() ?? "Genel Talep Listesi";

        if (_activeTab == "Ödeme Talep Karşılaştırma")
        {
            _gridMain.Visible = false;
            _gridKarsilastirma.Visible = true;
            _gridKarsilastirma.BringToFront();
            await LoadKarsilastirmaAsync();
        }
        else
        {
            _gridKarsilastirma.Visible = false;
            _gridMain.Visible = true;
            _gridMain.BringToFront();
            await LoadDataAsync();
        }
    }

    private Panel CreateSubToolbar()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.White,
            Padding = new Padding(15, 6, 15, 6)
        };

        _btnYazdir = new Button
        {
            Text = "Yazdır",
            Size = new Size(85, 32),
            Location = new Point(15, 7),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnYazdir.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnYazdir.Click += (_, _) => PrintCurrentView();

        _btnExcel = new Button
        {
            Text = "Excel Aktar",
            Size = new Size(100, 32),
            Location = new Point(108, 7),
            BackColor = Color.FromArgb(240, 253, 244),
            ForeColor = Color.FromArgb(22, 101, 52),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnExcel.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
        _btnExcel.Click += (_, _) => ExportToExcel();

        // Search Input on Right
        _txtArama = new TextBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnl.Width - 320, 8),
            Width = 300,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Text = "Hızlı Arama (Firma, Fiş No, Şube)..."
        };
        _txtArama.GotFocus += (_, _) =>
        {
            if (_txtArama.Text == "Hızlı Arama (Firma, Fiş No, Şube)...")
            {
                _txtArama.Text = "";
                _txtArama.ForeColor = Color.FromArgb(15, 23, 42);
            }
        };
        _txtArama.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtArama.Text))
            {
                _txtArama.Text = "Hızlı Arama (Firma, Fiş No, Şube)...";
                _txtArama.ForeColor = Color.FromArgb(100, 116, 139);
            }
        };
        _txtArama.TextChanged += (_, _) => ApplyFilter();

        pnl.Controls.AddRange(new Control[] { _btnYazdir, _btnExcel, _txtArama });

        var sepLine = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };
        pnl.Controls.Add(sepLine);

        return pnl;
    }

    private void CreateMainGrid()
    {
        _gridMain = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(226, 232, 240),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 28 },
            Font = new Font("Segoe UI", 9.25f)
        };

        // Header Styling
        _gridMain.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _gridMain.ColumnHeadersHeight = 36;
        _gridMain.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _gridMain.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
        _gridMain.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _gridMain.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        // Selection Styling
        _gridMain.DefaultCellStyle.SelectionBackColor = Color.FromArgb(198, 246, 213); // Soft green selection
        _gridMain.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

        // Columns exact 1:1 match with MacroSoft ERP screenshot:
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Sira", HeaderText = "Sıra", Width = 50, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "YetkiKodu", HeaderText = "İş Yeri / Şube", Width = 110 });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GrupSirket", HeaderText = "Grup Şirket", Width = 95 });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GonderenHesap", HeaderText = "Gönderen Hesap", Width = 115 });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Turu", HeaderText = "Türü", Width = 100 });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FisNo", HeaderText = "Fiş No", Width = 75, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FisTar", HeaderText = "Tarih", Width = 90, DefaultCellStyle = { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IslemTarihi", HeaderText = "İşlem Tarihi", Width = 135, DefaultCellStyle = { Format = "dd.MM.yyyy HH:mm:ss", Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "KasaAdi", HeaderText = "Kasa Adı", Width = 110 });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FirAdi", HeaderText = "Cari Hesap Ünvanı", Width = 230, DefaultCellStyle = { Font = new Font("Segoe UI", 9.25f, FontStyle.Bold) } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BelgeVeyaFaturaNo", HeaderText = "Belge No / Fatura No", Width = 140 });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BagkurVarmi", HeaderText = "Bağkur Kesinti ?", Width = 115, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TalepTutar", HeaderText = "Talep Tutarı", Width = 125, DefaultCellStyle = { Format = "#,##0.00 TL", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BagkurOrani", HeaderText = "Bağkur Kesinti Oranı%", Width = 130, DefaultCellStyle = { Format = "0.##'%'", Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BagkurTutar", HeaderText = "Bağkur Kesinti Tutarı", Width = 130, DefaultCellStyle = { Format = "#,##0.00 TL", Alignment = DataGridViewContentAlignment.MiddleRight } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Durum", HeaderText = "Durum", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridMain.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Aciklama", HeaderText = "Açıklama", Width = 180 });

        // Cell formatting for screenshot row highlights and red text
        _gridMain.CellFormatting += GridMain_CellFormatting;

        // Custom Header Painting: Kasa Adı and Bağkur columns have Red text in screenshot!
        _gridMain.CellPainting += GridMain_CellPainting;

        // Row selection changed
        _gridMain.SelectionChanged += (_, _) => UpdateSelectionInfo();

        // Double click to view details
        _gridMain.CellDoubleClick += (_, _) => OpenDetayDialog(isNew: false, isReadOnly: true);
    }

    private void CreateKarsilastirmaGrid()
    {
        _gridKarsilastirma = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(226, 232, 240),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 32 },
            Font = new Font("Segoe UI", 9.5f),
            Visible = false
        };

        _gridKarsilastirma.ColumnHeadersHeight = 36;
        _gridKarsilastirma.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _gridKarsilastirma.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
        _gridKarsilastirma.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Sube", HeaderText = "İş Yeri / Şube", Width = 160, DefaultCellStyle = { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Turu", HeaderText = "Ödeme Türü", Width = 140 });
        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TalepAdedi", HeaderText = "Talep Adedi", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ToplamTalepTutari", HeaderText = "Toplam Talep Tutarı", Width = 170, DefaultCellStyle = { Format = "#,##0.00 TL", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OnaylananTutar", HeaderText = "Onaylanan / Aktarılan", Width = 170, DefaultCellStyle = { Format = "#,##0.00 TL", Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(22, 101, 52) } });
        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RetEdilenTutar", HeaderText = "Ret Edilen Tutar", Width = 150, DefaultCellStyle = { Format = "#,##0.00 TL", Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(185, 28, 28) } });
        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DevamEdenTutar", HeaderText = "Devam Eden / Bekleyen", Width = 170, DefaultCellStyle = { Format = "#,##0.00 TL", Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(194, 65, 12) } });
        _gridKarsilastirma.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "KarsilamaOrani", HeaderText = "Onay Oranı (%)", Width = 120, DefaultCellStyle = { Format = "0.00'%'", Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
    }

    private Panel CreateFooterPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.White,
            Padding = new Padding(15, 6, 15, 6)
        };

        var topBorder = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };
        pnl.Controls.Add(topBorder);

        // Left Side: Record Counter (e.g. 1 / 266)
        _lblKayitSayisi = new Label
        {
            Text = "1 / 0",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(15, 14),
            AutoSize = true
        };

        _lblSeciliBilgi = new Label
        {
            Text = "Kayıt seçilmedi",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(110, 16),
            AutoSize = true
        };

        // Right Side: Grand Totals (Talep Tutarı Toplamı, Bağkur Kesintisi, Net Tutar)
        _lblToplamTalep = CreateFooterBadge("Talep Tutarı Toplamı: 0,00 TL", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70), AnchorStyles.Top | AnchorStyles.Right);
        _lblToplamBagkur = CreateFooterBadge("Bağkur: 0,00 TL", Color.FromArgb(254, 242, 242), Color.FromArgb(153, 27, 27), AnchorStyles.Top | AnchorStyles.Right);
        _lblToplamNet = CreateFooterBadge("Net Ödeme: 0,00 TL", Color.FromArgb(241, 245, 249), Color.FromArgb(30, 41, 59), AnchorStyles.Top | AnchorStyles.Right);

        // Position totals horizontally on the right
        pnl.Resize += (_, _) => RepositionFooterBadges(pnl);

        pnl.Controls.AddRange(new Control[] { _lblKayitSayisi, _lblSeciliBilgi, _lblToplamNet, _lblToplamBagkur, _lblToplamTalep });
        return pnl;
    }

    private Label CreateFooterBadge(string text, Color bg, Color fg, AnchorStyles anchor)
    {
        var lbl = new Label
        {
            Text = text,
            BackColor = bg,
            ForeColor = fg,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(10, 6, 10, 6),
            AutoSize = true,
            Anchor = anchor
        };
        return lbl;
    }

    private void RepositionFooterBadges(Panel pnl)
    {
        int right = pnl.Width - 15;

        _lblToplamNet.Location = new Point(right - _lblToplamNet.Width, 9);
        right -= (_lblToplamNet.Width + 10);

        _lblToplamBagkur.Location = new Point(right - _lblToplamBagkur.Width, 9);
        right -= (_lblToplamBagkur.Width + 10);

        _lblToplamTalep.Location = new Point(right - _lblToplamTalep.Width, 9);
    }

    #endregion

    #region Custom Rendering & Formatting

    private void GridMain_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        // Check if header row cell
        if (e.RowIndex == -1 && e.ColumnIndex >= 0)
        {
            string colName = _gridMain.Columns[e.ColumnIndex].DataPropertyName;

            // MacroSoft ERP red header columns: KasaAdi, BagkurVarmi, BagkurOrani, BagkurTutar
            if (colName == "KasaAdi" || colName == "BagkurVarmi" || colName == "BagkurOrani" || colName == "BagkurTutar")
            {
                e.PaintBackground(e.CellBounds, true);
                using (var brush = new SolidBrush(Color.FromArgb(197, 48, 48))) // MacroSoft Red accent
                using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near })
                {
                    var rect = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y, e.CellBounds.Width - 8, e.CellBounds.Height);
                    e.Graphics?.DrawString(e.Value?.ToString() ?? "", font, brush, rect, sf);
                }
                e.Handled = true;
            }
        }
    }

    private void GridMain_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _filteredRows.Count) return;

        var row = _filteredRows[e.RowIndex];
        var gridRow = _gridMain.Rows[e.RowIndex];

        // Row background tint based on Türü and Durum:
        if (row.Durum.ToUpperInvariant().Contains("RET"))
        {
            gridRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242); // Soft light red
        }
        else if (row.Turu.ToUpperInvariant().Contains("NAKİT"))
        {
            gridRow.DefaultCellStyle.BackColor = Color.FromArgb(224, 242, 254); // Soft ice cyan
        }
        else if (row.Turu.ToUpperInvariant().Contains("AVANS") || e.RowIndex == 0)
        {
            gridRow.DefaultCellStyle.BackColor = Color.FromArgb(220, 252, 231); // Soft emerald mint
        }
        else if (e.RowIndex % 2 == 1)
        {
            gridRow.DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252); // Soft zebra
        }
        else
        {
            gridRow.DefaultCellStyle.BackColor = Color.White;
        }

        // Column specific text styling
        string colName = _gridMain.Columns[e.ColumnIndex].DataPropertyName;

        if (colName == "BagkurVarmi")
        {
            string val = e.Value?.ToString() ?? "";
            if (val == "HAYIR")
            {
                e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38); // Red "HAYIR"
                e.CellStyle.SelectionForeColor = Color.FromArgb(185, 28, 28);
            }
            else if (val == "EVET")
            {
                e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52); // Green "EVET"
            }
        }
        else if (colName == "Durum")
        {
            string durum = e.Value?.ToString() ?? "";
            if (durum.Contains("RET"))
            {
                e.CellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            else if (durum == "Onaylandı" || durum == "Aktarildi" || durum == "Ödendi")
            {
                e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            else
            {
                e.CellStyle.ForeColor = Color.FromArgb(194, 65, 12);
            }
        }
    }

    #endregion

    #region Data Loading & Filtering

    private async Task LoadDataAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            _btnListele.Enabled = false;

            string? selectedSirKod = GetSelectedSirKod();

            _allRows = await _service.GetOdemeTalepleriAsync(
                _dtpBaslangic.Value,
                _dtpBitis.Value,
                _activeTab,
                "",
                selectedSirKod
            );

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnListele.Enabled = true;
            Cursor.Current = Cursors.Default;
        }
    }

    private async Task LoadKarsilastirmaAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            string? selectedSirKod = GetSelectedSirKod();

            _karsilastirmaRows = await _service.GetKarsilastirmaAsync(
                _dtpBaslangic.Value,
                _dtpBitis.Value,
                selectedSirKod
            );

            _gridKarsilastirma.DataSource = null;
            _gridKarsilastirma.DataSource = _karsilastirmaRows;

            // Update footer
            int totalRequests = _karsilastirmaRows.Sum(x => x.TalepAdedi);
            decimal totalAmount = _karsilastirmaRows.Sum(x => x.ToplamTalepTutari);
            decimal totalApproved = _karsilastirmaRows.Sum(x => x.OnaylananTutar);

            _lblKayitSayisi.Text = $"1 / {_karsilastirmaRows.Count}";
            _lblSeciliBilgi.Text = $"Toplam {totalRequests:N0} adet ödeme talebi karşılaştırıldı.";
            _lblToplamTalep.Text = $"Toplam Talep: {totalAmount:N2} TL";
            _lblToplamBagkur.Text = $"Onaylanan: {totalApproved:N2} TL";
            _lblToplamNet.Text = $"Fark: {(totalAmount - totalApproved):N2} TL";

            if (pnlFooterParent() is Panel pnl) RepositionFooterBadges(pnl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Karşılaştırma verileri yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void ApplyFilter()
    {
        string term = _txtArama.Text.Trim();
        if (term == "Hızlı Arama (Firma, Fiş No, Şube)...") term = "";

        if (string.IsNullOrWhiteSpace(term))
        {
            _filteredRows = new List<OdemeTalepDto>(_allRows);
        }
        else
        {
            string lower = term.ToLowerInvariant();
            _filteredRows = _allRows.Where(x =>
                x.FirAdi.ToLowerInvariant().Contains(lower) ||
                x.FisNo.ToString().Contains(lower) ||
                x.YetkiKodu.ToLowerInvariant().Contains(lower) ||
                x.Turu.ToLowerInvariant().Contains(lower) ||
                x.GrupSirket.ToLowerInvariant().Contains(lower) ||
                x.KasaAdi.ToLowerInvariant().Contains(lower) ||
                x.BelgeVeyaFaturaNo.ToLowerInvariant().Contains(lower) ||
                x.Aciklama.ToLowerInvariant().Contains(lower)
            ).ToList();
        }

        // Reassign 1..N Sıra numbers for clean view
        for (int i = 0; i < _filteredRows.Count; i++)
        {
            _filteredRows[i].Sira = i + 1;
        }

        _gridMain.DataSource = null;
        _gridMain.DataSource = _filteredRows;

        UpdateFooterTotals();
    }

    private void UpdateFooterTotals()
    {
        var summary = _service.ComputeSummary(_filteredRows);

        _lblToplamTalep.Text = $"Talep Tutarı Toplamı: {summary.ToplamTalepTutari:N2} TL";
        _lblToplamBagkur.Text = $"Bağkur Kesintisi: {summary.ToplamBagkurTutari:N2} TL";
        _lblToplamNet.Text = $"Net Ödeme Toplamı: {summary.ToplamNetTutar:N2} TL";

        UpdateSelectionInfo();

        if (pnlFooterParent() is Panel pnl) RepositionFooterBadges(pnl);
    }

    private void UpdateSelectionInfo()
    {
        int total = _filteredRows.Count;
        int current = _gridMain.CurrentRow != null ? _gridMain.CurrentRow.Index + 1 : (total > 0 ? 1 : 0);
        _lblKayitSayisi.Text = $"{current} / {total}";

        if (_gridMain.CurrentRow != null && _gridMain.CurrentRow.Index < _filteredRows.Count)
        {
            var sel = _filteredRows[_gridMain.CurrentRow.Index];
            _lblSeciliBilgi.Text = $"Seçili: [Fiş: {sel.FisNo}] {sel.FirAdi} | {sel.TalepTutar:N2} TL ({sel.Turu})";
        }
        else
        {
            _lblSeciliBilgi.Text = total > 0 ? "Kayıt seçildi" : "Kayıt bulunamadı";
        }
    }

    private string? GetSelectedSirKod()
    {
        if (_cmbSirketFiltre.SelectedIndex == 1) return "1"; // TEK ASYA
        if (_cmbSirketFiltre.SelectedIndex == 2) return "2"; // LAREX
        if (_cmbSirketFiltre.SelectedIndex == 3) return "3"; // AL SELEVIA
        return null; // Tümü
    }

    private Panel? pnlFooterParent()
    {
        foreach (Control c in this.Controls)
        {
            if (c is Panel p && p.Dock == DockStyle.Bottom) return p;
        }
        return null;
    }

    #endregion

    #region Action Handlers

    private void OpenDetayDialog(bool isNew, bool isReadOnly = false)
    {
        OdemeTalepDto target;

        if (isNew)
        {
            target = new OdemeTalepDto
            {
                FisTar = DateTime.Today,
                IslemTarihi = DateTime.Now,
                SirKod = GetSelectedSirKod() ?? "1",
                GrupSirket = "TEK ASYA",
                GonderenHesap = "TEK ASYA",
                Turu = "AVANS",
                YetkiKodu = "ALAŞEHİR",
                BagkurVarmi = "HAYIR",
                Durum = "Devam Ediyor"
            };
        }
        else
        {
            if (_gridMain.CurrentRow == null || _gridMain.CurrentRow.Index >= _filteredRows.Count)
            {
                MessageBox.Show("Lütfen işlem yapmak istediğiniz ödeme talebini seçiniz.", "Kayıt Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            target = _filteredRows[_gridMain.CurrentRow.Index];
        }

        using var dlg = new OdemeTalepDetayDialog(target, isReadOnly: isReadOnly, isNew: isNew);
        dlg.ShowDialog(this);

        if (dlg.IsSaved)
        {
            _ = LoadDataAsync();
        }
    }

    private void ExportToExcel()
    {
        if (_filteredRows.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak kayıt bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV Dosyası (*.csv)|*.csv|Metin Dosyası (*.txt)|*.txt",
            FileName = $"OdemeTalepleri_{_activeTab.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Sıra;İş Yeri / Şube;Grup Şirket;Gönderen Hesap;Türü;Fiş No;Tarih;İşlem Tarihi;Kasa Adı;Cari Hesap Ünvanı;Belge No;Bağkur ?;Talep Tutarı;Bağkur Oranı %;Bağkur Tutarı;Net Tutar;Durum;Açıklama");

                foreach (var r in _filteredRows)
                {
                    sb.AppendLine($"{r.Sira};\"{r.YetkiKodu}\";\"{r.GrupSirket}\";\"{r.GonderenHesap}\";\"{r.Turu}\";{r.FisNo};{r.FisTar:dd.MM.yyyy};{r.IslemTarihi:dd.MM.yyyy HH:mm:ss};\"{r.KasaAdi}\";\"{r.FirAdi.Replace("\"", "\"\"")}\";\"{r.BelgeVeyaFaturaNo}\";{r.BagkurVarmi};{r.TalepTutar:0.00};{r.BagkurOrani:0.00};{r.BagkurTutar:0.00};{r.NetTutar:0.00};\"{r.Durum}\";\"{r.Aciklama.Replace("\"", "\"\"")}\"");
                }

                // Write with UTF-8 BOM so Excel opens Turkish characters flawlessly
                File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                MessageBox.Show("Veriler başarıyla Excel CSV dosyası olarak aktarıldı!", "Aktarım Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Aktarım hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void PrintCurrentView()
    {
        if (_filteredRows.Count == 0)
        {
            MessageBox.Show("Yazdırılacak veri bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"Ödeme Talep Listesi - {_activeTab}";
        
        int printRowIndex = 0;
        printDoc.PrintPage += (s, ev) =>
        {
            float y = ev.MarginBounds.Top;
            var fontTitle = new Font("Segoe UI", 13f, FontStyle.Bold);
            var fontHeader = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            var fontRow = new Font("Segoe UI", 8f);

            // Title
            ev.Graphics?.DrawString($"ÖDEME TALEP LİSTESİ - {_activeTab.ToUpperInvariant()}", fontTitle, Brushes.Black, ev.MarginBounds.Left, y);
            y += 25;
            ev.Graphics?.DrawString($"Tarih Aralığı: {_dtpBaslangic.Value:dd.MM.yyyy} - {_dtpBitis.Value:dd.MM.yyyy} | Basım Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}", fontRow, Brushes.Gray, ev.MarginBounds.Left, y);
            y += 25;

            // Table Header Line
            ev.Graphics?.DrawLine(Pens.Black, ev.MarginBounds.Left, y, ev.MarginBounds.Right, y);
            y += 5;
            ev.Graphics?.DrawString("Fiş No", fontHeader, Brushes.Black, ev.MarginBounds.Left, y);
            ev.Graphics?.DrawString("Şube", fontHeader, Brushes.Black, ev.MarginBounds.Left + 50, y);
            ev.Graphics?.DrawString("Türü", fontHeader, Brushes.Black, ev.MarginBounds.Left + 120, y);
            ev.Graphics?.DrawString("Cari Ünvan", fontHeader, Brushes.Black, ev.MarginBounds.Left + 190, y);
            ev.Graphics?.DrawString("Talep Tutarı", fontHeader, Brushes.Black, ev.MarginBounds.Right - 100, y);
            y += 18;
            ev.Graphics?.DrawLine(Pens.Gray, ev.MarginBounds.Left, y, ev.MarginBounds.Right, y);
            y += 6;

            // Rows
            while (printRowIndex < _filteredRows.Count)
            {
                var r = _filteredRows[printRowIndex];
                ev.Graphics?.DrawString(r.FisNo.ToString(), fontRow, Brushes.Black, ev.MarginBounds.Left, y);
                ev.Graphics?.DrawString(r.YetkiKodu, fontRow, Brushes.Black, ev.MarginBounds.Left + 50, y);
                ev.Graphics?.DrawString(r.Turu, fontRow, Brushes.Black, ev.MarginBounds.Left + 120, y);
                
                string unvan = r.FirAdi.Length > 28 ? r.FirAdi.Substring(0, 28) + ".." : r.FirAdi;
                ev.Graphics?.DrawString(unvan, fontRow, Brushes.Black, ev.MarginBounds.Left + 190, y);

                string tutarStr = $"{r.TalepTutar:N2} TL";
                ev.Graphics?.DrawString(tutarStr, fontRow, Brushes.Black, ev.MarginBounds.Right - 100, y);

                y += 18;
                printRowIndex++;

                if (y > ev.MarginBounds.Bottom - 30)
                {
                    ev.HasMorePages = true;
                    return;
                }
            }

            ev.HasMorePages = false;
            printRowIndex = 0; // reset for next time
        };

        using var preview = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 900,
            Height = 700,
            StartPosition = FormStartPosition.CenterParent
        };
        preview.ShowDialog(this);
    }

    #endregion
}
