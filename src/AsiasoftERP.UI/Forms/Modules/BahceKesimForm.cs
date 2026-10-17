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
/// Master Management Form for Orchard Purchase & Cutting Contracts ('sozlesmebahceana')
/// and realized harvest cost tracking ('bahce_kesim_maliyet').
/// </summary>
public class BahceKesimForm : Form
{
    private readonly BahceKesimService _service = new();

    // UI Components
    private Panel _pnlHeader = null!;
    private Panel _pnlKpis = null!;
    private TabControl _tabs = null!;
    private DataGridView _gridSozlesmeler = null!;
    private DataGridView _gridSeferler = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    // KPI Labels
    private Label _lblKpiSozlesmeVal = null!;
    private Label _lblKpiTaahhutVal = null!;
    private Label _lblKpiGerceklesenVal = null!;
    private Label _lblKpiHacimVal = null!;

    // Filter controls
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private ComboBox _cboDurum = null!;
    private TextBox _txtSearch = null!;

    private List<BahceSozlesmeDto> _sozlesmeler = new();
    private List<BahceKesimSeferDto> _seferler = new();

    public BahceKesimForm()
    {
        this.DoubleBuffered = true;
        InitializeComponents();
        this.Load += async (_, _) => await InitialLoadAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "Bahçe Kesim & Alım Sözleşmeleri Yönetimi";
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
            Text = "🌳",
            Font = new Font("Segoe UI Emoji", 20f),
            Location = new Point(16, 12),
            Size = new Size(42, 42)
        };

