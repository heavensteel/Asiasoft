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

public class YuklemeProgramiForm : Form
{
    private readonly YuklemeProgramiService _service = new();

    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cboStatusFilter = null!;
    private Label _lblTotalCount = null!;
    private Label _lblTotalPallet = null!;
    private Label _lblStatusText = null!;
    private ModernGridManager _gridManager = null!;
    private FlowLayoutPanel _pnlActiveFiltersBar = null!;

    private List<YuklemeProgramiDto> _allPrograms = new();

    public YuklemeProgramiForm()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        InitializeComponents();
        this.Load += async (_, _) => await RefreshDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "Yükleme Programı ve Sevkiyat Yönetimi";
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);
        this.Dock = DockStyle.Fill;
        this.FormBorderStyle = FormBorderStyle.None;
        this.TopLevel = false;

        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            BackColor = ModernColors.Slate100
        };

        // 1. TOP HEADER & KPI CARDS
        var pnlHeader = BuildHeaderPanel();

        // 2. TOOLBAR (Search & Actions)
        var pnlToolbar = BuildToolbarPanel();

        // 3. DATA GRID CARD
        var pnlGridCard = BuildGridCard();

        pnlMain.Controls.Add(pnlGridCard);
        pnlMain.Controls.Add(pnlToolbar);
        pnlMain.Controls.Add(pnlHeader);

        this.Controls.Add(pnlMain);
    }

    private Panel BuildHeaderPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 14)
        };

        // Title and Subtitle
        var pnlTitle = new Panel
        {
            Dock = DockStyle.Left,
            Width = 520,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "🚚  Yükleme Programı (Sevkiyat Emirleri)",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "İhracat tır sevkiyat emirleri, çoklu ülke yükleme listeleri ve birebir antetli A4 onay formları",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(2, 38),
            AutoSize = true
        };

        pnlTitle.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // KPI Badges on the Right
        var pnlKpis = new Panel
        {
            Dock = DockStyle.Right,
            Width = 480,
            BackColor = Color.Transparent
        };

        int kx = 0;
        var cardProg = CreateKpiMiniCard("TOPLAM PROGRAM", "0 Adet", ModernColors.Emerald500);
        _lblTotalCount = (Label)cardProg.Controls["lblVal"]!;
        cardProg.Location = new Point(kx, 6);
        kx += 230;

        var cardPallet = CreateKpiMiniCard("TOPLAM PALET", "0 PLT", ModernColors.Indigo600);
        _lblTotalPallet = (Label)cardPallet.Controls["lblVal"]!;
        cardPallet.Location = new Point(kx, 6);

        pnlKpis.Controls.AddRange(new Control[] { cardProg, cardPallet });

        pnl.Controls.AddRange(new Control[] { pnlTitle, pnlKpis });
        return pnl;
    }

    private Panel CreateKpiMiniCard(string label, string val, Color accent)
    {
        var pnl = new Panel
        {
            Size = new Size(220, 68),
            BackColor = Color.White,
            Padding = new Padding(14, 10, 14, 10)
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
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(14, 8),
            AutoSize = true
        };

        var lblV = new Label
        {
            Name = "lblVal",
            Text = val,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(12, 28),
            AutoSize = true
        };

        pnl.Controls.AddRange(new Control[] { lblT, lblV });
        return pnl;
    }

    private Panel BuildToolbarPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 12)
        };

        // Left Side: Search + Filter
        var pnlSearch = new Panel
        {
            Dock = DockStyle.Left,
            Width = 460,
            BackColor = Color.Transparent
        };

        var lblSearchIcon = new Label
        {
            Text = "🔍",
            Location = new Point(2, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 11f)
        };

        _txtSearch = new TextBox
        {
            Location = new Point(28, 8),
            Size = new Size(260, 28),
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "Program No, Hedef veya Hazırlayan ara..."
        };
        _txtSearch.TextChanged += (_, _) => FilterData();

        _cboStatusFilter = new ComboBox
        {
            Location = new Point(298, 8),
            Size = new Size(130, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cboStatusFilter.Items.AddRange(new object[] { "Tüm Durumlar", "Onaylandı", "Taslak", "Yüklendi" });
        _cboStatusFilter.SelectedIndex = 0;
        _cboStatusFilter.SelectedIndexChanged += (_, _) => FilterData();

        pnlSearch.Controls.AddRange(new Control[] { lblSearchIcon, _txtSearch, _cboStatusFilter });

        // Right Side: Action Buttons
        var pnlActions = new Panel
        {
            Dock = DockStyle.Right,
            Width = 910,
            BackColor = Color.Transparent
        };

        int ax = pnlActions.Width;

        // 1. Yeni Program
        ax -= 160;
        var btnNew = new Button
        {
            Text = "➕  Yeni Program",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Emerald500,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnNew.FlatAppearance.BorderSize = 0;
        btnNew.Click += async (_, _) => await OpenCreateNewAsync();

        // 2. Düzenle
        ax -= 100;
        var btnEdit = new Button
        {
            Text = "✏️  Düzenle",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Blue600,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnEdit.FlatAppearance.BorderSize = 0;
        btnEdit.Click += async (_, _) => await OpenSelectedForEditAsync();

        // 3. Yazdır / Önizle
        ax -= 125;
        var btnPrint = new Button
        {
            Text = "🖨️  A4 Yazdır",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Indigo600,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(115, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += async (_, _) => await PrintSelectedAsync();

        // 3.5. WhatsApp Paylaşımı
        ax -= 135;
        var btnWhatsApp = new Button
        {
            Text = "💬  WhatsApp",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.WhatsAppGreen,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(125, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnWhatsApp.FlatAppearance.BorderSize = 0;
        btnWhatsApp.Click += async (_, _) => await ShareSelectedWhatsAppAsync();

        // 4. Kopyala / Çoğalt
        ax -= 100;
        var btnClone = new Button
        {
            Text = "📋  Çoğalt",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate800,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnClone.FlatAppearance.BorderSize = 0;
        btnClone.Click += async (_, _) => await CloneSelectedAsync();

        // 5. Parametreler / Sütun Düzeni
        ax -= 135;
        var btnParams = new Button
        {
            Text = "⚙️  Parametreler",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(125, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnParams.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnParams.Click += (_, _) => _gridManager.ShowParametersDialog();

        // 6. Forklift Operatörleri Tanım Rehberi
        ax -= 135;
        var btnForklift = new Button
        {
            Text = "🚜  Forkliftçiler",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(125, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnForklift.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnForklift.Click += (_, _) =>
        {
            using var dlg = new ForkliftOperatorTanimDialog();
            dlg.ShowDialog(this);
        };

        // 7. Yenile
        ax -= 80;
        var btnRefresh = new Button
        {
            Text = "🔄  Yenile",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(72, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += async (_, _) => await RefreshDataAsync();

        pnlActions.Controls.AddRange(new Control[] { btnNew, btnEdit, btnPrint, btnWhatsApp, btnClone, btnParams, btnForklift, btnRefresh });

        pnl.Controls.AddRange(new Control[] { pnlSearch, pnlActions });
        return pnl;
    }

    private Panel BuildGridCard()
    {
        var pnlCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        _pnlActiveFiltersBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = ModernColors.Slate50,
            Padding = new Padding(8, 4, 8, 4),
            Visible = false
        };
        _pnlActiveFiltersBar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, _pnlActiveFiltersBar.Height - 1, _pnlActiveFiltersBar.Width, _pnlActiveFiltersBar.Height - 1);
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = true,
            AllowUserToResizeColumns = true,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 40 }
        };

        // Grid Style
        _grid.ColumnHeadersDefaultCellStyle.BackColor = ModernColors.Slate800;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 42;

        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
        _grid.DefaultCellStyle.SelectionForeColor = ModernColors.Slate900;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

        // Columns
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Program No", DataPropertyName = "ProgramNo", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "TarihFormatted", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Genel Başlık / Hedef Müşteri", DataPropertyName = "Baslik", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Araç / Sütun", DataPropertyName = "SutunBilgi", Width = 105 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Toplam Palet", DataPropertyName = "PaletBilgi", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Forklift Operatörü", DataPropertyName = "ForkliftOperator", Width = 145 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hazırlayan", DataPropertyName = "Hazirlayan", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Durum", DataPropertyName = "Durum", Width = 100 });

        _grid.CellDoubleClick += async (_, _) => await OpenSelectedForEditAsync();
        _grid.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                await OpenSelectedForEditAsync();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
                await DeleteSelectedAsync();
            }
        };

        // Context Menu
        var ctxMenu = new ContextMenuStrip();
        ctxMenu.Items.Add("✏️ Düzenle", null, async (_, _) => await OpenSelectedForEditAsync());
        ctxMenu.Items.Add("🖨️ A4 Baskı Önizleme / Yazdır", null, async (_, _) => await PrintSelectedAsync());
        ctxMenu.Items.Add("💬 WhatsApp ile Paylaş", null, async (_, _) => await ShareSelectedWhatsAppAsync());
        ctxMenu.Items.Add("📋 Şablon Olarak Çoğalt", null, async (_, _) => await CloneSelectedAsync());
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("🗑️ Sil", null, async (_, _) => await DeleteSelectedAsync());
        _grid.ContextMenuStrip = ctxMenu;

        // Bottom Status Strip
        var pnlGridBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(12, 0, 12, 0)
        };
        _lblStatusText = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Text = "Yükleniyor..."
        };
        pnlGridBottom.Controls.Add(_lblStatusText);

        pnlCard.Controls.Add(_grid);
        pnlCard.Controls.Add(_pnlActiveFiltersBar);
        pnlCard.Controls.Add(pnlGridBottom);

        _gridManager = new ModernGridManager(_grid, "YuklemeProgramiForm");
        _gridManager.AttachActiveFiltersBar(_pnlActiveFiltersBar);
        return pnlCard;
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            _lblStatusText.Text = "Veriler MariaDB'den çekiliyor...";
            _allPrograms = await _service.GetAllAsync();
            FilterData();
        }
        catch (Exception ex)
        {
            _lblStatusText.Text = $"Hata: {ex.Message}";
        }
    }

    private void FilterData()
    {
        string search = _txtSearch.Text.Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));
        string statusFilter = _cboStatusFilter.SelectedItem?.ToString() ?? "Tüm Durumlar";

        var filtered = _allPrograms.Where(p =>
        {
            bool matchSearch = string.IsNullOrEmpty(search) ||
                               (p.ProgramNo?.ToLower().Contains(search) ?? false) ||
                               (p.Baslik?.ToLower().Contains(search) ?? false) ||
                               (p.Hazirlayan?.ToLower().Contains(search) ?? false);

            bool matchStatus = statusFilter == "Tüm Durumlar" || p.Durum == statusFilter;

            return matchSearch && matchStatus;
        }).ToList();

        var displayList = filtered.Select(p => new
        {
            p.Id,
            p.ProgramNo,
            TarihFormatted = p.Tarih.ToString("dd.MM.yyyy"),
            p.Baslik,
            SutunBilgi = p.SatirSayisi > 1 
                ? $"{p.SatirSayisi}x{p.SutunSayisi} Grid ({p.ToplamHucreSayisi} Bölüm)" 
                : (p.SutunSayisi > 1 ? $"{p.SutunSayisi} Sütun" : "1 Sütun (Tam Sayfa)"),
            PaletBilgi = $"{p.ToplamPalet} PLT",
            ForkliftOperator = string.IsNullOrWhiteSpace(p.ForkliftOperator) ? "-" : p.ForkliftOperator,
            p.Hazirlayan,
            p.Durum,
            Original = p
        }).ToList();

        _grid.DataSource = displayList;

        // Update KPIs
        _lblTotalCount.Text = $"{_allPrograms.Count} Adet";
        int totalPallets = _allPrograms.Sum(p => p.ToplamPalet);
        _lblTotalPallet.Text = $"{totalPallets:N0} PLT";
        _lblStatusText.Text = $"Toplam {filtered.Count} yükleme programı listelendi. (Veritabanı: Canlı)";
    }

    private YuklemeProgramiDto? GetSelectedProgram()
    {
        if (_grid.CurrentRow?.DataBoundItem == null) return null;
        dynamic row = _grid.CurrentRow.DataBoundItem;
        return (YuklemeProgramiDto?)row.Original;
    }

    private async Task OpenCreateNewAsync()
    {
        string nextProgNo = await _service.GenerateNextProgramNoAsync();
        var newProg = new YuklemeProgramiDto
        {
            ProgramNo = nextProgNo,
            Tarih = DateTime.Today,
            Baslik = "YENİ İHRACAT SEVKİYATI",
            SatirSayisi = 1,
            SutunSayisi = 1,
            Hazirlayan = AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL",
            Onaylayan = "İHRACAT BÖLÜMÜ",
            Durum = "Onaylandı",
            Sutunlar = new List<YuklemeSutunDto>
            {
                new YuklemeSutunDto
                {
                    SatirNo = 1,
                    SutunNo = 1,
                    HucreIndex = 1,
                    Baslik = "HEDEF ÜLKE / ALICI",
                    ToplamPalet = 26
                }
            }
        };

        using var dlg = new YuklemeProgramiEditDialog(newProg);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await RefreshDataAsync();
        }
    }

    private async Task OpenSelectedForEditAsync()
    {
        var sel = GetSelectedProgram();
        if (sel == null)
        {
            MessageBox.Show("Lütfen düzenlemek istediğiniz yükleme programını seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Fetch full entity with column details
        var fullProg = await _service.GetByIdAsync(sel.Id);
        if (fullProg == null) return;

        using var dlg = new YuklemeProgramiEditDialog(fullProg);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await RefreshDataAsync();
        }
    }

    private async Task PrintSelectedAsync()
    {
        var sel = GetSelectedProgram();
        if (sel == null)
        {
            MessageBox.Show("Lütfen yazdırmak istediğiniz yükleme programını seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var fullProg = await _service.GetByIdAsync(sel.Id);
        if (fullProg == null) return;

        try
        {
            var doc = YuklemeProgramiReportRenderer.CreatePrintDocument(fullProg);
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
            MessageBox.Show($"Baskı önizleme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ShareSelectedWhatsAppAsync()
    {
        var sel = GetSelectedProgram();
        if (sel == null)
        {
            MessageBox.Show("Lütfen WhatsApp ile paylaşmak istediğiniz yükleme programını seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var fullProg = await _service.GetByIdAsync(sel.Id);
        if (fullProg == null) return;

        try
        {
            using var dlg = new WhatsAppShareDialog(fullProg);
            dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WhatsApp paylaşımı açılırken hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CloneSelectedAsync()
    {
        var sel = GetSelectedProgram();
        if (sel == null) return;

        var fullProg = await _service.GetByIdAsync(sel.Id);
        if (fullProg == null) return;

        // Clone
        string nextProgNo = await _service.GenerateNextProgramNoAsync();
        var cloned = new YuklemeProgramiDto
        {
            Id = 0, // New
            ProgramNo = nextProgNo,
            Tarih = DateTime.Today,
            Baslik = $"{fullProg.Baslik} (Kopya)",
            SatirSayisi = fullProg.SatirSayisi,
            SutunSayisi = fullProg.SutunSayisi,
            ToplamPalet = fullProg.ToplamPalet,
            Hazirlayan = AppState.ActiveUser?.KullaniciAdi ?? fullProg.Hazirlayan,
            Onaylayan = fullProg.Onaylayan,
            Durum = "Taslak",
            Notlar = fullProg.Notlar,
            Sutunlar = fullProg.Sutunlar.Select(s => new YuklemeSutunDto
            {
                SatirNo = s.SatirNo,
                SutunNo = s.SutunNo,
                HucreIndex = s.HucreIndex,
                Baslik = s.Baslik,
                UrunlerMetin = s.UrunlerMetin,
                TalimatlarMetin = s.TalimatlarMetin,
                PartilerMetin = s.PartilerMetin,
                TarihlerMetin = s.TarihlerMetin,
                ToplamPalet = s.ToplamPalet
            }).ToList()
        };

        using var dlg = new YuklemeProgramiEditDialog(cloned);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            NotificationService.NotifyInfo("Program Kopyalandı", $"'{cloned.ProgramNo}' ({cloned.Baslik}) kopya olarak oluşturuldu.", "YuklemeProgrami");
            await RefreshDataAsync();
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var sel = GetSelectedProgram();
        if (sel == null) return;

        var res = MessageBox.Show($"'{sel.ProgramNo} — {sel.Baslik}' yükleme programını silmek istediğinize emin misiniz?",
                                  "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (res == DialogResult.Yes)
        {
            await _service.DeleteAsync(sel.Id);
            NotificationService.NotifyWarning("Program Silindi", $"'{sel.ProgramNo} — {sel.Baslik}' başarıyla silindi.", "YuklemeProgrami");
            await RefreshDataAsync();
        }
    }
}
