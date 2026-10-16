using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.Data.Services;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Base class for all list/grid forms with grid layout persistence and modern executive styling
/// </summary>
public abstract class BaseListForm : Form
{
    protected DataGridView Grid = null!;
    protected Panel ToolbarPanel = null!;
    protected Panel FilterPanel = null!;
    protected Label LblRecordCount = null!;
    protected TextBox TxtSearch = null!;
    protected Button BtnRefresh = null!;
    protected Button BtnExport = null!;
    protected Button BtnNew = null!;
    protected Button BtnEdit = null!;
    protected Button BtnDelete = null!;
    protected Button BtnSaveLayout = null!;
    protected ModernGridManager? GridManager;
    protected FlowLayoutPanel? ActiveFiltersBar;

    protected abstract string ModuleName { get; }
    protected abstract string FormTitle { get; }

    protected BaseListForm()
    {
        this.DoubleBuffered = true;
        InitializeBaseComponents();
    }

    private void InitializeBaseComponents()
    {
        this.Text = FormTitle;
        this.Size = new Size(1100, 650);
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        // Toolbar (Sleek Clean White Bar with subtle bottom border)
        ToolbarPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.White,
            Padding = new Padding(12, 7, 12, 7)
        };
        ToolbarPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
            e.Graphics.DrawLine(pen, 0, ToolbarPanel.Height - 1, ToolbarPanel.Width, ToolbarPanel.Height - 1);
        };

        int x = 12;

        if (AppState.CanEdit(ModuleName))
        {
            BtnNew = CreateToolButton("+ Yeni Kayıt", Color.FromArgb(4, 120, 87), Color.FromArgb(236, 253, 245), Color.FromArgb(167, 243, 208), x, 105);
            x += 112;
            BtnEdit = CreateToolButton("Düzenle", Color.FromArgb(51, 65, 85), Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), x, 85);
            x += 92;
            BtnDelete = CreateToolButton("Sil", Color.FromArgb(190, 18, 60), Color.FromArgb(255, 241, 242), Color.FromArgb(254, 205, 211), x, 65);
            x += 72;
            ToolbarPanel.Controls.AddRange(new Control[] { BtnNew, BtnEdit, BtnDelete });
        }

        BtnRefresh = CreateToolButton("Yenile", Color.FromArgb(51, 65, 85), Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), x, 75);
        x += 82;
        BtnExport = CreateToolButton("Excel Aktar", Color.FromArgb(4, 120, 87), Color.FromArgb(236, 253, 245), Color.FromArgb(167, 243, 208), x, 100);
        x += 107;
        BtnSaveLayout = CreateToolButton("⚙️ Parametreler", Color.FromArgb(71, 85, 105), Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), x, 125);

        ToolbarPanel.Controls.AddRange(new Control[] { BtnRefresh, BtnExport, BtnSaveLayout });

        // Search container (Right-anchored)
        var pnlSearch = new Panel
        {
            Size = new Size(240, 32),
            Location = new Point(this.Width - 260, 7),
            BackColor = Color.FromArgb(248, 250, 252),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
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

        TxtSearch = new TextBox
        {
            Location = new Point(28, 6),
            Width = 205,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Listede ara..."
        };
        TxtSearch.TextChanged += (s, e) => OnSearchChanged(TxtSearch.Text);

        pnlSearch.Controls.AddRange(new Control[] { lblSearchIcon, TxtSearch });
        ToolbarPanel.Controls.Add(pnlSearch);

        // Grid
        Grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White
        };
        ModernUiHelper.ApplyModernGridStyle(Grid);
        Grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OnRowDoubleClick(e.RowIndex); };
        Grid.SelectionChanged += (s, e) => OnSelectionChanged();

        // Active filters banner strip
        ActiveFiltersBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(8, 4, 8, 4),
            Visible = false
        };
        ActiveFiltersBar.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
            e.Graphics.DrawLine(pen, 0, ActiveFiltersBar.Height - 1, ActiveFiltersBar.Width, ActiveFiltersBar.Height - 1);
        };

        GridManager = new ModernGridManager(Grid, this.GetType().Name);
        GridManager.AttachActiveFiltersBar(ActiveFiltersBar);

        // Status bar
        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 5, 12, 5)
        };
        statusPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
            e.Graphics.DrawLine(pen, 0, 0, statusPanel.Width, 0);
        };

        LblRecordCount = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(12, 6)
        };
        statusPanel.Controls.Add(LblRecordCount);

        // Add controls in standard Dock order: Fill first, Bottom second, Top third
        this.Controls.Add(Grid);
        this.Controls.Add(ActiveFiltersBar);
        this.Controls.Add(statusPanel);
        this.Controls.Add(ToolbarPanel);

        // Wire events
        if (BtnNew != null) BtnNew.Click += (s, e) => OnNewRecord();
        if (BtnEdit != null) BtnEdit.Click += (s, e) => OnEditRecord();
        if (BtnDelete != null) BtnDelete.Click += (s, e) => OnDeleteRecord();
        BtnRefresh.Click += async (s, e) => await LoadDataAsync();
        BtnExport.Click += (s, e) => ExportToExcel();
        BtnSaveLayout.Click += (s, e) => GridManager?.ShowParametersDialog();

        this.Load += async (s, e) =>
        {
            await LoadDataAsync();
        };
    }

    private Button CreateToolButton(string text, Color textColor, Color backColor, Color borderColor, int x, int width)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 7),
            Height = 30,
            Width = width,
            BackColor = backColor,
            ForeColor = textColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = borderColor;
        btn.FlatAppearance.BorderSize = 1;
        btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(241, 245, 249);
        btn.MouseLeave += (s, e) => btn.BackColor = backColor;
        return btn;
    }

    protected void UpdateRecordCount(int count)
    {
        LblRecordCount.Text = $"Toplam: {count:N0} kayıt";
    }

    private async Task LoadGridLayoutAsync()
    {
        if (AppState.CurrentUser == null) return;
        await GridLayoutHelper.ApplyToDataGridViewAsync(Grid, AppState.CurrentUser.Id, this.Name);
    }

    private async Task SaveGridLayoutAsync()
    {
        if (AppState.CurrentUser == null) return;
        await GridLayoutHelper.SaveFromDataGridViewAsync(Grid, AppState.CurrentUser.Id, this.Name);
        MessageBox.Show("Grid düzeni kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected abstract Task LoadDataAsync();
    public async Task RefreshDataAsync() => await LoadDataAsync();
    protected virtual void OnSearchChanged(string searchText)
    {
        GridManager?.SetGlobalSearch(searchText);
    }
    protected virtual void OnRowDoubleClick(int rowIndex) { }
    protected virtual void OnSelectionChanged() { }
    protected virtual void OnNewRecord() { }
    protected virtual void OnEditRecord() { }
    protected virtual void OnDeleteRecord() { }

    protected virtual void ExportToExcel()
    {
        GridManager?.ExportToCsv();
    }
}
