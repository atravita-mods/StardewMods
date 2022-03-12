using System.Diagnostics;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extension methods for SMAPI's logging service.
/// </summary>
public static class LogExtensions
{
    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag
    /// Logs to verbose otherwise.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    public static void DebugLog(
        this IMonitor monitor,
        string message,
        LogLevel level = LogLevel.Debug)
    {
#if DEBUG
        monitor.Log(message, level);
#else
        monitor.VerboseLog(message);
#endif
    }

    [Conditional("DEBUG")]
    public static void DebugOnlyLog(this IMonitor monitor, string message, LogLevel level = LogLevel.Debug)
        => monitor.Log(message, level);
}