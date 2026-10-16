using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using Dapper;

namespace AsiasoftERP.UI.Forms.Modules;

public class StokListForm : BaseListForm
{
    protected override string ModuleName => "Stok";
    protected override string FormTitle => "📦  Stok ve Ürün Kartları (Canlı)";
    private List<StokKartDto> _allData = new();

    public StokListForm()
    {
        this.Name = "StokListForm";
        SetupColumns();
    }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colKod", DataPropertyName = "Kodu", HeaderText = "Stok Kodu", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colAdi", DataPropertyName = "Adi", HeaderText = "Stok / Ürün / Ambalaj Adı", Width = 350 },
            new DataGridViewTextBoxColumn { Name = "colGrup", DataPropertyName = "GrupKoduAdi", HeaderText = "Grup", Width = 140 },
            new DataGridViewTextBoxColumn { Name = "colBirim", DataPropertyName = "Birim", HeaderText = "Birim", Width = 65, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colMevcut", DataPropertyName = "Mevcut", HeaderText = "Mevcut Miktar", Width = 115, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colAlis", DataPropertyName = "AlisFiyat", HeaderText = "Alış Fiyatı", Width = 100, DefaultCellStyle = { Format = "N4", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colSatis", DataPropertyName = "SFiyat1", HeaderText = "Satış Fiyatı", Width = 100, DefaultCellStyle = { Format = "N4", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colKdv", DataPropertyName = "Kdv", HeaderText = "KDV %", Width = 65, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    SIRA as Sira,
                    COALESCE(KODU, '') as Kodu,
                    COALESCE(ADI, '') as Adi,
                    COALESCE(GRUPKODUADI, '') as GrupKoduAdi,
                    COALESCE(BIRIM, 'KG') as Birim,
                    COALESCE(MEVCUT, 0) as Mevcut,
                    COALESCE(ALISFIYAT, 0) as AlisFiyat,
                    COALESCE(SFIYAT1, 0) as SFiyat1,
                    COALESCE(KDV, 0) as Kdv
                FROM stok
                WHERE SIRKOD = '1'
                ORDER BY ADI ASC;";

            var data = (await conn.QueryAsync<StokKartDto>(sql)).ToList();
            _allData = data;
            Grid.DataSource = data;

            UpdateRecordCount(data.Count);
            ShowFooterTotals(data);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Stok kartları yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void ShowFooterTotals(List<StokKartDto> data)
    {
        var totalMevcut = data.Sum(s => s.Mevcut);
        LblRecordCount.Text = $"Toplam: {data.Count:N0} stok kartı  |  Toplam Depo Miktarı: {totalMevcut:N2}";
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

        var filtered = _allData.Where(s =>
            s.Kodu.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            s.Adi.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            s.GrupKoduAdi.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        Grid.DataSource = filtered;
        UpdateRecordCount(filtered.Count);
        ShowFooterTotals(filtered);
    }
}
