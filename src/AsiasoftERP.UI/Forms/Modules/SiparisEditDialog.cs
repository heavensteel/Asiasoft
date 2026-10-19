using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Sipariş Düzenleme ve Kalem Girişi Modal Formu
/// </summary>
public class SiparisEditDialog : Form
{
    private readonly SiparisService _service = new();
    private readonly SiparisDto _model;
    private readonly SiparisYonu _yon;
    private readonly bool _isNew;

    // Kontroller
    private TextBox _txtFisNo = null!;
    private DateTimePicker _dtpTarih = null!;
    private ComboBox _cmbDurumu = null!;
    private ComboBox _cmbSatisSekli = null!;
    private TextBox _txtFirmaAdi = null!;
    private TextBox _txtFirkod = null!;
    private TextBox _txtUlke = null!;
    private TextBox _txtSehir = null!;
    private Button _btnSecCari = null!;

    private DateTimePicker _dtpBaslama = null!;
    private DateTimePicker _dtpBitis = null!;
    private TextBox _txtAracPlaka = null!;
    private TextBox _txtAracPlaka2 = null!;
    private TextBox _txtSoforAdi = null!;
    private TextBox _txtSoforTel = null!;
    private TextBox _txtFrigo = null!;

    private TextBox _txtGumruk = null!;
    private TextBox _txtBosaltmaAdresi = null!;
    private TextBox _txtAciklama = null!;

    private DataGridView _gridDetay = null!;
    private Label _lblTotPalet = null!;
    private Label _lblTotKasa = null!;
    private Label _lblTotKg = null!;
    private Label _lblTotTutar = null!;

    private readonly List<SiparisDetayDto> _detayList = new();
    private List<dynamic> _firmaListesi = new();
    private List<StokLookupDto> _stokListesi = new();

    public SiparisEditDialog(SiparisYonu yon, SiparisDto? siparis = null)
    {
        _yon = yon;
        _isNew = siparis == null;
        _model = siparis ?? new SiparisDto { SiparisYonu = yon };

        InitializeComponent();
        LoadInitialData();
    }

