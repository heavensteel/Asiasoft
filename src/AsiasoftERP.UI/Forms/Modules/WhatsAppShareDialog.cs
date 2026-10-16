using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using AsiasoftERP.UI.Helpers;
using AsiasoftERP.UI.Reports;

namespace AsiasoftERP.UI.Forms.Modules;

public class WhatsAppShareDialog : Form
{
    private readonly YuklemeProgramiDto _prog;
    private TextBox _txtPhone = null!;
    private TextBox _txtMessage = null!;
    private CheckBox _chkCopyToClipboard = null!;
    private CheckBox _chkSaveFile = null!;
    private string _savedImagePath = string.Empty;

    public WhatsAppShareDialog(YuklemeProgramiDto prog)
    {
        _prog = prog ?? throw new ArgumentNullException(nameof(prog));
        InitializeComponents();
        BuildDefaultMessage();
    }

    private void InitializeComponents()
    {
        this.Text = "WhatsApp ile Yükleme Programı Paylaş";
        this.Size = new Size(800, 690);
        this.MinimumSize = new Size(760, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. TOP HEADER BANNER (WhatsApp Dark Style)
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = ModernColors.WhatsAppDark,
            Padding = new Padding(18, 12, 18, 12)
        };

        var lblIcon = new Label
        {
            Text = "💬",
            Font = new Font("Segoe UI", 22f),
            ForeColor = Color.White,
            Location = new Point(14, 12),
            Size = new Size(46, 46)
        };

        var lblTitle = new Label
        {
            Text = "WhatsApp ile Yükleme Programı Paylaş",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(64, 12),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = $"{_prog.ProgramNo} • {_prog.Baslik} • {_prog.ToplamPalet} PLT ({_prog.SatirSayisi}x{_prog.SutunSayisi} Grid)",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(178, 235, 206),
            Location = new Point(66, 38),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSubtitle });

