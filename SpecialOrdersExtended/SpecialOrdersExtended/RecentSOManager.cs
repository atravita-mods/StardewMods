using SpecialOrdersExtended.DataModels;

namespace SpecialOrdersExtended;

/// <summary>
/// Handles all references to recently completed special orders.
/// </summary>
internal class RecentSOManager
{
    private static RecentCompletedSO? recentCompletedSO;
    private static List<string>? currentOrderCache;

    public static void Load() => recentCompletedSO = RecentCompletedSO.Load();

    public static void Save()
    {
        if(recentCompletedSO is null)
        {
            throw new SaveNotLoadedError();
        }
        recentCompletedSO.Save();
    }

    /// <summary>
    /// Gets all keys that were set within a certain number of days.
    /// </summary>
    /// <param name="days">current number of days played.</param>
    /// <returns>IEnumerable of keys within the given timeframe. May return null.</returns>
    [Pure]
    public static IEnumerable<string>? GetKeys(uint days) => recentCompletedSO?.GetKeys(days);

    /// <summary>
    /// Run at the end of a day, in order to remove older completed orders.
    /// </summary>
    /// <param name="daysplayed">current number of days played.</param>
    /// <exception cref="SaveNotLoadedError">Raised whenver the field is null and should not be. (Save not loaded)</exception>
    /// <remarks>Should remove orders more than seven days old.</remarks>
    public static void DayUpdate(uint daysplayed)
    {
        if(recentCompletedSO is null) { throw new SaveNotLoadedError(); }
        recentCompletedSO.dayUpdate(daysplayed);
    }

    /// <summary>
    /// Gets the newest recently completed orders. Runs every 10 in-game minutes
    /// Grabs both the current orders marked as complete and looks for orders dismissed
    /// </summary>
    /// <returns>true if an order got added to RecentCompletedSO, false otherwise</returns>
    public static bool GrabNewRecentlyCompletedOrders()
    {

        Dictionary<string, SpecialOrder>? currentOrders = Game1.player?.team?.specialOrders?.ToDictionaryIgnoreDuplicates(a => a.questKey.Value, a => a)
            ?? SaveGame.loaded?.specialOrders?.ToDictionaryIgnoreDuplicates(a => a.questKey.Value, a => a);
        if (currentOrders is null)
        { // Save is not loaded
            return false;
        }
        List<string> currentOrderKeys = currentOrders.Keys.OrderBy(a => a).ToList();

        bool updatedCache = false;

        // Check for any completed orders in the current orders.
        foreach (SpecialOrder order in currentOrders.Values)
        {
            if (order.questState.Value == SpecialOrder.QuestState.Complete)
            {
                if (TryAdd(order.questKey.Value)) { updatedCache = true; }
            }
        }
        if (currentOrderKeys == currentOrderCache) { return updatedCache; } // No one has been added or dismissed

        // Grab my completed orders
        HashSet<string>? completedOrderKeys = null;
        if (Context.IsWorldReady)
        {
            completedOrderKeys = Game1.player!.team.completedSpecialOrders.Keys.OrderBy(a => a).ToHashSet();
        }
        else
        {
            completedOrderKeys = SaveGame.loaded?.completedSpecialOrders?.OrderBy(a => a)?.ToHashSet();
        }
        if (completedOrderKeys is null) { return updatedCache; } // This should not happen, but just in case?

        // Check to see if any quest has been recently dismissed.
        if (currentOrderCache is not null)
        {
            foreach (string cachedOrder in currentOrderCache)
            {
                if (!currentOrders.ContainsKey(cachedOrder) && completedOrderKeys.Contains(cachedOrder))
                {// A quest previously in the current quests is gone now
                 // and seems to have appeared in the completed orders
                    if (TryAdd(cachedOrder)) { updatedCache = true; }
                }
            }
        }
        currentOrderCache = currentOrderKeys;
        return updatedCache;
    }

    /// <summary>
    /// Tries to add a questkey to the RecentCompletedSO data model
    /// If it's already there, does nothing.
    /// </summary>
    /// <param name="questkey"></param>
    /// <returns></returns>
    /// <exception cref="SaveNotLoadedError"></exception>
    public static bool TryAdd(string questkey)
    {
        if (!Context.IsWorldReady)
        {
            throw new SaveNotLoadedError();
        }
        return recentCompletedSO!.TryAdd(questkey, Game1.stats.DaysPlayed);
    }

    /// <summary>
    /// Attempt to remove a questkey from the RecentCompletedSO.
    /// </summary>
    /// <param name="questkey">Quest key to search for.</param>
    /// <returns>True if removed successfully, false otherwise.</returns>
    /// <exception cref="SaveNotLoadedError">If called when teh save is not loaded.</exception>
    public static bool TryRemove(string questkey)
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return recentCompletedSO!.TryRemove(questkey);
    }

    [Pure]
    public static bool IsWithinXDays(string questkey, uint days)
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return recentCompletedSO!.IsWithinXDays(questkey, days);
    }


}
