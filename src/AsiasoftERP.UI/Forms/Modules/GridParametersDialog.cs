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
using AsiasoftERP.UI.Helpers;
using Dapper;

namespace AsiasoftERP.UI.Forms.Modules;

public class GridParametersDialog : Form
{
    private readonly DataGridView _grid;
    private readonly string _formName;
    private readonly string _gridName;
    private readonly int _userId;

    private ListView _lvColumns = null!;
    private NumericUpDown _numWidth = null!;
    private ComboBox _cboAlignment = null!;
    private CheckBox _chkVisible = null!;

    // Appearance
    private RadioButton _rbRowCompact = null!;
    private RadioButton _rbRowStandard = null!;
    private RadioButton _rbRowLarge = null!;
    private CheckBox _chkZebra = null!;
    private CheckBox _chkGridLines = null!;

    private class ColumnParamItem
    {
        public DataGridViewColumn Column { get; set; } = null!;
        public int DisplayIndex { get; set; }
        public int Width { get; set; }
        public bool Visible { get; set; }
        public DataGridViewContentAlignment Alignment { get; set; }
    }

    private readonly List<ColumnParamItem> _items = new();

    public GridParametersDialog(DataGridView grid, string formName, string gridName)
    {
        _grid = grid;
        _formName = formName;
        _gridName = string.IsNullOrWhiteSpace(gridName) ? grid.Name : gridName;
        _userId = (int)(AppState.ActiveUser?.KullaniciKodu ?? 100);

        this.Text = $"Tablo Parametreleri ve Sütun Yerleşimi — [{_formName}]";
        this.Size = new Size(820, 620);
        this.MinimumSize = new Size(720, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = ModernColors.Slate100;
        this.Font = new Font("Segoe UI", 9.5f);
        this.DoubleBuffered = true;

        CaptureCurrentGridState();
        InitializeComponents();
    }

    private void CaptureCurrentGridState()
    {
        _items.Clear();
        foreach (DataGridViewColumn col in _grid.Columns)
        {
            _items.Add(new ColumnParamItem
            {
                Column = col,
                DisplayIndex = col.DisplayIndex,
                Width = col.Width,
                Visible = col.Visible,
                Alignment = col.DefaultCellStyle.Alignment
            });
        }
    }

    private void InitializeComponents()
    {
        // 1. TOP HEADER BANNER
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ModernColors.Slate900,
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblTitle = new Label
        {
            Text = "⚙️  Tablo Parametreleri ve Sütun Düzeni",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(16, 10),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Sütun sıralamasını sürükleyip bırakabilir, görünürlükleri değiştirebilir ve düzeninizi kaydedebilirsiniz.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(18, 36),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // 2. MAIN TAB CONTROL
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Point(14, 6)
        };

        // Tab 1: Sütun Seçimi & Sıralama
        var tabColumns = new TabPage("  📋 Sütun Seçimi & Sıralama  ")
        {
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        BuildColumnsTab(tabColumns);

        // Tab 2: Görünüm & Izgara Parametreleri
        var tabAppearance = new TabPage("  🎨 Görünüm & Satır Ayarları  ")
        {
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        BuildAppearanceTab(tabAppearance);

        tabControl.TabPages.Add(tabColumns);
        tabControl.TabPages.Add(tabAppearance);

        // 3. BOTTOM ACTION BAR
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(16, 10, 16, 10)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        var btnReset = new Button
        {
            Text = "🔄  Varsayılana Dön",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate700,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(140, 36),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand
        };
        btnReset.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnReset.Click += async (_, _) => await ResetToDefaultAsync();

        var btnExportExcel = new Button
        {
            Text = "📊  Excel'e Aktar",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Emerald600,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 36),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand,
            Margin = new Padding(10, 0, 0, 0)
        };
        btnExportExcel.FlatAppearance.BorderColor = ModernColors.Emerald500;
        btnExportExcel.Click += (_, _) => ExportToCsv();

        var btnCancel = new Button
        {
            Text = "İptal",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 36),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        var btnSave = new Button
        {
            Text = "💾  Kaydet ve Uygula",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Emerald500,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 36),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (_, _) =>
        {
            ApplySettingsToGrid();
            await SaveLayoutToDatabaseAsync();
            this.DialogResult = DialogResult.OK;
        };

        pnlBottom.Controls.AddRange(new Control[] { btnSave, btnCancel, btnReset, btnExportExcel });

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHeader);
    }

    private void BuildColumnsTab(TabPage tab)
    {
        var pnlSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 480,
            SplitterWidth = 6,
            BackColor = ModernColors.Slate200
        };

        pnlSplit.Panel1.BackColor = Color.White;
        pnlSplit.Panel2.BackColor = Color.White;
        pnlSplit.Panel2.Padding = new Padding(16, 8, 16, 8);

        // LEFT: Column ListView with Checkboxes
        _lvColumns = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            CheckBoxes = true,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        _lvColumns.Columns.Add("Sütun Başlığı", 210);
        _lvColumns.Columns.Add("Alan Adı", 120);
        _lvColumns.Columns.Add("Genişlik", 70);
        _lvColumns.Columns.Add("Hizalama", 70);

        _lvColumns.ItemChecked += (_, e) =>
        {
            if (e.Item.Tag is ColumnParamItem item)
            {
                item.Visible = e.Item.Checked;
            }
        };

        _lvColumns.SelectedIndexChanged += (_, _) => OnSelectedColumnChanged();

        var pnlListToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(4)
        };

