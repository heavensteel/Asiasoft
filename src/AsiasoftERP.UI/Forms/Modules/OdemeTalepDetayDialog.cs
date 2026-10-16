using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class OdemeTalepDetayDialog : Form
{
    private readonly OdemeTalepService _service = new();
    private readonly OdemeTalepDto _model;
    private readonly bool _isReadOnly;
    private readonly bool _isNew;

    // Controls
    private TextBox _txtFisNo = null!;
    private DateTimePicker _dtpFisTar = null!;
    private ComboBox _cmbSube = null!;
    private ComboBox _cmbGrupSirket = null!;
    private ComboBox _cmbGonderen = null!;
    private ComboBox _cmbTuru = null!;
    private TextBox _txtKasaAdi = null!;
    private TextBox _txtFirKod = null!;
    private TextBox _txtFirAdi = null!;
    private Button _btnCariRehber = null!;
    private TextBox _txtFaturaNo = null!;
    private NumericUpDown _numTalepTutar = null!;
    private ComboBox _cmbBagkur = null!;
    private NumericUpDown _numBagkurOrani = null!;
    private Label _lblBagkurTutar = null!;
    private Label _lblNetTutar = null!;
    private ComboBox _cmbDurum = null!;
    private TextBox _txtBankaIban = null!;
    private TextBox _txtAciklama = null!;
    private Label _lblIslemTarihi = null!;
    private Label _lblKullanici = null!;

    public bool IsSaved { get; private set; }

    public OdemeTalepDetayDialog(OdemeTalepDto model, bool isReadOnly = false, bool isNew = false)
    {
        _model = model;
        _isReadOnly = isReadOnly;
        _isNew = isNew;

        InitializeComponents();
        LoadModelToControls();
    }

    private void InitializeComponents()
    {
        this.Text = _isNew ? "Yeni Ödeme Talebi" : (_isReadOnly ? "Ödeme Talebi İncele" : "Ödeme Talebi Değiştir / Düzenle");
        this.Size = new Size(780, 720);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9.5f);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblTitle = new Label
        {
            Text = _isNew ? "YENİ ÖDEME TALEBİ OLUŞTUR" : $"ÖDEME TALEBİ DETAYI - FİŞ NO: {_model.FisNo}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 12),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "MacroSoft ERP Banka & Kasa Ödeme Talebi Kaydı",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(22, 36),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // Content Panel (Scrollable)
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(25, 15, 25, 15),
            AutoScroll = true
        };

        int y = 15;

        // Group 1: Temel Bilgiler
        AddSectionHeader(pnlContent, "TEMEL FİŞ VE ŞUBE BİLGİLERİ", ref y);

        // Row 1: Fiş No & Tarih & İşlem Tarihi
        AddLabel(pnlContent, "Fiş No:", 25, y);
        _txtFisNo = new TextBox { Location = new Point(130, y - 2), Width = 120, ReadOnly = !_isNew, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        
        AddLabel(pnlContent, "Tarih:", 270, y);
        _dtpFisTar = new DateTimePicker { Location = new Point(330, y - 2), Width = 130, Format = DateTimePickerFormat.Short, Enabled = !_isReadOnly };

        AddLabel(pnlContent, "İşlem Tarihi:", 480, y);
        _lblIslemTarihi = new Label { Location = new Point(570, y), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };
        pnlContent.Controls.AddRange(new Control[] { _txtFisNo, _dtpFisTar, _lblIslemTarihi });
        y += 35;

        // Row 2: Şube / Yetki Kodu & Grup Şirket & Gönderen Hesap
        AddLabel(pnlContent, "İş Yeri / Şube:", 25, y);
        _cmbSube = new ComboBox { Location = new Point(130, y - 2), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !_isReadOnly };
        _cmbSube.Items.AddRange(new object[] { "ALAŞEHİR", "SEKİ", "BURSA", "DENİZLİ", "TARSUS", "MERKEZ", "MUHASEBE MASRAF", "ALSELEVIA", "LAREX" });

        AddLabel(pnlContent, "Grup Şirket:", 340, y);
        _cmbGrupSirket = new ComboBox { Location = new Point(430, y - 2), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !_isReadOnly };
        _cmbGrupSirket.Items.AddRange(new object[] { "TEK ASYA", "LAREX", "AL SELEVIA" });

        AddLabel(pnlContent, "Gönderen:", 585, y);
        _cmbGonderen = new ComboBox { Location = new Point(655, y - 2), Width = 80, Enabled = !_isReadOnly };
        _cmbGonderen.Items.AddRange(new object[] { "TEK ASYA", "LAREX", "AL SELEVIA" });
        pnlContent.Controls.AddRange(new Control[] { _cmbSube, _cmbGrupSirket, _cmbGonderen });
        y += 35;

        // Row 3: Türü & Kasa Adı
        AddLabel(pnlContent, "Ödeme Türü:", 25, y);
        _cmbTuru = new ComboBox { Location = new Point(130, y - 2), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !_isReadOnly };
        _cmbTuru.Items.AddRange(new object[] { "AVANS", "HAVALE", "BANKADA NAKİT", "ŞUBEDEN NAKİT", "EFT", "SENET", "ÇEK" });

        AddLabel(pnlContent, "Kasa Adı:", 340, y);
        _txtKasaAdi = new TextBox { Location = new Point(430, y - 2), Width = 305, ReadOnly = _isReadOnly };
        pnlContent.Controls.AddRange(new Control[] { _cmbTuru, _txtKasaAdi });
        y += 45;

        // Group 2: Cari & Finansal Detaylar
        AddSectionHeader(pnlContent, "CARİ VE TUTAR BİLGİLERİ", ref y);

        // Row 4: Cari Kodu & Firma Ünvanı & Rehber
        AddLabel(pnlContent, "Cari Hesap:", 25, y);
        _txtFirKod = new TextBox { Location = new Point(130, y - 2), Width = 90, ReadOnly = _isReadOnly };
        _txtFirAdi = new TextBox { Location = new Point(225, y - 2), Width = 380, ReadOnly = _isReadOnly, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _btnCariRehber = new Button
        {
            Text = "Rehber (F10)",
            Location = new Point(615, y - 3),
            Size = new Size(120, 27),
            BackColor = Color.FromArgb(238, 242, 246),
            FlatStyle = FlatStyle.Flat,
            Enabled = !_isReadOnly
        };
        _btnCariRehber.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnCariRehber.Click += (_, _) => OpenCariRehber();
        pnlContent.Controls.AddRange(new Control[] { _txtFirKod, _txtFirAdi, _btnCariRehber });
        y += 35;

        // Row 5: Belge / Fatura No & IBAN
        AddLabel(pnlContent, "Fatura/Belge No:", 25, y);
        _txtFaturaNo = new TextBox { Location = new Point(130, y - 2), Width = 180, ReadOnly = _isReadOnly };

        AddLabel(pnlContent, "Banka IBAN:", 340, y);
        _txtBankaIban = new TextBox { Location = new Point(430, y - 2), Width = 305, ReadOnly = _isReadOnly };
        pnlContent.Controls.AddRange(new Control[] { _txtFaturaNo, _txtBankaIban });
        y += 35;

        // Row 6: Talep Tutarı & Bağkur Kesinti Var mı?
        AddLabel(pnlContent, "Talep Tutarı:", 25, y);
        _numTalepTutar = new NumericUpDown
        {
            Location = new Point(130, y - 2),
            Width = 180,
            Maximum = 1000000000m,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Enabled = !_isReadOnly
        };
        _numTalepTutar.ValueChanged += (_, _) => RecalculateTotals();

        AddLabel(pnlContent, "Bağkur Kesinti ?:", 340, y);
        _cmbBagkur = new ComboBox { Location = new Point(450, y - 2), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !_isReadOnly };
        _cmbBagkur.Items.AddRange(new object[] { "HAYIR", "EVET" });
        _cmbBagkur.SelectedIndexChanged += (_, _) =>
        {
            if (_cmbBagkur.SelectedItem?.ToString() == "EVET" && _numBagkurOrani.Value == 0)
            {
                _numBagkurOrani.Value = 2.0m;
            }
            RecalculateTotals();
        };

        AddLabel(pnlContent, "Oran (%):", 560, y);
        _numBagkurOrani = new NumericUpDown
        {
            Location = new Point(630, y - 2),
            Width = 70,
            Maximum = 100,
            DecimalPlaces = 2,
            Enabled = !_isReadOnly
        };
        _numBagkurOrani.ValueChanged += (_, _) => RecalculateTotals();
        pnlContent.Controls.AddRange(new Control[] { _numTalepTutar, _cmbBagkur, _numBagkurOrani });
        y += 38;

        // Row 7: Bağkur Tutarı & Net Ödenecek Tutar (Prominent Banner)
        var pnlTotals = new Panel
        {
            Location = new Point(25, y),
            Size = new Size(710, 45),
            BackColor = Color.FromArgb(236, 253, 245),
            BorderStyle = BorderStyle.FixedSingle
        };

        _lblBagkurTutar = new Label
        {
            Text = "Bağkur Kesintisi: 0,00 TL",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(185, 28, 28),
            Location = new Point(15, 12),
            AutoSize = true
        };

        _lblNetTutar = new Label
        {
            Text = "Net Ödenecek Tutar: 0,00 TL",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(6, 95, 70),
            Location = new Point(340, 11),
            AutoSize = true
        };

        pnlTotals.Controls.AddRange(new Control[] { _lblBagkurTutar, _lblNetTutar });
        pnlContent.Controls.Add(pnlTotals);
        y += 60;

        // Group 3: Durum ve Açıklama
        AddSectionHeader(pnlContent, "ONAY, DURUM VE AÇIKLAMA", ref y);

        // Row 8: Durum & Ekleyen Kullanıcı
        AddLabel(pnlContent, "Talep Durumu:", 25, y);
        _cmbDurum = new ComboBox { Location = new Point(130, y - 2), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !_isReadOnly };
        _cmbDurum.Items.AddRange(new object[] { "Devam Ediyor", "Onaylandı", "Aktarildi", "RET", "Ödendi" });

        AddLabel(pnlContent, "Kullanıcı / Onaylayan:", 340, y);
        _lblKullanici = new Label { Location = new Point(480, y), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };
        pnlContent.Controls.AddRange(new Control[] { _cmbDurum, _lblKullanici });
        y += 35;

        // Row 9: Açıklama
        AddLabel(pnlContent, "Açıklama:", 25, y);
        _txtAciklama = new TextBox
        {
            Location = new Point(130, y - 2),
            Size = new Size(605, 55),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = _isReadOnly
        };
        pnlContent.Controls.Add(_txtAciklama);
        y += 70;

        // Footer Action Panel
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 12, 20, 12)
        };

        var btnClose = new Button
        {
            Text = "Kapat",
            Size = new Size(95, 34),
            Location = new Point(pnlFooter.Width - 115, 13),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            BackColor = Color.FromArgb(241, 245, 249),
            FlatStyle = FlatStyle.Flat
        };
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnClose.Click += (_, _) => this.Close();

        pnlFooter.Controls.Add(btnClose);

        if (_isReadOnly)
        {
            // Quick Status Actions in ReadOnly mode:
            var btnRet = new Button
            {
                Text = "❌ Reddet",
                Size = new Size(110, 34),
                Location = new Point(20, 13),
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(153, 27, 27),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnRet.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);
            btnRet.Click += async (_, _) => await ChangeStatusQuickly("RET");

            var btnOnayla = new Button
            {
                Text = "✓ Onayla",
                Size = new Size(110, 34),
                Location = new Point(140, 13),
                BackColor = Color.FromArgb(220, 252, 231),
                ForeColor = Color.FromArgb(22, 101, 52),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnOnayla.FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);
            btnOnayla.Click += async (_, _) => await ChangeStatusQuickly("Onaylandı");

            pnlFooter.Controls.AddRange(new Control[] { btnRet, btnOnayla });
        }
        else
        {
            var btnSave = new Button
            {
                Text = "💾 Kaydet",
                Size = new Size(120, 34),
                Location = new Point(pnlFooter.Width - 245, 13),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (_, _) => await SaveAsync();
            pnlFooter.Controls.Add(btnSave);
        }

        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);
    }

    private void AddSectionHeader(Panel parent, string title, ref int y)
    {
        var lbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(25, y),
            AutoSize = true
        };
        parent.Controls.Add(lbl);
        y += 24;
    }

    private void AddLabel(Panel parent, string text, int x, int y)
    {
        var lbl = new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(51, 65, 85)
        };
        parent.Controls.Add(lbl);
    }

    private void LoadModelToControls()
    {
        _txtFisNo.Text = _model.FisNo > 0 ? _model.FisNo.ToString() : "";
        _dtpFisTar.Value = _model.FisTar ?? DateTime.Today;
        _lblIslemTarihi.Text = _model.IslemTarihi.HasValue ? _model.IslemTarihi.Value.ToString("dd.MM.yyyy HH:mm:ss") : "-";

        if (!string.IsNullOrWhiteSpace(_model.YetkiKodu))
        {
            int idx = _cmbSube.FindStringExact(_model.YetkiKodu);
            if (idx >= 0) _cmbSube.SelectedIndex = idx;
            else _cmbSube.Text = _model.YetkiKodu;
        }
        else _cmbSube.SelectedIndex = 0;

        if (!string.IsNullOrWhiteSpace(_model.GrupSirket))
        {
            int idx = _cmbGrupSirket.FindStringExact(_model.GrupSirket);
            if (idx >= 0) _cmbGrupSirket.SelectedIndex = idx;
            else _cmbGrupSirket.Text = _model.GrupSirket;
        }
        else _cmbGrupSirket.SelectedIndex = 0;

        if (!string.IsNullOrWhiteSpace(_model.GonderenHesap))
        {
            _cmbGonderen.Text = _model.GonderenHesap;
        }
        else _cmbGonderen.Text = "TEK ASYA";

        if (!string.IsNullOrWhiteSpace(_model.Turu))
        {
            int idx = _cmbTuru.FindStringExact(_model.Turu);
            if (idx >= 0) _cmbTuru.SelectedIndex = idx;
            else _cmbTuru.Text = _model.Turu;
        }
        else _cmbTuru.SelectedIndex = 0;

        _txtKasaAdi.Text = _model.KasaAdi;
        _txtFirKod.Text = _model.FirKod > 0 ? _model.FirKod.ToString() : "";
        _txtFirAdi.Text = _model.FirAdi;
        _txtFaturaNo.Text = _model.BelgeVeyaFaturaNo;
        _txtBankaIban.Text = _model.BankaIban;
        _numTalepTutar.Value = _model.TalepTutar;

        int bagkurIdx = _cmbBagkur.FindStringExact(_model.BagkurVarmi);
        _cmbBagkur.SelectedIndex = bagkurIdx >= 0 ? bagkurIdx : 0;
        _numBagkurOrani.Value = _model.BagkurOrani;

        int durumIdx = _cmbDurum.FindStringExact(_model.Durum);
        _cmbDurum.SelectedIndex = durumIdx >= 0 ? durumIdx : 0;

        _lblKullanici.Text = $"{_model.Kullanici} {(!string.IsNullOrEmpty(_model.OnayKullanici) ? $"(Onay: {_model.OnayKullanici})" : "")}";
        _txtAciklama.Text = _model.Aciklama;

        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        decimal talep = _numTalepTutar.Value;
        decimal oran = _numBagkurOrani.Value;
        bool bagkurVar = _cmbBagkur.SelectedItem?.ToString() == "EVET";

        decimal bagkurTutar = bagkurVar && oran > 0 ? Math.Round(talep * (oran / 100m), 2) : 0m;
        decimal netTutar = talep - bagkurTutar;

        _lblBagkurTutar.Text = $"Bağkur Kesintisi: {bagkurTutar:N2} TL";
        _lblNetTutar.Text = $"Net Ödenecek Tutar: {netTutar:N2} TL";
    }

    private void OpenCariRehber()
    {
        using var rehber = new CariRehberForm();
        if (rehber.ShowDialog() == DialogResult.OK && rehber.SelectedCari != null)
        {
            _txtFirKod.Text = rehber.SelectedCari.FirKod.ToString();
            _txtFirAdi.Text = rehber.SelectedCari.FirmaAdi;
        }
    }

    private async Task ChangeStatusQuickly(string newStatus)
    {
        if (_model.IdNo <= 0) return;

        string currentUser = AppState.CurrentUser?.FullName ?? "Admin";
        bool ok = await _service.UpdateDurumAsync(_model.IdNo, newStatus, currentUser);
        if (ok)
        {
            _model.Durum = newStatus;
            _model.OnayKullanici = currentUser;
            _model.OnayTarihi = DateTime.Now;
            IsSaved = true;
            MessageBox.Show($"Talep durumu başarıyla '{newStatus}' olarak güncellendi.", "Durum Güncellendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
        else
        {
            MessageBox.Show("Durum güncellenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtFirAdi.Text))
        {
            MessageBox.Show("Lütfen Cari Hesap Ünvanını belirtiniz.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtFirAdi.Focus();
            return;
        }

        if (_numTalepTutar.Value <= 0)
        {
            MessageBox.Show("Talep tutarı sıfırdan büyük olmalıdır.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _numTalepTutar.Focus();
            return;
        }

        double.TryParse(_txtFisNo.Text, out double fisNo);
        double.TryParse(_txtFirKod.Text, out double firKod);

        _model.FisNo = fisNo;
        _model.FisTar = _dtpFisTar.Value.Date;
        _model.YetkiKodu = _cmbSube.SelectedItem?.ToString() ?? _cmbSube.Text;
        _model.GrupSirket = _cmbGrupSirket.SelectedItem?.ToString() ?? _cmbGrupSirket.Text;
        _model.GonderenHesap = _cmbGonderen.SelectedItem?.ToString() ?? _cmbGonderen.Text;
        _model.Turu = _cmbTuru.SelectedItem?.ToString() ?? _cmbTuru.Text;
        _model.KasaAdi = _txtKasaAdi.Text.Trim();
        _model.FirKod = firKod;
        _model.FirAdi = _txtFirAdi.Text.Trim();
        _model.FaturaNo = _txtFaturaNo.Text.Trim();
        _model.BelgeNo = _txtFaturaNo.Text.Trim();
        _model.BankaIban = _txtBankaIban.Text.Trim();
        _model.TalepTutar = _numTalepTutar.Value;
        _model.BagkurVarmi = _cmbBagkur.SelectedItem?.ToString() ?? "HAYIR";
        _model.BagkurOrani = _numBagkurOrani.Value;
        _model.BagkurTutar = _model.BagkurVarmi == "EVET" && _model.BagkurOrani > 0 
            ? Math.Round(_model.TalepTutar * (_model.BagkurOrani / 100m), 2) 
            : 0m;
        _model.Durum = _cmbDurum.SelectedItem?.ToString() ?? "Devam Ediyor";
        _model.Aciklama = _txtAciklama.Text.Trim();
        _model.Kullanici = AppState.CurrentUser?.FullName ?? "Admin";

        bool ok = await _service.SaveOdemeTalepAsync(_model);
        if (ok)
        {
            IsSaved = true;
            MessageBox.Show("Ödeme talebi başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
        else
        {
            MessageBox.Show("Ödeme talebi kaydedilirken bir hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
