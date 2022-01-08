
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace SpecialOrdersExtended;


internal static class IEnumerableExtensions
{
    [Pure]
    public static Dictionary<TKey,TValue> ToDictionaryIgnoreDuplicates<TEnumerable,TKey,TValue>(
        [NotNull] this IEnumerable<TEnumerable> enumerable,
        [NotNull] Func<TEnumerable, TKey> keyselector,
        [NotNull] Func<TEnumerable, TValue> valueselector
        ) where TEnumerable: notnull
          where TKey : notnull
          where TValue : notnull
    {
        Dictionary<TKey,TValue> result = new();
        foreach (TEnumerable item in enumerable)
        {
            if(!result.TryAdd(keyselector(item), valueselector(item)))
            {
                ModEntry.ModMonitor.DebugLog($"Recieved duplicate key {keyselector(item)}, ignoring");
            }
        }
        return result;
    }
}
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
