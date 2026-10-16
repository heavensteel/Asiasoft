using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Data.Services;
using AsiasoftERP.Core.Models;
using AsiasoftERP.UI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AsiasoftERP.UI.Forms.Modules;

// ==================== ÖDEME FORMLARI ====================

public class OdemeTalebiForm : BaseListForm
{
    protected override string ModuleName => "Odeme";
    protected override string FormTitle => "💳  Ödeme Talepleri";
    private List<OdemeTalebi> _data = new();

    public OdemeTalebiForm() { this.Name = "OdemeTalebiForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colId", DataPropertyName = "Id", HeaderText = "#", Width = 50 },
            new DataGridViewTextBoxColumn { Name = "colNo", DataPropertyName = "TalepNo", HeaderText = "Talep No", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TalepTarihi", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colVade", DataPropertyName = "VadeTarihi", HeaderText = "Vade", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colCari", HeaderText = "Cari Adı", Width = 250 },
            new DataGridViewTextBoxColumn { Name = "colTutar", DataPropertyName = "Tutar", HeaderText = "Tutar", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colOdenen", DataPropertyName = "OdenenTutar", HeaderText = "Ödenen", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colBakiye", DataPropertyName = "Bakiye", HeaderText = "Bakiye", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDurum", DataPropertyName = "Durum", HeaderText = "Durum", Width = 90 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var db = DbContextFactory.Create();
            _data = await db.OdemeTalepleri.Include(o => o.CariHesap)
                .OrderByDescending(o => o.TalepTarihi).ToListAsync();
            var display = _data.Select(o => new
            {
                o.Id, o.TalepNo, o.TalepTarihi, o.VadeTarihi,
                CariAdi = o.CariHesap?.CariAdi ?? "",
                o.Tutar, o.OdenenTutar, o.Bakiye, Durum = o.Durum.ToString()
            }).ToList();
            Grid.DataSource = display;
            if (Grid.Columns.Contains("colCari")) Grid.Columns["colCari"]!.DataPropertyName = "CariAdi";
            UpdateRecordCount(_data.Count);
        }
        catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
    }
}

public class OdemePlanForm : BaseListForm
{
    protected override string ModuleName => "Odeme";
    protected override string FormTitle => "💳  Ödeme Planı";
    public OdemePlanForm() { this.Name = "OdemePlanForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colId", DataPropertyName = "Id", HeaderText = "#", Width = 50 },
            new DataGridViewTextBoxColumn { Name = "colNo", DataPropertyName = "TalepNo", HeaderText = "Talep No", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colVade", DataPropertyName = "VadeTarihi", HeaderText = "Vade Tarihi", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colCari", HeaderText = "Cari Adı", Width = 250 },
            new DataGridViewTextBoxColumn { Name = "colTutar", DataPropertyName = "Tutar", HeaderText = "Tutar", Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colBakiye", DataPropertyName = "Bakiye", HeaderText = "Bakiye", Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDoviz", DataPropertyName = "DovizTipi", HeaderText = "Döviz", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "colYontem", DataPropertyName = "OdemeYontemi", HeaderText = "Yöntem", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "colDurum", DataPropertyName = "Durum", HeaderText = "Durum", Width = 90 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var db = DbContextFactory.Create();
            var data = await db.OdemeTalepleri.Include(o => o.CariHesap)
                .Where(o => o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.Onaylandi)
                .OrderBy(o => o.VadeTarihi).ToListAsync();
            var display = data.Select(o => new
            {
                o.Id, o.TalepNo, o.VadeTarihi,
                CariAdi = o.CariHesap?.CariAdi ?? "",
                o.Tutar, o.Bakiye, o.DovizTipi, o.OdemeYontemi, Durum = o.Durum.ToString()
            }).ToList();
            Grid.DataSource = display;
            if (Grid.Columns.Contains("colCari")) Grid.Columns["colCari"]!.DataPropertyName = "CariAdi";
            var totalBakiye = data.Sum(o => o.Bakiye);
            LblRecordCount.Text = $"Toplam: {data.Count:N0} kayıt  |  Toplam Bakiye: {totalBakiye:N2} TL";
        }
        catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
    }
}

