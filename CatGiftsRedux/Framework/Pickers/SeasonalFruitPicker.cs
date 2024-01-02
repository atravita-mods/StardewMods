using AtraShared.Utils.Extensions;

using StardewValley.Extensions;

namespace CatGiftsRedux.Framework.Pickers;

/// <summary>
/// Picks an appropriate seasonal fruit.
/// </summary>
internal static class SeasonalFruitPicker
{
    internal static Item? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Seasonal Fruit");

        List<StardewValley.GameData.FruitTrees.FruitTreeData> content = Game1.fruitTreeData.Values.Where(tree => tree.Seasons.Contains(Game1.season)).ToList();

        if (content.Count == 0)
        {
            return null;
        }

        int tries = 3;
        do
        {
            StardewValley.GameData.FruitTrees.FruitTreeData fruit = random.ChooseFrom(content);
            StardewValley.GameData.FruitTrees.FruitTreeFruitData drop = random.ChooseFrom(fruit.Fruit);

            if (!Utils.ForbiddenFromRandomPicking(drop.ItemId) && !GameStateQuery.CheckConditions(drop.Condition, Game1.getFarm(), random: random)
                && ItemRegistry.Create(ItemRegistry.type_object + drop.ItemId) is SObject obj)
            {
                return obj;
            }
        }
        while (tries-- > 3);
        return null;
    }
}
