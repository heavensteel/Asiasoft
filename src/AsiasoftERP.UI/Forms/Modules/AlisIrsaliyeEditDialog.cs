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

public class AlisIrsaliyeEditDialog : Form
{
    private readonly IrsaliyeService _service = new();
    private IrsaliyeStkDto _hdr;
    private readonly List<IrsaliyeDetayStkDto> _lines = new();
    private readonly bool _isNew;

    // Form Controls
    private TextBox _txtFisNo = null!;
    private TextBox _txtGibIrsNo = null!;
    private DateTimePicker _dtpFisTar = null!;
    private DateTimePicker _dtpIrsTar = null!;
    private DateTimePicker _dtpSaat = null!;

    // Supplier Controls
    private TextBox _txtCariKod = null!;
    private TextBox _txtCariUnvan = null!;
    private TextBox _txtVergiDaire = null!;
    private TextBox _txtVergiNo = null!;
    private TextBox _txtAdres = null!;

    // Logistics Controls
    private TextBox _txtPlaka = null!;
    private TextBox _txtArkaPlaka = null!;
    private TextBox _txtSofor = null!;
    private TextBox _txtSoforTc = null!;
    private TextBox _txtSoforTel = null!;
    private TextBox _txtTasiyici = null!;
    private ComboBox _cboDepo = null!;
    private TextBox _txtAciklama = null!;

    // Lines Grid & Badges
    private DataGridView _gridLines = null!;
    private Label _lblToplamKalem = null!;
    private Label _lblToplamMiktar = null!;
    private Label _lblAraToplam = null!;
    private Label _lblKdvToplam = null!;
    private Label _lblGenelToplam = null!;

    public AlisIrsaliyeEditDialog(IrsaliyeStkDto? irsaliye = null)
    {
        this.DoubleBuffered = true;
        _isNew = irsaliye == null;

        if (_isNew)
        {
            _hdr = new IrsaliyeStkDto
            {
                Sirkod = "1",
                FisTur = 220, // 220 = Alış İrsaliyesi
                FisTar = DateTime.Today,
                IrsTar = DateTime.Today,
                Saat = DateTime.Now.TimeOfDay,
                User = AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL",
                DepoAdi = "ANA DEPO"
            };
        }
        else
        {
            _hdr = irsaliye!;
        }

        InitializeComponents();
        this.Load += async (_, _) => await LoadDataToFormAsync();
    }

