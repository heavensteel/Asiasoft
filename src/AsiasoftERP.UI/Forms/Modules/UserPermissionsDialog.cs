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
/// Modal dialog to configure and assign granular page/module visibility permissions
/// for each user. Modules unchecked here will be completely hidden from the user's sidebar.
/// </summary>
public class UserPermissionsDialog : Form
{
    private readonly PermissionService _service = new();
    private ComboBox _cboUsers = null!;
    private Label _lblUserRoleBadge = null!;
    private Panel _pnlModulesContainer = null!;
    private readonly Dictionary<string, CheckBox> _checkMap = new(StringComparer.OrdinalIgnoreCase);

    private List<CompanyUserDto> _users = new();
    private string _selectedUserCode = string.Empty;
    private string _selectedUserName = string.Empty;
    private readonly string? _initialUserCode;
    private readonly Action? _onPermissionsChanged;

    public UserPermissionsDialog(string? initialUserCode = null, Action? onPermissionsChanged = null)
    {
        _initialUserCode = initialUserCode;
        _onPermissionsChanged = onPermissionsChanged;
        this.DoubleBuffered = true;

        InitializeComponents();
        this.Load += async (_, _) => await LoadUsersAsync();
    }

    public UserPermissionsDialog(Action? onPermissionsChanged) : this(null, onPermissionsChanged) { }