public class OdemeMakbuzForm : BaseListForm
{
    private readonly MakbuzTip _tip;
    protected override string ModuleName => "Odeme";
    protected override string FormTitle => _tip switch
    {
        MakbuzTip.OdemeMakbuzu => "💳  Ödeme Makbuzları",
        MakbuzTip.TahsilatMakbuzu => "💳  Tahsilat Makbuzları",
        MakbuzTip.MustahsilMakbuzu => "🌾  Müstahsil Makbuzları",
        MakbuzTip.SmmMakbuzu => "📄  SMM Makbuzları",
        _ => "💳  Makbuzlar"
    };

    public OdemeMakbuzForm(MakbuzTip tip)
    {
        _tip = tip; this.Name = $"OdemeMakbuzForm_{tip}"; SetupColumns();
    }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colId", DataPropertyName = "Id", HeaderText = "#", Width = 50 },
            new DataGridViewTextBoxColumn { Name = "colNo", DataPropertyName = "MakbuzNo", HeaderText = "Makbuz No", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "MakbuzTarihi", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colCari", HeaderText = "Cari Adı", Width = 250 },
            new DataGridViewTextBoxColumn { Name = "colTutar", DataPropertyName = "Tutar", HeaderText = "Tutar", Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDoviz", DataPropertyName = "DovizTipi", HeaderText = "Döviz", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "colYontem", DataPropertyName = "OdemeYontemi", HeaderText = "Yöntem", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "colBanka", DataPropertyName = "BankaAdi", HeaderText = "Banka", Width = 120 },
            new DataGridViewTextBoxColumn { Name = "colAciklama", DataPropertyName = "Aciklama", HeaderText = "Açıklama", Width = 200 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var db = DbContextFactory.Create();
            var data = await db.OdemeMakbuzlar.Include(m => m.CariHesap)
                .Where(m => m.MakbuzTipi == _tip)
                .OrderByDescending(m => m.MakbuzTarihi).ToListAsync();
            var display = data.Select(m => new
            {
                m.Id, m.MakbuzNo, m.MakbuzTarihi,
                CariAdi = m.CariHesap?.CariAdi ?? "",
                m.Tutar, m.DovizTipi, m.OdemeYontemi, m.BankaAdi, m.Aciklama
            }).ToList();
            Grid.DataSource = display;
            if (Grid.Columns.Contains("colCari")) Grid.Columns["colCari"]!.DataPropertyName = "CariAdi";
            var total = data.Sum(m => m.Tutar);
            LblRecordCount.Text = $"Toplam: {data.Count:N0} kayıt  |  Toplam: {total:N2} TL";
        }
        catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
    }
}

// ==================== BANKA FORMLARI ====================

public class BankaHesapForm : BaseListForm
{
    protected override string ModuleName => "Banka";
    protected override string FormTitle => "🏦  Banka Hesapları (Canlı)";

    public BankaHesapForm() { this.Name = "BankaHesapForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colKod", DataPropertyName = "KODU", HeaderText = "Hesap Kodu", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colBanka", DataPropertyName = "ADI", HeaderText = "Banka Adı", Width = 230, DefaultCellStyle = { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colSube", DataPropertyName = "SUBEADI", HeaderText = "Şube", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "colHesap", DataPropertyName = "HESAPNO", HeaderText = "Hesap No", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "colIban", DataPropertyName = "IBAN", HeaderText = "IBAN", Width = 260 },
            new DataGridViewTextBoxColumn { Name = "colDoviz", DataPropertyName = "DOVIZCINSI", HeaderText = "Döviz", Width = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colSekil", DataPropertyName = "CALISMASEKLI", HeaderText = "Çalışma Şekli", Width = 110 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    KODU,
                    COALESCE(ADI, '') as ADI,
                    COALESCE(SUBEADI, '') as SUBEADI,
                    COALESCE(HESAPNO, '') as HESAPNO,
                    COALESCE(IBAN, '') as IBAN,
                    COALESCE(DOVIZCINSI, 'TL') as DOVIZCINSI,
                    COALESCE(CALISMASEKLI, 'TL') as CALISMASEKLI
                FROM banka
                WHERE SIRKOD = '1'
                ORDER BY ADI ASC;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql)).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex) 
        { 
            MessageBox.Show($"Banka hesapları yüklenirken hata: {ex.Message}"); 
        }
    }
}

