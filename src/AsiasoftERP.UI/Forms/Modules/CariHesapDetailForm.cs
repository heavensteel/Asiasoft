using AsiasoftERP.Core.Models;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class CariHesapDetailForm : Form
{
    private readonly CariHesap? _existing;
    private TextBox _txtKod = null!, _txtAdi = null!, _txtAdres = null!, _txtSehir = null!,
        _txtUlke = null!, _txtTelefon = null!, _txtFax = null!, _txtEmail = null!,
        _txtVergiNo = null!, _txtVergiDairesi = null!, _txtTemsilci = null!, _txtAciklama = null!;
    private ComboBox _cmbTip = null!, _cmbDoviz = null!;
    private NumericUpDown _numKrediLimiti = null!;
    private Button _btnKaydet = null!, _btnIptal = null!;

    public CariHesapDetailForm(CariHesap? existing)
    {
        _existing = existing;
        InitializeComponents();
        if (existing != null) FillFields(existing);
    }

    private void InitializeComponents()
    {
        this.Text = _existing == null ? "Yeni Cari Hesap" : $"Cari Hesap Düzenle — {_existing.CariAdi}";
        this.Size = new Size(600, 640);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("Segoe UI", 9.5f);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        int y = 15;

        void AddRow(string label, Control control)
        {
            scroll.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(20, y + 3),
                Width = 130,
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            });
            control.Location = new Point(160, y);
            control.Width = 390;
            scroll.Controls.Add(control);
            y += 38;
        }

        _txtKod = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtAdi = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _cmbTip = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbTip.Items.AddRange(new[] { "M", "T", "MT" });
        _cmbTip.SelectedIndex = 0;
        _txtAdres = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtSehir = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtUlke = new TextBox { BorderStyle = BorderStyle.FixedSingle, Text = "Türkiye" };
        _txtTelefon = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtFax = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtEmail = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtVergiNo = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtVergiDairesi = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _cmbDoviz = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbDoviz.Items.AddRange(new[] { "TRY", "USD", "EUR", "GBP" });
        _cmbDoviz.SelectedIndex = 0;
        _numKrediLimiti = new NumericUpDown
        {
            Minimum = 0, Maximum = 999999999,
            DecimalPlaces = 2, ThousandsSeparator = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        _txtTemsilci = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtAciklama = new TextBox { BorderStyle = BorderStyle.FixedSingle, Multiline = true, Height = 60 };

        AddRow("Cari Kodu *", _txtKod);
        AddRow("Cari Adı *", _txtAdi);
        AddRow("Tip", _cmbTip);
        AddRow("Adres", _txtAdres);
        AddRow("Şehir", _txtSehir);
        AddRow("Ülke", _txtUlke);
        AddRow("Telefon", _txtTelefon);
        AddRow("Fax", _txtFax);
        AddRow("E-Posta", _txtEmail);
        AddRow("Vergi No", _txtVergiNo);
        AddRow("Vergi Dairesi", _txtVergiDairesi);
        AddRow("Döviz", _cmbDoviz);
        AddRow("Kredi Limiti", _numKrediLimiti);
        AddRow("Temsilci", _txtTemsilci);
        AddRow("Açıklama", _txtAciklama);
        y += 25; // extra for multiline

        var btnPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Color.FromArgb(240, 242, 248)
        };

        _btnKaydet = new Button
        {
            Text = "💾  Kaydet",
            Location = new Point(340, 12),
            Width = 110,
            Height = 34,
            BackColor = Color.FromArgb(0, 84, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        _btnKaydet.FlatAppearance.BorderSize = 0;
        _btnKaydet.Click += async (s, e) => await SaveAsync();

        _btnIptal = new Button
        {
            Text = "İptal",
            Location = new Point(460, 12),
            Width = 80,
            Height = 34,
            BackColor = Color.FromArgb(180, 180, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnIptal.FlatAppearance.BorderSize = 0;
        _btnIptal.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.AddRange(new Control[] { _btnKaydet, _btnIptal });

        this.Controls.Add(scroll);
        this.Controls.Add(btnPanel);
    }

    private void FillFields(CariHesap c)
    {
        _txtKod.Text = c.CariKod;
        _txtAdi.Text = c.CariAdi;
        _cmbTip.Text = c.CariTip;
        _txtAdres.Text = c.Adres;
        _txtSehir.Text = c.Sehir;
        _txtUlke.Text = c.Ulke ?? "Türkiye";
        _txtTelefon.Text = c.Telefon;
        _txtFax.Text = c.Fax;
        _txtEmail.Text = c.Email;
        _txtVergiNo.Text = c.VergiNo;
        _txtVergiDairesi.Text = c.VergiDairesi;
        _cmbDoviz.Text = c.DovizTipi ?? "TRY";
        _numKrediLimiti.Value = c.KrediLimiti;
        _txtTemsilci.Text = c.TemsilciAdi;
        _txtAciklama.Text = c.Aciklama;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtKod.Text))
        { MessageBox.Show("Cari kodu zorunludur.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(_txtAdi.Text))
        { MessageBox.Show("Cari adı zorunludur.", "Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        try
        {
            using var db = DbContextFactory.Create();
            CariHesap entity;

            if (_existing == null)
            {
                entity = new CariHesap();
                db.CariHesaplar.Add(entity);
            }
            else
            {
                entity = (await db.CariHesaplar.FindAsync(_existing.Id))!;
                entity.UpdatedAt = DateTime.Now;
            }

            entity.CariKod = _txtKod.Text.Trim().ToUpper();
            entity.CariAdi = _txtAdi.Text.Trim();
            entity.CariTip = _cmbTip.Text;
            entity.Adres = _txtAdres.Text.Trim();
            entity.Sehir = _txtSehir.Text.Trim();
            entity.Ulke = _txtUlke.Text.Trim();
            entity.Telefon = _txtTelefon.Text.Trim();
            entity.Fax = _txtFax.Text.Trim();
            entity.Email = _txtEmail.Text.Trim();
            entity.VergiNo = _txtVergiNo.Text.Trim();
            entity.VergiDairesi = _txtVergiDairesi.Text.Trim();
            entity.DovizTipi = _cmbDoviz.Text;
            entity.KrediLimiti = _numKrediLimiti.Value;
            entity.TemsilciAdi = _txtTemsilci.Text.Trim();
            entity.Aciklama = _txtAciklama.Text.Trim();

            await db.SaveChangesAsync();
            this.DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