        var btnUp = new Button
        {
            Text = "🔼  Yukarı Taşı",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 28),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand
        };
        btnUp.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnUp.Click += (_, _) => MoveSelectedItem(-1);

        var btnDown = new Button
        {
            Text = "🔽  Aşağı Taşı",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 28),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0)
        };
        btnDown.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnDown.Click += (_, _) => MoveSelectedItem(1);

        var btnSelectAll = new Button
        {
            Text = "Tümünü Seç",
            Font = new Font("Segoe UI", 8f),
            Size = new Size(90, 28),
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSelectAll.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnSelectAll.Click += (_, _) =>
        {
            foreach (ListViewItem li in _lvColumns.Items) li.Checked = true;
        };

        pnlListToolbar.Controls.AddRange(new Control[] { btnUp, btnDown, btnSelectAll });

        pnlSplit.Panel1.Controls.Add(_lvColumns);
        pnlSplit.Panel1.Controls.Add(pnlListToolbar);

        // RIGHT: Selected Column Properties
        var lblPropHdr = new Label
        {
            Text = "Sütun Özellikleri",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Dock = DockStyle.Top,
            Height = 30
        };

        _chkVisible = new CheckBox
        {
            Text = "Bu Sütunu Tabloda Göster",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(16, 42),
            AutoSize = true
        };
        _chkVisible.CheckedChanged += (_, _) =>
        {
            if (_lvColumns.SelectedItems.Count > 0)
            {
                _lvColumns.SelectedItems[0].Checked = _chkVisible.Checked;
            }
        };

        var lblWidth = new Label
        {
            Text = "Sütun Genişliği (Piksel):",
            Location = new Point(16, 80),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };

        _numWidth = new NumericUpDown
        {
            Location = new Point(16, 102),
            Size = new Size(130, 26),
            Minimum = 20,
            Maximum = 1200,
            Value = 100
        };
        _numWidth.ValueChanged += (_, _) =>
        {
            if (_lvColumns.SelectedItems.Count > 0 && _lvColumns.SelectedItems[0].Tag is ColumnParamItem item)
            {
                item.Width = (int)_numWidth.Value;
                _lvColumns.SelectedItems[0].SubItems[2].Text = item.Width.ToString();
            }
        };

        var lblAlign = new Label
        {
            Text = "Metin Hizalaması:",
            Location = new Point(16, 140),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };

        _cboAlignment = new ComboBox
        {
            Location = new Point(16, 162),
            Size = new Size(180, 26),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboAlignment.Items.AddRange(new object[] { "Sola Dayalı (Metinler)", "Sağa Dayalı (Tutarlar / Sayılar)", "Ortalı (Tarih / Kodlar)" });
        _cboAlignment.SelectedIndex = 0;
        _cboAlignment.SelectedIndexChanged += (_, _) =>
        {
            if (_lvColumns.SelectedItems.Count > 0 && _lvColumns.SelectedItems[0].Tag is ColumnParamItem item)
            {
                item.Alignment = _cboAlignment.SelectedIndex switch
                {
                    1 => DataGridViewContentAlignment.MiddleRight,
                    2 => DataGridViewContentAlignment.MiddleCenter,
                    _ => DataGridViewContentAlignment.MiddleLeft
                };
                _lvColumns.SelectedItems[0].SubItems[3].Text = _cboAlignment.SelectedIndex switch
                {
                    1 => "Sağ",
                    2 => "Orta",
                    _ => "Sol"
                };
            }
        };

        var btnAutoFit = new Button
        {
            Text = "📏  Tüm Sütunları Otomatik Sığdır",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate800,
            BackColor = ModernColors.Slate100,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(230, 34),
            Location = new Point(16, 215),
            Cursor = Cursors.Hand
        };
        btnAutoFit.FlatAppearance.BorderColor = ModernColors.Slate200;
        btnAutoFit.Click += (_, _) => AutoFitAllColumns();

        var pnlProps = new Panel { Dock = DockStyle.Fill };
        pnlProps.Controls.AddRange(new Control[] { lblPropHdr, _chkVisible, lblWidth, _numWidth, lblAlign, _cboAlignment, btnAutoFit });

        pnlSplit.Panel2.Controls.Add(pnlProps);
        tab.Controls.Add(pnlSplit);

        PopulateListView();
    }

    private void BuildAppearanceTab(TabPage tab)
    {
        var lblRowHdr = new Label
        {
            Text = "Satır Yüksekliği ve Kompaktlık",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(20, 16),
            AutoSize = true
        };

        _rbRowCompact = new RadioButton { Text = "Kompakt (28 px - Yoğun liste görünümü)", Location = new Point(24, 46), AutoSize = true };
        _rbRowStandard = new RadioButton { Text = "Standart (36 px - Önerilen dengeli görünüm)", Location = new Point(24, 76), AutoSize = true, Checked = true };
        _rbRowLarge = new RadioButton { Text = "Geniş (44 px - Dokunmatik / Ferah görünüm)", Location = new Point(24, 106), AutoSize = true };

        if (_grid.RowTemplate.Height <= 28) _rbRowCompact.Checked = true;
        else if (_grid.RowTemplate.Height >= 42) _rbRowLarge.Checked = true;
        else _rbRowStandard.Checked = true;

        var lblGridHdr = new Label
        {
            Text = "Tablo Renk ve Çizgi Ayarları",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(20, 160),
            AutoSize = true
        };

        _chkZebra = new CheckBox
        {
            Text = "Alternatif Satır Renklendirmesi (Zebra deseni)",
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(24, 190),
            AutoSize = true,
            Checked = true
        };

        _chkGridLines = new CheckBox
        {
            Text = "Izgara Çizgilerini Göster",
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(24, 220),
            AutoSize = true,
            Checked = _grid.CellBorderStyle != DataGridViewCellBorderStyle.None
        };

        tab.Controls.AddRange(new Control[] { lblRowHdr, _rbRowCompact, _rbRowStandard, _rbRowLarge, lblGridHdr, _chkZebra, _chkGridLines });
    }

    private void PopulateListView()
    {
        _lvColumns.Items.Clear();

        // Sort items by current DisplayIndex
        var sorted = _items.OrderBy(i => i.DisplayIndex).ToList();
        foreach (var item in sorted)
        {
            string alignStr = item.Alignment switch
            {
                DataGridViewContentAlignment.MiddleRight => "Sağ",
                DataGridViewContentAlignment.MiddleCenter => "Orta",
                _ => "Sol"
            };

            var li = new ListViewItem(new string[]
            {
                string.IsNullOrWhiteSpace(item.Column.HeaderText) ? item.Column.Name : item.Column.HeaderText,
                item.Column.DataPropertyName ?? item.Column.Name,
                item.Width.ToString(),
                alignStr
            })
            {
                Checked = item.Visible,
                Tag = item
            };
            _lvColumns.Items.Add(li);
        }

        if (_lvColumns.Items.Count > 0)
        {
            _lvColumns.Items[0].Selected = true;
        }
    }

    private void OnSelectedColumnChanged()
    {
        if (_lvColumns.SelectedItems.Count == 0) return;
        if (_lvColumns.SelectedItems[0].Tag is ColumnParamItem item)
        {
            _chkVisible.Checked = item.Visible;
            _numWidth.Value = Math.Max(_numWidth.Minimum, Math.Min(_numWidth.Maximum, item.Width));
            _cboAlignment.SelectedIndex = item.Alignment switch
            {
                DataGridViewContentAlignment.MiddleRight => 1,
                DataGridViewContentAlignment.MiddleCenter => 2,
                _ => 0
            };
        }
    }

    private void MoveSelectedItem(int direction)
    {
        if (_lvColumns.SelectedItems.Count == 0) return;
        int idx = _lvColumns.SelectedItems[0].Index;
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= _lvColumns.Items.Count) return;

        var li = _lvColumns.SelectedItems[0];
        _lvColumns.Items.RemoveAt(idx);
        _lvColumns.Items.Insert(newIdx, li);
        li.Selected = true;
        li.Focused = true;

        // Re-assign display indices in order of list
        for (int i = 0; i < _lvColumns.Items.Count; i++)
        {
            if (_lvColumns.Items[i].Tag is ColumnParamItem item)
            {
                item.DisplayIndex = i;
            }
        }
    }

    private void AutoFitAllColumns()
    {
        foreach (ListViewItem li in _lvColumns.Items)
        {
            if (li.Tag is ColumnParamItem item)
            {
                int prefWidth = item.Column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true);
                if (prefWidth < 80) prefWidth = 80;
                item.Width = prefWidth;
                li.SubItems[2].Text = prefWidth.ToString();
            }
        }
        if (_lvColumns.SelectedItems.Count > 0 && _lvColumns.SelectedItems[0].Tag is ColumnParamItem sel)
        {
            _numWidth.Value = sel.Width;
        }
    }

    private void ApplySettingsToGrid()
    {
        // Set row height
        int rowHeight = _rbRowCompact.Checked ? 28 : (_rbRowLarge.Checked ? 44 : 36);
        _grid.RowTemplate.Height = rowHeight;
        foreach (DataGridViewRow r in _grid.Rows)
        {
            r.Height = rowHeight;
        }

        // Zebra lines
        if (_chkZebra.Checked)
        {
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        }
        else
        {
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
        }

        _grid.CellBorderStyle = _chkGridLines.Checked
            ? DataGridViewCellBorderStyle.SingleHorizontal
            : DataGridViewCellBorderStyle.None;

        // Apply column order safely sorted by TargetDisplayIndex
        var sorted = _items.OrderBy(i => i.DisplayIndex).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            var item = sorted[i];
            item.Column.Visible = item.Visible;
            item.Column.Width = item.Width;
            item.Column.DefaultCellStyle.Alignment = item.Alignment;

            if (i < _grid.Columns.Count)
            {
                try
                {
                    item.Column.DisplayIndex = i;
                }
                catch { }
            }
        }
    }

    private async Task SaveLayoutToDatabaseAsync()
    {
        try
        {
            var states = _items.OrderBy(i => i.DisplayIndex).Select(i => new GridColumnState
            {
                FieldName = i.Column.DataPropertyName ?? i.Column.Name,
                Index = i.DisplayIndex,
                Width = i.Width,
                Visible = i.Visible,
                AlignmentHorz = i.Alignment switch
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
        }
        catch { }
    }

    private async Task ResetToDefaultAsync()
    {
        var res = MessageBox.Show("Bu tablonun kayıtlı tüm sütun yerleşimini fabrika ayarlarına sıfırlamak istediğinize emin misiniz?",
                                  "Sıfırlama Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            try
            {
                using var conn = DbConnectionConfig.CreateConnection();
                await conn.OpenAsync();
                await conn.ExecuteAsync("DELETE FROM erp.grid_layouts WHERE UserId = @UserId AND FormName = @FormName AND GridName = @GridName;",
                                        new { UserId = _userId, FormName = _formName, GridName = _gridName });
            }
            catch { }

            this.DialogResult = DialogResult.OK;
        }
    }

    private void ExportToCsv()
    {
        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Dosyası (*.csv)|*.csv|Excel Uyumlu Dosya (*.xls)|*.xls",
                FileName = $"{_formName}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                var sb = new StringBuilder();

                // Headers
                var visibleCols = _grid.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible)
                    .OrderBy(c => c.DisplayIndex)
                    .ToList();

                sb.AppendLine(string.Join(";", visibleCols.Select(c => $"\"{c.HeaderText}\"")));

                // Rows
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    var cells = visibleCols.Select(c =>
                    {
                        var val = row.Cells[c.Index].Value?.ToString() ?? "";
                        val = val.Replace("\"", "\"\"");
                        return $"\"{val}\"";
                    });
                    sb.AppendLine(string.Join(";", cells));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Tablo verileri başarıyla aktarıldı:\n{sfd.FileName}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dışa aktarma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
