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
/// Advanced multi-branch & FortiVPN weighbridge configuration dialog.
/// Supports dual-route IP management (Local 192.168.60.* vs Branch/VPN 192.168.45.*, 192.168.31.*)
/// with real-time network diagnostic and simultaneous dual path connection testing.
/// </summary>
public class KantarAyarlariDialog : Form
{
    private readonly MacroService _service = new();
    private readonly string _kullaniciKodu;
    private readonly string _kullaniciAdi;

    private CheckBox _chkKantarAktif = null!;
    private ComboBox _cmbKantarTanim = null!;
    private ComboBox _cmbBaglantiModu = null!;
    private TextBox _txtLokalIp = null!;
    private NumericUpDown _numLokalPort = null!;
    private TextBox _txtVpnIp = null!;
    private NumericUpDown _numVpnPort = null!;

    private Label _lblNetworkDiag = null!;
    private Button _btnDualTest = null!;
    private Panel _pnlTestResults = null!;
    private Label _lblLokalResult = null!;
    private Label _lblVpnResult = null!;

    private Button _btnKaydet = null!;
    private Button _btnIptal = null!;

    private List<KantarTanimDto> _registeredScales = new();

    public bool IsSaved { get; private set; }
    public KantarAyarDto CurrentSettings { get; private set; } = new();

    public KantarAyarlariDialog(string? kullaniciKodu = null, string? kullaniciAdi = null)
    {
        _kullaniciKodu = kullaniciKodu ?? AppState.CurrentUserCode;
        _kullaniciAdi = kullaniciAdi ?? AppState.CurrentUserName;

        this.DoubleBuffered = true;
        InitializeComponents();
        this.Load += async (_, _) => await LoadSettingsAsync();
    }

    private void InitializeComponents()
    {
        this.Text = "⚖️ Kantar IP & FortiVPN Çoklu Ağ Yapılandırması";
        this.Size = new Size(680, 740);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9.5f);

