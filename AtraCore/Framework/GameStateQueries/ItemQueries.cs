using StardewValley.Delegates;

using static StardewValley.GameStateQuery;

namespace AtraCore.Framework.GameStateQueries;

/// <summary>
/// Game state queries for items.
/// </summary>
internal static class ItemQueries
{
    /// <summary>
    /// Checks to see if a specific item is an error item.
    /// </summary>
    internal static bool ErrorItem(string[] query, GameStateQueryContext context)
    {
        if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out Item item, out string error))
        {
            return Helpers.ErrorResult(query, error);
        }
        return ItemRegistry.GetData(item.QualifiedItemId) is null;
    }
}
