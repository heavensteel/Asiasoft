using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Executive Farmer Purchase Receipts Management Form (Müstahsil Makbuzu)
/// conforming strictly to V.U.K. 235 and MariaDB 'mustahsilmakbuzu'.
/// </summary>
public class MustahsilMakbuzForm : Form
{
    private readonly MustahsilMakbuzService _service = new();
    private List<MustahsilMakbuzDto> _makbuzlar = new();

    // KPI Labels
    private Label _lblKpiCount = null!;
    private Label _lblKpiKg = null!;
    private Label _lblKpiKesinti = null!;
    private Label _lblKpiNetOdenen = null!;

    // Filter Controls
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private TextBox _txtSearch = null!;
    private Button _btnSearch = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnSms = null!;
    private Button _btnXml = null!;
    private Button _btnPrint = null!;
    private Button _btnDelete = null!;
    private Button _btnRefresh = null!;

    // Grid & Status
    private DataGridView _grid = null!;
    private Label _lblStatusInfo = null!;

    public MustahsilMakbuzForm()
    {
        this.DoubleBuffered = true;
        this.Text = "🌾 Müstahsil Makbuzları Yönetimi";
        this.Size = new Size(1300, 750);
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        InitializeUI();
        _ = LoadDataAsync();
    }

    private void InitializeUI()
    {
        // 1. TOP HEADER BANNER
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
            Text = "🌾",
            Font = new Font("Segoe UI", 20f),
            Location = new Point(16, 12),
            Size = new Size(40, 40)
        };

        var lblTitle = new Label
        {
            Text = "Müstahsil Makbuzları Yönetimi (Canlı Veri & V.U.K. 235)",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(62, 10),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Çiftçiden zirai mahsul alımları, Gelir Vergisi stopajı (%2), SGK/Bağ-Kur ve Ticaret Borsası tevkifat takibi",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(64, 34),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSub });

        // 2. KPI SUMMARY CARDS
        var pnlKpis = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(16, 8, 16, 8)
        };

        int cardW = 270;
        int gap = 14;
        int cx = 16;

        // Card 1: Makbuz Sayısı
        var card1 = CreateKpiCard("TOPLAM MÜSTAHSİL MAKBUZU", "0 Makbuz", ModernColors.Slate900, ModernColors.Slate600, cx, cardW);
        _lblKpiCount = (Label)card1.Controls[1];
        cx += cardW + gap;

        // Card 2: Net Tonaj
        var card2 = CreateKpiCard("TOPLAM SATIN ALINAN MAHSUL", "0,0 Ton", Color.FromArgb(13, 148, 136), ModernColors.Slate600, cx, cardW);
        _lblKpiKg = (Label)card2.Controls[1];
        cx += cardW + gap;

        // Card 3: Toplam Yasal Kesintiler
        var card3 = CreateKpiCard("YASAL KESİNTİLER (STOPAJ+BAĞKUR+BORSA)", "0 TL", Color.FromArgb(190, 18, 60), ModernColors.Slate600, cx, cardW);
        _lblKpiKesinti = (Label)card3.Controls[1];
        cx += cardW + gap;

        // Card 4: Net Ödenen
        var card4 = CreateKpiCard("MÜSTAHSİLE NET ÖDENEN BEDEL", "0 TL", Color.FromArgb(16, 120, 70), ModernColors.Slate600, cx, cardW + 40);
        _lblKpiNetOdenen = (Label)card4.Controls[1];

        pnlKpis.Controls.AddRange(new Control[] { card1, card2, card3, card4 });

        // 3. TOOLBAR PANEL
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

        int tx = 16;

        var lblTarih = new Label { Text = "Tarih:", Location = new Point(tx, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        tx += 45;

        _dtpStart = new DateTimePicker { Location = new Point(tx, 12), Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-60) };
        tx += 122;

        _dtpEnd = new DateTimePicker { Location = new Point(tx, 12), Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        tx += 125;

        _txtSearch = new TextBox { Location = new Point(tx, 12), Size = new Size(200, 26), PlaceholderText = "Makbuz No, Çiftçi Adı veya TC..." };
        _txtSearch.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadDataAsync(); } };
        tx += 208;