        // 2. BOTTOM ACTION BAR
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = Color.White,
            Padding = new Padding(16, 11, 16, 11)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        var btnClose = new Button
        {
            Text = "Kapat",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate200,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(85, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => this.DialogResult = DialogResult.Cancel;

        var btnOpenFolder = new Button
        {
            Text = "📁 Klasörde Aç",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate100,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(115, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        btnOpenFolder.FlatAppearance.BorderColor = ModernColors.Slate300;
        btnOpenFolder.Click += (_, _) => OpenInFolder();

        var btnCopyImage = new Button
        {
            Text = "🖼️ Resmi Kopyala",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate100,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(125, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        btnCopyImage.FlatAppearance.BorderColor = ModernColors.Slate300;
        btnCopyImage.Click += (_, _) => CopyImageToClipboard(silent: false);

        var btnCopyText = new Button
        {
            Text = "📋 Metni Kopyala",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate700,
            BackColor = ModernColors.Slate100,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        btnCopyText.FlatAppearance.BorderColor = ModernColors.Slate300;
        btnCopyText.Click += (_, _) =>
        {
            Clipboard.SetText(_txtMessage.Text);
            NotificationService.NotifySuccess("Metin Kopyalandı", "WhatsApp mesaj metni panoya kopyalandı.", "Clipboard");
        };

        var btnSendWhatsApp = new Button
        {
            Text = "🟢 WhatsApp'ta Gönder",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.WhatsAppGreen,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(220, 38),
            Dock = DockStyle.Left,
            Cursor = Cursors.Hand
        };
        btnSendWhatsApp.FlatAppearance.BorderSize = 0;
        btnSendWhatsApp.Click += (_, _) => SendWhatsApp();

        pnlBottom.Controls.AddRange(new Control[] { btnSendWhatsApp, btnClose, btnOpenFolder, btnCopyImage, btnCopyText });

        // 3. MAIN CONTENT BODY
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 12),
            AutoScroll = true
        };

        // Section A: Phone Number
        var lblPhone = new Label
        {
            Text = "Alıcı Telefon Numarası (Boş bırakırsanız WhatsApp açıldığında kişi seçebilirsiniz):",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(20, 14),
            AutoSize = true
        };

        _txtPhone = new TextBox
        {
            Location = new Point(20, 36),
            Size = new Size(330, 28),
            Font = new Font("Segoe UI", 10.5f),
            PlaceholderText = "Örn: 905321234567 veya +90 532 123 45 67"
        };

        var flpRoles = new FlowLayoutPanel
        {
            Location = new Point(360, 34),
            Size = new Size(400, 34),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        Button CreateRoleBtn(string text, string promptHint)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 28,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ModernColors.Slate700,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderColor = ModernColors.Slate300;
            btn.Click += (_, _) =>
            {
                _txtPhone.Focus();
                if (string.IsNullOrWhiteSpace(_txtPhone.Text))
                {
                    _txtPhone.Text = "+90 ";
                    _txtPhone.SelectionStart = _txtPhone.Text.Length;
                }
            };
            return btn;
        }

        flpRoles.Controls.Add(CreateRoleBtn("🚛 Şoför", "Şoför Numarası"));
        flpRoles.Controls.Add(CreateRoleBtn("📦 İhracat Müşterisi", "Alıcı Numarası"));
        flpRoles.Controls.Add(CreateRoleBtn("🏢 Nakliye", "Nakliye Numarası"));
        flpRoles.Controls.Add(CreateRoleBtn("🏭 Şef", "İşletme Şefi"));

        // Section B: Message Text
        var lblMsg = new Label
        {
            Text = "Paylaşılacak Özet Mesaj (WhatsApp Formatlı):",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(20, 78),
            AutoSize = true
        };

        _txtMessage = new TextBox
        {
            Location = new Point(20, 100),
            Size = new Size(660, 310),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f),
            BackColor = Color.White
        };

        // Section C: Options
        var pnlOptions = new Panel
        {
            Location = new Point(20, 420),
            Size = new Size(660, 70),
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(12)
        };
        pnlOptions.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlOptions.Width - 1, pnlOptions.Height - 1);
        };

        _chkCopyToClipboard = new CheckBox
        {
            Text = "🖼️  Antetli A4 Baskı Görselini Otomatik Panoya Kopyala (WhatsApp açıldığında Ctrl + V ile yapıştırın)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate800,
            Location = new Point(12, 10),
            Size = new Size(630, 22),
            Checked = true
        };

        _chkSaveFile = new CheckBox
        {
            Text = "📁  Yüksek çözünürlüklü A4 PNG belgesini geçici klasöre kaydet",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ModernColors.Slate600,
            Location = new Point(12, 36),
            Size = new Size(630, 22),
            Checked = true
        };

        pnlOptions.Controls.AddRange(new Control[] { _chkCopyToClipboard, _chkSaveFile });

        pnlBody.Controls.AddRange(new Control[]
        {
            lblPhone, _txtPhone, flpRoles,
            lblMsg, _txtMessage,
            pnlOptions
        });

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHeader);
    }

