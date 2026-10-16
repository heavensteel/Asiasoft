using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Continuous asynchronous TCP client for scale indicators (Kantar / Terazi).
/// Supports multi-branch routing, FortiVPN tunneling and smart auto-failover
/// across Local (192.168.60.*) and Branch / VPN (192.168.45.*, 192.168.31.*) subnets.
/// </summary>
public class KantarScaleClient : IDisposable
{
    public static KantarScaleClient Instance { get; } = new KantarScaleClient();

    // Primary Local IP & Port (default: 192.168.60.116:8080)
    public string LokalIp { get; set; } = "192.168.60.116";
    public int LokalPort { get; set; } = 8080;

    // FortiClient VPN / Branch IP & Port (default: 192.168.45.116:8080)
    public string VpnIp { get; set; } = "192.168.45.116";
    public int VpnPort { get; set; } = 8080;

    // Connection Mode: 'Auto' (Smart Local & FortiVPN Failover), 'Lokal', 'VPN'
    public string Mode { get; set; } = "Auto";

    // Backwards compatibility properties
    public string Host
    {
        get => !string.IsNullOrEmpty(_activeHost) ? _activeHost : LokalIp;
        set => LokalIp = value;
    }

    public int Port
    {
        get => _activePort > 0 ? _activePort : LokalPort;
        set => LokalPort = value;
    }

    private string _activeHost = "192.168.60.116";
    private int _activePort = 8080;
    private string _activeRouteName = "Lokal Ağ";

    public string ActiveEndpoint => $"{_activeHost}:{_activePort} ({_activeRouteName})";
    public decimal CurrentWeight { get; private set; } = 0.0m;
    public bool IsConnected { get; private set; } = false;

    public event Action<decimal>? OnWeightReceived;
    public event Action<bool, string>? OnConnectionStateChanged;

    private CancellationTokenSource? _cts;
    private Task? _workerTask;
    private readonly object _lock = new();

    public void Start(KantarAyarDto ayar)
    {
        lock (_lock)
        {
            LokalIp = !string.IsNullOrEmpty(ayar.LokalIp) ? ayar.LokalIp : "192.168.60.116";
            LokalPort = ayar.LokalPort > 0 ? ayar.LokalPort : 8080;
            VpnIp = !string.IsNullOrEmpty(ayar.VpnIp) ? ayar.VpnIp : "192.168.45.116";
            VpnPort = ayar.VpnPort > 0 ? ayar.VpnPort : 8080;
            Mode = !string.IsNullOrEmpty(ayar.BaglantiModu) ? ayar.BaglantiModu : "Auto";

            RestartWorker();
        }
    }

    public void Start(string? host = null, int? port = null)
    {
        lock (_lock)
        {
            if (host != null)
            {
                LokalIp = host;
                _activeHost = host;
            }
            if (port != null)
            {
                LokalPort = port.Value;
                _activePort = port.Value;
            }

            RestartWorker();
        }
    }

    private void RestartWorker()
    {
        if (_workerTask != null && !_workerTask.IsCompleted)
        {
            Stop();
        }

        _cts = new CancellationTokenSource();
        _workerTask = Task.Run(() => WorkerLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _workerTask = null;
            IsConnected = false;
            OnConnectionStateChanged?.Invoke(false, "Kantar bağlantısı durduruldu.");
        }
    }

    /// <summary>
    /// Intelligently resolves the active scale endpoint based on network topology & reachability.
    /// In 'Auto' mode, probes Local and FortiVPN paths with sub-second probe.
    /// </summary>
    public async Task<(string Host, int Port, string RouteName)> ResolveActiveEndpointAsync(CancellationToken token = default)
    {
        if (Mode == "Lokal")
        {
            return (LokalIp, LokalPort, "Lokal Ağ (Sabit)");
        }
        if (Mode == "VPN")
        {
            return (VpnIp, VpnPort, "FortiVPN (Sabit)");
        }

        // Auto Mode: Detect local subnet
        var diag = GetNetworkDiagnosticInfo();

        // If local PC is directly in branch subnet (e.g. 192.168.45.*), prioritize VpnIp/Branch IP first
        bool isBranchSubnet = diag.LocalIps.Any(ip => ip.StartsWith("192.168.45.") || ip.StartsWith("192.168.31."));

        if (isBranchSubnet)
        {
            // Probe Branch / VPN IP first
            if (await ProbeTcpAsync(VpnIp, VpnPort, 800, token))
            {
                return (VpnIp, VpnPort, "Şube Ağı / FortiVPN");
            }
            if (await ProbeTcpAsync(LokalIp, LokalPort, 800, token))
            {
                return (LokalIp, LokalPort, "Merkez Tesis / Lokal");
            }
        }
        else
        {
            // Probe Local / Merkez first
            if (await ProbeTcpAsync(LokalIp, LokalPort, 800, token))
            {
                return (LokalIp, LokalPort, "Merkez Tesis / Lokal");
            }
            // Fallback to FortiVPN / Branch
            if (await ProbeTcpAsync(VpnIp, VpnPort, 800, token))
            {
                return (VpnIp, VpnPort, "Şube Ağı / FortiVPN");
            }
        }

        // Default fallback if both currently unreachable
        return (LokalIp, LokalPort, "Lokal Ağ");
    }

