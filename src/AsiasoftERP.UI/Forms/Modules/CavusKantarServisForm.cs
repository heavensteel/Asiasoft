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

/// <summary>
/// Master Management Dashboard Form for Çavuş, Kantar and Servis reconciliation.
/// Cross-verifies harvest crew leaders, weighbridge tickets and transport shuttle fees.
/// </summary>
public class CavusKantarServisForm : Form
{
    private readonly CavusKantarServisService _service = new();

    // UI Components
    private Panel _pnlHeader = null!;
    private Panel _pnlKpis = null!;
    private TabControl _tabs = null!;
    private DataGridView _gridEslesmeler = null!;
    private DataGridView _gridBolgeler = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    // KPI Labels
    private Label _lblKpiKantarVal = null!;
    private Label _lblKpiCavusVal = null!;
    private Label _lblKpiServisVal = null!;
    private Label _lblKpiBekleyenVal = null!;

    // Filter controls
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private ComboBox _cboCavusFilter = null!;
    private ComboBox _cboDurumFilter = null!;
    private TextBox _txtSearch = null!;

    private List<CavusKantarEslesmeDto> _eslesmeler = new();
    private List<CavusBolgeTanimDto> _bolgeler = new();
    private List<CavusLookupItemDto> _cavusListesi = new();

    public CavusKantarServisForm()
    {
        this.DoubleBuffered = true;
        InitializeComponents();
        this.Load += async (_, _) => await InitialLoadAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "Çavuş & Kantar & Servis Eşleme Yönetimi";
        this.Size = new Size(1360, 780);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. TOP HEADER
        _pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        _pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
        };

        var lblIcon = new Label
        {
            Text = "🤝",
            Font = new Font("Segoe UI Emoji", 20f),
            Location = new Point(16, 12),
            Size = new Size(42, 42)
        };

