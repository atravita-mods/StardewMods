using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ItemManagement;

using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;

using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace CatGiftsRedux.Framework;

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

        List<TerrainFeature>? hoedirt = farm.terrainFeatures.Values.Where((feat) => feat is HoeDirt dirt && dirt.crop is not null && !dirt.crop.dead.Value).ToList();

        if (hoedirt.Count == 0)
        {
            return null;
        }

        int tries = 3;
        do
        {

            Crop? crop = ((HoeDirt)Utility.GetRandom(hoedirt, random)).crop;

            // confirm the item exists.
            if (!Game1Wrappers.ObjectInfo.TryGetValue(crop.indexOfHarvest.Value, out string? objectData)
                || objectData.GetNthChunk('1', SObject.objectInfoNameIndex).Contains("Qi", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (DataToItemMap.IsActuallyRing(crop.indexOfHarvest.Value))
            {
                return new Ring(crop.indexOfHarvest.Value);
            }

            return crop.programColored.Value
                ? new ColoredObject(crop.indexOfHarvest.Value, 1, crop.tintColor.Value)
                : new SObject(crop.indexOfHarvest.Value, 1);
        }
        while (tries-- > 0);
        return null;
    }
}
