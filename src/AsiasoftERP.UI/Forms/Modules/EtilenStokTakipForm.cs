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
/// Etilen ve Sarartma Odaları Takip Modülü
/// Live integration with Tek Asya Tarım's central database at \\192.168.1.252\it\ANIL\etilen_stok_v7.json
/// </summary>
public class EtilenStokTakipForm : Form
{
    private readonly EtilenService _service = new();
    private EtilenDatabaseRoot _dbData = new();

    // Header controls
    private Label _lblStatusBadge = null!;
    private Button _btnRefresh = null!;
    private Button _btnLaunchExe = null!;
    private Button _btnOpenFolder = null!;

    // KPI Labels
    private Label _lblKpiOdalar = null!;
    private Label _lblKpiDoluluk = null!;
    private Label _lblKpiGazlama = null!;
    private Label _lblKpiSaha = null!;

    // Navigation Tabs
    private TabControl _tabs = null!;
    private FlowLayoutPanel _flpRoomCards = null!;
    private DataGridView _gridLots = null!;
    private DataGridView _gridLogs = null!;

    public EtilenStokTakipForm()
    {
        InitializeComponents();
        this.Load += async (_, _) => await LoadDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "Etilen & Sarartma Odaları Takip Sistemi";
        this.Size = new Size(1350, 820);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(241, 245, 249);
        this.KeyPreview = true;
        this.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.F5) await LoadDataAsync();
        };

        // 1. Top Command Panel
        var pnlHeader = CreateHeaderPanel();

        // 2. KPI Ribbon
        var pnlKpi = CreateKpiRibbon();

        // 3. Tab Control
        CreateTabPages();

        this.Controls.Add(_tabs);
        this.Controls.Add(pnlKpi);
        this.Controls.Add(pnlHeader);
    }

    private Panel CreateHeaderPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        var lblIcon = new Label
        {
            Text = "ETİLEN",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Location = new Point(14, 15),
            Size = new Size(58, 26),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblTitle = new Label
        {
            Text = "SARARTMA ODALARI TAKİP SİSTEMİ",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(80, 11),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = @"Tek Asya Tarım - Merkezi Ağ Veritabanı: \\192.168.1.252\it\ANIL\etilen_stok_v7.json",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(82, 35),
            AutoSize = true
        };

        // Status Badge
        _lblStatusBadge = new Label
        {
            Text = "Bağlanılıyor...",
            BackColor = Color.FromArgb(254, 243, 199),
            ForeColor = Color.FromArgb(180, 83, 9),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(10, 5, 10, 5),
            Location = new Point(560, 17),
            AutoSize = true
        };

        // Action Buttons on Right
        int btnX = pnl.Width - 460;

        _btnRefresh = new Button
        {
            Text = "Yenile (F5)",
            Size = new Size(100, 34),
            Location = new Point(btnX, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(30, 41, 59),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnRefresh.Click += async (_, _) => await LoadDataAsync();
        btnX += 108;

        _btnLaunchExe = new Button
        {
            Text = "▶  Etilen EXE Başlat",
            Size = new Size(155, 34),
            Location = new Point(btnX, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnLaunchExe.FlatAppearance.BorderSize = 0;
        _btnLaunchExe.Click += (_, _) =>
        {
            if (!_service.LaunchDesktopApp(out string err))
            {
                MessageBox.Show(err, "Uygulama Başlatılamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        btnX += 163;

        _btnOpenFolder = new Button
        {
            Text = "Ağ Klasörü",
            Size = new Size(110, 34),
            Location = new Point(btnX, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(30, 41, 59),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnOpenFolder.Click += (_, _) => _service.OpenNetworkFolder();

        pnl.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSub, _lblStatusBadge, _btnRefresh, _btnLaunchExe, _btnOpenFolder });

        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(226, 232, 240) };
        pnl.Controls.Add(sep);

        return pnl;
    }

    private Panel CreateKpiRibbon()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(15, 8, 15, 8)
        };

        int cardW = 280;
        int x = 15;

        _lblKpiOdalar = CreateKpiCard(pnl, "AKTİF SARARTMA ODALARI", "8 Oda + Saha", Color.FromArgb(30, 41, 59), ref x, cardW);
        _lblKpiDoluluk = CreateKpiCard(pnl, "TOPLAM DOLULUK", "0 / 0 Palet", Color.FromArgb(2, 132, 199), ref x, cardW);
        _lblKpiGazlama = CreateKpiCard(pnl, "ETİLEN VERİLEN ODALAR", "0 Oda Aktif", Color.FromArgb(16, 185, 129), ref x, cardW);
        _lblKpiSaha = CreateKpiCard(pnl, "SAHADA BEKLEYEN PALET", "0 Palet", Color.FromArgb(180, 83, 9), ref x, cardW);

        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(226, 232, 240) };
        pnl.Controls.Add(sep);

        return pnl;
    }

    private Label CreateKpiCard(Panel parent, string title, string initialValue, Color valueColor, ref int x, int width)
    {
        var card = new Panel
        {
            Location = new Point(x, 8),
            Size = new Size(width, 54),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblT = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(12, 6),
            AutoSize = true
        };

        var lblV = new Label
        {
            Text = initialValue,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = valueColor,
            Location = new Point(12, 24),
            AutoSize = true
        };

        card.Controls.AddRange(new Control[] { lblT, lblV });
        parent.Controls.Add(card);

        x += width + 12;
        return lblV;
    }

    private void CreateTabPages()
    {
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            Padding = new Point(16, 8)
        };

        // Tab 1: Room Visual Matrix
        var tabRooms = new TabPage { Text = "Oda Görsel Matrisi (Kapasite & Doluluk)" };
        _flpRoomCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(15),
            BackColor = Color.FromArgb(241, 245, 249)
        };
        tabRooms.Controls.Add(_flpRoomCards);

        // Tab 2: All Lots DataGridView
        var tabLots = new TabPage { Text = "Tüm Parti ve Palet Detay Tablosu" };
        _gridLots = CreateLotsGrid();
        tabLots.Controls.Add(_gridLots);

        // Tab 3: System Logs
        var tabLogs = new TabPage { Text = "Son İşlem & Transfer Logları" };
        _gridLogs = CreateLogsGrid();
        tabLogs.Controls.Add(_gridLogs);

        _tabs.TabPages.AddRange(new TabPage[] { tabRooms, tabLots, tabLogs });
    }

    private DataGridView CreateLotsGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(226, 232, 240),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 }
        };

        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RoomId", HeaderText = "Oda", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Grower", HeaderText = "Müstahsil / Cari", Width = 230, DefaultCellStyle = { Font = new Font("Segoe UI", 9.25f, FontStyle.Bold) } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Fruit", HeaderText = "Meyve Türü", Width = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Variety", HeaderText = "Çeşit", Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Pallets", HeaderText = "Palet Sayısı", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HarvestDate", HeaderText = "Hasat Tarihi", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DurationHours", HeaderText = "Etilen Süresi (Saat)", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HoursRemaining", HeaderText = "Kalan Süre (Saat)", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(16, 185, 129), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });

        return grid;
    }

    private DataGridView CreateLogsGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(226, 232, 240),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 28 }
        };

        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DateTime", HeaderText = "İşlem Zamanı", Width = 150, DefaultCellStyle = { Format = "dd.MM.yyyy HH:mm:ss", Alignment = DataGridViewContentAlignment.MiddleCenter } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Kullanıcı", Width = 150, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RoomId", HeaderText = "Oda", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Action", HeaderText = "Gerçekleşen İşlem / Hareket", Width = 480 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StatusStr", HeaderText = "Durum", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Machine", HeaderText = "Terminal / Makine", Width = 140 });

        return grid;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            _btnRefresh.Enabled = false;

            var (data, isLive, msg) = await _service.LoadDataAsync();
            _dbData = data;

            _lblStatusBadge.Text = "●  " + msg.Replace("🟢", "").Replace("⏳", "").Trim();
            _lblStatusBadge.BackColor = isLive ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 243, 199);
            _lblStatusBadge.ForeColor = isLive ? Color.FromArgb(22, 101, 52) : Color.FromArgb(146, 64, 14);

            UpdateKpiStats();
            RenderRoomCards();
            RenderLotsGrid();
            RenderLogsGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veriler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnRefresh.Enabled = true;
            Cursor.Current = Cursors.Default;
        }
    }

    private void UpdateKpiStats()
    {
        int activeRooms = _dbData.Rooms.Count(r => r.Active && !r.IsSaha);
        int totalCapacity = _dbData.Rooms.Where(r => r.Active && !r.IsSaha).Sum(r => r.Capacity);
        int totalPalletsInRooms = _dbData.Rooms.Where(r => r.Active && !r.IsSaha).Sum(r => r.TotalPallets);
        int ethyleneActiveCount = _dbData.Rooms.Count(r => r.EthyleneActive);
        
        var sahaRoom = _dbData.Rooms.FirstOrDefault(r => r.IsSaha);
        int sahaPallets = sahaRoom?.TotalPallets ?? 0;

        double occPct = totalCapacity > 0 ? Math.Round((double)totalPalletsInRooms / totalCapacity * 100, 1) : 0;

        _lblKpiOdalar.Text = $"{activeRooms} Oda Aktif";
        _lblKpiDoluluk.Text = $"{totalPalletsInRooms} / {totalCapacity} Palet (%{occPct})";
        _lblKpiGazlama.Text = $"{ethyleneActiveCount} Oda Gazlama";
        _lblKpiSaha.Text = $"{sahaPallets} Palet";
    }

    private void RenderRoomCards()
    {
        _flpRoomCards.SuspendLayout();
        _flpRoomCards.Controls.Clear();

        foreach (var room in _dbData.Rooms.Where(r => r.Active).OrderBy(r => r.SortOrder))
        {
            var card = CreateRoomCard(room);
            _flpRoomCards.Controls.Add(card);
        }

        _flpRoomCards.ResumeLayout();
    }

    private Control CreateRoomCard(EtilenRoomDto room)
    {
        var pnl = new Panel
        {
            Size = new Size(390, 240),
            Margin = new Padding(10),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Header Strip
        var pnlHead = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = room.EthyleneActive 
                ? Color.FromArgb(220, 252, 231) 
                : (room.TotalPallets > 0 ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252)),
            Padding = new Padding(12, 6, 12, 6)
        };

        var lblStatus = new Label
        {
            Text = room.EthyleneActive ? "GAZ VERİLİYOR" : (room.TotalPallets > 0 ? $"{room.TotalPallets} Palet" : "BOŞ"),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = room.EthyleneActive ? Color.FromArgb(22, 101, 52) : (room.TotalPallets > 0 ? Color.FromArgb(30, 41, 59) : Color.FromArgb(148, 163, 184)),
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblRoomName = new Label
        {
            Text = room.Name.Replace("📍", "").Trim(),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = room.EthyleneActive ? Color.FromArgb(22, 101, 52) : Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnlHead.Controls.AddRange(new Control[] { lblStatus, lblRoomName });

        // Capacity Bar (Only if not Saha)
        var pnlCap = new Panel
        {
            Dock = DockStyle.Top,
            Height = 24,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 4, 12, 4)
        };

        var lblCap = new Label
        {
            Text = room.IsSaha ? $"Saha Depo Alanı (Toplam {room.TotalPallets} Palet)" : $"Kapasite: {room.TotalPallets} / {room.Capacity} Palet (Doluluk: %{room.OccupancyPercent})",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(12, 4),
            AutoSize = true
        };
        pnlCap.Controls.Add(lblCap);

        // Lots List Box
        var lstLots = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 8.5f),
            ItemHeight = 20
        };

        if (room.Lots != null && room.Lots.Count > 0)
        {
            foreach (var lot in room.Lots)
            {
                string info = $"• {lot.Grower} | {lot.Fruit} ({lot.Variety}) | {lot.Pallets} Palet";
                if (lot.HoursRemaining > 0) info += $" - {lot.HoursRemaining}s kaldı";
                lstLots.Items.Add(info);
            }
        }
        else
        {
            lstLots.Items.Add("   (Odada şu an ürün bulunmuyor)");
        }

        pnl.Controls.Add(lstLots);
        pnl.Controls.Add(pnlCap);
        pnl.Controls.Add(pnlHead);

        return pnl;
    }

    private void RenderLotsGrid()
    {
        var allLots = new List<EtilenLotDto>();
        foreach (var room in _dbData.Rooms)
        {
            if (room.Lots != null)
            {
                allLots.AddRange(room.Lots);
            }
        }

        _gridLots.DataSource = null;
        _gridLots.DataSource = allLots;
    }

    private void RenderLogsGrid()
    {
        _gridLogs.DataSource = null;
        if (_dbData.Logs != null)
        {
            _gridLogs.DataSource = _dbData.Logs.Take(150).ToList();
        }
    }
}
