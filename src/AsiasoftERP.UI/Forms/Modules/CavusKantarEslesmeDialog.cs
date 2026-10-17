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

/// <summary>
/// Modal dialog for matching and editing Çavuş (Harvest Crew),
/// Weighbridge receipt (Kantar) and transport service (Servis) allocation.
/// </summary>
public class CavusKantarEslesmeDialog : Form
{
    private readonly CavusKantarServisService _service = new();
    private readonly CavusKantarEslesmeDto _dto;

    // Controls
    private ComboBox _cboCavus = null!;
    private TextBox _txtCavusKod = null!;
    private TextBox _txtCavus2 = null!;
    private TextBox _txtYevmiye2 = null!;
    private TextBox _txtServis2 = null!;

    private ComboBox _cboBolge = null!;
    private TextBox _txtServisSayisi = null!;
    private TextBox _txtServisFiyati = null!;
    private TextBox _txtServisTutari = null!;
    private TextBox _txtToplamNakliye = null!;
    private CheckBox _chkOnay = null!;
    private TextBox _txtAciklama = null!;

    private List<CavusLookupItemDto> _cavuslar = new();
    private List<BolgeLookupItemDto> _bolgeler = new();

    public CavusKantarEslesmeDialog(CavusKantarEslesmeDto dto)
    {
        _dto = dto;
        this.DoubleBuffered = true;

        InitializeComponents();
        this.Load += async (_, _) => await LoadDataToFormAsync();
    }

    private void InitializeComponents()
    {
        this.Text = $"Çavuş & Servis Eşleme — Kantar Fişi #{_dto.GirisKodu}";
        this.Size = new Size(820, 680);
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
            Text = $"🤝  Çavuş, Kantar ve Servis Eşleme Formu (Fiş #{_dto.GirisKodu})",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(16, 8),
            AutoSize = true
        };

