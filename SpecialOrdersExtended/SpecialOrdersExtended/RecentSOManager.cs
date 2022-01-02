using SpecialOrdersExtended.DataModels;

namespace SpecialOrdersExtended;

internal class RecentSOManager
{
    private static RecentCompletedSO recentCompletedSO;
    private static List<string> currentOrderCache;

    public static void Load() => recentCompletedSO = RecentCompletedSO.Load();

    public static void Save() => recentCompletedSO.Save();

    /// <summary>
    /// Gets all keys that were set within a certain number of days.
    /// </summary>
    /// <param name="days"></param>
    /// <returns>IEnumerable of keys within the given timeframe. May return null.</returns>
    public static IEnumerable<string> GetKeys(uint days) => recentCompletedSO?.GetKeys(days);

    public static void DayUpdate(uint daysplayed)
    {
        recentCompletedSO.dayUpdate(daysplayed);
    }

    public static bool UpdateCurrentOrderCache()
    {
        Dictionary<string, SpecialOrder> currentOrders = Game1.player?.team?.specialOrders?.ToDictionary(a => a.questKey.Value, a => a)
            ?? SaveGame.loaded?.specialOrders?.ToDictionary(a => a.questKey.Value, a => a);
        if (currentOrders is null) { return false; }
        List<string> currentOrderKeys = currentOrders.Keys.OrderBy(a => a).ToList();
        bool updatedCache = false;
        foreach (SpecialOrder order in currentOrders.Values)
        {
            if (order.questState.Value == SpecialOrder.QuestState.Complete)
            {
                if (Add(order.questKey.Value)) { updatedCache = true; }
            }
        }
        if (currentOrderKeys == currentOrderCache) { return updatedCache; }
        HashSet<string> completedOrderKeys = null;
        if (Context.IsWorldReady)
        {
            completedOrderKeys = Game1.player.team.completedSpecialOrders.Keys.OrderBy(a => a).ToHashSet();
        }
        else
        {
            completedOrderKeys = SaveGame.loaded?.completedSpecialOrders?.OrderBy(a => a)?.ToHashSet();
        }
        if (completedOrderKeys is null) { return updatedCache; }
        if (currentOrderCache is not null)
        {
            foreach (string cachedOrder in currentOrderCache)
            {
                if (!currentOrders.ContainsKey(cachedOrder) && completedOrderKeys.Contains(cachedOrder))
                {//A quest previously in the current quests is gone now
                 //and seems to have appeared in the completed orders
                    if (Add(cachedOrder)) { updatedCache = true; }
                }
            }
        }
        currentOrderCache = currentOrderKeys;
        return updatedCache;
    }

    public static bool Add(string questkey)
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return recentCompletedSO.Add(questkey, Game1.stats.DaysPlayed);
    }

    public static bool Remove(string questkey)
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return recentCompletedSO.Remove(questkey);
    }

    public static bool IsWithinXDays(string questkey, uint days)
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return recentCompletedSO.IsWithinXDays(questkey, days);
    }


}
