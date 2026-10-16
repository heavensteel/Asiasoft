using System;
using System.Drawing;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;

namespace AsiasoftERP.UI.Forms.Modules;

public class AmeleEkleDialog : Form
{
    public PuantajDetayDto Amele { get; private set; }
    private readonly PuantajService _puantajService = new();

    private ComboBox _cmbAmeleAdi = null!;
    private NumericUpDown _numYevmiyeUcreti = null!;
    private NumericUpDown _numMesaiUcreti = null!;
    private CheckBox _chkYevmiyeciMi = null!;
    private Button _btnEkle = null!;
    private Button _btnIptal = null!;

    public AmeleEkleDialog(decimal defaultYevmiye = 900, decimal defaultMesai = 125)
    {
        Amele = new PuantajDetayDto
        {
            YevmiyeUcreti = defaultYevmiye,
            MesaiUcreti = defaultMesai,
            YevmiyeciMi = true
        };

        InitializeComponent();
        LoadWorkers();
    }

    private void InitializeComponent()
    {
        Text = "İşçi / Amele Ekle";
        Size = new Size(480, 320);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Segoe UI", 9.5f);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(20, 12, 20, 12)
        };
        var lblHeader = new Label
        {
            Text = "👷 Yeni İşçi / Amele Kaydı",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblHeader);

        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(25, 20, 25, 20)
        };

        int y = 15;
        int labelWidth = 140;
        int inputWidth = 270;

        // Amele Adı
        var lblAmele = new Label { Text = "Amele Adı Soyadı:", Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _cmbAmeleAdi = new ComboBox { Location = new Point(25 + labelWidth, y), Width = inputWidth, DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        pnlContent.Controls.AddRange(new Control[] { lblAmele, _cmbAmeleAdi });
        y += 40;

        // Günlük Yevmiye Ücreti
        var lblYevmiye = new Label { Text = "Günlük Yevmiye (₺):", Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59) };
        _numYevmiyeUcreti = new NumericUpDown { Location = new Point(25 + labelWidth, y), Width = inputWidth, Minimum = 0, Maximum = 100000, DecimalPlaces = 2, ThousandsSeparator = true, Value = Amele.YevmiyeUcreti };
        pnlContent.Controls.AddRange(new Control[] { lblYevmiye, _numYevmiyeUcreti });
        y += 40;

        // Saatlik Mesai Ücreti
        var lblMesai = new Label { Text = "Saatlik Mesai (₺):", Location = new Point(25, y + 3), Width = labelWidth, ForeColor = Color.FromArgb(30, 41, 59) };
        _numMesaiUcreti = new NumericUpDown { Location = new Point(25 + labelWidth, y), Width = inputWidth, Minimum = 0, Maximum = 10000, DecimalPlaces = 2, ThousandsSeparator = true, Value = Amele.MesaiUcreti };
        pnlContent.Controls.AddRange(new Control[] { lblMesai, _numMesaiUcreti });
        y += 40;

        // Yevmiyeci mi
        _chkYevmiyeciMi = new CheckBox { Text = "Yevmiyeci statüsünde mi?", Location = new Point(25 + labelWidth, y), Width = inputWidth, Checked = true };
        pnlContent.Controls.Add(_chkYevmiyeciMi);
        y += 35;

        // Bottom
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.White };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        _btnEkle = new Button
        {
            Text = "➕ Puantaja Ekle",
            DialogResult = DialogResult.OK,
            Size = new Size(130, 36),
            Location = new Point(pnlBottom.Width - 250, 10),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnEkle.FlatAppearance.BorderSize = 0;
        _btnEkle.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_cmbAmeleAdi.Text))
            {
                MessageBox.Show("Lütfen işçi adını giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            Amele.FirmaAdi = _cmbAmeleAdi.Text.Trim().ToUpper();
            Amele.YevmiyeUcreti = _numYevmiyeUcreti.Value;
            Amele.MesaiUcreti = _numMesaiUcreti.Value;
            Amele.YevmiyeciMi = _chkYevmiyeciMi.Checked;
            Amele.RecalculateTotals();
        };

        _btnIptal = new Button
        {
            Text = "İptal",
            DialogResult = DialogResult.Cancel,
            Size = new Size(90, 36),
            Location = new Point(pnlBottom.Width - 110, 10),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        _btnIptal.FlatAppearance.BorderSize = 0;

        pnlBottom.Controls.AddRange(new Control[] { _btnEkle, _btnIptal });

        Controls.AddRange(new Control[] { pnlContent, pnlBottom, pnlHeader });
        AcceptButton = _btnEkle;
        CancelButton = _btnIptal;
    }

    private async void LoadWorkers()
    {
        try
        {
            var workers = await _puantajService.GetAmeleRehberAsync();
            _cmbAmeleAdi.Items.Clear();
            _cmbAmeleAdi.Items.AddRange(workers.ToArray());
        }
        catch { }
    }
}
