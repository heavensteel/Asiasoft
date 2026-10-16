using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class VersionHistoryForm : Form
{
    private ListView _lvVersions = null!;
    private ListView _lvChanges = null!;
    private Label _lblReleaseNotes = null!;
    private RichTextBox _rtbNotes = null!;
    private SplitContainer _splitMain = null!;
    private SplitContainer _splitRight = null!;
    private Button _btnAddVersion = null!;

    public VersionHistoryForm()
    {
        InitializeComponents();
        _ = LoadDataAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "📋 Sürüm Geçmişi";
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(245, 246, 248);

        // Toolbar
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            BackColor = Color.FromArgb(0, 84, 166)
        };

        var lblTitle = new Label
        {
            Text = $"  🏷  Asiasoft ERP — Sürüm Geçmişi    (Güncel: v{VersionService.CurrentVersion})",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        if (AppState.IsAdmin)
        {
            _btnAddVersion = new Button
            {
                Text = "+ Sürüm Ekle",
                Dock = DockStyle.Right,
                Width = 130,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnAddVersion.FlatAppearance.BorderSize = 0;
            _btnAddVersion.Click += async (s, e) => await AddVersionAsync();
            toolbar.Controls.Add(_btnAddVersion);
        }

        toolbar.Controls.Add(lblTitle);

        // Split layout
        _splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 400
        };

        _splitRight = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 350
        };

        // Version list
        _lvVersions = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9.5f)
        };
        _lvVersions.Columns.Add("Sürüm", 100);
        _lvVersions.Columns.Add("Başlık", 250);
        _lvVersions.Columns.Add("Tarih", 110);
        _lvVersions.Columns.Add("Yayınlayan", 100);
        _lvVersions.Columns.Add("Kritik", 60);
        _lvVersions.SelectedIndexChanged += LvVersions_SelectedIndexChanged;

        // Changes list
        _lvChanges = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9.5f)
        };
        _lvChanges.Columns.Add("Tür", 90);
        _lvChanges.Columns.Add("Açıklama", 400);
        _lvChanges.Columns.Add("Modül", 100);

        // Release notes
        var notesPanel = new Panel { Dock = DockStyle.Fill };
        _lblReleaseNotes = new Label
        {
            Text = "Sürüm Notları:",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 84, 166)
        };
        _rtbNotes = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.None
        };
        notesPanel.Controls.Add(_rtbNotes);
        notesPanel.Controls.Add(_lblReleaseNotes);

        _splitRight.Panel1.Controls.Add(_lvChanges);
        _splitRight.Panel2.Controls.Add(notesPanel);

        _splitMain.Panel1.Controls.Add(_lvVersions);
        _splitMain.Panel2.Controls.Add(_splitRight);

        this.Controls.Add(_splitMain);
        this.Controls.Add(toolbar);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = DbContextFactory.Create();
            var service = new VersionService(db);
            var versions = await service.GetAllVersionsAsync();

            _lvVersions.Items.Clear();
            foreach (var v in versions)
            {
                var item = new ListViewItem(v.Version);
                item.SubItems.Add(v.Title);
                item.SubItems.Add(v.ReleaseDate.ToString("dd.MM.yyyy"));
                item.SubItems.Add(v.ReleasedBy);
                item.SubItems.Add(v.IsCritical ? "⚠ Kritik" : "");
                item.Tag = v;

                if (v.Version == VersionService.CurrentVersion)
                {
                    item.BackColor = Color.FromArgb(220, 255, 220);
                    item.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                if (v.IsCritical) item.ForeColor = Color.DarkRed;

                _lvVersions.Items.Add(item);
            }

            if (_lvVersions.Items.Count > 0)
                _lvVersions.Items[0].Selected = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LvVersions_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_lvVersions.SelectedItems.Count == 0) return;
        var v = _lvVersions.SelectedItems[0].Tag as VersionInfo;
        if (v == null) return;

        _rtbNotes.Text = v.ReleaseNotes;

        _lvChanges.Items.Clear();
        foreach (var change in v.Changes)
        {
            var typeText = change.ChangeType switch
            {
                ChangeType.NewFeature => "🆕 Yeni",
                ChangeType.BugFix => "🐛 Hata Düzeltme",
                ChangeType.Improvement => "⬆ İyileştirme",
                ChangeType.Breaking => "⚠ Kırıcı",
                ChangeType.Security => "🔒 Güvenlik",
                _ => change.ChangeType.ToString()
            };
            var item = new ListViewItem(typeText);
            item.SubItems.Add(change.Description);
            item.SubItems.Add(change.Module ?? "");
            item.ForeColor = change.ChangeType switch
            {
                ChangeType.BugFix => Color.DarkGreen,
                ChangeType.Breaking => Color.DarkRed,
                ChangeType.Security => Color.DarkBlue,
                _ => Color.Black
            };
            _lvChanges.Items.Add(item);
        }
    }

    private async Task AddVersionAsync()
    {
        using var form = new AddVersionForm();
        if (form.ShowDialog() == DialogResult.OK)
            await LoadDataAsync();
    }
}

