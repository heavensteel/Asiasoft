using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Dialog for creating or modifying Orchard Purchase and Cutting Contracts ('sozlesmebahceana')
/// </summary>
public class BahceSozlesmeEditDialog : Form
{
    private readonly BahceKesimService _service = new();
    private readonly BahceSozlesmeDto _sozlesme;
    private readonly bool _isNew;

    // Controls
    private TextBox _txtSozlesmeNo = null!;
    private DateTimePicker _dtpTarih = null!;
    private ComboBox _cboTur = null!;
    private TextBox _txtSozlesmeAdi = null!;
    private TextBox _txtBelgeNo = null!;

    private TextBox _txtCariKod = null!;
    private TextBox _txtCariAdi = null!;
    private TextBox _txtBahceAdi = null!;
    private TextBox _txtMevki = null!;
    private TextBox _txtAda = null!;
    private TextBox _txtParsel = null!;

    private TextBox _txtUrun = null!;
    private TextBox _txtHedefKg = null!;
    private TextBox _txtFiyat = null!;
    private TextBox _txtTutar = null!;
    private ComboBox _cboDurum = null!;
    private TextBox _txtAciklama = null!;

    public BahceSozlesmeEditDialog(BahceSozlesmeDto? existing = null)
    {
        _isNew = existing == null;
        _sozlesme = existing ?? new BahceSozlesmeDto
        {
            SozlesmeTuru = "TONAJ",
            Tarih = DateTime.Today,
            Durumu = "Aktif",
            Kullanici = AppState.CurrentUserName
        };

        this.DoubleBuffered = true;
        InitializeComponents();
        this.Load += async (_, _) => await LoadDataToFormAsync();
    }

