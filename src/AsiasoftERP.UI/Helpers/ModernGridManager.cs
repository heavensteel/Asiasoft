using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Forms.Modules;
using Dapper;

namespace AsiasoftERP.UI.Helpers;

public class ModernGridManager
{
    private readonly DataGridView _grid;
    private readonly string _formName;
    private readonly string _gridName;
    private readonly int _userId;

    // Filters state
    private readonly Dictionary<string, HashSet<string>> _columnFilters = new();
    private string _globalSearchTerm = string.Empty;

    // Active Filter Bar
    private FlowLayoutPanel? _pnlActiveFiltersBar;

    public ModernGridManager(DataGridView grid, string formName, string? gridName = null)
    {
        _grid = grid;
        _formName = formName;
        _gridName = string.IsNullOrWhiteSpace(gridName) ? grid.Name : gridName;
        _userId = (int)(AppState.ActiveUser?.KullaniciKodu ?? 100);

        ConfigureGridDefaults();
        HookGridEvents();

        // Load saved layout in background
        _ = LoadSavedLayoutAsync();
    }

    private void ConfigureGridDefaults()
    {
        // 1. Allow dragging and dropping column headers to reorder
        _grid.AllowUserToOrderColumns = true;
        _grid.AllowUserToResizeColumns = true;

        // Make double buffered
        var type = _grid.GetType();
        var pi = type.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        pi?.SetValue(_grid, true, null);
    }

    private void HookGridEvents()
    {
        _grid.CellMouseDown += OnGridCellMouseDown;
        _grid.CellPainting += OnGridCellPainting;
        _grid.ColumnWidthChanged += (_, _) => DebounceSaveLayout();
        _grid.ColumnDisplayIndexChanged += (_, _) => DebounceSaveLayout();
    }

    private System.Windows.Forms.Timer? _debounceSaveTimer;
    private void DebounceSaveLayout()
    {
        if (_debounceSaveTimer == null)
        {
            _debounceSaveTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _debounceSaveTimer.Tick += async (_, _) =>
            {
                _debounceSaveTimer.Stop();
                await SaveLayoutAsync();
            };
        }
        _debounceSaveTimer.Stop();
        _debounceSaveTimer.Start();
    }

