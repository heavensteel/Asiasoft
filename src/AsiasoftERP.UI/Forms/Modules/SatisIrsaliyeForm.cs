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

public class SatisIrsaliyeForm : Form
{
    private readonly IrsaliyeService _service = new();
    private List<IrsaliyeStkDto> _allData = new();
    private ModernGridManager _gridManager = null!;

    // UI Controls
    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cboStatusFilter = null!;
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private FlowLayoutPanel _pnlActiveFiltersBar = null!;
    private Label _lblStatusText = null!;

    // KPI Labels
    private Label _lblTotalCount = null!;
    private Label _lblTotalWeight = null!;
    private Label _lblOpenCount = null!;
    private Label _lblTotalAmount = null!;

    public SatisIrsaliyeForm()
    {
        this.DoubleBuffered = true;
        InitializeComponents();
        this.Load += async (_, _) => await RefreshDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "Satış Sevk İrsaliyeleri";
        this.Size = new Size(1380, 800);
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = ModernColors.Slate100
        };

        var pnlHeader = BuildHeaderPanel();
        var pnlToolbar = BuildToolbarPanel();
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
            Height = 78,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 10)
        };

        var pnlTitle = new Panel
        {
            Dock = DockStyle.Left,
            Width = 480,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "🚚  Satış Sevk İrsaliyeleri (Canlı)",
            Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Toptan meyve ve narenciye sevkiyat irsaliyeleri, tartı/palet dökümleri ve faturalaştırma takibi",
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

        var cardAmount = CreateKpiCard("TOPLAM TUTAR", "0,00 TL", ModernColors.Emerald500);
        _lblTotalAmount = (Label)cardAmount.Controls["lblVal"]!;

        var cardOpen = CreateKpiCard("AÇIK İRSALİYELER", "0 Adet", ModernColors.Amber500);
        _lblOpenCount = (Label)cardOpen.Controls["lblVal"]!;

        var cardWeight = CreateKpiCard("NET SEVKİYAT", "0 KG", ModernColors.Indigo600);
        _lblTotalWeight = (Label)cardWeight.Controls["lblVal"]!;

        var cardCount = CreateKpiCard("TOPLAM İRSALİYE", "0 Adet", ModernColors.Blue600);
        _lblTotalCount = (Label)cardCount.Controls["lblVal"]!;

        pnlKpis.Controls.AddRange(new Control[] { cardAmount, cardOpen, cardWeight, cardCount });

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
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(8, 28),
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
            Margin = new Padding(0, 0, 0, 10)
        };

        // Left Side: Search + Date Filters + Status Combo
        var pnlFilters = new Panel
        {
            Dock = DockStyle.Left,
            Width = 680,
            BackColor = Color.Transparent
        };

        var lblSearchIcon = new Label { Text = "🔍", Location = new Point(2, 10), AutoSize = true, Font = new Font("Segoe UI", 11f) };
        _txtSearch = new TextBox
        {
            Location = new Point(28, 8),
            Size = new Size(200, 28),
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "İrsaliye, Cari, Plaka ara..."
        };
        _txtSearch.TextChanged += (_, _) => FilterData();

        _dtpStart = new DateTimePicker
        {
            Location = new Point(236, 8),
            Size = new Size(105, 28),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(-60)
        };
        _dtpEnd = new DateTimePicker
        {
            Location = new Point(346, 8),
            Size = new Size(105, 28),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };

        var btnDateFilter = new Button
        {
            Text = "Getir",
            Location = new Point(456, 7),
            Size = new Size(54, 30),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Blue600,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnDateFilter.FlatAppearance.BorderSize = 0;
        btnDateFilter.Click += async (_, _) => await RefreshDataAsync();

        _cboStatusFilter = new ComboBox
        {
            Location = new Point(516, 8),
            Size = new Size(155, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cboStatusFilter.Items.AddRange(new object[] { "Tüm İrsaliyeler", "Açık İrsaliyeler", "Faturalananlar" });
        _cboStatusFilter.SelectedIndex = 0;
        _cboStatusFilter.SelectedIndexChanged += (_, _) => FilterData();

        pnlFilters.Controls.AddRange(new Control[] { lblSearchIcon, _txtSearch, _dtpStart, _dtpEnd, btnDateFilter, _cboStatusFilter });

        // Right Side: Action Buttons
        var pnlActions = new Panel
        {
            Dock = DockStyle.Right,
            Width = 660,
            BackColor = Color.Transparent
        };

        int ax = pnlActions.Width;

        // 1. Yeni İrsaliye
        ax -= 150;
        var btnNew = new Button
        {
            Text = "➕  Yeni İrsaliye",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Emerald500,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(140, 34),
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

        // 3. Yazdır
        ax -= 115;
        var btnPrint = new Button
        {
            Text = "🖨️  A4 Yazdır",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Indigo600,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(105, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += async (_, _) => await PrintSelectedAsync();

        // 4. Faturaya Dönüştür
        ax -= 145;
        var btnInvoice = new Button
        {
            Text = "🧾  Faturalandır",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(135, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnInvoice.FlatAppearance.BorderColor = ModernColors.Slate300;
        btnInvoice.Click += async (_, _) => await ConvertSelectedToInvoiceAsync();

        // 5. Parametreler
        ax -= 125;
        var btnParams = new Button
        {
            Text = "⚙️  Parametreler",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate800,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(115, 34),
            Location = new Point(ax, 6),
            Cursor = Cursors.Hand
        };
        btnParams.FlatAppearance.BorderSize = 0;
        btnParams.Click += (_, _) => _gridManager.ShowParametersDialog();

        pnlActions.Controls.AddRange(new Control[] { btnNew, btnEdit, btnPrint, btnInvoice, btnParams });

        pnl.Controls.AddRange(new Control[] { pnlFilters, pnlActions });
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

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false
        };
        ModernUiHelper.ApplyModernGridStyle(_grid);
        SetupGridColumns();

        _grid.CellDoubleClick += async (_, _) => await OpenSelectedForEditAsync();
        _grid.CellFormatting += (s, e) => FormatGridCells(e);

        // Context Menu
        var ctxMenu = new ContextMenuStrip();
        ctxMenu.Items.Add("✏️ Düzenle / İncele", null, async (_, _) => await OpenSelectedForEditAsync());
        ctxMenu.Items.Add("🖨️ A4 Sevk İrsaliyesi Yazdır", null, async (_, _) => await PrintSelectedAsync());
        ctxMenu.Items.Add("🧾 Faturaya Dönüştür (Faturalandır)", null, async (_, _) => await ConvertSelectedToInvoiceAsync());
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("🗑️ İrsaliyeyi Sil", null, async (_, _) => await DeleteSelectedAsync());
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
            Text = "İrsaliyeler yükleniyor..."
        };
        pnlGridBottom.Controls.Add(_lblStatusText);

        pnlCard.Controls.Add(_grid);
        pnlCard.Controls.Add(_pnlActiveFiltersBar);
        pnlCard.Controls.Add(pnlGridBottom);

        _gridManager = new ModernGridManager(_grid, "SatisIrsaliyeForm");
        _gridManager.AttachActiveFiltersBar(_pnlActiveFiltersBar);

        return pnlCard;
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "Fiş No", DataPropertyName = "FisNo", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "E-İrsaliye / Belge No", DataPropertyName = "BelgeNoDisplay", Width = 150 },
            new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "FisTarFormatted", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Sevk Saati", DataPropertyName = "SevkSaatFormatted", Width = 75, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Alıcı / Müşteri Ünvanı", DataPropertyName = "FirmaAdi", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { HeaderText = "Çekici Plaka", DataPropertyName = "Plaka", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Sürücü (Şoför)", DataPropertyName = "SoforTamAdi", Width = 130 },
            new DataGridViewTextBoxColumn { HeaderText = "Palet", DataPropertyName = "ToplamPalet", Width = 70, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { HeaderText = "Kasa", DataPropertyName = "ToplamKasa", Width = 75, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { HeaderText = "Net Miktar (KG)", DataPropertyName = "ToplamMiktar", Width = 115, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Genel Tutar (TL)", DataPropertyName = "GenTut", Width = 120, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Fatura Durumu", DataPropertyName = "DurumDisplay", Width = 145, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } }
        );
    }

    private void FormatGridCells(DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
        if (_grid.Columns[e.ColumnIndex].DataPropertyName == "DurumDisplay")
        {
            var row = _grid.Rows[e.RowIndex].DataBoundItem as IrsaliyeStkDto;
            if (row != null)
            {
                if (row.IsFaturalandi)
                {
                    e.CellStyle.ForeColor = Color.FromArgb(71, 85, 105); // Slate 600
                    e.CellStyle.BackColor = Color.FromArgb(241, 245, 249); // Slate 100
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(21, 128, 61); // Emerald 700
                    e.CellStyle.BackColor = Color.FromArgb(220, 252, 231); // Emerald 100
                    e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                }
            }
        }
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            _lblStatusText.Text = "Veritabanından irsaliyeler çekiliyor...";

            var list = await _service.GetSatisIrsaliyeleriAsync(
                _dtpStart.Value,
                _dtpEnd.Value,
                _txtSearch.Text,
                _cboStatusFilter.SelectedItem?.ToString() ?? "Tüm İrsaliyeler"
            );

            _allData = list;
            FilterData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"İrsaliyeler yüklenirken hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblStatusText.Text = $"Hata: {ex.Message}";
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void FilterData()
    {
        var filtered = _allData.AsEnumerable();

        string search = _txtSearch.Text.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(x =>
                x.FisNo.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.BelgeNoDisplay.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.FirmaAdi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Plaka.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.SoforTamAdi.Contains(search, StringComparison.OrdinalIgnoreCase)
            );
        }

        string statusFilter = _cboStatusFilter.SelectedItem?.ToString() ?? "Tüm İrsaliyeler";
        if (statusFilter == "Açık İrsaliyeler")
        {
            filtered = filtered.Where(x => !x.IsFaturalandi);
        }
        else if (statusFilter == "Faturalananlar")
        {
            filtered = filtered.Where(x => x.IsFaturalandi);
        }

        var list = filtered.ToList();
        _grid.DataSource = list;

        // Update KPIs
        _lblTotalCount.Text = $"{list.Count:N0} Adet";
        double totalKg = list.Sum(x => x.ToplamMiktar);
        _lblTotalWeight.Text = totalKg >= 1000 ? $"{(totalKg / 1000):N1} Ton" : $"{totalKg:N0} KG";
        int openCount = list.Count(x => !x.IsFaturalandi);
        _lblOpenCount.Text = $"{openCount:N0} Adet";
        double totalAmt = list.Sum(x => x.GenTut);
        _lblTotalAmount.Text = $"{totalAmt:N2} TL";

        _lblStatusText.Text = $"Toplam {list.Count:N0} satış irsaliyesi listelendi. ({openCount:N0} açık irsaliye fatura bekliyor)";
    }

    private IrsaliyeStkDto? GetSelectedIrsaliye()
    {
        if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0) return null;
        return _grid.CurrentRow.DataBoundItem as IrsaliyeStkDto;
    }

    private async Task OpenCreateNewAsync()
    {
        using var dlg = new SatisIrsaliyeEditDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await RefreshDataAsync();
        }
    }

    private async Task OpenSelectedForEditAsync()
    {
        var sel = GetSelectedIrsaliye();
        if (sel == null) return;

        using var dlg = new SatisIrsaliyeEditDialog(sel);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await RefreshDataAsync();
        }
    }

    private async Task PrintSelectedAsync()
    {
        var sel = GetSelectedIrsaliye();
        if (sel == null)
        {
            MessageBox.Show("Lütfen yazdırmak istediğiniz irsaliyeyi seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var lines = await _service.GetIrsaliyeDetayAsync(sel.FisNo);
            var doc = SatisIrsaliyeReportRenderer.CreatePrintDocument(sel, lines);
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

    private async Task ConvertSelectedToInvoiceAsync()
    {
        var sel = GetSelectedIrsaliye();
        if (sel == null)
        {
            MessageBox.Show("Lütfen faturaya dönüştürmek istediğiniz irsaliyeyi seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (sel.IsFaturalandi)
        {
            MessageBox.Show($"Bu irsaliye zaten faturalandırılmıştır!\nBağlı Fatura No: #{sel.FatNo:N0}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var prompt = new Form
        {
            Text = "İrsaliyeyi Faturalandır",
            Size = new Size(420, 210),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };

        var lbl = new Label
        {
            Text = $"'{sel.BelgeNoDisplay}' no'lu irsaliye ({sel.FirmaAdi})\niçin kesilen Fatura Numarasını giriniz:",
            Location = new Point(20, 16),
            Size = new Size(360, 40),
            Font = new Font("Segoe UI", 9.5f)
        };

        var txtFatNo = new TextBox
        {
            Location = new Point(20, 65),
            Size = new Size(360, 28),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
        };

        var btnOk = new Button
        {
            Text = "✓ Faturalandır ve Kapat",
            DialogResult = DialogResult.OK,
            Location = new Point(190, 115),
            Size = new Size(190, 36),
            BackColor = ModernColors.Emerald500,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        btnOk.FlatAppearance.BorderSize = 0;

        var btnIptal = new Button
        {
            Text = "Vazgeç",
            DialogResult = DialogResult.Cancel,
            Location = new Point(90, 115),
            Size = new Size(90, 36),
            BackColor = ModernColors.Slate200,
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat
        };
        btnIptal.FlatAppearance.BorderSize = 0;

        prompt.Controls.AddRange(new Control[] { lbl, txtFatNo, btnOk, btnIptal });
        prompt.AcceptButton = btnOk;

        if (prompt.ShowDialog(this) == DialogResult.OK)
        {
            if (double.TryParse(txtFatNo.Text.Trim(), out double fatNoVal) && fatNoVal > 0)
            {
                await _service.FaturayaDonusturAsync(sel.FisNo, fatNoVal);
                NotificationService.NotifySuccess(
                    "İrsaliye Faturalandı",
                    $"{sel.BelgeNoDisplay} no'lu irsaliye #{fatNoVal:N0} no'lu faturaya başarıyla bağlandı.",
                    "SatisIrsaliye");
                await RefreshDataAsync();
            }
            else
            {
                MessageBox.Show("Geçerli bir fatura numarası girilmedi.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var sel = GetSelectedIrsaliye();
        if (sel == null) return;

        if (sel.IsFaturalandi)
        {
            MessageBox.Show("Faturalandırılmış bir irsaliye silinemez! Önce faturasını iptal ediniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var res = MessageBox.Show(
            $"'{sel.BelgeNoDisplay}' ({sel.FirmaAdi}) sevk irsaliyesini silmek istediğinize emin misiniz?",
            "Silme Onayı",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (res == DialogResult.Yes)
        {
            await _service.DeleteIrsaliyeAsync(sel.SiraNo, sel.FisNo);
            NotificationService.NotifyWarning(
                "İrsaliye Silindi",
                $"{sel.BelgeNoDisplay} no'lu irsaliye başarıyla silindi.",
                "SatisIrsaliye");
            await RefreshDataAsync();
        }
    }
}
