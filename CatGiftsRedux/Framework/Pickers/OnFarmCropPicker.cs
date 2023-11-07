using AtraShared.Utils.Extensions;

using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace CatGiftsRedux.Framework.Pickers;

/// <summary>
/// Picks a random on-farm crop.
/// </summary>
internal static class OnFarmCropPicker
{
    /// <summary>
    /// Picks a random on-farm (living) crop.
    /// </summary>
    /// <param name="random">Random to use.</param>
    /// <returns>The product of a random crop.</returns>
    internal static Item? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Random Farm Crop");

        Farm? farm = Game1.getFarm();

        List<HoeDirt>? hoedirt = farm.terrainFeatures.Values.OfType<HoeDirt>().Where((dirt) => dirt.crop is not null && !dirt.crop.dead.Value).ToList();

        if (hoedirt.Count == 0)
        {
            return null;
        }

        int tries = 3;
        do
        {
            Crop? crop = random.ChooseFrom(hoedirt).crop;

            if (Utils.ForbiddenFromRandomPicking(crop.indexOfHarvest.Value))
            {
                continue;
            }

            return crop.programColored.Value
                ? new ColoredObject(crop.indexOfHarvest.Value, 1, crop.tintColor.Value)
                : ItemRegistry.Create(ItemRegistry.type_object + crop.indexOfHarvest.Value);
        }
        while (tries-- > 0);
        return null;
    }
}
