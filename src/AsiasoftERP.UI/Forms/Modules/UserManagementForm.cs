using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Executive User & Permission Management module directly backed by MariaDB 'user' and 'sirketkullanicilari'
/// </summary>
public class UserManagementForm : Form
{
    private readonly MacroService _macroService = new();
    private List<ErpUserDto> _allUsers = new();
    private List<CompanyDto> _companies = new();

    private DataGridView _grid = null!;
    private Panel _pnlToolbar = null!;
    private Panel _pnlBottomStatus = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cmbRoleFilter = null!;
    private ComboBox _cmbStatusFilter = null!;
    private Label _lblStats = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnPassword = null!;
    private Button _btnToggleStatus = null!;
    private Button _btnDelete = null!;
    private Button _btnRefresh = null!;

    public UserManagementForm()
    {
        this.DoubleBuffered = true;
        this.Text = "👑 Kullanıcı Yönetimi";
        this.Size = new Size(1150, 680);
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        InitializeUI();
        _ = LoadDataAsync();
    }

    private void InitializeUI()
    {
        // 1. Top Toolbar Panel
        _pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.White,
            Padding = new Padding(12, 8, 12, 8)
        };
        _pnlToolbar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, _pnlToolbar.Height - 1, _pnlToolbar.Width, _pnlToolbar.Height - 1);
        };

        int x = 12;

        _btnNew = CreateToolButton("➕ Yeni Kullanıcı", Color.FromArgb(4, 120, 87), Color.FromArgb(236, 253, 245), Color.FromArgb(167, 243, 208), x, 130);
        _btnNew.Click += (_, _) => OpenUserDialog(null);
        x += 138;

        _btnEdit = CreateToolButton("✏️ Düzenle", Color.FromArgb(51, 65, 85), Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), x, 95);
        _btnEdit.Click += (_, _) => EditSelectedUser();
        x += 102;

        _btnPassword = CreateToolButton("🔑 Şifre Değiştir", Color.FromArgb(67, 56, 202), Color.FromArgb(238, 242, 255), Color.FromArgb(199, 210, 254), x, 125);
        _btnPassword.Click += async (_, _) => await OpenPasswordDialogAsync();
        x += 132;

        _btnToggleStatus = CreateToolButton("⊘ Aktif/Pasif", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 243, 199), Color.FromArgb(253, 230, 138), x, 110);
        _btnToggleStatus.Click += async (_, _) => await ToggleStatusAsync();
        x += 117;

        _btnDelete = CreateToolButton("🗑️ Sil", Color.FromArgb(190, 18, 60), Color.FromArgb(255, 241, 242), Color.FromArgb(254, 205, 211), x, 80);
        _btnDelete.Click += async (_, _) => await DeleteSelectedUserAsync();
        x += 87;

        _btnRefresh = CreateToolButton("⟳ Yenile", Color.FromArgb(51, 65, 85), Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), x, 85);
        _btnRefresh.Click += async (_, _) => await LoadDataAsync();
        x += 92;

        var btnLogs = CreateToolButton("🛡️ Giriş Logları", Color.FromArgb(79, 70, 229), Color.FromArgb(238, 242, 255), Color.FromArgb(199, 210, 254), x, 125);
        btnLogs.Click += (_, _) =>
        {
            using var logsForm = new UserLoginLogsForm();
            logsForm.ShowDialog(this);
        };
        x += 132;

        var btnPerms = CreateToolButton("🔐 Modül Yetkileri", Color.FromArgb(13, 148, 136), Color.FromArgb(240, 253, 250), Color.FromArgb(153, 246, 228), x, 145);
        btnPerms.Click += (_, _) => OpenUserPermissions();
        x += 152;

        _pnlToolbar.Controls.AddRange(new Control[] { _btnNew, _btnEdit, _btnPassword, _btnToggleStatus, _btnDelete, _btnRefresh, btnLogs, btnPerms });

        // Right-aligned Filters and Search Box
        var pnlFilters = new Panel
        {
            Dock = DockStyle.Right,
            Width = 490,
            BackColor = Color.Transparent
        };

        // Search container
        var pnlSearch = new Panel
        {
            Size = new Size(190, 32),
            Location = new Point(290, 2),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        var lblSearchIcon = new Label
        {
            Text = "🔍",
            Size = new Size(24, 32),
            Location = new Point(4, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        _txtSearch = new TextBox
        {
            Location = new Point(28, 6),
            Width = 155,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Kullanıcı ara..."
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();
        pnlSearch.Controls.AddRange(new Control[] { lblSearchIcon, _txtSearch });

        // Role Filter
        _cmbRoleFilter = new ComboBox
        {
            Location = new Point(145, 4),
            Width = 135,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cmbRoleFilter.Items.AddRange(new object[] { "Tüm Roller", "👑 Yöneticiler", "👤 Standart", "💻 Terminal" });
        _cmbRoleFilter.SelectedIndex = 0;
        _cmbRoleFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        // Status Filter
        _cmbStatusFilter = new ComboBox
        {
            Location = new Point(10, 4),
            Width = 125,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cmbStatusFilter.Items.AddRange(new object[] { "Tüm Durumlar", "🟢 Aktifler", "🔴 Pasifler" });
        _cmbStatusFilter.SelectedIndex = 0;
        _cmbStatusFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        pnlFilters.Controls.AddRange(new Control[] { _cmbStatusFilter, _cmbRoleFilter, pnlSearch });
        _pnlToolbar.Controls.Add(pnlFilters);

        // 2. Bottom Status Strip
        _pnlBottomStatus = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 0, 12, 0)
        };
        _pnlBottomStatus.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, _pnlBottomStatus.Width, 0);
        };

        _lblStats = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = 600,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ModernColors.Slate600,
            Text = "Yükleniyor..."
        };

        var lblHint = new Label
        {
            Dock = DockStyle.Right,
            AutoSize = false,
            Width = 450,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = ModernColors.Slate500,
            Text = "💡 Çift tıklayarak kullanıcıyı ve izinlerini düzenleyebilirsiniz."
        };

        _pnlBottomStatus.Controls.AddRange(new Control[] { _lblStats, lblHint });

        // 3. Main DataGridView
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };

        ModernUiHelper.ApplyModernGridStyle(_grid);
        _grid.RowTemplate.Height = 36;

        SetupGridColumns();

        // Custom cell painting for badge pills
        _grid.CellPainting += Grid_CellPainting;
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelectedUser();
        };

        // Context Menu
        var contextMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9f) };
        contextMenu.Items.Add("✏️ Düzenle", null, (_, _) => EditSelectedUser());
        contextMenu.Items.Add("🔐 Modül Yetkilerini Yönet", null, (_, _) => OpenUserPermissions());
        contextMenu.Items.Add("🔑 Şifre Değiştir", null, async (_, _) => await OpenPasswordDialogAsync());
        contextMenu.Items.Add("⊘ Aktif / Pasif Yap", null, async (_, _) => await ToggleStatusAsync());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("🗑️ Kullanıcıyı Sil", null, async (_, _) => await DeleteSelectedUserAsync());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("⟳ Listeyi Yenile", null, async (_, _) => await LoadDataAsync());
        _grid.ContextMenuStrip = contextMenu;

        this.Controls.Add(_grid);
        this.Controls.Add(_pnlToolbar);
        this.Controls.Add(_pnlBottomStatus);
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                Name = "colKodu",
                DataPropertyName = "Kodu",
                HeaderText = "Kullanıcı No",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colAdi",
                DataPropertyName = "Adi",
                HeaderText = "Adı Soyadı",
                Width = 230,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colRole",
                DataPropertyName = "RoleDisplay",
                HeaderText = "Yetki / Rol",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colDurum",
                DataPropertyName = "StatusDisplay",
                HeaderText = "Durum",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colSirketler",
                DataPropertyName = "Sirketler",
                HeaderText = "Yetkili Şirketler",
                Width = 300,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    ForeColor = ModernColors.Slate700
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colOzelKodu",
                DataPropertyName = "OzelKodu",
                HeaderText = "Özel Kod",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colSifre",
                HeaderText = "Şifre",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = ModernColors.Slate500
                }
            }
        );

        _grid.CellFormatting += (s, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "colSifre")
            {
                e.Value = "••••••";
                e.FormattingApplied = true;
            }
        };
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0) return;

        string colName = _grid.Columns[e.ColumnIndex].Name;

        if (colName == "colRole" || colName == "colDurum")
        {
            e.PaintBackground(e.CellBounds, true);

            string text = e.Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(text)) return;

            Color bgColor;
            Color textColor;
            Color borderColor;

            if (colName == "colRole")
            {
                if (text.Contains("Yönetici"))
                {
                    bgColor = Color.FromArgb(238, 242, 255);
                    textColor = Color.FromArgb(67, 56, 202);
                    borderColor = Color.FromArgb(199, 210, 254);
                }
                else if (text.Contains("Terminal"))
                {
                    bgColor = Color.FromArgb(254, 243, 199);
                    textColor = Color.FromArgb(180, 83, 9);
                    borderColor = Color.FromArgb(253, 230, 138);
                }
                else
                {
                    bgColor = Color.FromArgb(241, 245, 249);
                    textColor = Color.FromArgb(51, 65, 85);
                    borderColor = Color.FromArgb(226, 232, 240);
                }
            }
            else // colDurum
            {
                if (text.Contains("Aktif"))
                {
                    bgColor = Color.FromArgb(220, 252, 231);
                    textColor = Color.FromArgb(21, 128, 61);
                    borderColor = Color.FromArgb(134, 239, 172);
                }
                else
                {
                    bgColor = Color.FromArgb(254, 226, 226);
                    textColor = Color.FromArgb(185, 28, 28);
                    borderColor = Color.FromArgb(252, 165, 165);
                }
            }

            if (e.Graphics == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var pillRect = new Rectangle(
                e.CellBounds.X + (e.CellBounds.Width - 96) / 2,
                e.CellBounds.Y + (e.CellBounds.Height - 24) / 2,
                96, 24);

            using var path = ModernUiHelper.GetRoundedPath(pillRect, 6);
            using var brush = new SolidBrush(bgColor);
            g.FillPath(brush, path);

            using var pen = new Pen(borderColor, 1f);
            g.DrawPath(pen, path);

            TextRenderer.DrawText(g, text, new Font("Segoe UI", 8.5f, FontStyle.Bold), pillRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
    }

    private Button CreateToolButton(string text, Color foreColor, Color backColor, Color borderColor, int x, int width)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 9),
            Size = new Size(width, 34),
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = borderColor;
        btn.FlatAppearance.BorderSize = 1;
        return btn;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _btnRefresh.Enabled = false;
            _lblStats.Text = "Veriler yükleniyor...";

            _companies = await _macroService.GetCompaniesAsync();
            _allUsers = await _macroService.GetAllErpUsersAsync();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kullanıcılar yüklenirken bir hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnRefresh.Enabled = true;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allUsers.AsEnumerable();

        // 1. Role Filter
        if (_cmbRoleFilter.SelectedIndex == 1) // Yöneticiler
            filtered = filtered.Where(u => u.IsAdmin);
        else if (_cmbRoleFilter.SelectedIndex == 2) // Standart
            filtered = filtered.Where(u => u.Yetki == "S");
        else if (_cmbRoleFilter.SelectedIndex == 3) // Terminal
            filtered = filtered.Where(u => u.Yetki == "N");

        // 2. Status Filter
        if (_cmbStatusFilter.SelectedIndex == 1) // Aktifler
            filtered = filtered.Where(u => u.IsActive);
        else if (_cmbStatusFilter.SelectedIndex == 2) // Pasifler
            filtered = filtered.Where(u => !u.IsActive);

        // 3. Search text
        string search = _txtSearch.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(u =>
                u.Kodu.ToLower().Contains(search) ||
                u.Adi.ToLower().Contains(search) ||
                u.OzelKodu.ToLower().Contains(search) ||
                u.Sirketler.ToLower().Contains(search) ||
                u.RoleDisplay.ToLower().Contains(search)
            );
        }

        var list = filtered.ToList();
        _grid.DataSource = list;

        int total = _allUsers.Count;
        int active = _allUsers.Count(u => u.IsActive);
        int passive = total - active;
        int admins = _allUsers.Count(u => u.IsAdmin);

        _lblStats.Text = $"👥 Toplam {total} Kullanıcı  |  🟢 {active} Aktif  |  🔴 {passive} Pasif  |  👑 {admins} Yönetici  (Gösterilen: {list.Count})";
    }

    private ErpUserDto? GetSelectedUser()
    {
        if (_grid.CurrentRow?.DataBoundItem is ErpUserDto user)
            return user;
        return null;
    }

    private void OpenUserDialog(ErpUserDto? user)
    {
        using var dlg = new UserEditDialog(user, _companies, _macroService);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _ = LoadDataAsync();
        }
    }

    private void EditSelectedUser()
    {
        var user = GetSelectedUser();
        if (user == null)
        {
            MessageBox.Show("Lütfen düzenlemek için bir kullanıcı seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenUserDialog(user);
    }

    private void OpenUserPermissions()
    {
        var user = GetSelectedUser();
        using var dlg = new UserPermissionsDialog(user?.Kodu);
        dlg.ShowDialog(this);
    }

    private async Task OpenPasswordDialogAsync()
    {
        var user = GetSelectedUser();
        if (user == null)
        {
            MessageBox.Show("Lütfen şifresini değiştirmek istediğiniz kullanıcıyı seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new QuickPasswordDialog(user.Kodu, user.Adi);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            bool ok = await _macroService.ResetErpUserPasswordAsync(user.Kodu, dlg.NewPassword);
            if (ok)
            {
                user.Sifre = dlg.NewPassword;
                MessageBox.Show($"'{user.Adi}' kullanıcısının şifresi başarıyla güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _grid.Refresh();
            }
            else
            {
                MessageBox.Show("Şifre güncellenirken bir hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task ToggleStatusAsync()
    {
        var user = GetSelectedUser();
        if (user == null)
        {
            MessageBox.Show("Lütfen durumunu değiştirmek istediğiniz kullanıcıyı seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string actionText = user.IsActive ? "PASİF" : "AKTİF";
        var res = MessageBox.Show(
            $"'{user.Adi}' ({user.Kodu}) isimli kullanıcı {actionText} duruma getirilsin mi?",
            "Durum Onayı",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            var (success, newStatus) = await _macroService.ToggleErpUserStatusAsync(user.Kodu);
            if (success)
            {
                user.Durum = newStatus;
                ApplyFilter();
                _grid.Refresh();
            }
            else
            {
                MessageBox.Show($"Durum güncellenirken hata oluştu:\n{newStatus}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task DeleteSelectedUserAsync()
    {
        var user = GetSelectedUser();
        if (user == null)
        {
            MessageBox.Show("Lütfen silmek istediğiniz kullanıcıyı seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (user.Kodu == "1" || user.Kodu == "100")
        {
            MessageBox.Show("Sistem ana yöneticisi silinemez!", "Güvenlik Engeli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var res = MessageBox.Show(
            $"'{user.Adi}' ({user.Kodu}) isimli kullanıcıyı ve tüm şirket yetkilerini kalıcı olarak silmek istediğinizden emin misiniz?\n\nBu işlem geri alınamaz!",
            "Kullanıcı Silme Onayı",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (res == DialogResult.Yes)
        {
            var (success, msg) = await _macroService.DeleteErpUserAsync(user.Kodu);
            if (success)
            {
                NotificationService.NotifyWarning(
                    "Kullanıcı Silindi",
                    $"'{user.Adi}' ({user.Kodu}) kullanıcısı sistemden silindi.",
                    "UserMgmt");
                MessageBox.Show("Kullanıcı başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show($"Kullanıcı silinemedi:\n{msg}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

/// <summary>
/// Modal dialog for creating or editing an ERP user with full permissions and company mapping
/// </summary>
public class UserEditDialog : Form
{
    private readonly ErpUserDto? _existing;
    private readonly List<CompanyDto> _companies;
    private readonly MacroService _macroService;

    // Controls - Tab 1: Temel Bilgiler
    private TextBox _txtKodu = null!;
    private TextBox _txtAdi = null!;
    private TextBox _txtSifre = null!;
    private ComboBox _cmbYetki = null!;
    private CheckBox _chkDurum = null!;
    private TextBox _txtOzelKodu = null!;
    private Button _btnTogglePassword = null!;

    // Controls - Tab 2: Şirketler
    private CheckedListBox _clbCompanies = null!;

    // Controls - Tab 3: Modül Yetkileri
    private CheckBox _chkCari = null!, _chkKasa = null!, _chkEkstre = null!, _chkBorcAlacak = null!, _chkPerakende = null!, _chkFis = null!;
    private CheckBox _chkSatis = null!, _chkAlis = null!;
    private CheckBox _chkTahsilat = null!, _chkOdeme = null!, _chkMustCek = null!, _chkMustSenet = null!, _chkFirmaCek = null!, _chkDekont = null!;
    private CheckBox _chkStok = null!, _chkFiyatGor = null!, _chkDepoStok = null!;
    private CheckBox _chkGunSonu = null!, _chkIsletme = null!, _chkKarlilik = null!, _chkCekRapor = null!, _chkAylikAnaliz = null!;

    public UserEditDialog(ErpUserDto? existing, List<CompanyDto> companies, MacroService macroService)
    {
        _existing = existing;
        _companies = companies;
        _macroService = macroService;

        this.DoubleBuffered = true;
        this.Text = existing == null ? "➕ Yeni Kullanıcı Tanımla" : $"✏️ Kullanıcı Düzenle — {existing.Adi} ({existing.Kodu})";
        this.Size = new Size(740, 640);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        InitializeComponents();
        LoadUserData();
    }

    private void InitializeComponents()
    {
        // 1. Header Banner
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = ModernColors.Slate800,
            Padding = new Padding(20, 12, 20, 12)
        };

        var lblHeaderTitle = new Label
        {
            Text = _existing == null ? "👑 Yeni ERP Kullanıcı Tanımlama" : $"✏️ Kullanıcı Düzenleme: {_existing.Adi}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(18, 10)
        };

        var lblHeaderSub = new Label
        {
            Text = "Kullanıcı temel bilgilerini, şirket erişimlerini ve detaylı modül yetkilerini yönetin.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 35)
        };

        pnlHeader.Controls.AddRange(new Control[] { lblHeaderTitle, lblHeaderSub });

        // 2. Bottom Action Panel
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        var btnSave = new Button
        {
            Text = "💾 Kaydet ve Uygula",
            Location = new Point(pnlBottom.Width - 280, 10),
            Size = new Size(160, 36),
            BackColor = Color.FromArgb(4, 120, 87),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveUserAsync();

        var btnCancel = new Button
        {
            Text = "Vazgeç",
            Location = new Point(pnlBottom.Width - 110, 10),
            Size = new Size(90, 36),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnCancel.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        pnlBottom.Controls.AddRange(new Control[] { btnSave, btnCancel });

        // 3. Tab Control in Center
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            Padding = new Point(14, 6)
        };

        var tabBasic = new TabPage { Text = " 👤 Temel Bilgiler ", BackColor = Color.White, Padding = new Padding(24) };
        var tabCompanies = new TabPage { Text = " 🏢 Şirket Yetkileri ", BackColor = Color.White, Padding = new Padding(24) };
        var tabPermissions = new TabPage { Text = " 🔐 Modül Yetkileri ", BackColor = Color.White, Padding = new Padding(20) };

        BuildBasicTab(tabBasic);
        BuildCompaniesTab(tabCompanies);
        BuildPermissionsTab(tabPermissions);

        tabControl.TabPages.AddRange(new TabPage[] { tabBasic, tabCompanies, tabPermissions });

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlBottom);
    }

    private void BuildBasicTab(TabPage page)
    {
        int y = 20;

        void AddField(string labelText, Control ctrl, string? note = null)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(24, y + 4),
                Width = 140,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ModernColors.Slate800
            };
            ctrl.Location = new Point(175, y);
            ctrl.Width = 320;
            page.Controls.Add(lbl);
            page.Controls.Add(ctrl);

            if (!string.IsNullOrEmpty(note))
            {
                var lblNote = new Label
                {
                    Text = note,
                    Location = new Point(505, y + 4),
                    Width = 180,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = ModernColors.Slate500
                };
                page.Controls.Add(lblNote);
            }

            y += 44;
        }

        _txtKodu = new TextBox { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
        if (_existing != null)
        {
            _txtKodu.ReadOnly = true;
            _txtKodu.BackColor = Color.FromArgb(241, 245, 249);
        }

        _txtAdi = new TextBox { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };

        var pnlPw = new Panel { Size = new Size(320, 28) };
        _txtSifre = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true,
            Font = new Font("Segoe UI", 9.5f),
            Width = 275,
            Location = new Point(0, 0)
        };
        _btnTogglePassword = new Button
        {
            Text = "👁️",
            Location = new Point(280, 0),
            Size = new Size(40, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(241, 245, 249),
            Cursor = Cursors.Hand
        };
        _btnTogglePassword.FlatAppearance.BorderColor = ModernColors.Slate200;
        _btnTogglePassword.Click += (s, e) =>
        {
            _txtSifre.UseSystemPasswordChar = !_txtSifre.UseSystemPasswordChar;
            _btnTogglePassword.Text = _txtSifre.UseSystemPasswordChar ? "👁️" : "🙈";
        };
        pnlPw.Controls.AddRange(new Control[] { _txtSifre, _btnTogglePassword });

        _cmbYetki = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbYetki.Items.AddRange(new object[] { "👑 Yönetici (Tam Yetkili)", "👤 Standart Kullanıcı", "💻 Terminal / Kısıtlı Kullanıcı" });
        _cmbYetki.SelectedIndex = 1;
        _cmbYetki.SelectedIndexChanged += (s, e) =>
        {
            if (_cmbYetki.SelectedIndex == 0)
            {
                // Select all companies & permissions when Admin is selected
                SelectAllCompanies(true);
                SelectAllPermissions(true);
            }
        };

        _chkDurum = new CheckBox
        {
            Text = "🟢 Aktif Hesap (Sisteme giriş yapabilir)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(21, 128, 61),
            Checked = true,
            AutoSize = true
        };

        _txtOzelKodu = new TextBox { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };

        AddField("Kullanıcı Kodu *", _txtKodu, "Tekil ID (Örn: 101)");
        AddField("Adı Soyadı *", _txtAdi, "Zorunlu alan");
        AddField("Giriş Şifresi *", pnlPw, "Düz metin parola");
        AddField("Yetki Seviyesi", _cmbYetki, "Sistem rolü");
        AddField("Hesap Durumu", _chkDurum);
        AddField("Özel Kod", _txtOzelKodu, "Baş harfler (Örn: AS)");
    }

    private void BuildCompaniesTab(TabPage page)
    {
        var lblDesc = new Label
        {
            Text = "Bu kullanıcının oturum açıp işlem yapmaya yetkili olduğu şirketleri işaretleyiniz:",
            Location = new Point(24, 16),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800
        };

        _clbCompanies = new CheckedListBox
        {
            Location = new Point(24, 48),
            Width = 480,
            Height = 220,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f),
            CheckOnClick = true
        };

        foreach (var comp in _companies)
        {
            _clbCompanies.Items.Add(comp, false);
        }

        var pnlActions = new Panel { Location = new Point(520, 48), Width = 170, Height = 220 };
        var btnSelectAll = new Button
        {
            Text = "✓ Tüm Şirketleri Seç",
            Location = new Point(0, 0),
            Size = new Size(160, 34),
            BackColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(67, 56, 202),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSelectAll.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
        btnSelectAll.Click += (s, e) => SelectAllCompanies(true);

        var btnClearAll = new Button
        {
            Text = "✕ Seçimleri Kaldır",
            Location = new Point(0, 44),
            Size = new Size(160, 34),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnClearAll.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnClearAll.Click += (s, e) => SelectAllCompanies(false);

        pnlActions.Controls.AddRange(new Control[] { btnSelectAll, btnClearAll });

        page.Controls.AddRange(new Control[] { lblDesc, _clbCompanies, pnlActions });
    }

    private void SelectAllCompanies(bool select)
    {
        for (int i = 0; i < _clbCompanies.Items.Count; i++)
        {
            _clbCompanies.SetItemChecked(i, select);
        }
    }

    private void BuildPermissionsTab(TabPage page)
    {
        var pnlScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        // Quick toolbar
        var pnlQuick = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White };
        var btnSelectAll = new Button
        {
            Text = "⚡ Tüm Yetkileri Ver",
            Location = new Point(0, 2),
            Size = new Size(140, 30),
            BackColor = Color.FromArgb(236, 253, 245),
            ForeColor = Color.FromArgb(4, 120, 87),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSelectAll.FlatAppearance.BorderColor = Color.FromArgb(167, 243, 208);
        btnSelectAll.Click += (s, e) => SelectAllPermissions(true);

        var btnClearAll = new Button
        {
            Text = "🧹 Tümünü Kaldır",
            Location = new Point(148, 2),
            Size = new Size(130, 30),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnClearAll.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnClearAll.Click += (s, e) => SelectAllPermissions(false);

        pnlQuick.Controls.AddRange(new Control[] { btnSelectAll, btnClearAll });

        // Group 1: Cari & Finans
        var grpCari = CreateGroupBox("👥 Cari & Nakit Modülü", 42, 115);
        _chkCari = CreateCheck("Cari Hesap Kartları", 16, 22, grpCari);
        _chkKasa = CreateCheck("Kasa & Nakit Yönetimi", 230, 22, grpCari);
        _chkEkstre = CreateCheck("Cari Hesap Ekstresi", 460, 22, grpCari);
        _chkBorcAlacak = CreateCheck("Borç / Alacak İşlemleri", 16, 52, grpCari);
        _chkPerakende = CreateCheck("Perakende Satış", 230, 52, grpCari);
        _chkFis = CreateCheck("Kasa & Banka Fişleri", 460, 52, grpCari);

        // Group 2: Faturalar
        var grpFatura = CreateGroupBox("📄 Fatura Modülü", 165, 80);
        _chkSatis = CreateCheck("Satış Faturaları & İadeler", 16, 25, grpFatura);
        _chkAlis = CreateCheck("Alış Faturaları & İadeler", 230, 25, grpFatura);

        // Group 3: Çek & Senet & Tahsilat
        var grpCek = CreateGroupBox("💰 Tahsilat & Çek / Senet", 253, 115);
        _chkTahsilat = CreateCheck("Tahsilat Girişi", 16, 22, grpCek);
        _chkOdeme = CreateCheck("Ödeme Girişi", 230, 22, grpCek);
        _chkMustCek = CreateCheck("Müşteri Çek Girişi", 460, 22, grpCek);
        _chkMustSenet = CreateCheck("Müşteri Senet Girişi", 16, 52, grpCek);
        _chkFirmaCek = CreateCheck("Firma Çek Çıkışı", 230, 52, grpCek);
        _chkDekont = CreateCheck("Borç / Alacak Dekontu", 460, 52, grpCek);

        // Group 4: Stok & Depo
        var grpStok = CreateGroupBox("📦 Stok & Envanter", 376, 85);
        _chkStok = CreateCheck("Stok Kartları & Hareketleri", 16, 25, grpStok);
        _chkDepoStok = CreateCheck("Depo Bazında Stok", 230, 25, grpStok);
        _chkFiyatGor = CreateCheck("Fiyat / Bakiye Görüntüleme", 460, 25, grpStok);

        // Group 5: Raporlar & Analiz
        var grpRapor = CreateGroupBox("📊 Raporlar & Analizler", 469, 115);
        _chkGunSonu = CreateCheck("Gün Sonu Raporu", 16, 22, grpRapor);
        _chkIsletme = CreateCheck("İşletme Durum Raporu", 230, 22, grpRapor);
        _chkKarlilik = CreateCheck("Satış Özeti & Karlılık", 460, 22, grpRapor);
        _chkCekRapor = CreateCheck("Çek / Senet Raporları", 16, 52, grpRapor);
        _chkAylikAnaliz = CreateCheck("Aylık Finans Analizleri", 230, 52, grpRapor);

        pnlScroll.Controls.AddRange(new Control[] { pnlQuick, grpCari, grpFatura, grpCek, grpStok, grpRapor });
        page.Controls.Add(pnlScroll);
    }

    private GroupBox CreateGroupBox(string title, int y, int height)
    {
        return new GroupBox
        {
            Text = title,
            Location = new Point(10, y),
            Size = new Size(675, height),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800
        };
    }

    private CheckBox CreateCheck(string text, int x, int y, GroupBox parent)
    {
        var chk = new CheckBox
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate700,
            Checked = true
        };
        parent.Controls.Add(chk);
        return chk;
    }

    private void SelectAllPermissions(bool check)
    {
        var checks = new[]
        {
            _chkCari, _chkKasa, _chkEkstre, _chkBorcAlacak, _chkPerakende, _chkFis,
            _chkSatis, _chkAlis,
            _chkTahsilat, _chkOdeme, _chkMustCek, _chkMustSenet, _chkFirmaCek, _chkDekont,
            _chkStok, _chkDepoStok, _chkFiyatGor,
            _chkGunSonu, _chkIsletme, _chkKarlilik, _chkCekRapor, _chkAylikAnaliz
        };
        foreach (var c in checks)
        {
            if (c != null) c.Checked = check;
        }
    }

    private async void LoadUserData()
    {
        if (_existing == null)
        {
            // Auto generate next user code
            _txtKodu.Text = await _macroService.GetNextErpUserCodeAsync();
            _chkDurum.Checked = true;
            _cmbYetki.SelectedIndex = 1; // Standart
            SelectAllCompanies(true);
            SelectAllPermissions(true);
        }
        else
        {
            _txtKodu.Text = _existing.Kodu;
            _txtAdi.Text = _existing.Adi;
            _txtSifre.Text = _existing.Sifre;
            _txtOzelKodu.Text = _existing.OzelKodu;
            _chkDurum.Checked = _existing.IsActive;

            if (_existing.IsAdmin) _cmbYetki.SelectedIndex = 0;
            else if (_existing.Yetki == "N") _cmbYetki.SelectedIndex = 2;
            else _cmbYetki.SelectedIndex = 1;

            // Companies
            for (int i = 0; i < _clbCompanies.Items.Count; i++)
            {
                if (_clbCompanies.Items[i] is CompanyDto c)
                {
                    bool isChecked = _existing.SirketKodlari.Contains(c.SirKod.ToString());
                    _clbCompanies.SetItemChecked(i, isChecked);
                }
            }

            // Permissions
            _chkCari.Checked = _existing.Cari;
            _chkKasa.Checked = _existing.Kasa;
            _chkEkstre.Checked = _existing.Ekstre;
            _chkBorcAlacak.Checked = _existing.BorcAlacak;
            _chkPerakende.Checked = _existing.Perakende;
            _chkFis.Checked = _existing.Fis;

            _chkSatis.Checked = _existing.Satis;
            _chkAlis.Checked = _existing.Alis;

            _chkTahsilat.Checked = _existing.TahsilatGirisi;
            _chkOdeme.Checked = _existing.OdemeGirisi;
            _chkMustCek.Checked = _existing.MusteriCekGirisi;
            _chkMustSenet.Checked = _existing.MusteriSenetGirisi;
            _chkFirmaCek.Checked = _existing.FirmaCekCikisi;
            _chkDekont.Checked = _existing.BorcDekontuGirisi;

            _chkStok.Checked = _existing.Stok;
            _chkDepoStok.Checked = _existing.DepoBazindaStokRaporu;
            _chkFiyatGor.Checked = _existing.FiyatGorGiris;

            _chkGunSonu.Checked = _existing.GunSonuRaporu;
            _chkIsletme.Checked = _existing.IsletmeDurumRaporu;
            _chkKarlilik.Checked = _existing.SatisKarlilik;
            _chkCekRapor.Checked = _existing.MusteriCekRaporu;
            _chkAylikAnaliz.Checked = _existing.AylikAnaliz;
        }
    }

    private async Task SaveUserAsync()
    {
        string kodu = _txtKodu.Text.Trim();
        string adi = _txtAdi.Text.Trim();
        string sifre = _txtSifre.Text.Trim();

        if (string.IsNullOrWhiteSpace(kodu))
        {
            MessageBox.Show("Lütfen kullanıcı kodu giriniz.", "Zorunlu Alan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(adi))
        {
            MessageBox.Show("Lütfen kullanıcı adı soyadı giriniz.", "Zorunlu Alan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(sifre))
        {
            MessageBox.Show("Lütfen kullanıcı şifresi giriniz.", "Zorunlu Alan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string yetki = _cmbYetki.SelectedIndex switch
        {
            0 => "A",
            2 => "N",
            _ => "S"
        };

        var selectedCompanyCodes = new List<string>();
        foreach (var item in _clbCompanies.CheckedItems)
        {
            if (item is CompanyDto c)
            {
                selectedCompanyCodes.Add(c.SirKod.ToString());
            }
        }

        if (selectedCompanyCodes.Count == 0)
        {
            var res = MessageBox.Show(
                "Bu kullanıcı için hiçbir şirket seçilmedi. Kullanıcı oturum açamayabilir.\n\nYine de kaydetmek istiyor musunuz?",
                "Şirket Seçilmedi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;
        }

        var dto = new ErpUserDto
        {
            Kodu = kodu,
            Adi = adi,
            Sifre = sifre,
            Yetki = yetki,
            Durum = _chkDurum.Checked ? "EVET" : "HAYIR",
            OzelKodu = _txtOzelKodu.Text.Trim(),
            Cari = _chkCari.Checked,
            Kasa = _chkKasa.Checked,
            Ekstre = _chkEkstre.Checked,
            BorcAlacak = _chkBorcAlacak.Checked,
            Perakende = _chkPerakende.Checked,
            PerakendeIade = _chkPerakende.Checked,
            Fis = _chkFis.Checked,
            Satis = _chkSatis.Checked,
            SatisIade = _chkSatis.Checked,
            Alis = _chkAlis.Checked,
            AlisIade = _chkAlis.Checked,
            TahsilatGirisi = _chkTahsilat.Checked,
            OdemeGirisi = _chkOdeme.Checked,
            MusteriCekGirisi = _chkMustCek.Checked,
            MusteriSenetGirisi = _chkMustSenet.Checked,
            FirmaCekCikisi = _chkFirmaCek.Checked,
            BorcDekontuGirisi = _chkDekont.Checked,
            AlacakDekontuGirisi = _chkDekont.Checked,
            Stok = _chkStok.Checked,
            DepoBazindaStokRaporu = _chkDepoStok.Checked,
            FiyatGorGiris = _chkFiyatGor.Checked,
            BakiyeGorGirisi = _chkFiyatGor.Checked,
            BakiyeGoster = _chkFiyatGor.Checked,
            GunSonuRaporu = _chkGunSonu.Checked,
            IsletmeDurumRaporu = _chkIsletme.Checked,
            SatisKarlilik = _chkKarlilik.Checked,
            SatisOzeti = _chkKarlilik.Checked,
            MusteriCekRaporu = _chkCekRapor.Checked,
            FirmaCekRaporu = _chkCekRapor.Checked,
            SenetRaporu = _chkCekRapor.Checked,
            AylikAnaliz = _chkAylikAnaliz.Checked,
            StokGirisCikisRaporu = _chkDepoStok.Checked
        };

        var (success, msg) = await _macroService.SaveErpUserAsync(dto, selectedCompanyCodes, _existing == null);
        if (success)
        {
            NotificationService.NotifySuccess(
                "Kullanıcı Kaydedildi",
                $"'{dto.Adi}' ({dto.Kodu}) kullanıcısı ve şirket yetkileri güncellendi.",
                "UserMgmt");
            MessageBox.Show("Kullanıcı ve yetkileri başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            MessageBox.Show($"Kullanıcı kaydedilemedi:\n{msg}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

/// <summary>
/// Modal dialog for quickly changing a user's password
/// </summary>
public class QuickPasswordDialog : Form
{
    private TextBox _txtNew = null!, _txtConfirm = null!;
    private CheckBox _chkShow = null!;
    public string NewPassword => _txtNew.Text.Trim();

    public QuickPasswordDialog(string userKodu, string userAdi)
    {
        this.DoubleBuffered = true;
        this.Text = "🔑 Şifre Değiştir";
        this.Size = new Size(420, 290);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.White;

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(238, 242, 255),
            Padding = new Padding(16, 12, 16, 12)
        };
        var lblTitle = new Label
        {
            Text = $"👤 {userAdi} ({userKodu})",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(67, 56, 202),
            AutoSize = true,
            Location = new Point(14, 14)
        };
        pnlHeader.Controls.Add(lblTitle);

        // Body
        var lblNew = new Label { Text = "Yeni Şifre:", Location = new Point(24, 72), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _txtNew = new TextBox { Location = new Point(145, 69), Width = 230, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };

        var lblConfirm = new Label { Text = "Şifre (Tekrar):", Location = new Point(24, 112), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _txtConfirm = new TextBox { Location = new Point(145, 109), Width = 230, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };

        _chkShow = new CheckBox { Text = "Şifreyi Göster", Location = new Point(145, 145), AutoSize = true };
        _chkShow.CheckedChanged += (s, e) =>
        {
            _txtNew.UseSystemPasswordChar = !_chkShow.Checked;
            _txtConfirm.UseSystemPasswordChar = !_chkShow.Checked;
        };

        // Buttons
        var btnOk = new Button
        {
            Text = "🔑 Şifreyi Kaydet",
            Location = new Point(145, 190),
            Width = 130,
            Height = 34,
            BackColor = Color.FromArgb(67, 56, 202),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_txtNew.Text))
            {
                MessageBox.Show("Lütfen bir şifre giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_txtNew.Text != _txtConfirm.Text)
            {
                MessageBox.Show("Girilen şifreler birbiriyle uyuşmuyor!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        };

        var btnCancel = new Button
        {
            Text = "İptal",
            Location = new Point(285, 190),
            Width = 90,
            Height = 34,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        this.Controls.AddRange(new Control[] { pnlHeader, lblNew, _txtNew, lblConfirm, _txtConfirm, _chkShow, btnOk, btnCancel });
    }
}
