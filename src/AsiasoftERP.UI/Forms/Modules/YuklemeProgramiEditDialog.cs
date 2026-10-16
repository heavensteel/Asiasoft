using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

public class YuklemeProgramiEditDialog : Form
{
    private readonly YuklemeProgramiService _service = new();
    private YuklemeProgramiDto _prog;
    private bool _isNew;

    // Controls
    private TextBox _txtProgramNo = null!;
    private DateTimePicker _dtpTarih = null!;
    private TextBox _txtBaslik = null!;
    private ComboBox _cboGridDuzen = null!;
    private TextBox _txtHazirlayan = null!;
    private TextBox _txtOnaylayan = null!;
    private ComboBox _cboDurum = null!;
    private Label _lblToplamPaletHero = null!;

    // Per-column/cell tab controls
    private TabControl _tabSutunlar = null!;
    private readonly List<ColumnInputControls> _colControls = new();

    // Preview & Layout
    private SplitContainer _split = null!;
    private double _previewZoomRatio = 1.0;
    private double _splitterRatio = 0.54; // 54% left inputs, 46% right preview
    private bool _isInitialized = false;
    private PictureBox _picPreview = null!;
    private Panel _pnlPreviewScroll = null!;
    private System.Windows.Forms.Timer _previewDebounceTimer = null!;

    private class ColumnInputControls
    {
        public int SatirNo = 1;
        public int SutunNo = 1;
        public int HucreIndex = 1;
        public TextBox TxtBaslik = null!;
        public NumericUpDown NumPalet = null!;
        public TextBox TxtUrunler = null!;
        public TextBox TxtTalimatlar = null!;
        public TextBox TxtPartiler = null!;
        public TextBox TxtTarihler = null!;
    }

    public YuklemeProgramiEditDialog(YuklemeProgramiDto? prog = null)
    {
        this.DoubleBuffered = true;
        _isNew = prog == null;
        if (_isNew)
        {
            _prog = new YuklemeProgramiDto
            {
                Tarih = DateTime.Today,
                Baslik = "YENİ SEVKİYAT",
                SutunSayisi = 1,
                Hazirlayan = AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL",
                Onaylayan = "İHRACAT BÖLÜMÜ",
                Durum = "Onaylandı",
                Sutunlar = new List<YuklemeSutunDto>
                {
                    new YuklemeSutunDto
                    {
                        SutunNo = 1,
                        Baslik = "ROMANYA / METRO",
                        ToplamPalet = 26
                    }
                }
            };
        }
        else
        {
            _prog = prog!;
        }

        InitializeComponents();
        LoadProgramToForm();
    }

    private void InitializeComponents()
    {
        this.Text = _isNew ? "Yeni Yükleme Programı Oluştur" : $"Yükleme Programı Düzenle — {_prog.ProgramNo}";
        this.Size = new Size(1360, 860);
        this.MinimumSize = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        _previewDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _previewDebounceTimer.Tick += (_, _) =>
        {
            _previewDebounceTimer.Stop();
            UpdateLivePreview();
        };

        // Main Horizontal SplitContainer: Left = Form inputs, Right = Live A4 Preview
        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 8,
            BackColor = ModernColors.Slate300
        };

        _split.Panel1.BackColor = ModernColors.Slate100;
        _split.Panel2.BackColor = ModernColors.Slate200;

        _split.SplitterMoved += (_, _) =>
        {
            if (_isInitialized && _split.Width > 600)
            {
                _splitterRatio = (double)_split.SplitterDistance / _split.Width;
            }
            AdjustPreviewLayout();
        };

        _split.Paint += (s, e) =>
        {
            var r = _split.SplitterRectangle;
            using var bgBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
            e.Graphics.FillRectangle(bgBrush, r);

            using var borderPen = new Pen(Color.FromArgb(203, 213, 225), 1f);
            e.Graphics.DrawLine(borderPen, r.Left, r.Top, r.Left, r.Bottom);
            e.Graphics.DrawLine(borderPen, r.Right - 1, r.Top, r.Right - 1, r.Bottom);

            // Draw vertical grip dots in the middle
            using var gripBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
            int cy = r.Top + r.Height / 2;
            int cx = r.Left + r.Width / 2;
            for (int i = -3; i <= 3; i++)
            {
                e.Graphics.FillEllipse(gripBrush, cx - 1, cy + (i * 7), 3, 3);
            }
        };

        this.Resize += (_, _) =>
        {
            if (_isInitialized && this.WindowState != FormWindowState.Minimized && _split.Width > 640)
            {
                int minLeft = 450;
                int minRight = 260;
                int totalW = _split.ClientSize.Width - _split.SplitterWidth;
                int desired = (int)(totalW * _splitterRatio);
                desired = Math.Max(minLeft, Math.Min(totalW - minRight, desired));
                try { _split.SplitterDistance = desired; } catch { }
                AdjustPreviewLayout();
            }
        };

        this.Shown += (_, _) =>
        {
            _isInitialized = true;
            // Initial split: 54% left inputs, 46% right preview
            SetPreviewWidthRatio(0.46);
            AdjustPreviewLayout();
        };

