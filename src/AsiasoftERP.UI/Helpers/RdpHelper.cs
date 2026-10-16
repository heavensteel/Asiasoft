using System.Runtime.InteropServices;

namespace AsiasoftERP.UI.Helpers;

/// <summary>
/// Helper for Windows Terminal Services / RDP session detection
/// </summary>
public static class RdpHelper
{
    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int Reserved,
        int Version,
        ref IntPtr ppSessionInfo,
        ref int pCount);

    [DllImport("Wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WTSQuerySessionInformation(
        IntPtr hServer,
        int sessionId,
        WtsInfoClass wtsInfoClass,
        out IntPtr ppBuffer,
        out int pBytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

    private enum WtsInfoClass
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType
    }

    private enum WtsConnectState
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int SessionID;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pWinStationName;
        public WtsConnectState State;
    }

    public class RdpSession
    {
        public int SessionId { get; set; }
        public string WinStationName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool IsCurrentSession { get; set; }
        public bool IsRdp => WinStationName.StartsWith("RDP", StringComparison.OrdinalIgnoreCase);
        public bool IsConsole => WinStationName.Equals("Console", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get current process session ID
    /// </summary>
    public static int GetCurrentSessionId()
    {
        return System.Diagnostics.Process.GetCurrentProcess().SessionId;
    }

    /// <summary>
    /// Get current session name from environment (e.g. "RDP-Tcp#5", "Console")
    /// </summary>
    public static string GetCurrentSessionName()
    {
        return Environment.GetEnvironmentVariable("SESSIONNAME") ?? "Console";
    }

    /// <summary>
    /// Check if current user is connected via RDP
    /// </summary>
    public static bool IsCurrentSessionRdp()
    {
        var sessionName = GetCurrentSessionName();
        return sessionName.StartsWith("RDP", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get all active RDP/Terminal sessions on this server
    /// </summary>
    public static List<RdpSession> GetAllSessions()
    {
        var sessions = new List<RdpSession>();
        IntPtr ppSessionInfo = IntPtr.Zero;
        int count = 0;
        int currentSessionId = GetCurrentSessionId();

        try
        {
            if (!WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref ppSessionInfo, ref count))
                return sessions;

            int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
            IntPtr current = ppSessionInfo;

            for (int i = 0; i < count; i++)
            {
                var si = (WTS_SESSION_INFO)Marshal.PtrToStructure(current, typeof(WTS_SESSION_INFO))!;
                current = IntPtr.Add(current, dataSize);

                var session = new RdpSession
                {
                    SessionId = si.SessionID,
                    WinStationName = si.pWinStationName ?? string.Empty,
                    State = si.State.ToString(),
                    IsCurrentSession = si.SessionID == currentSessionId
                };

                // Get username
                if (WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, si.SessionID,
                    WtsInfoClass.WTSUserName, out IntPtr userBuffer, out _))
                {
                    session.UserName = Marshal.PtrToStringUni(userBuffer) ?? string.Empty;
                    WTSFreeMemory(userBuffer);
                }

                // Get client name
                if (WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, si.SessionID,
                    WtsInfoClass.WTSClientName, out IntPtr clientBuffer, out _))
                {
                    session.ClientName = Marshal.PtrToStringUni(clientBuffer) ?? string.Empty;
                    WTSFreeMemory(clientBuffer);
                }

                sessions.Add(session);
            }
        }
        catch
        {
            // Fallback: just add current session
            sessions.Add(new RdpSession
            {
                SessionId = currentSessionId,
                WinStationName = GetCurrentSessionName(),
                UserName = Environment.UserName,
                State = "Active",
                IsCurrentSession = true
            });
        }
        finally
        {
            if (ppSessionInfo != IntPtr.Zero)
                WTSFreeMemory(ppSessionInfo);
        }

        return sessions;
    }

    /// <summary>
    /// Get info about the current session
    /// </summary>
    public static RdpSession GetCurrentSession()
    {
        int sessionId = GetCurrentSessionId();
        string sessionName = GetCurrentSessionName();

        var session = new RdpSession
        {
            SessionId = sessionId,
            WinStationName = sessionName,
            UserName = Environment.UserName,
            ClientName = Environment.MachineName,
            State = "Active",
            IsCurrentSession = true
        };

        // Try to get client name if RDP
        if (session.IsRdp)
        {
            if (WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, sessionId,
                WtsInfoClass.WTSClientName, out IntPtr clientBuffer, out _))
            {
                session.ClientName = Marshal.PtrToStringUni(clientBuffer) ?? Environment.MachineName;
                WTSFreeMemory(clientBuffer);
            }
        }

        return session;
    }
}
