using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;

namespace AsiasoftERP.UI.Forms.Modules;

public class CariRehberForm : Form
{
    private readonly CariEkstreService _service = new();
    private TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private System.Windows.Forms.Timer _searchDebounceTimer = null!;

    public CariLookupDto? SelectedCari { get; private set; }

    public CariRehberForm(string initialSearch = "")
    {
        InitializeComponents();
        _txtSearch.Text = initialSearch;
        this.Load += async (_, _) => await LoadDataAsync(_txtSearch.Text);
    }

    private void InitializeComponents()
    {
        this.Text = "Cari Hesap Rehberi (Firma / Müşteri / Üretici Seçimi)";
        this.Size = new Size(950, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(244, 246, 250);
        this.KeyPreview = true;
        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) this.Close();
            if (e.KeyCode == Keys.Enter && _grid.CurrentRow?.DataBoundItem is CariLookupDto dto)
            {
                SelectedCari = dto;
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

        var lblSearch = new Label
        {
            Text = "Cari Arama (Kod, Ünvan, Vergi No, Şehir):",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 70, 85),
            Location = new Point(15, 10),
            AutoSize = true
        };

        _txtSearch = new TextBox
        {
            Location = new Point(15, 33),
            Width = 450,
            Font = new Font("Segoe UI", 11f),
            PlaceholderText = "Aramak istediğiniz cari adını veya kodunu yazın..."
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

        var btnSec = new Button
        {
            Text = "✔  Seç (Enter)",
            Location = new Point(480, 31),
            Size = new Size(130, 32),
            BackColor = Color.FromArgb(0, 122, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSec.FlatAppearance.BorderSize = 0;
        btnSec.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is CariLookupDto dto)
            {
                SelectedCari = dto;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        };

        var btnKapat = new Button
        {
            Text = "İptal (Esc)",
            Location = new Point(620, 31),
            Size = new Size(100, 32),
            BackColor = Color.FromArgb(220, 224, 230),
            ForeColor = Color.FromArgb(40, 50, 60),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        btnKapat.FlatAppearance.BorderSize = 0;
        btnKapat.Click += (_, _) => this.Close();

        pnlHeader.Controls.AddRange(new Control[] { lblSearch, _txtSearch, btnSec, btnKapat });

        // Status Strip
        var statusStrip = new StatusStrip { BackColor = Color.White };
        _lblStatus = new ToolStripStatusLabel { Text = "Hazır", ForeColor = Color.FromArgb(100, 110, 125) };
        statusStrip.Items.Add(_lblStatus);

        // DataGridView
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 32 }
        };

        // Enable Double-buffering to eliminate flickering
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, _grid, new object[] { true });

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(238, 242, 248),
            ForeColor = Color.FromArgb(45, 55, 72),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };
        _grid.ColumnHeadersHeight = 36;

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = Color.FromArgb(220, 235, 252),
            SelectionForeColor = Color.Black,
            Font = new Font("Segoe UI", 9.5f),
            Padding = new Padding(6, 0, 6, 0)
        };

        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 250, 252)
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FirKod", HeaderText = "Cari Kodu", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FirmaAdi", HeaderText = "Cari Ünvanı", Width = 340 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VergiDairesi", HeaderText = "V. Dairesi", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VergiNo", HeaderText = "Vergi / TC No", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Telefon", HeaderText = "Telefon", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Sehir", HeaderText = "Şehir", Width = 90 });

        var colBakiye = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Bakiye",
            HeaderText = "Güncel Bakiye",
            Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "#,##0.00 TL"
            }
        };
        _grid.Columns.Add(colBakiye);

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BakiyeDurumu",
            HeaderText = "B/A",
            Width = 45,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        _grid.CellFormatting += (s, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is CariLookupDto item)
            {
                if (e.ColumnIndex == colBakiye.Index)
                {
                    e.CellStyle.ForeColor = item.Bakiye > 0 ? Color.FromArgb(180, 20, 20) : item.Bakiye < 0 ? Color.FromArgb(20, 130, 40) : Color.Black;
                }
            }
        };

        _grid.CellDoubleClick += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is CariLookupDto dto)
            {
                SelectedCari = dto;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        };

        this.Controls.Add(_grid);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(statusStrip);
    }

    private async Task LoadDataAsync(string query)
    {
        _lblStatus.Text = "Aranıyor...";
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var list = await _service.SearchCarilerAsync(query, 75);
            sw.Stop();

            _grid.DataSource = list;
            _lblStatus.Text = $"{list.Count} kayıt bulundu ({sw.ElapsedMilliseconds} ms) — Seçmek için çift tıklayın veya Enter'a basın";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Hata: {ex.Message}";
        }
    }
}
