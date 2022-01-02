using System.Reflection;

namespace SpecialOrdersExtended;

/// <summary>
/// Manages the handling of stats
/// Caches the available stats at first use, but clears at the end of each day.
/// </summary>
internal class StatsManager
{
    Dictionary<string, PropertyInfo> propertyInfos = new();

    //remove these stats, they make no sense.
    private readonly string[] denylist = { "AverageBedtime", "TimesUnconscious", "TotalMoneyGifted" };


    /// <summary>
    /// Populate the propertyInfos cache.
    /// </summary>
    public void GrabProperties()
    {
        propertyInfos = typeof(Stats).GetProperties()
            .Where((PropertyInfo p) => p.CanRead && p.PropertyType.Equals(typeof(uint)) && !denylist.Contains(p.Name))
            .ToDictionary((PropertyInfo p) => p.Name.ToLowerInvariant(), p => p);
    }

    public void ClearProperties() => propertyInfos.Clear();

    /// <summary>
    /// Get the value of the stat in the specific stat object.
    /// Looks through both the hardcoded stats and the stats dictionary
    /// but ignores the monster dictionary
    /// </summary>
    /// <param name="key"></param>
    /// <param name="stats">the stats to look through (which farmer do I want?)</param>
    /// <returns>value of the stat</returns>
    public uint GrabBasicProperty(string key, Stats stats)
    {
        if (propertyInfos.Count.Equals(0)) { GrabProperties(); }
        try
        {
            if (propertyInfos.TryGetValue(key.ToLowerInvariant(), out PropertyInfo property))
            {
                return (uint)property.GetValue(stats);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"{I18n.StatCacheFail(key: key, atra: "https://github.com/atravita-mods/SpecialOrdersExtended/issues")}\n\n{ex}", LogLevel.Error);
        }
        if (stats.stat_dictionary.TryGetValue(key, out uint result)) { return result; }
        ModEntry.ModMonitor.Log(I18n.StatNotFound(key), LogLevel.Trace);
        return 0u;
    }

    /// <summary>
    /// Console command to list all stats found, both hardcoded and in the stats dictionary.
    /// Note that this will include stats that other mods add as well.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    [SuppressMessage("ReSharper", "IDE0060", Justification = "Format expected by console commands")]
    public void ConsoleListProperties(string command, string[] args)
    {
        if (propertyInfos.Count.Equals(0)) { GrabProperties(); }
        ModEntry.ModMonitor.Log($"{I18n.CurrentKeysFound()}: \n    {I18n.Hardcoded()}:{String.Join(", ", Utilities.ContextSort(propertyInfos.Keys))}\n    {I18n.Dictionary()}:{String.Join(", ", Utilities.ContextSort(Game1.player.stats.stat_dictionary.Keys))}", LogLevel.Info);
    }
}