        var lblTitle = new Label
        {
            Text = "Bahçe Kesim & Alım Sözleşmeleri Yönetimi (Canlı)",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(64, 10),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = "Sözleşmeli bahçe alımları, tapu parseli, taahhüt edilen rekolte ve kantar kesim maliyetleri takibi",
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

        _dtpStart = new DateTimePicker { Location = new Point(fx, 11), Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(-1) };
        fx += 122;

        _dtpEnd = new DateTimePicker { Location = new Point(fx, 11), Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        fx += 130;

        var lblDurum = new Label { Text = "Durum:", Location = new Point(fx, 15), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        fx += 50;

        _cboDurum = new ComboBox { Location = new Point(fx, 11), Size = new Size(120, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboDurum.Items.AddRange(new object[] { "Tümü", "Aktif", "Tamamlandı", "İptal" });
        _cboDurum.SelectedIndex = 0;
        fx += 128;

        _txtSearch = new TextBox { Location = new Point(fx, 11), Size = new Size(180, 26), PlaceholderText = "🔍 Müstahsil, Bahçe, Mevki..." };
        _txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await RefreshDataAsync(); };
        fx += 188;

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

        // RIGHT ACTION BUTTONS
        var btnPrint = new Button
        {
            Text = "🖨️  Sözleşme Yazdır (A4)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(170, 28),
            Location = new Point(pnlFilter.Width - 190, 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += (_, _) => PrintCurrentContract();

        var btnEdit = new Button
        {
            Text = "✏️  Düzenle",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(95, 28),
            Location = new Point(pnlFilter.Width - 295, 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnEdit.FlatAppearance.BorderSize = 0;
        btnEdit.Click += (_, _) => OpenSelectedForEdit();

        var btnNew = new Button
        {
            Text = "➕  Yeni Sözleşme",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(140, 28),
            Location = new Point(pnlFilter.Width - 445, 10),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnNew.FlatAppearance.BorderSize = 0;
        btnNew.Click += (_, _) => OpenNewContract();

        pnlFilter.Controls.AddRange(new Control[] {
            lblTar, _dtpStart, _dtpEnd, lblDurum, _cboDurum, _txtSearch, btnSearch, btnNew, btnEdit, btnPrint
        });

        // 4. TAB CONTROL
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(14, 6),
            Font = new Font("Segoe UI", 9.5f)
        };

        // Tab 1: Sözleşmeler
        var tabSozlesmeler = new TabPage("  🌳  Bahçe Alım Sözleşmeleri Listesi  ");
        tabSozlesmeler.BackColor = Color.White;
        SetupSozlesmelerGrid();
        tabSozlesmeler.Controls.Add(_gridSozlesmeler);
        _tabs.TabPages.Add(tabSozlesmeler);

        // Tab 2: Kesim Seferleri & Maliyetler
        var tabSeferler = new TabPage("  🚜  Bahçe Kesim Seferleri & Maliyet Analizi (Canlı)  ");
        tabSeferler.BackColor = Color.White;
        SetupSeferlerGrid(tabSeferler);
        _tabs.TabPages.Add(tabSeferler);

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

        var card1 = CreateKpiCard("TOPLAM BAHÇE SÖZLEŞMESİ", "0 Sözleşme", ModernColors.Slate800, 0, cardWidth, cardHeight, out _lblKpiSozlesmeVal);
        var card2 = CreateKpiCard("TAAHHÜT EDİLEN REKOLTE", "0 Ton", Color.FromArgb(13, 148, 136), cardWidth + 12, cardWidth, cardHeight, out _lblKpiTaahhutVal);
        var card3 = CreateKpiCard("GERÇEKLEŞEN HASAT KESİMİ", "0 Ton", Color.FromArgb(16, 120, 70), (cardWidth + 12) * 2, cardWidth, cardHeight, out _lblKpiGerceklesenVal);
        var card4 = CreateKpiCard("TOPLAM SÖZLEŞME HACMİ", "0 TL", Color.FromArgb(180, 83, 9), (cardWidth + 12) * 3, cardWidth, cardHeight, out _lblKpiHacimVal);

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

    private void SetupSozlesmelerGrid()
    {
        _gridSozlesmeler = new DataGridView
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
            RowTemplate = { Height = 34 }
        };

        _gridSozlesmeler.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 4, 6, 4)
        };
        _gridSozlesmeler.ColumnHeadersHeight = 36;
        _gridSozlesmeler.GridColor = Color.FromArgb(226, 232, 240);

        _gridSozlesmeler.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colDurum", DataPropertyName = "Durumu", HeaderText = "Durum", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colSozNo", DataPropertyName = "SozlesmeNo", HeaderText = "Sözleşme No", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colTur", DataPropertyName = "SozlesmeTuru", HeaderText = "Tür", Width = 75, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TarihFormatted", HeaderText = "Tarih", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colMustahsil", DataPropertyName = "FirmaAdi", HeaderText = "Müstahsil / Bahçe Sahibi", Width = 200, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colBahce", DataPropertyName = "BahceAdi", HeaderText = "Bahçe Tanımı", Width = 170 },
            new DataGridViewTextBoxColumn { Name = "colMevki", DataPropertyName = "Mevki", HeaderText = "Mevki / Köy", Width = 140 },
            new DataGridViewTextBoxColumn { Name = "colUrun", DataPropertyName = "MalAdi", HeaderText = "Ürün / Cins", Width = 140 },
            new DataGridViewTextBoxColumn { Name = "colHedefTon", DataPropertyName = "HedefTonFormatted", HeaderText = "Hedef Rekolte", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colGerceklesenTon", DataPropertyName = "GerceklesenTonFormatted", HeaderText = "Kesilen Ton", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 120, 70) } },
            new DataGridViewTextBoxColumn { Name = "colTamamlanma", DataPropertyName = "TamamlanmaFormatted", HeaderText = "Gerçekleşme", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colHacim", DataPropertyName = "SozlesmeTutariFormatted", HeaderText = "Sözleşme Tutarı", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colMaliyet", DataPropertyName = "GerceklesenMaliyetFormatted", HeaderText = "Kesim Maliyeti", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(180, 83, 9) } },
            new DataGridViewTextBoxColumn { Name = "colKgMaliyet", DataPropertyName = "BirimKgMaliyetFormatted", HeaderText = "Birim Maliyet", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) } }
        );

        _gridSozlesmeler.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _sozlesmeler.Count) return;
            var item = _sozlesmeler[e.RowIndex];

            if (e.ColumnIndex == 0) // Durum
            {
                if (item.Durumu == "Tamamlandı")
                {
                    e.CellStyle!.BackColor = Color.FromArgb(240, 253, 244);
                    e.CellStyle!.ForeColor = Color.FromArgb(16, 120, 70);
                }
                else if (item.Durumu == "İptal")
                {
                    e.CellStyle!.BackColor = Color.FromArgb(254, 242, 242);
                    e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);
                }
                else
                {
                    e.CellStyle!.BackColor = Color.FromArgb(239, 246, 255);
                    e.CellStyle!.ForeColor = Color.FromArgb(2, 132, 199);
                }
            }
        };

        _gridSozlesmeler.DoubleClick += (_, _) => OpenSelectedForEdit();
    }

    private void SetupSeferlerGrid(TabPage parent)
    {
        _gridSeferler = new DataGridView
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

        _gridSeferler.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        _gridSeferler.ColumnHeadersHeight = 36;
        _gridSeferler.GridColor = Color.FromArgb(226, 232, 240);

        _gridSeferler.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colSozNo", DataPropertyName = "SozlesmeNo", HeaderText = "Sözleşme No", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TarihFormatted", HeaderText = "Tarih", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colPlaka", DataPropertyName = "Plaka", HeaderText = "Plaka", Width = 100, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colSofor", DataPropertyName = "Sofor", HeaderText = "Şoför", Width = 130 },
            new DataGridViewTextBoxColumn { Name = "colCavus", DataPropertyName = "KesimCavusu", HeaderText = "Hasat Çavuşu", Width = 140 },
            new DataGridViewTextBoxColumn { Name = "colUrun", DataPropertyName = "MalAdi", HeaderText = "Ürün", Width = 140 },
            new DataGridViewTextBoxColumn { Name = "colNetKg", DataPropertyName = "NetKgFormatted", HeaderText = "Net KG", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colMalTutar", DataPropertyName = "MalTutari", HeaderText = "Mal Bedeli (TL)", Width = 120, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colIscilik", DataPropertyName = "YevmiyeTutari", HeaderText = "Amele/İşçilik (TL)", Width = 120, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colServis", DataPropertyName = "ServisTutari", HeaderText = "Servis (TL)", Width = 100, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colGenelToplam", DataPropertyName = "GenelToplamFormatted", HeaderText = "Toplam Sefer Maliyeti", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 120, 70) } },
            new DataGridViewTextBoxColumn { Name = "colKgMaliyet", DataPropertyName = "KgMaliyetFormatted", HeaderText = "Birim KG Maliyet", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) } }
        );

        parent.Controls.Add(_gridSeferler);
    }

    private async Task InitialLoadAsync()
    {
        await RefreshDataAsync();
        await LoadSeferlerAsync();
    }

    public async Task RefreshDataAsync()
    {
        try
        {
            _lblStatus.Text = "Bahçe alım sözleşmeleri yükleniyor...";
            this.Cursor = Cursors.WaitCursor;

            string? durum = _cboDurum.SelectedItem?.ToString();
            string? search = _txtSearch.Text.Trim();

            _sozlesmeler = await _service.GetSozlesmelerAsync(_dtpStart.Value, _dtpEnd.Value, durum, search);
            _gridSozlesmeler.DataSource = _sozlesmeler;

            // KPIs
            var kpi = await _service.GetKpiSummaryAsync();
            _lblKpiSozlesmeVal.Text = $"{kpi.ToplamSozlesme:N0} Sözleşme ({kpi.AktifSozlesme:N0} Aktif)";
            _lblKpiTaahhutVal.Text = $"{kpi.ToplamTaahhutTon:N1} Ton";
            _lblKpiGerceklesenVal.Text = $"{kpi.ToplamGerceklesenTon:N1} Ton";
            _lblKpiHacimVal.Text = $"{kpi.ToplamSozlesmeHacmi:N0} TL";

            _lblStatus.Text = $"Toplam {kpi.ToplamSozlesme} sözleşmeden {_sozlesmeler.Count} kayıt listelendi.";
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

    private async Task LoadSeferlerAsync()
    {
        try
        {
            _seferler = await _service.GetKesimSeferleriAsync();
            _gridSeferler.DataSource = _seferler;
        }
        catch { }
    }

    private void OpenNewContract()
    {
        using var dlg = new BahceSozlesmeEditDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _ = RefreshDataAsync();
        }
    }

    private void OpenSelectedForEdit()
    {
        if (_gridSozlesmeler.CurrentRow?.DataBoundItem is BahceSozlesmeDto dto)
        {
            using var dlg = new BahceSozlesmeEditDialog(dto);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _ = RefreshDataAsync();
            }
        }
        else
        {
            MessageBox.Show("Lütfen düzenlemek için bir sözleşme seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void PrintCurrentContract()
    {
        if (_gridSozlesmeler.CurrentRow?.DataBoundItem is BahceSozlesmeDto dto)
        {
            var seferler = await _service.GetKesimSeferleriAsync(dto.SozlesmeNo);
            BahceSozlesmeReportRenderer.PrintReport(dto, seferler);
        }
        else
        {
            MessageBox.Show("Lütfen yazdırmak için bir bahçe sözleşmesi seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