    private void InitializeComponents()
    {
        this.Text = _isNew ? "Yeni Bahçe Alım & Kesim Sözleşmesi" : $"Sözleşme Düzenle — #{_sozlesme.SozlesmeNo:0}";
        this.Size = new Size(840, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // TOP BANNER
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
            Text = _isNew ? "🌳  Yeni Bahçe Alım Sözleşmesi Giriş Formu" : $"🌳  Bahçe Sözleşmesi Düzenle — #{_sozlesme.SozlesmeNo:0}",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(16, 8),
            AutoSize = true
        };

        var lblHdrSub = new Label
        {
            Text = "Müstahsilden dalında narenciye/meyve alım sözleşmesi, tapu parseli ve kesim şartları",
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
            Text = "🖨️  Kaydet ve Sözleşme Yazdır",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(215, 38),
            Location = new Point(pnlBottom.Width - 330, 12),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += async (_, _) =>
        {
            if (await SaveInternalAsync())
            {
                BahceSozlesmeReportRenderer.PrintReport(_sozlesme);
                this.DialogResult = DialogResult.OK;
            }
        };

        var btnSave = new Button
        {
            Text = "💾  Sözleşmeyi Kaydet",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(195, 38),
            Location = new Point(pnlBottom.Width - 535, 12),
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

        // CENTER BODY (3 Group panels)
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = ModernColors.Slate100,
            AutoScroll = true
        };

        int y = 8;

        // GROUP 1: SÖZLEŞME VE MÜSTAHSİL BİLGİLERİ
        var grpGenel = CreateGroupBox("1. SÖZLEŞME GENEL BİLGİLERİ VE MÜSTAHSİL", y, 140, 790);

        var lblNo = new Label { Text = "Sözleşme No:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtSozlesmeNo = new TextBox { Location = new Point(14, 48), Size = new Size(95, 26), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        var lblTur = new Label { Text = "Alım Türü:", Location = new Point(120, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboTur = new ComboBox { Location = new Point(120, 48), Size = new Size(110, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboTur.Items.AddRange(new object[] { "TONAJ", "KABALA" });
        _cboTur.SelectedIndex = 0;
        _cboTur.SelectedIndexChanged += (_, _) => OnAlimTuruChanged();

        var lblTar = new Label { Text = "Sözleşme Tarihi:", Location = new Point(240, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _dtpTarih = new DateTimePicker { Location = new Point(240, 48), Size = new Size(125, 26), Format = DateTimePickerFormat.Short };

        var lblSozAdi = new Label { Text = "Sözleşme / Dosya Adı:", Location = new Point(375, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtSozlesmeAdi = new TextBox { Location = new Point(375, 48), Size = new Size(240, 26) };

        var lblBelge = new Label { Text = "Belge / Kontrat No:", Location = new Point(625, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtBelgeNo = new TextBox { Location = new Point(625, 48), Size = new Size(140, 26) };

        // Row 2: Müstahsil Cari
        var lblCari = new Label { Text = "Müstahsil / Bahçe Sahibi:", Location = new Point(14, 84), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtCariKod = new TextBox { Location = new Point(14, 102), Size = new Size(80, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };
        _txtCariAdi = new TextBox { Location = new Point(98, 102), Size = new Size(320, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        var btnCariRehber = new Button
        {
            Text = "🔍 Cari Rehber (F10)",
            Location = new Point(424, 101),
            Size = new Size(135, 28),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCariRehber.FlatAppearance.BorderSize = 0;
        btnCariRehber.Click += (_, _) => OpenCariRehberi();

        var lblDurum = new Label { Text = "Sözleşme Durumu:", Location = new Point(575, 84), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _cboDurum = new ComboBox { Location = new Point(575, 102), Size = new Size(190, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboDurum.Items.AddRange(new object[] { "Aktif", "Tamamlandı", "İptal" });
        _cboDurum.SelectedIndex = 0;

        grpGenel.Controls.AddRange(new Control[] {
            lblNo, _txtSozlesmeNo, lblTur, _cboTur, lblTar, _dtpTarih, lblSozAdi, _txtSozlesmeAdi, lblBelge, _txtBelgeNo,
            lblCari, _txtCariKod, _txtCariAdi, btnCariRehber, lblDurum, _cboDurum
        });
        pnlBody.Controls.Add(grpGenel);
        y += 150;

        // GROUP 2: BAHÇE MEVKİ VE PARSEL BİLGİLERİ
        var grpParsel = CreateGroupBox("2. BAHÇE MEVKİ, ADA VE PARSEL BİLGİLERİ", y, 95, 790);

        var lblBahce = new Label { Text = "Bahçe Tanımı:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtBahceAdi = new TextBox { Location = new Point(14, 48), Size = new Size(260, 26) };

        var lblMevki = new Label { Text = "Mevki / Köy / İlçe:", Location = new Point(284, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtMevki = new TextBox { Location = new Point(284, 48), Size = new Size(230, 26) };

        var lblAda = new Label { Text = "Ada No:", Location = new Point(524, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtAda = new TextBox { Location = new Point(524, 48), Size = new Size(110, 26) };

        var lblParsel = new Label { Text = "Parsel No:", Location = new Point(644, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtParsel = new TextBox { Location = new Point(644, 48), Size = new Size(122, 26) };

        grpParsel.Controls.AddRange(new Control[] { lblBahce, _txtBahceAdi, lblMevki, _txtMevki, lblAda, _txtAda, lblParsel, _txtParsel });
        pnlBody.Controls.Add(grpParsel);
        y += 105;

        // GROUP 3: ÜRÜN, REKOLTE VE PARASAL ŞARTLAR
        var grpUrun = CreateGroupBox("3. ÜRÜN, REKOLTE VE PARASAL ŞARTLAR", y, 175, 790);

        var lblUrun = new Label { Text = "Ürün / Hasat Cinsi:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtUrun = new TextBox { Location = new Point(14, 48), Size = new Size(220, 26), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        var lblHedef = new Label { Text = "Hedef / Taahhüt Rekolte (KG):", Location = new Point(244, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtHedefKg = new TextBox { Location = new Point(244, 48), Size = new Size(150, 26), Text = "0" };
        _txtHedefKg.TextChanged += (_, _) => RecalculateTotals();

        var lblFiyat = new Label { Text = "Alım Birim Fiyatı (TL/KG):", Location = new Point(404, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtFiyat = new TextBox { Location = new Point(404, 48), Size = new Size(140, 26), Text = "0,00" };
        _txtFiyat.TextChanged += (_, _) => RecalculateTotals();

        var lblTutar = new Label { Text = "Toplam Sözleşme Tutarı (TL):", Location = new Point(554, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtTutar = new TextBox
        {
            Location = new Point(554, 48),
            Size = new Size(212, 28),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 120, 70),
            Text = "0,00",
            TextAlign = HorizontalAlignment.Right
        };

        var lblAck = new Label { Text = "Sözleşme Açıklama & Özel Şartlar Notu:", UseMnemonic = false, Location = new Point(14, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtAciklama = new TextBox { Location = new Point(14, 108), Size = new Size(752, 50), Multiline = true };

        grpUrun.Controls.AddRange(new Control[] { lblUrun, _txtUrun, lblHedef, _txtHedefKg, lblFiyat, _txtFiyat, lblTutar, _txtTutar, lblAck, _txtAciklama });
        pnlBody.Controls.Add(grpUrun);

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHdr);
    }

    private Panel CreateGroupBox(string title, int y, int h, int w = 790)
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
        if (_isNew)
        {
            _sozlesme.SozlesmeNo = await _service.GetNextSozlesmeNoAsync();
            _txtSozlesmeNo.Text = _sozlesme.SozlesmeNo.ToString("0");
            _txtUrun.Text = "LİMON - ENTER";
        }
        else
        {
            _txtSozlesmeNo.Text = _sozlesme.SozlesmeNo.ToString("0");
            _dtpTarih.Value = _sozlesme.Tarih;
            _cboTur.SelectedItem = _sozlesme.SozlesmeTuru;
            _txtSozlesmeAdi.Text = _sozlesme.SozlesmeAdi;
            _txtBelgeNo.Text = _sozlesme.BelgeNo;

            _txtCariKod.Text = _sozlesme.FirKod > 0 ? _sozlesme.FirKod.ToString("0") : "";
            _txtCariAdi.Text = _sozlesme.FirmaAdi;
            _txtBahceAdi.Text = _sozlesme.BahceAdi;
            _txtMevki.Text = _sozlesme.Mevki;
            _txtAda.Text = _sozlesme.Ada;
            _txtParsel.Text = _sozlesme.Parsel;

            _txtUrun.Text = _sozlesme.MalAdi;
            _txtHedefKg.Text = _sozlesme.HedefKg.ToString("N0");
            _txtFiyat.Text = _sozlesme.Fiyat.ToString("N2");
            _txtTutar.Text = (_sozlesme.SozlesmeTutari > 0 ? _sozlesme.SozlesmeTutari : _sozlesme.KabalaFiyat).ToString("N2");
            _cboDurum.SelectedItem = _sozlesme.Durumu;
            _txtAciklama.Text = _sozlesme.Aciklama;
        }

        OnAlimTuruChanged();
    }

    private void OnAlimTuruChanged()
    {
        bool isKabala = _cboTur.SelectedItem?.ToString() == "KABALA";
        if (isKabala)
        {
            _txtFiyat.Enabled = false;
            _txtTutar.ReadOnly = false;
            _txtTutar.BackColor = Color.White;
        }
        else
        {
            _txtFiyat.Enabled = true;
            _txtTutar.ReadOnly = true;
            _txtTutar.BackColor = Color.FromArgb(248, 250, 252);
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        if (_cboTur.SelectedItem?.ToString() == "KABALA") return;

        double.TryParse(_txtHedefKg.Text, out double hedef);
        double.TryParse(_txtFiyat.Text, out double fiyat);
        double tutar = hedef * fiyat;
        _txtTutar.Text = tutar.ToString("N2");
    }

    private void OpenCariRehberi()
    {
        using var dlg = new CariRehberForm();
        if (dlg.ShowDialog() == DialogResult.OK && dlg.SelectedCari != null)
        {
            _sozlesme.FirKod = dlg.SelectedCari.FirKod;
            _sozlesme.FirmaAdi = dlg.SelectedCari.FirmaAdi;
            _txtCariKod.Text = dlg.SelectedCari.FirKod.ToString();
            _txtCariAdi.Text = dlg.SelectedCari.FirmaAdi;

            if (string.IsNullOrEmpty(_txtBahceAdi.Text))
            {
                _txtBahceAdi.Text = $"{dlg.SelectedCari.FirmaAdi} BAHÇESİ";
            }
        }
    }

    private async Task<bool> SaveInternalAsync()
    {
        try
        {
            double.TryParse(_txtSozlesmeNo.Text, out double sozNo);
            _sozlesme.SozlesmeNo = sozNo;
            _sozlesme.SozlesmeTuru = _cboTur.SelectedItem?.ToString() ?? "TONAJ";
            _sozlesme.Tarih = _dtpTarih.Value;
            _sozlesme.SozlesmeAdi = _txtSozlesmeAdi.Text.Trim();
            _sozlesme.BelgeNo = _txtBelgeNo.Text.Trim();

            _sozlesme.FirmaAdi = _txtCariAdi.Text.Trim();
            _sozlesme.BahceAdi = _txtBahceAdi.Text.Trim();
            _sozlesme.Mevki = _txtMevki.Text.Trim();
            _sozlesme.Ada = _txtAda.Text.Trim();
            _sozlesme.Parsel = _txtParsel.Text.Trim();
            _sozlesme.Durumu = _cboDurum.SelectedItem?.ToString() ?? "Aktif";

            _sozlesme.MalAdi = _txtUrun.Text.Trim();
            double.TryParse(_txtHedefKg.Text, out double hedef);
            double.TryParse(_txtFiyat.Text, out double fiyat);
            double.TryParse(_txtTutar.Text, out double tutar);

            _sozlesme.HedefKg = hedef;
            _sozlesme.Fiyat = fiyat;
            _sozlesme.SozlesmeTutari = tutar;
            if (_sozlesme.SozlesmeTuru == "KABALA") _sozlesme.KabalaFiyat = tutar;

            _sozlesme.Aciklama = _txtAciklama.Text.Trim();

            string user = AppState.CurrentUserName;
            bool ok = await _service.SaveSozlesmeAsync(_sozlesme, user);

            if (ok)
            {
                NotificationService.NotifySuccess(
                    "Bahçe Sözleşmesi Kaydedildi",
                    $"#{_sozlesme.SozlesmeNo} nolu '{_sozlesme.FirmaAdi}' bahçe alım sözleşmesi kaydedildi.",
                    "BahceKesim",
                    showToast: true);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
