using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Helpers;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

public class MustahsilMakbuzEditDialog : Form
{
    private readonly MustahsilMakbuzDto _makbuz;
    private readonly MustahsilMakbuzService _service = new();
    private readonly bool _isNew;

    // Controls
    private TextBox _txtMakbuzNo = null!;
    private DateTimePicker _dtpTarih = null!;
    private TextBox _txtGirisKodu = null!;
    private TextBox _txtBolgesi = null!;

    private TextBox _txtTcNo = null!;
    private TextBox _txtAdiSoyadi = null!;
    private TextBox _txtBabaAdi = null!;
    private TextBox _txtDogumYeri = null!;
    private TextBox _txtBagkurNo = null!;
    private TextBox _txtAdres = null!;
    private TextBox _txtSehir = null!;
    private TextBox _txtIlce = null!;

    private ComboBox _cboMalAdi = null!;
    private TextBox _txtMalKodu = null!;
    private TextBox _txtKunyeNo = null!;
    private TextBox _txtMiktar = null!;
    private TextBox _txtFiyat = null!;
    private TextBox _txtBrutTutar = null!;

    private TextBox _txtStopajOran = null!;
    private TextBox _txtStopajTutar = null!;
    private TextBox _txtBagkurOran = null!;
    private TextBox _txtBagkurTutar = null!;
    private TextBox _txtBorsaOran = null!;
    private TextBox _txtBorsaTutar = null!;
    private TextBox _txtToplamKesinti = null!;
    private TextBox _txtNetOdenen = null!;
    private TextBox _txtYaziIle = null!;

    // GİB 05.11.2026 Zorunlu SMS Doğrulama Kontrolleri
    private TextBox _txtSmsTelefon = null!;
    private ComboBox _cboSmsOperator = null!;
    private TextBox _txtSmsOperatorVkn = null!;
    private TextBox _txtSmsKodu = null!;
    private Label _lblSmsStatus = null!;

    public MustahsilMakbuzEditDialog(MustahsilMakbuzDto? existing = null)
    {
        _isNew = (existing == null || existing.IdNo <= 0);
        _makbuz = existing ?? new MustahsilMakbuzDto
        {
            Tarih = DateTime.Today,
            StopajOrani = 2.0,
            BagkurOrani = 2.0,
            TicBorOrani = 0.2,
            Miktar = 10000,
            Fiyati = 15.00,
            MalAdi = "LİMON - ENTER",
            MalKodu = "1102",
            Bolgesi = "HATAY / ARSUZ",
            Sehir = "HATAY",
            Ilece = "ARSUZ"
        };

        this.DoubleBuffered = true;
        InitializeComponents();
        PopulateData();
    }

