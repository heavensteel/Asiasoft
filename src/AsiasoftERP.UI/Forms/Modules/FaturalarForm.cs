using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class FaturalarForm : BaseListForm
{
    private readonly FaturaTip _tip;
    private readonly FaturaService _service = new();

    protected override string ModuleName => "Fatura";
    protected override string FormTitle => _tip switch
    {
        FaturaTip.AlisFaturasi => "📥  Alış Faturaları (Canlı)",
        FaturaTip.SatisFaturasi => "📄  Satış Faturaları (Canlı)",
        FaturaTip.IhracatFaturasi => "🌐  İhracat Faturaları (Canlı)",
        _ => "🧾  Faturalar"
    };

    private List<FaturaStkDto> _allData = new();
    private DateTimePicker _dtpBaslangic = null!, _dtpBitis = null!;
    private Button _btnFiltrele = null!;

    public FaturalarForm(FaturaTip tip)
    {
        _tip = tip;
        this.Name = $"FaturalarForm_{tip}";
        SetupColumns();
        AddDateFilter();
    }

    private void AddDateFilter()
    {
        var filterBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 6, 12, 6)
        };
        filterBar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, filterBar.Height - 1, filterBar.Width, filterBar.Height - 1);
        };

        var lblStart = new Label { Text = "Başlangıç:", Location = new Point(12, 12), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = ModernColors.Slate700 };
        _dtpBaslangic = new DateTimePicker
        {
            Location = new Point(88, 8),
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(-90)
        };

        var lblEnd = new Label { Text = "Bitiş:", Location = new Point(226, 12), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = ModernColors.Slate700 };
        _dtpBitis = new DateTimePicker
        {
            Location = new Point(268, 8),
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };

        _btnFiltrele = new Button
        {
            Text = "Tarihleri Filtrele",
            Location = new Point(408, 7),
            Width = 125,
            Height = 30,
            BackColor = ModernColors.Teal600,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnFiltrele.FlatAppearance.BorderSize = 0;
        _btnFiltrele.Click += async (s, e) => await LoadDataAsync();

        var lblHint = new Label
        {
            Text = "💡 Faturaya çift tıklayarak kalem detaylarını inceleyebilirsiniz.",
            Location = new Point(560, 12),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate500
        };

        filterBar.Controls.AddRange(new Control[] { lblStart, _dtpBaslangic, lblEnd, _dtpBitis, _btnFiltrele, lblHint });
        this.Controls.Add(filterBar);
    }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "FisNo", HeaderText = "Fiş No", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colGibNo", DataPropertyName = "GibFaturaNo", HeaderText = "E-Fatura / Belge No", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "FisTar", HeaderText = "Tarih", Width = 90, DefaultCellStyle = { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colVade", DataPropertyName = "VadeTar", HeaderText = "Vade", Width = 90, DefaultCellStyle = { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colCari", DataPropertyName = "FirmaAdi", HeaderText = "Cari / Müşteri Ünvanı", Width = 280 },
            new DataGridViewTextBoxColumn { Name = "colTip", DataPropertyName = "Tip", HeaderText = "Fatura Türü", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colFisTutar", DataPropertyName = "FisTut", HeaderText = "Tutar (TL)", Width = 115, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colIskonto", DataPropertyName = "IskTut", HeaderText = "İskonto", Width = 90, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colKdv", DataPropertyName = "KdvTut", HeaderText = "KDV", Width = 95, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colGenel", DataPropertyName = "GenTut", HeaderText = "Genel Toplam (TL)", Width = 135, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colDoviz", DataPropertyName = "DovizTuru", HeaderText = "Döviz", Width = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colDovizTutar", DataPropertyName = "DovizTutar", HeaderText = "Döviz Tutar", Width = 115, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            var start = _dtpBaslangic?.Value ?? DateTime.Today.AddDays(-90);
            var end = _dtpBitis?.Value ?? DateTime.Today;

            var data = await _service.GetFaturalarAsync(_tip, start, end, TxtSearch?.Text ?? "");
            _allData = data;
            Grid.DataSource = data;

            UpdateRecordCount(data.Count);
            ShowFooterTotals(data);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Faturalar yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void ShowFooterTotals(List<FaturaStkDto> data)
    {
        var totalGenel = data.Sum(f => f.GenTut);
        var totalDoviz = data.Where(f => f.DovizTuru != "TL").Sum(f => f.DovizTutar);
        LblRecordCount.Text = $"Toplam: {data.Count:N0} fatura  |  Genel Toplam: {totalGenel:N2} TL" +
            (totalDoviz > 0 ? $"  |  Döviz Toplamı: {totalDoviz:N2}" : "");
    }

    protected override void OnSearchChanged(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Grid.DataSource = _allData;
            UpdateRecordCount(_allData.Count);
            ShowFooterTotals(_allData);
            return;
        }

        var filtered = _allData.Where(f =>
            f.FisNo.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            f.GibFaturaNo.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            f.FirmaAdi.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        Grid.DataSource = filtered;
        UpdateRecordCount(filtered.Count);
        ShowFooterTotals(filtered);
    }

    protected override void OnRowDoubleClick(int rowIndex) => OnEditRecord();

    protected override void OnEditRecord()
    {
        if (Grid.SelectedRows.Count == 0) return;
        var fatura = Grid.SelectedRows[0].DataBoundItem as FaturaStkDto;
        if (fatura == null) return;

        using var dialog = new FaturaDetailDialog(fatura);
        dialog.ShowDialog(this);
    }
}
