using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Forklift Operatörleri Tanımlama ve Yönetim Penceresi
/// </summary>
public class ForkliftOperatorTanimDialog : Form
{
    private readonly YuklemeProgramiService _service = new();
    private DataGridView _grid = null!;
    private List<ForkliftOperatorDto> _operatorler = new();
    private Label _lblStatus = null!;

    public ForkliftOperatorDto? SelectedOperator { get; private set; }

    public ForkliftOperatorTanimDialog()
    {
        this.DoubleBuffered = true;
        InitializeComponents();
        this.Load += async (_, _) => await RefreshDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "🚜 Forklift Operatörleri Tanımlama ve Yönetim Rehberi";
        this.Size = new Size(950, 620);
        this.MinimumSize = new Size(800, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. ÜST HEADER
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(20, 12, 20, 12)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblIcon = new Label
        {
            Text = "🚜",
            Font = new Font("Segoe UI", 22f),
            Location = new Point(16, 10),
            Size = new Size(46, 46)
        };

        var lblTitle = new Label
        {
            Text = "Forklift Operatörleri Tanımlama & Yetkilendirme",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(68, 12),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Tır yükleme rampası, paketleme ve soğuk hava depolarında yükleme yapan sertifikalı forklift operatörleri",
            Font = new Font("Segoe UI", 8.8f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(69, 36),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSub });

        // 2. TOOLBAR
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        pnlToolbar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1);
        };

        var btnNew = CreateButton("➕  Yeni Operatör", ModernColors.Emerald500, Color.White, 150);
        btnNew.Click += (_, _) => OpenEditDialog(null);

        var btnEdit = CreateButton("✏️  Düzenle", ModernColors.Blue600, Color.White, 110);
        btnEdit.Click += (_, _) => EditSelected();

        var btnDelete = CreateButton("🗑️  Sil", Color.FromArgb(239, 68, 68), Color.White, 95);
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

        var btnRefresh = CreateButton("🔄  Yenile", ModernColors.Slate200, ModernColors.Slate800, 95);
        btnRefresh.Click += async (_, _) => await RefreshDataAsync();

        var flpButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        flpButtons.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDelete, btnRefresh });
        pnlToolbar.Controls.Add(flpButtons);

        // 3. DATA GRID
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(241, 245, 249),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 36 }
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernColors.Slate700,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };
        _grid.ColumnHeadersHeight = 38;

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = Color.FromArgb(238, 242, 255),
            SelectionForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(8, 0, 8, 0)
        };

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "# ID", DataPropertyName = "Id", Width = 60 },
            new DataGridViewTextBoxColumn { HeaderText = "Operatör Adı Soyadı", DataPropertyName = "AdSoyad", Width = 190 },
            new DataGridViewTextBoxColumn { HeaderText = "Telefon", DataPropertyName = "Telefon", Width = 120 },
            new DataGridViewTextBoxColumn { HeaderText = "Ehliyet / Belge No", DataPropertyName = "EhliyetNo", Width = 130 },
            new DataGridViewTextBoxColumn { HeaderText = "Forklift Kodu / Makine", DataPropertyName = "ForkliftKodu", Width = 160 },
            new DataGridViewTextBoxColumn { HeaderText = "Vardiya", DataPropertyName = "Vardiya", Width = 130 },
            new DataGridViewTextBoxColumn { HeaderText = "Durum", DataPropertyName = "Durum", Width = 90 },
            new DataGridViewTextBoxColumn { HeaderText = "Açıklama / Görev", DataPropertyName = "Aciklama", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
        );

        _grid.CellPainting += Grid_CellPainting;
        _grid.CellDoubleClick += (_, _) => EditSelected();

        // 4. STATUS FOOTER
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        pnlFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        _lblStatus = new Label
        {
            Text = "Operatörler yükleniyor...",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.8f),
            ForeColor = ModernColors.Slate600
        };
        pnlFooter.Controls.Add(_lblStatus);

        this.Controls.Add(_grid);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlFooter);
    }

    private Button CreateButton(string text, Color bg, Color fg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, 32),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 0, 4, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            _lblStatus.Text = "Forklift operatörleri yükleniyor...";
            _operatorler = await _service.GetForkliftOperatorlerAsync();
            _grid.DataSource = null;
            _grid.DataSource = _operatorler;
            _lblStatus.Text = $"Toplam {_operatorler.Count} forklift operatörü kayıtlı.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Hata: {ex.Message}";
        }
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.Graphics == null) return;

        // Durum Kolonu (Aktif / Pasif Pill Badge)
        if (_grid.Columns["Durum"] != null && e.ColumnIndex == _grid.Columns["Durum"]!.Index)
        {
            e.PaintBackground(e.CellBounds, true);
            var val = e.Value?.ToString() ?? "Aktif";
            bool isAktif = val.Equals("Aktif", StringComparison.OrdinalIgnoreCase);

            Color bg = isAktif ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 226, 226);
            Color fg = isAktif ? Color.FromArgb(22, 101, 52) : Color.FromArgb(185, 28, 28);

            var badgeRect = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 7, 70, 22);
            using var brushBg = new SolidBrush(bg);
            using var brushFg = new SolidBrush(fg);
            using var pen = new Pen(fg, 0.8f);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillRectangle(brushBg, badgeRect);
            e.Graphics.DrawRectangle(pen, badgeRect);

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI", 7.8f, FontStyle.Bold);
            e.Graphics.DrawString(val, font, brushFg, badgeRect, sf);

            e.Handled = true;
        }
    }

    private void EditSelected()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Lütfen düzenlemek istediğiniz operatörü seçin.", "Seçim Yapılmadı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var op = _grid.SelectedRows[0].DataBoundItem as ForkliftOperatorDto;
        if (op != null)
        {
            OpenEditDialog(op);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (_grid.SelectedRows.Count == 0) return;
        var op = _grid.SelectedRows[0].DataBoundItem as ForkliftOperatorDto;
        if (op == null) return;

        var res = MessageBox.Show($"'{op.AdSoyad}' isimli forklift operatörünü silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            await _service.DeleteForkliftOperatorAsync(op.Id);
            await RefreshDataAsync();
        }
    }

    private void OpenEditDialog(ForkliftOperatorDto? existing)
    {
        using var dlg = new ForkliftOperatorEditSubDialog(existing);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = RefreshDataAsync();
        }
    }
}