public class BankaFisForm : BaseListForm
{
    protected override string ModuleName => "Banka";
    protected override string FormTitle => "🏦  Banka İşlem Fişleri";
    public BankaFisForm() { this.Name = "BankaFisForm"; SetupBankaColumns(); }
    private void SetupBankaColumns() => SetupCommonBankaColumns("Tüm İşlemler");

    protected void SetupCommonBankaColumns(string tip)
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colId", DataPropertyName = "Id", HeaderText = "#", Width = 50 },
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "FisNo", HeaderText = "Fiş No", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "FisTarihi", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colBanka", HeaderText = "Banka", Width = 160 },
            new DataGridViewTextBoxColumn { Name = "colTip", DataPropertyName = "FisTip", HeaderText = "İşlem Tipi", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "colTutar", DataPropertyName = "Tutar", HeaderText = "Tutar", Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDoviz", DataPropertyName = "DovizTipi", HeaderText = "Döviz", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "colGonderenAlici", DataPropertyName = "GonderenAlici", HeaderText = "Gönderen / Alıcı", Width = 180 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var db = DbContextFactory.Create();
            var data = await db.BankaFisler.Include(b => b.BankaHesap)
                .OrderByDescending(b => b.FisTarihi).Take(1000).ToListAsync();
            var display = data.Select(b => new
            {
                b.Id, b.FisNo, b.FisTarihi,
                BankaAdi = b.BankaHesap?.BankaAdi ?? "",
                FisTip = b.FisTip.ToString(),
                b.Tutar, b.DovizTipi, b.GonderenAlici
            }).ToList();
            Grid.DataSource = display;
            if (Grid.Columns.Contains("colBanka")) Grid.Columns["colBanka"]!.DataPropertyName = "BankaAdi";
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
    }
}

public class BankaVirmanForm : BankaFisForm
{
    protected override string FormTitle => "🏦  Virman Fişleri";
    public BankaVirmanForm() { this.Name = "BankaVirmanForm"; }
}

public class BankaDovizForm : BankaFisForm
{
    protected override string FormTitle => "🏦  Döviz Fişleri";
    public BankaDovizForm() { this.Name = "BankaDovizForm"; }
}

public class CekListForm : BaseListForm
{
    protected override string ModuleName => "Banka";
    protected override string FormTitle => "📝  Çek & Senet Portföyü (Canlı)";

    public CekListForm() { this.Name = "CekListForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colNo", DataPropertyName = "CEKNO", HeaderText = "Çek No", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colVade", DataPropertyName = "VADETARIHI", HeaderText = "Vade Tarihi", Width = 95, DefaultCellStyle = { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colCari", DataPropertyName = "FIRADI", HeaderText = "Cari / Verilen Firma", Width = 260 },
            new DataGridViewTextBoxColumn { Name = "colBanka", DataPropertyName = "BANKAADI", HeaderText = "Banka Adı", Width = 180 },
            new DataGridViewTextBoxColumn { Name = "colTur", DataPropertyName = "TURADI", HeaderText = "Çek Türü", Width = 120 },
            new DataGridViewTextBoxColumn { Name = "colTutar", DataPropertyName = "TUTAR", HeaderText = "Tutar (TL)", Width = 125, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colDoviz", DataPropertyName = "DOVIZTURU", HeaderText = "Döviz", Width = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colDurum", DataPropertyName = "DURUM", HeaderText = "Durum", Width = 100 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            string searchClause = "";
            if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
            {
                searchClause = " AND (FIRADI LIKE @p OR CEKNO LIKE @p OR BANKAADI LIKE @p)";
            }

            string sql = $@"
                SELECT 
                    COALESCE(CEKNO, '') as CEKNO,
                    VADETARIHI,
                    COALESCE(FIRADI, '') as FIRADI,
                    COALESCE(BANKAADI, '') as BANKAADI,
                    COALESCE(TURADI, 'KENDİ ÇEKİMİZ') as TURADI,
                    COALESCE(TUTAR, 0) as TUTAR,
                    COALESCE(DOVIZTURU, 'TL') as DOVIZTURU,
                    COALESCE(DURUM, 'PORTFÖYDE') as DURUM
                FROM cekdetay
                WHERE SIRKOD = '1' {searchClause}
                ORDER BY VADETARIHI DESC
                LIMIT 500;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql, new { p = $"%{TxtSearch?.Text}%" })).ToList();
            Grid.DataSource = data;

            double total = 0;
            foreach (var d in data) total += (double)(d.TUTAR ?? 0);
            LblRecordCount.Text = $"Toplam: {data.Count:N0} çek kaydı  |  Toplam Tutar: {total:N2} TL";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Çek listesi hatası: {ex.Message}");
        }
    }
}

