using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ItemManagement;

using AtraShared.Utils.Extensions;

using StardewValley.Extensions;
using StardewValley.GameData.FarmAnimals;
using StardewValley.Objects;

namespace CatGiftsRedux.Framework.Pickers;

/// <summary>
/// Tries to pick a random animal product.
/// </summary>
internal static class AnimalProductPicker
{
    /// <summary>
    /// Pick a random animal product.
    /// </summary>
    /// <param name="random">The seeded random.</param>
    /// <returns>An item.</returns>
    internal static Item? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Animal Products");

        IDictionary<string, FarmAnimalData> data = Game1.farmAnimalData;
        if (data.Count == 0)
        {
            return null;
        }

        int tries = 3;
        do
        {
            FarmAnimalData randomAnimal = data.Values.ElementAt(random.Next(data.Count));
            FarmAnimalProduce product = random.ChooseFrom(randomAnimal.ProduceItemIds);
            if (!Utils.ForbiddenFromRandomPicking(product.ItemId)
                && GameStateQuery.CheckConditions(product.Condition, Game1.getFarm(), random: random)
                && ItemRegistry.Create(ItemRegistry.type_object + product.ItemId) is SObject obj)
            {
                return obj;
            }
        }
        while (tries-- > 0);
        return null;
    }
}