public class AddVersionForm : Form
{
    private TextBox _txtVersion = null!, _txtTitle = null!, _txtNotes = null!;
    private CheckBox _chkCritical = null!;
    private DataGridView _grdChanges = null!;
    private Button _btnKaydet = null!, _btnIptal = null!;

    public AddVersionForm()
    {
        this.Text = "Yeni Sürüm Ekle";
        this.Size = new Size(700, 580);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9.5f);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(15) };
        int y = 15;

        void AddLbl(string text, Control ctrl)
        {
            scroll.Controls.Add(new Label { Text = text, Location = new Point(15, y + 3), Width = 110, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            ctrl.Location = new Point(130, y); ctrl.Width = 520; scroll.Controls.Add(ctrl); y += 38;
        }

        _txtVersion = new TextBox { BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "örn: 1.1.0" };
        _txtTitle = new TextBox { BorderStyle = BorderStyle.FixedSingle };
        _txtNotes = new TextBox { BorderStyle = BorderStyle.FixedSingle, Multiline = true, Height = 80 };
        _chkCritical = new CheckBox { Text = "Kritik Güncelleme" };

        AddLbl("Sürüm *", _txtVersion);
        AddLbl("Başlık *", _txtTitle);
        AddLbl("Sürüm Notları", _txtNotes);
        y += 50; // extra for multiline

        scroll.Controls.Add(new Label { Text = "Değişiklikler:", Location = new Point(15, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true });
        y += 22;

        _grdChanges = new DataGridView { Location = new Point(15, y), Width = 635, Height = 160, AllowUserToAddRows = true };
        var colType = new DataGridViewComboBoxColumn { HeaderText = "Tür", Width = 120, Name = "colType" };
        colType.Items.AddRange("Yeni Özellik", "Hata Düzeltme", "İyileştirme", "Kırıcı", "Güvenlik");
        _grdChanges.Columns.Add(colType);
        _grdChanges.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Açıklama", Width = 380, Name = "colDesc" });
        _grdChanges.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Modül", Width = 100, Name = "colModule" });
        scroll.Controls.Add(_grdChanges);
        y += 175;

        scroll.Controls.Add(_chkCritical);
        _chkCritical.Location = new Point(15, y);

        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.FromArgb(240, 242, 248) };
        _btnKaydet = new Button { Text = "💾 Kaydet", Location = new Point(490, 12), Width = 100, Height = 34,
            BackColor = Color.FromArgb(0, 84, 166), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnKaydet.FlatAppearance.BorderSize = 0;
        _btnKaydet.Click += async (s, e) => await SaveAsync();
        _btnIptal = new Button { Text = "İptal", Location = new Point(600, 12), Width = 75, Height = 34,
            BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnIptal.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.AddRange(new Control[] { _btnKaydet, _btnIptal });
        this.Controls.Add(scroll); this.Controls.Add(btnPanel);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtVersion.Text)) { MessageBox.Show("Sürüm zorunludur."); return; }
        if (string.IsNullOrWhiteSpace(_txtTitle.Text)) { MessageBox.Show("Başlık zorunludur."); return; }

        var changes = new List<(ChangeType, string, string?)>();
        foreach (DataGridViewRow row in _grdChanges.Rows)
        {
            if (row.IsNewRow) continue;
            var typeStr = row.Cells["colType"].Value?.ToString() ?? "";
            var desc = row.Cells["colDesc"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(desc)) continue;
            var changeType = typeStr switch
            {
                "Yeni Özellik" => ChangeType.NewFeature,
                "Hata Düzeltme" => ChangeType.BugFix,
                "İyileştirme" => ChangeType.Improvement,
                "Kırıcı" => ChangeType.Breaking,
                "Güvenlik" => ChangeType.Security,
                _ => ChangeType.NewFeature
            };
            changes.Add((changeType, desc, row.Cells["colModule"].Value?.ToString()));
        }

        try
        {
            using var db = DbContextFactory.Create();
            var service = new VersionService(db);
            await service.AddVersionAsync(_txtVersion.Text.Trim(), _txtTitle.Text.Trim(),
                _txtNotes.Text.Trim(), _chkCritical.Checked,
                AppState.CurrentUser?.Username ?? "Admin", changes);
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
    }
}