        var lblHdrSub = new Label
        {
            Text = "Hasat amele çavuşunu kantar tartımı ve servis nakliye seferiyle mutabakatlandırın.",
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
            Text = "🖨️  Kaydet ve İcmal Yazdır",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(195, 38),
            Location = new Point(pnlBottom.Width - 310, 12),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += async (_, _) =>
        {
            if (await SaveInternalAsync())
            {
                PrintSummary();
                this.DialogResult = DialogResult.OK;
            }
        };

        var btnSave = new Button
        {
            Text = "💾  Eşleşmeyi Kaydet",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 38),
            Location = new Point(pnlBottom.Width - 480, 12),
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

        // GROUP 1: KANTAR TARTIM REFERANS BİLGİLERİ (Readonly)
        var grpKantar = CreateGroupBox("1. KANTAR TARTIM VE ARAÇ BİLGİLERİ (REFERANS)", y, 105, 780);

        var lblPlaka = new Label { Text = "Plaka & Şoför:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        var txtPlaka = new TextBox { Text = $"{_dto.Plaka}  —  {_dto.SoforAdi}", Location = new Point(14, 48), Size = new Size(220, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        var lblUrun = new Label { Text = "Ürün / Hasat Cinsi:", Location = new Point(246, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        var txtUrun = new TextBox { Text = _dto.UrunAdi, Location = new Point(246, 48), Size = new Size(200, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };

        var lblMustahsil = new Label { Text = "Bahçe / Müstahsil Sahibi:", Location = new Point(458, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        var txtMustahsil = new TextBox { Text = _dto.MustahsilAdi, Location = new Point(458, 48), Size = new Size(180, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };

        var lblNet = new Label { Text = "Net Hasat (KG):", Location = new Point(650, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        var txtNet = new TextBox
        {
            Text = _dto.NetKgFormatted,
            Location = new Point(650, 48),
            Size = new Size(116, 26),
            ReadOnly = true,
            BackColor = Color.FromArgb(240, 253, 244),
            ForeColor = Color.FromArgb(16, 120, 70),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Right
        };

        var lblTar = new Label { Text = $"İşlem Tarihi: {_dto.TarihFormatted}  Saat: {_dto.GirisSaati}", Location = new Point(14, 80), AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(100, 116, 139) };

        grpKantar.Controls.AddRange(new Control[] { lblPlaka, txtPlaka, lblUrun, txtUrun, lblMustahsil, txtMustahsil, lblNet, txtNet, lblTar });
        pnlBody.Controls.Add(grpKantar);
        y += 115;

        // GROUP 2: HASAT ÇAVUŞU & EKİP BİLGİLERİ
        var grpCavus = CreateGroupBox("2. HASAT ÇAVUŞU VE AMELE EKİBİ", y, 140, 780);

        var lblCavus = new Label { Text = "Hasat Çavuşu (1. Ekip Başı):", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboCavus = new ComboBox { Location = new Point(14, 48), Size = new Size(260, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboCavus.SelectedIndexChanged += async (_, _) => await OnCavusChangedAsync();

        var btnCariRehber = new Button
        {
            Text = "🔍 Cari Rehber (F10)",
            Location = new Point(280, 47),
            Size = new Size(130, 28),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCariRehber.FlatAppearance.BorderSize = 0;
        btnCariRehber.Click += (_, _) => OpenCariRehberi();

        var lblCavusKod = new Label { Text = "Çavuş Kodu:", Location = new Point(420, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtCavusKod = new TextBox { Location = new Point(420, 48), Size = new Size(90, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };

        // 2. Ekip / Çavuş
        var lblCavus2 = new Label { Text = "2. Hasat Çavuşu (Varsa Ortak Ekip):", Location = new Point(14, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtCavus2 = new TextBox { Location = new Point(14, 106), Size = new Size(260, 26), Text = _dto.CavusAdi2 };

        var lblYevmiye2 = new Label { Text = "2. Ekip Yevmiye:", Location = new Point(284, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtYevmiye2 = new TextBox { Location = new Point(284, 106), Size = new Size(100, 26), Text = _dto.CavusYevmiye2.ToString("0.#") };

        var lblServis2 = new Label { Text = "2. Ekip Servis:", Location = new Point(394, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtServis2 = new TextBox { Location = new Point(394, 106), Size = new Size(100, 26), Text = _dto.CavusServis2.ToString("0.#") };

        grpCavus.Controls.AddRange(new Control[] { lblCavus, _cboCavus, btnCariRehber, lblCavusKod, _txtCavusKod, lblCavus2, _txtCavus2, lblYevmiye2, _txtYevmiye2, lblServis2, _txtServis2 });
        pnlBody.Controls.Add(grpCavus);
        y += 150;

        // GROUP 3: SERVİS, GÜZERGAH VE HAKEDİŞ HESAPLAMA
        var grpServis = CreateGroupBox("3. SERVİS ARACI, GÜZERGAH VE HAKEDİŞ HESABI", y, 205, 780);

        var lblBolge = new Label { Text = "Bölge / Servis Güzergahı:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboBolge = new ComboBox { Location = new Point(14, 48), Size = new Size(260, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboBolge.SelectedIndexChanged += (_, _) => OnBolgeChanged();

        var lblAdet = new Label { Text = "Servis Sayısı:", Location = new Point(284, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtServisSayisi = new TextBox { Location = new Point(284, 48), Size = new Size(80, 26), Text = _dto.ServisSayisi > 0 ? _dto.ServisSayisi.ToString("0.#") : "1" };
        _txtServisSayisi.TextChanged += (_, _) => RecalculateTotals();

        var lblFiyat = new Label { Text = "Birim Sefer Fiyatı:", Location = new Point(374, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtServisFiyati = new TextBox { Location = new Point(374, 48), Size = new Size(110, 26), Text = _dto.ServisFiyati.ToString("N2") };
        _txtServisFiyati.TextChanged += (_, _) => RecalculateTotals();

        var lblTutar = new Label { Text = "Toplam Servis Tutarı (TL):", Location = new Point(494, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtServisTutari = new TextBox
        {
            Location = new Point(494, 48),
            Size = new Size(140, 26),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 120, 70),
            Text = _dto.ServisTutari.ToString("N2"),
            TextAlign = HorizontalAlignment.Right
        };

        var lblNakliye = new Label { Text = "Toplam Nakliye:", Location = new Point(644, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtToplamNakliye = new TextBox { Location = new Point(644, 48), Size = new Size(122, 26), Text = _dto.ToplamNakliye.ToString("N2") };

        // Onay ve Açıklama
        _chkOnay = new CheckBox
        {
            Text = "✅  Eşleşme ve Servis Hakedişini Onayla (Muhasebe Kaydı Aç)",
            UseMnemonic = false,
            Location = new Point(14, 88),
            Size = new Size(500, 26),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 120, 70),
            Checked = _dto.Onay
        };

        var lblAck = new Label { Text = "Eşleşme Açıklama & Mutabakat Notu:", UseMnemonic = false, Location = new Point(14, 122), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtAciklama = new TextBox { Location = new Point(14, 142), Size = new Size(752, 26), Text = _dto.Aciklama };

        grpServis.Controls.AddRange(new Control[] { lblBolge, _cboBolge, lblAdet, _txtServisSayisi, lblFiyat, _txtServisFiyati, lblTutar, _txtServisTutari, lblNakliye, _txtToplamNakliye, _chkOnay, lblAck, _txtAciklama });
        pnlBody.Controls.Add(grpServis);

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
            _cavuslar = await _service.GetCavuslarAsync();
            _cboCavus.Items.Clear();
            _cboCavus.Items.Add("-- Çavuş Seçiniz --");

            int selIndex = 0;
            for (int i = 0; i < _cavuslar.Count; i++)
            {
                var c = _cavuslar[i];
                _cboCavus.Items.Add(c);
                if (c.FirKod == _dto.CavusKodu || (!string.IsNullOrEmpty(_dto.CavusAdi) && c.CavusAdi.Equals(_dto.CavusAdi, StringComparison.OrdinalIgnoreCase)))
                {
                    selIndex = i + 1;
                }
            }
            _cboCavus.SelectedIndex = selIndex;
            _txtCavusKod.Text = _dto.CavusKodu > 0 ? _dto.CavusKodu.ToString("0") : "";

            await LoadBolgelerAsync(_dto.CavusKodu);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veriler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadBolgelerAsync(double? firKod)
    {
        _bolgeler = await _service.GetBolgelerAsync(firKod);
        _cboBolge.Items.Clear();
        _cboBolge.Items.Add("-- Bölge / Güzergah Seçiniz --");

        int selIndex = 0;
        for (int i = 0; i < _bolgeler.Count; i++)
        {
            var b = _bolgeler[i];
            _cboBolge.Items.Add(b);
            if (!string.IsNullOrEmpty(_dto.Bolge) && b.BolgeAdi.Equals(_dto.Bolge, StringComparison.OrdinalIgnoreCase))
            {
                selIndex = i + 1;
            }
        }
        _cboBolge.SelectedIndex = selIndex;
    }

    private async Task OnCavusChangedAsync()
    {
        if (_cboCavus.SelectedItem is CavusLookupItemDto sel)
        {
            _txtCavusKod.Text = sel.FirKod.ToString("0");
            await LoadBolgelerAsync(sel.FirKod);
        }
    }

    private void OnBolgeChanged()
    {
        if (_cboBolge.SelectedItem is BolgeLookupItemDto sel && sel.VarsayilanFiyat > 0)
        {
            _txtServisFiyati.Text = sel.VarsayilanFiyat.ToString("N2");
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        double.TryParse(_txtServisSayisi.Text, out double adet);
        double.TryParse(_txtServisFiyati.Text, out double fiyat);

        if (adet <= 0) adet = 1;
        double tutar = adet * fiyat;
        _txtServisTutari.Text = tutar.ToString("N2");
    }

    private void OpenCariRehberi()
    {
        using var dlg = new CariRehberForm();
        if (dlg.ShowDialog() == DialogResult.OK && dlg.SelectedCari != null)
        {
            _dto.CavusKodu = dlg.SelectedCari.FirKod;
            _dto.CavusAdi = dlg.SelectedCari.FirmaAdi;
            _txtCavusKod.Text = dlg.SelectedCari.FirKod.ToString();

            // Check if in combo
            bool found = false;
            for (int i = 1; i < _cboCavus.Items.Count; i++)
            {
                if (_cboCavus.Items[i] is CavusLookupItemDto item && item.FirKod == dlg.SelectedCari.FirKod)
                {
                    _cboCavus.SelectedIndex = i;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var newItem = new CavusLookupItemDto { FirKod = dlg.SelectedCari.FirKod, CavusAdi = dlg.SelectedCari.FirmaAdi };
                _cboCavus.Items.Add(newItem);
                _cboCavus.SelectedItem = newItem;
            }
        }
    }

    private async Task<bool> SaveInternalAsync()
    {
        try
        {
            if (_cboCavus.SelectedItem is CavusLookupItemDto selCavus)
            {
                _dto.CavusKodu = selCavus.FirKod;
                _dto.CavusAdi = selCavus.CavusAdi;
            }

            if (_cboBolge.SelectedItem is BolgeLookupItemDto selBolge)
            {
                _dto.Bolge = selBolge.BolgeAdi;
            }

            double.TryParse(_txtServisSayisi.Text, out double sSayisi);
            double.TryParse(_txtServisFiyati.Text, out double sFiyat);
            double.TryParse(_txtServisTutari.Text, out double sTutar);
            double.TryParse(_txtToplamNakliye.Text, out double nakliye);

            double.TryParse(_txtYevmiye2.Text, out double yev2);
            double.TryParse(_txtServis2.Text, out double ser2);

            _dto.ServisSayisi = sSayisi;
            _dto.ServisFiyati = sFiyat;
            _dto.ServisTutari = sTutar;
            _dto.ToplamNakliye = nakliye;

            _dto.CavusAdi2 = _txtCavus2.Text.Trim();
            _dto.CavusYevmiye2 = yev2;
            _dto.CavusServis2 = ser2;

            _dto.Onay = _chkOnay.Checked;
            _dto.Aciklama = _txtAciklama.Text.Trim();

            string activeUser = AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL";
            bool success = await _service.SaveEslesmeAsync(_dto, activeUser);

            if (success)
            {
                NotificationService.NotifySuccess(
                    "Çavuş & Servis Eşleşmesi Kaydedildi",
                    $"Kantar Fişi #{_dto.GirisKodu} için '{_dto.CavusAdi}' ve servis tutarı ({_dto.ServisTutariFormatted}) kaydedildi.",
                    "CavusServis",
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

    private void PrintSummary()
    {
        var list = new List<CavusKantarEslesmeDto> { _dto };
        CavusKantarReportRenderer.PrintReport(list, _dto.CavusAdi, _dto.Bolge, _dto.Tarih, _dto.Tarih);
    }
}
