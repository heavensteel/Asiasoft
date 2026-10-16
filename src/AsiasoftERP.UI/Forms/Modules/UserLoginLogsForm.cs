using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

/// <summary>
/// Executive User Login & Security Audit Log Form.
/// Displays who logged in, from which computer (Machine Name), Windows Username, IP address, and session status.
/// </summary>
public class UserLoginLogsForm : Form
{
    private readonly MacroService _macroService = new();
    private List<UserLoginLogDto> _allLogs = new();

    private DataGridView _grid = null!;
    private Panel _pnlTopToolbar = null!;
    private Panel _pnlKpiContainer = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cmbStatusFilter = null!;
    private ComboBox _cmbDateFilter = null!;
    private Label _lblFooterStats = null!;
    private Button _btnRefresh = null!;
    private Button _btnExport = null!;

    // KPI Labels
    private Label _lblKpiTotalVal = null!;
    private Label _lblKpiActiveVal = null!;
    private Label _lblKpiMachinesVal = null!;
    private Label _lblKpiFailedVal = null!;

    public UserLoginLogsForm()
    {
        this.DoubleBuffered = true;
        this.Text = "🛡️ Kullanıcı Giriş & Güvenlik Logları";
        this.Size = new Size(1250, 720);
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = ModernColors.Slate100;

        InitializeUI();
        _ = LoadLogsAsync();
    }

    private void InitializeUI()
    {
        // 1. Top KPI Summary Cards Panel
        _pnlKpiContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(12, 10, 12, 6)
        };

        var tlpKpi = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        var card1 = CreateKpiCard("👥 TOPLAM GİRİŞ", "0", Color.FromArgb(37, 99, 235), out _lblKpiTotalVal);
        var card2 = CreateKpiCard("🟢 AKTİF OTURUMLAR", "0", Color.FromArgb(16, 185, 129), out _lblKpiActiveVal);
        var card3 = CreateKpiCard("💻 FARKLI BİLGİSAYARLAR", "0", Color.FromArgb(79, 70, 229), out _lblKpiMachinesVal);
        var card4 = CreateKpiCard("⚠️ HATALI ŞİFRE DENEMELERİ", "0", Color.FromArgb(225, 29, 72), out _lblKpiFailedVal);

        tlpKpi.Controls.Add(card1, 0, 0);
        tlpKpi.Controls.Add(card2, 1, 0);
        tlpKpi.Controls.Add(card3, 2, 0);
        tlpKpi.Controls.Add(card4, 3, 0);

        _pnlKpiContainer.Controls.Add(tlpKpi);

