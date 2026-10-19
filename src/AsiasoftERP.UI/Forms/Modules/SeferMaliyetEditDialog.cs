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
/// Tır Seferi ve Masraf Girişi Modal Formu
/// </summary>
public class SeferMaliyetEditDialog : Form
{
    private readonly SeferMaliyetService _service = new();
    private readonly SeferFisDto _model;
    private readonly bool _isNew;

    // Kontroller
    private TextBox _txtFisNo = null!;
    private DateTimePicker _dtpTarih = null!;
    private ComboBox _cmbSeferTuru = null!;
    private ComboBox _cmbDurumu = null!;
    private TextBox _txtPlaka = null!;
    private TextBox _txtDorsePlaka = null!;
    private TextBox _txtSurucuAdi = null!;
    private Button _btnSecArac = null!;

    private TextBox _txtCikisNoktasi = null!;
    private TextBox _txtVarisNoktasi = null!;
    private TextBox _txtTeslimYeri = null!;
    private TextBox _txtYukCinsi = null!;

    private NumericUpDown _numCikisKm = null!;
    private NumericUpDown _numDonusKm = null!;
    private Label _lblYapilanKm = null!;

    private NumericUpDown _numCikisLitre = null!;
    private NumericUpDown _numAlinanLitre = null!;
    private NumericUpDown _numKalanLitre = null!;
    private Label _lblTuketilenLitre = null!;
    private Label _lblYuzKmSarfiyat = null!;

    private NumericUpDown _numNavlunTl = null!;
    private NumericUpDown _numNavlunDoviz = null!;
    private ComboBox _cmbNavlunDovizTuru = null!;
    private NumericUpDown _numDovizKuru = null!;
    private NumericUpDown _numAvansTl = null!;
    private NumericUpDown _numPrimTl = null!;

    private DataGridView _gridDetay = null!;
    private Label _lblToplamMasraf = null!;
    private Label _lblAvansFarki = null!;
    private Label _lblNetKarZarar = null!;
    private TextBox _txtAciklama = null!;

    private readonly List<SeferDetayDto> _detayList = new();
    private List<AracLookupDto> _aracListesi = new();

    public SeferMaliyetEditDialog(SeferFisDto? sefer = null)
    {
        _isNew = sefer == null;
        _model = sefer ?? new SeferFisDto();

        InitializeComponent();
        LoadInitialData();
    }