        _btnSearch = CreateToolButton("Filtrele", ModernColors.Slate700, Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), tx, 75);
        _btnSearch.Click += async (_, _) => await LoadDataAsync();
        tx += 85;

        // Action Buttons (Right Aligned Container)
        var pnlActions = new Panel { Dock = DockStyle.Right, Width = 720, BackColor = Color.Transparent };
        int ax = 0;

        _btnNew = CreateToolButton("➕ Yeni", Color.FromArgb(16, 120, 70), Color.FromArgb(240, 253, 244), Color.FromArgb(134, 239, 172), ax, 80);
        _btnNew.Click += (_, _) => OpenMakbuzDialog(null);
        ax += 86;

        _btnEdit = CreateToolButton("✏️ Düzenle", ModernColors.Slate700, Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), ax, 85);
        _btnEdit.Click += (_, _) => EditSelectedMakbuz();
        ax += 91;

        _btnSms = CreateToolButton("📲 SMS Onayı", Color.FromArgb(15, 23, 42), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), ax, 105);
        _btnSms.Click += (_, _) => SmsVerifySelectedMakbuz();
        ax += 111;

        _btnXml = CreateToolButton("📄 GİB XML", Color.FromArgb(15, 23, 42), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), ax, 90);
        _btnXml.Click += (_, _) => ShowXmlForSelectedMakbuz();
        ax += 96;

        _btnPrint = CreateToolButton("🖨️ Yazdır", Color.FromArgb(30, 41, 59), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), ax, 85);
        _btnPrint.Click += (_, _) => PrintSelectedMakbuz();
        ax += 91;

        _btnDelete = CreateToolButton("🗑️ Sil", Color.FromArgb(190, 18, 60), Color.FromArgb(255, 241, 242), Color.FromArgb(254, 205, 211), ax, 65);
        _btnDelete.Click += async (_, _) => await DeleteSelectedMakbuzAsync();
        ax += 71;

        _btnRefresh = CreateToolButton("⟳ Yenile", ModernColors.Slate700, Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), ax, 75);
        _btnRefresh.Click += async (_, _) => await LoadDataAsync();

        pnlActions.Controls.AddRange(new Control[] { _btnNew, _btnEdit, _btnSms, _btnXml, _btnPrint, _btnDelete, _btnRefresh });

        pnlToolbar.Controls.AddRange(new Control[] { lblTarih, _dtpStart, _dtpEnd, _txtSearch, _btnSearch, pnlActions });

        // 4. BOTTOM STATUS BAR
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        _lblStatusInfo = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate600,
            Text = "Yükleniyor..."
        };

        var lblHint = new Label
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = ModernColors.Slate500,
            Text = "💡 Çift tıklayarak müstahsil makbuzunu görüntüleyebilir veya düzenleyebilirsiniz."
        };

        pnlBottom.Controls.AddRange(new Control[] { _lblStatusInfo, lblHint });

        // 5. MAIN DATAGRIDVIEW
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            BackgroundColor = Color.White
        };
        ModernUiHelper.ApplyModernGridStyle(_grid);
        _grid.RowTemplate.Height = 36;

        SetupGridColumns();

        _grid.CellPainting += Grid_CellPainting;
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelectedMakbuz();
        };

        // Context Menu
        var contextMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9f) };
        contextMenu.Items.Add("🖨️ Resmi A4 Makbuzu Yazdır", null, (_, _) => PrintSelectedMakbuz());
        contextMenu.Items.Add("✏️ Makbuzu Düzenle", null, (_, _) => EditSelectedMakbuz());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("📲 SMS Onayı / Kodu Gönder", null, (_, _) => SmsVerifySelectedMakbuz());
        contextMenu.Items.Add("📄 GİB UBL-TR XML Önizle", null, (_, _) => ShowXmlForSelectedMakbuz());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("🗑️ Makbuzu Sil", null, async (_, _) => await DeleteSelectedMakbuzAsync());
        contextMenu.Items.Add("⟳ Listeyi Yenile", null, async (_, _) => await LoadDataAsync());
        _grid.ContextMenuStrip = contextMenu;

        this.Controls.Add(_grid);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlHeader);
    }

    private Panel CreateKpiCard(string title, string value, Color valColor, Color titleColor, int x, int w)
    {
        var pnl = new Panel
        {
            Location = new Point(x, 8),
            Size = new Size(w, 62),
            BackColor = Color.White,
            Padding = new Padding(12, 8, 12, 8)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };

        var lblT = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = titleColor,
            Location = new Point(10, 8),
            AutoSize = true
        };

        var lblV = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = valColor,
            Location = new Point(10, 26),
            AutoSize = true
        };

        pnl.Controls.AddRange(new Control[] { lblT, lblV });
        return pnl;
    }

    private Button CreateToolButton(string text, Color textColor, Color bgColor, Color borderColor, int x, int w)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = textColor,
            BackColor = bgColor,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(w, 32),
            Location = new Point(x, 10),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        btn.FlatAppearance.BorderColor = borderColor;
        return btn;
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                Name = "MakbuzNo",
                DataPropertyName = "MakbuzNo",
                HeaderText = "Makbuz No",
                Width = 115,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "TarihFormatted",
                DataPropertyName = "TarihFormatted",
                HeaderText = "Tarih",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "TcNo",
                DataPropertyName = "TcNo",
                HeaderText = "T.C. Kimlik No",
                Width = 115,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "AdiSoyadi",
                DataPropertyName = "AdiSoyadi",
                HeaderText = "Müstahsil / Çiftçi Adı",
                Width = 190,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "SmsDurumuBadge",
                DataPropertyName = "SmsDurumuBadge",
                HeaderText = "GİB SMS Onay",
                Width = 145,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Bolgesi",
                DataPropertyName = "Bolgesi",
                HeaderText = "Bölge / İlçe",
                Width = 130
            },
            new DataGridViewTextBoxColumn
            {
                Name = "MalAdi",
                DataPropertyName = "MalAdi",
                HeaderText = "Mahsul Cinsi",
                Width = 150
            },
            new DataGridViewTextBoxColumn
            {
                Name = "MiktarFormatted",
                DataPropertyName = "MiktarFormatted",
                HeaderText = "Net KG",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "FiyatFormatted",
                DataPropertyName = "FiyatFormatted",
                HeaderText = "Birim Fiyat",
                Width = 95,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "BrutTutarFormatted",
                DataPropertyName = "BrutTutarFormatted",
                HeaderText = "Brüt Tutar",
                Width = 115,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "StopajFormatted",
                DataPropertyName = "StopajFormatted",
                HeaderText = "Stopaj (%2)",
                Width = 95,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    ForeColor = Color.FromArgb(185, 28, 28)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "BagkurFormatted",
                DataPropertyName = "BagkurFormatted",
                HeaderText = "Bağ-Kur (%2)",
                Width = 95,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    ForeColor = Color.FromArgb(185, 28, 28)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "NetKesintiFormatted",
                DataPropertyName = "NetKesintiFormatted",
                HeaderText = "Top. Kesinti",
                Width = 105,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(190, 18, 60)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "OdenenFormatted",
                DataPropertyName = "OdenenFormatted",
                HeaderText = "Net Ödenen",
                Width = 125,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(16, 120, 70)
                }
            }
        );
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex >= 0 && e.Graphics != null && _grid.Columns["SmsDurumuBadge"] != null && e.ColumnIndex == _grid.Columns["SmsDurumuBadge"]!.Index)
        {
            e.PaintBackground(e.CellBounds, true);
            var rowObj = _grid.Rows[e.RowIndex].DataBoundItem as MustahsilMakbuzDto;
            bool isVerified = rowObj?.IsSmsDogrulandi ?? false;

            var badgeRect = new Rectangle(
                e.CellBounds.X + (e.CellBounds.Width - 130) / 2,
                e.CellBounds.Y + (e.CellBounds.Height - 24) / 2,
                130, 24);

            Color bgColor = isVerified ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 243, 199);
            Color brdColor = isVerified ? Color.FromArgb(134, 239, 172) : Color.FromArgb(252, 211, 77);
            Color txtColor = isVerified ? Color.FromArgb(21, 128, 61) : Color.FromArgb(180, 83, 9);
            string text = isVerified ? $"✓ Onaylı ({rowObj!.SmsDogrulamaKodu})" : "⏳ SMS Bekliyor";

            using var brushBg = new SolidBrush(bgColor);
            using var penBrd = new Pen(brdColor, 1f);
            using var path = ModernUiHelper.GetRoundedPath(badgeRect, 6);
            e.Graphics.FillPath(brushBg, path);
            e.Graphics.DrawPath(penBrd, path);

            using var brushText = new SolidBrush(txtColor);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(text, new Font("Segoe UI", 8.2f, FontStyle.Bold), brushText, badgeRect, sf);

            e.Handled = true;
            return;
        }

        if (e.RowIndex >= 0 && e.Graphics != null && _grid.Columns["OdenenFormatted"] != null && e.ColumnIndex == _grid.Columns["OdenenFormatted"]!.Index)
        {
            e.PaintBackground(e.CellBounds, true);
            var valStr = e.Value?.ToString() ?? "";

            var badgeRect = new Rectangle(
                e.CellBounds.Right - 110,
                e.CellBounds.Y + (e.CellBounds.Height - 24) / 2,
                100, 24);

            using var brushBg = new SolidBrush(Color.FromArgb(240, 253, 244));
            using var penBrd = new Pen(Color.FromArgb(134, 239, 172), 1f);
            using var path = ModernUiHelper.GetRoundedPath(badgeRect, 6);
            e.Graphics.FillPath(brushBg, path);
            e.Graphics.DrawPath(penBrd, path);

            using var brushText = new SolidBrush(Color.FromArgb(16, 120, 70));
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(valStr, new Font("Segoe UI", 9f, FontStyle.Bold), brushText, badgeRect, sf);

            e.Handled = true;
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            DateTime start = _dtpStart.Value.Date;
            DateTime end = _dtpEnd.Value.Date;
            string search = _txtSearch.Text.Trim();

            _makbuzlar = await _service.GetMakbuzlarAsync(start, end, search);
            var kpis = await _service.GetKpisAsync(start, end);

            _grid.DataSource = _makbuzlar;

            _lblKpiCount.Text = $"{kpis.ToplamMakbuzSayisi} Makbuz";
            _lblKpiKg.Text = kpis.ToplamTonFormatted;
            _lblKpiKesinti.Text = kpis.ToplamKesintiFormatted;
            _lblKpiNetOdenen.Text = kpis.NetOdenenFormatted;

            _lblStatusInfo.Text = $"👥 Gösterilen: {_makbuzlar.Count} Müstahsil Makbuzu  |  Brüt Hacim: {kpis.BrutHacimFormatted}  |  Net Ödenen: {kpis.NetOdenenFormatted}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Müstahsil makbuzları yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private MustahsilMakbuzDto? GetSelectedMakbuz()
    {
        if (_grid.CurrentRow?.DataBoundItem is MustahsilMakbuzDto m)
            return m;
        return null;
    }

    private void OpenMakbuzDialog(MustahsilMakbuzDto? m)
    {
        using var dlg = new MustahsilMakbuzEditDialog(m);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync();
        }
    }

    private void EditSelectedMakbuz()
    {
        var selected = GetSelectedMakbuz();
        if (selected == null)
        {
            MessageBox.Show("Lütfen düzenlemek için bir makbuz seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenMakbuzDialog(selected);
    }

    private void PrintSelectedMakbuz()
    {
        var selected = GetSelectedMakbuz();
        if (selected == null)
        {
            MessageBox.Show("Lütfen yazdırmak için bir makbuz seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        MustahsilMakbuzReportRenderer.PrintReport(selected);
    }

    private void ShowXmlForSelectedMakbuz()
    {
        var selected = GetSelectedMakbuz();
        if (selected == null)
        {
            MessageBox.Show("Lütfen XML'ini görüntülemek için bir makbuz seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string xml = selected.GenerateGibUblTrXml();

        using var dlg = new Form
        {
            Text = $"📄 GİB e-Müstahsil UBL-TR 1.2.1 XML — {selected.MakbuzNo}",
            Size = new Size(880, 640),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ModernColors.Slate100,
            Font = new Font("Segoe UI", 9.5f)
        };

        var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(16, 10, 16, 10) };
        var lblT = new Label
        {
            Text = $"GİB 22.05.2026 Duyurusu & V1.1 Kılavuzuna Uygun UBL-TR 1.2.1 ({selected.MakbuzNo})",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(14, 14),
            AutoSize = true
        };
        var btnCopy = new Button
        {
            Text = "📋 XML'i Kopyala",
            Dock = DockStyle.Right,
            Width = 140,
            BackColor = Color.FromArgb(16, 120, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopy.FlatAppearance.BorderSize = 0;
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(xml);
            MessageBox.Show("UBL-TR 1.2.1 XML panoya kopyalandı.", "Kopyalandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        pnlHdr.Controls.AddRange(new Control[] { lblT, btnCopy });

        var txtXml = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9.5f),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(226, 232, 240),
            Text = xml,
            ReadOnly = true
        };

        dlg.Controls.Add(txtXml);
        dlg.Controls.Add(pnlHdr);
        dlg.ShowDialog(this);
    }

    private void SmsVerifySelectedMakbuz()
    {
        var selected = GetSelectedMakbuz();
        if (selected == null)
        {
            MessageBox.Show("Lütfen SMS doğrulaması yapmak için bir makbuz seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        OpenMakbuzDialog(selected);
    }

    private async Task DeleteSelectedMakbuzAsync()
    {
        var selected = GetSelectedMakbuz();
        if (selected == null)
        {
            MessageBox.Show("Lütfen silinecek bir makbuz seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"'{selected.MakbuzNo}' no'lu {selected.AdiSoyadi} adına düzenlenmiş makbuz kalıcı olarak silinecek.\n\nOnaylıyor musunuz?",
            "Makbuzu Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            bool ok = await _service.DeleteMakbuzAsync(selected.IdNo);
            if (ok)
            {
                NotificationService.NotifySuccess(
                    "Makbuz Silindi",
                    $"'{selected.MakbuzNo}' no'lu müstahsil makbuzu silindi.",
                    "Mustahsil",
                    showToast: true);

                await LoadDataAsync();
            }
        }
    }
}
