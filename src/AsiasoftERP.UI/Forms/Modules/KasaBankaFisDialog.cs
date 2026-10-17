using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

public class KasaBankaFisDialog : Form
{
    private readonly KasaBankaService _service = new();
    private readonly FinansFisGirisDto _fis;
    private readonly bool _isNew;

    // Controls
    private ComboBox _cboIslemTipi = null!;
    private ComboBox _cboHesap = null!;
    private TextBox _txtFisNo = null!;
    private TextBox _txtMakbuzNo = null!;
    private DateTimePicker _dtpFisTar = null!;
    private DateTimePicker _dtpSaat = null!;

    private TextBox _txtCariKod = null!;
    private TextBox _txtCariAdi = null!;
    private TextBox _txtVergiDaire = null!;
    private TextBox _txtVergiNo = null!;

    private TextBox _txtTutar = null!;
    private ComboBox _cboDoviz = null!;
    private TextBox _txtKur = null!;
    private TextBox _txtBelgeNo = null!;
    private TextBox _txtAciklama = null!;
    private TextBox _txtYaziIle = null!;

    private List<KasaKartDto> _kasalar = new();
    private List<BankaHesapDto> _bankalar = new();

    public KasaBankaFisDialog(FinansFisGirisDto? existing = null, FinansIslemTipi defaultTip = FinansIslemTipi.KasaTahsilat)
    {
        this.DoubleBuffered = true;
        _isNew = existing == null;

        if (_isNew)
        {
            _fis = new FinansFisGirisDto
            {
                IslemTipi = defaultTip,
                FisTar = DateTime.Today,
                Saat = DateTime.Now.TimeOfDay,
                User = AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL"
            };
        }
        else
        {
            _fis = existing!;
        }

        InitializeComponents();
        this.Load += async (_, _) => await LoadDataToFormAsync();
    }

    private void InitializeComponents()
    {
        this.Text = _isNew ? "Yeni Kasa / Banka Fiş Girişi" : $"Finans Fişi Düzenle — #{_fis.FisNo}";
        this.Size = new Size(820, 640);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // TOP HEADER BANNER
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

        var lblHdrTitle = new Label
        {
            Text = "💰  Kasa & Banka Fiş Giriş Formu",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(16, 8),
            AutoSize = true
        };

        var lblHdrSub = new Label
        {
            Text = "Nakit tahsilat/tediye, banka gelen/gönderilen havale ve virman işlemleri",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(18, 36),
            AutoSize = true
        };
        pnlHdr.Controls.AddRange(new Control[] { lblHdrTitle, lblHdrSub });

        // BOTTOM ACTION BAR
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10)
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
            Location = new Point(pnlBottom.Width - 105, 12),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        var btnPrint = new Button
        {
            Text = "🖨️  Kaydet ve Yazdır",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(165, 38),
            Location = new Point(pnlBottom.Width - 280, 12),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += async (_, _) =>
        {
            if (await SaveInternalAsync())
            {
                PrintDocument();
                this.DialogResult = DialogResult.OK;
            }
        };

        var btnSave = new Button
        {
            Text = "💾  Kaydet",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(125, 38),
            Location = new Point(pnlBottom.Width - 415, 12),
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

        // CENTER BODY (3 Groups)
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 14, 20, 14),
            BackColor = ModernColors.Slate100,
            AutoScroll = true
        };

        int y = 10;

        // GROUP 1: İŞLEM VE HESAP BİLGİLERİ
        var grpHesap = CreateGroupBox("1. İŞLEM VE HESAP SEÇİMİ", y, 125, 780);
        