    private void OnGridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex == -1 && e.ColumnIndex >= 0) // Header click
        {
            var col = _grid.Columns[e.ColumnIndex];

            // Right-click: Open Full Header Context Menu
            if (e.Button == MouseButtons.Right)
            {
                ShowHeaderContextMenu(col, e.Location);
            }
            // Left-click on filter icon area (rightmost 24px of header)
            else if (e.Button == MouseButtons.Left)
            {
                var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
                if (e.X >= cellRect.Width - 28)
                {
                    ShowColumnFilterPopup(col);
                }
            }
        }
    }

    private void OnGridCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex == -1 && e.ColumnIndex >= 0)
        {
            var col = _grid.Columns[e.ColumnIndex];
            string colKey = col.DataPropertyName ?? col.Name;
            bool isFiltered = _columnFilters.ContainsKey(colKey);

            e.Paint(e.ClipBounds, DataGridViewPaintParts.All);

            // Draw small funnel / filter icon on header right
            int iconX = e.CellBounds.Right - 22;
            int iconY = e.CellBounds.Top + (e.CellBounds.Height - 14) / 2;

            using (var brush = new SolidBrush(isFiltered ? ModernColors.Emerald500 : Color.FromArgb(148, 163, 184)))
            {
                // Draw a sleek filter funnel icon (▼ or funnel polygon)
                Point[] funnel = new Point[]
                {
                    new(iconX, iconY + 2),
                    new(iconX + 12, iconY + 2),
                    new(iconX + 8, iconY + 7),
                    new(iconX + 8, iconY + 12),
                    new(iconX + 5, iconY + 10),
                    new(iconX + 5, iconY + 7)
                };
                e.Graphics?.FillPolygon(brush, funnel);
            }

            e.Handled = true;
        }
    }

    public void ShowColumnFilterPopup(DataGridViewColumn col)
    {
        string colKey = col.DataPropertyName ?? col.Name;

        // Extract distinct values from grid
        var distinctValues = new HashSet<string>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var val = row.Cells[col.Index].Value?.ToString() ?? "";
            distinctValues.Add(val);
        }

        _columnFilters.TryGetValue(colKey, out var currentSelected);

        var popup = new GridColumnFilterPopup(colKey, col.HeaderText, distinctValues.ToList(), currentSelected);

        popup.FilterApplied += (cName, selectedVals) =>
        {
            if (selectedVals == null || selectedVals.Count == distinctValues.Count)
            {
                _columnFilters.Remove(cName);
            }
            else
            {
                _columnFilters[cName] = selectedVals;
            }
            ApplyAllFilters();
            _grid.Invalidate();
        };

        popup.SortRequested += (cName, order) =>
        {
            if (order == SortOrder.Ascending)
                _grid.Sort(col, System.ComponentModel.ListSortDirection.Ascending);
            else if (order == SortOrder.Descending)
                _grid.Sort(col, System.ComponentModel.ListSortDirection.Descending);
        };

        popup.OpenParametersRequested += (_) => ShowParametersDialog();

        // Position popup right below header cell
        var cellRect = _grid.GetCellDisplayRectangle(col.Index, -1, true);
        var screenPoint = _grid.PointToScreen(new Point(cellRect.Left, cellRect.Bottom));

        // Adjust if overflowing screen width
        if (screenPoint.X + popup.Width > Screen.PrimaryScreen?.WorkingArea.Right)
        {
            screenPoint.X = Screen.PrimaryScreen.WorkingArea.Right - popup.Width - 10;
        }

        popup.Location = screenPoint;
        popup.Show();
    }

    private void ShowHeaderContextMenu(DataGridViewColumn col, Point clickLoc)
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add($"🔍 '{col.HeaderText}' Sütununu Filtrele...", null, (_, _) => ShowColumnFilterPopup(col));
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("🔼 A'dan Z'ye Sırala", null, (_, _) =>
            _grid.Sort(col, System.ComponentModel.ListSortDirection.Ascending));

        menu.Items.Add("🔽 Z'den A'ya Sırala", null, (_, _) =>
            _grid.Sort(col, System.ComponentModel.ListSortDirection.Descending));

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("⚙️ Tablo Parametreleri (Sütunlar ve Görünüm)...", null, (_, _) => ShowParametersDialog());
        menu.Items.Add("📏 Bu Sütunu Otomatik Sığdır", null, (_, _) =>
        {
            col.Width = col.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true);
        });

        menu.Items.Add("💾 Bu Düzeni Kaydet (Kullanıcıya Özel)", null, async (_, _) =>
        {
            await SaveLayoutAsync();
            MessageBox.Show("Tablo yerleşimi ve sütun sıralamanız kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });

        menu.Items.Add("🔄 Varsayılan Düzene Dön", null, async (_, _) =>
        {
            await ResetLayoutAsync();
        });

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("📊 Excel'e Aktar (CSV)", null, (_, _) => ExportToCsv());

        menu.Show(_grid, _grid.PointToClient(Cursor.Position));
    }

    public void ShowParametersDialog()
    {
        using var dlg = new GridParametersDialog(_grid, _formName, _gridName);
        if (dlg.ShowDialog(_grid.FindForm()) == DialogResult.OK)
        {
            _ = LoadSavedLayoutAsync();
        }
    }

    public void SetGlobalSearch(string search)
    {
        _globalSearchTerm = search.Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));
        ApplyAllFilters();
    }

    public void ClearAllFilters()
    {
        _columnFilters.Clear();
        _globalSearchTerm = string.Empty;
        ApplyAllFilters();
        _grid.Invalidate();
    }

    private void ApplyAllFilters()
    {
        // For standard WinForms DataGridView rows:
        _grid.SuspendLayout();
        var cm = _grid.DataSource != null ? _grid.BindingContext?[_grid.DataSource] as CurrencyManager : null;
        cm?.SuspendBinding();

        try
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;

                bool isVisible = true;

                // 1. Column-specific filters
                foreach (var kvp in _columnFilters)
                {
                    var col = _grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => (c.DataPropertyName ?? c.Name) == kvp.Key);
                    if (col != null)
                    {
                        string cellVal = row.Cells[col.Index].Value?.ToString() ?? "";
                        if (!kvp.Value.Contains(cellVal))
                        {
                            isVisible = false;
                            break;
                        }
                    }
                }

                // 2. Global search term across all visible columns
                if (isVisible && !string.IsNullOrEmpty(_globalSearchTerm))
                {
                    bool found = false;
                    foreach (DataGridViewColumn col in _grid.Columns)
                    {
                        if (col.Visible)
                        {
                            string val = row.Cells[col.Index].Value?.ToString()?.ToLower(new System.Globalization.CultureInfo("tr-TR")) ?? "";
                            if (val.Contains(_globalSearchTerm))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    isVisible = found;
                }

                if (row.Visible != isVisible)
                {
                    row.Visible = isVisible;
                }
            }
        }
        catch { }
        finally
        {
            cm?.ResumeBinding();
            _grid.ResumeLayout();
        }

        UpdateActiveFiltersBar();
    }

    public void AttachActiveFiltersBar(FlowLayoutPanel bar)
    {
        _pnlActiveFiltersBar = bar;
        UpdateActiveFiltersBar();
    }

    private void UpdateActiveFiltersBar()
    {
        if (_pnlActiveFiltersBar == null) return;

        _pnlActiveFiltersBar.SuspendLayout();
        _pnlActiveFiltersBar.Controls.Clear();

        if (_columnFilters.Count == 0 && string.IsNullOrEmpty(_globalSearchTerm))
        {
            _pnlActiveFiltersBar.Visible = false;
            _pnlActiveFiltersBar.ResumeLayout();
            return;
        }

        _pnlActiveFiltersBar.Visible = true;

        var lblIcon = new Label
        {
            Text = "⚡ Aktif Filtreler:",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            AutoSize = true,
            Margin = new Padding(4, 6, 6, 4)
        };
        _pnlActiveFiltersBar.Controls.Add(lblIcon);

        foreach (var kvp in _columnFilters)
        {
            var col = _grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => (c.DataPropertyName ?? c.Name) == kvp.Key);
            string header = col?.HeaderText ?? kvp.Key;
            string preview = string.Join(", ", kvp.Value.Take(2)) + (kvp.Value.Count > 2 ? $" (+{kvp.Value.Count - 2})" : "");

            var chip = new Button
            {
                Text = $"{header}: {preview}  ✕",
                Font = new Font("Segoe UI", 8.5f),
                BackColor = ModernColors.Emerald50,
                ForeColor = ModernColors.Emerald600,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Height = 26,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 2, 4, 2)
            };
            chip.FlatAppearance.BorderColor = ModernColors.Emerald500;
            chip.Click += (_, _) =>
            {
                _columnFilters.Remove(kvp.Key);
                ApplyAllFilters();
                _grid.Invalidate();
            };
            _pnlActiveFiltersBar.Controls.Add(chip);
        }

        // Clear All Button
        var btnClearAll = new Button
        {
            Text = "Filtreleri Temizle ✕",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            BackColor = ModernColors.Rose600,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Height = 26,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 2, 4, 2)
        };
        btnClearAll.FlatAppearance.BorderSize = 0;
        btnClearAll.Click += (_, _) => ClearAllFilters();
        _pnlActiveFiltersBar.Controls.Add(btnClearAll);

        _pnlActiveFiltersBar.ResumeLayout();
    }

    public async Task SaveLayoutAsync()
    {
        try
        {
            var states = _grid.Columns.Cast<DataGridViewColumn>()
                .OrderBy(c => c.DisplayIndex)
                .Select(c => new GridColumnState
                {
                    FieldName = c.DataPropertyName ?? c.Name,
                    Index = c.DisplayIndex,
                    Width = c.Width,
                    Visible = c.Visible,
                    AlignmentHorz = c.DefaultCellStyle.Alignment switch
                    {
                        DataGridViewContentAlignment.MiddleRight => 1,
                        DataGridViewContentAlignment.MiddleCenter => 2,
                        _ => 0
                    }
                }).ToList();

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(states);

            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO erp.grid_layouts (UserId, FormName, GridName, LayoutJson, UpdatedAt)
                VALUES (@UserId, @FormName, @GridName, @LayoutJson, NOW())
                ON DUPLICATE KEY UPDATE LayoutJson = @LayoutJson, UpdatedAt = NOW();";

            await conn.ExecuteAsync(sql, new
            {
                UserId = _userId,
                FormName = _formName,
                GridName = _gridName,
                LayoutJson = json
            });

            NotificationService.NotifySuccess("Tablo Görünümü Kaydedildi", $"'{_formName}' sütun ve parametre yerleşimi profilinize kaydedildi.");
        }
        catch { }
    }

    public async Task LoadSavedLayoutAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT LayoutJson FROM erp.grid_layouts
                WHERE UserId = @UserId AND FormName = @FormName AND GridName = @GridName LIMIT 1;";

            string? json = await conn.QueryFirstOrDefaultAsync<string>(sql, new
            {
                UserId = _userId,
                FormName = _formName,
                GridName = _gridName
            });

            if (string.IsNullOrEmpty(json)) return;

            var states = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GridColumnState>>(json);
            if (states == null || states.Count == 0) return;

            _grid.SuspendLayout();

            // Match columns by FieldName
            var matchedColumns = new List<(DataGridViewColumn col, GridColumnState state)>();
            foreach (var state in states)
            {
                var col = _grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => (c.DataPropertyName ?? c.Name) == state.FieldName);
                if (col != null)
                {
                    matchedColumns.Add((col, state));
                }
            }

            // Apply widths, visibility, alignments
            foreach (var (col, state) in matchedColumns)
            {
                col.Visible = state.Visible;
                col.Width = state.Width;
                col.DefaultCellStyle.Alignment = state.AlignmentHorz switch
                {
                    1 => DataGridViewContentAlignment.MiddleRight,
                    2 => DataGridViewContentAlignment.MiddleCenter,
                    _ => DataGridViewContentAlignment.MiddleLeft
                };
            }

            // Apply DisplayIndex sorted in ascending order of saved index
            var sortedByTargetIndex = matchedColumns.OrderBy(m => m.state.Index).ToList();
            for (int i = 0; i < sortedByTargetIndex.Count; i++)
            {
                if (i < _grid.Columns.Count)
                {
                    try
                    {
                        sortedByTargetIndex[i].col.DisplayIndex = i;
                    }
                    catch { }
                }
            }
        }
        catch { }
        finally
        {
            _grid.ResumeLayout();
        }
    }

    public async Task ResetLayoutAsync()
    {
        try
        {
            using var conn = DbConnectionConfig.CreateConnection();
            await conn.OpenAsync();
            await conn.ExecuteAsync("DELETE FROM erp.grid_layouts WHERE UserId = @UserId AND FormName = @FormName AND GridName = @GridName;",
                                    new { UserId = _userId, FormName = _formName, GridName = _gridName });
            NotificationService.NotifyInfo("Tablo Sıfırlandı", $"'{_formName}' tablosu varsayılan fabrika görünümüne getirildi.");
            MessageBox.Show("Tablo yerleşimi sıfırlandı. Form yeniden açıldığında varsayılan düzen yüklenecektir.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch { }
    }

    public void ExportToCsv()
    {
        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Dosyası (*.csv)|*.csv|Excel Dosyası (*.xls)|*.xls",
                FileName = $"{_formName}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog(_grid.FindForm()) == DialogResult.OK)
            {
                var sb = new StringBuilder();

                var visibleCols = _grid.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible)
                    .OrderBy(c => c.DisplayIndex)
                    .ToList();

                sb.AppendLine(string.Join(";", visibleCols.Select(c => $"\"{c.HeaderText}\"")));

                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow || !row.Visible) continue;
                    var cells = visibleCols.Select(c =>
                    {
                        var val = row.Cells[c.Index].Value?.ToString() ?? "";
                        val = val.Replace("\"", "\"\"");
                        return $"\"{val}\"";
                    });
                    sb.AppendLine(string.Join(";", cells));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                NotificationService.NotifySuccess("Excel / CSV Aktarıldı", $"'{Path.GetFileName(sfd.FileName)}' başarıyla kaydedildi.");
                MessageBox.Show($"Tablo verileri başarıyla aktarıldı:\n{sfd.FileName}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            NotificationService.NotifyError("Aktarım Hatası", ex.Message);
            MessageBox.Show($"Dışa aktarma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
