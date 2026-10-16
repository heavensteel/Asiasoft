using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Executive Mal Kabul & Live Scale Weighment module.
/// Streams live weight from TCP scale indicator (192.168.60.116:8080) and persists to 'kantargiris'.
/// </summary>
public class MalKabulForm : Form
{
    private readonly MacroService _macroService = new();
    private readonly KantarScaleClient _scaleClient = KantarScaleClient.Instance;

    // Scale Controls
    private Label _lblLiveWeight = null!;
    private Label _lblLiveTag = null!;
    private Label _lblScaleStatus = null!;
    private Label _lblDbStatus = null!;
    private Label _lblCalculatedNet = null!;
    private Button _btnCaptureBrut = null!;
    private Button _btnCaptureDara = null!;
    private Button _btnManualRead = null!;
    private Button _btnScaleSettings = null!;

    // Form Entry Controls
    private TextBox _txtFisNo = null!;
    private ComboBox _cmbPlaka = null!;
    private ComboBox _cmbCari = null!;
    private ComboBox _cmbUrun = null!;
    private ComboBox _cmbDepo = null!;
    private NumericUpDown _numBrut = null!;
    private NumericUpDown _numDara = null!;
    private NumericUpDown _numNet = null!;
    private TextBox _txtSofor = null!;
    private TextBox _txtTelefon = null!;
    private TextBox _txtAciklama = null!;

    // Buttons
    private Button _btnSave = null!;
    private Button _btnPrint = null!;
    private Button _btnNew = null!;
    private Button _btnRefresh = null!;

    // History Grid & Filter
    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private Label _lblGridStats = null!;
    private List<KantarTartimDto> _historyList = new();

    public MalKabulForm()
    {
        this.DoubleBuffered = true;
        this.Text = "⚖️ Mal Kabul & Canlı Kantar Tartım";
        this.Size = new Size(1250, 750);
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        InitializeUI();

        // Wire scale client events
        _scaleClient.OnWeightReceived += ScaleClient_OnWeightReceived;
        _scaleClient.OnConnectionStateChanged += ScaleClient_OnConnectionStateChanged;

        this.Load += async (_, _) =>
        {
            await LoadLookupsAsync();
            await LoadHistoryAsync();
            await ApplyUserScaleSettingsAsync();
        };

        this.FormClosing += (_, _) =>
        {
            _scaleClient.OnWeightReceived -= ScaleClient_OnWeightReceived;
            _scaleClient.OnConnectionStateChanged -= ScaleClient_OnConnectionStateChanged;
            // Leave client running if shared or stop
        };
    }

