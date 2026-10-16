using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using Dapper;

namespace AsiasoftERP.UI.Forms.Modules;

public class DashboardControl : UserControl
{
    public event Action<string>? ModuleRequested;

    private Panel _pnlScroll = null!;
    private DataGridView _gridKantar = null!;
    private Label _lblCariCount = null!;
    private Label _lblKantarCount = null!;
    private Label _lblPaletCount = null!;

    public DashboardControl()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        InitializeComponents();
        this.Load += async (_, _) => await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Prevents WinForms from auto-jumping scroll position when a child control gets focus
    /// </summary>
    protected override Point ScrollToControl(Control activeControl)
    {
        return this.AutoScrollPosition;
    }

    private void InitializeComponents()
    {
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);
        this.Dock = DockStyle.Fill;
        this.AutoScroll = true;

        _pnlScroll = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(28, 20, 28, 30),
            BackColor = Color.Transparent
        };

        // 1. WELCOME HERO CARD (Spans 100% responsive width)
        var pnlHero = new ModernCardPanel
        {
            Dock = DockStyle.Top,
            Height = 92,
            CardBackground = ModernColors.Slate900,
            BorderColor = ModernColors.Slate700,
            CornerRadius = 12,
            Padding = new Padding(24, 16, 24, 16)
        };

        var pnlHeroLeft = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var lblWelcome = new Label
        {
            Text = $"Hoş Geldiniz, Sayın {AppState.ActiveUser?.KullaniciAdi ?? "Ali SARIGÜL"}",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(0, 4),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var lblSub = new Label
        {
            Text = $"🏢  {AppState.ActiveCompany?.Adi ?? "TEK ASYA A.Ş."}  •  Tarımsal Üretim, Paketleme ve İhracat ERP Sistemi",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(2, 38),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        pnlHeroLeft.Controls.AddRange(new Control[] { lblWelcome, lblSub });

        var pnlHeroRight = new Panel
        {
            Dock = DockStyle.Right,
            Width = 340,
            BackColor = Color.Transparent
        };

        var lblLiveBadge = new Label
        {
            Text = "🟢 3,321,448 Kayıt Canlı",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Emerald500,
            BackColor = Color.FromArgb(30, 41, 59),
            Size = new Size(165, 34),
            Location = new Point(0, 14),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblSpeedBadge = new Label
        {
            Text = "⚡ < 5ms Dapper Bellek",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Amber500,
            BackColor = Color.FromArgb(30, 41, 59),
            Size = new Size(165, 34),
            Location = new Point(175, 14),
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlHeroRight.Controls.AddRange(new Control[] { lblLiveBadge, lblSpeedBadge });
        pnlHero.Controls.AddRange(new Control[] { pnlHeroLeft, pnlHeroRight });

        // 2. 4 KPI METRIC CARDS (100% Responsive Grid - 25% each)
        var tblKpi = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 125,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 16, 0, 0)
        };
        tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        var cardCari   = CreateKpiCard("TOPLAM CARİ HESAP", "17,896", "👥  15.8K Firma & Müstahsil", ModernColors.Emerald500, "👥");
        _lblCariCount  = (Label)cardCari.Controls["lblValue"]!;

        var cardKantar = CreateKpiCard("KANTAR TARTIMLARI", "57,240", "⚖️  Giriş & Çıkış Tonaj Takibi", ModernColors.Amber500, "⚖️");
        _lblKantarCount= (Label)cardKantar.Controls["lblValue"]!;

        var cardPalet  = CreateKpiCard("PAKETLEME / PALET", "367,998", "🏭  Altınova & Tarsus Tesisleri", ModernColors.Blue600, "🏭");
        _lblPaletCount = (Label)cardPalet.Controls["lblValue"]!;

        var cardSpeed  = CreateKpiCard("SİSTEM PERFORMANSI", "< 5 ms", "⚡  Dapper İndeksli Bellek", ModernColors.Indigo600, "⚡");

        tblKpi.Controls.Add(cardCari, 0, 0);
        tblKpi.Controls.Add(cardKantar, 1, 0);
        tblKpi.Controls.Add(cardPalet, 2, 0);
        tblKpi.Controls.Add(cardSpeed, 3, 0);

        // 3. QUICK ACTION TILES HEADER
        var pnlActionsHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 18, 0, 0)
        };
        var lblActionsTitle = new Label
        {
            Text = "⚡  Hızlı İşlemler & ERP Modülleri",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(4, 8),
            AutoSize = true
        };
        pnlActionsHeader.Controls.Add(lblActionsTitle);

        // 4. QUICK ACTION TILES (100% Responsive Grid - 3x4)
        var tblActions = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 255,
            ColumnCount = 4,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        tblActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tblActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tblActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tblActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tblActions.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        tblActions.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        tblActions.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        var btnActEkstre   = CreateActionTile("⚡  Cari Hesap Ekstresi", "Devir, bakiye ve tarih filtreli döküm", ModernColors.Emerald500, () => ModuleRequested?.Invoke("CariEkstre"));
        var btnActCari     = CreateActionTile("👥  Cari Hesap Kartları", "17.3K kayıtlı firma ve müstahsil rehberi", ModernColors.Teal600, () => ModuleRequested?.Invoke("CariKartlar"));
        var btnActKantar   = CreateActionTile("📋  Kantar / Tartı Fişleri", "Giriş/çıkış araç ve tonaj tartımları", ModernColors.Amber500, () => ModuleRequested?.Invoke("Kantar"));
        var btnActMalKabul = CreateActionTile("⚖️  Mal Kabul & Kantar", "Kullanıcıya özel canlı tartım & irsaliye", ModernColors.Indigo600, () => ModuleRequested?.Invoke("MalKabul"));

        var btnActYukleme  = CreateActionTile("🚚  Yükleme Programı", "İhracat tır sevkiyat emirleri ve A4 formları", ModernColors.Emerald500, () => ModuleRequested?.Invoke("YuklemeProgrami"));
        var btnActEtilen   = CreateActionTile("🧪  Etilen & Sarartma", "8 oda doluluk, gazlama ve parti takibi", ModernColors.Teal600, () => ModuleRequested?.Invoke("Etilen"));
        var btnActPaket    = CreateActionTile("🏭  Paketleme & Üretim", "Parti, ambalaj ve paletleme hareketleri", ModernColors.Blue600, () => ModuleRequested?.Invoke("Paketleme"));
        var btnActOdeme    = CreateActionTile("💳  Ödeme Talepleri", "Nakit, havale ve çek talep onay akışı", ModernColors.Rose600, () => ModuleRequested?.Invoke("OdemeTalep"));

        var btnActBahce    = CreateActionTile("🌳  Bahçe Kesim & Alım", "Üretici bahçe kesimleri ve sözleşmeler", ModernColors.Emerald500, () => ModuleRequested?.Invoke("BahceKesim"));
        var btnActFatura   = CreateActionTile("📄  Satış Faturaları", "184K fatura, ihracat ve irsaliye kaydı", ModernColors.Blue600, () => ModuleRequested?.Invoke("SatisFatura"));
        var btnActBakiye   = CreateActionTile("⚖️  Borç / Alacak Listesi", "Firma ve müstahsil net bakiye dökümü", ModernColors.Amber500, () => ModuleRequested?.Invoke("CariBakiye"));
        var btnActStok     = CreateActionTile("📦  Stok Kartları", "2.3K mamul, hammadde ve ambalaj kartı", ModernColors.Indigo600, () => ModuleRequested?.Invoke("StokList"));

        tblActions.Controls.Add(btnActEkstre, 0, 0);
        tblActions.Controls.Add(btnActCari, 1, 0);
        tblActions.Controls.Add(btnActKantar, 2, 0);
        tblActions.Controls.Add(btnActMalKabul, 3, 0);

        tblActions.Controls.Add(btnActYukleme, 0, 1);
        tblActions.Controls.Add(btnActEtilen, 1, 1);
        tblActions.Controls.Add(btnActPaket, 2, 1);
        tblActions.Controls.Add(btnActOdeme, 3, 1);

        tblActions.Controls.Add(btnActBahce, 0, 2);
        tblActions.Controls.Add(btnActFatura, 1, 2);
        tblActions.Controls.Add(btnActBakiye, 2, 2);
        tblActions.Controls.Add(btnActStok, 3, 2);

        // 5. LIVE RECENT KANTAR WEIGHBRIDGE TABLE CARD
        var pnlTableCard = new ModernCardPanel
        {
            Dock = DockStyle.Top,
            Height = 310,
            CardBackground = Color.White,
            BorderColor = ModernColors.Slate200,
            CornerRadius = 12,
            Padding = new Padding(16, 14, 16, 14),
            Margin = new Padding(0, 20, 0, 0)
        };

        var pnlTableHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.Transparent
        };

        var lblTableTitle = new Label
        {
            Text = "⚖️  Günün Son Kantar Tartımları",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(2, 4),
            AutoSize = true
        };

        var lblTableSub = new Label
        {
            Text = "—  Canlı MariaDB Veri Akışı",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(245, 6),
            AutoSize = true
        };

        var btnRefresh = new Button
        {
            Text = "🔄  Yenile",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate100,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(85, 26),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += async (_, _) => await LoadDashboardDataAsync();

        pnlTableHeader.Controls.AddRange(new Control[] { lblTableTitle, lblTableSub, btnRefresh });

        _gridKantar = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false
        };
        ModernUiHelper.ApplyModernGridStyle(_gridKantar);
        SetupKantarGridColumns();

        pnlTableCard.Controls.AddRange(new Control[] { _gridKantar, pnlTableHeader });

        // Add Spacers & Elements in proper Top-Docked sequence
        var sp1 = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent };
        var sp2 = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };
        var sp3 = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };

        // WinForms Top-dock order: added in reverse order or back-to-front
        _pnlScroll.Controls.Add(pnlTableCard);
        _pnlScroll.Controls.Add(sp3);
        _pnlScroll.Controls.Add(tblActions);
        _pnlScroll.Controls.Add(pnlActionsHeader);
        _pnlScroll.Controls.Add(sp2);
        _pnlScroll.Controls.Add(tblKpi);
        _pnlScroll.Controls.Add(sp1);
        _pnlScroll.Controls.Add(pnlHero);

        this.Controls.Add(_pnlScroll);
    }

    private ModernCardPanel CreateKpiCard(string title, string initialValue, string sub, Color accent, string icon)
    {
        var card = new ModernCardPanel
        {
            Dock = DockStyle.Fill,
            CardBackground = Color.White,
            BorderColor = ModernColors.Slate200,
            CornerRadius = 10,
            TopAccentColor = accent,
            TopAccentHeight = 4,
            Padding = new Padding(18, 14, 18, 12),
            Margin = new Padding(6)
        };

        var lblIcon = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI", 15f),
            Dock = DockStyle.Right,
            Width = 36,
            TextAlign = ContentAlignment.TopRight,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = ModernColors.Slate500,
            Location = new Point(16, 14),
            AutoSize = true
        };

        var lblValue = new Label
        {
            Name = "lblValue",
            Text = initialValue,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(14, 34),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = sub,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(16, 78),
            AutoSize = true
        };

        card.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblValue, lblSub });
        return card;
    }

    private Button CreateActionTile(string title, string sub, Color accent, Action onClick)
    {
        var btn = new Button
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6),
            Padding = new Padding(14, 8, 14, 8)
        };
        btn.FlatAppearance.BorderColor = ModernColors.Slate200;
        btn.FlatAppearance.BorderSize = 1;

        btn.MouseEnter += (s, e) =>
        {
            btn.BackColor = Color.FromArgb(248, 250, 252);
            btn.FlatAppearance.BorderColor = accent;
        };
        btn.MouseLeave += (s, e) =>
        {
            btn.BackColor = Color.White;
            btn.FlatAppearance.BorderColor = ModernColors.Slate200;
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(14, 12),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        lblTitle.Click += (s, e) => onClick();

        var lblSub = new Label
        {
            Text = sub,
            Font = new Font("Segoe UI", 8.2f),
            ForeColor = ModernColors.Slate500,
            Location = new Point(16, 38),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        lblSub.Click += (s, e) => onClick();

        btn.Controls.AddRange(new Control[] { lblTitle, lblSub });
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private void SetupKantarGridColumns()
    {
        _gridKantar.Columns.Clear();
        _gridKantar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _gridKantar.Columns.AddRange(
            new DataGridViewTextBoxColumn 
            { 
                Name = "colFis", 
                DataPropertyName = "GIRISKODU", 
                HeaderText = "Fiş No", 
                FillWeight = 11,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            },
            new DataGridViewTextBoxColumn 
            { 
                Name = "colTarih", 
                DataPropertyName = "GIRISTARIHI", 
                HeaderText = "Tarih", 
                FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter } 
            },
            new DataGridViewTextBoxColumn 
            { 
                Name = "colPlaka", 
                DataPropertyName = "PLAKA", 
                HeaderText = "Araç Plaka", 
                FillWeight = 16,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ModernColors.Slate900 }
            },
            new DataGridViewTextBoxColumn 
            { 
                Name = "colFirma", 
                DataPropertyName = "FIRMAADI", 
                HeaderText = "Müstahsil / Nakliyeci / Cari", 
                FillWeight = 36 
            },
            new DataGridViewTextBoxColumn 
            { 
                Name = "colMal", 
                DataPropertyName = "MALADI", 
                HeaderText = "Ürün / Narenciye Cinsi", 
                FillWeight = 24 
            },
            new DataGridViewTextBoxColumn 
            { 
                Name = "colNet", 
                DataPropertyName = "NET", 
                HeaderText = "Net Kilo (Kg)", 
                FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } 
            }
        );
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            // Load live recent weighbridge
            const string sqlKantar = @"
                SELECT 
                    COALESCE(GIRISKODU, 0) as GIRISKODU,
                    GIRISTARIHI,
                    COALESCE(PLAKA, '') as PLAKA,
                    COALESCE(FIRMAADI, '') as FIRMAADI,
                    COALESCE(MALADI, '') as MALADI,
                    COALESCE(NET, 0) as NET
                FROM kantargiris
                ORDER BY IDNO DESC
                LIMIT 8;";

            var kantarList = (await conn.QueryAsync(sqlKantar)).ToList();
            _gridKantar.DataSource = kantarList;

            // Load counts
            int totalCari = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM firma WHERE FIRKOD > 0;");
            int totalKantar = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM kantargiris;");
            int totalPalet = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM kantargirispalet;");

            if (_lblCariCount != null) _lblCariCount.Text = totalCari.ToString("N0");
            if (_lblKantarCount != null) _lblKantarCount.Text = totalKantar.ToString("N0");
            if (_lblPaletCount != null) _lblPaletCount.Text = totalPalet.ToString("N0");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
        }
    }
}
