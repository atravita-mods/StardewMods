
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace SpecialOrdersExtended;

/// <summary>
/// LINQ-like extensions on enumerables.
/// </summary>
internal static class IEnumerableExtensions
{
    /// <summary>
    /// Similar to LINQ's ToDictionary, but ignores duplicates instead of erroring.
    /// </summary>
    /// <typeparam name="TEnumerable"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="keyselector"></param>
    /// <param name="valueselector"></param>
    /// <returns></returns>
    [Pure]
    public static Dictionary<TKey, TValue> ToDictionaryIgnoreDuplicates<TEnumerable, TKey, TValue>(
        [NotNull] this IEnumerable<TEnumerable> enumerable,
        [NotNull] Func<TEnumerable, TKey> keyselector,
        [NotNull] Func<TEnumerable, TValue> valueselector)
        where TEnumerable : notnull
        where TKey : notnull
        where TValue : notnull
    {
        Dictionary<TKey, TValue> result = new();
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

/// <summary>
/// Extensions to SMAPI's IMonitor.
/// </summary>
internal static class LogExtensions
{
    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag.
    /// Logs to verbose otherwise.
    /// </summary>
    /// <param name="monitor">The SMAPI logging Monitor.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="level">The <see cref="LogLevel"/> to log at.</param>
    public static void DebugLog(
        [NotNull] this IMonitor monitor,
        [NotNull] string message,
        [NotNull] LogLevel level = LogLevel.Debug) =>
#if DEBUG
        monitor.Log(message, level);
#else
        monitor.VerboseLog(message);
#endif

}