    private void BuildDefaultMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine("🚚 *TEKASYA TARIM - YÜKLEME PROGRAMI*");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"📋 *Program No:* {_prog.ProgramNo}");
        sb.AppendLine($"📅 *Yükleme Tarihi:* {_prog.Tarih:dd.MM.yyyy}");
        sb.AppendLine($"🎯 *Sevkiyat / Hedef:* {_prog.Baslik}");
        sb.AppendLine($"📦 *Toplam Palet:* {_prog.ToplamPalet} PLT ({_prog.SatirSayisi}x{_prog.SutunSayisi} Grid Mizanpaj)");
        sb.AppendLine($"👤 *Hazırlayan:* {_prog.Hazirlayan} | *Onaylayan:* {_prog.Onaylayan}");
        sb.AppendLine($"📌 *Durum:* {_prog.Durum}");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("*BÖLÜM VE ALICI DETAYLARI:*");

        var activeCells = _prog.Sutunlar
            .Where(s => s.SatirNo >= 1 && s.SatirNo <= _prog.SatirSayisi && s.SutunNo >= 1 && s.SutunNo <= _prog.SutunSayisi)
            .OrderBy(s => s.SatirNo).ThenBy(s => s.SutunNo)
            .ToList();

        if (activeCells.Count == 0)
        {
            activeCells = _prog.Sutunlar.ToList();
        }

        int cellNum = 1;
        foreach (var c in activeCells)
        {
            string title = string.IsNullOrWhiteSpace(c.Baslik) ? $"Bölüm {cellNum}" : c.Baslik;
            int plt = c.ToplamPalet > 0 ? c.ToplamPalet : YuklemeProgramiReportRenderer.CalculatePalletsFromText(c.UrunlerMetin);
            sb.AppendLine($"\n📍 *{cellNum}. {title}* ({plt} PLT)");

            // Products summary
            if (!string.IsNullOrWhiteSpace(c.UrunlerMetin))
            {
                var prodLines = c.UrunlerMetin.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => l.Contains("PLT", StringComparison.OrdinalIgnoreCase))
                    .Take(4);
                foreach (var pl in prodLines)
                {
                    sb.AppendLine($"   • {pl.Trim()}");
                }
            }

            // Party & Thermoking
            if (!string.IsNullOrWhiteSpace(c.PartilerMetin))
            {
                var pLines = c.PartilerMetin.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Take(2);
                foreach (var pl in pLines)
                {
                    sb.AppendLine($"   ▪️ _{pl.Trim()}_");
                }
            }

            cellNum++;
        }

        sb.AppendLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("📄 _Antetli resmi A4 yükleme belgesi panonuza kopyalanmıştır. Sohbette Ctrl + V yaparak doğrudan görseli gönderebilirsiniz._");
        sb.AppendLine("🏢 *Tek Asya Tarım Ürünleri İç ve Dış Ticaret A.Ş.*");

        _txtMessage.Text = sb.ToString();
    }

    private void SendWhatsApp()
    {
        try
        {
            // 1. Copy image to clipboard if selected
            if (_chkCopyToClipboard.Checked)
            {
                CopyImageToClipboard(silent: true);
            }

            // 2. Save file if selected
            if (_chkSaveFile.Checked)
            {
                EnsureImageSaved();
            }

            // 3. Prepare WhatsApp URL
            string text = _txtMessage.Text.Trim();
            string phone = CleanPhoneNumber(_txtPhone.Text.Trim());

            string url = string.IsNullOrWhiteSpace(phone)
                ? $"https://api.whatsapp.com/send?text={Uri.EscapeDataString(text)}"
                : $"https://api.whatsapp.com/send?phone={phone}&text={Uri.EscapeDataString(text)}";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            NotificationService.NotifySuccess(
                "WhatsApp Açılıyor",
                "Yükleme Programı hazırlandı. WhatsApp açıldığında sohbete Ctrl+V yaparak antetli belgeyi de yapıştırabilirsiniz.",
                "WhatsApp");

            this.DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WhatsApp açılırken bir hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyImageToClipboard(bool silent = false)
    {
        try
        {
            using var bmp = YuklemeProgramiReportRenderer.RenderToBitmap(_prog, 1200, 1695);
            Clipboard.SetImage(bmp);
            if (!silent)
            {
                NotificationService.NotifySuccess("Resim Kopyalandı", "A4 yükleme belgesi panoya kopyalandı! (Ctrl + V)", "Clipboard");
            }
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show($"Resim kopyalanamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private string EnsureImageSaved()
    {
        if (!string.IsNullOrEmpty(_savedImagePath) && File.Exists(_savedImagePath))
            return _savedImagePath;

        string tempFolder = Path.Combine(Path.GetTempPath(), "AsiasoftYukleme");
        if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

        string safeNo = string.Join("_", _prog.ProgramNo.Split(Path.GetInvalidFileNameChars()));
        _savedImagePath = Path.Combine(tempFolder, $"Yukleme_Programi_{safeNo}.png");

        using var bmp = YuklemeProgramiReportRenderer.RenderToBitmap(_prog, 1200, 1695);
        bmp.Save(_savedImagePath, System.Drawing.Imaging.ImageFormat.Png);
        return _savedImagePath;
    }

    private void OpenInFolder()
    {
        try
        {
            string path = EnsureImageSaved();
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Klasör açılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string CleanPhoneNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("05") && digits.Length == 11)
        {
            digits = "9" + digits; // convert 0532... to 90532...
        }
        else if (digits.StartsWith("5") && digits.Length == 10)
        {
            digits = "90" + digits; // convert 532... to 90532...
        }
        return digits;
    }
}