        BuildLeftInputPanel(_split.Panel1);
        BuildRightPreviewPanel(_split.Panel2);

        // Bottom Action Bar
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
            Size = new Size(100, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        var btnPrintDirect = new Button
        {
            Text = "🖨️  Kaydet ve Yazdır",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Blue600,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 12, 0)
        };
        btnPrintDirect.FlatAppearance.BorderSize = 0;
        btnPrintDirect.Click += async (_, _) =>
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
            BackColor = ModernColors.Emerald500,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 12, 0)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (_, _) =>
        {
            if (await SaveInternalAsync())
            {
                this.DialogResult = DialogResult.OK;
            }
        };

        var btnWhatsAppBottom = new Button
        {
            Text = "💬  WhatsApp'ta Paylaş",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.WhatsAppGreen,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(185, 38),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand
        };
        btnWhatsAppBottom.FlatAppearance.BorderSize = 0;
        btnWhatsAppBottom.Click += (_, _) => ShareViaWhatsApp();

        pnlBottom.Controls.AddRange(new Control[] { btnWhatsAppBottom, btnSave, btnPrintDirect, btnCancel });

        this.Controls.Add(_split);
        this.Controls.Add(pnlBottom);

        try
        {
            if (_split.Width > 600)
            {
                _split.SplitterDistance = (int)(_split.ClientSize.Width * 0.54);
            }
        }
        catch { }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_split != null && _split.Width > 600)
        {
            int desired = (int)(_split.ClientSize.Width * _splitterRatio);
            try { _split.SplitterDistance = desired; } catch { }
        }
        AdjustPreviewLayout();
    }

    private void BuildLeftInputPanel(Panel pnl)
    {
        pnl.AutoScroll = true;
        pnl.Padding = new Padding(16, 12, 16, 12);

        // Header Strip
        var pnlHdr = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.White,
            Padding = new Padding(14, 10, 14, 10)
        };
        pnlHdr.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlHdr.Width - 1, pnlHdr.Height - 1);
        };

        // Row 1: Program No, Tarih, Grid Mizanpajı (Satır x Sütun)
        var lblProgNo = new Label { Text = "Program No:", Location = new Point(12, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtProgramNo = new TextBox { Location = new Point(12, 30), Size = new Size(130, 26), ReadOnly = false };

        var lblTarih = new Label { Text = "Yükleme Tarihi:", Location = new Point(150, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _dtpTarih = new DateTimePicker { Location = new Point(150, 30), Size = new Size(115, 26), Format = DateTimePickerFormat.Short };
        _dtpTarih.ValueChanged += (_, _) => TriggerPreviewUpdate();

        var lblGrid = new Label { Text = "Grid Mizanpajı (Satır x Sütun):", Location = new Point(275, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboGridDuzen = new ComboBox { Location = new Point(275, 30), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboGridDuzen.Items.AddRange(new object[]
        {
            "1x1 (Tam Sayfa)",
            "1x2 (2 Sütun)",
            "1x3 (3 Sütun Körfez)",
            "2x1 (2 Satır)",
            "2x2 (4'lü Grid)",
            "2x3 (6'lı Grid)",
            "3x1 (3 Satır)",
            "3x2 (6'lı Grid)",
            "3x3 (9'lu Grid)"
        });
        _cboGridDuzen.SelectedIndex = 0;
        _cboGridDuzen.SelectedIndexChanged += (_, _) => OnGridDuzenChanged();

        // Quick Grid Preset Buttons
        Button CreateGridPresetBtn(string text, int r, int c, int x)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 30),
                Size = new Size(34, 26),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = ModernColors.Slate700,
                BackColor = ModernColors.Slate100,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = ModernColors.Slate300;
            btn.Click += (_, _) => SelectGridDuzen(r, c);
            return btn;
        }

        var btnG11 = CreateGridPresetBtn("1x1", 1, 1, 432);
        var btnG13 = CreateGridPresetBtn("1x3", 1, 3, 468);
        var btnG22 = CreateGridPresetBtn("2x2", 2, 2, 504);
        var btnG33 = CreateGridPresetBtn("3x3", 3, 3, 540);

        var lblDurum = new Label { Text = "Durum:", Location = new Point(584, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cboDurum = new ComboBox { Location = new Point(584, 30), Size = new Size(95, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboDurum.Items.AddRange(new object[] { "Onaylandı", "Taslak", "Yüklendi", "İptal" });
        _cboDurum.SelectedIndex = 0;

        // Row 2: Genel Başlık / Sevkiyat Adı
        var lblBaslik = new Label { Text = "Genel Başlık / Sevkiyat Hedefi:", Location = new Point(12, 62), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtBaslik = new TextBox { Location = new Point(12, 80), Size = new Size(380, 26) };
        _txtBaslik.TextChanged += (_, _) => TriggerPreviewUpdate();

        _lblToplamPaletHero = new Label
        {
            Text = "26 PLT",
            Location = new Point(402, 70),
            Size = new Size(277, 36),
            BackColor = ModernColors.Slate900,
            ForeColor = ModernColors.Emerald500,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlHdr.Controls.AddRange(new Control[]
        {
            lblProgNo, _txtProgramNo,
            lblTarih, _dtpTarih,
            lblGrid, _cboGridDuzen,
            btnG11, btnG13, btnG22, btnG33,
            lblDurum, _cboDurum,
            lblBaslik, _txtBaslik,
            _lblToplamPaletHero
        });

        void RelayoutHeader()
        {
            int w = pnlHdr.ClientSize.Width - 28;
            if (w < 150) return;

            // Row 1
            _txtProgramNo.Width = Math.Min(125, Math.Max(85, (int)(w * 0.17)));
            _dtpTarih.Location = new Point(_txtProgramNo.Right + 8, 30);
            _dtpTarih.Width = Math.Min(115, Math.Max(85, (int)(w * 0.15)));
            lblTarih.Location = new Point(_dtpTarih.Left, 12);

            _cboGridDuzen.Location = new Point(_dtpTarih.Right + 10, 30);
            _cboGridDuzen.Width = Math.Min(145, Math.Max(110, (int)(w * 0.22)));
            lblGrid.Location = new Point(_cboGridDuzen.Left, 12);

            btnG11.Location = new Point(_cboGridDuzen.Right + 5, 30);
            btnG13.Location = new Point(btnG11.Right + 3, 30);
            btnG22.Location = new Point(btnG13.Right + 3, 30);
            btnG33.Location = new Point(btnG22.Right + 3, 30);

            _cboDurum.Location = new Point(btnG33.Right + 8, 30);
            _cboDurum.Width = Math.Min(105, Math.Max(75, pnlHdr.ClientSize.Width - _cboDurum.Left - 14));
            lblDurum.Location = new Point(_cboDurum.Left, 12);

            // Row 2
            int heroWidth = Math.Min(270, Math.Max(160, (int)(w * 0.38)));
            _lblToplamPaletHero.Width = heroWidth;
            _lblToplamPaletHero.Location = new Point(pnlHdr.ClientSize.Width - 14 - heroWidth, 68);

            _txtBaslik.Width = Math.Max(160, _lblToplamPaletHero.Left - 24);
        }

        pnlHdr.Resize += (_, _) => RelayoutHeader();
        RelayoutHeader();

        // Personel Strip (Hazırlayan, Onaylayan)
        var pnlPersonel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 8, 0, 0)
        };
        pnlPersonel.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlPersonel.Width - 1, pnlPersonel.Height - 1);
        };

        var lblHaz = new Label { Text = "Hazırlayan:", Location = new Point(12, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtHazirlayan = new TextBox { Location = new Point(12, 26), Size = new Size(250, 24), Text = "Ali SARIGÜL" };
        _txtHazirlayan.TextChanged += (_, _) => TriggerPreviewUpdate();

        var lblOnay = new Label { Text = "Onaylayan Bölüm:", Location = new Point(280, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _txtOnaylayan = new TextBox { Location = new Point(280, 26), Size = new Size(250, 24), Text = "İHRACAT BÖLÜMÜ" };
        _txtOnaylayan.TextChanged += (_, _) => TriggerPreviewUpdate();

        pnlPersonel.Resize += (_, _) =>
        {
            int pw = (pnlPersonel.ClientSize.Width - 36) / 2;
            if (pw > 100)
            {
                _txtHazirlayan.Width = pw;
                lblOnay.Location = new Point(_txtHazirlayan.Right + 12, 8);
                _txtOnaylayan.Location = new Point(_txtHazirlayan.Right + 12, 26);
                _txtOnaylayan.Width = pw;
            }
        };

        pnlPersonel.Controls.AddRange(new Control[] { lblHaz, _txtHazirlayan, lblOnay, _txtOnaylayan });

        // TabControl for Columns (1, 2, or 3)
        _tabSutunlar = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 0)
        };

        pnl.Controls.Add(_tabSutunlar);
        pnl.Controls.Add(pnlPersonel);
        pnl.Controls.Add(pnlHdr);
    }

    private void BuildRightPreviewPanel(Panel pnl)
    {
        // Top Toolbar of Preview
        var pnlPreviewBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = ModernColors.Slate900,
            Padding = new Padding(10, 6, 10, 6)
        };

        var lblTitle = new Label
        {
            Text = "📄 Canlı A4 Baskı",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(12, 12),
            AutoSize = true
        };

        var flpTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 7, 6, 0)
        };

        // Width Presets & Zoom Buttons
        var btnExpand = CreateBarButton("◀◀ %60", "Önizleme panelini %60 genişlet", () => SetPreviewWidthRatio(0.60), 65);
        var btnEqual = CreateBarButton("↔ %50", "Yarı yarıya eşit böl (%50)", () => SetPreviewWidthRatio(0.50), 55);
        var btnShrink = CreateBarButton("▶▶ %35", "Önizleme panelini %35 daralt", () => SetPreviewWidthRatio(0.35), 65);

        var btnZoomOut = CreateBarButton("➖", "Uzaklaştır", () =>
        {
            _previewZoomRatio = Math.Max(0.5, _previewZoomRatio - 0.15);
            AdjustPreviewLayout();
        }, 30);

        var btnZoomIn = CreateBarButton("➕", "Yakınlaştır", () =>
        {
            _previewZoomRatio = Math.Min(2.5, _previewZoomRatio + 0.15);
            AdjustPreviewLayout();
        }, 30);

        var btnFit = CreateBarButton("📐 Sığdır", "Genişliğe Tam Sığdır", () =>
        {
            _previewZoomRatio = 1.0;
            AdjustPreviewLayout();
        }, 65);

        var btnSavePng = CreateBarButton("💾 Resim", "Önizlemeyi PNG Olarak Kaydet", () => ExportAsImage(), 75);
        btnSavePng.BackColor = ModernColors.Slate700;

        var btnPrint = CreateBarButton("🖨️ Yazdır", "Yükleme Programını Yazdır", () => PrintDocument(), 80);
        btnPrint.BackColor = ModernColors.Blue600;

        var btnWhatsApp = CreateBarButton("💬 WhatsApp", "WhatsApp ile Paylaş", () => ShareViaWhatsApp(), 95);
        btnWhatsApp.BackColor = ModernColors.WhatsAppGreen;

        flpTools.Controls.AddRange(new Control[]
        {
            btnExpand, btnEqual, btnShrink,
            btnZoomOut, btnZoomIn, btnFit,
            btnSavePng, btnPrint, btnWhatsApp
        });

        pnlPreviewBar.Controls.AddRange(new Control[] { lblTitle, flpTools });

        // Preview Scroll Panel & PictureBox
        _pnlPreviewScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ModernColors.Slate300,
            Padding = new Padding(16)
        };
        _pnlPreviewScroll.Resize += (_, _) => AdjustPreviewLayout();

        _pnlPreviewScroll.MouseWheel += (s, e) =>
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.Delta > 0)
                    _previewZoomRatio = Math.Min(2.5, _previewZoomRatio + 0.10);
                else
                    _previewZoomRatio = Math.Max(0.5, _previewZoomRatio - 0.10);
                AdjustPreviewLayout();
            }
        };

        _picPreview = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White,
            Location = new Point(16, 16)
        };
        _picPreview.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(160, 160, 160), 1.5f);
            e.Graphics.DrawRectangle(pen, 0, 0, _picPreview.Width - 1, _picPreview.Height - 1);
        };

        _pnlPreviewScroll.Controls.Add(_picPreview);

        pnl.Controls.Add(_pnlPreviewScroll);
        pnl.Controls.Add(pnlPreviewBar);
    }

    private Button CreateBarButton(string text, string toolTip, Action onClick, int width = 60)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 28),
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 0, 2, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => onClick();

        var tt = new ToolTip();
        tt.SetToolTip(btn, toolTip);
        return btn;
    }

    public void SetPreviewWidthRatio(double rightRatio)
    {
        if (_split == null || _split.Width <= 100) return;
        int totalW = _split.ClientSize.Width - _split.SplitterWidth;
        int minLeft = 380;
        int minRight = 260;
        if (totalW < minLeft + minRight) return;

        int desiredLeft = (int)(totalW * (1.0 - rightRatio));
        desiredLeft = Math.Max(minLeft, Math.Min(totalW - minRight, desiredLeft));
        try
        {
            _split.SplitterDistance = desiredLeft;
            _splitterRatio = (double)desiredLeft / _split.Width;
        }
        catch { }
        AdjustPreviewLayout();
    }

    private void AdjustPreviewLayout()
    {
        if (_pnlPreviewScroll == null || _picPreview == null) return;

        int margin = 16;
        int availW = _pnlPreviewScroll.ClientSize.Width - (margin * 2);
        if (availW < 180) availW = 180;

        int targetW = (int)(availW * _previewZoomRatio);
        int targetH = (int)(targetW * 1.4142); // A4 portrait ratio (297 / 210 = ~1.414)

        _picPreview.Size = new Size(targetW, targetH);
        int posX = targetW < _pnlPreviewScroll.ClientSize.Width
            ? (_pnlPreviewScroll.ClientSize.Width - targetW) / 2
            : margin;
        _picPreview.Location = new Point(Math.Max(margin, posX), margin);

        TriggerPreviewUpdate();
    }

    private void LoadProgramToForm()
    {
        _txtProgramNo.Text = _prog.ProgramNo;
        _dtpTarih.Value = _prog.Tarih > DateTime.MinValue ? _prog.Tarih : DateTime.Today;
        _txtBaslik.Text = _prog.Baslik;
        _txtHazirlayan.Text = _prog.Hazirlayan;
        _txtOnaylayan.Text = _prog.Onaylayan;
        _cboDurum.SelectedItem = _prog.Durum;

        int satirlar = Math.Max(1, Math.Min(3, _prog.SatirSayisi));
        int sutunlar = Math.Max(1, Math.Min(3, _prog.SutunSayisi));

        SelectGridDuzen(satirlar, sutunlar);
        RebuildGridTabs(satirlar, sutunlar);
        TriggerPreviewUpdate();
    }

    private void SelectGridDuzen(int satirlar, int sutunlar)
    {
        int idx = (satirlar, sutunlar) switch
        {
            (1, 1) => 0,
            (1, 2) => 1,
            (1, 3) => 2,
            (2, 1) => 3,
            (2, 2) => 4,
            (2, 3) => 5,
            (3, 1) => 6,
            (3, 2) => 7,
            (3, 3) => 8,
            _ => 0
        };
        if (_cboGridDuzen.SelectedIndex != idx)
        {
            _cboGridDuzen.SelectedIndex = idx;
        }
        else
        {
            OnGridDuzenChanged();
        }
    }

    private (int satirlar, int sutunlar) GetCurrentGridDimensions()
    {
        return _cboGridDuzen.SelectedIndex switch
        {
            0 => (1, 1),
            1 => (1, 2),
            2 => (1, 3),
            3 => (2, 1),
            4 => (2, 2),
            5 => (2, 3),
            6 => (3, 1),
            7 => (3, 2),
            8 => (3, 3),
            _ => (1, 1)
        };
    }

    private void OnGridDuzenChanged()
    {
        var (satirlar, sutunlar) = GetCurrentGridDimensions();

        // Preserve current inputs into _prog
        SyncFormToModel();
        _prog.SatirSayisi = satirlar;
        _prog.SutunSayisi = sutunlar;
        RebuildGridTabs(satirlar, sutunlar);
        UpdateTotalPalletHero();
        TriggerPreviewUpdate();
    }

    private void RebuildGridTabs(int satirlar, int sutunlar)
    {
        _tabSutunlar.TabPages.Clear();
        _colControls.Clear();

        int totalCells = satirlar * sutunlar;

        // Ensure _prog.Sutunlar has items for all cells in the grid
        for (int r = 1; r <= satirlar; r++)
        {
            for (int c = 1; c <= sutunlar; c++)
            {
                int cellIdx = (r - 1) * sutunlar + c;
                var existing = _prog.Sutunlar.FirstOrDefault(s => (s.SatirNo == r && s.SutunNo == c) || s.HucreIndex == cellIdx);
                if (existing == null)
                {
                    _prog.Sutunlar.Add(new YuklemeSutunDto
                    {
                        SatirNo = r,
                        SutunNo = c,
                        HucreIndex = cellIdx,
                        Baslik = totalCells == 1 
                            ? "ROMANYA / METRO" 
                            : (satirlar == 1 ? $"SEVKİYAT {c}" : $"BÖLÜM {cellIdx} (S{r}-K{c})"),
                        ToplamPalet = 26
                    });
                }
                else
                {
                    existing.SatirNo = r;
                    existing.SutunNo = c;
                    existing.HucreIndex = cellIdx;
                }
            }
        }

        for (int r = 1; r <= satirlar; r++)
        {
            for (int c = 1; c <= sutunlar; c++)
            {
                int cellIdx = (r - 1) * sutunlar + c;
                var sutun = _prog.Sutunlar.FirstOrDefault(s => s.SatirNo == r && s.SutunNo == c)
                            ?? _prog.Sutunlar.First(s => s.HucreIndex == cellIdx);

                string tabTitle = totalCells == 1
                    ? " Genel Bölüm "
                    : (satirlar == 1 ? $" {c}. Sütun / Ülke " : $" [{r},{c}] Bölüm {cellIdx} ");

                var tab = new TabPage(tabTitle)
                {
                    BackColor = Color.White,
                    Padding = new Padding(12)
                };

                var controls = BuildSingleColumnTab(tab, sutun, r, c, cellIdx, totalCells);
                _colControls.Add(controls);
                _tabSutunlar.TabPages.Add(tab);
            }
        }
    }

    private ColumnInputControls BuildSingleColumnTab(TabPage tab, YuklemeSutunDto sutun, int r, int c, int cellIdx, int totalCells)
    {
        var pnlScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 0, 8, 10)
        };

        var ctrl = new ColumnInputControls
        {
            SatirNo = r,
            SutunNo = c,
            HucreIndex = cellIdx
        };

        // Row 1: Sütun Başlığı & Palet Sayısı
        var pnlR1 = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(0) };
        string headerLabelText = totalCells == 1 
            ? "Sütun / Alıcı Başlığı:" 
            : (totalCells <= 3 ? $"{c}. Sütun / Alıcı Başlığı:" : $"[{r},{c}] {cellIdx}. Bölüm Alıcı Başlığı:");
        var lblBas = new Label { Text = headerLabelText, Location = new Point(0, 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        ctrl.TxtBaslik = new TextBox { Location = new Point(0, 24), Size = new Size(360, 26), Text = sutun.Baslik };
        ctrl.TxtBaslik.TextChanged += (_, _) => TriggerPreviewUpdate();

        var lblPalet = new Label { Text = "Palet:", Location = new Point(375, 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        ctrl.NumPalet = new NumericUpDown { Location = new Point(375, 24), Size = new Size(90, 26), Minimum = 1, Maximum = 200, Value = sutun.ToplamPalet > 0 ? sutun.ToplamPalet : 26 };
        ctrl.NumPalet.ValueChanged += (_, _) =>
        {
            UpdateTotalPalletHero();
            TriggerPreviewUpdate();
        };

        pnlR1.Controls.AddRange(new Control[] { lblBas, ctrl.TxtBaslik, lblPalet, ctrl.NumPalet });

        // Row 2: Ürünler & Miktarlar + Hızlı Ürün Ekle Butonları
        var pnlUrunHdr = new Panel { Dock = DockStyle.Top, Height = 32, Margin = new Padding(0, 6, 0, 0) };
        var lblUrun = new Label { Text = "1. Ürünler ve Palet Dağılımı:", Dock = DockStyle.Left, AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

        var btnPresetUrun = new Button
        {
            Text = "➕  Hızlı Ürün Şablonu",
            Dock = DockStyle.Right,
            Size = new Size(160, 26),
            BackColor = ModernColors.Emerald50,
            ForeColor = ModernColors.Emerald600,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPresetUrun.FlatAppearance.BorderColor = ModernColors.Emerald500;
        btnPresetUrun.Click += (_, _) => ShowProductPresetsMenu(btnPresetUrun, ctrl);

        pnlUrunHdr.Controls.AddRange(new Control[] { lblUrun, btnPresetUrun });

        ctrl.TxtUrunler = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 110,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f),
            Text = NormalizeNewlines(sutun.UrunlerMetin)
        };
        ctrl.TxtUrunler.TextChanged += (_, _) =>
        {
            // Auto calculate pallets
            int calcPalet = YuklemeProgramiReportRenderer.CalculatePalletsFromText(ctrl.TxtUrunler.Text);
            if (calcPalet > 0)
            {
                ctrl.NumPalet.Value = Math.Min(ctrl.NumPalet.Maximum, Math.Max(ctrl.NumPalet.Minimum, calcPalet));
            }
            TriggerPreviewUpdate();
        };

        // Row 3: Kalite & Yükleme Talimatları + Hızlı Kural Ekle
        var pnlTalimatHdr = new Panel { Dock = DockStyle.Top, Height = 32, Margin = new Padding(0, 8, 0, 0) };
        var lblTalimat = new Label { Text = "2. Kalite ve Sevkiyat Talimatları:", Dock = DockStyle.Left, AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

        var btnPresetKural = new Button
        {
            Text = "➕  Standart Kurallar",
            Dock = DockStyle.Right,
            Size = new Size(160, 26),
            BackColor = ModernColors.Blue50,
            ForeColor = ModernColors.Blue600,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPresetKural.FlatAppearance.BorderColor = ModernColors.Blue600;
        btnPresetKural.Click += (_, _) => ShowRulesPresetsMenu(btnPresetKural, ctrl);

        pnlTalimatHdr.Controls.AddRange(new Control[] { lblTalimat, btnPresetKural });

        ctrl.TxtTalimatlar = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 120,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 8.5f),
            Text = NormalizeNewlines(sutun.TalimatlarMetin)
        };
        ctrl.TxtTalimatlar.TextChanged += (_, _) => TriggerPreviewUpdate();

        // Row 4: Parti Numaraları & Termoking
        var lblParti = new Label { Text = "3. Parti Numaraları, Termoking & Plaka:", Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        ctrl.TxtPartiler = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 70,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 8.5f),
            Text = NormalizeNewlines(sutun.PartilerMetin)
        };
        ctrl.TxtPartiler.TextChanged += (_, _) => TriggerPreviewUpdate();

        // Row 5: Sevkiyat Takvimi
        var lblTarihSec = new Label { Text = "4. Sevkiyat Takvimi (Analiz, Çıkış, Kapıkule, Varış):", Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        ctrl.TxtTarihler = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 65,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 8.5f),
            Text = NormalizeNewlines(sutun.TarihlerMetin)
        };
        ctrl.TxtTarihler.TextChanged += (_, _) => TriggerPreviewUpdate();

        // Reverse dock adding so layout flows Top to Bottom naturally
        pnlScroll.Controls.Add(ctrl.TxtTarihler);
        pnlScroll.Controls.Add(lblTarihSec);
        pnlScroll.Controls.Add(ctrl.TxtPartiler);
        pnlScroll.Controls.Add(lblParti);
        pnlScroll.Controls.Add(ctrl.TxtTalimatlar);
        pnlScroll.Controls.Add(pnlTalimatHdr);
        pnlScroll.Controls.Add(ctrl.TxtUrunler);
        pnlScroll.Controls.Add(pnlUrunHdr);
        pnlScroll.Controls.Add(pnlR1);

        tab.Controls.Add(pnlScroll);
        return ctrl;
    }

    private void ShowProductPresetsMenu(Button sourceBtn, ColumnInputControls ctrl)
    {
        var menu = new ContextMenuStrip();
        var presets = new (string title, string text)[]
        {
            ("Limon Enter (7 PLT)", "LİMON ENTER 30x40x18 MAVİ PLS : 7 PLT\r\n( 58mm+ ) ( SIZE 3-4 )\r\n( TARTILI ) ( NET : 9KG )\r\n( 100x120 PALET )"),
            ("Mandalin Primasol (9 PLT)", "MAND. PRİMASOL 18,5x29x12,5 : 9 PLT\r\nLACİVERT TEKASYA KUTU\r\n( 3KG MAVİ KUTU ) ( KIRMIZI FİLE )\r\n( TARTILI ) ( NET : 3KG )\r\n( 57mm+ )"),
            ("Nar Metro Chef (1 PLT)", "NAR 30x40x12 METRO CHEF KUTU : 1 PLT\r\n( 7-9 ) ( TORBALI )\r\n( TARTILI ) ( NET : 4,3 KG )"),
            ("Greyfurt Star (9 PLT)", "GREYF. STAR 40x60x18 METRO CHEF KUTU : 9 PLT\r\n( 40-55 ) ( TARTILI ) ( NET : 14KG )"),
            ("Elma Granny Smith (15 PLT)", "ELMA GRANNY SMITH 18KG : 15 PLT\r\nTELESKOPİK KUTU"),
            ("Elma Gala (11 PLT)", "ELMA GALA 18KG TELESKOPİK : 11 PLT\r\nKUTU"),
            ("Portakal Washington (26 PLT)", "PORTAKAL WASHINGTON 15KG : 26 PLT\r\nTEKASYA KARTON KUTU\r\n( 70-85mm )")
        };

        foreach (var p in presets)
        {
            menu.Items.Add(p.title, null, (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(ctrl.TxtUrunler.Text))
                    ctrl.TxtUrunler.Text = p.text;
                else
                    ctrl.TxtUrunler.Text += "\r\n\r\n" + p.text;
            });
        }

        menu.Show(sourceBtn, new Point(0, sourceBtn.Height));
    }

    private void ShowRulesPresetsMenu(Button sourceBtn, ColumnInputControls ctrl)
    {
        var menu = new ContextMenuStrip();
        var rules = new (string title, string text)[]
        {
            ("Metro Romanya Tam Kalite Kural Seti",
             "HER TANE ETİKETLİ OLACAKTIR.\r\nKÖŞEBENTLİ OLACAKTIR\r\nPALET ORTASINDA PALET OLACAK\r\nHER KUTU TEK BOY OLACAK\r\nNİZAMİ BİR ŞEKİLDE DİZİLECEKTİR\r\nÜRÜN EN ÜST KALİTEDE AVRUPA STANDARTINDA OLACAKTIR.\r\nANALİZ OLACAKTIR\r\nTEMİZ MAL OLACAKTIR\r\nGLOBAL GAP Lİ OLACAKTIR\r\nAB PESTİSİT KRİTERLERİNE UYGUN OLACAK\r\nMAL KESİNLİKLE 1. KALİTE OLACAKTIR.\r\nKALİTEYE ÇOK DİKKAT EDİLECEK\r\nYABANCI ETİKET KOYULACAK\r\nFULL ROMANYA ETİKETİ KULLANILACAK\r\nTIR'A 2 ADET SENSİTECH CASUS OLACAK\r\nCASUSLAR TIRIN EN ARKASINA DEĞİL TIRIN EN ORTASINDAKİ SAĞ VE SOL PALETLERE KOYULACAK\r\nTOPLAM BRÜT : 21.500 KG GEÇMEYECEK"),

            ("Ortadoğu / Körfez Standart Kural Seti",
             "TEKASYA TIR SURİYE ÜZERİ OLACAK\r\nBRÜT: 25.000 KG GEÇMEYECEK\r\nTEKASYA ARACI OLACAK"),

            ("+ Termoking +8 Derece", "TERMOKİNG DERECESİ : + 8"),
            ("+ Termoking +2 Derece", "TERMOKİNG DERECESİ : + 2"),
            ("+ 2 Adet Sensitech Casus Cihazı", "TIR'A 2 ADET SENSİTECH CASUS OLACAK\r\nCASUSLAR TIRIN EN ARKASINA DEĞİL TIRIN EN ORTASINDAKİ SAĞ VE SOL PALETLERE KOYULACAK"),
            ("+ Köşebent ve Çemberleme Kuralı", "KÖŞEBENTLİ OLACAKTIR\r\nPALET ORTASINDA PALET OLACAK\r\nNİZAMİ BİR ŞEKİLDE DİZİLECEKTİR")
        };

        foreach (var r in rules)
        {
            menu.Items.Add(r.title, null, (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(ctrl.TxtTalimatlar.Text))
                    ctrl.TxtTalimatlar.Text = r.text;
                else
                    ctrl.TxtTalimatlar.Text += "\r\n" + r.text;
            });
        }

        menu.Show(sourceBtn, new Point(0, sourceBtn.Height));
    }

    private void SyncFormToModel()
    {
        _prog.ProgramNo = _txtProgramNo.Text.Trim();
        _prog.Tarih = _dtpTarih.Value;
        _prog.Baslik = _txtBaslik.Text.Trim();
        var (satirlar, sutunlar) = GetCurrentGridDimensions();
        _prog.SatirSayisi = satirlar;
        _prog.SutunSayisi = sutunlar;
        _prog.Hazirlayan = _txtHazirlayan.Text.Trim();
        _prog.Onaylayan = _txtOnaylayan.Text.Trim();
        _prog.Durum = _cboDurum.SelectedItem?.ToString() ?? "Onaylandı";

        for (int i = 0; i < _colControls.Count; i++)
        {
            var ctrl = _colControls[i];
            var sutun = _prog.Sutunlar.FirstOrDefault(s => s.SatirNo == ctrl.SatirNo && s.SutunNo == ctrl.SutunNo)
                        ?? _prog.Sutunlar.FirstOrDefault(s => s.HucreIndex == ctrl.HucreIndex)
                        ?? _prog.Sutunlar.ElementAtOrDefault(i);
            if (sutun != null)
            {
                sutun.SatirNo = ctrl.SatirNo;
                sutun.SutunNo = ctrl.SutunNo;
                sutun.HucreIndex = ctrl.HucreIndex;
                sutun.Baslik = ctrl.TxtBaslik.Text.Trim();
                sutun.ToplamPalet = (int)ctrl.NumPalet.Value;
                sutun.UrunlerMetin = ctrl.TxtUrunler.Text;
                sutun.TalimatlarMetin = ctrl.TxtTalimatlar.Text;
                sutun.PartilerMetin = ctrl.TxtPartiler.Text;
                sutun.TarihlerMetin = ctrl.TxtTarihler.Text;
            }
        }

        _prog.ToplamPalet = _colControls.Sum(c => (int)c.NumPalet.Value);
    }

    private void UpdateTotalPalletHero()
    {
        int total = _colControls.Sum(c => (int)c.NumPalet.Value);
        var (satirlar, sutunlar) = GetCurrentGridDimensions();
        int totalCells = satirlar * sutunlar;
        _lblToplamPaletHero.Text = $"{total} PLT  •  {satirlar}x{sutunlar} ({totalCells} Bölüm)";
    }

    private void TriggerPreviewUpdate()
    {
        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    private void UpdateLivePreview()
    {
        try
        {
            SyncFormToModel();
            UpdateTotalPalletHero();

            int targetW = _picPreview.Width;
            int targetH = _picPreview.Height;
            if (targetW < 100 || targetH < 100)
            {
                targetW = 650;
                targetH = 920;
            }

            var bmp = new Bitmap(targetW, targetH);
            using (var g = Graphics.FromImage(bmp))
            {
                YuklemeProgramiReportRenderer.Render(g, _prog, new Rectangle(0, 0, targetW, targetH));
            }

            var oldImg = _picPreview.Image;
            _picPreview.Image = bmp;
            oldImg?.Dispose();
        }
        catch { }
    }

    private async System.Threading.Tasks.Task<bool> SaveInternalAsync()
    {
        try
        {
            SyncFormToModel();

            if (string.IsNullOrWhiteSpace(_prog.Baslik))
            {
                MessageBox.Show("Lütfen bir genel başlık veya hedef ülke/firma giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBaslik.Focus();
                return false;
            }

            // Keep only cells within the current grid dimensions
            int activeCount = _prog.SatirSayisi * _prog.SutunSayisi;
            _prog.Sutunlar = _prog.Sutunlar
                .Where(s => s.SatirNo >= 1 && s.SatirNo <= _prog.SatirSayisi && s.SutunNo >= 1 && s.SutunNo <= _prog.SutunSayisi)
                .Take(activeCount)
                .ToList();

            await _service.SaveAsync(_prog);
            NotificationService.NotifySuccess(
                "Yükleme Programı Kaydedildi",
                $"'{_prog.ProgramNo}' ({_prog.Baslik}) {_prog.SatirSayisi}x{_prog.SutunSayisi} Grid ({_prog.ToplamPalet} PLT) başarıyla kaydedildi.",
                "YuklemeProgrami");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void PrintDocument()
    {
        try
        {
            SyncFormToModel();
            var doc = YuklemeProgramiReportRenderer.CreatePrintDocument(_prog);
            using var dlg = new PrintPreviewDialog
            {
                Document = doc,
                Width = 900,
                Height = 700,
                StartPosition = FormStartPosition.CenterParent
            };
            dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Baskı önizleme açılırken hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportAsImage()
    {
        try
        {
            SyncFormToModel();
            using var sfd = new SaveFileDialog
            {
                Filter = "PNG Resmi (*.png)|*.png|JPEG Resmi (*.jpg)|*.jpg",
                FileName = $"Yukleme_Programi_{_prog.ProgramNo.Replace('/', '_')}.png"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                using var bmp = YuklemeProgramiReportRenderer.RenderToBitmap(_prog, 1600, 2260); // High res
                bmp.Save(sfd.FileName);
                NotificationService.NotifySuccess(
                    "Görsel Dışa Aktarıldı",
                    $"'{Path.GetFileName(sfd.FileName)}' yüksek çözünürlüklü olarak kaydedildi.",
                    "YuklemeProgrami");
                MessageBox.Show($"Yükleme programı başarıyla kaydedildi:\n{sfd.FileName}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dışa aktarma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShareViaWhatsApp()
    {
        try
        {
            SyncFormToModel();
            using var dlg = new WhatsAppShareDialog(_prog);
            dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WhatsApp paylaşım penceresi açılırken hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string NormalizeNewlines(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\r\n", "\n").Replace("\n", "\r\n");
    }
}
