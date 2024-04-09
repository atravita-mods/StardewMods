using StardewValley.Internal;

namespace AtraCore.Framework.ItemResolvers;

/// <summary>
/// Gets the current raccoon seed, as a query.
/// </summary>
internal static class RaccoonSeedQuery
{
    /// <summary>
    /// An ItemQuery that returns the current raccoon seed.
    /// </summary>
    /// <returns>Current raccoon seeds.</returns>
    internal static IEnumerable<ItemQueryResult> Query(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
    {
        Random r = context?.Random ?? Random.Shared;
        Farmer farmer = context?.Player ?? Game1.player;

        int stack;
        if (string.IsNullOrWhiteSpace(arguments))
        {
            stack = -1;
        }
        else if (!int.TryParse(key, out stack))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"'{arguments}' is not a valid number");
            yield break;
        }

        yield return new ItemQueryResult(Utility.getRaccoonSeedForCurrentTimeOfYear(farmer, r, stack));
    }
}