        // 2. Toolbar & Filter Panel
        _pnlTopToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.White,
            Padding = new Padding(12, 8, 12, 8)
        };
        _pnlTopToolbar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, _pnlTopToolbar.Height - 1, _pnlTopToolbar.Width, _pnlTopToolbar.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "🛡️  Oturum & Denetim Kayıtları",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            AutoSize = true,
            Location = new Point(10, 12)
        };

        _btnRefresh = new Button
        {
            Text = "⟳ Yenile",
            Location = new Point(245, 8),
            Size = new Size(85, 32),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRefresh.FlatAppearance.BorderColor = ModernColors.Slate200;
        _btnRefresh.Click += async (_, _) => await LoadLogsAsync();

        _btnExport = new Button
        {
            Text = "📊 Excel'e Aktar",
            Location = new Point(335, 8),
            Size = new Size(115, 32),
            BackColor = Color.FromArgb(236, 253, 245),
            ForeColor = Color.FromArgb(4, 120, 87),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnExport.FlatAppearance.BorderColor = Color.FromArgb(167, 243, 208);
        _btnExport.Click += (_, _) => ExportToCsv();

        // Right-anchored Filter Controls
        var pnlFilters = new Panel
        {
            Dock = DockStyle.Right,
            Width = 560,
            BackColor = Color.Transparent
        };

        // Date Filter
        _cmbDateFilter = new ComboBox
        {
            Location = new Point(10, 9),
            Width = 115,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cmbDateFilter.Items.AddRange(new object[] { "Tüm Tarihler", "Bugün", "Son 3 Gün", "Son 7 Gün", "Bu Ay" });
        _cmbDateFilter.SelectedIndex = 0;
        _cmbDateFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        // Status Filter
        _cmbStatusFilter = new ComboBox
        {
            Location = new Point(135, 9),
            Width = 145,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cmbStatusFilter.Items.AddRange(new object[] { "Tüm Durumlar", "🟢 Aktif Oturumlar", "✅ Başarılı Girişler", "❌ Hatalı Şifreler" });
        _cmbStatusFilter.SelectedIndex = 0;
        _cmbStatusFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        // Search Box
        var pnlSearch = new Panel
        {
            Size = new Size(245, 30),
            Location = new Point(295, 8),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        var lblSearchIcon = new Label
        {
            Text = "🔍",
            Size = new Size(24, 30),
            Location = new Point(4, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        _txtSearch = new TextBox
        {
            Location = new Point(28, 5),
            Width = 210,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Kullanıcı, Bilgisayar, Windows Adı..."
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        pnlSearch.Controls.AddRange(new Control[] { lblSearchIcon, _txtSearch });
        pnlFilters.Controls.AddRange(new Control[] { _cmbDateFilter, _cmbStatusFilter, pnlSearch });

        _pnlTopToolbar.Controls.AddRange(new Control[] { lblTitle, _btnRefresh, _btnExport, pnlFilters });

        // 3. Bottom Status Bar
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(14, 0, 14, 0)
        };
        pnlFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        _lblFooterStats = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = 650,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate600,
            Text = "Loglar yükleniyor..."
        };

        var lblHint = new Label
        {
            Dock = DockStyle.Right,
            AutoSize = false,
            Width = 500,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = ModernColors.Slate500,
            Text = "💡 Giriş logları kullanıcı kodu, bilgisayar makine adı ve Windows kullanıcı adı bazında tutulur."
        };

        pnlFooter.Controls.AddRange(new Control[] { _lblFooterStats, lblHint });

        // 4. Main DataGridView
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };

        ModernUiHelper.ApplyModernGridStyle(_grid);
        _grid.RowTemplate.Height = 34;

        SetupGridColumns();

        _grid.CellPainting += Grid_CellPainting;

        // Context Menu
        var ctx = new ContextMenuStrip { Font = new Font("Segoe UI", 9f) };
        ctx.Items.Add("🔍 Bu Kullanıcıya Göre Filtrele", null, (_, _) => FilterBySelected(g => g.KullaniciAdi));
        ctx.Items.Add("💻 Bu Bilgisayara Göre Filtrele", null, (_, _) => FilterBySelected(g => g.BilgisayarAdi));
        ctx.Items.Add("🪟 Bu Windows Hesabına Göre Filtrele", null, (_, _) => FilterBySelected(g => g.WindowsKullanici));
        ctx.Items.Add(new ToolStripSeparator());
        ctx.Items.Add("⟳ Listeyi Yenile", null, async (_, _) => await LoadLogsAsync());
        _grid.ContextMenuStrip = ctx;

        this.Controls.Add(_grid);
        this.Controls.Add(_pnlTopToolbar);
        this.Controls.Add(_pnlKpiContainer);
        this.Controls.Add(pnlFooter);
    }

    private Control CreateKpiCard(string title, string initialVal, Color accentColor, out Label lblVal)
    {
        var card = new ModernCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            CornerRadius = 8,
            TopAccentColor = accentColor,
            TopAccentHeight = 3,
            CardBackground = Color.White,
            BorderColor = ModernColors.Slate200,
            Padding = new Padding(12, 8, 12, 8)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate500,
            Dock = DockStyle.Top,
            Height = 18
        };

        lblVal = new Label
        {
            Text = initialVal,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        card.Controls.Add(lblVal);
        card.Controls.Add(lblTitle);
        return card;
    }

    private void SetupGridColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                Name = "colGirisZamani",
                DataPropertyName = "GirisZamani",
                HeaderText = "Giriş Zamanı",
                Width = 145,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colKullanici",
                DataPropertyName = "KullaniciAdi",
                HeaderText = "ERP Kullanıcısı",
                Width = 180,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = ModernColors.Slate900
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colSirket",
                DataPropertyName = "SirketAdi",
                HeaderText = "Şirket",
                Width = 140
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colBilgisayar",
                DataPropertyName = "BilgisayarAdi",
                HeaderText = "💻 Bilgisayar (Makine Adı)",
                Width = 180,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(67, 56, 202)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colWindowsUser",
                DataPropertyName = "WindowsKullanici",
                HeaderText = "🪟 Windows Hesabı",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 118, 110)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colIp",
                DataPropertyName = "IpAdresi",
                HeaderText = "IP Adresi",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colOturum",
                DataPropertyName = "OturumTuru",
                HeaderText = "Oturum",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colSure",
                DataPropertyName = "SureDisplay",
                HeaderText = "Oturum Süresi",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colDurum",
                DataPropertyName = "Durum",
                HeaderText = "Durum",
                Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colDetay",
                DataPropertyName = "Detay",
                HeaderText = "Detay / Açıklama",
                Width = 200,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    ForeColor = ModernColors.Slate600
                }
            }
        );
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0) return;

        string colName = _grid.Columns[e.ColumnIndex].Name;

        if (colName == "colDurum")
        {
            e.PaintBackground(e.CellBounds, true);

            string text = e.Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(text)) return;

            Color bgColor, textColor, borderColor;

            if (text.Contains("BAŞARILI"))
            {
                bgColor = Color.FromArgb(220, 252, 231);
                textColor = Color.FromArgb(21, 128, 61);
                borderColor = Color.FromArgb(134, 239, 172);
            }
            else if (text.Contains("HATALI"))
            {
                bgColor = Color.FromArgb(254, 226, 226);
                textColor = Color.FromArgb(185, 28, 28);
                borderColor = Color.FromArgb(252, 165, 165);
            }
            else
            {
                bgColor = Color.FromArgb(241, 245, 249);
                textColor = Color.FromArgb(71, 85, 105);
                borderColor = Color.FromArgb(226, 232, 240);
            }

            if (e.Graphics == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var pillRect = new Rectangle(
                e.CellBounds.X + (e.CellBounds.Width - 116) / 2,
                e.CellBounds.Y + (e.CellBounds.Height - 22) / 2,
                116, 22);

            using var path = ModernUiHelper.GetRoundedPath(pillRect, 5);
            using var brush = new SolidBrush(bgColor);
            g.FillPath(brush, path);

            using var pen = new Pen(borderColor, 1f);
            g.DrawPath(pen, path);

            TextRenderer.DrawText(g, text, new Font("Segoe UI", 8f, FontStyle.Bold), pillRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
    }

    private async Task LoadLogsAsync()
    {
        try
        {
            _btnRefresh.Enabled = false;
            _lblFooterStats.Text = "Loglar sunucudan yükleniyor...";

            _allLogs = await _macroService.GetRecentUserLoginsAsync(300);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Loglar yüklenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnRefresh.Enabled = true;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allLogs.AsEnumerable();

        // 1. Date filter
        DateTime today = DateTime.Today;
        if (_cmbDateFilter.SelectedIndex == 1) // Bugün
            filtered = filtered.Where(l => l.GirisZamani.Date == today);
        else if (_cmbDateFilter.SelectedIndex == 2) // Son 3 Gün
            filtered = filtered.Where(l => l.GirisZamani.Date >= today.AddDays(-3));
        else if (_cmbDateFilter.SelectedIndex == 3) // Son 7 Gün
            filtered = filtered.Where(l => l.GirisZamani.Date >= today.AddDays(-7));
        else if (_cmbDateFilter.SelectedIndex == 4) // Bu Ay
            filtered = filtered.Where(l => l.GirisZamani.Year == today.Year && l.GirisZamani.Month == today.Month);

        // 2. Status filter
        if (_cmbStatusFilter.SelectedIndex == 1) // Aktif Oturumlar
            filtered = filtered.Where(l => l.IsActiveSession);
        else if (_cmbStatusFilter.SelectedIndex == 2) // Başarılı
            filtered = filtered.Where(l => l.IsSuccess);
        else if (_cmbStatusFilter.SelectedIndex == 3) // Hatalı
            filtered = filtered.Where(l => !l.IsSuccess);

        // 3. Search query
        string s = _txtSearch.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(s))
        {
            filtered = filtered.Where(l =>
                l.KullaniciAdi.ToLower().Contains(s) ||
                l.KullaniciKodu.ToLower().Contains(s) ||
                l.BilgisayarAdi.ToLower().Contains(s) ||
                l.WindowsKullanici.ToLower().Contains(s) ||
                l.IpAdresi.ToLower().Contains(s) ||
                l.SirketAdi.ToLower().Contains(s)
            );
        }

        var list = filtered.ToList();
        _grid.DataSource = list;

        // Update KPIs
        int total = _allLogs.Count;
        int active = _allLogs.Count(l => l.IsActiveSession);
        int distinctMachines = _allLogs.Select(l => l.BilgisayarAdi).Where(m => !string.IsNullOrEmpty(m)).Distinct().Count();
        int failed = _allLogs.Count(l => !l.IsSuccess);

        _lblKpiTotalVal.Text = total.ToString("N0");
        _lblKpiActiveVal.Text = active.ToString("N0");
        _lblKpiMachinesVal.Text = distinctMachines.ToString("N0");
        _lblKpiFailedVal.Text = failed.ToString("N0");

        _lblFooterStats.Text = $"📊 Toplam {total} Kayıt  |  🟢 {active} Aktif Oturum  |  💻 {distinctMachines} Farklı Makine  |  ⚠️ {failed} Hatalı Giriş  (Gösterilen: {list.Count})";
    }

    private void FilterBySelected(Func<UserLoginLogDto, string> selector)
    {
        if (_grid.CurrentRow?.DataBoundItem is UserLoginLogDto dto)
        {
            string val = selector(dto);
            if (!string.IsNullOrEmpty(val))
            {
                _txtSearch.Text = val;
            }
        }
    }

    private void ExportToCsv()
    {
        try
        {
            if (_grid.DataSource is not List<UserLoginLogDto> list || list.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak kayıt bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Dosyası (*.csv)|*.csv",
                FileName = $"Giris_Loglari_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var lines = new List<string>
                {
                    "Giriş Zamanı;ERP Kullanıcısı;Şirket;Bilgisayar Adı;Windows Kullanıcısı;IP Adresi;Oturum Türü;Oturum Süresi;Durum;Detay"
                };

                foreach (var item in list)
                {
                    lines.Add($"{item.GirisZamani:dd.MM.yyyy HH:mm:ss};{item.KullaniciAdi};{item.SirketAdi};{item.BilgisayarAdi};{item.WindowsKullanici};{item.IpAdresi};{item.OturumTuru};{item.SureDisplay};{item.Durum};{item.Detay}");
                }

                System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                MessageBox.Show($"Loglar başarıyla kaydedildi:\n{sfd.FileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
