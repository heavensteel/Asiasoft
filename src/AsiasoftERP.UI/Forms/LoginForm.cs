using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms;

public class LoginForm : Form
{
    private readonly MacroService _macroService = new();

    // PInvoke for borderless window dragging
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    // Controls
    private Panel _pnlLeft = null!;
    private Panel _pnlRight = null!;

    private PictureBox _picBrand = null!;
    private Label _lblBrandTitle = null!;
    private Label _lblBrandSub = null!;

    private Label _lblWelcome = null!;
    private Label _lblInstruction = null!;
    private Label _lblFirma = null!;
    private ComboBox _cmbFirma = null!;
    private Label _lblKullanici = null!;
    private ComboBox _cmbKullanici = null!;
    private Label _lblSifre = null!;
    private TextBox _txtSifre = null!;
    private Button _btnGiris = null!;
    private Label _lblStatusPill = null!;

    private Button _btnClose = null!;
    private Button _btnMinimize = null!;

    private List<CompanyDto> _companies = new();
    private List<CompanyUserDto> _users = new();

    public LoginForm()
    {
        InitializeComponents();
        this.Load += async (_, _) => await LoadDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "TEK ASYA MacERP - Giriş";
        this.Size = new Size(880, 540);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.KeyPreview = true;

        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) Application.Exit();
            else if (e.KeyCode == Keys.Enter) PerformLogin();
        };

        // --- LEFT BRAND PANEL (360px) ---
        _pnlLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 360,
            BackColor = ModernColors.Slate900
        };
        _pnlLeft.Paint += DrawLeftPanelGradient;
        _pnlLeft.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };

        _picBrand = new PictureBox
        {
            Size = new Size(90, 90),
            Location = new Point(135, 50),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        LoadBrandImage();

        _lblBrandTitle = new Label
        {
            Text = "TEK ASYA",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 155),
            Size = new Size(320, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        _lblBrandSub = new Label
        {
            Text = "MacERP Kurumsal Yönetim Sistemi\n(.NET 9 Yüksek Hızlı Mimari)",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(20, 195),
            Size = new Size(320, 45),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        // Feature Highlights
        int featY = 265;
        AddFeatureBadge(_pnlLeft, "⚡  3.32 Milyon Canlı Kayıt", featY);
        featY += 40;
        AddFeatureBadge(_pnlLeft, "📊  Yüksek Hızlı Cari Ekstre (<10ms)", featY);
        featY += 40;
        AddFeatureBadge(_pnlLeft, "⚖️  Kantar & Paketleme Entegrasyonu", featY);
        featY += 40;
        AddFeatureBadge(_pnlLeft, "🔒  MariaDB Güvenli Replikasyon", featY);

        var lblVersion = new Label
        {
            Text = "v7.2 Reborn  •  © 2026 Tek Asya A.Ş.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Bottom,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        _pnlLeft.Controls.AddRange(new Control[] { _picBrand, _lblBrandTitle, _lblBrandSub, lblVersion });

        // --- RIGHT FORM PANEL (520px) ---
        _pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(50, 25, 50, 25)
        };
        _pnlRight.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };

        // Window controls (Top right)
        _btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = ModernColors.Slate500,
            Location = new Point(475, 12),
            Size = new Size(32, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        _btnClose.FlatAppearance.BorderSize = 0;
        _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Color.FromArgb(239, 68, 68); _btnClose.ForeColor = Color.White; };
        _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.Transparent; _btnClose.ForeColor = ModernColors.Slate500; };
        _btnClose.Click += (s, e) => Application.Exit();

        _btnMinimize = new Button
        {
            Text = "—",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = ModernColors.Slate500,
            Location = new Point(440, 12),
            Size = new Size(32, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        _btnMinimize.FlatAppearance.BorderSize = 0;
        _btnMinimize.MouseEnter += (s, e) => { _btnMinimize.BackColor = ModernColors.Slate100; };
        _btnMinimize.MouseLeave += (s, e) => { _btnMinimize.BackColor = Color.Transparent; };
        _btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

        int rightY = 40;

        _lblWelcome = new Label
        {
            Text = "Sisteme Giriş Yapın",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(50, rightY),
            AutoSize = true
        };
        rightY += 34;

        _lblInstruction = new Label
        {
            Text = "Çalışmak istediğiniz şirket ve kullanıcı hesabını seçiniz",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(52, rightY),
            AutoSize = true
        };
        rightY += 35;

        // 1. Şirket (Company)
        _lblFirma = new Label
        {
            Text = "🏢  Çalışılacak Şirket / Şube",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(50, rightY),
            AutoSize = true
        };
        rightY += 24;

        _cmbFirma = new ComboBox
        {
            Location = new Point(50, rightY),
            Width = 420,
            Height = 36,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = ModernColors.Slate50,
            FlatStyle = FlatStyle.Flat
        };
        _cmbFirma.SelectedIndexChanged += async (_, _) => await OnFirmaChangedAsync();
        rightY += 46;

        // 2. Kullanıcı (User)
        _lblKullanici = new Label
        {
            Text = "👤  Kullanıcı Hesabı",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(50, rightY),
            AutoSize = true
        };
        rightY += 24;

        _cmbKullanici = new ComboBox
        {
            Location = new Point(50, rightY),
            Width = 420,
            Height = 36,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = ModernColors.Slate50,
            FlatStyle = FlatStyle.Flat
        };
        _cmbKullanici.SelectedIndexChanged += (_, _) =>
        {
            _txtSifre.Clear();
            _lblStatusPill.ForeColor = ModernColors.Teal600;
            _lblStatusPill.BackColor = Color.FromArgb(240, 253, 250);
            _lblStatusPill.Text = "🔒 Güvenli Bağlantı: Yerel MariaDB 10.11 (3,321,448 Kayıt Aktif)";
        };
        rightY += 46;

        // 3. Şifre (Password)
        _lblSifre = new Label
        {
            Text = "🔑  Parola / Şifre",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(50, rightY),
            AutoSize = true
        };
        rightY += 24;

        _txtSifre = new TextBox
        {
            Location = new Point(50, rightY),
            Width = 420,
            Height = 36,
            Font = new Font("Segoe UI", 11f),
            UseSystemPasswordChar = true,
            PlaceholderText = "Parolanızı giriniz (Enter ile doğrudan giriş)",
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ModernColors.Slate50
        };
        _txtSifre.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PerformLogin();
            }
        };
        rightY += 52;

        // Login Button
        _btnGiris = new Button
        {
            Text = "Giriş Yap  ➔",
            Location = new Point(50, rightY),
            Width = 420,
            Height = 44,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            BackColor = ModernColors.Teal600,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnGiris.FlatAppearance.BorderSize = 0;
        _btnGiris.MouseEnter += (s, e) => _btnGiris.BackColor = Color.FromArgb(15, 118, 110);
        _btnGiris.MouseLeave += (s, e) => _btnGiris.BackColor = ModernColors.Teal600;
        _btnGiris.Click += (_, _) => PerformLogin();
        rightY += 55;

        // Status Pill Badge
        _lblStatusPill = new Label
        {
            Text = "🔒 Güvenli Bağlantı: Yerel MariaDB 10.11 (3,321,448 Kayıt Aktif)",
            Location = new Point(50, rightY),
            Width = 420,
            Height = 28,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = ModernColors.Teal600,
            BackColor = Color.FromArgb(240, 253, 250),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _pnlRight.Controls.AddRange(new Control[]
        {
            _btnClose, _btnMinimize,
            _lblWelcome, _lblInstruction,
            _lblFirma, _cmbFirma,
            _lblKullanici, _cmbKullanici,
            _lblSifre, _txtSifre,
            _btnGiris, _lblStatusPill
        });

        this.Controls.Add(_pnlRight);
        this.Controls.Add(_pnlLeft);
    }

    private void LoadBrandImage()
    {
        try
        {
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "tekasya.jpg");
            if (!File.Exists(imgPath))
                imgPath = @"D:\AsiaSoft\src\AsiasoftERP.UI\Resources\Images\tekasya.jpg";

            if (File.Exists(imgPath))
            {
                _picBrand.Image = Image.FromFile(imgPath);
            }
        }
        catch { }
    }

    private void AddFeatureBadge(Panel parent, string text, int y)
    {
        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(35, y),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        parent.Controls.Add(lbl);
    }

    private void DrawLeftPanelGradient(object? sender, PaintEventArgs e)
    {
        var rect = _pnlLeft.ClientRectangle;
        using var brush = new LinearGradientBrush(rect, Color.FromArgb(11, 19, 43), Color.FromArgb(30, 41, 59), 45f);
        e.Graphics.FillRectangle(brush, rect);

        // Subtle accent line on right edge
        using var pen = new Pen(Color.FromArgb(51, 65, 85), 1f);
        e.Graphics.DrawLine(pen, _pnlLeft.Width - 1, 0, _pnlLeft.Width - 1, _pnlLeft.Height);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _lblStatusPill.Text = "⏳ Şirketler ve kullanıcılar yükleniyor...";

            _companies = await _macroService.GetCompaniesAsync();
            _cmbFirma.DataSource = _companies;
            _cmbFirma.DisplayMember = "Adi";
            _cmbFirma.ValueMember = "SirKod";

            var (savedSirKod, _, savedKulKod, _) = _macroService.LoadSettings();

            int foundCompIdx = _companies.FindIndex(c => c.SirKod == savedSirKod);
            if (foundCompIdx >= 0) _cmbFirma.SelectedIndex = foundCompIdx;
            else if (_companies.Count > 0) _cmbFirma.SelectedIndex = 0;

            await LoadUsersForSelectedCompanyAsync(savedKulKod);

            _lblStatusPill.Text = "🔒 Güvenli Bağlantı: Yerel MariaDB 10.11 (3,321,448 Kayıt Aktif)";
            _txtSifre.Focus();
        }
        catch (Exception ex)
        {
            _lblStatusPill.ForeColor = ModernColors.Rose600;
            _lblStatusPill.BackColor = Color.FromArgb(255, 241, 242);
            _lblStatusPill.Text = $"Bağlantı Hatası: {ex.Message}";
        }
    }

    private async Task OnFirmaChangedAsync()
    {
        if (_cmbFirma.SelectedItem is CompanyDto comp)
        {
            await LoadUsersForSelectedCompanyAsync();
        }
    }

    private async Task LoadUsersForSelectedCompanyAsync(double preferredUserKod = 0)
    {
        if (_cmbFirma.SelectedItem is not CompanyDto comp) return;

        _users = await _macroService.GetCompanyUsersAsync(comp.SirKod.ToString());
        _cmbKullanici.DataSource = _users;
        _cmbKullanici.DisplayMember = "KullaniciAdi";
        _cmbKullanici.ValueMember = "KullaniciKodu";

        if (preferredUserKod > 0)
        {
            int uIdx = _users.FindIndex(u => Math.Abs(u.KullaniciKodu - preferredUserKod) < 0.1);
            if (uIdx >= 0) _cmbKullanici.SelectedIndex = uIdx;
        }
        else if (_users.Count > 0)
        {
            _cmbKullanici.SelectedIndex = 0;
        }
    }

    private async void PerformLogin()
    {
        if (_cmbFirma.SelectedItem is not CompanyDto comp) return;
        if (_cmbKullanici.SelectedItem is not CompanyUserDto user) return;

        _lblStatusPill.ForeColor = ModernColors.Teal600;
        _lblStatusPill.BackColor = Color.FromArgb(240, 253, 250);
        _lblStatusPill.Text = "⏳ Kimlik doğrulanıyor...";
        _btnGiris.Enabled = false;

        try
        {
            var (success, msg, isAdmin) = await _macroService.ValidateUserPasswordAsync(user.KullaniciKodu, _txtSifre.Text);
            if (!success)
            {
                _ = _macroService.LogUserLoginAsync(
                    user.KullaniciKodu.ToString(),
                    user.KullaniciAdi,
                    comp.SirKod,
                    comp.Adi,
                    false,
                    "Hatalı şifre girildi."
                );

                _lblStatusPill.ForeColor = ModernColors.Rose600;
                _lblStatusPill.BackColor = Color.FromArgb(255, 241, 242);
                _lblStatusPill.Text = $"❌ {msg}";
                _txtSifre.SelectAll();
                _txtSifre.Focus();
                return;
            }

            user.IsAdmin = isAdmin;
            _macroService.SaveSettings(comp.SirKod, comp.Adi, user.KullaniciKodu, user.KullaniciAdi);

            int logId = await _macroService.LogUserLoginAsync(
                user.KullaniciKodu.ToString(),
                user.KullaniciAdi,
                comp.SirKod,
                comp.Adi,
                true,
                "Oturum başarıyla açıldı."
            );

            AppState.CurrentSessionDbId = logId;
            AppState.ActiveCompany = comp;
            AppState.ActiveUser = user;

            this.Hide();
            var mainForm = new MainForm();
            mainForm.FormClosed += (_, _) =>
            {
                if (AppState.CurrentSessionDbId.HasValue)
                {
                    _ = _macroService.LogUserLogoutAsync(AppState.CurrentSessionDbId.Value);
                }
                Application.Exit();
            };
            mainForm.Show();
        }
        catch (Exception ex)
        {
            _lblStatusPill.ForeColor = ModernColors.Rose600;
            _lblStatusPill.BackColor = Color.FromArgb(255, 241, 242);
            _lblStatusPill.Text = $"Hata: {ex.Message}";
        }
        finally
        {
            _btnGiris.Enabled = true;
        }
    }
}