        // 1. Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = ModernColors.Slate900,
            Padding = new Padding(22, 14, 22, 14)
        };

        var lblTitle = new Label
        {
            Text = "⚖️  KANTAR IP & FORTIVPN ÇOKLU AĞ YÖNETİMİ",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 12),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = $"Kullanıcı: {_kullaniciAdi} (Kod: {_kullaniciKodu}) — Lokal (192.168.60.*) ve Şube / VPN (192.168.45.*, 192.168.31.*) Geçişi",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(22, 38),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // 2. Main Scrollable Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(25, 18, 25, 18)
        };

        int y = 14;

        // Active Checkbox
        _chkKantarAktif = new CheckBox
        {
            Text = "Bu kullanıcı için canlı kantar indikatör bağlantısı AKTİF",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(25, y),
            Size = new Size(580, 28),
            Checked = true
        };
        _chkKantarAktif.CheckedChanged += (_, _) => UpdateFieldsEnabledState();
        pnlBody.Controls.Add(_chkKantarAktif);
        y += 36;

        var lblHint = new Label
        {
            Text = "ℹ️ Kantarınız şube ağı veya FortiVPN arkasındaysa veriler otomatik tünel üzerinden güvenle çekilir.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(25, y),
            Size = new Size(600, 20)
        };
        pnlBody.Controls.Add(lblHint);
        y += 30;

        // Group 1: Kantar İstasyonu & Bağlantı Modu
        AddSectionHeader(pnlBody, "KANTAR İSTASYONU & AĞ YÖNLENDİRME MODU", ref y);

        AddLabel(pnlBody, "Bağlı Kantar İstasyonu / Şube Profili:", 25, y);
        _cmbKantarTanim = new ComboBox
        {
            Location = new Point(25, y + 22),
            Size = new Size(380, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbKantarTanim.SelectedIndexChanged += (_, _) => OnSelectedScaleProfileChanged();

        AddLabel(pnlBody, "Bağlantı Yöntemi:", 420, y);
        _cmbBaglantiModu = new ComboBox
        {
            Location = new Point(420, y + 22),
            Size = new Size(205, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        _cmbBaglantiModu.Items.AddRange(new object[]
        {
            "🌐 Otomatik (Lokal & VPN)",
            "🏠 Yalnızca Lokal Ağ",
            "🛡️ Yalnızca FortiVPN"
        });
        _cmbBaglantiModu.SelectedIndex = 0;

        pnlBody.Controls.AddRange(new Control[] { _cmbKantarTanim, _cmbBaglantiModu });
        y += 62;

        // Group 2: Dual IP Configuration
        AddSectionHeader(pnlBody, "LOKAL VE FORTIVPN IP YAPILANDIRMASI", ref y);

        // Row 1: Lokal IP
        AddLabel(pnlBody, "🏠 Lokal Ağ IP Adresi (Merkez / 192.168.60.*):", 25, y);
        AddLabel(pnlBody, "Port:", 480, y);

        _txtLokalIp = new TextBox
        {
            Location = new Point(25, y + 22),
            Size = new Size(435, 28),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Text = "192.168.60.116"
        };

        _numLokalPort = new NumericUpDown
        {
            Location = new Point(480, y + 22),
            Size = new Size(145, 28),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Minimum = 1,
            Maximum = 65535,
            Value = 8080
        };
        pnlBody.Controls.AddRange(new Control[] { _txtLokalIp, _numLokalPort });
        y += 58;

        // Row 2: FortiVPN IP
        AddLabel(pnlBody, "🛡️ FortiVPN / Şube IP Adresi (Dörtyol 45.* veya Mersin 31.*):", 25, y);
        AddLabel(pnlBody, "Port:", 480, y);

        _txtVpnIp = new TextBox
        {
            Location = new Point(25, y + 22),
            Size = new Size(435, 28),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Text = "192.168.45.116"
        };

        _numVpnPort = new NumericUpDown
        {
            Location = new Point(480, y + 22),
            Size = new Size(145, 28),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Minimum = 1,
            Maximum = 65535,
            Value = 8080
        };
        pnlBody.Controls.AddRange(new Control[] { _txtVpnIp, _numVpnPort });
        y += 60;

        // Group 3: Network Diagnostics Card
        AddSectionHeader(pnlBody, "BİLGİSAYAR AĞ TEŞHİSİ & FORTIVPN DURUMU", ref y);

        var pnlDiagCard = new Panel
        {
            Location = new Point(25, y),
            Size = new Size(600, 48),
            BackColor = Color.FromArgb(241, 245, 249),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12, 6, 12, 6)
        };
        _lblNetworkDiag = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(30, 41, 59),
            Text = "Ağ bağdaştırıcıları analiz ediliyor..."
        };
        pnlDiagCard.Controls.Add(_lblNetworkDiag);
        pnlBody.Controls.Add(pnlDiagCard);
        y += 56;

        // Group 4: Dual Test Actions & Results
        AddSectionHeader(pnlBody, "ÇİFT YÖNLÜ CANLI İNDİKATÖR BAĞLANTI TESTİ", ref y);

        _btnDualTest = new Button
        {
            Text = "⚡  Çift Canlı Test Yap (Lokal & FortiVPN)",
            Location = new Point(25, y),
            Size = new Size(310, 36),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnDualTest.FlatAppearance.BorderSize = 0;
        _btnDualTest.Click += async (_, _) => await RunDualConnectionTestAsync();
        pnlBody.Controls.Add(_btnDualTest);
        y += 44;

        // Results Container
        _pnlTestResults = new Panel
        {
            Location = new Point(25, y),
            Size = new Size(600, 78),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10, 8, 10, 8)
        };

        _lblLokalResult = new Label
        {
            Location = new Point(10, 10),
            Size = new Size(575, 26),
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Text = "🏠 Lokal Test: Henüz test edilmedi"
        };

        _lblVpnResult = new Label
        {
            Location = new Point(10, 40),
            Size = new Size(575, 26),
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Text = "🛡️ FortiVPN Test: Henüz test edilmedi"
        };

        _pnlTestResults.Controls.AddRange(new Control[] { _lblLokalResult, _lblVpnResult });
        pnlBody.Controls.Add(_pnlTestResults);
        y += 90;

        // 3. Footer Actions
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 12, 20, 12)
        };
        pnlFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        _btnIptal = new Button
        {
            Text = "İptal",
            Size = new Size(100, 36),
            Location = new Point(pnlFooter.Width - 120, 12),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = ModernColors.Slate700,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        _btnIptal.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnIptal.Click += (_, _) => this.Close();

        _btnKaydet = new Button
        {
            Text = "💾 Yapılandırmayı Kaydet",
            Size = new Size(200, 36),
            Location = new Point(pnlFooter.Width - 330, 12),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            BackColor = ModernColors.Emerald500,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnKaydet.FlatAppearance.BorderSize = 0;
        _btnKaydet.Click += async (_, _) => await SaveSettingsAsync();

        pnlFooter.Controls.AddRange(new Control[] { _btnIptal, _btnKaydet });

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);
    }

    private void AddSectionHeader(Panel parent, string title, ref int y)
    {
        var lbl = new Label
        {
            Text = title,
            Location = new Point(25, y),
            Size = new Size(600, 20),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105)
        };
        parent.Controls.Add(lbl);
        y += 24;
    }

    private void AddLabel(Panel parent, string text, int x, int y)
    {
        var lbl = new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85)
        };
        parent.Controls.Add(lbl);
    }

    private void UpdateFieldsEnabledState()
    {
        bool active = _chkKantarAktif.Checked;
        _cmbKantarTanim.Enabled = active;
        _cmbBaglantiModu.Enabled = active;
        _txtLokalIp.Enabled = active;
        _numLokalPort.Enabled = active;
        _txtVpnIp.Enabled = active;
        _numVpnPort.Enabled = active;
        _btnDualTest.Enabled = active;
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // 1. Analyze local network
            var diag = KantarScaleClient.GetNetworkDiagnosticInfo();
            _lblNetworkDiag.Text = $"🖥️ {diag.Summary} | Bulunan IP'ler: {string.Join(", ", diag.LocalIps)}";

            // 2. Fetch weighbridge stations
            _registeredScales = await _service.GetKantarTanimlariAsync();
            _cmbKantarTanim.Items.Clear();
            foreach (var scale in _registeredScales)
            {
                _cmbKantarTanim.Items.Add($"{scale.KantarAdi} — {scale.SubeAdi}");
            }
            _cmbKantarTanim.Items.Add("⚙️ Özel Yapılandırma / Manuel IP Girişi");

            // 3. Load user's saved settings
            CurrentSettings = await _service.GetKullaniciKantarAyariAsync(_kullaniciKodu);

            _chkKantarAktif.Checked = CurrentSettings.KantarAktif;
            _txtLokalIp.Text = CurrentSettings.LokalIp;
            _numLokalPort.Value = CurrentSettings.LokalPort > 0 ? CurrentSettings.LokalPort : 8080;
            _txtVpnIp.Text = CurrentSettings.VpnIp;
            _numVpnPort.Value = CurrentSettings.VpnPort > 0 ? CurrentSettings.VpnPort : 8080;

            // Connection Mode
            int modeIdx = CurrentSettings.BaglantiModu switch
            {
                "Lokal" => 1,
                "VPN" => 2,
                _ => 0
            };
            _cmbBaglantiModu.SelectedIndex = modeIdx;

            // Find matching scale profile
            int selIdx = _registeredScales.FindIndex(s => s.KantarKodu == CurrentSettings.SubeKodu || s.KantarAdi == CurrentSettings.KantarAdi);
            if (selIdx >= 0)
            {
                _cmbKantarTanim.SelectedIndex = selIdx;
            }
            else if (_cmbKantarTanim.Items.Count > 0)
            {
                _cmbKantarTanim.SelectedIndex = 0;
            }

            UpdateFieldsEnabledState();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ayarlar yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSelectedScaleProfileChanged()
    {
        int idx = _cmbKantarTanim.SelectedIndex;
        if (idx >= 0 && idx < _registeredScales.Count)
        {
            var sel = _registeredScales[idx];
            _txtLokalIp.Text = sel.LokalIp;
            _numLokalPort.Value = sel.LokalPort;
            _txtVpnIp.Text = sel.VpnIp;
            _numVpnPort.Value = sel.VpnPort;
        }
    }

    private async Task RunDualConnectionTestAsync()
    {
        string lokalIp = _txtLokalIp.Text.Trim();
        int lokalPort = (int)_numLokalPort.Value;
        string vpnIp = _txtVpnIp.Text.Trim();
        int vpnPort = (int)_numVpnPort.Value;

        _btnDualTest.Enabled = false;
        _btnDualTest.Text = "⏳ Test Ediliyor (Lokal & FortiVPN)...";

        _lblLokalResult.Text = $"🏠 Lokal ({lokalIp}:{lokalPort}): Sinyal taranıyor...";
        _lblLokalResult.ForeColor = Color.FromArgb(146, 64, 14);

        _lblVpnResult.Text = $"🛡️ FortiVPN ({vpnIp}:{vpnPort}): Sinyal taranıyor...";
        _lblVpnResult.ForeColor = Color.FromArgb(146, 64, 14);

        try
        {
            var res = await KantarScaleClient.TestDualConnectionAsync(lokalIp, lokalPort, vpnIp, vpnPort, 2500);

            // Lokal Result
            if (res.Lokal.success)
            {
                _lblLokalResult.Text = $"✅ Lokal ({lokalIp}:{lokalPort}): {res.Lokal.message}";
                _lblLokalResult.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else
            {
                _lblLokalResult.Text = $"❌ Lokal ({lokalIp}:{lokalPort}): {res.Lokal.message} ({res.Lokal.pingMs} ms)";
                _lblLokalResult.ForeColor = Color.FromArgb(185, 28, 28);
            }

            // VPN Result
            if (res.Vpn.success)
            {
                _lblVpnResult.Text = $"✅ FortiVPN ({vpnIp}:{vpnPort}): {res.Vpn.message}";
                _lblVpnResult.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else
            {
                _lblVpnResult.Text = $"❌ FortiVPN ({vpnIp}:{vpnPort}): {res.Vpn.message} ({res.Vpn.pingMs} ms)";
                _lblVpnResult.ForeColor = Color.FromArgb(185, 28, 28);
            }
        }
        finally
        {
            _btnDualTest.Enabled = true;
            _btnDualTest.Text = "⚡  Çift Canlı Test Yap (Lokal & FortiVPN)";
        }
    }

    private async Task SaveSettingsAsync()
    {
        CurrentSettings.KullaniciKodu = _kullaniciKodu;
        CurrentSettings.KullaniciAdi = _kullaniciAdi;
        CurrentSettings.KantarAktif = _chkKantarAktif.Checked;

        int idx = _cmbKantarTanim.SelectedIndex;
        if (idx >= 0 && idx < _registeredScales.Count)
        {
            CurrentSettings.SubeKodu = _registeredScales[idx].KantarKodu;
            CurrentSettings.KantarAdi = _registeredScales[idx].KantarAdi;
        }
        else
        {
            CurrentSettings.SubeKodu = "OZEL";
            CurrentSettings.KantarAdi = _cmbKantarTanim.Text;
        }

        CurrentSettings.LokalIp = _txtLokalIp.Text.Trim();
        CurrentSettings.LokalPort = (int)_numLokalPort.Value;
        CurrentSettings.VpnIp = _txtVpnIp.Text.Trim();
        CurrentSettings.VpnPort = (int)_numVpnPort.Value;

        CurrentSettings.BaglantiModu = _cmbBaglantiModu.SelectedIndex switch
        {
            1 => "Lokal",
            2 => "VPN",
            _ => "Auto"
        };

        bool ok = await _service.SaveKullaniciKantarAyariAsync(CurrentSettings);
        if (ok)
        {
            IsSaved = true;
            NotificationService.NotifySuccess(
                "Kantar Yapılandırması Kaydedildi",
                $"'{CurrentSettings.KantarAdi}' için Lokal ({CurrentSettings.LokalIp}) ve VPN ({CurrentSettings.VpnIp}) yönlendirme ayarları kaydedildi.",
                "MalKabul");

            MessageBox.Show("Kantar ve VPN çoklu ağ bağlantı ayarları başarıyla kaydedildi.", "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
        else
        {
            MessageBox.Show("Ayarlar kaydedilirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