    private void InitializeComponent()
    {
        string yonStr = _yon == SiparisYonu.Alinan ? "Müşteri Satış Siparişi" : "Satın Alma Siparişi";
        this.Text = _isNew ? $"Yeni {yonStr} Oluştur" : $"{yonStr} — SP-{(int)_model.FisNo}";
        this.Size = new Size(1040, 790);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9f);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = _isNew ? $"📦 Yeni {yonStr} Girişi" : $"📦 {yonStr} SP-{(int)_model.FisNo} — {_model.FirmaAdi}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(16, 10),
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Müşteri ve teslimat koşulları, ürün/ambalaj kalemleri, palet-kasa adetleri ve dövizli sipariş tutarları",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(17, 34),
            AutoSize = true
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // Footer Butonları
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10)
        };
        pnlFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        var btnIptal = new Button
        {
            Text = "İptal",
            Size = new Size(95, 38),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(pnlFooter.Width - 110, 11),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat
        };
        btnIptal.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnIptal.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        var btnKaydetYazdir = new Button
        {
            Text = "🖨️ Kaydet ve Yazdır (A4)",
            Size = new Size(185, 38),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(pnlFooter.Width - 305, 11),
            BackColor = Color.FromArgb(30, 58, 138),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnKaydetYazdir.FlatAppearance.BorderSize = 0;
        btnKaydetYazdir.Click += async (s, e) => await SaveAndPrintAsync();

        var btnKaydet = new Button
        {
            Text = "💾 Siparişi Kaydet",
            Size = new Size(150, 38),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(pnlFooter.Width - 465, 11),
            BackColor = Color.FromArgb(22, 101, 52),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnKaydet.FlatAppearance.BorderSize = 0;
        btnKaydet.Click += async (s, e) => await SaveRecordAsync();

        pnlFooter.Controls.AddRange(new Control[] { btnIptal, btnKaydetYazdir, btnKaydet });

        // Scroll Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(16, 10, 16, 10)
        };

        // 1. CARİ & MÜŞTERİ BİLGİLERİ
        var grpCari = CreateGroupBox("1. CARİ HESAP VE SİPARİŞ KÜNYESİ", 12, 10, 980, 115);
        BuildCariSection(grpCari);

        // 2. LOJİSTİK VE TESLİMAT BİLGİLERİ
        var grpLojistik = CreateGroupBox("2. SEVKİYAT, GÜMRÜK VE TESLİMAT KOŞULLARI", 12, 132, 980, 115);
        BuildLojistikSection(grpLojistik);

        // 3. SİPARİŞ KALEMLERİ TABLOSU
        var grpDetay = CreateGroupBox("3. SİPARİŞ EDİLEN ÜRÜN VE AMBALAJ KALEMLERİ", 12, 254, 980, 375);
        BuildDetaySection(grpDetay);

        pnlBody.Controls.AddRange(new Control[] { grpCari, grpLojistik, grpDetay });

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);
    }

    private GroupBox CreateGroupBox(string title, int x, int y, int w, int h)
    {
        return new GroupBox
        {
            Text = title,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138),
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.White,
            Padding = new Padding(12, 8, 12, 8)
        };
    }

    private void BuildCariSection(GroupBox grp)
    {
        int y1 = 24, y2 = 58;

        // Fiş No
        grp.Controls.Add(new Label { Text = "Sipariş No:", Location = new Point(16, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtFisNo = new TextBox { Location = new Point(88, y1), Width = 95, ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249) };
        grp.Controls.Add(_txtFisNo);

        // Tarih
        grp.Controls.Add(new Label { Text = "Tarih:", Location = new Point(200, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _dtpTarih = new DateTimePicker { Location = new Point(245, y1), Width = 110, Format = DateTimePickerFormat.Short };
        grp.Controls.Add(_dtpTarih);

        // Durumu
        grp.Controls.Add(new Label { Text = "Durumu:", Location = new Point(375, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _cmbDurumu = new ComboBox { Location = new Point(440, y1), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbDurumu.Items.AddRange(new object[] { "SİPARİŞ ALINDI", "HAZIRLANIYOR", "YÜKLENDİ", "SEVK EDİLDİ", "TAMAMLANDI", "İPTAL EDİLDİ" });
        _cmbDurumu.SelectedIndex = 0;
        grp.Controls.Add(_cmbDurumu);

        // Teslim Şekli
        grp.Controls.Add(new Label { Text = "Teslim Şekli:", Location = new Point(600, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _cmbSatisSekli = new ComboBox { Location = new Point(680, y1), Width = 95, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSatisSekli.Items.AddRange(new object[] { "DAP", "FOB", "CIF", "CPT", "EXW" });
        _cmbSatisSekli.SelectedIndex = 0;
        grp.Controls.Add(_cmbSatisSekli);

        // Cari Seçimi (F10)
        string lblCariText = _yon == SiparisYonu.Alinan ? "Müşteri / Cari:" : "Tedarikçi / Cari:";
        grp.Controls.Add(new Label { Text = lblCariText, Location = new Point(16, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtFirkod = new TextBox { Location = new Point(105, y2), Width = 78, ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249) };
        _txtFirmaAdi = new TextBox { Location = new Point(190, y2), Width = 390 };
        _btnSecCari = new Button { Text = "🔍 F10 Seç", Location = new Point(585, y2 - 1), Width = 80, Height = 25, BackColor = Color.FromArgb(238, 242, 255), FlatStyle = FlatStyle.Flat };
        _btnSecCari.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
        _btnSecCari.Click += (s, e) => ShowCariRehberi();
        grp.Controls.AddRange(new Control[] { _txtFirkod, _txtFirmaAdi, _btnSecCari });

        // Ülke & Şehir
        grp.Controls.Add(new Label { Text = "Ülke:", Location = new Point(680, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtUlke = new TextBox { Location = new Point(720, y2), Width = 110 };
        grp.Controls.Add(_txtUlke);

        grp.Controls.Add(new Label { Text = "Şehir:", Location = new Point(840, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtSehir = new TextBox { Location = new Point(885, y2), Width = 80 };
        grp.Controls.Add(_txtSehir);
    }

    private void BuildLojistikSection(GroupBox grp)
    {
        int y1 = 24, y2 = 56, y3 = 86;

        // Sevk Tarihleri
        grp.Controls.Add(new Label { Text = "Sevk Tarihi:", Location = new Point(16, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _dtpBaslama = new DateTimePicker { Location = new Point(95, y1), Width = 105, Format = DateTimePickerFormat.Short };
        grp.Controls.Add(_dtpBaslama);

        grp.Controls.Add(new Label { Text = "Termin:", Location = new Point(215, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _dtpBitis = new DateTimePicker { Location = new Point(270, y1), Width = 105, Format = DateTimePickerFormat.Short };
        grp.Controls.Add(_dtpBitis);

        // Plakalar
        grp.Controls.Add(new Label { Text = "Çekici Plaka:", Location = new Point(390, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtAracPlaka = new TextBox { Location = new Point(475, y1), Width = 95, CharacterCasing = CharacterCasing.Upper };
        grp.Controls.Add(_txtAracPlaka);

        grp.Controls.Add(new Label { Text = "Dorse:", Location = new Point(585, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtAracPlaka2 = new TextBox { Location = new Point(635, y1), Width = 90, CharacterCasing = CharacterCasing.Upper };
        grp.Controls.Add(_txtAracPlaka2);

        // Şoför
        grp.Controls.Add(new Label { Text = "Şoför:", Location = new Point(740, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtSoforAdi = new TextBox { Location = new Point(785, y1), Width = 180 };
        grp.Controls.Add(_txtSoforAdi);

        // Gümrük Noktası
        grp.Controls.Add(new Label { Text = "Gümrük Noktası:", Location = new Point(16, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtGumruk = new TextBox { Location = new Point(125, y2), Width = 445 };
        grp.Controls.Add(_txtGumruk);

        // Frigo & Şoför Tel
        grp.Controls.Add(new Label { Text = "Frigo:", Location = new Point(585, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtFrigo = new TextBox { Location = new Point(635, y2), Width = 90, Text = "+4°C" };
        grp.Controls.Add(_txtFrigo);

        grp.Controls.Add(new Label { Text = "Şoför Tel:", Location = new Point(740, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtSoforTel = new TextBox { Location = new Point(810, y2), Width = 155 };
        grp.Controls.Add(_txtSoforTel);

        // Boşaltma Adresi
        grp.Controls.Add(new Label { Text = "Boşaltma Adresi:", Location = new Point(16, y3 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtBosaltmaAdresi = new TextBox { Location = new Point(125, y3), Width = 840 };
        grp.Controls.Add(_txtBosaltmaAdresi);
    }

    private void BuildDetaySection(GroupBox grp)
    {
        // Butonlar Barı
        var pnlBar = new Panel { Location = new Point(12, 22), Size = new Size(956, 32) };
        var btnAddRow = new Button
        {
            Text = "➕ Yeni Kalem Ekle",
            Size = new Size(135, 28),
            Location = new Point(0, 0),
            BackColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(30, 58, 138),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        btnAddRow.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
        btnAddRow.Click += (s, e) => AddDetayRow();

        var btnDelRow = new Button
        {
            Text = "🗑️ Satırı Sil",
            Size = new Size(95, 28),
            Location = new Point(142, 0),
            BackColor = Color.FromArgb(254, 242, 242),
            ForeColor = Color.Crimson,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f)
        };
        btnDelRow.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
        btnDelRow.Click += (s, e) => DeleteSelectedDetayRow();

        pnlBar.Controls.AddRange(new Control[] { btnAddRow, btnDelRow });
        grp.Controls.Add(pnlBar);

        // Grid
        _gridDetay = new DataGridView
        {
            Location = new Point(12, 56),
            Size = new Size(650, 240),
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = new Font("Segoe UI", 8.5f)
        };

        _gridDetay.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colMal", HeaderText = "Ürün / Mahsul Cinsi", Width = 170, DataPropertyName = "MalAdi" },
            new DataGridViewTextBoxColumn { Name = "colBoy", HeaderText = "Boy/Kalibre", Width = 80, DataPropertyName = "Boy" },
            new DataGridViewTextBoxColumn { Name = "colKutu", HeaderText = "Kasa/Kutu Türü", Width = 140, DataPropertyName = "KutuAdi" },
            new DataGridViewTextBoxColumn { Name = "colPalet", HeaderText = "Palet", Width = 55, DataPropertyName = "PaletSayisi", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colKasa", HeaderText = "Kasa", Width = 60, DataPropertyName = "ParcaSayisi", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colNetKg", HeaderText = "Net KG", Width = 75, DataPropertyName = "NetKg", DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colFiyat", HeaderText = "Fiyat", Width = 65, DataPropertyName = "Fiyati", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colTutar", HeaderText = "Tutar", Width = 80, DataPropertyName = "Tutari", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colDoviz", HeaderText = "Döviz", Width = 50, DataPropertyName = "DovizTuru" }
        );

        grp.Controls.Add(_gridDetay);

        // Sağ Taraf: Toplam Kartı
        var pnlTotals = new Panel
        {
            Location = new Point(670, 56),
            Size = new Size(298, 240),
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12)
        };

        var lblTotHdr = new Label
        {
            Text = "SİPARİŞ MİKTAR VE BEDEL ÖZETİ",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138),
            Location = new Point(12, 10),
            AutoSize = true
        };

        var lblP1 = new Label { Text = "Toplam Palet Adedi:", Location = new Point(12, 40), AutoSize = true };
        _lblTotPalet = new Label { Text = "0 Palet", Location = new Point(160, 40), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

        var lblP2 = new Label { Text = "Toplam Kasa Sayısı:", Location = new Point(12, 70), AutoSize = true };
        _lblTotKasa = new Label { Text = "0 Kasa", Location = new Point(160, 70), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

        var lblP3 = new Label { Text = "Toplam Net Ağırlık:", Location = new Point(12, 100), AutoSize = true };
        _lblTotKg = new Label { Text = "0 KG", Location = new Point(160, 100), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(14, 116, 144) };

        var lblSep = new Label { BackColor = Color.FromArgb(203, 213, 225), Location = new Point(12, 130), Size = new Size(270, 1) };

        var lblP4 = new Label { Text = "GENEL TOPLAM SİPARİŞ TUTARI:", Location = new Point(12, 142), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _lblTotTutar = new Label
        {
            Text = "0.00 EUR",
            Location = new Point(12, 168),
            Size = new Size(270, 45),
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 101, 52),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(240, 253, 244),
            BorderStyle = BorderStyle.FixedSingle
        };

        pnlTotals.Controls.AddRange(new Control[] { lblTotHdr, lblP1, _lblTotPalet, lblP2, _lblTotKasa, lblP3, _lblTotKg, lblSep, lblP4, _lblTotTutar });
        grp.Controls.Add(pnlTotals);

        // Açıklama Notu
        grp.Controls.Add(new Label { Text = "Sipariş Notu / Şartlar:", Location = new Point(12, 305), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtAciklama = new TextBox { Location = new Point(12, 325), Size = new Size(956, 32) };
        grp.Controls.Add(_txtAciklama);
    }

    private async void LoadInitialData()
    {
        try
        {
            _firmaListesi = await _service.GetFirmalarLookupAsync();
            _stokListesi = await _service.GetStoklarLookupAsync();

            if (_isNew)
            {
                var nextNo = await _service.GetNextFisNoAsync(_yon);
                _txtFisNo.Text = $"SP-{(int)nextNo}";
                _dtpTarih.Value = DateTime.Today;
                _dtpBaslama.Value = DateTime.Today.AddDays(1);
                _dtpBitis.Value = DateTime.Today.AddDays(7);

                if (_firmaListesi.Count > 0)
                {
                    var f = _firmaListesi.First();
                    _txtFirkod.Text = f.FirKod.ToString();
                    _txtFirmaAdi.Text = f.FirmaAdi;
                    _txtUlke.Text = f.Ulke;
                    _txtSehir.Text = f.Sehir;
                }
            }
            else
            {
                _txtFisNo.Text = $"SP-{(int)_model.FisNo}";
                _dtpTarih.Value = _model.Tarih;
                _cmbDurumu.SelectedItem = string.IsNullOrWhiteSpace(_model.Durumu) ? "SİPARİŞ ALINDI" : _model.Durumu;
                _cmbSatisSekli.SelectedItem = string.IsNullOrWhiteSpace(_model.SatisSekli) ? "DAP" : _model.SatisSekli;

                _txtFirkod.Text = _model.Firkod;
                _txtFirmaAdi.Text = _model.FirmaAdi;
                _txtUlke.Text = _model.Ulke;
                _txtSehir.Text = _model.Sehir;

                if (_model.BaslamaTarihi.HasValue) _dtpBaslama.Value = _model.BaslamaTarihi.Value;
                if (_model.BitisTarihi.HasValue) _dtpBitis.Value = _model.BitisTarihi.Value;

                _txtAracPlaka.Text = _model.AracPlaka;
                _txtAracPlaka2.Text = _model.AracPlaka2;
                _txtSoforAdi.Text = _model.SoforAdi;
                _txtSoforTel.Text = _model.SoforTelefon;
                _txtFrigo.Text = _model.FrigoDerece;
                _txtGumruk.Text = _model.GumrukAdres;
                _txtBosaltmaAdresi.Text = _model.BosaltmaAdresi;
                _txtAciklama.Text = _model.Aciklama;

                var details = await _service.GetSiparisDetaylarAsync(_yon, _model.FisNo);
                _detayList.Clear();
                if (details != null && details.Count > 0)
                {
                    _detayList.AddRange(details);
                }
                else if (_model.Detaylar != null && _model.Detaylar.Count > 0)
                {
                    _detayList.AddRange(_model.Detaylar);
                }
            }

            RefreshDetayGrid();
            RecalculateTotals();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SiparisEditDialog] Data load error: {ex.Message}");
        }
    }

    private void ShowCariRehberi()
    {
        using var dlg = new Form
        {
            Text = "Cari Hesap / Müşteri Rehberi (F10)",
            Size = new Size(680, 450),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            BackgroundColor = Color.White
        };

        grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colKod", HeaderText = "Cari Kodu", DataPropertyName = "FirKod", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "colAd", HeaderText = "Firma / Müşteri Adı", DataPropertyName = "FirmaAdi", Width = 300 },
            new DataGridViewTextBoxColumn { Name = "colUlke", HeaderText = "Ülke", DataPropertyName = "Ulke", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colSehir", HeaderText = "Şehir", DataPropertyName = "Sehir", Width = 110 }
        );

        grid.DataSource = _firmaListesi;

        void SelectAndClose()
        {
            if (grid.SelectedRows.Count > 0)
            {
                var rowItem = grid.SelectedRows[0].DataBoundItem;
                if (rowItem != null)
                {
                    dynamic item = rowItem;
                    _txtFirkod.Text = item.FirKod?.ToString() ?? "";
                    _txtFirmaAdi.Text = item.FirmaAdi ?? "";
                    _txtUlke.Text = item.Ulke ?? "";
                    _txtSehir.Text = item.Sehir ?? "";
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
            }
        }

        grid.CellDoubleClick += (s, e) => SelectAndClose();

        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(8) };
        var btnSelect = new Button { Text = "Seç", Size = new Size(80, 26), Dock = DockStyle.Right, BackColor = Color.FromArgb(30, 58, 138), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnSelect.Click += (s, e) => SelectAndClose();
        pnlBottom.Controls.Add(btnSelect);

        dlg.Controls.Add(grid);
        dlg.Controls.Add(pnlBottom);
        dlg.ShowDialog(this);
    }

    private void AddDetayRow()
    {
        using var dlg = new Form
        {
            Text = "Yeni Sipariş Kalemi Ekle",
            Size = new Size(540, 420),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblU = new Label { Text = "Ürün / Mahsul Cinsi:", Location = new Point(20, 20), AutoSize = true };
        var cmbUrun = new ComboBox { Location = new Point(20, 42), Width = 360, DropDownStyle = ComboBoxStyle.DropDown };
        foreach (var s in _stokListesi) cmbUrun.Items.Add(s.MalAdi);
        if (cmbUrun.Items.Count == 0)
        {
            cmbUrun.Items.AddRange(new object[] { "LİMON - ENTER", "LİMON - MAYER", "MANDALİNA - W.MURCOTT", "MANDALİNA - PRIMASOL", "PORTAKAL - VALENCIA", "PORTAKAL - WASHINGTON", "GREYFURT - STAR RUBY" });
        }
        cmbUrun.SelectedIndex = 0;

        var lblBoy = new Label { Text = "Boy / Kalibre:", Location = new Point(20, 80), AutoSize = true };
        var txtBoy = new TextBox { Location = new Point(20, 102), Width = 110, Text = "58MM+" };

        var lblKal = new Label { Text = "Kalite:", Location = new Point(145, 80), AutoSize = true };
        var cmbKal = new ComboBox { Location = new Point(145, 102), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbKal.Items.AddRange(new object[] { "1.KALİTE", "EKSTRA", "2.KALİTE", "SANAYİ" });
        cmbKal.SelectedIndex = 0;

        var lblKutu = new Label { Text = "Kasa / Kutu Türü:", Location = new Point(270, 80), AutoSize = true };
        var cmbKutu = new ComboBox { Location = new Point(270, 102), Width = 230, DropDownStyle = ComboBoxStyle.DropDown };
        cmbKutu.Items.AddRange(new object[] { "30*40*18 PLASTİK KASA MAVİ", "KARTON KUTU 40*60 (15 KG)", "AHŞAP KASA (10 KG)", "TAŞIYICI PLASTİK KASA" });
        cmbKutu.SelectedIndex = 0;

        var lblPalet = new Label { Text = "Palet Adedi:", Location = new Point(20, 140), AutoSize = true };
        var numPalet = new NumericUpDown { Location = new Point(20, 162), Width = 110, Maximum = 999, Value = 16 };

        var lblKasa = new Label { Text = "Kasa Sayısı:", Location = new Point(145, 140), AutoSize = true };
        var numKasa = new NumericUpDown { Location = new Point(145, 162), Width = 110, Maximum = 99999, Value = 1200 };

        var lblKg = new Label { Text = "Net KG:", Location = new Point(270, 140), AutoSize = true };
        var numKg = new NumericUpDown { Location = new Point(270, 162), Width = 110, Maximum = 999999, DecimalPlaces = 0, Value = 18000 };

        var lblFiyat = new Label { Text = "Birim Fiyat:", Location = new Point(395, 140), AutoSize = true };
        var numFiyat = new NumericUpDown { Location = new Point(395, 162), Width = 105, Maximum = 9999, DecimalPlaces = 2, Value = 1.25m };

        var lblDoviz = new Label { Text = "Para Birimi:", Location = new Point(20, 200), AutoSize = true };
        var cmbDoviz = new ComboBox { Location = new Point(20, 222), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbDoviz.Items.AddRange(new object[] { "EUR", "USD", "TL" });
        cmbDoviz.SelectedIndex = 0;

        var lblTutar = new Label { Text = "Toplam Tutar:", Location = new Point(145, 200), AutoSize = true };
        var numTutar = new NumericUpDown { Location = new Point(145, 222), Width = 150, Maximum = 9999999, DecimalPlaces = 2 };

        void CalcTutar()
        {
            numTutar.Value = numKg.Value * numFiyat.Value;
        }

        numKg.ValueChanged += (s, e) => CalcTutar();
        numFiyat.ValueChanged += (s, e) => CalcTutar();
        CalcTutar();

        var btnOk = new Button { Text = "Ekle", Location = new Point(320, 320), Size = new Size(85, 32), BackColor = Color.FromArgb(22, 101, 52), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnCancel = new Button { Text = "Vazgeç", Location = new Point(415, 320), Size = new Size(85, 32), FlatStyle = FlatStyle.Flat };

        btnOk.Click += (s, e) =>
        {
            _detayList.Add(new SiparisDetayDto
            {
                MalKodu = "1100",
                MalAdi = cmbUrun.Text.Trim(),
                Boy = txtBoy.Text.Trim(),
                Kalite = cmbKal.SelectedItem?.ToString() ?? "1.KALİTE",
                KutuAdi = cmbKutu.Text.Trim(),
                PaletAdi = "80*120 EPAL PALET",
                PaletSayisi = (double)numPalet.Value,
                ParcaSayisi = (double)numKasa.Value,
                NetKg = (double)numKg.Value,
                Fiyati = (double)numFiyat.Value,
                DovizTuru = cmbDoviz.SelectedItem?.ToString() ?? "EUR",
                Tutari = (double)numTutar.Value
            });

            RefreshDetayGrid();
            RecalculateTotals();
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        btnCancel.Click += (s, e) => dlg.Close();

        dlg.Controls.AddRange(new Control[] {
            lblU, cmbUrun, lblBoy, txtBoy, lblKal, cmbKal, lblKutu, cmbKutu,
            lblPalet, numPalet, lblKasa, numKasa, lblKg, numKg, lblFiyat, numFiyat,
            lblDoviz, cmbDoviz, lblTutar, numTutar, btnOk, btnCancel
        });
        dlg.ShowDialog(this);
    }

    private void DeleteSelectedDetayRow()
    {
        if (_gridDetay.SelectedRows.Count == 0) return;
        int idx = _gridDetay.SelectedRows[0].Index;
        if (idx >= 0 && idx < _detayList.Count)
        {
            _detayList.RemoveAt(idx);
            RefreshDetayGrid();
            RecalculateTotals();
        }
    }

    private void RefreshDetayGrid()
    {
        _gridDetay.DataSource = null;
        _gridDetay.DataSource = _detayList;
    }

    private void RecalculateTotals()
    {
        double palet = _detayList.Sum(d => d.PaletSayisi);
        double kasa = _detayList.Sum(d => d.ParcaSayisi);
        double kg = _detayList.Sum(d => d.NetKg);
        double tutar = _detayList.Sum(d => d.Tutari);
        string doviz = _detayList.FirstOrDefault()?.DovizTuru ?? "EUR";

        _lblTotPalet.Text = $"{palet:N0} Palet";
        _lblTotKasa.Text = $"{kasa:N0} Kasa";
        _lblTotKg.Text = $"{kg:N0} KG ({kg / 1000.0:N1} Ton)";
        _lblTotTutar.Text = $"{tutar:N2} {doviz}";
    }

    private async Task<bool> SaveRecordAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtFirmaAdi.Text))
        {
            MessageBox.Show("Lütfen müşteri / firma adını giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtFirmaAdi.Focus();
            return false;
        }

        try
        {
            _model.Tarih = _dtpTarih.Value;
            _model.Durumu = _cmbDurumu.SelectedItem?.ToString() ?? "SİPARİŞ ALINDI";
            _model.SatisSekli = _cmbSatisSekli.SelectedItem?.ToString() ?? "DAP";
            _model.Firkod = _txtFirkod.Text.Trim();
            _model.FirmaAdi = _txtFirmaAdi.Text.Trim();
            _model.Ulke = _txtUlke.Text.Trim();
            _model.Sehir = _txtSehir.Text.Trim();

            _model.BaslamaTarihi = _dtpBaslama.Value;
            _model.BitisTarihi = _dtpBitis.Value;

            _model.AracPlaka = _txtAracPlaka.Text.Trim();
            _model.AracPlaka2 = _txtAracPlaka2.Text.Trim();
            _model.SoforAdi = _txtSoforAdi.Text.Trim();
            _model.SoforTelefon = _txtSoforTel.Text.Trim();
            _model.FrigoDerece = _txtFrigo.Text.Trim();

            _model.GumrukAdres = _txtGumruk.Text.Trim();
            _model.BosaltmaAdresi = _txtBosaltmaAdresi.Text.Trim();
            _model.Aciklama = _txtAciklama.Text.Trim();
            _model.Kullanici = Environment.UserName;

            if (_detayList.Count > 0)
            {
                _model.DovizTuru = _detayList.First().DovizTuru;
            }

            await _service.SaveSiparisAsync(_yon, _model, _detayList);
            this.DialogResult = DialogResult.OK;
            this.Close();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayıt sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private async Task SaveAndPrintAsync()
    {
        if (await SaveRecordAsync())
        {
            _model.Detaylar = _detayList;
            PrintSiparisRaporu(_model);
        }
    }

    public static void PrintSiparisRaporu(SiparisDto siparis)
    {
        using var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);

        pd.PrintPage += (s, e) =>
        {
            if (e.Graphics != null)
            {
                SiparisReportRenderer.RenderToGraphics(e.Graphics, siparis, e.MarginBounds);
            }
        };

        using var preview = new PrintPreviewDialog
        {
            Document = pd,
            Width = 900,
            Height = 750,
            StartPosition = FormStartPosition.CenterScreen,
            Text = $"Sipariş Formu — SP-{(int)siparis.FisNo}"
        };
        preview.ShowDialog();
    }
}