    private void InitializeComponents()
    {
        this.Text = _isNew ? "Yeni Alış İrsaliyesi / Mal Kabul Girişi" : $"Alış İrsaliyesi Düzenle — {_hdr.BelgeNoDisplay} ({_hdr.FirmaAdi})";
        this.Size = new Size(1340, 840);
        this.MinimumSize = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. TOP CARDS PANEL (Tedarikçi, İrsaliye, Lojistik)
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 225,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 10)
        };
        pnlTop.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };

        BuildTopSections(pnlTop);

        // 2. BOTTOM ACTION & TOTALS BAR
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        BuildBottomBar(pnlBottom);

        // 3. CENTER GRID PANEL (Items & Toolbar)
        var pnlCenter = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 8),
            BackColor = ModernColors.Slate100
        };

        var pnlGridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };
        pnlGridCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlGridCard.Width - 1, pnlGridCard.Height - 1);
        };

        // Grid Action Toolbar
        var pnlGridTools = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 6, 12, 6)
        };
        pnlGridTools.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlGridTools.Height - 1, pnlGridTools.Width, pnlGridTools.Height - 1);
        };

        var lblGridTitle = new Label
        {
            Text = "📥  KABUL EDİLEN MAL & AMBALAJ KALEMLERİ",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(12, 11),
            AutoSize = true
        };

        var btnAddLine = new Button
        {
            Text = "➕  Boş Satır Ekle",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 30),
            Location = new Point(320, 7),
            Cursor = Cursors.Hand
        };
        btnAddLine.FlatAppearance.BorderColor = ModernColors.Slate300;
        btnAddLine.Click += (_, _) => AddEmptyLine();

        var btnPickStok = new Button
        {
            Text = "🔍  Stok / Ambalaj Rehberinden Seç",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Purchase Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(230, 30),
            Location = new Point(460, 7),
            Cursor = Cursors.Hand
        };
        btnPickStok.FlatAppearance.BorderSize = 0;
        btnPickStok.Click += (_, _) => OpenStokRehberi();

        var btnRemoveLine = new Button
        {
            Text = "🗑️  Seçili Satırı Sil",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Rose600,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 30),
            Location = new Point(700, 7),
            Cursor = Cursors.Hand
        };
        btnRemoveLine.FlatAppearance.BorderColor = Color.FromArgb(254, 205, 211);
        btnRemoveLine.Click += (_, _) => RemoveSelectedLine();

        pnlGridTools.Controls.AddRange(new Control[] { lblGridTitle, btnAddLine, btnPickStok, btnRemoveLine });

        // Setup DataGridView
        _gridLines = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9.5f)
        };
        ModernUiHelper.ApplyModernGridStyle(_gridLines);
        SetupGridColumns();

        _gridLines.CellValueChanged += (s, e) => RecalculateLine(e.RowIndex, e.ColumnIndex);
        _gridLines.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex == 1 || e.ColumnIndex == 2))
            {
                OpenStokRehberiForRow(e.RowIndex);
            }
        };

        pnlGridCard.Controls.Add(_gridLines);
        pnlGridCard.Controls.Add(pnlGridTools);
        pnlCenter.Controls.Add(pnlGridCard);

        this.Controls.Add(pnlCenter);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlTop);
    }

    private void BuildTopSections(Panel pnl)
    {
        int cardH = 200;

        // CARD 1: TEDARİKÇİ (SATICI) BİLGİLERİ
        var cardCari = CreateCardBox(14, 10, 420, cardH, "TEDARİKÇİ (SATICI) BİLGİLERİ");

        var lblCari = new Label { Text = "Tedarikçi Firma:", Location = new Point(12, 34), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtCariKod = new TextBox { Location = new Point(12, 54), Size = new Size(80, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };
        _txtCariUnvan = new TextBox { Location = new Point(96, 54), Size = new Size(240, 26), ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252) };

        var btnCariSec = new Button
        {
            Text = "🔍 Seç",
            Location = new Point(340, 53),
            Size = new Size(68, 28),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCariSec.FlatAppearance.BorderSize = 0;
        btnCariSec.Click += (_, _) => OpenCariRehberi();

        var lblVd = new Label { Text = "Vergi Dairesi / No:", Location = new Point(12, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtVergiDaire = new TextBox { Location = new Point(12, 108), Size = new Size(185, 26) };
        _txtVergiNo = new TextBox { Location = new Point(203, 108), Size = new Size(205, 26) };

        var lblAdres = new Label { Text = "Tedarikçi Adresi / Şehir:", Location = new Point(12, 142), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtAdres = new TextBox { Location = new Point(12, 162), Size = new Size(396, 26) };

        cardCari.Controls.AddRange(new Control[] { lblCari, _txtCariKod, _txtCariUnvan, btnCariSec, lblVd, _txtVergiDaire, _txtVergiNo, lblAdres, _txtAdres });

        // CARD 2: İRSALİYE BİLGİLERİ
        var cardIrs = CreateCardBox(444, 10, 380, cardH, "İRSALİYE & BELGE BİLGİLERİ");

        var lblFisNo = new Label { Text = "Sistem Fiş / İrs No:", Location = new Point(12, 34), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtFisNo = new TextBox { Location = new Point(12, 54), Size = new Size(160, 26), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

        var lblGibNo = new Label { Text = "Tedarikçi e-İrsaliye / Matbu No:", Location = new Point(184, 34), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtGibIrsNo = new TextBox { Location = new Point(184, 54), Size = new Size(180, 26), BackColor = Color.FromArgb(254, 252, 232) }; // Light amber focus

        var lblTar = new Label { Text = "Kabul / Giriş Tarihi:", Location = new Point(12, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _dtpFisTar = new DateTimePicker { Location = new Point(12, 108), Size = new Size(110, 26), Format = DateTimePickerFormat.Short };

        var lblIrsTar = new Label { Text = "Belge İrsaliye Tarihi:", Location = new Point(132, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _dtpIrsTar = new DateTimePicker { Location = new Point(132, 108), Size = new Size(110, 26), Format = DateTimePickerFormat.Short };

        var lblSaat = new Label { Text = "Kabul Saati:", Location = new Point(252, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _dtpSaat = new DateTimePicker { Location = new Point(252, 108), Size = new Size(112, 26), Format = DateTimePickerFormat.Time, ShowUpDown = true };

        var lblDepo = new Label { Text = "Giriş / Kabul Deposu:", Location = new Point(12, 142), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _cboDepo = new ComboBox { Location = new Point(12, 162), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboDepo.Items.AddRange(new object[] { "ANA DEPO", "AMBALAJ VE KASA DEPOSU", "SOĞUK HAVA 1", "SOĞUK HAVA 2", "PAKETLEME DEPOSU" });
        _cboDepo.SelectedIndex = 0;

        var lblAck = new Label { Text = "Açıklama / Mal Kabul Notu:", Location = new Point(184, 142), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtAciklama = new TextBox { Location = new Point(184, 162), Size = new Size(180, 26) };

        cardIrs.Controls.AddRange(new Control[] { lblFisNo, _txtFisNo, lblGibNo, _txtGibIrsNo, lblTar, _dtpFisTar, lblIrsTar, _dtpIrsTar, lblSaat, _dtpSaat, lblDepo, _cboDepo, lblAck, _txtAciklama });

        // CARD 3: NAKLİYE VE TAŞIMA
        var cardLoj = CreateCardBox(834, 10, pnl.Width - 850, cardH, "GETİREN ARAÇ & NAKLİYE BİLGİLERİ");
        cardLoj.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var lblPlaka = new Label { Text = "Çekici Plaka:", Location = new Point(12, 34), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtPlaka = new TextBox { Location = new Point(12, 54), Size = new Size(120, 26), CharacterCasing = CharacterCasing.Upper };

        var lblDorse = new Label { Text = "Dorse Plaka:", Location = new Point(140, 34), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtArkaPlaka = new TextBox { Location = new Point(140, 54), Size = new Size(120, 26), CharacterCasing = CharacterCasing.Upper };

        var lblSofor = new Label { Text = "Sürücü Adı Soyadı:", Location = new Point(12, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtSofor = new TextBox { Location = new Point(12, 108), Size = new Size(150, 26) };

        var lblTc = new Label { Text = "Sürücü T.C. No:", Location = new Point(170, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtSoforTc = new TextBox { Location = new Point(170, 108), Size = new Size(130, 26), MaxLength = 11 };

        var lblTel = new Label { Text = "Sürücü Tel:", Location = new Point(308, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtSoforTel = new TextBox { Location = new Point(308, 108), Size = new Size(130, 26) };

        var lblTas = new Label { Text = "Taşıyıcı Firma / Lojistik:", Location = new Point(12, 142), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtTasiyici = new TextBox { Location = new Point(12, 162), Size = new Size(426, 26) };

        cardLoj.Controls.AddRange(new Control[] { lblPlaka, _txtPlaka, lblDorse, _txtArkaPlaka, lblSofor, _txtSofor, lblTc, _txtSoforTc, lblTel, _txtSoforTel, lblTas, _txtTasiyici });

        pnl.Controls.AddRange(new Control[] { cardCari, cardIrs, cardLoj });
    }

    private Panel CreateCardBox(int x, int y, int w, int h, string title)
    {
        var pnl = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.White,
            Padding = new Padding(12)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var bgHdr = new SolidBrush(Color.FromArgb(240, 248, 245));
            e.Graphics.FillRectangle(bgHdr, 0, 0, pnl.Width - 1, 26);
            using var brushT = new SolidBrush(Color.FromArgb(20, 45, 35));
            using var fontT = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            e.Graphics.DrawString(title, fontT, brushT, 10, 5);
        };
        return pnl;
    }

    private void SetupGridColumns()
    {
        _gridLines.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "#", Width = 40, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Stok Kodu", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Malzeme / Ürün Açıklaması", Width = 320, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Birim", Width = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "Kabul Miktarı", Width = 110, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Birim Alış Fiyatı", Width = 110, DefaultCellStyle = { Format = "N4", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { HeaderText = "KDV %", Width = 65, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { HeaderText = "KDV Tutar", Width = 95, ReadOnly = true, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { HeaderText = "Tutar (TL)", Width = 120, ReadOnly = true, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { HeaderText = "Açıklama / Not", Width = 180, DefaultCellStyle = { Font = new Font("Segoe UI", 8.5f) } }
        );
    }

    private void BuildBottomBar(Panel pnl)
    {
        // Left side: Badges
        var flpBadges = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 480,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };

        _lblToplamKalem = CreateStatBadge(flpBadges, "TOPLAM KALEM", "0 KALEM", ModernColors.Indigo600);
        _lblToplamMiktar = CreateStatBadge(flpBadges, "TOPLAM MİKTAR", "0,00", Color.FromArgb(16, 120, 70));

        // Right side: Totals & Action Buttons
        var pnlActions = new Panel
        {
            Dock = DockStyle.Right,
            Width = 680,
            BackColor = Color.Transparent
        };

        int rx = pnlActions.Width;

        rx -= 100;
        var btnCancel = new Button
        {
            Text = "İptal",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 42),
            Location = new Point(rx, 2),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        rx -= 175;
        var btnPrint = new Button
        {
            Text = "🖨️  Kaydet ve Yazdır",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(165, 42),
            Location = new Point(rx, 2),
            Cursor = Cursors.Hand
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

        rx -= 140;
        var btnSave = new Button
        {
            Text = "💾  Kaydet",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 42),
            Location = new Point(rx, 2),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (_, _) =>
        {
            if (await SaveInternalAsync())
            {
                this.DialogResult = DialogResult.OK;
            }
        };

        // Monetary Totals Summary Box
        rx -= 250;
        var pnlTotalsBox = new Panel
        {
            Location = new Point(rx, 0),
            Size = new Size(240, 48),
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(8, 4, 8, 4)
        };
        pnlTotalsBox.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlTotalsBox.Width - 1, pnlTotalsBox.Height - 1);
        };

        _lblAraToplam = new Label { Text = "Ara Toplam: 0,00 TL", Font = new Font("Segoe UI", 8f), Location = new Point(6, 4), AutoSize = true, ForeColor = ModernColors.Slate600 };
        _lblKdvToplam = new Label { Text = "KDV: 0,00 TL", Font = new Font("Segoe UI", 8f), Location = new Point(6, 26), AutoSize = true, ForeColor = ModernColors.Slate600 };
        _lblGenelToplam = new Label { Text = "0,00 TL", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), Location = new Point(110, 12), AutoSize = true, ForeColor = Color.FromArgb(180, 20, 20) };

        pnlTotalsBox.Controls.AddRange(new Control[] { _lblAraToplam, _lblKdvToplam, _lblGenelToplam });

        pnlActions.Controls.AddRange(new Control[] { pnlTotalsBox, btnSave, btnPrint, btnCancel });

        pnl.Controls.AddRange(new Control[] { flpBadges, pnlActions });
    }

    private Label CreateStatBadge(Panel parent, string title, string val, Color col)
    {
        var pnl = new Panel
        {
            Size = new Size(150, 48),
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(8, 4, 8, 4)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var b = new SolidBrush(col);
            e.Graphics.FillRectangle(b, 0, 0, 3, pnl.Height);
        };

        var lblT = new Label { Text = title, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = ModernColors.Slate500, Location = new Point(8, 4), AutoSize = true };
        var lblV = new Label { Text = val, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = col, Location = new Point(8, 20), AutoSize = true };

        pnl.Controls.AddRange(new Control[] { lblT, lblV });
        parent.Controls.Add(pnl);
        return lblV;
    }

    private async Task LoadDataToFormAsync()
    {
        try
        {
            if (_isNew)
            {
                var nextNo = await _service.GetNextFisNoAsync(220);
                _hdr.FisNo = nextNo;
                _hdr.IrsNo = nextNo;
            }

            _txtFisNo.Text = _hdr.FisNo > 0 ? _hdr.FisNo.ToString("N0") : "";
            _txtGibIrsNo.Text = _hdr.GibIrsNo;
            _dtpFisTar.Value = _hdr.FisTar ?? DateTime.Today;
            _dtpIrsTar.Value = _hdr.IrsTar ?? DateTime.Today;
            if (_hdr.Saat.HasValue) _dtpSaat.Value = DateTime.Today.Add(_hdr.Saat.Value);

            _txtCariKod.Text = _hdr.FirKod > 0 ? _hdr.FirKod.ToString("N0") : "";
            _txtCariUnvan.Text = _hdr.FirmaAdi;
            _txtVergiDaire.Text = _hdr.VergiDairesi;
            _txtVergiNo.Text = _hdr.VergiNumarasi;
            _txtAdres.Text = _hdr.FirmaAdresi;

            _txtPlaka.Text = _hdr.Plaka;
            _txtArkaPlaka.Text = _hdr.ArkaPlaka;
            _txtSofor.Text = _hdr.SoforTamAdi;
            _txtSoforTc.Text = _hdr.SoforTc;
            _txtSoforTel.Text = _hdr.SoforTelefon;
            _txtTasiyici.Text = _hdr.TasiyiciFirma;
            _txtAciklama.Text = _hdr.Aciklama;

            if (!string.IsNullOrEmpty(_hdr.DepoAdi))
            {
                int idx = _cboDepo.FindStringExact(_hdr.DepoAdi);
                if (idx >= 0) _cboDepo.SelectedIndex = idx;
            }

            // Load Lines
            if (!_isNew)
            {
                var dbLines = await _service.GetIrsaliyeDetayAsync(_hdr.FisNo, 220);
                _lines.Clear();
                _lines.AddRange(dbLines);
            }

            if (_lines.Count == 0)
            {
                AddEmptyLine();
            }

            RefreshGridRows();
            RecalculateTotals();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Alış irsaliyesi bilgileri yüklenirken hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshGridRows()
    {
        _gridLines.Rows.Clear();
        int idx = 1;
        foreach (var l in _lines)
        {
            _gridLines.Rows.Add(
                idx++,
                l.MalKodu,
                l.MalAdi,
                l.Birimi,
                l.Miktar,
                l.Fiyati,
                l.Kdv,
                l.KdvTut,
                l.Tutar,
                l.Aciklama
            );
        }
    }

    private void AddEmptyLine()
    {
        var line = new IrsaliyeDetayStkDto
        {
            FisTur = 220,
            Birimi = "ADET",
            Kdv = 1
        };
        _lines.Add(line);
        RefreshGridRows();
        RecalculateTotals();
    }

    private void RemoveSelectedLine()
    {
        if (_gridLines.CurrentRow == null || _gridLines.CurrentRow.Index < 0) return;
        int idx = _gridLines.CurrentRow.Index;
        if (idx < _lines.Count)
        {
            _lines.RemoveAt(idx);
            RefreshGridRows();
            RecalculateTotals();
        }
    }

    private void OpenStokRehberi()
    {
        using var dlg = new StokRehberForm();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedStok != null)
        {
            var stok = dlg.SelectedStok;
            var line = new IrsaliyeDetayStkDto
            {
                FisTur = 220,
                MalKodu = stok.Kodu,
                MalAdi = stok.Adi,
                Birimi = stok.Birim,
                Fiyati = stok.AlisFiyat > 0 ? stok.AlisFiyat : (stok.SFiyat1 > 0 ? stok.SFiyat1 : 0),
                Kdv = stok.Kdv > 0 ? stok.Kdv : 1,
                Miktar = 1
            };
            line.Tutar = line.Miktar * line.Fiyati;
            line.KdvTut = line.Tutar * (line.Kdv / 100);

            _lines.Add(line);
            RefreshGridRows();
            RecalculateTotals();
        }
    }

    private void OpenStokRehberiForRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _lines.Count) return;
        using var dlg = new StokRehberForm();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedStok != null)
        {
            var stok = dlg.SelectedStok;
            var line = _lines[rowIndex];
            line.MalKodu = stok.Kodu;
            line.MalAdi = stok.Adi;
            line.Birimi = stok.Birim;
            if (line.Fiyati <= 0) line.Fiyati = stok.AlisFiyat > 0 ? stok.AlisFiyat : stok.SFiyat1;
            line.Kdv = stok.Kdv > 0 ? stok.Kdv : 1;

            line.Tutar = line.Miktar * line.Fiyati;
            line.KdvTut = line.Tutar * (line.Kdv / 100);

            RefreshGridRows();
            RecalculateTotals();
        }
    }

    private void OpenCariRehberi()
    {
        using var dlg = new CariRehberForm();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedCari != null)
        {
            var c = dlg.SelectedCari;
            _hdr.FirKod = c.FirKod;
            _hdr.FirmaAdi = c.FirmaAdi;
            _hdr.VergiDairesi = c.VergiDairesi ?? "";
            _hdr.VergiNumarasi = c.VergiNo ?? "";
            _hdr.Sehir = c.Sehir ?? "";
            _hdr.FirmaAdresi = c.Sehir ?? "";

            _txtCariKod.Text = c.FirKod > 0 ? c.FirKod.ToString("N0") : "";
            _txtCariUnvan.Text = c.FirmaAdi;
            _txtVergiDaire.Text = c.VergiDairesi ?? "";
            _txtVergiNo.Text = c.VergiNo ?? "";
            _txtAdres.Text = _hdr.FirmaAdresi;
        }
    }

    private void RecalculateLine(int rowIdx, int colIdx)
    {
        if (rowIdx < 0 || rowIdx >= _lines.Count) return;
        var r = _gridLines.Rows[rowIdx];
        var l = _lines[rowIdx];

        l.MalKodu = r.Cells[1].Value?.ToString() ?? "";
        l.MalAdi = r.Cells[2].Value?.ToString() ?? "";
        l.Birimi = r.Cells[3].Value?.ToString() ?? "ADET";

        double.TryParse(r.Cells[4].Value?.ToString(), out double miktar);
        double.TryParse(r.Cells[5].Value?.ToString(), out double fiyat);
        double.TryParse(r.Cells[6].Value?.ToString(), out double kdv);

        l.Miktar = miktar;
        l.Fiyati = fiyat;
        l.Kdv = kdv;

        l.Tutar = l.Miktar * l.Fiyati;
        l.KdvTut = l.Tutar * (l.Kdv / 100);
        l.Aciklama = r.Cells[9].Value?.ToString() ?? "";

        r.Cells[7].Value = l.KdvTut;
        r.Cells[8].Value = l.Tutar;

        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        int totalItems = _lines.Count(l => !string.IsNullOrWhiteSpace(l.MalAdi));
        double totalQty = _lines.Sum(l => l.Miktar);
        double subTotal = _lines.Sum(l => l.Tutar);
        double kdvTotal = _lines.Sum(l => l.KdvTut);
        double grandTotal = subTotal + kdvTotal;

        _lblToplamKalem.Text = $"{totalItems} KALEM";
        _lblToplamMiktar.Text = $"{totalQty:N2}";

        _lblAraToplam.Text = $"Ara Toplam: {subTotal:N2} TL";
        _lblKdvToplam.Text = $"KDV: {kdvTotal:N2} TL";
        _lblGenelToplam.Text = $"{grandTotal:N2} TL";
    }

    private void SyncFormToModel()
    {
        if (double.TryParse(_txtFisNo.Text.Trim(), out double fisNo)) _hdr.FisNo = fisNo;
        _hdr.FisTur = 220;
        _hdr.GibIrsNo = _txtGibIrsNo.Text.Trim();
        _hdr.FisTar = _dtpFisTar.Value.Date;
        _hdr.IrsTar = _dtpIrsTar.Value.Date;
        _hdr.Saat = _dtpSaat.Value.TimeOfDay;

        _hdr.FirmaAdi = _txtCariUnvan.Text.Trim();
        _hdr.VergiDairesi = _txtVergiDaire.Text.Trim();
        _hdr.VergiNumarasi = _txtVergiNo.Text.Trim();
        _hdr.FirmaAdresi = _txtAdres.Text.Trim();

        _hdr.Plaka = _txtPlaka.Text.Trim().ToUpperInvariant();
        _hdr.ArkaPlaka = _txtArkaPlaka.Text.Trim().ToUpperInvariant();
        _hdr.SoforAdi = _txtSofor.Text.Trim();
        _hdr.SoforTc = _txtSoforTc.Text.Trim();
        _hdr.SoforTelefon = _txtSoforTel.Text.Trim();
        _hdr.TasiyiciFirma = _txtTasiyici.Text.Trim();
        _hdr.DepoAdi = _cboDepo.SelectedItem?.ToString() ?? "ANA DEPO";
        _hdr.Aciklama = _txtAciklama.Text.Trim();

        _hdr.ToplamMiktar = _lines.Sum(l => l.Miktar);
        _hdr.FisTut = _lines.Sum(l => l.Tutar);
        _hdr.KdvTut = _lines.Sum(l => l.KdvTut);
        _hdr.GenTut = _hdr.FisTut + _hdr.KdvTut;
    }

    private async Task<bool> SaveInternalAsync()
    {
        try
        {
            SyncFormToModel();

            if (string.IsNullOrWhiteSpace(_hdr.FirmaAdi))
            {
                MessageBox.Show("Lütfen tedarikçi (satıcı) firma seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_lines.Count == 0 || _lines.All(l => string.IsNullOrWhiteSpace(l.MalAdi)))
            {
                MessageBox.Show("Lütfen en az bir adet mal veya ambalaj kalemi giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var validLines = _lines.Where(l => !string.IsNullOrWhiteSpace(l.MalAdi)).ToList();

            var savedNo = await _service.SaveAlisIrsaliyesiAsync(_hdr, validLines);
            NotificationService.NotifySuccess(
                "Alış İrsaliyesi Kaydedildi",
                $"{_hdr.BelgeNoDisplay} no'lu alış irsaliyesi ({_hdr.FirmaAdi} - {_hdr.ToplamMiktar:N2} Birim) başarıyla kaydedildi.",
                "AlisIrsaliye");

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
            var validLines = _lines.Where(l => !string.IsNullOrWhiteSpace(l.MalAdi)).ToList();
            var doc = AlisIrsaliyeReportRenderer.CreatePrintDocument(_hdr, validLines);
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
