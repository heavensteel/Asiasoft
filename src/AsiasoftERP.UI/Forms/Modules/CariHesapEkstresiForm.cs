using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class CariHesapEkstresiForm : Form
{
    private readonly CariEkstreService _service = new();

    // Controls
    private TextBox _txtCariKod = null!;
    private TextBox _txtCariUnvan = null!;
    private Button _btnRehber = null!;
    private DateTimePicker _dtpBaslangic = null!;
    private DateTimePicker _dtpBitis = null!;
    private Button _btnSorgula = null!;
    private Button _btnExcel = null!;
    private DataGridView _grid = null!;

    // Footer summary labels
    private Label _lblDevirBorc = null!;
    private Label _lblDevirAlacak = null!;
    private Label _lblDevirBakiye = null!;
    private Label _lblDonemBorc = null!;
    private Label _lblDonemAlacak = null!;
    private Label _lblDonemFark = null!;
    private Label _lblGenelBakiye = null!;
    private Label _lblKayitSayisi = null!;
    private Label _lblPerfInfo = null!;

    private List<CariEkstreRow> _currentRows = new();
    private CariEkstreSummary? _currentSummary;
    private double _selectedFirKod = 0;

    public CariHesapEkstresiForm() : this(0)
    {
    }

    public CariHesapEkstresiForm(double initialFirKod)
    {
        _selectedFirKod = initialFirKod;
        InitializeComponents();
        this.Load += async (_, _) =>
        {
            if (_selectedFirKod > 0)
            {
                await LoadCariDetailsAsync(_selectedFirKod);
                await SorgulaAsync();
            }
        };
    }

    private void InitializeComponents()
    {
        this.Text = "Cari Hesap Ekstresi";
        this.Size = new Size(1300, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f);
        this.BackColor = Color.FromArgb(240, 243, 248);
        this.KeyPreview = true;
        this.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.F5) await SorgulaAsync();
            if (e.KeyCode == Keys.F10) OpenCariRehber();
        };

        // Top Filter Panel
        var pnlFilter = CreateFilterPanel();

        // Center Grid
        CreateGrid();

        // Bottom Summary Panel
        var pnlFooter = CreateFooterPanel();

        this.Controls.Add(_grid);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlFilter);
    }

    private Panel CreateFilterPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        // Cari Kodu
        var lblCariKod = new Label
        {
            Text = "Cari Kodu (F10):",
            Location = new Point(15, 12),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 60, 75)
        };

        _txtCariKod = new TextBox
        {
            Location = new Point(15, 36),
            Width = 100,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        _txtCariKod.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && double.TryParse(_txtCariKod.Text.Trim(), out double firkod))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await LoadCariDetailsAsync(firkod);
                await SorgulaAsync();
            }
        };

        _btnRehber = new Button
        {
            Text = "...",
            Location = new Point(120, 36),
            Size = new Size(32, 28),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(230, 235, 245),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnRehber.FlatAppearance.BorderSize = 0;
        _btnRehber.Click += (_, _) => OpenCariRehber();

        // Cari Ünvanı
        var lblCariUnvan = new Label
        {
            Text = "Cari Ünvanı / Firma Adı:",
            Location = new Point(165, 12),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 60, 75)
        };

        _txtCariUnvan = new TextBox
        {
            Location = new Point(165, 36),
            Width = 320,
            ReadOnly = true,
            BackColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 10f)
        };

        // Tarih Aralığı
        var lblTarih = new Label
        {
            Text = "Tarih Aralığı:",
            Location = new Point(500, 12),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 60, 75)
        };

        _dtpBaslangic = new DateTimePicker
        {
            Location = new Point(500, 36),
            Width = 115,
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(DateTime.Now.Year, 1, 1),
            Font = new Font("Segoe UI", 9.5f)
        };

        var lblTire = new Label
        {
            Text = "—",
            Location = new Point(620, 39),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.Gray
        };

        _dtpBitis = new DateTimePicker
        {
            Location = new Point(640, 36),
            Width = 115,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.Date,
            Font = new Font("Segoe UI", 9.5f)
        };

        // Quick Date Shortcuts
        var btnBuYil = CreateQuickDateButton("Bu Yıl", () =>
        {
            _dtpBaslangic.Value = new DateTime(DateTime.Now.Year, 1, 1);
            _dtpBitis.Value = new DateTime(DateTime.Now.Year, 12, 31);
        }, 765);

        var btnBuAy = CreateQuickDateButton("Bu Ay", () =>
        {
            var now = DateTime.Now;
            _dtpBaslangic.Value = new DateTime(now.Year, now.Month, 1);
            _dtpBitis.Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        }, 830);

        var btnTumu = CreateQuickDateButton("Tümü", () =>
        {
            _dtpBaslangic.Value = new DateTime(2020, 1, 1);
            _dtpBitis.Value = DateTime.Now.Date.AddDays(1);
        }, 895);

        // Action Buttons
        _btnSorgula = new Button
        {
            Text = "🔍  Sorgula (F5)",
            Location = new Point(975, 32),
            Size = new Size(140, 35),
            BackColor = ModernColors.Teal600,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnSorgula.FlatAppearance.BorderSize = 0;
        _btnSorgula.MouseEnter += (s, e) => _btnSorgula.BackColor = Color.FromArgb(15, 118, 110);
        _btnSorgula.MouseLeave += (s, e) => _btnSorgula.BackColor = ModernColors.Teal600;
        _btnSorgula.Click += async (_, _) => await SorgulaAsync();

        _btnExcel = new Button
        {
            Text = "📊  Excel'e Aktar",
            Location = new Point(1125, 32),
            Size = new Size(130, 35),
            BackColor = ModernColors.Emerald500,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnExcel.FlatAppearance.BorderSize = 0;
        _btnExcel.Click += (_, _) => ExportToExcel();

        pnl.Controls.AddRange(new Control[]
        {
            lblCariKod, _txtCariKod, _btnRehber,
            lblCariUnvan, _txtCariUnvan,
            lblTarih, _dtpBaslangic, lblTire, _dtpBitis,
            btnBuYil, btnBuAy, btnTumu,
            _btnSorgula, _btnExcel
        });

        return pnl;
    }

    private Button CreateQuickDateButton(string text, Action onClick, int x)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 37),
            Size = new Size(60, 26),
            BackColor = Color.FromArgb(240, 244, 250),
            ForeColor = Color.FromArgb(60, 75, 95),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(210, 220, 235);
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private void CreateGrid()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 }
        };

        // Enable Double-buffering to eliminate flickering
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, _grid, new object[] { true });

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ModernColors.Slate800,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };
        _grid.ColumnHeadersHeight = 36;

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = Color.FromArgb(215, 232, 252),
            SelectionForeColor = Color.Black,
            Font = new Font("Segoe UI", 9.5f),
            Padding = new Padding(5, 0, 5, 0)
        };

        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 252, 255)
        };

        // Grid Columns
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SiraNo", HeaderText = "Sıra", Width = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Tarih",
            HeaderText = "Tarih",
            Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "VadeTarihi",
            HeaderText = "Vade Tar.",
            Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Turu", HeaderText = "İşlem Türü", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BelgeNo", HeaderText = "Belge No", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Aciklama", HeaderText = "Açıklama", Width = 300 });

        // Financial Columns
        var colBorc = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Borc",
            HeaderText = "Borç (TL)",
            Width = 115,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.00" }
        };
        var colAlacak = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Alacak",
            HeaderText = "Alacak (TL)",
            Width = 115,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.00" }
        };
        var colBakiye = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Bakiye",
            HeaderText = "Bakiye (TL)",
            Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.00", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
        };
        var colBA = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BakiyeDurumu",
            HeaderText = "B/A",
            Width = 45,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
        };

        _grid.Columns.AddRange(new DataGridViewColumn[] { colBorc, colAlacak, colBakiye, colBA });

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DovizTuru", HeaderText = "Döviz", Width = 60 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "DovizBakiye",
            HeaderText = "Döviz Bakiye",
            Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.00" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Kullanici", HeaderText = "Kullanıcı", Width = 90 });

        // Custom Cell Formatting
        _grid.CellFormatting += (s, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is CariEkstreRow row)
            {
                if (row.IsDevir)
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 252, 235);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }

                if (e.ColumnIndex == colBorc.Index && row.Borc > 0)
                {
                    e.CellStyle.ForeColor = Color.FromArgb(170, 20, 20);
                }
                else if (e.ColumnIndex == colAlacak.Index && row.Alacak > 0)
                {
                    e.CellStyle.ForeColor = Color.FromArgb(20, 130, 40);
                }
                else if (e.ColumnIndex == colBakiye.Index || e.ColumnIndex == colBA.Index)
                {
                    e.CellStyle.ForeColor = row.BakiyeDurumu == "(B)" ? Color.FromArgb(170, 20, 20) :
                                            row.BakiyeDurumu == "(A)" ? Color.FromArgb(20, 130, 40) : Color.Black;
                }
            }
        };
    }

    private Panel CreateFooterPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            BackColor = Color.FromArgb(248, 250, 253),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15, 8, 15, 8)
        };

        // Group 1: Devir
        var grpDevir = CreateSummaryGroup("Önceki Dönem Devir", 15, out _lblDevirBorc, out _lblDevirAlacak, out _lblDevirBakiye);

        // Group 2: Dönem İçi
        var grpDonem = CreateSummaryGroup("Dönem İçi Toplamlar", 310, out _lblDonemBorc, out _lblDonemAlacak, out _lblDonemFark);

        // Group 3: Genel Bakiye (Large)
        var pnlGenelBakiye = new Panel
        {
            Location = new Point(620, 10),
            Size = new Size(360, 85),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblGenelTitle = new Label
        {
            Text = "GENEL NET BAKİYE",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 110, 125)
        };

        _lblGenelBakiye = new Label
        {
            Text = "0,00 TL (0)",
            Location = new Point(10, 30),
            AutoSize = true,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 50, 70)
        };
        pnlGenelBakiye.Controls.AddRange(new Control[] { lblGenelTitle, _lblGenelBakiye });

        // Status & Performance Info
        _lblKayitSayisi = new Label
        {
            Text = "0 hareket kaydı",
            Location = new Point(1000, 20),
            AutoSize = true,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 70, 85)
        };

        _lblPerfInfo = new Label
        {
            Text = "Sorgulama süresi: 0 ms",
            Location = new Point(1000, 48),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(120, 130, 145)
        };

        pnl.Controls.AddRange(new Control[] { grpDevir, grpDonem, pnlGenelBakiye, _lblKayitSayisi, _lblPerfInfo });
        return pnl;
    }

    private Panel CreateSummaryGroup(string title, int x, out Label lblBorc, out Label lblAlacak, out Label lblFark)
    {
        var grp = new Panel
        {
            Location = new Point(x, 10),
            Size = new Size(280, 85),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTitle = new Label
        {
            Text = title,
            Location = new Point(10, 6),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 110, 125)
        };

        var lblB = new Label { Text = "Borç:", Location = new Point(10, 26), AutoSize = true, ForeColor = Color.FromArgb(80, 90, 100) };
        lblBorc = new Label { Text = "0,00 TL", Location = new Point(60, 26), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(170, 20, 20) };

        var lblA = new Label { Text = "Alacak:", Location = new Point(10, 44), AutoSize = true, ForeColor = Color.FromArgb(80, 90, 100) };
        lblAlacak = new Label { Text = "0,00 TL", Location = new Point(60, 44), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 130, 40) };

        var lblF = new Label { Text = "Bakiye:", Location = new Point(10, 62), AutoSize = true, ForeColor = Color.FromArgb(80, 90, 100) };
        lblFark = new Label { Text = "0,00 TL", Location = new Point(60, 62), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

        grp.Controls.AddRange(new Control[] { lblTitle, lblB, lblBorc, lblA, lblAlacak, lblF, lblFark });
        return grp;
    }

    private void OpenCariRehber()
    {
        using var rehber = new CariRehberForm(_txtCariKod.Text.Trim());
        if (rehber.ShowDialog(this) == DialogResult.OK && rehber.SelectedCari != null)
        {
            _selectedFirKod = rehber.SelectedCari.FirKod;
            _txtCariKod.Text = _selectedFirKod.ToString();
            _txtCariUnvan.Text = rehber.SelectedCari.FirmaAdi;
            _ = SorgulaAsync();
        }
    }

    private async Task LoadCariDetailsAsync(double firkod)
    {
        var cari = await _service.GetCariByKodAsync(firkod);
        if (cari != null)
        {
            _selectedFirKod = cari.FirKod;
            _txtCariKod.Text = cari.FirKod.ToString();
            _txtCariUnvan.Text = cari.FirmaAdi;
        }
        else
        {
            _txtCariUnvan.Text = "Cari bulunamadı!";
        }
    }

    private async Task SorgulaAsync()
    {
        if (_selectedFirKod <= 0)
        {
            if (double.TryParse(_txtCariKod.Text.Trim(), out double firkod))
            {
                _selectedFirKod = firkod;
                await LoadCariDetailsAsync(firkod);
            }
            else
            {
                OpenCariRehber();
                return;
            }
        }

        _btnSorgula.Enabled = false;
        _btnSorgula.Text = "Yükleniyor...";
        Cursor.Current = Cursors.WaitCursor;

        try
        {
            var sw = Stopwatch.StartNew();

            var filter = new CariEkstreFilter
            {
                FirKod = _selectedFirKod,
                BaslangicTarihi = _dtpBaslangic.Value.Date,
                BitisTarihi = _dtpBitis.Value.Date
            };

            var (rows, summary) = await _service.GetEkstreAsync(filter);
            sw.Stop();

            _currentRows = rows;
            _currentSummary = summary;
            _grid.DataSource = rows;

            // Update Summaries
            _lblDevirBorc.Text = summary.DevirBorc.ToString("#,##0.00 TL");
            _lblDevirAlacak.Text = summary.DevirAlacak.ToString("#,##0.00 TL");
            _lblDevirBakiye.Text = $"{Math.Abs(summary.DevirBakiye):#,##0.00 TL} {(summary.DevirBakiye > 0 ? "(B)" : summary.DevirBakiye < 0 ? "(A)" : "(0)")}";

            _lblDonemBorc.Text = summary.DonemBorc.ToString("#,##0.00 TL");
            _lblDonemAlacak.Text = summary.DonemAlacak.ToString("#,##0.00 TL");
            _lblDonemFark.Text = $"{Math.Abs(summary.DonemFark):#,##0.00 TL} {(summary.DonemFark > 0 ? "(B)" : summary.DonemFark < 0 ? "(A)" : "(0)")}";

            _lblGenelBakiye.Text = $"{Math.Abs(summary.GenelBakiye):#,##0.00 TL} {summary.GenelBakiyeDurumu}";
            _lblGenelBakiye.ForeColor = summary.GenelBakiye > 0 ? Color.FromArgb(180, 20, 20) :
                                       summary.GenelBakiye < 0 ? Color.FromArgb(20, 130, 40) : Color.Black;

            _lblKayitSayisi.Text = $"{summary.ToplamKayitSayisi:N0} hareket kaydı";
            _lblPerfInfo.Text = $"Sorgulama süresi: {sw.ElapsedMilliseconds} ms (Ultra Hızlı)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ekstre sorgulama hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSorgula.Enabled = true;
            _btnSorgula.Text = "🔍  Sorgula (F5)";
            Cursor.Current = Cursors.Default;
        }
    }

    private void ExportToExcel()
    {
        if (_currentRows.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak veri bulunamadı. Lütfen önce sorgulama yapın.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV Dosyası (*.csv)|*.csv|Metin Dosyası (*.txt)|*.txt",
            FileName = $"CariEkstre_{_selectedFirKod}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Sıra;Tarih;Vade Tarihi;İşlem Türü;Belge No;Açıklama;Borç;Alacak;Bakiye;B/A;Döviz Türü;Döviz Bakiye;Kullanıcı");

                foreach (var r in _currentRows)
                {
                    sb.AppendLine($"{r.SiraNo};{r.Tarih:dd.MM.yyyy};{r.VadeTarihi:dd.MM.yyyy};{r.Turu};{r.BelgeNo};\"{r.Aciklama.Replace("\"", "\"\"")}\";{r.Borc:0.00};{r.Alacak:0.00};{r.Bakiye:0.00};{r.BakiyeDurumu};{r.DovizTuru};{r.DovizBakiye:0.00};{r.Kullanici}");
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Ekstre başarıyla Excel CSV formatında kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dışa aktarma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
