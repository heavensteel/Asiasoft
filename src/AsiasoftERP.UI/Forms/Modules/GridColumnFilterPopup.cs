using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class GridColumnFilterPopup : Form
{
    public string ColumnName { get; }
    public string ColumnHeaderText { get; }

    private readonly List<string> _allDistinctValues;
    private readonly HashSet<string> _initiallySelectedValues;
    private CheckedListBox _chkList = null!;
    private TextBox _txtSearch = null!;
    private CheckBox _chkSelectAll = null!;

    public event Action<string, HashSet<string>?>? FilterApplied;
    public event Action<string, SortOrder>? SortRequested;
    public event Action<string>? OpenParametersRequested;

    public GridColumnFilterPopup(string columnName, string columnHeaderText, List<string> distinctValues, HashSet<string>? currentSelectedValues)
    {
        ColumnName = columnName;
        ColumnHeaderText = columnHeaderText;
        _allDistinctValues = distinctValues.OrderBy(v => v).ToList();
        _initiallySelectedValues = currentSelectedValues != null
            ? new HashSet<string>(currentSelectedValues)
            : new HashSet<string>(_allDistinctValues);

        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.ShowInTaskbar = false;
        this.Size = new Size(260, 350);
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 9f);
        this.DoubleBuffered = true;

        InitializeComponents();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    private void InitializeComponents()
    {
        // Outer border
        this.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate700, 1.5f);
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        };

        // 1. Header (Slate 800)
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = ModernColors.Slate900,
            Padding = new Padding(10, 0, 8, 0)
        };

        var lblTitle = new Label
        {
            Text = $"📌 {ColumnHeaderText}",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(24, 24),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => this.Close();

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, btnClose });

        // 2. Sort Bar (A-Z, Z-A)
        var pnlSort = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(6, 4, 6, 4)
        };

        var btnSortAsc = new Button
        {
            Text = "🔼 A → Z Sırala",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(116, 24),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand
        };
        btnSortAsc.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnSortAsc.Click += (_, _) =>
        {
            SortRequested?.Invoke(ColumnName, SortOrder.Ascending);
            this.Close();
        };

        var btnSortDesc = new Button
        {
            Text = "🔽 Z → A Sırala",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(116, 24),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnSortDesc.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnSortDesc.Click += (_, _) =>
        {
            SortRequested?.Invoke(ColumnName, SortOrder.Descending);
            this.Close();
        };

        pnlSort.Controls.AddRange(new Control[] { btnSortAsc, btnSortDesc });

        // 3. Search Box
        var pnlSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.White,
            Padding = new Padding(8, 5, 8, 4)
        };

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "🔍 Filtrele...",
            Font = new Font("Segoe UI", 8.5f)
        };
        _txtSearch.TextChanged += (_, _) => FilterListItems();
        pnlSearch.Controls.Add(_txtSearch);

        // 4. Select All Checkbox
        var pnlSelectAll = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = Color.White,
            Padding = new Padding(8, 2, 8, 2)
        };

        _chkSelectAll = new CheckBox
        {
            Text = "(Tümünü Seç)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            Checked = _initiallySelectedValues.Count == _allDistinctValues.Count
        };
        _chkSelectAll.CheckedChanged += (_, _) =>
        {
            bool check = _chkSelectAll.Checked;
            for (int i = 0; i < _chkList.Items.Count; i++)
            {
                _chkList.SetItemChecked(i, check);
            }
        };
        pnlSelectAll.Controls.Add(_chkSelectAll);

        // 5. Bottom Action Buttons
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(8, 6, 8, 6)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        var btnClear = new Button
        {
            Text = "Temizle",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Rose600,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(72, 28),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand
        };
        btnClear.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnClear.Click += (_, _) =>
        {
            FilterApplied?.Invoke(ColumnName, null); // Clear filter
            this.Close();
        };

        var btnParams = new Button
        {
            Text = "⚙️",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate700,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(32, 28),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0)
        };
        btnParams.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnParams.Click += (_, _) =>
        {
            OpenParametersRequested?.Invoke(ColumnName);
            this.Close();
        };

        var btnApply = new Button
        {
            Text = "Uygula",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Emerald500,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(80, 28),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnApply.FlatAppearance.BorderSize = 0;
        btnApply.Click += (_, _) =>
        {
            var selected = new HashSet<string>();
            foreach (var item in _chkList.CheckedItems)
            {
                selected.Add(item.ToString() ?? "");
            }

            // If all items are selected, it means no filter is applied
            if (selected.Count == _allDistinctValues.Count)
            {
                FilterApplied?.Invoke(ColumnName, null);
            }
            else
            {
                FilterApplied?.Invoke(ColumnName, selected);
            }
            this.Close();
        };

        pnlBottom.Controls.AddRange(new Control[] { btnClear, btnParams, btnApply });

        // 6. Distinct Values CheckedListBox (Fill Middle)
        _chkList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            CheckOnClick = true,
            Font = new Font("Segoe UI", 8.5f),
            IntegralHeight = false
        };

        PopulateList(_allDistinctValues);

        this.Controls.Add(_chkList);
        this.Controls.Add(pnlSelectAll);
        this.Controls.Add(pnlSearch);
        this.Controls.Add(pnlSort);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlBottom);

        // Close when clicking outside
        this.Deactivate += (_, _) => this.Close();
    }

    private void PopulateList(IEnumerable<string> items)
    {
        _chkList.Items.Clear();
        foreach (var val in items)
        {
            string display = string.IsNullOrEmpty(val) ? "(Boş)" : val;
            bool isChecked = _initiallySelectedValues.Contains(val);
            _chkList.Items.Add(display, isChecked);
        }
    }

    private void FilterListItems()
    {
        string q = _txtSearch.Text.Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));
        if (string.IsNullOrEmpty(q))
        {
            PopulateList(_allDistinctValues);
        }
        else
        {
            var filtered = _allDistinctValues.Where(v => v.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(q)).ToList();
            PopulateList(filtered);
        }
    }
}