        var lblTip = new Label { Text = "İşlem Türü:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboIslemTipi = new ComboBox { Location = new Point(14, 48), Size = new Size(220, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboIslemTipi.Items.AddRange(new object[] { "Kasa Tahsilat Fişi (Nakit Giriş)", "Kasa Tediye Fişi (Nakit Çıkış)", "Banka Tahsilat (Gelen Havale)", "Banka Tediye (Gönderilen Havale)" });
        _cboIslemTipi.SelectedIndex = (int)_fis.IslemTipi;
        _cboIslemTipi.SelectedIndexChanged += (_, _) => OnIslemTipiChanged();

        var lblHesap = new Label { Text = "Kasa / Banka Hesabı:", Location = new Point(246, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboHesap = new ComboBox { Location = new Point(246, 48), Size = new Size(260, 26), DropDownStyle = ComboBoxStyle.DropDownList };

        var lblFis = new Label { Text = "Fiş / Dekont No:", Location = new Point(520, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtFisNo = new TextBox { Location = new Point(520, 48), Size = new Size(100, 26), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _txtMakbuzNo = new TextBox { Location = new Point(626, 48), Size = new Size(138, 26), PlaceholderText = "Dekont No" };

        var lblTar = new Label { Text = "İşlem Tarihi:", Location = new Point(14, 82), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _dtpFisTar = new DateTimePicker { Location = new Point(100, 80), Size = new Size(134, 26), Format = DateTimePickerFormat.Short };

        var lblSaat = new Label { Text = "Saat:", Location = new Point(246, 82), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _dtpSaat = new DateTimePicker { Location = new Point(286, 80), Size = new Size(95, 26), Format = DateTimePickerFormat.Time, ShowUpDown = true };

        grpHesap.Controls.AddRange(new Control[] { lblTip, _cboIslemTipi, lblHesap, _cboHesap, lblFis, _txtFisNo, _txtMakbuzNo, lblTar, _dtpFisTar, lblSaat, _dtpSaat });
        pnlBody.Controls.Add(grpHesap);
        y += 135;

        // GROUP 2: CARİ HESAP / ŞAHIS BİLGİLERİ
        var grpCari = CreateGroupBox("2. CARİ HESAP / MUHATAP BİLGİLERİ", y, 95, 780);

        var lblCari = new Label { Text = "Cari Hesap / Muhatap:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtCariKod = new TextBox { Location = new Point(14, 48), Size = new Size(80, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };
        _txtCariAdi = new TextBox { Location = new Point(98, 48), Size = new Size(270, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };

        var btnCariSec = new Button
        {
            Text = "🔍 Seç (F10)",
            Location = new Point(374, 47),
            Size = new Size(88, 28),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCariSec.FlatAppearance.BorderSize = 0;
        btnCariSec.Click += (_, _) => OpenCariRehberi();

        var lblVd = new Label { Text = "V.D. / VKN:", Location = new Point(480, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtVergiDaire = new TextBox { Location = new Point(480, 48), Size = new Size(130, 26) };
        _txtVergiNo = new TextBox { Location = new Point(618, 48), Size = new Size(146, 26) };

        grpCari.Controls.AddRange(new Control[] { lblCari, _txtCariKod, _txtCariAdi, btnCariSec, lblVd, _txtVergiDaire, _txtVergiNo });
        pnlBody.Controls.Add(grpCari);
        y += 105;

        // GROUP 3: TUTAR VE AÇIKLAMA DETAYLARI
        var grpTutar = CreateGroupBox("3. PARASAL TUTAR VE AÇIKLAMA", y, 175, 780);

        var lblTutar = new Label { Text = "İşlem Tutarı:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtTutar = new TextBox
        {
            Location = new Point(14, 48),
            Size = new Size(160, 30),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 120, 70),
            Text = "0,00"
        };
        _txtTutar.TextChanged += (_, _) => UpdateYaziIle();

        var lblDov = new Label { Text = "Para Birimi / Kur:", Location = new Point(184, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _cboDoviz = new ComboBox { Location = new Point(184, 48), Size = new Size(70, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboDoviz.Items.AddRange(new object[] { "TL", "USD", "EUR" });
        _cboDoviz.SelectedIndex = 0;

        _txtKur = new TextBox { Location = new Point(260, 48), Size = new Size(70, 26), Text = "1.0000" };

        var lblBelge = new Label { Text = "Referans / Belge No:", Location = new Point(344, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBelgeNo = new TextBox { Location = new Point(344, 48), Size = new Size(150, 26) };

        var lblYazi = new Label { Text = "Yalnız (Yazıyla):", Location = new Point(14, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtYaziIle = new TextBox { Location = new Point(14, 106), Size = new Size(750, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };

        var lblAck = new Label { Text = "Açıklama / Masraf Kategori Notu:", Location = new Point(14, 138), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtAciklama = new TextBox { Location = new Point(200, 136), Size = new Size(564, 26) };

        grpTutar.Controls.AddRange(new Control[] { lblTutar, _txtTutar, lblDov, _cboDoviz, _txtKur, lblBelge, _txtBelgeNo, lblYazi, _txtYaziIle, lblAck, _txtAciklama });
        pnlBody.Controls.Add(grpTutar);

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHdr);
    }

    private Panel CreateGroupBox(string title, int y, int h, int w = 780)
    {
        var pnl = new Panel
        {
            Location = new Point(10, y),
            Size = new Size(w, h),
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
            using var brushT = new SolidBrush(Color.FromArgb(30, 41, 59));
            using var fontT = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            e.Graphics.DrawString(title, fontT, brushT, 10, 4);
        };
        return pnl;
    }

    private async Task LoadDataToFormAsync()
    {
        try
        {
            _kasalar = await _service.GetKasaKartlariAsync();
            _bankalar = await _service.GetBankaHesaplariAsync();

            OnIslemTipiChanged();

            if (_isNew)
            {
                string tblName = _fis.IslemTipi == FinansIslemTipi.KasaTahsilat ? "tahsilatana" :
                                 _fis.IslemTipi == FinansIslemTipi.KasaTediye ? "odemeana" :
                                 _fis.IslemTipi == FinansIslemTipi.BankaTahsilat ? "bankatahsilatana" : "bankaodemeana";
                _fis.FisNo = await _service.GetNextFisNoAsync(tblName);
                _fis.MakbuzNo = _fis.FisNo;
            }

            _txtFisNo.Text = _fis.FisNo > 0 ? _fis.FisNo.ToString("N0") : "";
            _txtMakbuzNo.Text = _fis.MakbuzNo > 0 ? _fis.MakbuzNo.ToString("N0") : "";
            _dtpFisTar.Value = _fis.FisTar;
            _dtpSaat.Value = DateTime.Today.Add(_fis.Saat);

            _txtCariKod.Text = _fis.FirKod > 0 ? _fis.FirKod.ToString("N0") : "";
            _txtCariAdi.Text = _fis.CariAdi;
            _txtVergiDaire.Text = _fis.VergiDairesi;
            _txtVergiNo.Text = _fis.VergiNo;

            _txtTutar.Text = _fis.Tutar > 0 ? _fis.Tutar.ToString("N2") : "0,00";
            _txtBelgeNo.Text = _fis.BelgeNo;
            _txtAciklama.Text = _fis.Aciklama;

            UpdateYaziIle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fiş bilgileri yüklenirken hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnIslemTipiChanged()
    {
        _cboHesap.Items.Clear();
        int tipIdx = _cboIslemTipi.SelectedIndex;
        if (tipIdx == 0 || tipIdx == 1) // Kasa
        {
            foreach (var k in _kasalar)
            {
                _cboHesap.Items.Add($"#{k.Kodu} {k.Adi} ({k.DovizTuru})");
            }
            if (_cboHesap.Items.Count > 0)
            {
                int defaultIdx = _kasalar.FindIndex(k => k.Kodu == 4); // Merkez TL
                _cboHesap.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;
            }
        }
        else // Banka
        {
            foreach (var b in _bankalar)
            {
                _cboHesap.Items.Add($"#{b.Kodu} {b.Adi} ({b.DovizCinsi})");
            }
            if (_cboHesap.Items.Count > 0)
            {
                int defaultIdx = _bankalar.FindIndex(b => b.Kodu == 2); // QNB Finansbank
                _cboHesap.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;
            }
        }
    }

    private void OpenCariRehberi()
    {
        using var dlg = new CariRehberForm();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedCari != null)
        {
            var c = dlg.SelectedCari;
            _fis.FirKod = c.FirKod;
            _fis.CariAdi = c.FirmaAdi;
            _fis.VergiDairesi = c.VergiDairesi ?? "";
            _fis.VergiNo = c.VergiNo ?? "";
            _fis.CariAdresi = c.Sehir ?? "";

            _txtCariKod.Text = c.FirKod > 0 ? c.FirKod.ToString("N0") : "";
            _txtCariAdi.Text = c.FirmaAdi;
            _txtVergiDaire.Text = c.VergiDairesi ?? "";
            _txtVergiNo.Text = c.VergiNo ?? "";
        }
    }

    private void UpdateYaziIle()
    {
        if (double.TryParse(_txtTutar.Text.Trim(), out double val))
        {
            _txtYaziIle.Text = $"#{val:N2} {_cboDoviz.SelectedItem}#";
        }
    }

    private void SyncFormToModel()
    {
        _fis.IslemTipi = (FinansIslemTipi)_cboIslemTipi.SelectedIndex;
        if (double.TryParse(_txtFisNo.Text.Trim(), out double fisNo)) _fis.FisNo = fisNo;
        if (double.TryParse(_txtMakbuzNo.Text.Trim(), out double makNo)) _fis.MakbuzNo = makNo;
        _fis.FisTar = _dtpFisTar.Value.Date;
        _fis.Saat = _dtpSaat.Value.TimeOfDay;

        // Selected Account
        int selIdx = _cboHesap.SelectedIndex;
        if ((int)_fis.IslemTipi <= 1 && selIdx >= 0 && selIdx < _kasalar.Count)
        {
            _fis.KasaKodu = _kasalar[selIdx].Kodu;
            _fis.KasaAdi = _kasalar[selIdx].Adi;
        }
        else if ((int)_fis.IslemTipi >= 2 && selIdx >= 0 && selIdx < _bankalar.Count)
        {
            _fis.BankaKodu = _bankalar[selIdx].Kodu;
            _fis.BankaAdi = _bankalar[selIdx].Adi;
        }

        _fis.CariAdi = _txtCariAdi.Text.Trim();
        _fis.VergiDairesi = _txtVergiDaire.Text.Trim();
        _fis.VergiNo = _txtVergiNo.Text.Trim();

        if (double.TryParse(_txtTutar.Text.Trim(), out double tutar)) _fis.Tutar = tutar;
        _fis.DovizTuru = _cboDoviz.SelectedItem?.ToString() ?? "TL";
        if (double.TryParse(_txtKur.Text.Trim(), out double kur)) _fis.DovizKur = kur;
        _fis.DovizTutar = _fis.DovizKur > 0 ? _fis.Tutar / _fis.DovizKur : _fis.Tutar;

        _fis.BelgeNo = _txtBelgeNo.Text.Trim();
        _fis.Aciklama = _txtAciklama.Text.Trim();
        _fis.YaziIle = _txtYaziIle.Text.Trim();
    }

    private async Task<bool> SaveInternalAsync()
    {
        try
        {
            SyncFormToModel();

            if (_fis.Tutar <= 0)
            {
                MessageBox.Show("Lütfen geçerli bir işlem tutarı giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_fis.CariAdi))
            {
                MessageBox.Show("Lütfen cari hesap seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var savedNo = await _service.SaveFinansFisiAsync(_fis);
            NotificationService.NotifySuccess(
                "Finans Fişi Kaydedildi",
                $"#{savedNo} no'lu {_fis.IslemTipi} fişi ({_fis.CariAdi} - {_fis.Tutar:N2} {_fis.DovizTuru}) başarıyla kaydedildi.",
                "KasaBanka");

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void PrintDocument()
    {
        try
        {
            SyncFormToModel();
            var doc = KasaBankaReportRenderer.CreatePrintDocument(_fis);
            using var dlg = new PrintPreviewDialog
            {
                Document = doc,
                Width = 950,
                Height = 750,
                StartPosition = FormStartPosition.CenterParent
            };
            dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Baskı önizleme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
