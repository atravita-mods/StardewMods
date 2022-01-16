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

    public RecentCompletedSO(string savefile): base(savefile)
    {
    }

    public static RecentCompletedSO Load()
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return ModEntry.DataHelper.ReadGlobalData<RecentCompletedSO>(Constants.SaveFolderName + identifier)
            ?? new RecentCompletedSO(Constants.SaveFolderName);
    }

    public static RecentCompletedSO LoadTempIfAvailable()
    {
        throw new NotImplementedException();
    }

    public void SaveTemp() => base.SaveTemp(identifier);

    public void Save() => base.Save(identifier);


    /// <summary>
    /// Removes any quest that was completed more than seven days ago.
    /// </summary>
    /// <param name="daysPlayed">Total number of days played.</param>
    /// <returns>A list of removed keys.</returns>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Follows convention used in game.")]
    public List<string> dayUpdate(uint daysPlayed)
    {
        List<string> keysRemoved = new();
        foreach (string key in this.RecentOrdersCompleted.Keys)
        {
            if (daysPlayed > this.RecentOrdersCompleted[key] + 7)
            {
                this.RecentOrdersCompleted.Remove(key);
                keysRemoved.Add(key);
            }
        }
        return keysRemoved;
    }

    /// <summary>
    /// Tries to add a quest key to the data model
    /// </summary>
    /// <param name="orderKey"></param>
    /// <param name="daysPlayed"></param>
    /// <returns>true if the quest key was successfully added, false otherwise</returns>
    public bool TryAdd(string orderKey, uint daysPlayed) => this.RecentOrdersCompleted.TryAdd(orderKey, daysPlayed);

    public bool TryRemove(string orderKey) => this.RecentOrdersCompleted.Remove(orderKey);

    [Pure]
    public bool IsWithinXDays(string orderKey, uint days)
    {
        if (this.RecentOrdersCompleted.TryGetValue(orderKey, out uint dayCompleted))
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
    [Pure]
    public IEnumerable<string> GetKeys(uint days)
    {
        return this.RecentOrdersCompleted.Keys
            .Where(a => this.RecentOrdersCompleted[a] + days >= Game1.stats.DaysPlayed);
    }

    [Pure]
    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"RecentCompletedSO{this.Savefile}");
        foreach (string key in Utilities.ContextSort(this.RecentOrdersCompleted.Keys))
        {
            stringBuilder.AppendLine($"{key} completed on Day {this.RecentOrdersCompleted[key]}");
        }
        return stringBuilder.ToString();
    }
}