public class HavaleForm : BaseListForm
{
    private readonly bool _gelenMi;
    protected override string ModuleName => "Banka";
    protected override string FormTitle => _gelenMi ? "💸  Gelen Havaleler" : "💸  Gönderilen Havaleler";

    public HavaleForm(bool gelenMi)
    {
        _gelenMi = gelenMi; this.Name = $"HavaleForm_{(gelenMi ? "Gelen" : "Gonderilen")}"; SetupColumns();
    }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colId", DataPropertyName = "Id", HeaderText = "#", Width = 50 },
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "FisNo", HeaderText = "Fiş No", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "FisTarihi", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colGonderenAlici", DataPropertyName = "GonderenAlici", HeaderText = _gelenMi ? "Gönderen" : "Alıcı", Width = 200 },
            new DataGridViewTextBoxColumn { Name = "colTutar", DataPropertyName = "Tutar", HeaderText = "Tutar", Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDoviz", DataPropertyName = "DovizTipi", HeaderText = "Döviz", Width = 60 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var db = DbContextFactory.Create();
            var tip = _gelenMi ? BankaFisTip.Giris : BankaFisTip.Cikis;
            var data = await db.BankaFisler
                .Where(b => b.FisTip == tip)
                .OrderByDescending(b => b.FisTarihi).ToListAsync();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
    }
}

// ==================== CARİ EKSTRESİ & BAKİYE ====================

public class CariEkstresForm : Form
{
    private ComboBox _cmbCari = null!;
    private DateTimePicker _dtpBaslangic = null!, _dtpBitis = null!;
    private DataGridView _grid = null!;
    private Label _lblBakiye = null!;

    public CariEkstresForm()
    {
        this.Text = "📋  Cari Hesap Ekstresi";
        this.Size = new Size(1000, 650);
        this.Font = new Font("Segoe UI", 9.5f);
        InitUI();
    }

    private void InitUI()
    {
        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(240, 244, 250), Padding = new Padding(8) };
        filterPanel.Controls.Add(new Label { Text = "Cari:", Location = new Point(8, 15), AutoSize = true });
        _cmbCari = new ComboBox { Location = new Point(50, 11), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        _dtpBaslangic = new DateTimePicker { Location = new Point(380, 11), Width = 120, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, 1, 1) };
        _dtpBitis = new DateTimePicker { Location = new Point(520, 11), Width = 120, Format = DateTimePickerFormat.Short };
        var btnFiltrele = new Button { Text = "Göster", Location = new Point(655, 9), Width = 80, Height = 28,
            BackColor = Color.FromArgb(0, 84, 166), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnFiltrele.Click += async (s, e) => await LoadEkstreAsync();
        filterPanel.Controls.AddRange(new Control[] { _cmbCari, _dtpBaslangic, _dtpBitis, btnFiltrele });

        _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, RowHeadersVisible = false,
            AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None };

        _lblBakiye = new Label { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(220, 235, 255),
            Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 20, 0) };

        this.Controls.Add(_grid); this.Controls.Add(_lblBakiye); this.Controls.Add(filterPanel);
        this.Load += async (s, e) => await LoadCariListAsync();
    }

    private async Task LoadCariListAsync()
    {
        using var db = DbContextFactory.Create();
        var cariler = await db.CariHesaplar.OrderBy(c => c.CariAdi).ToListAsync();
        _cmbCari.DataSource = cariler;
        _cmbCari.DisplayMember = "CariAdi";
        _cmbCari.ValueMember = "Id";
    }

    private async Task LoadEkstreAsync()
    {
        if (_cmbCari.SelectedValue == null) return;
        var cariId = (int)_cmbCari.SelectedValue;
        using var db = DbContextFactory.Create();
        var data = await db.CariEkstreler
            .Where(e => e.CariHesapId == cariId && e.Tarih >= _dtpBaslangic.Value.Date && e.Tarih <= _dtpBitis.Value.Date)
            .OrderBy(e => e.Tarih).ThenBy(e => e.Id).ToListAsync();
        _grid.DataSource = data;
        var bakiye = data.LastOrDefault()?.Bakiye ?? 0;
        _lblBakiye.Text = $"Son Bakiye: {bakiye:N2} TL    ";
    }
}