    private static async Task<bool> ProbeTcpAsync(string host, int port, int timeoutMs, CancellationToken token)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeoutMs);

            var connectTask = client.ConnectAsync(host, port, cts.Token).AsTask();
            await connectTask;
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Performs a one-shot quick read from the scale with auto-fallback timeout.
    /// Useful for instant manual verification.
    /// </summary>
    public async Task<(bool Success, decimal Weight, string Message)> ReadOnceAsync(int timeoutMs = 2500)
    {
        try
        {
            var (targetHost, targetPort, routeName) = await ResolveActiveEndpointAsync();

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(targetHost, targetPort);
            var delayTask = Task.Delay(timeoutMs);

            var completed = await Task.WhenAny(connectTask, delayTask);
            if (completed == delayTask || !client.Connected)
            {
                return (false, 0, $"{targetHost}:{targetPort} ({routeName}) bağlantı zaman aşımına uğradı.");
            }

            using var stream = client.GetStream();
            stream.ReadTimeout = timeoutMs;
            using var reader = new StreamReader(stream, Encoding.ASCII);

            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                return (false, 0, $"Kantar indikatöründen ({targetHost}) boş veri alındı.");
            }

            if (TryParseWeight(line, out decimal weight))
            {
                CurrentWeight = weight;
                _activeHost = targetHost;
                _activePort = targetPort;
                _activeRouteName = routeName;
                return (true, weight, $"Okundu: {weight:N1} KG ({routeName})");
            }

            return (false, 0, $"Veri ayrıştırılamadı: '{line}'");
        }
        catch (Exception ex)
        {
            return (false, 0, $"Kantar okuma hatası: {ex.Message}");
        }
    }

    private async Task WorkerLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                // Intelligently resolve endpoint for this connection attempt
                var (targetHost, targetPort, routeName) = await ResolveActiveEndpointAsync(token);
                _activeHost = targetHost;
                _activePort = targetPort;
                _activeRouteName = routeName;

                OnConnectionStateChanged?.Invoke(false, $"Kantar bağlanıyor ({targetHost}:{targetPort} - {routeName})...");
                client = new TcpClient();

                // Connect with timeout
                var connectTask = client.ConnectAsync(targetHost, targetPort);
                var delayTask = Task.Delay(3000, token);
                var completed = await Task.WhenAny(connectTask, delayTask);

                if (completed == delayTask || !client.Connected)
                {
                    throw new TimeoutException($"Kantar ({targetHost}:{targetPort} - {routeName}) bağlantı zaman aşımı.");
                }

                IsConnected = true;
                OnConnectionStateChanged?.Invoke(true, $"Canlı Bağlantı Aktif ({targetHost}:{targetPort} via {routeName})");

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII);

                while (!token.IsCancellationRequested && client.Connected)
                {
                    string? line = await reader.ReadLineAsync(token);
                    if (line == null)
                    {
                        // Socket closed by scale
                        break;
                    }

                    if (TryParseWeight(line, out decimal weight))
                    {
                        CurrentWeight = weight;
                        OnWeightReceived?.Invoke(weight);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnConnectionStateChanged?.Invoke(false, $"Kantar hatası ({_activeHost}:{_activePort}): {ex.Message}. Yeniden deneniyor...");
            }
            finally
            {
                IsConnected = false;
                try { client?.Close(); client?.Dispose(); } catch { }
            }

            if (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2500, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        IsConnected = false;
        OnConnectionStateChanged?.Invoke(false, "Bağlantı kapalı");
    }

    public static bool TryParseWeight(string raw, out decimal weight)
    {
        weight = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var sb = new StringBuilder();
        foreach (char c in raw)
        {
            if (char.IsDigit(c) || c == '.' || c == ',' || c == '-')
            {
                sb.Append(c);
            }
        }

        string cleaned = sb.ToString().Trim().Replace(',', '.');
        if (string.IsNullOrEmpty(cleaned)) return false;

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out weight);
    }

    public static async Task<(bool success, decimal weight, string message, long pingMs)> TestConnectionSingleAsync(string host, int port, int timeoutMs = 2500)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(timeoutMs);

            var connectTask = client.ConnectAsync(host, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeoutMs, cts.Token));

            if (completedTask != connectTask || !client.Connected)
            {
                sw.Stop();
                return (false, 0m, $"Bağlantı zaman aşımı ({host}:{port})", sw.ElapsedMilliseconds);
            }

            using var stream = client.GetStream();
            stream.ReadTimeout = timeoutMs;
            byte[] buffer = new byte[256];

            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            sw.Stop();

            if (bytesRead <= 0)
            {
                return (false, 0m, "Soket açık fakat veri akışı yok", sw.ElapsedMilliseconds);
            }

            string raw = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            if (TryParseWeight(raw, out decimal weight))
            {
                return (true, weight, $"Sinyal Başarılı! Okunan: {weight:N1} KG ({sw.ElapsedMilliseconds} ms)", sw.ElapsedMilliseconds);
            }

            return (true, 0m, $"Sinyal Başarılı ({sw.ElapsedMilliseconds} ms)", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, 0m, $"Bağlantı Hatası: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    public static async Task<(bool ok, decimal weight, string message)> TestConnectionAsync(string host, int port, int timeoutMs = 3000)
    {
        var res = await TestConnectionSingleAsync(host, port, timeoutMs);
        return (res.success, res.weight, res.message);
    }

    /// <summary>
    /// Concurrently tests both Local and FortiVPN paths and returns comparative diagnostics.
    /// </summary>
    public static async Task<DualTestResult> TestDualConnectionAsync(string lokalIp, int lokalPort, string vpnIp, int vpnPort, int timeoutMs = 2500)
    {
        var lokalTask = TestConnectionSingleAsync(lokalIp, lokalPort, timeoutMs);
        var vpnTask = TestConnectionSingleAsync(vpnIp, vpnPort, timeoutMs);

        await Task.WhenAll(lokalTask, vpnTask);

        return new DualTestResult
        {
            Lokal = await lokalTask,
            Vpn = await vpnTask
        };
    }

    /// <summary>
    /// Analyzes local PC network adapters to detect FortiVPN, local subnets and active IPs.
    /// </summary>
    public static NetworkDiagnosticInfo GetNetworkDiagnosticInfo()
    {
        var info = new NetworkDiagnosticInfo();

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            foreach (var ni in interfaces)
            {
                string desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                bool isVpn = desc.Contains("forti") || desc.Contains("vpn") || desc.Contains("tunnel") || desc.Contains("ppp");
                if (isVpn) info.HasFortiVpn = true;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipStr = ua.Address.ToString();
                        info.LocalIps.Add(ipStr);

                        if (isVpn && string.IsNullOrEmpty(info.DetectedAdapter))
                        {
                            info.DetectedAdapter = $"{ni.Name} ({ipStr})";
                        }
                    }
                }
            }

            if (info.LocalIps.Any(ip => ip.StartsWith("192.168.60.")))
            {
                info.ActiveSubnet = "192.168.60.* (Merkez Lokal Ağ)";
            }
            else if (info.LocalIps.Any(ip => ip.StartsWith("192.168.45.")))
            {
                info.ActiveSubnet = "192.168.45.* (Dörtyol Şube / FortiVPN)";
            }
            else if (info.LocalIps.Any(ip => ip.StartsWith("192.168.31.")))
            {
                info.ActiveSubnet = "192.168.31.* (Mersin Şube / FortiVPN)";
            }
            else if (info.LocalIps.Any(ip => ip.StartsWith("192.168.1.")))
            {
                info.ActiveSubnet = "192.168.1.* (Ana Veritabanı / Merkez Ağ)";
            }
            else
            {
                info.ActiveSubnet = info.LocalIps.FirstOrDefault() ?? "Ağ Tespit Edilemedi";
            }

            string vpnStatus = info.HasFortiVpn ? "🛡️ FortiClient VPN Aktif" : "🏠 Yerel Ağ Bağlantısı";
            info.Summary = $"{vpnStatus} | Bulunulan Blok: {info.ActiveSubnet}";
        }
        catch (Exception ex)
        {
            info.Summary = $"Ağ tespiti yapılamadı: {ex.Message}";
        }

        return info;
    }

    public void Dispose()
    {
        Stop();
    }
}

public class DualTestResult
{
    public (bool success, decimal weight, string message, long pingMs) Lokal { get; set; }
    public (bool success, decimal weight, string message, long pingMs) Vpn { get; set; }
}
