using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Additional extensions on logging.
/// </summary>
public static class ExtendedLogExt
{
    #region helpers

    /// <summary>
    /// Logs a stopwatch.
    /// </summary>
    /// <param name="monitor">Monitor instance to use.</param>
    /// <param name="action">Action being performed.</param>
    /// <param name="sw">Stopwatch to log.</param>
    /// <param name="level">The level to log at.</param>
    [DebuggerHidden]
    [Conditional("TRACELOG")]
    public static void LogTimespan(this IMonitor monitor, string action, Stopwatch sw, LogLevel level = LogLevel.Info)
    {
        monitor.Log($"{action} took {sw.Elapsed.TotalMilliseconds:F2} ms.", level);
    }

    /// <summary>
    /// Logs an exception.
    /// </summary>
    /// <param name="monitor">Logging instance to use.</param>
    /// <param name="method">The current method being transpiled.</param>
    /// <param name="ex">The exception.</param>
    [DebuggerHidden]
    public static void LogTranspilerError(this IMonitor monitor, MethodBase method, Exception ex)
    {
        monitor.Log($"Mod crashed while transpiling {method.FullDescription()}, see log for details.", LogLevel.Error);
        monitor.Log(ex.ToString());
        monitor.Log($"Other patches on this method:");
        method.Snitch(monitor);
    }

    #endregion
}