public class CariBakiyeForm : BaseListForm
{
    protected override string ModuleName => "CariHesap";
    protected override string FormTitle => "⚖️  Borç / Alacak Bakiye Listesi (Canlı)";

    public CariBakiyeForm() { this.Name = "CariBakiyeForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colKod", DataPropertyName = "FIRKOD", HeaderText = "Kodu", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colAdi", DataPropertyName = "FIRMAADI", HeaderText = "Cari / Firma Ünvanı", Width = 320 },
            new DataGridViewTextBoxColumn { Name = "colBorc", DataPropertyName = "TOPLAM_BORC", HeaderText = "Toplam Borç", Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colAlacak", DataPropertyName = "TOPLAM_ALACAK", HeaderText = "Toplam Alacak", Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colBakiye", DataPropertyName = "BAKIYE", HeaderText = "Net Bakiye (TL)", Width = 145,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "SON_ISLEM_TARIHI", HeaderText = "Son Hareket", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            string searchClause = "";
            if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
            {
                searchClause = " AND (FIRMAADI LIKE @p OR CAST(FIRKOD AS CHAR) LIKE @p)";
            }

            string sql = $@"
                SELECT 
                    FIRKOD,
                    COALESCE(FIRMAADI, '') as FIRMAADI,
                    TOPLAM_BORC,
                    TOPLAM_ALACAK,
                    BAKIYE,
                    SON_ISLEM_TARIHI
                FROM erp.cari_bakiye_ozet
                WHERE SIRKOD = '1' AND BAKIYE != 0 {searchClause}
                ORDER BY ABS(BAKIYE) DESC
                LIMIT 500;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql, new { p = $"%{TxtSearch?.Text}%" })).ToList();
            Grid.DataSource = data;

            string sumSql = "SELECT SUM(CASE WHEN BAKIYE > 0 THEN BAKIYE ELSE 0 END) as TopBorc, SUM(CASE WHEN BAKIYE < 0 THEN ABS(BAKIYE) ELSE 0 END) as TopAlacak, COUNT(*) as TopAdet FROM erp.cari_bakiye_ozet WHERE SIRKOD = '1' AND BAKIYE != 0;";
            var sum = await Dapper.SqlMapper.QueryFirstOrDefaultAsync(conn, sumSql);
            double tBorc = sum?.TopBorc != null ? (double)sum.TopBorc : 0;
            double tAlacak = sum?.TopAlacak != null ? (double)sum.TopAlacak : 0;
            long tAdet = sum?.TopAdet != null ? (long)sum.TopAdet : 0;

            LblRecordCount.Text = $"Toplam: {tAdet:N0} bakiyeli cari (Listelenen: {data.Count})  |  Toplam Borç: {tBorc:N2} TL  |  Toplam Alacak: {tAlacak:N2} TL";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Bakiye listesi hatası: {ex.Message}");
        }
    }
}

// ==================== STOK HAREKET & RAPOR ====================

public class StokHareketForm : BaseListForm
{
    protected override string ModuleName => "Stok";
    protected override string FormTitle => "🔄  Stok Hareketleri (Canlı)";
    public StokHareketForm() { this.Name = "StokHareketForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "FISNO", HeaderText = "Fiş No", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TARIH", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colStok", DataPropertyName = "STOKADI", HeaderText = "Stok / Ürün / Ambalaj Adı", Width = 320 },
            new DataGridViewTextBoxColumn { Name = "colBirim", DataPropertyName = "BIRIM", HeaderText = "Birim", Width = 65, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "colMiktar", DataPropertyName = "MIKTAR", HeaderText = "Miktar", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "colFiyat", DataPropertyName = "BIRIMFIYAT", HeaderText = "Birim Fiyat", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N4", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colTutar", DataPropertyName = "TOPLAMTUTAR", HeaderText = "Toplam Tutar (TL)", Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            string searchClause = "";
            if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
            {
                searchClause = " AND (d.MALADI LIKE @p OR CAST(d.FISNO AS CHAR) LIKE @p)";
            }

            string sql = $@"
                SELECT 
                    COALESCE(d.FISNO, 0) as FISNO,
                    d.FISTAR as TARIH,
                    COALESCE(d.MALADI, '') as STOKADI,
                    COALESCE(d.BIRIMI, 'KG') as BIRIM,
                    COALESCE(d.MIKTAR, 0) as MIKTAR,
                    COALESCE(d.FIYATI, 0) as BIRIMFIYAT,
                    COALESCE(d.TUTAR, 0) as TOPLAMTUTAR
                FROM stkdet d
                WHERE d.SIRKOD = '1' {searchClause}
                ORDER BY d.FISTAR DESC, d.IDNO DESC
                LIMIT 500;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql, new { p = $"%{TxtSearch?.Text}%" })).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex) 
        { 
            MessageBox.Show($"Stok hareketleri yüklenirken hata: {ex.Message}"); 
        }
    }
}

