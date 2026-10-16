using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class CariHesaplarForm : BaseListForm
{
    private readonly MacroService _macroService = new();
    protected override string ModuleName => "CariHesap";
    protected override string FormTitle => "👥 MacroSoft Cari Hesap Kartları";

    private Button _btnEkstre = null!;

    public CariHesaplarForm()
    {
        this.Name = "CariHesaplarForm";
        SetupColumns();
        AddCustomButtons();
    }

    private void AddCustomButtons()
    {
        _btnEkstre = new Button
        {
            Text = "⚡ Cari Ekstre Aç",
            Width = 135,
            Height = 30,
            Location = new Point(560, 6),
            BackColor = Color.FromArgb(24, 130, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnEkstre.FlatAppearance.BorderSize = 0;
        _btnEkstre.Click += (_, _) => OpenSelectedEkstre();

        if (ToolbarPanel != null)
        {
            ToolbarPanel.Controls.Add(_btnEkstre);
        }
    }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colFirKod", DataPropertyName = "FirKod", HeaderText = "Cari No", Width = 75,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colFirmaAdi", DataPropertyName = "FirmaAdi", HeaderText = "Cari Adı / Firma", Width = 280 },
            new DataGridViewTextBoxColumn { Name = "colUnvan", DataPropertyName = "Unvan", HeaderText = "Ticari Ünvan", Width = 200 },
            new DataGridViewTextBoxColumn { Name = "colVergiNo", DataPropertyName = "VergiNo", HeaderText = "Vergi / TC No", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colVergiDairesi", DataPropertyName = "VergiDairesi", HeaderText = "Vergi Dairesi", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colTelefon", DataPropertyName = "Telefon", HeaderText = "Telefon", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colSehir", DataPropertyName = "Sehir", HeaderText = "Şehir", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colBorc", DataPropertyName = "Borc", HeaderText = "Borç (TL)", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colAlacak", DataPropertyName = "Alacak", HeaderText = "Alacak (TL)", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colBakiye", DataPropertyName = "Bakiye", HeaderText = "Bakiye (TL)", Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            string search = TxtSearch?.Text?.Trim() ?? "";
            var (items, total) = await _macroService.GetCarilerPagedAsync(search, 0, 200);
            Grid.DataSource = items;
            UpdateRecordCount(total);
            LblRecordCount.Text = $"Listelenen: {items.Count:N0}  |  Toplam Veritabanı: {total:N0} Cari Kaydı";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override async void OnSearchChanged(string searchText)
    {
        await LoadDataAsync();
    }

    protected override void OnRowDoubleClick(int rowIndex)
    {
        OpenSelectedEkstre();
    }

    private void OpenSelectedEkstre()
    {
        if (Grid.CurrentRow?.DataBoundItem is CariListItemDto item)
        {
            var ekstreForm = new CariHesapEkstresiForm(item.FirKod);
            if (this.MdiParent is MainForm main)
            {
                ekstreForm.MdiParent = main;
                ekstreForm.StartPosition = FormStartPosition.CenterScreen;
                ekstreForm.Show();
            }
            else
            {
                ekstreForm.ShowDialog();
            }
        }
        else
        {
            MessageBox.Show("Lütfen önce bir cari hesap seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
