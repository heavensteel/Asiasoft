using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

public class KasaBankaYonetimForm : Form
{
    private readonly KasaBankaService _service = new();

    // KPI Labels
    private Label _lblKasaToplam = null!;
    private Label _lblBankaToplam = null!;
    private Label _lblDonemTahsilat = null!;
    private Label _lblDonemOdeme = null!;

    // TabControl
    private TabControl _tabControl = null!;

    // TAB 1: Genel Hareketler
    private DataGridView _gridGenel = null!;
    private TextBox _txtSearchGenel = null!;
    private ComboBox _cboIslemFiltre = null!;
    private DateTimePicker _dtpStartGenel = null!;
    private DateTimePicker _dtpEndGenel = null!;
    private Label _lblGenelStatus = null!;
    private List<FinansHareketDto> _allGenelData = new();

    // TAB 2: Kasa & Ekstre
    private ListBox _lstKasalar = null!;
    private DataGridView _gridKasaEkstre = null!;
    private Label _lblSeciliKasaAdi = null!;
    private Label _lblSeciliKasaBakiye = null!;
    private DateTimePicker _dtpStartKasa = null!;
    private DateTimePicker _dtpEndKasa = null!;
    private List<KasaKartDto> _kasalar = new();

    // TAB 3: Banka & Ekstre
    private ListBox _lstBankalar = null!;
    private DataGridView _gridBankaEkstre = null!;
    private Label _lblSeciliBankaAdi = null!;
    private Label _lblSeciliBankaIban = null!;
    private Label _lblSeciliBankaBakiye = null!;
    private DateTimePicker _dtpStartBanka = null!;
    private DateTimePicker _dtpEndBanka = null!;
    private List<BankaHesapDto> _bankalar = new();

    public KasaBankaYonetimForm(int initialTabIndex = 0)
    {
        this.DoubleBuffered = true;
        InitializeComponents();
        if (initialTabIndex >= 0 && initialTabIndex < _tabControl.TabPages.Count)
        {
            _tabControl.SelectedIndex = initialTabIndex;
        }

        this.Load += async (_, _) => await RefreshAllDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "Kasa & Banka Yönetimi";
        this.Size = new Size(1380, 820);
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = ModernColors.Slate100
        };

        var pnlHeader = BuildHeaderPanel();

