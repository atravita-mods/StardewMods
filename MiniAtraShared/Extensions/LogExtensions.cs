// Ignore Spelling: pred

namespace MiniAtraShared.Extensions;

using System.Diagnostics;
using System.Runtime.CompilerServices;

using MiniAtraShared.Models;

/// <summary>
/// Extension methods for SMAPI's logging service.
/// </summary>
public static class LogExtensions
{
    private const MethodImplOptions Hot = MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining;

    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag
    /// Logs to verbose otherwise.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    [DebuggerHidden]
    [MethodImpl(Hot)]
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
    [MethodImpl(Hot)]
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
    [MethodImpl(Hot)]
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
    [MethodImpl(Hot)]
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
    [MethodImpl(Hot)]
    public static void DebugOnlyLog(this IMonitor monitor, bool pred, [InterpolatedStringHandlerArgument("pred")] ref PredicateInterpolatedStringHandler message, LogLevel level = LogLevel.Debug)
    {
        if (pred)
        {
            monitor.Log(message.ToString(), level);
        }
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
        monitor.Log($"Error while {action}, see log for details.", LogLevel.Error);
        if (ex is not null)
        {
            monitor.Log(ex.ToString());
        }
    }
}