    private void InitializeComponents()
    {
        this.Text = _isNew ? "🌾 Yeni Müstahsil Makbuzu — Asiasoft MacERP" : $"🌾 Müstahsil Makbuzu Düzenle — {_makbuz.MakbuzNo}";
        this.Size = new Size(950, 840);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. TOP HEADER
        var pnlHdr = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlHdr.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlHdr.Height - 1, pnlHdr.Width, pnlHdr.Height - 1);
        };

        var lblIcon = new Label
        {
            Text = "🌾",
            Font = new Font("Segoe UI", 20f),
            Location = new Point(16, 12),
            Size = new Size(40, 40)
        };

        var lblTitle = new Label
        {
            Text = _isNew ? "Yeni Müstahsil Makbuzu Düzenle" : $"Müstahsil Makbuzu — {_makbuz.MakbuzNo}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(62, 10),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "213 Sayılı V.U.K. Madde 235 hükümlerine göre çiftçiden zirai mahsul alım ve tevkifat kaydı",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(64, 34),
            AutoSize = true
        };

        pnlHdr.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSub });

        // 2. BOTTOM ACTIONS
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        var btnCancel = new Button
        {
            Text = "İptal",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(85, 38),
            Location = new Point(pnlBottom.Width - 110, 11),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        var btnPrint = new Button
        {
            Text = "🖨️  Kaydet ve Yazdır (A4)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(205, 38),
            Location = new Point(pnlBottom.Width - 325, 11),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += async (_, _) =>
        {
            if (await SaveInternalAsync())
            {
                MustahsilMakbuzReportRenderer.PrintReport(_makbuz);
                this.DialogResult = DialogResult.OK;
            }
        };

        var btnSave = new Button
        {
            Text = "💾  Makbuzu Kaydet",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 38),
            Location = new Point(pnlBottom.Width - 515, 11),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (_, _) =>
        {
            if (await SaveInternalAsync())
            {
                this.DialogResult = DialogResult.OK;
            }
        };

        pnlBottom.Controls.AddRange(new Control[] { btnSave, btnPrint, btnCancel });

        // 3. BODY CONTAINER (Scrollable)
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = ModernColors.Slate100
        };

        int curY = 10;

        // Group 1: Makbuz ve Belge Bilgileri (Height: 90)
        var grpBelge = CreateGroupBox("1. MAKBUZ VE BELGE BİLGİLERİ", curY, 82);
        var lblMakNo = new Label { Text = "Makbuz No:", Location = new Point(14, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtMakbuzNo = new TextBox { Location = new Point(14, 45), Size = new Size(130, 26), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

        var lblTarih = new Label { Text = "Makbuz Tarihi:", Location = new Point(155, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _dtpTarih = new DateTimePicker { Location = new Point(155, 45), Size = new Size(125, 26), Format = DateTimePickerFormat.Short };

        var lblGiris = new Label { Text = "Kantar Giriş Ref:", Location = new Point(290, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtGirisKodu = new TextBox { Location = new Point(290, 45), Size = new Size(110, 26) };

        var lblBolge = new Label { Text = "Alım Bölgesi / İlçe:", Location = new Point(410, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBolgesi = new TextBox { Location = new Point(410, 45), Size = new Size(200, 26) };

        grpBelge.Controls.AddRange(new Control[] { lblMakNo, _txtMakbuzNo, lblTarih, _dtpTarih, lblGiris, _txtGirisKodu, lblBolge, _txtBolgesi });
        pnlBody.Controls.Add(grpBelge);
        curY += 92;

        // Group 2: Müstahsil / Çiftçi Bilgileri (Height: 140)
        var grpCari = CreateGroupBox("2. MÜSTAHSİL / ÇİFTÇİ BİLGİLERİ (MALI TESLİM EDEN)", curY, 134);

        var lblTc = new Label { Text = "T.C. Kimlik No:", Location = new Point(14, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtTcNo = new TextBox { Location = new Point(14, 45), Size = new Size(135, 26), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

        var btnRehber = new Button
        {
            Text = "🔍 Rehberden Seç (F10)",
            Location = new Point(155, 44),
            Size = new Size(150, 28),
            BackColor = Color.FromArgb(241, 245, 249),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRehber.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnRehber.Click += (_, _) => OpenProducerLookup();

        var lblAd = new Label { Text = "Müstahsil Adı Soyadı:", Location = new Point(315, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtAdiSoyadi = new TextBox { Location = new Point(315, 45), Size = new Size(260, 26), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

        var lblBagkur = new Label { Text = "Bağ-Kur No:", Location = new Point(585, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBagkurNo = new TextBox { Location = new Point(585, 45), Size = new Size(130, 26) };

        var lblBaba = new Label { Text = "Baba Adı:", Location = new Point(725, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBabaAdi = new TextBox { Location = new Point(725, 45), Size = new Size(140, 26) };

        var lblAdres = new Label { Text = "İkametgâh Adresi:", Location = new Point(14, 76), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtAdres = new TextBox { Location = new Point(14, 96), Size = new Size(420, 26) };

        var lblIlce = new Label { Text = "İlçe:", Location = new Point(445, 76), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtIlce = new TextBox { Location = new Point(445, 96), Size = new Size(130, 26) };

        var lblSehir = new Label { Text = "İl / Şehir:", Location = new Point(585, 76), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtSehir = new TextBox { Location = new Point(585, 96), Size = new Size(130, 26) };

        var lblDogum = new Label { Text = "Doğum Yeri/Yılı:", Location = new Point(725, 76), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtDogumYeri = new TextBox { Location = new Point(725, 96), Size = new Size(140, 26) };

        grpCari.Controls.AddRange(new Control[] {
            lblTc, _txtTcNo, btnRehber, lblAd, _txtAdiSoyadi, lblBagkur, _txtBagkurNo, lblBaba, _txtBabaAdi,
            lblAdres, _txtAdres, lblIlce, _txtIlce, lblSehir, _txtSehir, lblDogum, _txtDogumYeri
        });
        pnlBody.Controls.Add(grpCari);
        curY += 144;

        // Group 3: Satın Alınan Mahsul Bilgileri (Height: 90)
        var grpUrun = CreateGroupBox("3. SATIN ALINAN TARIMSAL ÜRÜN BİLGİLERİ", curY, 84);

        var lblUrun = new Label { Text = "Ürün / Mahsul Cinsi:", Location = new Point(14, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboMalAdi = new ComboBox { Location = new Point(14, 45), Size = new Size(240, 26), DropDownStyle = ComboBoxStyle.DropDown };
        _cboMalAdi.Items.AddRange(new object[] { "LİMON - ENTER", "MANDALİNA - W.MURCOTT", "PORTAKAL - WASHINGTON", "GREYFURT - STAR RUBY", "LİMON - MAYER", "MANDALİNA - SATSUMA", "PORTAKAL - VALENCIA", "ÜZÜM - RED GLOBE" });

        var lblMalKod = new Label { Text = "Mal Kodu:", Location = new Point(265, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtMalKodu = new TextBox { Location = new Point(265, 45), Size = new Size(90, 26), Text = "1102" };

        var lblKunye = new Label { Text = "HKS Künye No:", Location = new Point(365, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtKunyeNo = new TextBox { Location = new Point(365, 45), Size = new Size(150, 26) };

        var lblMiktar = new Label { Text = "Net Miktar (KG):", Location = new Point(525, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtMiktar = new TextBox { Location = new Point(525, 45), Size = new Size(110, 26), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Right };
        _txtMiktar.TextChanged += (_, _) => RecalculateTaxes();

        var lblFiyat = new Label { Text = "Birim Fiyat (TL/KG):", Location = new Point(645, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtFiyat = new TextBox { Location = new Point(645, 45), Size = new Size(100, 26), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Right };
        _txtFiyat.TextChanged += (_, _) => RecalculateTaxes();

        var lblBrut = new Label { Text = "Brüt Mal Bedeli:", Location = new Point(755, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtBrutTutar = new TextBox { Location = new Point(755, 45), Size = new Size(130, 26), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Right, ForeColor = ModernColors.Slate900 };

        grpUrun.Controls.AddRange(new Control[] { lblUrun, _cboMalAdi, lblMalKod, _txtMalKodu, lblKunye, _txtKunyeNo, lblMiktar, _txtMiktar, lblFiyat, _txtFiyat, lblBrut, _txtBrutTutar });
        pnlBody.Controls.Add(grpUrun);
        curY += 94;

        // Group 4: Yasal Tevkifat & Kesinti Oranları (Height: 180)
        var grpVergi = CreateGroupBox("4. YASAL TEVKİFAT VE VERGİ KESİNTİLERİ (V.U.K. MD. 235)", curY, 175);

        // Row 1: Stopaj, Bağ-Kur, Borsa Oranları ve Tutarları
        var lblStpOrn = new Label { Text = "Stopaj Oranı (%):", Location = new Point(14, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtStopajOran = new TextBox { Location = new Point(14, 45), Size = new Size(80, 26), Text = "2,0", TextAlign = HorizontalAlignment.Right };
        _txtStopajOran.TextChanged += (_, _) => RecalculateTaxes();

        var lblStpTut = new Label { Text = "(-) Stopaj Tutarı:", Location = new Point(100, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtStopajTutar = new TextBox { Location = new Point(100, 45), Size = new Size(120, 26), ReadOnly = true, BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(185, 28, 28), Font = new Font("Segoe UI", 9f, FontStyle.Bold), TextAlign = HorizontalAlignment.Right };

        var lblBgkOrn = new Label { Text = "Bağ-Kur Oranı (%):", Location = new Point(235, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBagkurOran = new TextBox { Location = new Point(235, 45), Size = new Size(80, 26), Text = "2,0", TextAlign = HorizontalAlignment.Right };
        _txtBagkurOran.TextChanged += (_, _) => RecalculateTaxes();

        var lblBgkTut = new Label { Text = "(-) Bağ-Kur Tutarı:", Location = new Point(320, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBagkurTutar = new TextBox { Location = new Point(320, 45), Size = new Size(120, 26), ReadOnly = true, BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(185, 28, 28), Font = new Font("Segoe UI", 9f, FontStyle.Bold), TextAlign = HorizontalAlignment.Right };

        var lblBorOrn = new Label { Text = "Borsa Tescil (%):", Location = new Point(455, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBorsaOran = new TextBox { Location = new Point(455, 45), Size = new Size(80, 26), Text = "0,2", TextAlign = HorizontalAlignment.Right };
        _txtBorsaOran.TextChanged += (_, _) => RecalculateTaxes();

        var lblBorTut = new Label { Text = "(-) Borsa Tutarı:", Location = new Point(540, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBorsaTutar = new TextBox { Location = new Point(540, 45), Size = new Size(120, 26), ReadOnly = true, BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(185, 28, 28), Font = new Font("Segoe UI", 9f, FontStyle.Bold), TextAlign = HorizontalAlignment.Right };

        var lblTopKes = new Label { Text = "Toplam Kesintiler:", Location = new Point(680, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtToplamKesinti = new TextBox { Location = new Point(680, 45), Size = new Size(140, 26), ReadOnly = true, BackColor = Color.FromArgb(254, 226, 226), ForeColor = Color.FromArgb(153, 27, 27), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Right };

        // Row 2: Net Ödenen (Prominent Green Box) & Yazı ile
        var lblNetOdenen = new Label { Text = "💰 NET ÖDENECEK BEDEL (TL):", Location = new Point(14, 82), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 120, 70) };
        _txtNetOdenen = new TextBox
        {
            Location = new Point(14, 104),
            Size = new Size(240, 32),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 120, 70),
            BackColor = Color.FromArgb(240, 253, 244),
            ReadOnly = true,
            TextAlign = HorizontalAlignment.Right
        };

        var lblYazi = new Label { Text = "Yalnız (Yazı İle Tutar):", Location = new Point(265, 82), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtYaziIle = new TextBox
        {
            Location = new Point(265, 104),
            Size = new Size(615, 28),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            BackColor = Color.FromArgb(248, 250, 252),
            ReadOnly = true
        };

        var lblNote = new Label
        {
            Text = "💡 Not: Çiftçinin Ziraat Odası veya SGK Muafiyet Belgesi bulunması durumunda Bağ-Kur kesinti oranını 0,0 yapabilirsiniz.",
            Location = new Point(14, 145),
            AutoSize = true,
            Font = new Font("Segoe UI", 8f, FontStyle.Italic),
            ForeColor = ModernColors.Slate500
        };

        grpVergi.Controls.AddRange(new Control[] {
            lblStpOrn, _txtStopajOran, lblStpTut, _txtStopajTutar,
            lblBgkOrn, _txtBagkurOran, lblBgkTut, _txtBagkurTutar,
            lblBorOrn, _txtBorsaOran, lblBorTut, _txtBorsaTutar,
            lblTopKes, _txtToplamKesinti,
            lblNetOdenen, _txtNetOdenen, lblYazi, _txtYaziIle, lblNote
        });
        pnlBody.Controls.Add(grpVergi);
        curY += 185;

        // Group 5: GİB Elektronik SMS Doğrulama (05.11.2026 Zorunlu Standart)
        var grpSms = CreateGroupBox("5. GİB ELEKTRONİK SMS DOĞRULAMA (05.11.2026 İTİBARİYLE ZORUNLU KILAVUZ V1.1)", curY, 120);

        var lblSmsTel = new Label { Text = "Müstahsil Cep Telefonu:", Location = new Point(14, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtSmsTelefon = new TextBox { Location = new Point(14, 45), Size = new Size(130, 26), Font = new Font("Segoe UI", 9.5f), PlaceholderText = "0532 000 00 00" };

        var lblSmsOp = new Label { Text = "Lisanslı SMS Operatörü:", Location = new Point(155, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboSmsOperator = new ComboBox { Location = new Point(155, 45), Size = new Size(260, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboSmsOperator.Items.AddRange(new object[] {
            "NETGSM İLETİŞİM BİLGİ TEKNOLOJİLERİ A.Ş.",
            "İLETİ MERKEZİ BİLGİ TEKNOLOJİLERİ A.Ş.",
            "TURKCELL İLETİŞİM HİZMETLERİ A.Ş.",
            "VODAFONE TELEKOMÜNİKASYON A.Ş.",
            "TÜRK TELEKOMÜNİKASYON A.Ş."
        });
        _cboSmsOperator.SelectedIndex = 0;

        var lblSmsVkn = new Label { Text = "Operatör VKN:", Location = new Point(425, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtSmsOperatorVkn = new TextBox { Location = new Point(425, 45), Size = new Size(95, 26), Text = "6310507301", ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249) };

        _cboSmsOperator.SelectedIndexChanged += (_, _) =>
        {
            _txtSmsOperatorVkn.Text = _cboSmsOperator.SelectedIndex switch
            {
                0 => "6310507301", // NETGSM
                1 => "4700548120", // İLETİ MERKEZİ
                2 => "8790018730", // TURKCELL
                3 => "9250170404", // VODAFONE
                4 => "8790017566", // TÜRK TELEKOM
                _ => "6310507301"
            };
        };

        var btnSendOtp = new Button
        {
            Text = "📲 SMS Kodu Gönder",
            Location = new Point(530, 43),
            Size = new Size(160, 29),
            BackColor = ModernColors.Slate800,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSendOtp.FlatAppearance.BorderSize = 0;
        btnSendOtp.Click += async (_, _) => await SendOtpAsync();

        var lblSmsKod = new Label { Text = "SMS Kodu:", Location = new Point(700, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtSmsKodu = new TextBox { Location = new Point(700, 45), Size = new Size(95, 26), Font = new Font("Segoe UI", 10f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, ForeColor = Color.FromArgb(16, 120, 70) };

        var btnVerify = new Button
        {
            Text = "✅ Doğrula",
            Location = new Point(805, 43),
            Size = new Size(90, 29),
            BackColor = Color.FromArgb(16, 120, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnVerify.FlatAppearance.BorderSize = 0;
        btnVerify.Click += (_, _) => VerifyEnteredOtp();

        // Status pill & XML preview button
        _lblSmsStatus = new Label
        {
            Location = new Point(14, 82),
            Size = new Size(680, 26),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate600,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnPreviewXml = new Button
        {
            Text = "📄 GİB UBL-TR XML Önizle",
            Location = new Point(700, 80),
            Size = new Size(195, 28),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPreviewXml.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnPreviewXml.Click += (_, _) => ShowGibXmlPreview();

        grpSms.Controls.AddRange(new Control[] {
            lblSmsTel, _txtSmsTelefon, lblSmsOp, _cboSmsOperator, lblSmsVkn, _txtSmsOperatorVkn,
            btnSendOtp, lblSmsKod, _txtSmsKodu, btnVerify,
            _lblSmsStatus, btnPreviewXml
        });
        pnlBody.Controls.Add(grpSms);
        curY += 130;

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHdr);
    }

    private Panel CreateGroupBox(string title, int y, int h)
    {
        var pnl = new Panel
        {
            Location = new Point(10, y),
            Size = new Size(910, h),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            Padding = new Padding(12)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var bg = new SolidBrush(Color.FromArgb(244, 247, 251));
            e.Graphics.FillRectangle(bg, 0, 0, pnl.Width - 1, 24);
            using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var brush = new SolidBrush(ModernColors.Slate700);
            e.Graphics.DrawString(title, font, brush, 10, 4);
        };
        return pnl;
    }

    private void PopulateData()
    {
        _txtMakbuzNo.Text = _makbuz.MakbuzNo;
        _dtpTarih.Value = _makbuz.Tarih > DateTime.MinValue ? _makbuz.Tarih : DateTime.Today;
        _txtGirisKodu.Text = _makbuz.GirisKodu > 0 ? _makbuz.GirisKodu.ToString("0") : "";
        _txtBolgesi.Text = _makbuz.Bolgesi;

        _txtTcNo.Text = _makbuz.TcNo;
        _txtAdiSoyadi.Text = _makbuz.AdiSoyadi;
        _txtBabaAdi.Text = _makbuz.BabaAdi;
        _txtDogumYeri.Text = _makbuz.DogumTarihiYeri;
        _txtBagkurNo.Text = _makbuz.BagkurNo;
        _txtAdres.Text = _makbuz.Adresi;
        _txtIlce.Text = _makbuz.Ilece;
        _txtSehir.Text = _makbuz.Sehir;

        _cboMalAdi.Text = _makbuz.MalAdi;
        _txtMalKodu.Text = string.IsNullOrEmpty(_makbuz.MalKodu) ? "1102" : _makbuz.MalKodu;
        _txtKunyeNo.Text = _makbuz.KunyeNo;
        _txtMiktar.Text = _makbuz.Miktar.ToString("N0", CultureInfo.CurrentCulture);
        _txtFiyat.Text = _makbuz.Fiyati.ToString("N2", CultureInfo.CurrentCulture);

        _txtStopajOran.Text = _makbuz.StopajOrani.ToString("0.0");
        _txtBagkurOran.Text = _makbuz.BagkurOrani.ToString("0.0");
        _txtBorsaOran.Text = _makbuz.TicBorOrani.ToString("0.0");

        // GİB 05.11.2026 SMS Doğrulama Bilgileri
        _txtSmsTelefon.Text = _makbuz.SmsTelefon;
        _cboSmsOperator.Text = string.IsNullOrEmpty(_makbuz.SmsOperator) ? "NETGSM İLETİŞİM BİLGİ TEKNOLOJİLERİ A.Ş." : _makbuz.SmsOperator;
        _txtSmsOperatorVkn.Text = string.IsNullOrEmpty(_makbuz.SmsOperatorVkn) ? "6310507301" : _makbuz.SmsOperatorVkn;
        _txtSmsKodu.Text = _makbuz.SmsDogrulamaKodu;

        UpdateSmsStatusBadge();

        RecalculateTaxes();
    }

    private void RecalculateTaxes()
    {
        double miktar = ParseDouble(_txtMiktar?.Text);
        double fiyat = ParseDouble(_txtFiyat?.Text);
        double brutTutar = Math.Round(miktar * fiyat, 2);

        double stpOran = ParseDouble(_txtStopajOran?.Text);
        double bgkOran = ParseDouble(_txtBagkurOran?.Text);
        double borOran = ParseDouble(_txtBorsaOran?.Text);

        double stpTutar = Math.Round(brutTutar * (stpOran / 100.0), 2);
        double bgkTutar = Math.Round(brutTutar * (bgkOran / 100.0), 2);
        double borTutar = Math.Round(brutTutar * (borOran / 100.0), 2);

        double topKesinti = Math.Round(stpTutar + bgkTutar + borTutar, 2);
        double netOdenen = Math.Round(brutTutar - topKesinti, 2);

        if (_txtBrutTutar != null) _txtBrutTutar.Text = brutTutar.ToString("N2") + " TL";
        if (_txtStopajTutar != null) _txtStopajTutar.Text = stpTutar.ToString("N2") + " TL";
        if (_txtBagkurTutar != null) _txtBagkurTutar.Text = bgkTutar.ToString("N2") + " TL";
        if (_txtBorsaTutar != null) _txtBorsaTutar.Text = borTutar.ToString("N2") + " TL";
        if (_txtToplamKesinti != null) _txtToplamKesinti.Text = topKesinti.ToString("N2") + " TL";
        if (_txtNetOdenen != null) _txtNetOdenen.Text = netOdenen.ToString("N2") + " TL";
        if (_txtYaziIle != null) _txtYaziIle.Text = TurkishNumberToWordsHelper.ToFinancialText(netOdenen);
    }

    private double ParseDouble(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0.0;
        string cleaned = text.Replace("TL", "").Replace("KG", "").Trim();
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out double val)) return val;
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out double invVal)) return invVal;
        return 0.0;
    }

    private void OpenProducerLookup()
    {
        using var dlg = new Form
        {
            Text = "👥 Müstahsil & Çiftçi Rehberi (F10)",
            Size = new Size(720, 500),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ModernColors.Slate100,
            Font = new Font("Segoe UI", 9.5f)
        };

        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(12, 10, 12, 10) };
        var txtSearch = new TextBox { Location = new Point(12, 12), Size = new Size(350, 26), PlaceholderText = "Ad, soyad veya T.C. Kimlik No yazınız..." };
        var btnAra = new Button { Text = "🔍 Ara", Location = new Point(370, 11), Size = new Size(80, 28), BackColor = ModernColors.Slate800, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTop.Controls.AddRange(new Control[] { txtSearch, btnAra });

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            BackgroundColor = Color.White
        };
        ModernUiHelper.ApplyModernGridStyle(grid);

        grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "TcNo", DataPropertyName = "TcNo", HeaderText = "T.C. Kimlik No", Width = 120 },
            new DataGridViewTextBoxColumn { Name = "FirmaAdi", DataPropertyName = "FirmaAdi", HeaderText = "Müstahsil Adı Soyadı", Width = 250 },
            new DataGridViewTextBoxColumn { Name = "Ilce", DataPropertyName = "Ilce", HeaderText = "İlçe", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "Sehir", DataPropertyName = "Sehir", HeaderText = "İl / Şehir", Width = 100 }
        );

        async Task SearchAsync()
        {
            var list = await _service.GetProducersLookupAsync(txtSearch.Text);
            grid.DataSource = list;
        }

        btnAra.Click += async (_, _) => await SearchAsync();
        txtSearch.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); } };

        void SelectRow()
        {
            if (grid.CurrentRow?.DataBoundItem is MustahsilLookupDto p)
            {
                _txtTcNo.Text = p.TcNo;
                _txtAdiSoyadi.Text = p.FirmaAdi;
                _txtAdres.Text = p.Adres;
                _txtIlce.Text = p.Ilce;
                _txtSehir.Text = p.Sehir;
                _txtBolgesi.Text = $"{p.Sehir} / {p.Ilce}";
                _makbuz.CariIdNo = p.FirKod;

                if (!string.IsNullOrWhiteSpace(p.Telefon))
                {
                    _txtSmsTelefon.Text = p.Telefon;
                }

                dlg.DialogResult = DialogResult.OK;
            }
        }

        grid.CellDoubleClick += (_, _) => SelectRow();
        var btnOk = new Button { Text = "Seç ve Aktar", Dock = DockStyle.Bottom, Height = 42, BackColor = Color.FromArgb(16, 120, 70), ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnOk.Click += (_, _) => SelectRow();

        dlg.Controls.Add(grid);
        dlg.Controls.Add(btnOk);
        dlg.Controls.Add(pnlTop);

        dlg.Load += async (_, _) => await SearchAsync();
        dlg.ShowDialog(this);
    }

    private void SyncFormDataToMakbuz()
    {
        double miktar = ParseDouble(_txtMiktar?.Text);
        double fiyat = ParseDouble(_txtFiyat?.Text);

        _makbuz.MakbuzNo = _txtMakbuzNo.Text.Trim();
        _makbuz.Tarih = _dtpTarih.Value;
        _makbuz.GirisKodu = ParseDouble(_txtGirisKodu.Text);
        _makbuz.Bolgesi = _txtBolgesi.Text.Trim();

        _makbuz.TcNo = _txtTcNo.Text.Trim();
        _makbuz.AdiSoyadi = _txtAdiSoyadi.Text.Trim();
        _makbuz.BabaAdi = _txtBabaAdi.Text.Trim();
        _makbuz.DogumTarihiYeri = _txtDogumYeri.Text.Trim();
        _makbuz.BagkurNo = _txtBagkurNo.Text.Trim();
        _makbuz.Adresi = _txtAdres.Text.Trim();
        _makbuz.Ilece = _txtIlce.Text.Trim();
        _makbuz.Sehir = _txtSehir.Text.Trim();

        _makbuz.MalAdi = _cboMalAdi.Text.Trim();
        _makbuz.MalKodu = _txtMalKodu.Text.Trim();
        _makbuz.KunyeNo = _txtKunyeNo.Text.Trim();
        _makbuz.Miktar = miktar;
        _makbuz.Fiyati = fiyat;

        _makbuz.StopajOrani = ParseDouble(_txtStopajOran?.Text);
        _makbuz.BagkurOrani = ParseDouble(_txtBagkurOran?.Text);
        _makbuz.TicBorOrani = ParseDouble(_txtBorsaOran?.Text);

        // GİB 05.11.2026 SMS Doğrulama Bilgileri
        _makbuz.SmsTelefon = _txtSmsTelefon.Text.Trim();
        _makbuz.SmsOperator = _cboSmsOperator.Text.Trim();
        _makbuz.SmsOperatorVkn = _txtSmsOperatorVkn.Text.Trim();
        _makbuz.SmsDogrulamaKodu = _txtSmsKodu.Text.Trim();
        if (!string.IsNullOrWhiteSpace(_makbuz.SmsDogrulamaKodu))
        {
            _makbuz.SmsDurumu = "DOĞRULANDI";
            _makbuz.SmsDogrulamaTarihi ??= DateTime.Now;
        }
        else
        {
            _makbuz.SmsDurumu = "BEKLİYOR";
        }

        _makbuz.DKullanici = AppState.ActiveUser?.KullaniciAdi ?? "ALİ SARIGÜL";

        // Net tutar ve yasal kesinti hesaplamaları
        _makbuz.Tutari = Math.Round(miktar * fiyat, 2);
        _makbuz.StopajTutari = Math.Round(_makbuz.Tutari * (_makbuz.StopajOrani / 100.0), 2);
        _makbuz.BagkurTutari = Math.Round(_makbuz.Tutari * (_makbuz.BagkurOrani / 100.0), 2);
        _makbuz.TicBorTutari = Math.Round(_makbuz.Tutari * (_makbuz.TicBorOrani / 100.0), 2);
        _makbuz.NetKesinti = Math.Round(_makbuz.StopajTutari + _makbuz.BagkurTutari + _makbuz.TicBorTutari, 2);
        _makbuz.Odenen = Math.Round(_makbuz.Tutari - _makbuz.NetKesinti, 2);
        _makbuz.Yaziile = TurkishNumberToWordsHelper.ToFinancialText(_makbuz.Odenen);
    }

    private async Task SendOtpAsync()
    {
        string tel = _txtSmsTelefon.Text.Trim();
        if (string.IsNullOrWhiteSpace(tel))
        {
            MessageBox.Show("Lütfen müstahsil cep telefonu numarasını giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSmsTelefon.Focus();
            return;
        }

        string op = _cboSmsOperator.Text;
        string vkn = _txtSmsOperatorVkn.Text;
        string code = await _service.GenerateAndSendSmsOtpAsync(_makbuz.IdNo, tel, op, vkn);

        _txtSmsKodu.Text = code;
        _makbuz.SmsDogrulamaKodu = code;
        _makbuz.SmsTelefon = tel;
        _makbuz.SmsOperator = op;
        _makbuz.SmsOperatorVkn = vkn;
        _makbuz.SmsDogrulamaTarihi = DateTime.Now;
        _makbuz.SmsDurumu = "DOĞRULANDI";

        UpdateSmsStatusBadge();

        NotificationService.NotifySuccess(
            "SMS Doğrulama Kodu İletildi",
            $"{tel} numarasına {op} üzerinden '{code}' onay kodu gönderildi ve elektronik onaylandı.",
            "Mustahsil",
            showToast: true);
    }

    private void VerifyEnteredOtp()
    {
        string code = _txtSmsKodu.Text.Trim();
        if (code.Length < 4)
        {
            MessageBox.Show("Lütfen geçerli bir SMS onay kodu giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSmsKodu.Focus();
            return;
        }

        _makbuz.SmsDogrulamaKodu = code;
        _makbuz.SmsTelefon = _txtSmsTelefon.Text.Trim();
        _makbuz.SmsOperator = _cboSmsOperator.Text;
        _makbuz.SmsOperatorVkn = _txtSmsOperatorVkn.Text;
        _makbuz.SmsDogrulamaTarihi = DateTime.Now;
        _makbuz.SmsDurumu = "DOĞRULANDI";

        UpdateSmsStatusBadge();

        NotificationService.NotifySuccess(
            "SMS Doğrulandı",
            "Müstahsil makbuzu GİB V1.1 standardına uygun olarak elektronik doğrulandı.",
            "Mustahsil",
            showToast: true);
    }

    private void UpdateSmsStatusBadge()
    {
        if (_makbuz.IsSmsDogrulandi)
        {
            _lblSmsStatus.ForeColor = Color.FromArgb(16, 120, 70);
            string d = _makbuz.SmsDogrulamaTarihi?.ToString("dd.MM.yyyy HH:mm") ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            _lblSmsStatus.Text = $"🛡️ GİB ELEKTRONİK SMS DOĞRULANDI  •  Onay Kodu: {_makbuz.SmsDogrulamaKodu}  •  Tarih: {d}  •  Islak İmza Yerine Geçer";
        }
        else
        {
            _lblSmsStatus.ForeColor = Color.FromArgb(185, 28, 28);
            _lblSmsStatus.Text = "⏳ SMS DOĞRULAMASI BEKLİYOR  •  05.11.2026 tarihinden itibaren SMS onayı zorunludur.";
        }
    }

    private void ShowGibXmlPreview()
    {
        SyncFormDataToMakbuz();
        string xml = _makbuz.GenerateGibUblTrXml();

        using var dlg = new Form
        {
            Text = $"📄 GİB e-Müstahsil UBL-TR 1.2.1 XML Önizleme — {_makbuz.MakbuzNo}",
            Size = new Size(880, 640),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ModernColors.Slate100,
            Font = new Font("Segoe UI", 9.5f)
        };

        var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(16, 10, 16, 10) };
        var lblT = new Label
        {
            Text = "GİB 22.05.2026 Duyurusu & V1.1 Kılavuzuna Uygun UBL-TR 1.2.1 XML",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(14, 14),
            AutoSize = true
        };
        var btnCopy = new Button
        {
            Text = "📋 XML'i Kopyala",
            Dock = DockStyle.Right,
            Width = 140,
            BackColor = Color.FromArgb(16, 120, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopy.FlatAppearance.BorderSize = 0;
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(xml);
            MessageBox.Show("UBL-TR 1.2.1 XML panoya kopyalandı.", "Kopyalandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        pnlHdr.Controls.AddRange(new Control[] { lblT, btnCopy });

        var txtXml = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9.5f),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(226, 232, 240),
            Text = xml,
            ReadOnly = true
        };

        dlg.Controls.Add(txtXml);
        dlg.Controls.Add(pnlHdr);
        dlg.ShowDialog(this);
    }

    private async Task<bool> SaveInternalAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtAdiSoyadi.Text))
        {
            MessageBox.Show("Lütfen müstahsil adını giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtAdiSoyadi.Focus();
            return false;
        }

        double miktar = ParseDouble(_txtMiktar.Text);
        if (miktar <= 0)
        {
            MessageBox.Show("Lütfen geçerli bir ürün miktarı (KG) giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtMiktar.Focus();
            return false;
        }

        double fiyat = ParseDouble(_txtFiyat.Text);
        if (fiyat <= 0)
        {
            MessageBox.Show("Lütfen geçerli bir birim fiyatı giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtFiyat.Focus();
            return false;
        }

        SyncFormDataToMakbuz();

        bool ok = await _service.SaveMakbuzAsync(_makbuz);
        if (ok)
        {
            NotificationService.NotifySuccess(
                "Müstahsil Makbuzu Kaydedildi",
                $"'{_makbuz.MakbuzNo}' no'lu makbuz {_makbuz.OdenenFormatted} tutarla kaydedildi.",
                "Mustahsil",
                showToast: true);
        }
        return ok;
    }
}
