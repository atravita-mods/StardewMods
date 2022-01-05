using System.Text;

namespace SpecialOrdersExtended.DataModels;

internal class RecentCompletedSO : AbstractDataModel
{
    /// <summary>
    /// constant identifier in filename.
    /// </summary>
    private const string identifier = "_SOmemory";

    /// <summary>
    /// Dictionary of Recent Order questkeys & the day they were completed.
    /// </summary>
    public Dictionary<string, uint> RecentOrdersCompleted { get; set; } = new();

    public RecentCompletedSO(string savefile)
    {
        this.Savefile = savefile;
    }

    public static RecentCompletedSO Load()
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return ModEntry.DataHelper.ReadGlobalData<RecentCompletedSO>(Constants.SaveFolderName + identifier)
            ?? new RecentCompletedSO(Constants.SaveFolderName);
    }

    public void Save()
    {
        base.Save(identifier);
    }

    /// <summary>
    /// Removes any quest that was completed more than seven days ago.
    /// </summary>
    /// <param name="daysPlayed"></param>
    [SuppressMessage("ReSharper", "IDE1006", Justification = "Method naming follows convention used in-game")]
    public void dayUpdate(uint daysPlayed)
    {
        foreach (string key in RecentOrdersCompleted.Keys)
        {
            if (daysPlayed > RecentOrdersCompleted[key] + 7)
            {
                RecentOrdersCompleted.Remove(key);
            }
        }
    }

    /// <summary>
    /// Tries to add a quest key to the data model
    /// </summary>
    /// <param name="orderKey"></param>
    /// <param name="daysPlayed"></param>
    /// <returns>true if the quest key was successfully added, false otherwise</returns>
    public bool TryAdd(string orderKey, uint daysPlayed) => RecentOrdersCompleted.TryAdd(orderKey, daysPlayed);

    public bool TryRemove(string orderKey) => RecentOrdersCompleted.Remove(orderKey);


    public bool IsWithinXDays(string orderKey, uint days)
    {
        if (RecentOrdersCompleted.TryGetValue(orderKey, out uint dayCompleted))
        {
            return dayCompleted + days > Game1.stats.daysPlayed;
        }
        return false;
    }

    /// <summary>
    /// Gets all keys that were set within a certain number of days.
    /// </summary>
    /// <param name="days"></param>
    /// <returns>IEnumerable of keys within the given timeframe.</returns>
    public IEnumerable<string> GetKeys(uint days)
    {
        return RecentOrdersCompleted.Keys
            .Where(a => RecentOrdersCompleted[a] + days >= Game1.stats.DaysPlayed);
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"RecentCompletedSO{Savefile}");
        foreach (string key in Utilities.ContextSort(RecentOrdersCompleted.Keys))
        {
            stringBuilder.AppendLine($"{key} completed on Day {RecentOrdersCompleted[key]}");
        }
        return stringBuilder.ToString();
    }
}
