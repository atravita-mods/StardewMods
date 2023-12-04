// Ignore Spelling: pred

namespace AtraShared.Utils.Extensions;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using AtraBase.Internal;
using AtraBase.Toolkit;

using AtraShared.Internal;

using HarmonyLib;

/// <summary>
/// Extension methods for SMAPI's logging service.
/// </summary>
public static class LogExtensions
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
    /// <param name="action">The current actions being taken when the exception happened.</param>
    /// <param name="ex">The exception.</param>
    [DebuggerHidden]
    public static void LogError(this IMonitor monitor, string action, Exception? ex)
    {
        monitor.Log($"Mod failed while {action}, see log for details.", LogLevel.Error);
        if (ex is not null)
        {
            monitor.Log(ex.ToString());
        }
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

    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag
    /// Logs to verbose otherwise.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    [DebuggerHidden]
    [MethodImpl(TKConstants.Hot)]
    public static void DebugLog(this IMonitor monitor, string message, LogLevel level = LogLevel.Debug) =>
#if DEBUG
        monitor.Log(message, level);
#else
        monitor.VerboseLog(message);
#endif

    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the TRACE flag only.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    [DebuggerHidden]
    [Conditional("TRACELOG")]
    [MethodImpl(TKConstants.Hot)]
    public static void TraceOnlyLog(this IMonitor monitor, string message, LogLevel level = LogLevel.Debug)
        => monitor.Log(message, level);

    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag only.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    [DebuggerHidden]
    [Conditional("DEBUG")]
    [MethodImpl(TKConstants.Hot)]
    public static void DebugOnlyLog(this IMonitor monitor, string message, LogLevel level = LogLevel.Debug)
        => monitor.Log(message, level);

    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag only.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="pred">Whether to log or not.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    /// <remarks>This exists because the entire function call is removed when compiled not debug
    /// including the predicate code.</remarks>
    [DebuggerHidden]
    [Conditional("DEBUG")]
    [MethodImpl(TKConstants.Hot)]
    public static void DebugOnlyLog(this IMonitor monitor, bool pred, string message, LogLevel level = LogLevel.Debug)
    {
        if (pred)
        {
            monitor.Log(message, level);
        }
    }

    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag only.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="pred">Whether to log or not.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    /// <remarks>This exists because the entire function call is removed when compiled not debug
    /// including the predicate code.</remarks>
    [DebuggerHidden]
    [Conditional("DEBUG")]
    [MethodImpl(TKConstants.Hot)]
    public static void DebugOnlyLog(this IMonitor monitor, bool pred, [InterpolatedStringHandlerArgument("pred")] ref PredicateInterpolatedStringHandler message, LogLevel level = LogLevel.Debug)
    {
        if (pred)
        {
            monitor.Log(message.ToString(), level);
        }
    }

    /// <summary>
    /// Logs only if verbose is enabled.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    [DebuggerHidden]
    [MethodImpl(TKConstants.Hot)]
    public static void LogIfVerbose(this IMonitor monitor, [InterpolatedStringHandlerArgument("monitor")] ref SmapiInterpolatedStringHandler message, LogLevel level = LogLevel.Trace)
    {
        if (monitor.IsVerbose)
        {
            monitor.Log(message.ToString(), level);
        }
    }
}