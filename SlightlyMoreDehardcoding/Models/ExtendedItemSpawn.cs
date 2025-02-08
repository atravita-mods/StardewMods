using StardewValley.Delegates;
using StardewValley.GameData;
using StardewValley.Internal;

namespace SlightlyMoreDehardcoding.Models;
public class ExtendedItemSpawn: GenericSpawnItemDataWithCondition
{
    public ItemQuerySearchMode SelectionMode { get; set; } = ItemQuerySearchMode.FirstOfTypeItem;

    public bool AvoidRepeat = false;

    internal IEnumerable<ItemQueryResult> Get(GameStateQueryContext queryContext, ItemQueryContext itemContext, HashSet<string>? seen)
        => GameStateQuery.CheckConditions(this.Condition, queryContext) ?
        ItemQueryResolver.TryResolve(
            this,
            itemContext,
            filter: this.SelectionMode,
            avoidRepeat: this.AvoidRepeat,
            avoidItemIds: this.AvoidRepeat ? seen : null)
        : Enumerable.Empty<ItemQueryResult>();
}
