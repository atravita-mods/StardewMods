using StardewValley.Delegates;
using StardewValley.Internal;

namespace SlightlyMoreDehardcoding.Models;
public sealed class ExtendedMailData
{
    public List<ExtendedItemSpawn> Items = [];

    internal IEnumerable<ItemQueryResult> Get(GameLocation? location, Farmer? farmer, string? source)
    {
        HashSet<string> seen = [];

        GameStateQueryContext queryContext = new (
            location ?? Game1.currentLocation,
            farmer ?? Game1.player,
            null,
            null,
            Random.Shared,
            customFields: new () {["LetterId"] = source});

        ItemQueryContext itemQueryContext = new(
            queryContext.Location,
            queryContext.Player,
            Random.Shared,
            $"letter-extensions: {source}");

        foreach (var spawn in this.Items)
        {
            foreach (var item in spawn.Get(queryContext, itemQueryContext, seen))
            {
                yield return item;
            }
        }
    }
}
