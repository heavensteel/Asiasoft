using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class FaturaDetailDialog : Form
{
    private readonly FaturaStkDto _fatura;
    private readonly FaturaService _service = new();
    private DataGridView _grid = null!;
    private Label _lblTotals = null!;

    public FaturaDetailDialog(FaturaStkDto fatura)
    {
        _fatura = fatura;
        InitUI();
        this.Load += async (_, _) => await LoadDetailsAsync();
    }

    private void InitUI()
    {
        this.Text = $"Fatura Detayı - {_fatura.BelgeNoDisplay} - {_fatura.FirmaAdi}";
        this.Size = new Size(1100, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        // Top Info Header Card
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 125,
            BackColor = Color.White,
            Padding = new Padding(20, 14, 20, 14)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = $"🧾  FATURA: {_fatura.BelgeNoDisplay}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(18, 12),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = $"Cari: {_fatura.FirKod} - {_fatura.FirmaAdi}  |  Tarih: {_fatura.FisTar:dd.MM.yyyy}  |  Vade: {_fatura.VadeTar:dd.MM.yyyy}  |  Tür: {_fatura.Tip}",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ModernColors.Slate600,
            Location = new Point(20, 38),
            AutoSize = true
        };

        // KPI Badges Panel on Right
        var pnlKpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 520,
            Height = 85,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };

        AddHeaderPill(pnlKpis, "Fatura Tutarı", $"{_fatura.FisTut:N2} {_fatura.DovizTuru}", ModernColors.Slate700);
        AddHeaderPill(pnlKpis, "KDV Tutarı", $"{_fatura.KdvTut:N2} {_fatura.DovizTuru}", ModernColors.Amber500);
        AddHeaderPill(pnlKpis, "Genel Toplam", $"{_fatura.GenTut:N2} {_fatura.DovizTuru}", ModernColors.Emerald500, isBold: true);
        if (_fatura.DovizTutar > 0 && _fatura.DovizTuru != "TL")
        {
            AddHeaderPill(pnlKpis, "Döviz Toplamı", $"{_fatura.DovizTutar:N2} {_fatura.DovizTuru}", ModernColors.Indigo600);
        }

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, pnlKpis });

        // Center Grid
        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 12, 15, 8),
            BackColor = ModernColors.Slate100
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true
        };
        ModernUiHelper.ApplyModernGridStyle(_grid);
        SetupGridColumns();
        pnlGridContainer.Controls.Add(_grid);

        // Bottom Footer Bar
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.White,
            Padding = new Padding(15, 8, 15, 8)
        };
        pnlFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        _lblTotals = new Label
        {
            Text = "Kalemler yükleniyor...",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        var btnClose = new Button
        {
            Text = "Kapat (ESC)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate100,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 36),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => this.Close();

        pnlFooter.Controls.AddRange(new Control[] { _lblTotals, btnClose });

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);

        this.KeyPreview = true;
        this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };
    }

    private void AddHeaderPill(Panel parent, string title, string val, Color col, bool isBold = false)
    {
        var pnl = new Panel
        {
            Size = new Size(122, 60),
            Margin = new Padding(4),
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle
        };
        var lblT = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate500,
            Location = new Point(6, 6),
            AutoSize = true
        };
        var lblV = new Label
        {
            Text = val,
            Font = new Font("Segoe UI", isBold ? 9.5f : 9f, FontStyle.Bold),
            ForeColor = col,
            Location = new Point(6, 26),
            AutoSize = true
        };
        pnl.Controls.AddRange(new Control[] { lblT, lblV });
        parent.Controls.Add(pnl);
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { DataPropertyName = "Sira", HeaderText = "#", Width = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { DataPropertyName = "MalKodu", HeaderText = "Ürün Kodu", Width = 110 },
            new DataGridViewTextBoxColumn { DataPropertyName = "MalAdi", HeaderText = "Ürün / Kalem Açıklaması", Width = 310 },
            new DataGridViewTextBoxColumn { DataPropertyName = "Miktar", HeaderText = "Miktar", Width = 95, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { DataPropertyName = "Birimi", HeaderText = "Birim", Width = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { DataPropertyName = "Fiyati", HeaderText = "Birim Fiyat", Width = 100, DefaultCellStyle = { Format = "N4", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { DataPropertyName = "Kdv", HeaderText = "KDV %", Width = 60, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { DataPropertyName = "KdvTut", HeaderText = "KDV Tutar", Width = 95, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { DataPropertyName = "Tutar", HeaderText = "Toplam (TL)", Width = 115, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { DataPropertyName = "DovizTutar", HeaderText = "Döviz Tutar", Width = 105, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    private async Task LoadDetailsAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            var details = await _service.GetFaturaDetayAsync(_fatura.FisNo);
            _grid.DataSource = details;

            double totalQty = 0;
            double totalLineAmt = 0;
            foreach (var d in details)
            {
                totalQty += d.Miktar;
                totalLineAmt += d.Tutar;
            }

            _lblTotals.Text = $"Toplam Kalem: {details.Count} adet  |  Toplam Miktar: {totalQty:N2}  |  Kalemler Toplamı: {totalLineAmt:N2} TL";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kalemler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }
}
