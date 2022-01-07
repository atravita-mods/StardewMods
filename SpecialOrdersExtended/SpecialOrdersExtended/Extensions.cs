using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace SpecialOrdersExtended;

internal static class LogExtensions
{

    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag
    /// Logs to verbose otherwise
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="message"></param>
    /// <param name="level"></param>
    public static void DebugLog(
        [NotNull] this IMonitor monitor,
        [NotNull] string message,
        [NotNull] LogLevel level = LogLevel.Debug)
    {
#if DEBUG
        monitor.Log(message, level);
#else
        monitor.VerboseLog(message);
#endif
    }
}