        var lblTitle = new Label
        {
            Text = "Çavuş & Kantar & Servis Eşleme Yönetimi (Canlı)",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(64, 10),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = "Hasat amele çavuşları, kantar tartım fişleri ve servis seferlerinin çapraz mutabakatı ve hakediş kontrolü",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(66, 36),
            AutoSize = true
        };
        _pnlHeader.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSubtitle });

        // 2. TOP KPI CARDS PANEL
        _pnlKpis = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(16, 8, 16, 8)
        };

        _pnlKpis.Resize += (_, _) => BuildKpiCards();
        BuildKpiCards();

        // 3. FILTER & ACTION TOOLBAR
        var pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        pnlFilter.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlFilter.Height - 1, pnlFilter.Width, pnlFilter.Height - 1);
        };

        int fx = 16;
        var lblTar = new Label { Text = "Tarih:", Location = new Point(fx, 15), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        fx += 42;

        _dtpStart = new DateTimePicker { Location = new Point(fx, 11), Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
        fx += 122;

        _dtpEnd = new DateTimePicker { Location = new Point(fx, 11), Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        fx += 130;

        var lblCavus = new Label { Text = "Çavuş:", Location = new Point(fx, 15), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        fx += 46;

        _cboCavusFilter = new ComboBox { Location = new Point(fx, 11), Size = new Size(170, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        fx += 178;

        var lblDurum = new Label { Text = "Durum:", Location = new Point(fx, 15), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        fx += 50;

        _cboDurumFilter = new ComboBox { Location = new Point(fx, 11), Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboDurumFilter.Items.AddRange(new object[] { "Tümü", "✅ Tam Eşleşenler", "⚠️ Çavuş Bekleyenler", "🚐 Servis Bekleyenler" });
        _cboDurumFilter.SelectedIndex = 0;
        fx += 148;

        _txtSearch = new TextBox { Location = new Point(fx, 11), Size = new Size(160, 26), PlaceholderText = "🔍 Plaka, Şoför, Ürün..." };
        _txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await RefreshDataAsync(); };
        fx += 168;

        var btnSearch = new Button
        {
            Text = "Filtrele",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(76, 28),
            Location = new Point(fx, 10),
            Cursor = Cursors.Hand
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += async (_, _) => await RefreshDataAsync();
        fx += 86;

        // RIGHT ACTION BUTTONS
        var btnPrint = new Button
        {
            Text = "🖨️  İcmal Yazdır (A4)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 28),
            Location = new Point(pnlFilter.Width - 170, 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += (_, _) => PrintCurrentReport();

        var btnBulkApprove = new Button
        {
            Text = "✅  Hızlı Onayla",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(115, 28),
            Location = new Point(pnlFilter.Width - 295, 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnBulkApprove.FlatAppearance.BorderSize = 0;
        btnBulkApprove.Click += async (_, _) => await BulkApproveSelectedAsync();

        var btnEdit = new Button
        {
            Text = "✏️  Seçileni Eşle",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 28),
            Location = new Point(pnlFilter.Width - 415, 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnEdit.FlatAppearance.BorderSize = 0;
        btnEdit.Click += (_, _) => OpenSelectedForEdit();

        pnlFilter.Controls.AddRange(new Control[] {
            lblTar, _dtpStart, _dtpEnd, lblCavus, _cboCavusFilter, lblDurum, _cboDurumFilter,
            _txtSearch, btnSearch, btnEdit, btnBulkApprove, btnPrint
        });

        // 4. TAB CONTROL
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(14, 6),
            Font = new Font("Segoe UI", 9.5f)
        };

        // Tab 1: Eşleşmeler
        var tabEslesme = new TabPage("  🚛  Kantar Tartımları & Çavuş-Servis Eşleşme Listesi  ");
        tabEslesme.BackColor = Color.White;
        SetupEslesmelerGrid();
        tabEslesme.Controls.Add(_gridEslesmeler);
        _tabs.TabPages.Add(tabEslesme);

        // Tab 2: Bölge Servis Fiyat Tanımları
        var tabBolgeler = new TabPage("  📋  Çavuş & Bölge Servis Fiyat Listesi (Tanımlar)  ");
        tabBolgeler.BackColor = Color.White;
        SetupBolgelerGrid(tabBolgeler);
        _tabs.TabPages.Add(tabBolgeler);

        // 5. BOTTOM STATUS STRIP
        _statusStrip = new StatusStrip
        {
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9f)
        };
        _lblStatus = new ToolStripStatusLabel
        {
            Text = "Hazır",
            ForeColor = ModernColors.Slate600
        };
        _statusStrip.Items.Add(_lblStatus);

        this.Controls.Add(_tabs);
        this.Controls.Add(pnlFilter);
        this.Controls.Add(_pnlKpis);
        this.Controls.Add(_pnlHeader);
        this.Controls.Add(_statusStrip);
    }

    private void BuildKpiCards()
    {
        _pnlKpis.Controls.Clear();
        int totalWidth = _pnlKpis.ClientSize.Width >= 600 
            ? _pnlKpis.ClientSize.Width - 32 
            : (this.ClientSize.Width >= 600 ? this.ClientSize.Width - 32 : 1300);
        int cardWidth = Math.Max((totalWidth - 36) / 4, 280);
        int cardHeight = 68;

        // Card 1: Toplam Kantar Tartımı
        var card1 = CreateKpiCard("TOPLAM KANTAR TARTIMI", "0 Sefer  (0 Ton)", ModernColors.Slate800, 0, cardWidth, cardHeight, out _lblKpiKantarVal);
        // Card 2: Çavuş Eşleşen Hasat
        var card2 = CreateKpiCard("ÇAVUŞ EŞLEŞEN HASAT", "0 Ton (%0)", Color.FromArgb(13, 148, 136), cardWidth + 12, cardWidth, cardHeight, out _lblKpiCavusVal);
        // Card 3: Servis Seferi & Maliyeti
        var card3 = CreateKpiCard("TOPLAM SERVİS HAKEDİŞİ", "0 TL  (0 Sefer)", Color.FromArgb(16, 120, 70), (cardWidth + 12) * 2, cardWidth, cardHeight, out _lblKpiServisVal);
        // Card 4: Eşleşme Bekleyen Tartım
        var card4 = CreateKpiCard("EŞLEŞME BEKLEYEN TARTIM", "0 Sefer", Color.FromArgb(220, 38, 38), (cardWidth + 12) * 3, cardWidth, cardHeight, out _lblKpiBekleyenVal);

        _pnlKpis.Controls.AddRange(new Control[] { card1, card2, card3, card4 });
    }

    private Panel CreateKpiCard(string title, string initialValue, Color accentColor, int x, int w, int h, out Label valLabel)
    {
        var pnl = new Panel
        {
            Location = new Point(16 + x, 8),
            Size = new Size(w, h),
            BackColor = Color.White
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var brushAccent = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(brushAccent, 0, 0, 4, pnl.Height);
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(14, 8),
            AutoSize = true
        };

        valLabel = new Label
        {
            Text = initialValue,
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(14, 28),
            AutoSize = true
        };

        pnl.Controls.AddRange(new Control[] { lblTitle, valLabel });
        return pnl;
    }

    private void SetupEslesmelerGrid()
    {
        _gridEslesmeler = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 34 }
        };

        _gridEslesmeler.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 4, 6, 4)
        };
        _gridEslesmeler.ColumnHeadersHeight = 36;
        _gridEslesmeler.GridColor = Color.FromArgb(226, 232, 240);

        _gridEslesmeler.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colDurum", DataPropertyName = "EslesmeDurumu", HeaderText = "Durum", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colGirisKodu", DataPropertyName = "GirisKodu", HeaderText = "Fiş No", Width = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TarihFormatted", HeaderText = "Tarih", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colPlaka", DataPropertyName = "Plaka", HeaderText = "Plaka", Width = 100, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colSofor", DataPropertyName = "SoforAdi", HeaderText = "Şoför", Width = 130 },
            new DataGridViewTextBoxColumn { Name = "colUrun", DataPropertyName = "UrunAdi", HeaderText = "Ürün / Hasat Cinsi", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "colMustahsil", DataPropertyName = "MustahsilAdi", HeaderText = "Bahçe / Müstahsil", Width = 170 },
            new DataGridViewTextBoxColumn { Name = "colNetKg", DataPropertyName = "NetKgFormatted", HeaderText = "Net KG", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colCavus", DataPropertyName = "CavusAdi", HeaderText = "Hasat Çavuşu", Width = 150, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) } },
            new DataGridViewTextBoxColumn { Name = "colBolge", DataPropertyName = "Bolge", HeaderText = "Bölge / Güzergah", Width = 120 },
            new DataGridViewTextBoxColumn { Name = "colServisSayisi", DataPropertyName = "ServisSayisi", HeaderText = "Servis", Width = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colServisTutari", DataPropertyName = "ServisTutariFormatted", HeaderText = "Servis Tutarı", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 120, 70) } },
            new DataGridViewCheckBoxColumn { Name = "colOnay", DataPropertyName = "Onay", HeaderText = "Onay", Width = 55 }
        );

        _gridEslesmeler.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _eslesmeler.Count) return;
            var item = _eslesmeler[e.RowIndex];

            if (e.ColumnIndex == 0) // Durum
            {
                if (item.Onay)
                {
                    e.CellStyle!.BackColor = Color.FromArgb(240, 253, 244);
                    e.CellStyle!.ForeColor = Color.FromArgb(16, 120, 70);
                }
                else if (item.EslesmeDurumu == "Onay Bekliyor")
                {
                    e.CellStyle!.BackColor = Color.FromArgb(239, 246, 255);
                    e.CellStyle!.ForeColor = Color.FromArgb(2, 132, 199);
                }
                else if (item.EslesmeDurumu == "Çavuş Eksik")
                {
                    e.CellStyle!.BackColor = Color.FromArgb(254, 242, 242);
                    e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);
                }
                else
                {
                    e.CellStyle!.BackColor = Color.FromArgb(254, 243, 199);
                    e.CellStyle!.ForeColor = Color.FromArgb(180, 83, 9);
                }
            }
        };

        _gridEslesmeler.DoubleClick += (_, _) => OpenSelectedForEdit();
    }

    private void SetupBolgelerGrid(TabPage parent)
    {
        _gridBolgeler = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 32 }
        };

        _gridBolgeler.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        _gridBolgeler.ColumnHeadersHeight = 36;
        _gridBolgeler.GridColor = Color.FromArgb(226, 232, 240);

        _gridBolgeler.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colId", DataPropertyName = "IdNo", HeaderText = "#", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "colCavus", DataPropertyName = "CavusAdi", HeaderText = "Çavuş Adı", Width = 220, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colFirKod", DataPropertyName = "FirKod", HeaderText = "Cari Kod", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colBolgeKodu", DataPropertyName = "BolgeKodu", HeaderText = "Bölge Kod", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colBolgeAdi", DataPropertyName = "BolgeAdi", HeaderText = "Bölge / Güzergah Tanımı", Width = 260 },
            new DataGridViewTextBoxColumn { Name = "colFiyat", DataPropertyName = "BirimFiyatFormatted", HeaderText = "Standart Servis Fiyatı", Width = 160, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 120, 70) } }
        );

        parent.Controls.Add(_gridBolgeler);
    }

    private async Task InitialLoadAsync()
    {
        try
        {
            _cavusListesi = await _service.GetCavuslarAsync();
            _cboCavusFilter.Items.Clear();
            _cboCavusFilter.Items.Add("TÜM ÇAVUŞLAR");
            foreach (var c in _cavusListesi)
            {
                _cboCavusFilter.Items.Add(c.CavusAdi);
            }
            _cboCavusFilter.SelectedIndex = 0;

            await RefreshDataAsync();
            await LoadBolgelerAsync();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Başlangıç yükleme hatası: {ex.Message}";
        }
    }

    public async Task RefreshDataAsync()
    {
        try
        {
            _lblStatus.Text = "Kantar tartımları ve çavuş eşleşmeleri yükleniyor...";
            this.Cursor = Cursors.WaitCursor;

            string? cavus = _cboCavusFilter.SelectedIndex > 0 ? _cboCavusFilter.SelectedItem?.ToString() : null;
            string? durum = _cboDurumFilter.SelectedIndex switch
            {
                1 => "TamEslesen",
                2 => "CavusBekleyen",
                3 => "ServisBekleyen",
                _ => null
            };
            string? search = _txtSearch.Text.Trim();

            _eslesmeler = await _service.GetEslesmelerAsync(_dtpStart.Value, _dtpEnd.Value, cavus, durum, search);
            _gridEslesmeler.DataSource = _eslesmeler;

            // Update KPIs
            var kpi = await _service.GetKpiSummaryAsync(_dtpStart.Value, _dtpEnd.Value);
            _lblKpiKantarVal.Text = $"{kpi.ToplamSefer} Sefer  ({kpi.ToplamNetTon:N1} Ton)";
            _lblKpiCavusVal.Text = $"{(kpi.ToplamNetKg / 1000.0):N1} Ton (%{kpi.EslesmeOrani:0.0})";
            _lblKpiServisVal.Text = $"{kpi.ToplamServisMaliyeti:N0} TL ({kpi.ToplamServisSayisi:0} Sefer)";
            _lblKpiBekleyenVal.Text = $"{kpi.BekleyenSefer} Sefer";

            _lblStatus.Text = $"Toplam {kpi.ToplamSefer} tartımdan {_eslesmeler.Count} kayıt listelendi. (Eşleşen: {kpi.EslesenSefer}, Bekleyen: {kpi.BekleyenSefer})";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Hata: {ex.Message}";
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }

    private async Task LoadBolgelerAsync()
    {
        try
        {
            _bolgeler = await _service.GetCavusBolgeTanimlariAsync();
            _gridBolgeler.DataSource = _bolgeler;
        }
        catch { }
    }

    private void OpenSelectedForEdit()
    {
        if (_gridEslesmeler.CurrentRow?.DataBoundItem is CavusKantarEslesmeDto dto)
        {
            using var dlg = new CavusKantarEslesmeDialog(dto);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _ = RefreshDataAsync();
            }
        }
        else
        {
            MessageBox.Show("Lütfen düzenlemek veya eşlemek için bir kantar tartım satırı seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task BulkApproveSelectedAsync()
    {
        var selectedDtos = _gridEslesmeler.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as CavusKantarEslesmeDto)
            .Where(d => d != null && d.GirisKodu > 0)
            .ToList();

        if (selectedDtos.Count == 0)
        {
            MessageBox.Show("Lütfen onaylamak için en az bir kantar seferi satırı seçiniz.", "Seçim Yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Seçili {selectedDtos.Count} adet kantar tartımının Çavuş ve Servis mutabakatını onaylamak istiyor musunuz?",
            "Toplu Onay",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            string user = AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL";
            int count = await _service.TopluOnaylaAsync(selectedDtos.Select(d => d!.GirisKodu).ToList(), user);
            NotificationService.NotifySuccess(
                "Toplu Eşleşme Onaylandı",
                $"{count} adet tartım ve servis seferi onaylandı.",
                "CavusServis",
                showToast: true);
            await RefreshDataAsync();
        }
    }

    private void PrintCurrentReport()
    {
        if (_eslesmeler.Count == 0)
        {
            MessageBox.Show("Yazdırılacak veri bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string cavus = _cboCavusFilter.SelectedIndex > 0 ? _cboCavusFilter.SelectedItem?.ToString() ?? "" : "";
        string bolge = "Tüm Güzergahlar";
        CavusKantarReportRenderer.PrintReport(_eslesmeler, cavus, bolge, _dtpStart.Value, _dtpEnd.Value);
    }
}
