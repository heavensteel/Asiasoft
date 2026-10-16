using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class ActiveSessionsForm : Form
{
    private ListView _lvDbSessions = null!;
    private ListView _lvRdpSessions = null!;
    private System.Windows.Forms.Timer _refreshTimer = null!;
    private Label _lblDbTitle = null!, _lblRdpTitle = null!;
    private Button _btnRefresh = null!;

    public ActiveSessionsForm()
    {
        InitializeComponents();
        _ = LoadDataAsync();
        StartAutoRefresh();
    }

    private void InitializeComponents()
    {
        this.Text = "🖥 Aktif Oturumlar (RDP)";
        this.Size = new Size(900, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(245, 246, 248);

        // Toolbar
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(0, 84, 166) };
        toolbar.Controls.Add(new Label
        {
            Text = "  👥  Aktif Kullanıcı Oturumları",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        });
        _btnRefresh = new Button
        {
            Text = "⟳ Yenile",
            Dock = DockStyle.Right,
            Width = 100,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _btnRefresh.FlatAppearance.BorderSize = 0;
        _btnRefresh.Click += async (s, e) => await LoadDataAsync();
        toolbar.Controls.Add(_btnRefresh);

        // Split container
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280
        };

        // === ERP DB Sessions ===
        var topPanel = new Panel { Dock = DockStyle.Fill };
        _lblDbTitle = new Label
        {
            Text = "  📊 ERP Uygulama Oturumları (Veritabanı Kayıtları)",
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(30, 80, 150),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lvDbSessions = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9.5f)
        };
        _lvDbSessions.Columns.Add("Kullanıcı", 140);
        _lvDbSessions.Columns.Add("Ad Soyad", 150);
        _lvDbSessions.Columns.Add("Makine", 130);
        _lvDbSessions.Columns.Add("Oturum", 110);
        _lvDbSessions.Columns.Add("RDP ID", 60);
        _lvDbSessions.Columns.Add("Giriş Saati", 130);
        _lvDbSessions.Columns.Add("Süre", 90);
        _lvDbSessions.Columns.Add("Modül", 130);

        topPanel.Controls.Add(_lvDbSessions);
        topPanel.Controls.Add(_lblDbTitle);

        // === Windows RDP Sessions ===
        var bottomPanel = new Panel { Dock = DockStyle.Fill };
        _lblRdpTitle = new Label
        {
            Text = "  🖥 Windows RDP/Terminal Oturumları (Bu Sunucu)",
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(30, 80, 150),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lvRdpSessions = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9.5f)
        };
        _lvRdpSessions.Columns.Add("Session ID", 80);
        _lvRdpSessions.Columns.Add("Kullanıcı", 140);
        _lvRdpSessions.Columns.Add("İstasyon", 120);
        _lvRdpSessions.Columns.Add("İstemci", 130);
        _lvRdpSessions.Columns.Add("Durum", 100);
        _lvRdpSessions.Columns.Add("Türü", 80);

        bottomPanel.Controls.Add(_lvRdpSessions);
        bottomPanel.Controls.Add(_lblRdpTitle);

        split.Panel1.Controls.Add(topPanel);
        split.Panel2.Controls.Add(bottomPanel);

        // Info label
        var infoLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            Text = $"  ℹ Mevcut oturum: {AppState.SessionName} (ID: {AppState.RdpSessionId}) — {AppState.CurrentUser?.Username}",
            BackColor = Color.FromArgb(220, 235, 255),
            ForeColor = Color.FromArgb(0, 60, 120),
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };

        this.Controls.Add(split);
        this.Controls.Add(infoLabel);
        this.Controls.Add(toolbar);
    }

    private async Task LoadDataAsync()
    {
        // Load ERP sessions from DB
        try
        {
            using var db = DbContextFactory.Create();
            var service = new AuthService(db);
            var sessions = await service.GetActiveSessionsAsync();

            _lvDbSessions.Items.Clear();
            foreach (var s in sessions)
            {
                var elapsed = DateTime.Now - s.LoginTime;
                var elapsedStr = elapsed.TotalHours >= 1
                    ? $"{(int)elapsed.TotalHours}s {elapsed.Minutes}dk"
                    : $"{elapsed.Minutes}dk {elapsed.Seconds}sn";

                var item = new ListViewItem(s.User?.Username ?? "?");
                item.SubItems.Add(s.User?.FullName ?? "");
                item.SubItems.Add(s.MachineName);
                item.SubItems.Add(s.SessionName);
                item.SubItems.Add(s.RdpSessionId?.ToString() ?? "");
                item.SubItems.Add(s.LoginTime.ToString("dd.MM.yyyy HH:mm:ss"));
                item.SubItems.Add(elapsedStr);
                item.SubItems.Add(s.CurrentModule ?? "");

                // Highlight current user
                if (s.User?.Id == AppState.CurrentUser?.Id)
                {
                    item.BackColor = Color.FromArgb(220, 255, 220);
                    item.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }

                _lvDbSessions.Items.Add(item);
            }
            _lblDbTitle.Text = $"  📊 ERP Uygulama Oturumları ({sessions.Count} aktif)";
        }
        catch { }

        // Load Windows RDP sessions
        try
        {
            var rdpSessions = RdpHelper.GetAllSessions();
            _lvRdpSessions.Items.Clear();

            foreach (var s in rdpSessions)
            {
                var item = new ListViewItem(s.SessionId.ToString());
                item.SubItems.Add(s.UserName);
                item.SubItems.Add(s.WinStationName);
                item.SubItems.Add(s.ClientName);
                item.SubItems.Add(s.State);
                item.SubItems.Add(s.IsRdp ? "RDP" : s.IsConsole ? "Konsol" : "Diğer");

                if (s.IsCurrentSession)
                {
                    item.BackColor = Color.FromArgb(220, 255, 220);
                    item.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }

                _lvRdpSessions.Items.Add(item);
            }
            _lblRdpTitle.Text = $"  🖥 Windows RDP Oturumları ({rdpSessions.Count(r => r.State == "WTSActive")} aktif, toplam {rdpSessions.Count})";
        }
        catch { }
    }

    private void StartAutoRefresh()
    {
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 30000 }; // 30 seconds
        _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
        _refreshTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        base.OnFormClosed(e);
    }
}