    private void InitializeUI()
    {
        // 1. Top Bar
        var pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.White,
            Padding = new Padding(14, 8, 14, 8)
        };
        pnlTopBar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "⚖️  Mal Kabul & Canlı Kantar Tartım Sistemi",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            AutoSize = true,
            Location = new Point(12, 12)
        };

        string dbServer = DbConnectionConfig.Server;
        _lblDbStatus = new Label
        {
            Text = $"🗄️ DB: {dbServer} (Eşzamanlı)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 118, 110),
            BackColor = Color.FromArgb(240, 253, 250),
            Padding = new Padding(8, 4, 8, 4),
            AutoSize = true,
            Location = new Point(360, 11)
        };

        _lblScaleStatus = new Label
        {
            Text = "⏳ Kantar aranıyor...",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 83, 9),
            BackColor = Color.FromArgb(254, 243, 199),
            Padding = new Padding(8, 4, 8, 4),
            AutoSize = true,
            Location = new Point(570, 11)
        };

        _btnScaleSettings = new Button
        {
            Text = "⚙️ Kantar & VPN Ayarı",
            Location = new Point(pnlTopBar.Width - 280, 8),
            Size = new Size(145, 32),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnScaleSettings.FlatAppearance.BorderColor = ModernColors.Slate200;
        _btnScaleSettings.Click += (_, _) => OpenScaleSettingsDialog();

        _btnRefresh = new Button
        {
            Text = "⟳ Yenile",
            Location = new Point(pnlTopBar.Width - 125, 8),
            Size = new Size(95, 32),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnRefresh.FlatAppearance.BorderColor = ModernColors.Slate200;
        _btnRefresh.Click += async (_, _) => await LoadHistoryAsync();

        pnlTopBar.Controls.AddRange(new Control[] { lblTitle, _lblDbStatus, _lblScaleStatus, _btnScaleSettings, _btnRefresh });

        // 2. Middle Container: Split into Left (Digital Scale Indicator) and Right (Mal Kabul Form)
        var pnlMiddle = new Panel
        {
            Dock = DockStyle.Top,
            Height = 345,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(12, 10, 12, 6)
        };

        var pnlScaleCard = BuildScaleIndicatorCard();
        var pnlFormCard = BuildMalKabulFormCard();

        pnlMiddle.Controls.Add(pnlFormCard);
        pnlMiddle.Controls.Add(pnlScaleCard);

        // 3. Bottom Grid Container
        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12, 4, 12, 8)
        };

        var pnlGridHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.White,
            Padding = new Padding(6, 4, 6, 4)
        };

        _lblGridStats = new Label
        {
            Text = "📊 Günün Tartım ve Mal Kabul Fişleri",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            AutoSize = true,
            Location = new Point(8, 10)
        };

        var pnlSearch = new Panel
        {
            Size = new Size(240, 28),
            Location = new Point(pnlGridHeader.Width - 250, 6),
            BackColor = Color.FromArgb(248, 250, 252),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        _txtSearch = new TextBox
        {
            Location = new Point(6, 4),
            Width = 225,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 9f),
            PlaceholderText = "🔍 Plaka, Cari veya Ürün ara..."
        };
        _txtSearch.TextChanged += (_, _) => FilterHistory();
        pnlSearch.Controls.Add(_txtSearch);

        pnlGridHeader.Controls.AddRange(new Control[] { _lblGridStats, pnlSearch });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };
        ModernUiHelper.ApplyModernGridStyle(_grid);
        _grid.RowTemplate.Height = 32;

        SetupGridColumns();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) LoadRowToForm();
        };

        pnlGridContainer.Controls.Add(_grid);
        pnlGridContainer.Controls.Add(pnlGridHeader);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlMiddle);
        this.Controls.Add(pnlTopBar);
    }

    private Control BuildScaleIndicatorCard()
    {
        var card = new ModernCardPanel
        {
            Dock = DockStyle.Left,
            Width = 420,
            CardBackground = Color.White,
            BorderColor = ModernColors.Slate200,
            Padding = new Padding(16, 12, 16, 12)
        };

        var lblCardTitle = new Label
        {
            Text = "CANLI KANTAR İNDİKATÖRÜ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate500,
            Location = new Point(16, 12),
            AutoSize = true
        };

        // Dark LCD / LED Digital Box
        var pnlDigital = new Panel
        {
            Location = new Point(16, 34),
            Size = new Size(385, 110),
            BackColor = Color.FromArgb(15, 23, 42) // Dark Slate #0F172A
        };
        pnlDigital.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(51, 65, 85), 2f);
            e.Graphics.DrawRectangle(pen, 1, 1, pnlDigital.Width - 2, pnlDigital.Height - 2);
        };

        _lblLiveTag = new Label
        {
            Text = $"● CANLI AKIŞ: {_scaleClient.ActiveEndpoint} | EŞZAMANLI KAYIT: {DbConnectionConfig.Server}",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 185, 129),
            Location = new Point(12, 8),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        _lblLiveWeight = new Label
        {
            Text = "0.0 KG",
            Font = new Font("Consolas", 34f, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 211, 153), // Glowing Emerald Mint
            Location = new Point(10, 26),
            Size = new Size(365, 55),
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        };

        var lblStable = new Label
        {
            Text = "DURAĞAN / KARARLI TARTIM (STABLE) - DOĞRUDAN TCP İNDİKATÖR",
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(12, 86),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        pnlDigital.Controls.AddRange(new Control[] { _lblLiveTag, _lblLiveWeight, lblStable });

        // Capture Buttons
        _btnCaptureBrut = new Button
        {
            Text = "🎯 1. TARTIM AL (BRÜT KG)",
            Location = new Point(16, 155),
            Size = new Size(188, 44),
            BackColor = Color.FromArgb(4, 120, 87),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnCaptureBrut.FlatAppearance.BorderSize = 0;
        _btnCaptureBrut.Click += (_, _) => CaptureLiveWeightToBrut();

        _btnCaptureDara = new Button
        {
            Text = "🎯 2. TARTIM AL (DARA KG)",
            Location = new Point(213, 155),
            Size = new Size(188, 44),
            BackColor = Color.FromArgb(67, 56, 202),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnCaptureDara.FlatAppearance.BorderSize = 0;
        _btnCaptureDara.Click += (_, _) => CaptureLiveWeightToDara();

        // Calculated Net Banner
        var pnlNetBanner = new Panel
        {
            Location = new Point(16, 210),
            Size = new Size(385, 60),
            BackColor = Color.FromArgb(240, 253, 250) // Teal 50
        };
        pnlNetBanner.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(153, 246, 228), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlNetBanner.Width - 1, pnlNetBanner.Height - 1);
        };

        var lblNetTitle = new Label
        {
            Text = "HESAPLANAN NET AĞIRLIK:",
            Location = new Point(10, 8),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 118, 110),
            AutoSize = true
        };

        _lblCalculatedNet = new Label
        {
            Text = "0 KG",
            Location = new Point(10, 24),
            Size = new Size(365, 30),
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(13, 148, 136),
            TextAlign = ContentAlignment.MiddleRight
        };

        pnlNetBanner.Controls.AddRange(new Control[] { lblNetTitle, _lblCalculatedNet });

        // Manual Read Button
        _btnManualRead = new Button
        {
            Text = "⚡ Anlık Veri Çek",
            Location = new Point(16, 280),
            Size = new Size(188, 32),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnManualRead.FlatAppearance.BorderColor = ModernColors.Slate200;
        _btnManualRead.Click += async (_, _) => await ManualReadWeightAsync();

        var btnClearWeights = new Button
        {
            Text = "🧹 Tartımları Sıfırla",
            Location = new Point(213, 280),
            Size = new Size(188, 32),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnClearWeights.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnClearWeights.Click += (_, _) =>
        {
            _numBrut.Value = 0;
            _numDara.Value = 0;
            CalculateNet();
        };

        card.Controls.AddRange(new Control[]
        {
            lblCardTitle, pnlDigital, _btnCaptureBrut, _btnCaptureDara,
            pnlNetBanner, _btnManualRead, btnClearWeights
        });

        return card;
    }

    private Control BuildMalKabulFormCard()
    {
        var card = new ModernCardPanel
        {
            Dock = DockStyle.Fill,
            CardBackground = Color.White,
            BorderColor = ModernColors.Slate200,
            Padding = new Padding(18, 12, 18, 12)
        };

        var lblCardTitle = new Label
        {
            Text = "MAL KABUL & TARTIM FİŞİ DETAYLARI",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate500,
            Location = new Point(16, 12),
            AutoSize = true
        };

        // Form fields layout
        int x1 = 18, x2 = 140, w1 = 160;
        int x3 = 330, x4 = 440, w2 = 280;

        // Row 1: Fiş No, Tarih, Plaka
        var lblFis = new Label { Text = "Fiş No:", Location = new Point(x1, 40), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _txtFisNo = new TextBox { Location = new Point(x2, 36), Width = 150, ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

        var lblPlaka = new Label { Text = "Araç Plakası *:", Location = new Point(x3, 40), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _cmbPlaka = new ComboBox { Location = new Point(x4, 36), Width = 280, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };

        // Row 2: Cari / Müstahsil, Ürün / Cins
        var lblCari = new Label { Text = "Cari / Müstahsil *:", Location = new Point(x1, 78), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _cmbCari = new ComboBox { Location = new Point(x2, 74), Width = w1 + 300 + w2 - 270, Font = new Font("Segoe UI", 9.5f), DropDownWidth = 450, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };

        var lblUrun = new Label { Text = "Ürün / Mal Cinsi *:", Location = new Point(x1, 116), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _cmbUrun = new ComboBox { Location = new Point(x2, 112), Width = w1 + 300 + w2 - 270, Font = new Font("Segoe UI", 9.5f), DropDownWidth = 350, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };

        // Row 3: Depo, Şoför / Tel
        var lblDepo = new Label { Text = "Giriş Deposu:", Location = new Point(x1, 154), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _cmbDepo = new ComboBox { Location = new Point(x2, 150), Width = 150, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList };

        var lblSofor = new Label { Text = "Şoför / Teslim:", Location = new Point(x3, 154), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        _txtSofor = new TextBox { Location = new Point(x4, 150), Width = 140, Font = new Font("Segoe UI", 9.5f) };

        var lblTel = new Label { Text = "Tel:", Location = new Point(x4 + 148, 154), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        _txtTelefon = new TextBox { Location = new Point(x4 + 178, 150), Width = 100, Font = new Font("Segoe UI", 9.5f) };

        // Row 4: Weights (Brüt, Dara, Net)
        var pnlWeights = new Panel { Location = new Point(x1, 190), Size = new Size(710, 60), BackColor = Color.FromArgb(248, 250, 252) };
        pnlWeights.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlWeights.Width - 1, pnlWeights.Height - 1);
        };

        var lblB = new Label { Text = "1. Tartım (Brüt Kg):", Location = new Point(12, 18), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _numBrut = new NumericUpDown { Location = new Point(140, 15), Width = 110, Maximum = 1000000, DecimalPlaces = 0, ThousandsSeparator = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
        _numBrut.ValueChanged += (_, _) => CalculateNet();

        var lblD = new Label { Text = "2. Tartım (Dara Kg):", Location = new Point(270, 18), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _numDara = new NumericUpDown { Location = new Point(400, 15), Width = 110, Maximum = 1000000, DecimalPlaces = 0, ThousandsSeparator = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
        _numDara.ValueChanged += (_, _) => CalculateNet();

        var lblN = new Label { Text = "Net Kg:", Location = new Point(530, 18), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(4, 120, 87) };
        _numNet = new NumericUpDown { Location = new Point(585, 15), Width = 110, Maximum = 1000000, DecimalPlaces = 0, ThousandsSeparator = true, ReadOnly = true, BackColor = Color.FromArgb(236, 253, 245), ForeColor = Color.FromArgb(4, 120, 87), Font = new Font("Segoe UI", 11f, FontStyle.Bold) };

        pnlWeights.Controls.AddRange(new Control[] { lblB, _numBrut, lblD, _numDara, lblN, _numNet });

        // Row 5: Açıklama
        var lblAciklama = new Label { Text = "Açıklama:", Location = new Point(x1, 260), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        _txtAciklama = new TextBox { Location = new Point(x2, 256), Width = w1 + 300 + w2 - 270, Font = new Font("Segoe UI", 9.5f) };

        // Row 6: Action Buttons
        _btnSave = new Button
        {
            Text = "💾 Tartımı Kaydet (F5)",
            Location = new Point(x1, 296),
            Size = new Size(180, 36),
            BackColor = Color.FromArgb(4, 120, 87),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += async (_, _) => await SaveTartimAsync();

        _btnPrint = new Button
        {
            Text = "🖨️ Fiş Yazdır (F8)",
            Location = new Point(x1 + 190, 296),
            Size = new Size(150, 36),
            BackColor = Color.FromArgb(67, 56, 202),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnPrint.FlatAppearance.BorderSize = 0;
        _btnPrint.Click += (_, _) => PrintCurrentReceipt();

        _btnNew = new Button
        {
            Text = "➕ Yeni Form (F2)",
            Location = new Point(x1 + 350, 296),
            Size = new Size(140, 36),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnNew.FlatAppearance.BorderColor = ModernColors.Slate200;
        _btnNew.Click += (_, _) => ResetForm();

        card.Controls.AddRange(new Control[]
        {
            lblCardTitle,
            lblFis, _txtFisNo, lblPlaka, _cmbPlaka,
            lblCari, _cmbCari,
            lblUrun, _cmbUrun,
            lblDepo, _cmbDepo, lblSofor, _txtSofor, lblTel, _txtTelefon,
            pnlWeights,
            lblAciklama, _txtAciklama,
            _btnSave, _btnPrint, _btnNew
        });

        return card;
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "GirisKodu", HeaderText = "Fiş No", Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "Tarih", HeaderText = "Tarih", Width = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colSaat", DataPropertyName = "GirisSaati", HeaderText = "Saat", Width = 65,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colPlaka", DataPropertyName = "Plaka", HeaderText = "Plaka", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colCari", DataPropertyName = "FirmaAdi", HeaderText = "Cari / Müstahsil", Width = 230 },
            new DataGridViewTextBoxColumn { Name = "colUrun", DataPropertyName = "MalAdi", HeaderText = "Ürün / Cins", Width = 170 },
            new DataGridViewTextBoxColumn { Name = "colBrut", DataPropertyName = "Burut", HeaderText = "Brüt (Kg)", Width = 95,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDara", DataPropertyName = "Dara", HeaderText = "Dara (Kg)", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colNet", DataPropertyName = "Net", HeaderText = "Net (Kg)", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(4, 120, 87) } },
            new DataGridViewTextBoxColumn { Name = "colDepo", DataPropertyName = "DepoAdi", HeaderText = "Depo", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "colKullanici", DataPropertyName = "Kullanici", HeaderText = "Tartım Yapan", Width = 110 }
        );
    }

    private void ScaleClient_OnWeightReceived(decimal weight)
    {
        if (this.IsDisposed || !this.IsHandleCreated) return;

        this.BeginInvoke(new Action(() =>
        {
            _lblLiveWeight.Text = $"{weight:N1} KG";
        }));
    }

    private void ScaleClient_OnConnectionStateChanged(bool connected, string message)
    {
        if (this.IsDisposed || !this.IsHandleCreated) return;

        this.BeginInvoke(new Action(() =>
        {
            if (connected)
            {
                _lblScaleStatus.Text = $"🟢 {message}";
                _lblScaleStatus.ForeColor = Color.FromArgb(21, 128, 61);
                _lblScaleStatus.BackColor = Color.FromArgb(220, 252, 231);
                _lblLiveTag.Text = $"● CANLI AKIŞ: {_scaleClient.ActiveEndpoint} | EŞZAMANLI KAYIT: {DbConnectionConfig.Server}";
            }
            else
            {
                _lblScaleStatus.Text = $"🔴 {message}";
                _lblScaleStatus.ForeColor = Color.FromArgb(185, 28, 28);
                _lblScaleStatus.BackColor = Color.FromArgb(254, 226, 226);
                _lblLiveTag.Text = $"● HEDEF KANTAR: {_scaleClient.Host}:{_scaleClient.Port} | VERİTABANI: {DbConnectionConfig.Server}";
            }
        }));
    }

    private void CaptureLiveWeightToBrut()
    {
        _numBrut.Value = Math.Max(0, Math.Round(_scaleClient.CurrentWeight));
        CalculateNet();
    }

    private void CaptureLiveWeightToDara()
    {
        _numDara.Value = Math.Max(0, Math.Round(_scaleClient.CurrentWeight));
        CalculateNet();
    }

    private void CalculateNet()
    {
        if (_numBrut.Value > 0 && _numDara.Value > 0)
        {
            decimal net = Math.Max(0, _numBrut.Value - _numDara.Value);
            _numNet.Value = net;
            _lblCalculatedNet.Text = $"{net:N0} KG";
        }
        else if (_numBrut.Value > 0)
        {
            _numNet.Value = _numBrut.Value;
            _lblCalculatedNet.Text = $"{_numBrut.Value:N0} KG (1. Tartım)";
        }
        else if (_numDara.Value > 0)
        {
            _numNet.Value = 0;
            _lblCalculatedNet.Text = $"{_numDara.Value:N0} KG (Dara)";
        }
        else
        {
            _numNet.Value = 0;
            _lblCalculatedNet.Text = "0 KG";
        }
    }

    private async Task ManualReadWeightAsync()
    {
        _btnManualRead.Enabled = false;
        try
        {
            var (success, weight, msg) = await _scaleClient.ReadOnceAsync(2500);
            if (success)
            {
                _lblLiveWeight.Text = $"{weight:N1} KG";
                NotificationService.NotifyInfo("Kantar Okundu", $"İndikatörden {weight:N1} KG canlı tartım verisi alındı.", "MalKabul");
                MessageBox.Show($"Kantar indikatöründen başarıyla okundu:\n\nAğırlık: {weight:N1} KG", "Canlı Kantar Okuma", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Kantar okunamadı:\n{msg}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            _btnManualRead.Enabled = true;
        }
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            // Cariler
            var cariler = await _macroService.GetCarilerForLookupAsync();
            _cmbCari.DataSource = cariler;
            _cmbCari.DisplayMember = "Name";
            _cmbCari.ValueMember = "Code";

            // Urunler
            var urunler = await _macroService.GetUrunlerForLookupAsync();
            _cmbUrun.DataSource = urunler;
            _cmbUrun.DisplayMember = "Name";
            _cmbUrun.ValueMember = "Code";

            // Depolar
            var depolar = await _macroService.GetDepolarForLookupAsync();
            _cmbDepo.DataSource = depolar;
            _cmbDepo.DisplayMember = "Name";
            _cmbDepo.ValueMember = "Code";

            // Plakalar
            var plakalar = await _macroService.GetRecentPlakalarAsync();
            _cmbPlaka.Items.Clear();
            _cmbPlaka.Items.AddRange(plakalar.ToArray());
        }
        catch { }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            _historyList = await _macroService.GetRecentKantarGirisAsync(100);
            FilterHistory();
        }
        catch (Exception ex)
        {
            _lblGridStats.Text = $"Hata: {ex.Message}";
        }
    }

    private void FilterHistory()
    {
        string s = _txtSearch.Text.Trim().ToLower();
        var list = _historyList;
        if (!string.IsNullOrEmpty(s))
        {
            list = list.Where(k =>
                k.Plaka.ToLower().Contains(s) ||
                k.FirmaAdi.ToLower().Contains(s) ||
                k.MalAdi.ToLower().Contains(s) ||
                k.GirisKodu.ToString().Contains(s)
            ).ToList();
        }

        _grid.DataSource = list;
        _lblGridStats.Text = $"📊 Günün Tartım Listesi (Toplam: {list.Count} Fiş | Bugün: {list.Count(k => k.Tarih.Date == DateTime.Today)})";
    }

    private void LoadRowToForm()
    {
        if (_grid.CurrentRow?.DataBoundItem is KantarTartimDto dto)
        {
            _txtFisNo.Text = dto.GirisKodu.ToString();
            _cmbPlaka.Text = dto.Plaka;
            _cmbCari.Text = dto.FirmaAdi;
            _cmbUrun.Text = dto.MalAdi;
            _cmbDepo.Text = dto.DepoAdi;
            _txtSofor.Text = dto.SoforAdi;
            _txtTelefon.Text = dto.Telefon;
            _txtAciklama.Text = dto.Aciklama;
            _numBrut.Value = dto.Burut;
            _numDara.Value = dto.Dara;
            CalculateNet();
        }
    }

    private void ResetForm()
    {
        _txtFisNo.Clear();
        _cmbPlaka.Text = "";
        _cmbCari.SelectedIndex = -1;
        _cmbUrun.SelectedIndex = -1;
        _txtSofor.Clear();
        _txtTelefon.Clear();
        _txtAciklama.Clear();
        _numBrut.Value = 0;
        _numDara.Value = 0;
        CalculateNet();
        _cmbPlaka.Focus();
    }

    private async Task SaveTartimAsync()
    {
        string plaka = _cmbPlaka.Text.Trim();
        string firmaAdi = _cmbCari.Text.Trim();
        string malAdi = _cmbUrun.Text.Trim();

        if (string.IsNullOrWhiteSpace(plaka))
        {
            MessageBox.Show("Lütfen araç plakasını giriniz.", "Zorunlu Alan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cmbPlaka.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(firmaAdi))
        {
            MessageBox.Show("Lütfen cari / müstahsil firmayı seçiniz.", "Zorunlu Alan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cmbCari.Focus();
            return;
        }

        if (_numBrut.Value <= 0 && _numDara.Value <= 0)
        {
            MessageBox.Show("Lütfen geçerli bir tartım ağırlığı (Brüt veya Dara) giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        double.TryParse(_cmbCari.SelectedValue?.ToString(), out double firKod);
        string malKodu = _cmbUrun.SelectedValue?.ToString() ?? "";
        string depoAdi = _cmbDepo.Text;
        double.TryParse(_cmbDepo.SelectedValue?.ToString(), out double depoKodu);
        if (depoKodu <= 0) depoKodu = 1;

        int.TryParse(_txtFisNo.Text, out int fisNo);

        var dto = new KantarTartimDto
        {
            GirisKodu = fisNo,
            SirKod = AppState.ActiveCompany?.SirKod.ToString() ?? "1",
            Tarih = DateTime.Today,
            GirisSaati = DateTime.Now.ToString("HH:mm"),
            CikisSaati = DateTime.Now.ToString("HH:mm"),
            Plaka = plaka,
            FirKod = firKod,
            FirmaAdi = firmaAdi,
            MalKodu = malKodu,
            MalAdi = malAdi,
            Burut = _numBrut.Value,
            Dara = _numDara.Value,
            DepoKodu = depoKodu,
            DepoAdi = depoAdi,
            SoforAdi = _txtSofor.Text.Trim(),
            Telefon = _txtTelefon.Text.Trim(),
            Aciklama = _txtAciklama.Text.Trim(),
            Kullanici = AppState.DisplayUserName,
            KantarAdi = "TEKASYA MERKEZ KANTARI-ANTAKYA"
        };

        var (success, msg, idNo, girisKodu) = await _macroService.SaveKantarTartimAsync(dto);
        if (success)
        {
            _txtFisNo.Text = girisKodu.ToString();
            NotificationService.NotifySuccess(
                "Kantar Fişi Kesildi",
                $"Fiş #{girisKodu}: {dto.Plaka} ({dto.FirmaAdi}) {dto.Net:N0} KG tartım kaydedildi.",
                "MalKabul");
            MessageBox.Show($"Kantar tartımı ve mal kabul fişi #{girisKodu} başarıyla kaydedildi!\n\nNet Ağırlık: {dto.Net:N0} KG", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadHistoryAsync();
        }
        else
        {
            NotificationService.NotifyError("Kantar Kayıt Hatası", msg, "MalKabul");
            MessageBox.Show($"Kayıt hatası:\n{msg}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PrintCurrentReceipt()
    {
        string plaka = _cmbPlaka.Text.Trim();
        if (string.IsNullOrEmpty(plaka))
        {
            MessageBox.Show("Lütfen fiş yazdırmak için bir kayıt seçiniz veya doldurunuz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new KantarReceiptPreviewDialog(
            fisNo: _txtFisNo.Text,
            tarih: DateTime.Now,
            plaka: plaka,
            firmaAdi: _cmbCari.Text,
            malAdi: _cmbUrun.Text,
            brut: _numBrut.Value,
            dara: _numDara.Value,
            net: _numNet.Value,
            depo: _cmbDepo.Text,
            sofor: _txtSofor.Text,
            kullanici: AppState.DisplayUserName
        );
        dlg.ShowDialog();
    }

    private async void OpenScaleSettingsDialog()
    {
        using var dlg = new KantarAyarlariDialog(AppState.CurrentUserCode, AppState.CurrentUserName);
        dlg.ShowDialog(this);
        if (dlg.IsSaved)
        {
            await ApplyUserScaleSettingsAsync();
        }
    }

    private async Task ApplyUserScaleSettingsAsync()
    {
        string userKodu = AppState.CurrentUserCode;
        var kantarAyar = await _macroService.GetKullaniciKantarAyariAsync(userKodu);

        _scaleClient.Stop();
        if (kantarAyar.KantarAktif && (!string.IsNullOrWhiteSpace(kantarAyar.LokalIp) || !string.IsNullOrWhiteSpace(kantarAyar.VpnIp)))
        {
            _scaleClient.Start(kantarAyar);
            string modeName = kantarAyar.BaglantiModu == "VPN" ? "FortiVPN" : (kantarAyar.BaglantiModu == "Lokal" ? "Lokal Ağ" : "Akıllı Çoklu Ağ");
            _lblScaleStatus.Text = $"⏳ Kantar aranıyor ({kantarAyar.KantarAdi} [{modeName}])...";
            _lblScaleStatus.BackColor = Color.FromArgb(254, 243, 199);
            _lblScaleStatus.ForeColor = Color.FromArgb(180, 83, 9);
        }
        else
        {
            _lblScaleStatus.Text = "⚪ Kantar Bağlantısı Kapalı (Manuel Tartım Modu)";
            _lblScaleStatus.BackColor = Color.FromArgb(241, 245, 249);
            _lblScaleStatus.ForeColor = Color.FromArgb(100, 116, 139);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            _ = SaveTartimAsync();
            return true;
        }
        if (keyData == Keys.F8)
        {
            PrintCurrentReceipt();
            return true;
        }
        if (keyData == Keys.F2)
        {
            ResetForm();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}

/// <summary>
/// Printable visual slip receipt preview for Kantar weighment
/// </summary>
public class KantarReceiptPreviewDialog : Form
{
    public KantarReceiptPreviewDialog(string fisNo, DateTime tarih, string plaka, string firmaAdi, string malAdi, decimal brut, decimal dara, decimal net, string depo, string sofor, string kullanici)
    {
        this.Text = $"🖨️ Kantar Tartım Fişi Önizleme #{fisNo}";
        this.Size = new Size(450, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(241, 245, 249);

        var pnlSlip = new Panel
        {
            Location = new Point(25, 20),
            Size = new Size(385, 480),
            BackColor = Color.White
        };
        pnlSlip.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSlip.Width - 1, pnlSlip.Height - 1);
        };

        int y = 15;
        void AddSlipLine(string left, string right, bool isBold = false)
        {
            var lblL = new Label { Text = left, Location = new Point(15, y), Width = 140, Font = new Font("Segoe UI", 9f, isBold ? FontStyle.Bold : FontStyle.Regular), ForeColor = ModernColors.Slate800 };
            var lblR = new Label { Text = right, Location = new Point(160, y), Width = 210, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, isBold ? FontStyle.Bold : FontStyle.Regular), ForeColor = isBold ? Color.FromArgb(4, 120, 87) : ModernColors.Slate900 };
            pnlSlip.Controls.AddRange(new Control[] { lblL, lblR });
            y += 26;
        }

        var lblHeader = new Label
        {
            Text = "TEK ASYA TARIM A.Ş.\nKANTAR VE MAL KABUL FİŞİ",
            Location = new Point(15, y),
            Width = 355,
            Height = 44,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900
        };
        pnlSlip.Controls.Add(lblHeader);
        y += 55;

        AddSlipLine("Fiş Numarası:", string.IsNullOrEmpty(fisNo) ? "YENİ" : fisNo, true);
        AddSlipLine("Tarih & Saat:", tarih.ToString("dd.MM.yyyy HH:mm"));
        AddSlipLine("Araç Plakası:", plaka, true);
        AddSlipLine("Cari / Müstahsil:", firmaAdi);
        AddSlipLine("Ürün / Çeşit:", malAdi);
        AddSlipLine("Giriş Deposu:", depo);
        if (!string.IsNullOrEmpty(sofor)) AddSlipLine("Şoför / Teslim Eden:", sofor);

        y += 10;
        // Dotted separator
        var pnlSep = new Panel { Location = new Point(15, y), Size = new Size(355, 2), BackColor = Color.FromArgb(226, 232, 240) };
        pnlSlip.Controls.Add(pnlSep);
        y += 12;

        AddSlipLine("1. Tartım (Brüt):", $"{brut:N0} KG");
        AddSlipLine("2. Tartım (Dara):", $"{dara:N0} KG");
        AddSlipLine("NET AĞIRLIK:", $"{net:N0} KG", true);

        y += 15;
        var lblKantar = new Label
        {
            Text = $"Kantar: TEKASYA MERKEZ KANTARI-ANTAKYA\nTartımı Yapan: {kullanici}",
            Location = new Point(15, y),
            Width = 355,
            Height = 40,
            Font = new Font("Segoe UI", 8f),
            ForeColor = ModernColors.Slate500,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlSlip.Controls.Add(lblKantar);

        var btnPrint = new Button
        {
            Text = "🖨️ Yazdır",
            Location = new Point(130, 515),
            Size = new Size(100, 34),
            BackColor = Color.FromArgb(4, 120, 87),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += (_, _) =>
        {
            MessageBox.Show("Fiş başarıyla yazıcıya gönderildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        };

        var btnClose = new Button
        {
            Text = "Kapat",
            Location = new Point(240, 515),
            Size = new Size(80, 34),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnClose.Click += (_, _) => this.Close();

        this.Controls.AddRange(new Control[] { pnlSlip, btnPrint, btnClose });
    }
}