public class StokRaporForm : Form
{
    public StokRaporForm()
    {
        this.Text = "📊  Stok Giriş / Çıkış Raporu";
        this.Size = new Size(900, 600);
        this.Font = new Font("Segoe UI", 9.5f);
        this.Controls.Add(new Label
        {
            Text = "Stok Giriş / Çıkış Raporu\n\nTarih aralığı ve stok grubu seçerek rapor oluşturabilirsiniz.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.Gray
        });
    }
}

public class FaturaDetailForm : Form
{
    private readonly Fatura? _existing;
    private readonly FaturaTip _tip;

    public FaturaDetailForm(Fatura? existing, FaturaTip tip)
    {
        _existing = existing; _tip = tip;
        this.Text = existing == null ? $"Yeni Fatura ({tip})" : $"Fatura: {existing.FisNo}";
        this.Size = new Size(900, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9.5f);
        this.Controls.Add(new Label
        {
            Text = "Fatura detay formu yapım aşamasında.\nLütfen daha sonra tekrar deneyiniz.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Gray
        });
        var btnKapat = new Button { Text = "Kapat", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnKapat.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        this.Controls.Add(btnKapat);
    }
}

// ==================== MACROSOFT ÖZEL MODÜLLER (KANTAR, PAKETLEME, BAHÇE) ====================

public class KantarForm : BaseListForm
{
    protected override string ModuleName => "Kantar";
    protected override string FormTitle => "⚖️  Kantar / Tartı Fişleri (Canlı)";

    public KantarForm() { this.Name = "KantarForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "GIRISKODU", HeaderText = "Fiş No", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TARIH", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colPlaka", DataPropertyName = "PLAKANO", HeaderText = "Plaka", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colFirma", DataPropertyName = "FIRMAADI", HeaderText = "Cari / Müstahsil", Width = 250 },
            new DataGridViewTextBoxColumn { Name = "colMal", DataPropertyName = "MALADI", HeaderText = "Ürün / Cins", Width = 160 },
            new DataGridViewTextBoxColumn { Name = "colDolu", DataPropertyName = "DOLU", HeaderText = "Dolu (Kg)", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDara", DataPropertyName = "DARA", HeaderText = "Dara (Kg)", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colNet", DataPropertyName = "NET", HeaderText = "Net (Kg)", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colFiresiz", DataPropertyName = "FIRESIZ", HeaderText = "Firesiz (Kg)", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT 
                    COALESCE(GIRISKODU, 0) as GIRISKODU,
                    GIRISTARIHI as TARIH,
                    COALESCE(PLAKA, '') as PLAKANO,
                    COALESCE(FIRMAADI, '') as FIRMAADI,
                    COALESCE(MALADI, '') as MALADI,
                    COALESCE(BURUT, 0) as DOLU,
                    COALESCE(DARA, 0) as DARA,
                    COALESCE(NET, 0) as NET,
                    COALESCE(FIRESIZ, 0) as FIRESIZ
                FROM kantargiris
                ORDER BY IDNO DESC
                LIMIT 200;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql)).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex)
        {
            LblRecordCount.Text = $"Kantar tablosu okundu: {ex.Message}";
        }
    }
}

public class PaketlemeForm : BaseListForm
{
    protected override string ModuleName => "Paketleme";
    protected override string FormTitle => "🏭  Paketleme & Tartım Listesi (Altınova / Tarsus)";