/// <summary>
/// Forklift Operatörü Ekleme / Düzenleme Modal Alt Formu
/// </summary>
public class ForkliftOperatorEditSubDialog : Form
{
    private readonly YuklemeProgramiService _service = new();
    private readonly ForkliftOperatorDto _model;
    private readonly bool _isNew;

    private TextBox _txtAdSoyad = null!;
    private TextBox _txtTelefon = null!;
    private TextBox _txtEhliyetNo = null!;
    private TextBox _txtForkliftKodu = null!;
    private ComboBox _cboVardiya = null!;
    private ComboBox _cboDurum = null!;
    private TextBox _txtAciklama = null!;

    public ForkliftOperatorEditSubDialog(ForkliftOperatorDto? model)
    {
        _isNew = model == null;
        _model = model ?? new ForkliftOperatorDto();

        InitializeComponents();
        LoadModel();
    }

    private void InitializeComponents()
    {
        this.Text = _isNew ? "➕ Yeni Forklift Operatörü Ekle" : $"✏️ Forklift Operatörü Düzenle — {_model.AdSoyad}";
        this.Size = new Size(540, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9f);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = _isNew ? "🚜 Yeni Forkliftçi Tanımı" : $"🚜 {_model.AdSoyad}",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(16, 8),
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Operatör adı, ehliyet no, kullandığı makine kodu ve vardiya bilgileri",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(17, 30),
            AutoSize = true
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // Body Form
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 16),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        int y = 14;
        void AddRow(string labelText, Control ctrl, int h = 26)
        {
            var lbl = new Label { Text = labelText, Location = new Point(20, y + 3), AutoSize = true, Font = new Font("Segoe UI", 8.8f, FontStyle.Bold), ForeColor = ModernColors.Slate700 };
            ctrl.Location = new Point(170, y);
            ctrl.Width = 320;
            pnlBody.Controls.AddRange(new Control[] { lbl, ctrl });
            y += h + 14;
        }

        _txtAdSoyad = new TextBox();
        AddRow("Adı Soyadı (*):", _txtAdSoyad);

        _txtTelefon = new TextBox();
        AddRow("İletişim / Cep Tel:", _txtTelefon);

        _txtEhliyetNo = new TextBox();
        AddRow("Ehliyet / Belge No:", _txtEhliyetNo);

        _txtForkliftKodu = new TextBox();
        AddRow("Forklift / Makine Kodu:", _txtForkliftKodu);

        _cboVardiya = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cboVardiya.Items.AddRange(new object[] { "1. Vardiya (Gündüz)", "2. Vardiya (Akşam)", "3. Vardiya (Gece)", "Genel / Joker" });
        _cboVardiya.SelectedIndex = 0;
        AddRow("Çalışma Vardiyası:", _cboVardiya);

        _cboDurum = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cboDurum.Items.AddRange(new object[] { "Aktif", "Pasif" });
        _cboDurum.SelectedIndex = 0;
        AddRow("Durum:", _cboDurum);

        _txtAciklama = new TextBox { Multiline = true, Height = 50 };
        AddRow("Görev / Notlar:", _txtAciklama, 50);

        // Footer Buttons
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        pnlFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        var btnSave = new Button
        {
            Text = "💾  Kaydet",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Emerald500,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 34),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (_, _) => await SaveAsync();

        var btnCancel = new Button
        {
            Text = "İptal",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 34),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        pnlFooter.Controls.AddRange(new Control[] { btnSave, btnCancel });

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);
    }

    private void LoadModel()
    {
        _txtAdSoyad.Text = _model.AdSoyad;
        _txtTelefon.Text = _model.Telefon;
        _txtEhliyetNo.Text = _model.EhliyetNo;
        _txtForkliftKodu.Text = _model.ForkliftKodu;
        if (!string.IsNullOrWhiteSpace(_model.Vardiya)) _cboVardiya.SelectedItem = _model.Vardiya;
        if (!string.IsNullOrWhiteSpace(_model.Durum)) _cboDurum.SelectedItem = _model.Durum;
        _txtAciklama.Text = _model.Aciklama;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtAdSoyad.Text))
        {
            MessageBox.Show("Lütfen forklift operatörünün Adı Soyadı alanını doldurun.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtAdSoyad.Focus();
            return;
        }

        _model.AdSoyad = _txtAdSoyad.Text.Trim();
        _model.Telefon = _txtTelefon.Text.Trim();
        _model.EhliyetNo = _txtEhliyetNo.Text.Trim();
        _model.ForkliftKodu = _txtForkliftKodu.Text.Trim();
        _model.Vardiya = _cboVardiya.SelectedItem?.ToString() ?? "1. Vardiya (Gündüz)";
        _model.Durum = _cboDurum.SelectedItem?.ToString() ?? "Aktif";
        _model.Aciklama = _txtAciklama.Text.Trim();

        await _service.SaveForkliftOperatorAsync(_model);
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