        // TAB CONTROL
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ItemSize = new Size(180, 32),
            SizeMode = TabSizeMode.Fixed
        };

        var tabGenel = new TabPage { Text = "📊  Finans Hareketleri", BackColor = Color.White };
        var tabKasa = new TabPage { Text = "💰  Kasa Hesapları & Ekstre", BackColor = Color.White };
        var tabBanka = new TabPage { Text = "🏦  Banka Hesapları & Ekstre", BackColor = Color.White };

        BuildTabGenel(tabGenel);
        BuildTabKasa(tabKasa);
        BuildTabBanka(tabBanka);

        _tabControl.TabPages.AddRange(new TabPage[] { tabGenel, tabKasa, tabBanka });

        pnlMain.Controls.Add(_tabControl);
        pnlMain.Controls.Add(pnlHeader);

        this.Controls.Add(pnlMain);
    }

    private Panel BuildHeaderPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 10)
        };

        var pnlTitle = new Panel
        {
            Dock = DockStyle.Left,
            Width = 490,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "💰  Kasa & Banka Yönetimi (Canlı)",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Nakit kasa hesapları, banka mevduat hareketleri, tahsilat & tediye fiş takibi",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(2, 38),
            AutoSize = true
        };
        pnlTitle.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // KPI Badges on the Right
        var pnlKpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 840,
            Height = 78,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
        };

        var cardOdeme = CreateKpiCard("DÖNEM ÖDEMELERİ", "0,00 TL", Color.FromArgb(185, 28, 28));
        _lblDonemOdeme = (Label)cardOdeme.Controls["lblVal"]!;

        var cardTahsilat = CreateKpiCard("DÖNEM TAHSİLATLARI", "0,00 TL", Color.FromArgb(16, 120, 70));
        _lblDonemTahsilat = (Label)cardTahsilat.Controls["lblVal"]!;

        var cardBanka = CreateKpiCard("TOPLAM BANKA MEVDUATI", "0,00 TL", ModernColors.Blue600);
        _lblBankaToplam = (Label)cardBanka.Controls["lblVal"]!;

        var cardKasa = CreateKpiCard("TOPLAM KASA NAKDİ", "0,00 TL", ModernColors.Emerald500);
        _lblKasaToplam = (Label)cardKasa.Controls["lblVal"]!;

        pnlKpis.Controls.AddRange(new Control[] { cardOdeme, cardTahsilat, cardBanka, cardKasa });

        pnl.Controls.AddRange(new Control[] { pnlTitle, pnlKpis });
        return pnl;
    }

    private Panel CreateKpiCard(string label, string val, Color accent)
    {
        var pnl = new Panel
        {
            Size = new Size(195, 68),
            Margin = new Padding(6, 4, 0, 4),
            BackColor = Color.White,
            Padding = new Padding(12, 8, 12, 8)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var bAcc = new SolidBrush(accent);
            e.Graphics.FillRectangle(bAcc, 0, 0, 4, pnl.Height);
        };

        var lblT = new Label
        {
            Text = label,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(10, 8),
            AutoSize = true
        };

        var lblV = new Label
        {
            Name = "lblVal",
            Text = val,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(8, 28),
            AutoSize = true
        };

        pnl.Controls.AddRange(new Control[] { lblT, lblV });
        return pnl;
    }

    // ==========================================
    // TAB 1: GENEL FİNANS HAREKETLERİ
    // ==========================================
    private void BuildTabGenel(TabPage tab)
    {
        // 1. TOOLBAR
        var pnlTools = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 8, 12, 8)
        };
        pnlTools.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlTools.Height - 1, pnlTools.Width, pnlTools.Height - 1);
        };

        // Left Filters
        var lblSearch = new Label { Text = "🔍", Location = new Point(10, 16), AutoSize = true };
        _txtSearchGenel = new TextBox { Location = new Point(32, 12), Size = new Size(180, 28), PlaceholderText = "Fiş, Cari, Açıklama ara..." };
        _txtSearchGenel.TextChanged += (_, _) => FilterGenelData();

        _dtpStartGenel = new DateTimePicker { Location = new Point(220, 12), Size = new Size(100, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
        _dtpEndGenel = new DateTimePicker { Location = new Point(325, 12), Size = new Size(100, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

        var btnGetir = new Button
        {
            Text = "Getir",
            Location = new Point(430, 11),
            Size = new Size(54, 30),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Blue600,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnGetir.FlatAppearance.BorderSize = 0;
        btnGetir.Click += async (_, _) => await RefreshGenelDataAsync();

        _cboIslemFiltre = new ComboBox { Location = new Point(490, 12), Size = new Size(150, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboIslemFiltre.Items.AddRange(new object[] { "Tümü", "Kasa Tahsilat", "Kasa Tediye", "Gelen Havale", "Gönderilen Havale" });
        _cboIslemFiltre.SelectedIndex = 0;
        _cboIslemFiltre.SelectedIndexChanged += (_, _) => FilterGenelData();

        pnlTools.Controls.AddRange(new Control[] { lblSearch, _txtSearchGenel, _dtpStartGenel, _dtpEndGenel, btnGetir, _cboIslemFiltre });

        // Right Action Buttons
        var pnlActions = new Panel { Dock = DockStyle.Right, Width = 660, BackColor = Color.Transparent };
        int ax = pnlActions.Width;

        ax -= 135;
        var btnKasaTahsilat = CreateToolButton("➕ Kasa Tahsilat", Color.FromArgb(16, 120, 70), ax, async () => await OpenNewFisAsync(FinansIslemTipi.KasaTahsilat));
        ax -= 125;
        var btnKasaTediye = CreateToolButton("➖ Kasa Tediye", Color.FromArgb(185, 28, 28), ax, async () => await OpenNewFisAsync(FinansIslemTipi.KasaTediye));
        ax -= 135;
        var btnBankaGelen = CreateToolButton("📥 Gelen Havale", ModernColors.Blue600, ax, async () => await OpenNewFisAsync(FinansIslemTipi.BankaTahsilat));
        ax -= 145;
        var btnBankaGiden = CreateToolButton("📤 Giden Havale", ModernColors.Indigo600, ax, async () => await OpenNewFisAsync(FinansIslemTipi.BankaTediye));
        ax -= 110;
        var btnPrint = CreateToolButton("🖨️ Makbuz", ModernColors.Slate700, ax, async () => await PrintSelectedGenelAsync());

        pnlActions.Controls.AddRange(new Control[] { btnKasaTahsilat, btnKasaTediye, btnBankaGelen, btnBankaGiden, btnPrint });
        pnlTools.Controls.Add(pnlActions);

        // 2. GRID
        _gridGenel = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9.5f)
        };
        ModernUiHelper.ApplyModernGridStyle(_gridGenel);

        _gridGenel.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "Fiş No", DataPropertyName = "FisNo", Width = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "TarihFormatted", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "İşlem Türü", DataPropertyName = "IslemTipi", Width = 135, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Kasa / Banka Hesabı", DataPropertyName = "HesapAdi", Width = 170 },
            new DataGridViewTextBoxColumn { HeaderText = "Cari Hesap / Muhatap", DataPropertyName = "CariAdi", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { HeaderText = "Tahsilat / Giriş", DataPropertyName = "Borc", Width = 125, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 120, 70) } },
            new DataGridViewTextBoxColumn { HeaderText = "Tediye / Çıkış", DataPropertyName = "Alacak", Width = 125, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(185, 28, 28) } },
            new DataGridViewTextBoxColumn { HeaderText = "Açıklama / Not", DataPropertyName = "Aciklama", Width = 230 },
            new DataGridViewTextBoxColumn { HeaderText = "Kullanıcı", DataPropertyName = "User", Width = 100 }
        );

        _gridGenel.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _gridGenel.Rows.Count) return;
            if (_gridGenel.Columns[e.ColumnIndex].DataPropertyName == "IslemTipi")
            {
                string tip = e.Value?.ToString() ?? "";
                if (tip.Contains("Tahsilat") || tip.Contains("Gelen"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(16, 120, 70);
                    e.CellStyle.BackColor = Color.FromArgb(235, 248, 240);
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.BackColor = Color.FromArgb(254, 242, 242);
                }
            }
        };

        // Bottom Status
        var pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = ModernColors.Slate100, Padding = new Padding(12, 0, 12, 0) };
        _lblGenelStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 116, 139), Text = "Hareketler yükleniyor..." };
        pnlStatus.Controls.Add(_lblGenelStatus);

        tab.Controls.Add(_gridGenel);
        tab.Controls.Add(pnlStatus);
        tab.Controls.Add(pnlTools);
    }

    private Button CreateToolButton(string text, Color bg, int x, Func<Task> action)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = bg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(text.Length * 7 + 35, 32),
            Location = new Point(x, 9),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += async (_, _) => await action();
        return btn;
    }

    // ==========================================
    // TAB 2: KASA HESAPLARI & EKSTRE
    // ==========================================
    private void BuildTabKasa(TabPage tab)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 380,
            FixedPanel = FixedPanel.Panel1,
            BackColor = ModernColors.Slate200
        };

        // LEFT: Kasa Kartları Listesi
        var pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        var pnlLeftHdr = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(10, 8, 10, 8) };
        pnlLeftHdr.Controls.Add(new Label { Text = "💰  KASA KARTLARI (TANIMLAR)", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ModernColors.Slate800, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });

        _lstKasalar = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ItemHeight = 28
        };
        _lstKasalar.SelectedIndexChanged += async (_, _) => await OnSelectedKasaChangedAsync();

        pnlLeft.Controls.Add(_lstKasalar);
        pnlLeft.Controls.Add(pnlLeftHdr);
        split.Panel1.Controls.Add(pnlLeft);

        // RIGHT: Seçili Kasa Ekstresi
        var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        
        var pnlRightHdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(14, 8, 14, 8) };
        _lblSeciliKasaAdi = new Label { Text = "MERKEZ KASA", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = ModernColors.Slate900, Location = new Point(14, 8), AutoSize = true };
        _lblSeciliKasaBakiye = new Label { Text = "Bakiye: 0,00 TL", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 120, 70), Location = new Point(14, 32), AutoSize = true };

        var pnlKasaDate = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 360, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.Transparent };
        var btnGetirKasa = new Button { Text = "Ekstre Getir", Size = new Size(95, 30), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(16, 120, 70), FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 12, 0, 0), Cursor = Cursors.Hand };
        btnGetirKasa.FlatAppearance.BorderSize = 0;
        btnGetirKasa.Click += async (_, _) => await LoadSelectedKasaEkstreAsync();

        _dtpEndKasa = new DateTimePicker { Size = new Size(100, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(4, 14, 0, 0) };
        _dtpStartKasa = new DateTimePicker { Size = new Size(100, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-60), Margin = new Padding(4, 14, 0, 0) };

        pnlKasaDate.Controls.AddRange(new Control[] { btnGetirKasa, _dtpEndKasa, _dtpStartKasa });
        pnlRightHdr.Controls.AddRange(new Control[] { _lblSeciliKasaAdi, _lblSeciliKasaBakiye, pnlKasaDate });

        _gridKasaEkstre = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9.5f)
        };
        ModernUiHelper.ApplyModernGridStyle(_gridKasaEkstre);

        _gridKasaEkstre.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = "SiraNo", Width = 40, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "TarihFormatted", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Fiş No", DataPropertyName = "FisNo", Width = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "İşlem Türü", DataPropertyName = "IslemTipi", Width = 110 },
            new DataGridViewTextBoxColumn { HeaderText = "Cari Hesap / Muhatap", DataPropertyName = "CariAdi", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { HeaderText = "Tahsilat (Giriş)", DataPropertyName = "Borc", Width = 120, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(16, 120, 70) } },
            new DataGridViewTextBoxColumn { HeaderText = "Tediye (Çıkış)", DataPropertyName = "Alacak", Width = 120, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(185, 28, 28) } },
            new DataGridViewTextBoxColumn { HeaderText = "Bakiye", DataPropertyName = "Bakiye", Width = 130, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Açıklama", DataPropertyName = "Aciklama", Width = 200 }
        );

        pnlRight.Controls.Add(_gridKasaEkstre);
        pnlRight.Controls.Add(pnlRightHdr);
        split.Panel2.Controls.Add(pnlRight);

        tab.Controls.Add(split);
    }

    // ==========================================
    // TAB 3: BANKA HESAPLARI & EKSTRE
    // ==========================================
    private void BuildTabBanka(TabPage tab)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 380,
            FixedPanel = FixedPanel.Panel1,
            BackColor = ModernColors.Slate200
        };

        // LEFT: Banka Hesap Kartları
        var pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        var pnlLeftHdr = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(10, 8, 10, 8) };
        pnlLeftHdr.Controls.Add(new Label { Text = "🏦  BANKA HESAPLARI (TANIMLAR)", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ModernColors.Slate800, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });

        _lstBankalar = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ItemHeight = 28
        };
        _lstBankalar.SelectedIndexChanged += async (_, _) => await OnSelectedBankaChangedAsync();

        pnlLeft.Controls.Add(_lstBankalar);
        pnlLeft.Controls.Add(pnlLeftHdr);
        split.Panel1.Controls.Add(pnlLeft);

        // RIGHT: Seçili Banka Ekstresi
        var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        
        var pnlRightHdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(14, 6, 14, 6) };
        _lblSeciliBankaAdi = new Label { Text = "QNB FİNANSBANK", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = ModernColors.Slate900, Location = new Point(14, 6), AutoSize = true };
        _lblSeciliBankaIban = new Label { Text = "IBAN: TR...", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(14, 26), AutoSize = true };
        _lblSeciliBankaBakiye = new Label { Text = "Mevduat Bakiyesi: 0,00 TL", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = ModernColors.Blue600, Location = new Point(14, 42), AutoSize = true };

        var pnlBankaDate = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 360, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.Transparent };
        var btnGetirBanka = new Button { Text = "Ekstre Getir", Size = new Size(95, 30), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, BackColor = ModernColors.Blue600, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 16, 0, 0), Cursor = Cursors.Hand };
        btnGetirBanka.FlatAppearance.BorderSize = 0;
        btnGetirBanka.Click += async (_, _) => await LoadSelectedBankaEkstreAsync();

        _dtpEndBanka = new DateTimePicker { Size = new Size(100, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(4, 18, 0, 0) };
        _dtpStartBanka = new DateTimePicker { Size = new Size(100, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-60), Margin = new Padding(4, 18, 0, 0) };

        pnlBankaDate.Controls.AddRange(new Control[] { btnGetirBanka, _dtpEndBanka, _dtpStartBanka });
        pnlRightHdr.Controls.AddRange(new Control[] { _lblSeciliBankaAdi, _lblSeciliBankaIban, _lblSeciliBankaBakiye, pnlBankaDate });

        _gridBankaEkstre = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9.5f)
        };
        ModernUiHelper.ApplyModernGridStyle(_gridBankaEkstre);

        _gridBankaEkstre.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = "SiraNo", Width = 40, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "TarihFormatted", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Fiş No", DataPropertyName = "FisNo", Width = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "İşlem Türü", DataPropertyName = "IslemTipi", Width = 130 },
            new DataGridViewTextBoxColumn { HeaderText = "Cari Hesap / Muhatap", DataPropertyName = "CariAdi", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { HeaderText = "Yatan (Borç)", DataPropertyName = "Borc", Width = 120, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(16, 120, 70) } },
            new DataGridViewTextBoxColumn { HeaderText = "Çekilen (Alacak)", DataPropertyName = "Alacak", Width = 120, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(185, 28, 28) } },
            new DataGridViewTextBoxColumn { HeaderText = "Bakiye", DataPropertyName = "Bakiye", Width = 130, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Açıklama / Dekont", DataPropertyName = "Aciklama", Width = 220 }
        );

        pnlRight.Controls.Add(_gridBankaEkstre);
        pnlRight.Controls.Add(pnlRightHdr);
        split.Panel2.Controls.Add(pnlRight);

        tab.Controls.Add(split);
    }

    // ==========================================
    // DATA LOADING & LOGIC
    // ==========================================
    private async Task RefreshAllDataAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;

            // Load Kasa & Banka Master Lists
            _kasalar = await _service.GetKasaKartlariAsync();
            _bankalar = await _service.GetBankaHesaplariAsync();

            // Populate Kasa ListBox
            _lstKasalar.Items.Clear();
            foreach (var k in _kasalar)
            {
                _lstKasalar.Items.Add($"#{k.Kodu} {k.Adi} — {k.Bakiye:N2} {k.DovizTuru}");
            }
            if (_lstKasalar.Items.Count > 0) _lstKasalar.SelectedIndex = 0;

            // Populate Banka ListBox
            _lstBankalar.Items.Clear();
            foreach (var b in _bankalar)
            {
                _lstBankalar.Items.Add($"#{b.Kodu} {b.Adi} — {b.Bakiye:N2} {b.DovizCinsi}");
            }
            if (_lstBankalar.Items.Count > 0) _lstBankalar.SelectedIndex = 0;

            // Update Master KPIs
            double totalKasa = _kasalar.Where(k => k.DovizTuru == "TL").Sum(k => k.Bakiye);
            _lblKasaToplam.Text = $"{totalKasa:N2} TL";

            double totalBanka = _bankalar.Where(b => b.DovizCinsi == "TL").Sum(b => b.Bakiye);
            _lblBankaToplam.Text = $"{totalBanka:N2} TL";

            // Load Tab 1 Genel Data
            await RefreshGenelDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Finans verileri yüklenirken hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private async Task RefreshGenelDataAsync()
    {
        try
        {
            _lblGenelStatus.Text = "Son hareketler veritabanından çekiliyor...";
            _allGenelData = await _service.GetSonFinansHareketleriAsync(
                _dtpStartGenel.Value,
                _dtpEndGenel.Value,
                _cboIslemFiltre.SelectedItem?.ToString() ?? "Tümü",
                _txtSearchGenel.Text
            );

            FilterGenelData();
        }
        catch (Exception ex)
        {
            _lblGenelStatus.Text = $"Hata: {ex.Message}";
        }
    }

    private void FilterGenelData()
    {
        var filtered = _allGenelData.AsEnumerable();

        string search = _txtSearchGenel.Text.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(x =>
                x.FisNo.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.CariAdi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.HesapAdi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Aciklama.Contains(search, StringComparison.OrdinalIgnoreCase)
            );
        }

        string tipFilter = _cboIslemFiltre.SelectedItem?.ToString() ?? "Tümü";
        if (tipFilter != "Tümü")
        {
            filtered = filtered.Where(x => x.IslemTipi.Contains(tipFilter, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        _gridGenel.DataSource = list;

        double donemTahsilat = list.Sum(x => x.Borc);
        double donemOdeme = list.Sum(x => x.Alacak);

        _lblDonemTahsilat.Text = $"{donemTahsilat:N2} TL";
        _lblDonemOdeme.Text = $"{donemOdeme:N2} TL";

        _lblGenelStatus.Text = $"Toplam {list.Count:N0} adet finans hareketi listelendi. (Tahsilat: {donemTahsilat:N2} TL, Tediye: {donemOdeme:N2} TL)";
    }

    private async Task OnSelectedKasaChangedAsync()
    {
        if (_lstKasalar.SelectedIndex < 0 || _lstKasalar.SelectedIndex >= _kasalar.Count) return;
        var k = _kasalar[_lstKasalar.SelectedIndex];
        _lblSeciliKasaAdi.Text = $"#{k.Kodu} {k.Adi}";
        _lblSeciliKasaBakiye.Text = $"Güncel Bakiye: {k.Bakiye:N2} {k.DovizTuru}";
        await LoadSelectedKasaEkstreAsync();
    }

    private async Task LoadSelectedKasaEkstreAsync()
    {
        if (_lstKasalar.SelectedIndex < 0 || _lstKasalar.SelectedIndex >= _kasalar.Count) return;
        var k = _kasalar[_lstKasalar.SelectedIndex];
        var data = await _service.GetKasaEkstreAsync(k.Kodu, _dtpStartKasa.Value, _dtpEndKasa.Value);
        _gridKasaEkstre.DataSource = data;
    }

    private async Task OnSelectedBankaChangedAsync()
    {
        if (_lstBankalar.SelectedIndex < 0 || _lstBankalar.SelectedIndex >= _bankalar.Count) return;
        var b = _bankalar[_lstBankalar.SelectedIndex];
        _lblSeciliBankaAdi.Text = $"#{b.Kodu} {b.Adi} ({b.SubeAdi})";
        _lblSeciliBankaIban.Text = $"IBAN: {b.IbanFormatted}  •  Hesap No: {b.HesapNo}";
        _lblSeciliBankaBakiye.Text = $"Mevduat Bakiyesi: {b.Bakiye:N2} {b.DovizCinsi}";
        await LoadSelectedBankaEkstreAsync();
    }

    private async Task LoadSelectedBankaEkstreAsync()
    {
        if (_lstBankalar.SelectedIndex < 0 || _lstBankalar.SelectedIndex >= _bankalar.Count) return;
        var b = _bankalar[_lstBankalar.SelectedIndex];
        var data = await _service.GetBankaEkstreAsync(b.Kodu, _dtpStartBanka.Value, _dtpEndBanka.Value);
        _gridBankaEkstre.DataSource = data;
    }

    private async Task OpenNewFisAsync(FinansIslemTipi tip)
    {
        using var dlg = new KasaBankaFisDialog(null, tip);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await RefreshAllDataAsync();
        }
    }

    private async Task PrintSelectedGenelAsync()
    {
        if (_gridGenel.CurrentRow == null || _gridGenel.CurrentRow.Index < 0)
        {
            MessageBox.Show("Lütfen makbuzunu yazdırmak istediğiniz finans hareketini seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _gridGenel.CurrentRow.DataBoundItem as FinansHareketDto;
        if (item == null) return;

        FinansIslemTipi tip = item.IslemTipi switch
        {
            "Kasa Tahsilat" => FinansIslemTipi.KasaTahsilat,
            "Kasa Tediye" => FinansIslemTipi.KasaTediye,
            "Gelen Havale" or "Banka Tahsilat" => FinansIslemTipi.BankaTahsilat,
            _ => FinansIslemTipi.BankaTediye
        };

        var fisDto = new FinansFisGirisDto
        {
            FisNo = item.FisNo,
            MakbuzNo = item.MakbuzNo > 0 ? item.MakbuzNo : item.FisNo,
            FisTar = item.FisTar ?? DateTime.Today,
            Saat = item.Saat ?? DateTime.Now.TimeOfDay,
            IslemTipi = tip,
            KasaAdi = item.HesapAdi,
            BankaAdi = item.HesapAdi,
            FirKod = item.FirKod,
            CariAdi = item.CariAdi,
            Tutar = item.Borc > 0 ? item.Borc : item.Alacak,
            DovizTuru = item.DovizTuru,
            DovizTutar = item.DovizTutar,
            BelgeNo = item.BelgeNo,
            Aciklama = item.Aciklama,
            User = item.User
        };

        try
        {
            var doc = KasaBankaReportRenderer.CreatePrintDocument(fisDto);
            using var dlg = new PrintPreviewDialog
            {
                Document = doc,
                Width = 950,
                Height = 750,
                StartPosition = FormStartPosition.CenterParent
            };
            dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Baskı hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
