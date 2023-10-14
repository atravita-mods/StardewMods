namespace SpecialOrdersExtended.Managers;

/// <summary>
/// Manages the handling of stats
/// Caches the available stats at first use, but clears at the end of each day.
/// </summary>
/// <remarks>Clearing the cache should only be handled by one player in splitscreen.</remarks>
internal static class StatsManager
{
    /// <summary>
    /// Get the value of the stat in the specific stat object.
    /// Looks through both the hardcoded stats and the stats dictionary
    /// but ignores the monster dictionary.
    /// </summary>
    /// <param name="key">The key of the stat to search for.</param>
    /// <param name="stats">The stats to look through (which farmer do I want?).</param>
    /// <returns>Value of the stat.</returns>
    internal static uint GrabBasicProperty(string key, Stats stats)
    {
        if (stats.Values.TryGetValue(key, out uint result))
        {
            return result;
        }
        ModEntry.ModMonitor.Log(I18n.StatNotFound(key), LogLevel.Trace);
        return 0u;
    }
}