    public PaketlemeForm() { this.Name = "PaketlemeForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colParti", DataPropertyName = "PARTNO", HeaderText = "Parti No", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TARIH", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colFirma", DataPropertyName = "FIRMAADI", HeaderText = "Cari / Müstahsil", Width = 240 },
            new DataGridViewTextBoxColumn { Name = "colMal", DataPropertyName = "MALADI", HeaderText = "Ürün / Çeşit", Width = 180 },
            new DataGridViewTextBoxColumn { Name = "colPalet", DataPropertyName = "PALETSAYISI", HeaderText = "Palet", Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colKutu", DataPropertyName = "KUTUSAYISI", HeaderText = "Kasa/Kutu", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colKilo", DataPropertyName = "KILO", HeaderText = "Toplam Kg", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT 
                    COALESCE(PARTNO, 0) as PARTNO,
                    TARIH,
                    COALESCE(FIRMAADI, '') as FIRMAADI,
                    COALESCE(MALADI, '') as MALADI,
                    COALESCE(PALETSAYISI, 0) as PALETSAYISI,
                    COALESCE(KUTUSAYISI, 0) as KUTUSAYISI,
                    COALESCE(KILO, 0) as KILO
                FROM kantargirispalet
                ORDER BY TARIH DESC, PARTNO DESC
                LIMIT 200;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql)).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex)
        {
            LblRecordCount.Text = $"Paketleme kayıtları: {ex.Message}";
        }
    }
}

public class BahceKesimForm : BaseListForm
{
    protected override string ModuleName => "BahceKesim";
    protected override string FormTitle => "🌳  Bahçe Kesim & Alım Sözleşmeleri";

    public BahceKesimForm() { this.Name = "BahceKesimForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colSozlesme", DataPropertyName = "SOZLESMENO", HeaderText = "Sözleşme No", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TARIH", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colMustahsil", DataPropertyName = "FIRMAADI", HeaderText = "Müstahsil / Bahçe Sahibi", Width = 260 },
            new DataGridViewTextBoxColumn { Name = "colUrun", DataPropertyName = "MALADI", HeaderText = "Ürün / Çeşit", Width = 180 },
            new DataGridViewTextBoxColumn { Name = "colFiyat", DataPropertyName = "FIYAT", HeaderText = "Birim Fiyat", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colTonaj", DataPropertyName = "TAHMINITONAJ", HeaderText = "Tahmini Ton", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT 
                    COALESCE(SOZLESMENO, 0) as SOZLESMENO,
                    TARIH,
                    COALESCE(FIRMAADI, '') as FIRMAADI,
                    COALESCE(MALADI, '') as MALADI,
                    COALESCE(FIYAT, 0) as FIYAT,
                    COALESCE(TAHMINITONAJ, 0) as TAHMINITONAJ
                FROM sozlesme
                ORDER BY TARIH DESC
                LIMIT 200;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql)).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex)
        {
            LblRecordCount.Text = $"Sözleşme kayıtları: {ex.Message}";
        }
    }
}

public class KasaEkstreForm : BaseListForm
{
    protected override string ModuleName => "Kasa";
    protected override string FormTitle => "💰  Kasa Hesap Ekstresi";

    public KasaEkstreForm() { this.Name = "KasaEkstreForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TARIH", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "FISNO", HeaderText = "Fiş No", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colKasa", DataPropertyName = "KASAADI", HeaderText = "Kasa Adı", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "colAciklama", DataPropertyName = "ACIKLAMA", HeaderText = "Açıklama", Width = 280 },
            new DataGridViewTextBoxColumn { Name = "colGiris", DataPropertyName = "GIRIS", HeaderText = "Tahsilat (Giriş)", Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colCikis", DataPropertyName = "CIKIS", HeaderText = "Tediye (Çıkış)", Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colBakiye", DataPropertyName = "BAKIYE", HeaderText = "Bakiye", Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT 
                    TARIH,
                    COALESCE(BELGENO, '') as FISNO,
                    COALESCE(KASAKODU, 'MERKEZ KASA') as KASAADI,
                    COALESCE(ACIKLAMA, '') as ACIKLAMA,
                    COALESCE(BORC, 0) as GIRIS,
                    COALESCE(ALACAK, 0) as CIKIS,
                    0 as BAKIYE
                FROM kasahareket
                ORDER BY TARIH DESC
                LIMIT 200;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql)).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex)
        {
            LblRecordCount.Text = $"Kasa hareketleri: {ex.Message}";
        }
    }
}

