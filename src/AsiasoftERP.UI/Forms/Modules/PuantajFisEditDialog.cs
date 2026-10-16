using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;

namespace AsiasoftERP.UI.Forms.Modules;

public class PuantajFisEditDialog : Form
{
    public PuantajFisDto Fis { get; private set; }
    private readonly PuantajService _puantajService = new();

    private ComboBox _cmbIsYeri = null!;
    private ComboBox _cmbBolge = null!;
    private ComboBox _cmbAy = null!;
    private NumericUpDown _numYil = null!;
    private NumericUpDown _numBelgeNo = null!;
    private DateTimePicker _dtpBaslangic = null!;
    private DateTimePicker _dtpBitis = null!;
    private NumericUpDown _numServisSayisi = null!;
    private NumericUpDown _numServisFiyati = null!;
    private TextBox _txtAciklama = null!;
    private Button _btnKaydet = null!;
    private Button _btnIptal = null!;

    public PuantajFisEditDialog(PuantajFisDto? existingFis = null)
    {
        Fis = existingFis ?? new PuantajFisDto
        {
            Yil = DateTime.Today.Year,
            Ay = DateTime.Today.ToString("MMMM", new CultureInfo("tr-TR")).ToUpper(new CultureInfo("tr-TR")),
            BTarih = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            STarih = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)),
            TgAdi = "MERKEZ AMELE",
            BolgeAdi = "BALLIÖZ",
            ServisSayisi = 0,
            ServisFiyati = 0
        };

        InitializeComponent();
        LoadLookups();
        PopulateFields();
    }

    private void InitializeComponent()
    {
        Text = Fis.FisNo > 0 ? $"Puantaj Fişi Düzenle - Fiş #{Fis.FisNo}" : "Yeni Puantaj Fişi Oluştur";
        Size = new Size(580, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Segoe UI", 9.5f);

        // Header panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(20, 12, 20, 12)
        };
        var lblHeader = new Label
        {
            Text = Fis.FisNo > 0 ? "📋 Puantaj Fişi Bilgileri" : "📋 Yeni Puantaj Fişi",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "İş yeri, servis bölgesi, çalışma ayı ve servis maliyet parametreleri",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(20, 34),
            AutoSize = true
        };
        pnlHeader.Controls.AddRange(new Control[] { lblHeader, lblSub });

        // Content panel
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(25, 20, 25, 20)
        };

        int y = 15;
        int labelWidth = 140;
        int inputWidth = 360;

        // İş Yeri / Şube
        AddRow(pnlContent, "İş Yeri / Şube:", ref y, labelWidth, out _cmbIsYeri, inputWidth);
        _cmbIsYeri.DropDownStyle = ComboBoxStyle.DropDown;

        // Servis Bölgesi
        AddRow(pnlContent, "Servis Bölgesi:", ref y, labelWidth, out _cmbBolge, inputWidth);
        _cmbBolge.DropDownStyle = ComboBoxStyle.DropDown;

        // Ay ve Yıl (yan yana)
        var lblAy = new Label { Text = "Dönem (Ay / Yıl):", Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _cmbAy = new ComboBox { Location = new Point(25 + labelWidth, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbAy.Items.AddRange(new object[] { "OCAK", "ŞUBAT", "MART", "NİSAN", "MAYIS", "HAZİRAN", "TEMMUZ", "AĞUSTOS", "EYLÜL", "EKİM", "KASIM", "ARALIK" });
        _numYil = new NumericUpDown { Location = new Point(25 + labelWidth + 190, y), Width = 170, Minimum = 2020, Maximum = 2035, Value = DateTime.Today.Year };
        pnlContent.Controls.AddRange(new Control[] { lblAy, _cmbAy, _numYil });
        y += 40;

        // Tarih Aralığı
        var lblTarih = new Label { Text = "Tarih Aralığı:", Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59) };
        _dtpBaslangic = new DateTimePicker { Location = new Point(25 + labelWidth, y), Width = 175, Format = DateTimePickerFormat.Short };
        _dtpBitis = new DateTimePicker { Location = new Point(25 + labelWidth + 185, y), Width = 175, Format = DateTimePickerFormat.Short };
        pnlContent.Controls.AddRange(new Control[] { lblTarih, _dtpBaslangic, _dtpBitis });
        y += 40;

        // Belge No
        AddNumericRow(pnlContent, "Belge No:", ref y, labelWidth, out _numBelgeNo, inputWidth, 0, 999999);

        // Servis Sayısı
        AddNumericRow(pnlContent, "Servis Sayısı (Tur):", ref y, labelWidth, out _numServisSayisi, inputWidth, 0, 999, 1);

        // Servis Fiyatı
        AddNumericRow(pnlContent, "Servis Fiyatı (₺):", ref y, labelWidth, out _numServisFiyati, inputWidth, 0, 1000000, 2);

        // Açıklama
        var lblAciklama = new Label { Text = "Açıklama / Not:", Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59) };
        _txtAciklama = new TextBox { Location = new Point(25 + labelWidth, y), Width = inputWidth, Height = 55, Multiline = true };
        pnlContent.Controls.AddRange(new Control[] { lblAciklama, _txtAciklama });
        y += 65;

        // Bottom action buttons panel
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        _btnKaydet = new Button
        {
            Text = "💾 Kaydet",
            DialogResult = DialogResult.OK,
            Size = new Size(130, 38),
            Location = new Point(pnlBottom.Width - 280, 11),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnKaydet.FlatAppearance.BorderSize = 0;
        _btnKaydet.Click += OnSaveClick;

        _btnIptal = new Button
        {
            Text = "İptal",
            DialogResult = DialogResult.Cancel,
            Size = new Size(100, 38),
            Location = new Point(pnlBottom.Width - 130, 11),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        _btnIptal.FlatAppearance.BorderSize = 0;

        pnlBottom.Controls.AddRange(new Control[] { _btnKaydet, _btnIptal });

        Controls.AddRange(new Control[] { pnlContent, pnlBottom, pnlHeader });

        AcceptButton = _btnKaydet;
        CancelButton = _btnIptal;
    }

    private void AddRow(Panel parent, string labelText, ref int y, int labelWidth, out ComboBox combo, int inputWidth)
    {
        var lbl = new Label { Text = labelText, Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59) };
        combo = new ComboBox { Location = new Point(25 + labelWidth, y), Width = inputWidth };
        parent.Controls.AddRange(new Control[] { lbl, combo });
        y += 40;
    }

    private void AddNumericRow(Panel parent, string labelText, ref int y, int labelWidth, out NumericUpDown num, int inputWidth, decimal min, decimal max, int decimals = 0)
    {
        var lbl = new Label { Text = labelText, Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59) };
        num = new NumericUpDown
        {
            Location = new Point(25 + labelWidth, y),
            Width = inputWidth,
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            ThousandsSeparator = true
        };
        parent.Controls.AddRange(new Control[] { lbl, num });
        y += 40;
    }

    private async void LoadLookups()
    {
        try
        {
            var isyerleri = await _puantajService.GetIsyerleriAsync();
            _cmbIsYeri.Items.Clear();
            _cmbIsYeri.Items.AddRange(isyerleri.ToArray());

            var bolgeler = await _puantajService.GetBolgelerAsync();
            _cmbBolge.Items.Clear();
            _cmbBolge.Items.AddRange(bolgeler.ToArray());

            if (!string.IsNullOrWhiteSpace(Fis.TgAdi)) _cmbIsYeri.Text = Fis.TgAdi;
            if (!string.IsNullOrWhiteSpace(Fis.BolgeAdi)) _cmbBolge.Text = Fis.BolgeAdi;
        }
        catch { }
    }

    private void PopulateFields()
    {
        _cmbIsYeri.Text = Fis.TgAdi ?? "MERKEZ AMELE";
        _cmbBolge.Text = Fis.BolgeAdi ?? "BALLIÖZ";
        _cmbAy.SelectedItem = Fis.Ay ?? "EYLÜL";
        _numYil.Value = Fis.Yil > 2000 ? Fis.Yil : DateTime.Today.Year;
        _numBelgeNo.Value = Fis.FatNo;
        _numServisSayisi.Value = Fis.ServisSayisi;
        _numServisFiyati.Value = Fis.ServisFiyati;
        _txtAciklama.Text = Fis.Aciklama ?? string.Empty;

        if (Fis.BTarih.HasValue) _dtpBaslangic.Value = Fis.BTarih.Value;
        if (Fis.STarih.HasValue) _dtpBitis.Value = Fis.STarih.Value;
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_cmbIsYeri.Text))
        {
            MessageBox.Show("Lütfen iş yeri / şube bilgisini giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Fis.TgAdi = _cmbIsYeri.Text.Trim();
        Fis.BolgeAdi = _cmbBolge.Text.Trim();
        Fis.Ay = _cmbAy.SelectedItem?.ToString() ?? _cmbAy.Text.Trim();
        Fis.Yil = (int)_numYil.Value;
        Fis.FatNo = (int)_numBelgeNo.Value;
        Fis.BTarih = _dtpBaslangic.Value.Date;
        Fis.STarih = _dtpBitis.Value.Date;
        Fis.ServisSayisi = _numServisSayisi.Value;
        Fis.ServisFiyati = _numServisFiyati.Value;
        Fis.ServisTutari = Fis.ServisSayisi * Fis.ServisFiyati;
        Fis.KalanServisTutari = Fis.ServisTutari - Fis.ServisAvansi;
        Fis.Aciklama = _txtAciklama.Text.Trim();
    }
}
