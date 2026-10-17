using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Forms.Modules;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms;

public class MainForm : Form
{
    // Primary Structural Panels
    private Panel _pnlTopBar = null!;
    private Panel _pnlBody = null!;
    private Panel _pnlSidebar = null!;
    private Panel _pnlMainContent = null!;
    private TabControl _tabControl = null!;
    private StatusStrip _statusStrip = null!;

    // TopBar Labels & Controls
    private Label _lblBrand = null!;
    private Label _lblVersionBadge = null!;
    private Button _btnCompanyPill = null!;
    private Label _lblUserPill = null!;
    private Label _lblDbPill = null!;
    private Label _lblCurrencyPill = null!;
    private Button _btnNotificationBell = null!;
    private Label _lblClock = null!;

    // Active sidebar button tracking
    private Button? _activeSidebarBtn;
    private readonly List<Button> _sidebarButtons = new();

    // Clock Timer
    private System.Windows.Forms.Timer _clockTimer = null!;

    // Home Dashboard View
    private DashboardControl _dashboardView = null!;

    public MainForm()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        InitializeComponents();
        StartClock();
    }

    private void InitializeComponents()
    {
        string compName = AppState.ActiveCompany?.Adi ?? "TEK ASYA A.Ş.";
        string userName = AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL";

        this.Text = $"TEK ASYA MacERP v7.2 — [{compName}] — [{userName}]";
        this.Size = new Size(1440, 900);
        this.MinimumSize = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. Build Full-Width Top Bar (Top edge)
        BuildTopBar();

        // 2. Build Full-Width Status Bar (Bottom edge)
        BuildStatusBar();

        // 3. Build Body Panel (Strictly between TopBar and StatusBar)
        _pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(0)
        };

        // 4. Build Sidebar (Left side of Body)
        BuildSidebar();

        // 5. Build Tab Container / Main Workspace (Fill side of Body)
        BuildTabContainer();

        // Assemble Body container:
        // MainContent fills the right, Sidebar takes the left
        _pnlBody.Controls.Add(_pnlMainContent);
        _pnlBody.Controls.Add(_pnlSidebar);

        // Assemble Form: Body fills the middle, TopBar at top, Status at bottom
        this.Controls.Add(_pnlBody);
        this.Controls.Add(_statusStrip);
        this.Controls.Add(_pnlTopBar);

        // Wire Real-Time Notifications
        NotificationService.NotificationsChanged += () =>
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(UpdateNotificationBellBadge));
            else
                UpdateNotificationBellBadge();
        };

        NotificationService.ModuleRequested += moduleKey =>
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => { if (!string.IsNullOrEmpty(moduleKey)) OnDashboardModuleRequested(moduleKey); }));
            else
                if (!string.IsNullOrEmpty(moduleKey)) OnDashboardModuleRequested(moduleKey);
        };
    }

    private void BuildTopBar()
    {
        _pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = ModernColors.Slate900,
            Padding = new Padding(16, 0, 16, 0)
        };

        // Subtle accent line on bottom edge
        _pnlTopBar.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(30, 41, 59), 1f);
            e.Graphics.DrawLine(pen, 0, _pnlTopBar.Height - 1, _pnlTopBar.Width, _pnlTopBar.Height - 1);
        };

        // 1. BRAND & COMPANY (Left - 370px)
        var pnlBrand = new Panel
        {
            Dock = DockStyle.Left,
            Width = 370,
            BackColor = Color.Transparent
        };

        var picLogo = new PictureBox
        {
            Size = new Size(34, 34),
            Location = new Point(0, 11),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        LoadLogo(picLogo);

        _lblBrand = new Label
        {
            Text = "TEK ASYA",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(42, 14),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var lblSub = new Label
        {
            Text = "MacERP",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 212, 191), // Teal 400
            Location = new Point(132, 14),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        _lblVersionBadge = new Label
        {
            Text = "v7.2",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = ModernColors.Slate800,
            Location = new Point(204, 18),
            Size = new Size(38, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _btnCompanyPill = new Button
        {
            Text = $"🏢  {AppState.ActiveCompany?.Adi ?? "TEK ASYA A.Ş."}",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(112, 30),
            Location = new Point(248, 13),
            Cursor = Cursors.Hand
        };
        _btnCompanyPill.FlatAppearance.BorderSize = 0;
        _btnCompanyPill.Click += (_, _) => SwitchCompany();

        pnlBrand.Controls.AddRange(new Control[] { picLogo, _lblBrand, lblSub, _lblVersionBadge, _btnCompanyPill });

        // 2. SEARCH BOX (Center - Fill)
        var pnlSearch = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(20, 11, 20, 11)
        };

        var pnlSearchBox = new Panel
        {
            Width = 430,
            Height = 34,
            BackColor = ModernColors.Slate800,
            Location = new Point(20, 11),
            Cursor = Cursors.Hand
        };
        pnlSearchBox.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(51, 65, 85), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearchBox.Width - 1, pnlSearchBox.Height - 1);
        };

        var lblSearchPlaceholder = new Label
        {
            Text = "🔍  Cari rehberi, fatura veya tartı fişi ara... (F10)",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        lblSearchPlaceholder.Click += (_, _) => OpenModuleTab("CariKartlar", "👥 Cari Hesaplar", () => new CariHesaplarForm());
        pnlSearchBox.Click += (_, _) => OpenModuleTab("CariKartlar", "👥 Cari Hesaplar", () => new CariHesaplarForm());
        pnlSearchBox.Controls.Add(lblSearchPlaceholder);
        pnlSearch.Controls.Add(pnlSearchBox);

        // 3. RIGHT STATUS & USER BADGES (Right - 660px)
        var pnlRight = new Panel
        {
            Dock = DockStyle.Right,
            Width = 660,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 11, 0, 11)
        };

        int rx = 0;

        // Currency Pill
        _lblCurrencyPill = new Label
        {
            Text = "💵 34.25  💶 37.10",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Amber500,
            BackColor = ModernColors.Slate800,
            Size = new Size(130, 32),
            Location = new Point(rx, 12),
            TextAlign = ContentAlignment.MiddleCenter
        };
        rx += 138;

        // DB Pill
        _lblDbPill = new Label
        {
            Text = "🟢 3.32M Canlı",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Emerald500,
            BackColor = ModernColors.Slate800,
            Size = new Size(110, 32),
            Location = new Point(rx, 12),
            TextAlign = ContentAlignment.MiddleCenter
        };
        rx += 118;

        // User Avatar Pill
        bool isAdmin = AppState.IsAdmin;
        _lblUserPill = new Label
        {
            Text = $"{(isAdmin ? "👑 " : "👤 ")}{AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL"}",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = isAdmin ? ModernColors.Amber500 : Color.FromArgb(226, 232, 240),
            BackColor = isAdmin ? Color.FromArgb(30, 41, 59) : Color.FromArgb(39, 53, 76),
            Size = new Size(165, 32),
            Location = new Point(rx, 12),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        rx += 172;

        // Notification Bell Button
        _btnNotificationBell = new Button
        {
            Text = "🔔",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Slate800,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(54, 32),
            Location = new Point(rx, 12),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _btnNotificationBell.FlatAppearance.BorderSize = 0;
        _btnNotificationBell.Click += (_, _) => ShowNotificationDropdown();
        UpdateNotificationBellBadge();
        rx += 60;

        // Clock Pill
        _lblClock = new Label
        {
            Text = DateTime.Now.ToString("HH:mm:ss"),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            Size = new Size(72, 32),
            Location = new Point(rx, 12),
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlRight.Controls.AddRange(new Control[] { _lblCurrencyPill, _lblDbPill, _lblUserPill, _btnNotificationBell, _lblClock });
        _pnlTopBar.Controls.AddRange(new Control[] { pnlBrand, pnlSearch, pnlRight });
    }

    private void UpdateNotificationBellBadge()
    {
        if (_btnNotificationBell == null || _btnNotificationBell.IsDisposed) return;
        int unread = NotificationService.GetUnreadCount();
        if (unread > 0)
        {
            _btnNotificationBell.Text = $"🔔 {unread}";
            _btnNotificationBell.ForeColor = ModernColors.Amber500;
            _btnNotificationBell.BackColor = Color.FromArgb(45, 55, 72);
        }
        else
        {
            _btnNotificationBell.Text = "🔔";
            _btnNotificationBell.ForeColor = Color.FromArgb(148, 163, 184);
            _btnNotificationBell.BackColor = ModernColors.Slate800;
        }
    }

    private void ShowNotificationDropdown()
    {
        var screenPoint = _btnNotificationBell.PointToScreen(new Point(0, _btnNotificationBell.Height + 4));
        var dropdown = new NotificationDropdown();
        int left = screenPoint.X - dropdown.Width + _btnNotificationBell.Width;
        if (left < 10) left = 10;
        dropdown.Location = new Point(left, screenPoint.Y);
        dropdown.ModuleRequested += moduleKey =>
        {
            if (!string.IsNullOrEmpty(moduleKey))
            {
                OnDashboardModuleRequested(moduleKey);
            }
        };
        dropdown.Show();
    }

    private void LoadLogo(PictureBox pic)
    {
        try
        {
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "tekasya.jpg");
            if (!File.Exists(imgPath))
                imgPath = @"D:\AsiaSoft\src\AsiasoftERP.UI\Resources\Images\tekasya.jpg";
            if (File.Exists(imgPath))
                pic.Image = Image.FromFile(imgPath);
        }
        catch { }
    }

    private void BuildSidebar()
    {
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 235,
            BackColor = ModernColors.Slate900,
            AutoScroll = true
        };

        // Right vertical divider line
        _pnlSidebar.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(30, 41, 59), 1f);
            e.Graphics.DrawLine(pen, _pnlSidebar.Width - 1, 0, _pnlSidebar.Width - 1, _pnlSidebar.Height);
        };

        var pnlNavList = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8, 10, 8, 20)
        };

        int sy = 0;

        // 1. GENEL
        AddSidebarHeader(pnlNavList, "GENEL", ref sy);
        var btnHome = AddSidebarButton(pnlNavList, "🏠  Genel Bakış", ref sy, () => _tabControl.SelectedIndex = 0, isHome: true);
        _activeSidebarBtn = btnHome;

        // 2. CARİ YÖNETİMİ
        AddSidebarHeader(pnlNavList, "CARİ & MÜŞTERİ", ref sy);
        AddSidebarButton(pnlNavList, "⚡  Cari Hesap Ekstresi", ref sy, () => OpenModuleTab("CariEkstre", "⚡ Cari Hesap Ekstresi", () => new CariHesapEkstresiForm()), isHighlighted: true);
        AddSidebarButton(pnlNavList, "👥  Cari Kartları (17.3K)", ref sy, () => OpenModuleTab("CariKartlar", "👥 Cari Hesaplar", () => new CariHesaplarForm()));
        AddSidebarButton(pnlNavList, "⚖️  Borç / Alacak Listesi", ref sy, () => OpenModuleTab("CariBakiye", "⚖️ Borç / Alacak Listesi", () => new CariBakiyeForm()));

        // 3. TARTI & ÜRETİM
        AddSidebarHeader(pnlNavList, "KANTAR & ÜRETİM", ref sy);
        AddSidebarButton(pnlNavList, "🚚  Yükleme Programı", ref sy, () => OpenModuleTab("YuklemeProgrami", "🚚 Yükleme Programı", () => new YuklemeProgramiForm()), isHighlighted: true);
        AddSidebarButton(pnlNavList, "📋  Amele Puantaj Defteri", ref sy, () => OpenModuleTab("Puantaj", "📋 Amele Puantaj Defteri", () => new PuantajForm()), isHighlighted: true);
        AddSidebarButton(pnlNavList, "⚖️  Mal Kabul & Canlı Kantar", ref sy, () => OpenModuleTab("MalKabul", "⚖️ Mal Kabul & Kantar", () => new MalKabulForm()));
        AddSidebarButton(pnlNavList, "📋  Kantar / Tartı Fişleri", ref sy, () => OpenModuleTab("Kantar", "📋 Kantar Fişleri", () => new KantarForm()));
        AddSidebarButton(pnlNavList, "🧪  Etilen & Sarartma Odaları", ref sy, () => OpenModuleTab("Etilen", "🧪 Etilen & Sarartma", () => new EtilenStokTakipForm()));
        AddSidebarButton(pnlNavList, "🏭  Paketleme & Tartım", ref sy, () => OpenModuleTab("Paketleme", "🏭 Paketleme & Üretim", () => new PaketlemeForm()));
        AddSidebarButton(pnlNavList, "🌳  Bahçe Kesim & Sözleşme", ref sy, () => OpenModuleTab("BahceKesim", "🌳 Bahçe Kesim & Alım", () => new BahceKesimForm()));

        // 4. STOK & DEPO
        AddSidebarHeader(pnlNavList, "STOK & DEPO", ref sy);
        AddSidebarButton(pnlNavList, "📦  Stok Kartları", ref sy, () => OpenModuleTab("StokList", "📦 Stok Kartları", () => new StokListForm()));
        AddSidebarButton(pnlNavList, "🔄  Stok Hareketleri", ref sy, () => OpenModuleTab("StokHareket", "🔄 Stok Hareketleri", () => new StokHareketForm()));
        AddSidebarButton(pnlNavList, "📊  Stok Kartı Ekstresi", ref sy, () => OpenModuleTab("StokEkstre", "📊 Stok Ekstresi", () => new StokEkstreForm()));

        // 5. TİCARİ & FATURA
        AddSidebarHeader(pnlNavList, "TİCARİ & FATURA", ref sy);
        AddSidebarButton(pnlNavList, "🚚  Satış İrsaliyeleri", ref sy, () => OpenModuleTab("SatisIrsaliye", "🚚 Satış İrsaliyeleri", () => new SatisIrsaliyeForm()), isHighlighted: true);
        AddSidebarButton(pnlNavList, "📄  Satış Faturaları", ref sy, () => OpenModuleTab("SatisFatura", "📄 Satış Faturaları", () => new FaturalarForm(FaturaTip.SatisFaturasi)));
        AddSidebarButton(pnlNavList, "📥  Alış Faturaları", ref sy, () => OpenModuleTab("AlisFatura", "📥 Alış Faturaları", () => new FaturalarForm(FaturaTip.AlisFaturasi)));
        AddSidebarButton(pnlNavList, "🌐  İhracat Faturaları", ref sy, () => OpenModuleTab("IhracatFatura", "🌐 İhracat Faturaları", () => new FaturalarForm(FaturaTip.IhracatFaturasi)));
        AddSidebarButton(pnlNavList, "🌾  Müstahsil Makbuzları", ref sy, () => OpenModuleTab("Mustahsil", "🌾 Müstahsil Makbuzları", () => new OdemeMakbuzForm(MakbuzTip.MustahsilMakbuzu)));

        // 6. FİNANS & BANKA
        AddSidebarHeader(pnlNavList, "FİNANS & BANKA", ref sy);
        AddSidebarButton(pnlNavList, "💳  Ödeme Talep İşlemleri", ref sy, () => OpenModuleTab("OdemeTalep", "💳 Ödeme Talepleri", () => new OdemeTalepIslemleriForm()));
        AddSidebarButton(pnlNavList, "💰  Kasa Hesap Ekstresi", ref sy, () => OpenModuleTab("KasaEkstre", "💰 Kasa Ekstresi", () => new KasaEkstreForm()));
        AddSidebarButton(pnlNavList, "🏦  Banka Hesapları", ref sy, () => OpenModuleTab("BankaHesap", "🏦 Banka Hesapları", () => new BankaHesapForm()));
        AddSidebarButton(pnlNavList, "📝  Çek & Senet Listesi", ref sy, () => OpenModuleTab("CekList", "📝 Çek Listesi", () => new CekListForm()));

        // 7. SİSTEM
        AddSidebarHeader(pnlNavList, "SİSTEM", ref sy);
        if (AppState.IsAdmin)
        {
            AddSidebarButton(pnlNavList, "👑  Kullanıcı Yönetimi", ref sy, () => OpenModuleTab("UserMgmt", "👑 Kullanıcı Yönetimi", () => new UserManagementForm()));
            AddSidebarButton(pnlNavList, "🛡️  Giriş & Güvenlik Logları", ref sy, () => OpenModuleTab("LoginLogs", "🛡️ Giriş Logları", () => new UserLoginLogsForm()));
        }
        AddSidebarButton(pnlNavList, "🚪  Oturumu Kapat", ref sy, DoLogout);

        _pnlSidebar.Controls.Add(pnlNavList);
    }

    private void AddSidebarHeader(Panel parent, string title, ref int y)
    {
        var lbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(14, y + 8),
            AutoSize = true
        };
        parent.Controls.Add(lbl);
        y += 26;
    }

    private Button AddSidebarButton(Panel parent, string title, ref int y, Action onClick, bool isHighlighted = false, bool isHome = false)
    {
        var btn = new Button
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, isHighlighted || isHome ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = isHighlighted ? ModernColors.Emerald500 : Color.FromArgb(226, 232, 240),
            BackColor = isHome ? Color.FromArgb(30, 41, 59) : Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(4, y),
            Size = new Size(210, 36),
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 0, 0, 0)
        };
        btn.FlatAppearance.BorderSize = 0;

        btn.MouseEnter += (s, e) =>
        {
            btn.BackColor = Color.FromArgb(30, 41, 59);
            btn.ForeColor = Color.White;
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn != _activeSidebarBtn)
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = isHighlighted ? ModernColors.Emerald500 : Color.FromArgb(226, 232, 240);
            }
        };

        btn.Click += (_, _) =>
        {
            SetActiveSidebarButton(btn);
            onClick();
        };

        _sidebarButtons.Add(btn);
        parent.Controls.Add(btn);
        y += 38;
        return btn;
    }

    private void SetActiveSidebarButton(Button btn)
    {
        if (_activeSidebarBtn != null)
        {
            _activeSidebarBtn.BackColor = Color.Transparent;
            _activeSidebarBtn.ForeColor = Color.FromArgb(226, 232, 240);
        }
        _activeSidebarBtn = btn;
        _activeSidebarBtn.BackColor = Color.FromArgb(30, 41, 59);
        _activeSidebarBtn.ForeColor = Color.White;
    }

    private void BuildTabContainer()
    {
        _pnlMainContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(0)
        };

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(185, 34),
            SizeMode = TabSizeMode.Normal,
            Padding = new Point(14, 6)
        };

        // Modern TabControl Owner Draw Styling
        _tabControl.DrawItem += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var tabRect = _tabControl.GetTabRect(e.Index);
            bool isSelected = _tabControl.SelectedIndex == e.Index;

            // Background
            var bgColor = isSelected ? Color.White : Color.FromArgb(241, 245, 249);
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, tabRect);
            }

            // Top accent indicator for active tab
            if (isSelected)
            {
                using var accentBrush = new SolidBrush(ModernColors.Teal600);
                g.FillRectangle(accentBrush, tabRect.X, tabRect.Y, tabRect.Width, 3);
            }

            // Text
            string text = _tabControl.TabPages[e.Index].Text;
            var textColor = isSelected ? ModernColors.Slate900 : ModernColors.Slate500;
            var font = isSelected ? new Font("Segoe UI", 9f, FontStyle.Bold) : new Font("Segoe UI", 9f);
            using (var textBrush = new SolidBrush(textColor))
            {
                var textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(text, font, textBrush, tabRect, textFormat);
            }

            // Bottom border
            using var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f);
            g.DrawLine(borderPen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right, tabRect.Bottom - 1);
            if (!isSelected)
            {
                g.DrawLine(borderPen, tabRect.Right - 1, tabRect.Top + 6, tabRect.Right - 1, tabRect.Bottom - 6);
            }
        };

        // Tab 0: Home Dashboard
        var tabHome = new TabPage
        {
            Text = "  🏠  Genel Bakış  ",
            Tag = "Dashboard",
            BackColor = ModernColors.Slate100
        };

        _dashboardView = new DashboardControl();
        _dashboardView.ModuleRequested += OnDashboardModuleRequested;
        tabHome.Controls.Add(_dashboardView);

        _tabControl.TabPages.Add(tabHome);
        _pnlMainContent.Controls.Add(_tabControl);
    }

    private void OnDashboardModuleRequested(string moduleKey)
    {
        switch (moduleKey)
        {
            case "CariEkstre":
                OpenModuleTab("CariEkstre", "⚡ Cari Hesap Ekstresi", () => new CariHesapEkstresiForm());
                break;
            case "CariKartlar":
                OpenModuleTab("CariKartlar", "👥 Cari Hesaplar", () => new CariHesaplarForm());
                break;
            case "CariBakiye":
                OpenModuleTab("CariBakiye", "⚖️ Borç / Alacak Listesi", () => new CariBakiyeForm());
                break;
            case "OdemeTalep":
                OpenModuleTab("OdemeTalep", "💳 Ödeme Talepleri", () => new OdemeTalepIslemleriForm());
                break;
            case "Kantar":
                OpenModuleTab("Kantar", "📋 Kantar Fişleri", () => new KantarForm());
                break;
            case "MalKabul":
                OpenModuleTab("MalKabul", "⚖️ Mal Kabul & Kantar", () => new MalKabulForm());
                break;
            case "Etilen":
                OpenModuleTab("Etilen", "🧪 Etilen & Sarartma", () => new EtilenStokTakipForm());
                break;
            case "Paketleme":
                OpenModuleTab("Paketleme", "🏭 Paketleme & Üretim", () => new PaketlemeForm());
                break;
            case "BahceKesim":
                OpenModuleTab("BahceKesim", "🌳 Bahçe Kesim & Alım", () => new BahceKesimForm());
                break;
            case "YuklemeProgrami":
                OpenModuleTab("YuklemeProgrami", "🚚 Yükleme Programı", () => new YuklemeProgramiForm());
                break;
            case "Puantaj":
                OpenModuleTab("Puantaj", "📋 Amele Puantaj Defteri", () => new PuantajForm());
                break;
            case "StokList":
                OpenModuleTab("StokList", "📦 Stok Kartları", () => new StokListForm());
                break;
            case "StokHareket":
                OpenModuleTab("StokHareket", "🔄 Stok Hareketleri", () => new StokHareketForm());
                break;
            case "StokEkstre":
                OpenModuleTab("StokEkstre", "📊 Stok Ekstresi", () => new StokEkstreForm());
                break;
            case "SatisIrsaliye":
                OpenModuleTab("SatisIrsaliye", "🚚 Satış İrsaliyeleri", () => new SatisIrsaliyeForm());
                break;
            case "SatisFatura":
                OpenModuleTab("SatisFatura", "📄 Satış Faturaları", () => new FaturalarForm(FaturaTip.SatisFaturasi));
                break;
            case "AlisFatura":
                OpenModuleTab("AlisFatura", "📥 Alış Faturaları", () => new FaturalarForm(FaturaTip.AlisFaturasi));
                break;
            case "IhracatFatura":
                OpenModuleTab("IhracatFatura", "🌐 İhracat Faturaları", () => new FaturalarForm(FaturaTip.IhracatFaturasi));
                break;
            case "Mustahsil":
                OpenModuleTab("Mustahsil", "🌾 Müstahsil Makbuzları", () => new OdemeMakbuzForm(MakbuzTip.MustahsilMakbuzu));
                break;
            case "KasaEkstre":
                OpenModuleTab("KasaEkstre", "💰 Kasa Ekstresi", () => new KasaEkstreForm());
                break;
            case "BankaHesap":
                OpenModuleTab("BankaHesap", "🏦 Banka Hesapları", () => new BankaHesapForm());
                break;
            case "CekList":
                OpenModuleTab("CekList", "📝 Çek Listesi", () => new CekListForm());
                break;
            case "UserMgmt":
                if (AppState.IsAdmin) OpenModuleTab("UserMgmt", "👑 Kullanıcı Yönetimi", () => new UserManagementForm());
                break;
            case "LoginLogs":
                if (AppState.IsAdmin) OpenModuleTab("LoginLogs", "🛡️ Giriş Logları", () => new UserLoginLogsForm());
                break;
        }
    }

    public void OpenModuleTab(string tabKey, string tabTitle, Func<Form> formFactory)
    {
        // Check if tab already open
        foreach (TabPage tab in _tabControl.TabPages)
        {
            if (tab.Tag as string == tabKey)
            {
                _tabControl.SelectedTab = tab;
                return;
            }
        }

        // Create new tab page
        var newTab = new TabPage
        {
            Text = $"  {tabTitle}  ✕",
            Tag = tabKey,
            BackColor = Color.White
        };

        var form = formFactory();
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        form.Visible = true;

        newTab.Controls.Add(form);
        _tabControl.TabPages.Add(newTab);
        _tabControl.SelectedTab = newTab;

        // Click on '✕' area to close tab
        _tabControl.MouseClick -= TabControl_MouseClick;
        _tabControl.MouseClick += TabControl_MouseClick;
    }

    private void TabControl_MouseClick(object? sender, MouseEventArgs e)
    {
        for (int i = 1; i < _tabControl.TabPages.Count; i++)
        {
            var rect = _tabControl.GetTabRect(i);
            var closeRect = new Rectangle(rect.Right - 28, rect.Top + 4, 24, 26);
            if (closeRect.Contains(e.Location))
            {
                var pageToClose = _tabControl.TabPages[i];
                _tabControl.TabPages.Remove(pageToClose);
                pageToClose.Dispose();
                break;
            }
        }
    }

    private void BuildStatusBar()
    {
        _statusStrip = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            BackColor = ModernColors.Slate900,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(148, 163, 184)
        };

        var lblStatusDb = new ToolStripStatusLabel
        {
            Text = "⚡ Yerel Replikasyon: 127.0.0.1:3306/erp (Aktif & Senkron)",
            ForeColor = ModernColors.Emerald500,
            Padding = new Padding(10, 0, 10, 0)
        };

        var lblStatusCanli = new ToolStripStatusLabel
        {
            Text = "🔒 Canlı Sistem: Salt Okunur Güvenlik Protokolü devrede",
            ForeColor = Color.FromArgb(203, 213, 225),
            Padding = new Padding(10, 0, 10, 0)
        };

        var lblStatusCur = new ToolStripStatusLabel
        {
            Text = "💵 USD/TRY: 34.2500   💶 EUR/TRY: 37.1000   🇪🇺 EUR/USD: 1.0832",
            ForeColor = ModernColors.Amber500,
            Padding = new Padding(10, 0, 10, 0)
        };

        _statusStrip.Items.AddRange(new ToolStripItem[] { lblStatusDb, lblStatusCanli, lblStatusCur });
    }

    private void StartClock()
    {
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) =>
        {
            if (_lblClock != null && !_lblClock.IsDisposed)
                _lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
        };
        _clockTimer.Start();
    }

    private void SwitchCompany()
    {
        var result = MessageBox.Show(
            "Farklı bir şirkete geçiş yapmak için mevcut oturum kapatılacak ve şirket seçim ekranına yönlendirileceksiniz.\n\nDevam etmek istiyor musunuz?",
            "Şirket Değiştir", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            DoLogout();
        }
    }

    private void DoLogout()
    {
        if (AppState.CurrentSessionDbId.HasValue)
        {
            var macro = new MacroService();
            _ = macro.LogUserLogoutAsync(AppState.CurrentSessionDbId.Value);
        }
        _clockTimer?.Stop();
        AppState.Clear();
        this.Hide();
        var login = new LoginForm();
        login.Show();
    }
}
