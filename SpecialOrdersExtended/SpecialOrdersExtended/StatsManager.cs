using System.Reflection;

namespace SpecialOrdersExtended;

/// <summary>
/// Manages the handling of stats
/// Caches the available stats at first use, but clears at the end of each day.
/// </summary>
internal class StatsManager
{
    // Remove these stats, they make no sense.
    private readonly string[] denylist = { "AverageBedtime", "TimesUnconscious", "TotalMoneyGifted" };

    private Dictionary<string, Func<Stats, uint>> propertyGetters = new();

    /// <summary>
    /// Populate the propertyInfos cache.
    /// </summary>
    public void GrabProperties()
    {
#pragma warning disable CS8604 // Possible null reference argument.  - Not actually possible, the Where should filter out any that don't have a Get.
        this.propertyGetters = typeof(Stats).GetProperties()
            .Where((PropertyInfo p) => p.CanRead && p.PropertyType.Equals(typeof(uint)) && !this.denylist.Contains(p.Name))
            .ToDictionary((PropertyInfo p) => p.Name.ToLowerInvariant(), p => (Func<Stats, uint>)Delegate.CreateDelegate(typeof(Func<Stats, uint>), p.GetGetMethod()));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    /// <summary>
    /// Clears the list of possible stats.
    /// </summary>
    public void ClearProperties() => this.propertyGetters.Clear();

    /// <summary>
    /// Get the value of the stat in the specific stat object.
    /// Looks through both the hardcoded stats and the stats dictionary
    /// but ignores the monster dictionary.
    /// </summary>
    /// <param name="key">The key of the stat to search for.</param>
    /// <param name="stats">The stats to look through (which farmer do I want?).</param>
    /// <returns>Value of the stat.</returns>
    public uint GrabBasicProperty(string key, Stats stats)
    {
        if (this.propertyGetters.Count.Equals(0))
        {
            this.GrabProperties();
        }
        try
        {
            if (this.propertyGetters.TryGetValue(key.ToLowerInvariant(), out Func<Stats, uint>? property))
            {
                return property(stats);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"{I18n.StatCacheFail(key: key, atra: "https://github.com/atravita-mods/SpecialOrdersExtended/issues")}\n\n{ex}", LogLevel.Error);
        }
        if (stats.stat_dictionary.TryGetValue(key, out uint result))
        {
            return result;
        }
        ModEntry.ModMonitor.Log(I18n.StatNotFound(key), LogLevel.Trace);
        return 0u;
    }

    /// <summary>
    /// Console command to list all stats found, both hardcoded and in the stats dictionary.
    /// Note that this will include stats that other mods add as well.
    /// </summary>
    /// <param name="command">The name of the command.</param>
    /// <param name="args">Any arguments (none for this command).</param>
    [SuppressMessage("ReSharper", "IDE0060", Justification = "Format expected by console commands")]
    public void ConsoleListProperties(string command, string[] args)
    {
        if (this.propertyGetters.Count.Equals(0))
        {
            this.GrabProperties();
        }
        ModEntry.ModMonitor.Log($"{I18n.CurrentKeysFound()}: \n    {I18n.Hardcoded()}:{string.Join(", ", Utilities.ContextSort(this.propertyGetters.Keys))}\n    {I18n.Dictionary()}:{string.Join(", ", Utilities.ContextSort(Game1.player.stats.stat_dictionary.Keys))}", LogLevel.Info);
    }
}