public class BankaEkstreForm : BaseListForm
{
    protected override string ModuleName => "Banka";
    protected override string FormTitle => "🏦  Banka Hesap Ekstresi";

    public BankaEkstreForm() { this.Name = "BankaEkstreForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TARIH", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colBanka", DataPropertyName = "BANKAADI", HeaderText = "Banka", Width = 180 },
            new DataGridViewTextBoxColumn { Name = "colHesap", DataPropertyName = "HESAPNO", HeaderText = "Hesap No", Width = 140 },
            new DataGridViewTextBoxColumn { Name = "colAciklama", DataPropertyName = "ACIKLAMA", HeaderText = "Açıklama", Width = 280 },
            new DataGridViewTextBoxColumn { Name = "colGiris", DataPropertyName = "GIRIS", HeaderText = "Yatan (Borç)", Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colCikis", DataPropertyName = "CIKIS", HeaderText = "Çekilen (Alacak)", Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT 
                    TARIH,
                    COALESCE(BANKAADI, '') as BANKAADI,
                    COALESCE(HESAPNO, '') as HESAPNO,
                    COALESCE(ACIKLAMA, '') as ACIKLAMA,
                    COALESCE(BORC, 0) as GIRIS,
                    COALESCE(ALACAK, 0) as CIKIS
                FROM bankahareket
                ORDER BY TARIH DESC
                LIMIT 200;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql)).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex)
        {
            LblRecordCount.Text = $"Banka hareketleri: {ex.Message}";
        }
    }
}

public class StokEkstreForm : BaseListForm
{
    protected override string ModuleName => "Stok";
    protected override string FormTitle => "📦  Stok Kartı Ekstresi";

    public StokEkstreForm() { this.Name = "StokEkstreForm"; SetupColumns(); }

    private void SetupColumns()
    {
        Grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colTarih", DataPropertyName = "TARIH", HeaderText = "Tarih", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } },
            new DataGridViewTextBoxColumn { Name = "colFisNo", DataPropertyName = "FISNO", HeaderText = "Fiş No", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colStok", DataPropertyName = "STOKADI", HeaderText = "Stok / Ürün", Width = 220 },
            new DataGridViewTextBoxColumn { Name = "colIslem", DataPropertyName = "ISLEMTIPI", HeaderText = "İşlem Türü", Width = 120 },
            new DataGridViewTextBoxColumn { Name = "colGiris", DataPropertyName = "GIRISMIKTAR", HeaderText = "Giriş Miktarı", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colCikis", DataPropertyName = "CIKISMIKTAR", HeaderText = "Çıkış Miktarı", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colBirim", DataPropertyName = "BIRIM", HeaderText = "Birim", Width = 60 }
        );
    }

    protected override async Task LoadDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT 
                    d.TARIH,
                    COALESCE(f.FISNO, '') as FISNO,
                    COALESCE(s.ADI, '') as STOKADI,
                    COALESCE(f.ISLEMTIPI, '') as ISLEMTIPI,
                    CASE WHEN d.GIRISCIKIS = 'G' THEN d.MIKTAR ELSE 0 END as GIRISMIKTAR,
                    CASE WHEN d.GIRISCIKIS = 'C' THEN d.MIKTAR ELSE 0 END as CIKISMIKTAR,
                    COALESCE(s.BIRIM, 'KG') as BIRIM
                FROM stkdet d
                LEFT JOIN stkfis f ON f.SIRKOD = d.SIRKOD AND f.FISNO = d.FISNO
                LEFT JOIN stok s ON s.SIRKOD = d.SIRKOD AND s.KODU = d.STOKKODU
                ORDER BY d.TARIH DESC
                LIMIT 200;";

            var data = (await Dapper.SqlMapper.QueryAsync(conn, sql)).ToList();
            Grid.DataSource = data;
            UpdateRecordCount(data.Count);
        }
        catch (Exception ex)
        {
            LblRecordCount.Text = $"Stok hareketleri: {ex.Message}";
        }
    }
}