    private void InitializeComponents()
    {
        this.Text = "Modül & Menü Yetki Yöneticisi — Asiasoft ERP";
        this.Size = new Size(920, 720);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. TOP HEADER BANNER
        var pnlHdr = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlHdr.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlHdr.Height - 1, pnlHdr.Width, pnlHdr.Height - 1);
        };

        var lblIcon = new Label
        {
            Text = "🔐",
            Font = new Font("Segoe UI Emoji", 20f),
            Location = new Point(16, 14),
            Size = new Size(42, 42)
        };

        var lblHdrTitle = new Label
        {
            Text = "Kullanıcı Sayfa & Modül Yetki Yöneticisi",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(64, 12),
            AutoSize = true
        };

        var lblHdrSub = new Label
        {
            Text = "Hangi personelin hangi sayfaları görebileceğini belirleyin. İzni olmayan sayfalar sol menüde asla gözükmez.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(66, 38),
            AutoSize = true
        };
        pnlHdr.Controls.AddRange(new Control[] { lblIcon, lblHdrTitle, lblHdrSub });

        // 2. USER SELECTION & PRESETS TOOLBAR
        var pnlUserSelect = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlUserSelect.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlUserSelect.Height - 1, pnlUserSelect.Width, pnlUserSelect.Height - 1);
        };

        var lblUser = new Label
        {
            Text = "Yetkilendirilecek Kullanıcı:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(18, 12),
            AutoSize = true
        };

        _cboUsers = new ComboBox
        {
            Location = new Point(18, 34),
            Size = new Size(260, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cboUsers.SelectedIndexChanged += async (_, _) => await OnUserSelectedAsync();

        _lblUserRoleBadge = new Label
        {
            Text = "Standart",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            Location = new Point(286, 36),
            Size = new Size(90, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Presets Label & Buttons
        var lblPresets = new Label
        {
            Text = "Hızlı Rol Şablonları:",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(390, 12),
            AutoSize = true
        };

        var pnlPresets = new FlowLayoutPanel
        {
            Location = new Point(390, 32),
            Size = new Size(500, 34),
            WrapContents = false,
            AutoScroll = false
        };

        AddPresetButton(pnlPresets, "⚖️ Kantar", new[] { "MalKabul", "Kantar" });
        AddPresetButton(pnlPresets, "📋 Amele & Çavuş", new[] { "Puantaj", "CavusServis" });
        AddPresetButton(pnlPresets, "🚚 Sevkiyat", new[] { "YuklemeProgrami", "SatisIrsaliye", "Kantar" });
        AddPresetButton(pnlPresets, "🌳 Bahçe & Kesim", new[] { "BahceKesim", "CavusServis", "MalKabul", "Kantar" });
        AddPresetButton(pnlPresets, "💰 Finans", new[] { "KasaBanka", "KasaEkstre", "BankaHesap", "OdemeTalep", "CekList" });
        AddPresetButton(pnlPresets, "✨ Tümü", null, isAll: true);
        AddPresetButton(pnlPresets, "❌ Temizle", null, isNone: true);

        pnlUserSelect.Controls.AddRange(new Control[] { lblUser, _cboUsers, _lblUserRoleBadge, lblPresets, pnlPresets });

        // 3. BOTTOM ACTION BAR
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
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
            Text = "Kapat",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(85, 36),
            Location = new Point(pnlBottom.Width - 105, 12),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        var btnSave = new Button
        {
            Text = "💾  Yetkileri Kaydet ve Uygula",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(16, 120, 70), // Emerald
            FlatStyle = FlatStyle.Flat,
            Size = new Size(230, 36),
            Location = new Point(pnlBottom.Width - 345, 12),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (_, _) => await SavePermissionsAsync();

        pnlBottom.Controls.AddRange(new Control[] { btnSave, btnCancel });

        // 4. MODULES CONTAINER (Grouped by Category)
        _pnlModulesContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = ModernColors.Slate100
        };

        this.Controls.Add(_pnlModulesContainer);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlUserSelect);
        this.Controls.Add(pnlHdr);
    }

    private void AddPresetButton(FlowLayoutPanel pnl, string title, string[]? modules, bool isAll = false, bool isNone = false)
    {
        var btn = new Button
        {
            Text = title,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = isNone ? Color.FromArgb(220, 38, 38) : (isAll ? Color.FromArgb(16, 120, 70) : ModernColors.Slate700),
            BackColor = Color.FromArgb(241, 245, 249),
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Height = 26,
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 2, 4, 2)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

        btn.Click += (_, _) =>
        {
            if (isAll)
            {
                foreach (var chk in _checkMap.Values) chk.Checked = true;
            }
            else if (isNone)
            {
                foreach (var chk in _checkMap.Values) chk.Checked = false;
            }
            else if (modules != null)
            {
                var modSet = new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in _checkMap)
                {
                    kvp.Value.Checked = modSet.Contains(kvp.Key);
                }
            }
        };

        pnl.Controls.Add(btn);
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            _users = await _service.GetAllUsersAsync();
            _cboUsers.Items.Clear();

            int selIndex = 0;
            string targetCode = !string.IsNullOrEmpty(_initialUserCode) ? _initialUserCode : AppState.CurrentUserCode;

            for (int i = 0; i < _users.Count; i++)
            {
                var u = _users[i];
                _cboUsers.Items.Add(u);
                if (u.KullaniciKodu.ToString("0") == targetCode || u.KullaniciKodu.ToString() == targetCode)
                {
                    selIndex = i;
                }
            }

            if (_cboUsers.Items.Count > 0)
            {
                _cboUsers.SelectedIndex = selIndex;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kullanıcılar yüklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnUserSelectedAsync()
    {
        if (_cboUsers.SelectedItem is not CompanyUserDto user) return;

        _selectedUserCode = user.KullaniciKodu.ToString("0");
        _selectedUserName = user.KullaniciAdi;

        _lblUserRoleBadge.Text = user.IsAdmin ? "👑 Yönetici" : "👤 Standart";
        _lblUserRoleBadge.BackColor = user.IsAdmin ? Color.FromArgb(254, 243, 199) : ModernColors.Slate200;
        _lblUserRoleBadge.ForeColor = user.IsAdmin ? Color.FromArgb(180, 83, 9) : ModernColors.Slate700;

        await RenderModuleCheckboxesAsync(user.IsAdmin);
    }

    private async Task RenderModuleCheckboxesAsync(bool isSuperAdmin)
    {
        _pnlModulesContainer.Controls.Clear();
        _checkMap.Clear();

        var permissions = await _service.GetUserPermissionsAsync(_selectedUserCode, _selectedUserName, isSuperAdmin);
        var groups = permissions.GroupBy(p => p.Category).ToList();

        int y = 10;
        int groupW = _pnlModulesContainer.ClientSize.Width >= 850 ? _pnlModulesContainer.ClientSize.Width - 36 : 850;

        foreach (var grp in groups)
        {
            int chkCount = grp.Count();
            int rows = (chkCount + 1) / 2;
            int grpH = 34 + (rows * 34) + 8;

            var pnlGroup = new Panel
            {
                Location = new Point(14, y),
                Size = new Size(groupW, grpH),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlGroup.Paint += (s, e) =>
            {
                using var pen = new Pen(ModernColors.Slate200, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlGroup.Width - 1, pnlGroup.Height - 1);
                using var bg = new SolidBrush(Color.FromArgb(244, 247, 251));
                e.Graphics.FillRectangle(bg, 0, 0, pnlGroup.Width - 1, 24);
                using var brushT = new SolidBrush(Color.FromArgb(30, 41, 59));
                using var fontT = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                e.Graphics.DrawString(grp.Key, fontT, brushT, 10, 4);
            };

            int colW = (groupW - 40) / 2;
            int idx = 0;
            foreach (var mod in grp)
            {
                int r = idx / 2;
                int c = idx % 2;
                int cx = 14 + (c * (colW + 12));
                int cy = 30 + (r * 34);

                var chk = new CheckBox
                {
                    Text = mod.ModuleTitle,
                    UseMnemonic = false,
                    Font = new Font("Segoe UI", 9f, mod.HasPermission ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = mod.HasPermission ? ModernColors.Slate900 : ModernColors.Slate600,
                    Checked = mod.HasPermission,
                    Location = new Point(cx, cy),
                    Size = new Size(colW, 28),
                    Cursor = Cursors.Hand
                };

                chk.CheckedChanged += (s, _) =>
                {
                    chk.Font = new Font("Segoe UI", 9f, chk.Checked ? FontStyle.Bold : FontStyle.Regular);
                    chk.ForeColor = chk.Checked ? ModernColors.Slate900 : ModernColors.Slate600;
                };

                _checkMap[mod.ModuleKey] = chk;
                pnlGroup.Controls.Add(chk);
                idx++;
            }

            _pnlModulesContainer.Controls.Add(pnlGroup);
            y += grpH + 12;
        }
    }

    private async Task SavePermissionsAsync()
    {
        if (string.IsNullOrEmpty(_selectedUserCode)) return;

        try
        {
            var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _checkMap)
            {
                dict[kvp.Key] = kvp.Value.Checked;
            }

            bool ok = await _service.SaveUserPermissionsAsync(_selectedUserCode, _selectedUserName, dict);

            if (ok)
            {
                // If editing current logged in user, refresh AppState immediately
                if (_selectedUserCode == AppState.CurrentUserCode || _selectedUserName.Equals(AppState.CurrentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    await AppState.LoadPermissionsAsync();
                }

                NotificationService.NotifySuccess(
                    "Yetkiler Kaydedildi",
                    $"'{_selectedUserName}' kullanıcısının modül izinleri güncellendi.",
                    "Yetki",
                    showToast: true);

                _onPermissionsChanged?.Invoke();
                this.DialogResult = DialogResult.OK;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yetkiler kaydedilirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
