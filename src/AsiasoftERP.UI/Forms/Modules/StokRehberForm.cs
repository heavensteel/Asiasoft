using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class StokRehberForm : Form
{
    private readonly IrsaliyeService _service = new();
    private TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private System.Windows.Forms.Timer _searchDebounceTimer = null!;

    public StokKartDto? SelectedStok { get; private set; }

    public StokRehberForm(string initialSearch = "")
    {
        InitializeComponents();
        _txtSearch.Text = initialSearch;
        this.Load += async (_, _) => await LoadDataAsync(_txtSearch.Text);
    }

    private void InitializeComponents()
    {
        this.Text = "Ürün / Stok Rehberi (Stok Seçimi)";
        this.Size = new Size(950, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(244, 246, 250);
        this.KeyPreview = true;
        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) this.Close();
            if (e.KeyCode == Keys.Enter && _grid.CurrentRow?.DataBoundItem is StokKartDto dto)
            {
                SelectedStok = dto;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        };

        // Header Search Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = Color.White,
            Padding = new Padding(15, 12, 15, 12)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(220, 225, 235), 1);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblSearch = new Label
        {
            Text = "Ürün Arama (Stok Kodu, Ürün Adı, Grup):",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 70, 85),
            Location = new Point(15, 10),
            AutoSize = true
        };

        _txtSearch = new TextBox
        {
            Location = new Point(15, 34),
            Size = new Size(450, 29),
            Font = new Font("Segoe UI", 10.5f),
            PlaceholderText = "Aramak istediğiniz ürün veya ambalaj adını yazın..."
        };

        _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _searchDebounceTimer.Tick += async (_, _) =>
        {
            _searchDebounceTimer.Stop();
            await LoadDataAsync(_txtSearch.Text);
        };
        _txtSearch.TextChanged += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        };

        var btnSelect = new Button
        {
            Text = "✓ Seç ve Aktar (ENTER)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Emerald500,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 32),
            Location = new Point(480, 32),
            Cursor = Cursors.Hand
        };
        btnSelect.FlatAppearance.BorderSize = 0;
        btnSelect.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is StokKartDto dto)
            {
                SelectedStok = dto;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        };

        pnlHeader.Controls.AddRange(new Control[] { lblSearch, _txtSearch, btnSelect });

        // Center Grid
        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 12, 15, 10)
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        ModernUiHelper.ApplyModernGridStyle(_grid);

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colKod", DataPropertyName = "Kodu", HeaderText = "Stok Kodu", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colAdi", DataPropertyName = "Adi", HeaderText = "Stok / Ürün Adı", Width = 340 },
            new DataGridViewTextBoxColumn { Name = "colGrup", DataPropertyName = "GrupKoduAdi", HeaderText = "Ürün Grubu", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "colBirim", DataPropertyName = "Birim", HeaderText = "Birim", Width = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colMevcut", DataPropertyName = "Mevcut", HeaderText = "Mevcut Stok", Width = 110, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colFiyat", DataPropertyName = "SFiyat1", HeaderText = "Satış Fiyatı", Width = 110, DefaultCellStyle = { Format = "N4", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colKdv", DataPropertyName = "Kdv", HeaderText = "KDV %", Width = 70, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );

        _grid.CellDoubleClick += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is StokKartDto dto)
            {
                SelectedStok = dto;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        };

        pnlGrid.Controls.Add(_grid);

        // Status Strip
        var statusStrip = new StatusStrip { BackColor = Color.White };
        _lblStatus = new ToolStripStatusLabel { Text = "Ürünler yükleniyor..." };
        statusStrip.Items.Add(_lblStatus);

        this.Controls.Add(pnlGrid);
        this.Controls.Add(statusStrip);
        this.Controls.Add(pnlHeader);
    }

    private async Task LoadDataAsync(string query)
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            var list = await _service.SearchStokKartlariAsync(query);
            _grid.DataSource = list;
            _lblStatus.Text = $"{list.Count} adet ürün listelendi. Seçmek için çift tıklayın veya Enter'a basın.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Hata: {ex.Message}";
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }
}