    private void InitializeComponent()
    {
        this.Text = _isNew ? "Yeni Tır Seferi Başlat" : $"Tır Sefer & Maliyet Kartı — SF-{(int)_model.FisNo}";
        this.Size = new Size(1020, 780);
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
            Text = _isNew ? "🚛 Yeni Tır Seferi & Lojistik Masraf Girişi" : $"🚛 Tır Seferi SF-{(int)_model.FisNo} — {_model.Plaka}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(16, 10),
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Çıkış/Dönüş KM sayaçları, yakıt sarfiyatı, yol masrafları ve net sefer kârlılık kartı",
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
            Size = new Size(180, 38),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(pnlFooter.Width - 300, 11),
            BackColor = Color.FromArgb(30, 58, 138),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnKaydetYazdir.FlatAppearance.BorderSize = 0;
        btnKaydetYazdir.Click += async (s, e) => await SaveAndPrintAsync();

        var btnKaydet = new Button
        {
            Text = "💾 Seferi Kaydet",
            Size = new Size(145, 38),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(pnlFooter.Width - 455, 11),
            BackColor = Color.FromArgb(22, 101, 52),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnKaydet.FlatAppearance.BorderSize = 0;
        btnKaydet.Click += async (s, e) => await SaveRecordAsync();

        pnlFooter.Controls.AddRange(new Control[] { btnIptal, btnKaydetYazdir, btnKaydet });

        // Ana Scroll Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 12, 20, 12)
        };

        // Bölüm 1: Araç, Personel ve Güzergah Paneli (GroupBox)
        var grpInfo = CreateGroupBox("1. ARAÇ, PERSONEL VE GÜZERGAH BİLGİLERİ", 12, 12, 960, 125);
        BuildInfoSection(grpInfo);

        // Bölüm 2: KM ve Yakıt (Mazot) Paneli (GroupBox)
        var grpKmYakiti = CreateGroupBox("2. KM SAYAÇLARI VE YAKIT (MAZOT) HESABI", 12, 145, 960, 110);
        BuildKmYakitiSection(grpKmYakiti);

        // Bölüm 3: Navlun Geliri ve Şoför Avansı (GroupBox)
        var grpNavlun = CreateGroupBox("3. NAVLUN (TAŞIMA BEDELİ) VE AVANS BİLGİLERİ", 12, 263, 960, 75);
        BuildNavlunSection(grpNavlun);

        // Bölüm 4: Masraf Kalemleri ve Kârlılık Özeti (GroupBox)
        var grpMasraflar = CreateGroupBox("4. SEFER MASRAF KALEMLERİ VE KÂRLILIK ÖZETİ", 12, 346, 960, 310);
        BuildMasraflarSection(grpMasraflar);

        pnlBody.Controls.AddRange(new Control[] { grpInfo, grpKmYakiti, grpNavlun, grpMasraflar });

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

    private void BuildInfoSection(GroupBox grp)
    {
        int y1 = 24, y2 = 56, y3 = 88;

        // Fiş No
        grp.Controls.Add(new Label { Text = "Fiş No:", Location = new Point(16, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtFisNo = new TextBox { Location = new Point(80, y1), Width = 95, ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249) };
        grp.Controls.Add(_txtFisNo);

        // Tarih
        grp.Controls.Add(new Label { Text = "Tarih:", Location = new Point(190, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _dtpTarih = new DateTimePicker { Location = new Point(235, y1), Width = 110, Format = DateTimePickerFormat.Short };
        grp.Controls.Add(_dtpTarih);

        // Sefer Türü
        grp.Controls.Add(new Label { Text = "Sefer Türü:", Location = new Point(365, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _cmbSeferTuru = new ComboBox { Location = new Point(445, y1), Width = 115, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSeferTuru.Items.AddRange(new object[] { "YURT DIŞI", "YURT İÇİ", "KATRA" });
        _cmbSeferTuru.SelectedIndex = 0;
        grp.Controls.Add(_cmbSeferTuru);

        // Durumu
        grp.Controls.Add(new Label { Text = "Durumu:", Location = new Point(580, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _cmbDurumu = new ComboBox { Location = new Point(645, y1), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbDurumu.Items.AddRange(new object[] { "AÇIK", "KAPALI" });
        _cmbDurumu.SelectedIndex = 0;
        grp.Controls.Add(_cmbDurumu);

        // Çekici Plaka + F10 Buton
        grp.Controls.Add(new Label { Text = "Çekici Plaka:", Location = new Point(16, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtPlaka = new TextBox { Location = new Point(100, y2), Width = 120, CharacterCasing = CharacterCasing.Upper };
        _btnSecArac = new Button { Text = "🔍 F10 Seç", Location = new Point(225, y2 - 1), Width = 80, Height = 25, BackColor = Color.FromArgb(238, 242, 255), FlatStyle = FlatStyle.Flat };
        _btnSecArac.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
        _btnSecArac.Click += (s, e) => ShowAracRehberi();
        grp.Controls.AddRange(new Control[] { _txtPlaka, _btnSecArac });

        // Dorse Plaka
        grp.Controls.Add(new Label { Text = "Dorse / Termokin:", Location = new Point(325, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtDorsePlaka = new TextBox { Location = new Point(445, y2), Width = 115, CharacterCasing = CharacterCasing.Upper };
        grp.Controls.Add(_txtDorsePlaka);

        // Sürücü Adı
        grp.Controls.Add(new Label { Text = "Sürücü / Lojistik:", Location = new Point(580, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtSurucuAdi = new TextBox { Location = new Point(695, y2), Width = 245 };
        grp.Controls.Add(_txtSurucuAdi);

        // Güzergah: Çıkış -> Varış
        grp.Controls.Add(new Label { Text = "Çıkış Noktası:", Location = new Point(16, y3 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtCikisNoktasi = new TextBox { Location = new Point(100, y3), Width = 120, Text = "ANTAKYA" };
        grp.Controls.Add(_txtCikisNoktasi);

        grp.Controls.Add(new Label { Text = "Varış Noktası:", Location = new Point(240, y3 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtVarisNoktasi = new TextBox { Location = new Point(325, y3), Width = 140 };
        grp.Controls.Add(_txtVarisNoktasi);

        grp.Controls.Add(new Label { Text = "Teslimat Yeri:", Location = new Point(480, y3 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtTeslimYeri = new TextBox { Location = new Point(565, y3), Width = 150 };
        grp.Controls.Add(_txtTeslimYeri);

        grp.Controls.Add(new Label { Text = "Yük Cinsi:", Location = new Point(730, y3 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtYukCinsi = new TextBox { Location = new Point(800, y3), Width = 140, Text = "NARENCİYE" };
        grp.Controls.Add(_txtYukCinsi);
    }

    private void BuildKmYakitiSection(GroupBox grp)
    {
        int y1 = 24, y2 = 62;

        // KM
        grp.Controls.Add(new Label { Text = "Çıkış KM:", Location = new Point(16, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numCikisKm = new NumericUpDown { Location = new Point(85, y1), Width = 100, Maximum = 9999999, DecimalPlaces = 0, ThousandsSeparator = true };
        _numCikisKm.ValueChanged += (s, e) => RecalculateKmYakiti();
        grp.Controls.Add(_numCikisKm);

        grp.Controls.Add(new Label { Text = "Dönüş KM:", Location = new Point(200, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numDonusKm = new NumericUpDown { Location = new Point(275, y1), Width = 100, Maximum = 9999999, DecimalPlaces = 0, ThousandsSeparator = true };
        _numDonusKm.ValueChanged += (s, e) => RecalculateKmYakiti();
        grp.Controls.Add(_numDonusKm);

        grp.Controls.Add(new Label { Text = "Yapılan KM:", Location = new Point(390, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _lblYapilanKm = new Label
        {
            Text = "0 KM",
            Location = new Point(470, y1 + 2),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138)
        };
        grp.Controls.Add(_lblYapilanKm);

        // Yakıt Litreleri
        grp.Controls.Add(new Label { Text = "Çıkış Yakıt (LT):", Location = new Point(16, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numCikisLitre = new NumericUpDown { Location = new Point(115, y2), Width = 80, Maximum = 99999, DecimalPlaces = 0 };
        _numCikisLitre.ValueChanged += (s, e) => RecalculateKmYakiti();
        grp.Controls.Add(_numCikisLitre);

        grp.Controls.Add(new Label { Text = "Alınan Yakıt (LT):", Location = new Point(210, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numAlinanLitre = new NumericUpDown { Location = new Point(315, y2), Width = 80, Maximum = 99999, DecimalPlaces = 0 };
        _numAlinanLitre.ValueChanged += (s, e) => RecalculateKmYakiti();
        grp.Controls.Add(_numAlinanLitre);

        grp.Controls.Add(new Label { Text = "Dönüş Depo Kalan (LT):", Location = new Point(410, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numKalanLitre = new NumericUpDown { Location = new Point(555, y2), Width = 80, Maximum = 99999, DecimalPlaces = 0 };
        _numKalanLitre.ValueChanged += (s, e) => RecalculateKmYakiti();
        grp.Controls.Add(_numKalanLitre);

        grp.Controls.Add(new Label { Text = "Tüketilen:", Location = new Point(650, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _lblTuketilenLitre = new Label
        {
            Text = "0 LT",
            Location = new Point(715, y2 + 2),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        grp.Controls.Add(_lblTuketilenLitre);

        grp.Controls.Add(new Label { Text = "100 KM Sarfiyat:", Location = new Point(780, y2 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _lblYuzKmSarfiyat = new Label
        {
            Text = "0.00 LT/100KM",
            Location = new Point(880, y2 + 2),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 101, 52)
        };
        grp.Controls.Add(_lblYuzKmSarfiyat);
    }

    private void BuildNavlunSection(GroupBox grp)
    {
        int y1 = 28;

        // Navlun Bedeli TL
        grp.Controls.Add(new Label { Text = "Navlun (TL):", Location = new Point(16, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numNavlunTl = new NumericUpDown { Location = new Point(95, y1), Width = 110, Maximum = 9999999, DecimalPlaces = 2, ThousandsSeparator = true };
        _numNavlunTl.ValueChanged += (s, e) => RecalculateFinansal();
        grp.Controls.Add(_numNavlunTl);

        // Dövizli Navlun
        grp.Controls.Add(new Label { Text = "Dövizli Navlun:", Location = new Point(220, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numNavlunDoviz = new NumericUpDown { Location = new Point(310, y1), Width = 95, Maximum = 999999, DecimalPlaces = 2 };
        _numNavlunDoviz.ValueChanged += (s, e) => RecalculateDovizToTl();
        _cmbNavlunDovizTuru = new ComboBox { Location = new Point(410, y1), Width = 65, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbNavlunDovizTuru.Items.AddRange(new object[] { "EUR", "USD", "TL" });
        _cmbNavlunDovizTuru.SelectedIndex = 0;
        _numDovizKuru = new NumericUpDown { Location = new Point(480, y1), Width = 65, Maximum = 999, DecimalPlaces = 2, Value = 55.10m };
        _numDovizKuru.ValueChanged += (s, e) => RecalculateDovizToTl();
        grp.Controls.AddRange(new Control[] { _numNavlunDoviz, _cmbNavlunDovizTuru, _numDovizKuru });

        // Şoför Avansı
        grp.Controls.Add(new Label { Text = "Şoför Avansı (TL):", Location = new Point(560, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numAvansTl = new NumericUpDown { Location = new Point(670, y1), Width = 100, Maximum = 999999, DecimalPlaces = 2, ThousandsSeparator = true };
        _numAvansTl.ValueChanged += (s, e) => RecalculateFinansal();
        grp.Controls.Add(_numAvansTl);

        // Şoför Primi
        grp.Controls.Add(new Label { Text = "Şoför Primi (TL):", Location = new Point(780, y1 + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _numPrimTl = new NumericUpDown { Location = new Point(880, y1), Width = 70, Maximum = 99999, DecimalPlaces = 2 };
        _numPrimTl.ValueChanged += (s, e) => RecalculateFinansal();
        grp.Controls.Add(_numPrimTl);
    }

    private void BuildMasraflarSection(GroupBox grp)
    {
        // Masraf Ekle Butonları Barı
        var pnlBar = new Panel { Location = new Point(12, 22), Size = new Size(936, 32) };
        var btnAddRow = new Button
        {
            Text = "➕ Yeni Masraf Ekle",
            Size = new Size(140, 28),
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
            Size = new Size(100, 28),
            Location = new Point(148, 0),
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
            Size = new Size(620, 185),
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = new Font("Segoe UI", 8.5f)
        };

        _gridDetay.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colTarih", HeaderText = "Tarih", Width = 75, DataPropertyName = "TarihStr" },
            new DataGridViewTextBoxColumn { Name = "colHizmet", HeaderText = "Masraf Kalemi / Hizmet Türü", Width = 230, DataPropertyName = "HizmetAdi" },
            new DataGridViewTextBoxColumn { Name = "colBirim", HeaderText = "Birim", Width = 55, DataPropertyName = "Birim" },
            new DataGridViewTextBoxColumn { Name = "colDoviz", HeaderText = "Döviz", Width = 75, DataPropertyName = "DovizTutari", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colTutar", HeaderText = "TL Tutarı", Width = 85, DataPropertyName = "Tutar", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "colAciklama", HeaderText = "Açıklama", Width = 100, DataPropertyName = "Aciklama" }
        );

        grp.Controls.Add(_gridDetay);

        // Sağ Taraf: Canlı Finansal Kartlar Paneli
        var pnlSummary = new Panel
        {
            Location = new Point(642, 56),
            Size = new Size(306, 185),
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblSumHdr = new Label
        {
            Text = "FİNANSAL SEFER KÂRLILIK ÖZETİ",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138),
            Location = new Point(12, 10),
            AutoSize = true
        };

        var lblM1 = new Label { Text = "Toplam Masraf (TL):", Location = new Point(12, 38), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _lblToplamMasraf = new Label { Text = "0.00 TL", Location = new Point(160, 37), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Crimson };

        var lblM2 = new Label { Text = "Avans - Masraf Farkı:", Location = new Point(12, 62), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _lblAvansFarki = new Label { Text = "0.00 TL", Location = new Point(160, 61), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };

        var lblLine = new Label { BackColor = Color.FromArgb(203, 213, 225), Location = new Point(12, 90), Size = new Size(280, 1) };

        var lblM3 = new Label { Text = "NET SEFER KÂRI / ZARARI:", Location = new Point(12, 102), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
        _lblNetKarZarar = new Label
        {
            Text = "0.00 TL",
            Location = new Point(12, 126),
            Size = new Size(280, 36),
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 101, 52),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(240, 253, 244),
            BorderStyle = BorderStyle.FixedSingle
        };

        pnlSummary.Controls.AddRange(new Control[] { lblSumHdr, lblM1, _lblToplamMasraf, lblM2, _lblAvansFarki, lblLine, lblM3, _lblNetKarZarar });
        grp.Controls.Add(pnlSummary);

        // Açıklama Notu
        grp.Controls.Add(new Label { Text = "Sefer Notu / Açıklama:", Location = new Point(12, 248), AutoSize = true, Font = new Font("Segoe UI", 8.5f) });
        _txtAciklama = new TextBox { Location = new Point(12, 268), Size = new Size(936, 30) };
        grp.Controls.Add(_txtAciklama);
    }

    private async void LoadInitialData()
    {
        try
        {
            _aracListesi = await _service.GetAraclarLookupAsync();

            if (_isNew)
            {
                var nextNo = await _service.GetNextFisNoAsync();
                _txtFisNo.Text = $"SF-{(int)nextNo}";
                _dtpTarih.Value = DateTime.Today;

                if (_aracListesi.Count > 0)
                {
                    var defaultArac = _aracListesi.First();
                    _txtPlaka.Text = defaultArac.Plaka;
                    _txtDorsePlaka.Text = defaultArac.DorsePlaka;
                    _txtSurucuAdi.Text = defaultArac.SurucuAdi;
                }
            }
            else
            {
                _txtFisNo.Text = $"SF-{(int)_model.FisNo}";
                _dtpTarih.Value = _model.Tarih;
                _cmbSeferTuru.SelectedItem = _model.SeferTuru;
                _cmbDurumu.SelectedItem = _model.Durumu;
                _txtPlaka.Text = _model.Plaka;
                _txtDorsePlaka.Text = _model.DorsePlaka;
                _txtSurucuAdi.Text = _model.SurucuAdi;

                _txtCikisNoktasi.Text = _model.CikisNoktasi;
                _txtVarisNoktasi.Text = _model.VarisNoktasi;
                _txtTeslimYeri.Text = _model.YuklemeBosaltmaYeri;
                _txtYukCinsi.Text = _model.YukCinsi;

                _numCikisKm.Value = (decimal)_model.CikisKm;
                _numDonusKm.Value = (decimal)_model.DonusKm;

                _numCikisLitre.Value = (decimal)_model.CikisLitre;
                _numAlinanLitre.Value = (decimal)_model.ToplamAlinanLitre;
                _numKalanLitre.Value = (decimal)_model.SeferDonusKalanLitre;

                _numNavlunTl.Value = (decimal)_model.SeferBedeli;
                _numNavlunDoviz.Value = (decimal)_model.SeferTutar;
                _cmbNavlunDovizTuru.SelectedItem = string.IsNullOrWhiteSpace(_model.SeferDovizTuru) ? "EUR" : _model.SeferDovizTuru;
                if (_model.DovizKur > 0) _numDovizKuru.Value = (decimal)_model.DovizKur;
                _numAvansTl.Value = (decimal)_model.AlinanTlAvans;
                _numPrimTl.Value = (decimal)_model.PrimTutariTl;
                _txtAciklama.Text = _model.Aciklama;

                // Detay masrafları yükle
                var details = await _service.GetSeferDetaylarAsync(_model.FisNo);
                _detayList.Clear();
                _detayList.AddRange(details);
            }

            RefreshDetayGrid();
            RecalculateKmYakiti();
            RecalculateFinansal();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veriler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowAracRehberi()
    {
        using var dlg = new Form
        {
            Text = "Araç & Dorse & Sürücü Rehberi (F10)",
            Size = new Size(720, 480),
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
            new DataGridViewTextBoxColumn { Name = "colPlaka", HeaderText = "Çekici Plaka", DataPropertyName = "Plaka", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colDorse", HeaderText = "Dorse Plaka", DataPropertyName = "DorsePlaka", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "colSurucu", HeaderText = "Sürücü / Şoför", DataPropertyName = "SurucuAdi", Width = 200 },
            new DataGridViewTextBoxColumn { Name = "colTip", HeaderText = "Sefer Tipi", DataPropertyName = "AracSeferTipi", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "colMarka", HeaderText = "Marka / Model", DataPropertyName = "Marka", Width = 120 }
        );

        grid.DataSource = _aracListesi;

        void SelectAndClose()
        {
            if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].DataBoundItem is AracLookupDto item)
            {
                _txtPlaka.Text = item.Plaka;
                _txtDorsePlaka.Text = item.DorsePlaka;
                _txtSurucuAdi.Text = item.SurucuAdi;
                if (!string.IsNullOrWhiteSpace(item.AracSeferTipi))
                {
                    _cmbSeferTuru.SelectedItem = item.AracSeferTipi;
                }
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            }
        }

        grid.CellDoubleClick += (s, e) => SelectAndClose();

        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(10) };
        var btnSelect = new Button { Text = "Seç", Size = new Size(85, 28), Dock = DockStyle.Right, BackColor = Color.FromArgb(30, 58, 138), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
            Text = "Yeni Masraf Kalemi Ekle",
            Size = new Size(480, 320),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblH = new Label { Text = "Masraf / Hizmet Kalemi:", Location = new Point(20, 20), AutoSize = true };
        var cmbHizmet = new ComboBox { Location = new Point(20, 42), Width = 420, DropDownStyle = ComboBoxStyle.DropDown };
        cmbHizmet.Items.AddRange(_service.GetStandartMasrafTurleri().ToArray());
        if (cmbHizmet.Items.Count > 0) cmbHizmet.SelectedIndex = 0;

        var lblB = new Label { Text = "Para Birimi:", Location = new Point(20, 80), AutoSize = true };
        var cmbBirim = new ComboBox { Location = new Point(20, 102), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbBirim.Items.AddRange(new object[] { "TL", "EUR", "USD" });
        cmbBirim.SelectedIndex = 0;

        var lblD = new Label { Text = "Döviz Tutarı:", Location = new Point(120, 80), AutoSize = true };
        var numDoviz = new NumericUpDown { Location = new Point(120, 102), Width = 100, Maximum = 999999, DecimalPlaces = 2 };

        var lblT = new Label { Text = "TL Tutarı:", Location = new Point(240, 80), AutoSize = true };
        var numTl = new NumericUpDown { Location = new Point(240, 102), Width = 120, Maximum = 999999, DecimalPlaces = 2 };

        cmbBirim.SelectedIndexChanged += (s, e) =>
        {
            bool isDoviz = cmbBirim.SelectedItem?.ToString() != "TL";
            numDoviz.Enabled = isDoviz;
            if (!isDoviz) numDoviz.Value = 0;
        };

        numDoviz.ValueChanged += (s, e) =>
        {
            if (cmbBirim.SelectedItem?.ToString() != "TL" && _numDovizKuru.Value > 0)
            {
                numTl.Value = numDoviz.Value * _numDovizKuru.Value;
            }
        };

        var lblA = new Label { Text = "Açıklama (Fiş No / Lokasyon):", Location = new Point(20, 140), AutoSize = true };
        var txtAciklama = new TextBox { Location = new Point(20, 162), Width = 420 };

        var btnOk = new Button { Text = "Ekle", Location = new Point(260, 220), Size = new Size(85, 32), BackColor = Color.FromArgb(22, 101, 52), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnCancel = new Button { Text = "Vazgeç", Location = new Point(355, 220), Size = new Size(85, 32), FlatStyle = FlatStyle.Flat };

        btnOk.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(cmbHizmet.Text))
            {
                MessageBox.Show("Lütfen bir masraf türü seçiniz veya yazınız.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _detayList.Add(new SeferDetayDto
            {
                Tarih = _dtpTarih.Value,
                HizmetAdi = cmbHizmet.Text.Trim(),
                Birim = cmbBirim.SelectedItem?.ToString() ?? "TL",
                DovizTutari = (double)numDoviz.Value,
                Tutar = (double)numTl.Value,
                Aciklama = txtAciklama.Text.Trim()
            });

            RefreshDetayGrid();
            RecalculateFinansal();
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        btnCancel.Click += (s, e) => dlg.Close();

        dlg.Controls.AddRange(new Control[] { lblH, cmbHizmet, lblB, cmbBirim, lblD, numDoviz, lblT, numTl, lblA, txtAciklama, btnOk, btnCancel });
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
            RecalculateFinansal();
        }
    }

    private void RefreshDetayGrid()
    {
        var display = _detayList.Select(d => new
        {
            TarihStr = d.Tarih.ToString("dd.MM.yyyy"),
            d.HizmetAdi,
            d.Birim,
            d.DovizTutari,
            d.Tutar,
            d.Aciklama
        }).ToList();

        _gridDetay.DataSource = null;
        _gridDetay.DataSource = display;
    }

    private void RecalculateKmYakiti()
    {
        double cikisKm = (double)_numCikisKm.Value;
        double donusKm = (double)_numDonusKm.Value;
        double yapilanKm = (donusKm > cikisKm && cikisKm > 0) ? (donusKm - cikisKm) : 0;
        _lblYapilanKm.Text = $"{yapilanKm:N0} KM";

        double cikisLt = (double)_numCikisLitre.Value;
        double alinanLt = (double)_numAlinanLitre.Value;
        double kalanLt = (double)_numKalanLitre.Value;
        double tuketilenLt = (cikisLt + alinanLt >= kalanLt) ? (cikisLt + alinanLt - kalanLt) : 0;
        _lblTuketilenLitre.Text = $"{tuketilenLt:N0} LT";

        double sarfiyat = yapilanKm > 0 ? Math.Round((tuketilenLt / yapilanKm) * 100, 2) : 0;
        _lblYuzKmSarfiyat.Text = $"{sarfiyat:N2} LT/100KM";
    }

    private void RecalculateDovizToTl()
    {
        if (_cmbNavlunDovizTuru.SelectedItem?.ToString() != "TL" && _numNavlunDoviz.Value > 0 && _numDovizKuru.Value > 0)
        {
            _numNavlunTl.Value = _numNavlunDoviz.Value * _numDovizKuru.Value;
        }
        RecalculateFinansal();
    }

    private void RecalculateFinansal()
    {
        double toplamMasraf = _detayList.Sum(d => d.Tutar);
        _lblToplamMasraf.Text = $"{toplamMasraf:N2} TL";

        double avans = (double)_numAvansTl.Value;
        double avansFarki = avans - toplamMasraf;
        _lblAvansFarki.Text = $"{avansFarki:N2} TL" + (avansFarki >= 0 ? " (Şoför Borçlu)" : " (Şoför Alacaklı)");

        double navlun = (double)_numNavlunTl.Value;
        double prim = (double)_numPrimTl.Value;
        double netKar = navlun - (toplamMasraf + prim);

        _lblNetKarZarar.Text = $"{netKar:N2} TL";
        if (netKar >= 0)
        {
            _lblNetKarZarar.ForeColor = Color.FromArgb(22, 101, 52);
            _lblNetKarZarar.BackColor = Color.FromArgb(240, 253, 244);
        }
        else
        {
            _lblNetKarZarar.ForeColor = Color.Crimson;
            _lblNetKarZarar.BackColor = Color.FromArgb(254, 242, 242);
        }
    }

    private async Task<bool> SaveRecordAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtPlaka.Text))
        {
            MessageBox.Show("Lütfen çekici plakasını giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPlaka.Focus();
            return false;
        }

        try
        {
            _model.Tarih = _dtpTarih.Value;
            _model.SeferTuru = _cmbSeferTuru.SelectedItem?.ToString() ?? "YURT DIŞI";
            _model.Durumu = _cmbDurumu.SelectedItem?.ToString() ?? "AÇIK";
            _model.Plaka = _txtPlaka.Text.Trim();
            _model.DorsePlaka = _txtDorsePlaka.Text.Trim();
            _model.SurucuAdi = _txtSurucuAdi.Text.Trim();

            _model.CikisNoktasi = _txtCikisNoktasi.Text.Trim();
            _model.VarisNoktasi = _txtVarisNoktasi.Text.Trim();
            _model.YuklemeBosaltmaYeri = _txtTeslimYeri.Text.Trim();
            _model.YukCinsi = _txtYukCinsi.Text.Trim();

            _model.CikisKm = (double)_numCikisKm.Value;
            _model.DonusKm = (double)_numDonusKm.Value;

            _model.CikisLitre = (double)_numCikisLitre.Value;
            _model.ToplamAlinanLitre = (double)_numAlinanLitre.Value;
            _model.SeferDonusKalanLitre = (double)_numKalanLitre.Value;

            _model.SeferBedeli = (double)_numNavlunTl.Value;
            _model.SeferTutar = (double)_numNavlunDoviz.Value;
            _model.SeferDovizTuru = _cmbNavlunDovizTuru.SelectedItem?.ToString() ?? "EUR";
            _model.DovizKur = (double)_numDovizKuru.Value;

            _model.AlinanTlAvans = (double)_numAvansTl.Value;
            _model.PrimTutariTl = (double)_numPrimTl.Value;
            _model.Aciklama = _txtAciklama.Text.Trim();
            _model.Kullanici = Environment.UserName;

            await _service.SaveSeferAsync(_model, _detayList);
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
            PrintSeferRaporu(_model);
        }
    }

    public static void PrintSeferRaporu(SeferFisDto sefer)
    {
        using var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);

        pd.PrintPage += (s, e) =>
        {
            if (e.Graphics != null)
            {
                SeferMaliyetReportRenderer.RenderToGraphics(e.Graphics, sefer, e.MarginBounds);
            }
        };

        using var preview = new PrintPreviewDialog
        {
            Document = pd,
            Width = 900,
            Height = 750,
            StartPosition = FormStartPosition.CenterScreen,
            Text = $"Tır Sefer Maliyet Raporu — SF-{(int)sefer.FisNo}"
        };
        preview.ShowDialog();
    }
}
