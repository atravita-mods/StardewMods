using AtraShared.Utils.Extensions;

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
    internal static SObject? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Random Farm Crop");

        Farm? farm = Game1.getFarm();

        List<TerrainFeature>? hoedirt = farm.terrainFeatures.Values.Where((feat) => feat is HoeDirt dirt && dirt.crop is not null && !dirt.crop.dead.Value).ToList();

        if (hoedirt.Count == 0)
        {
            return null;
        }

        Crop? crop = ((HoeDirt)Utility.GetRandom(hoedirt, random)).crop;
        return crop.programColored.Value
            ? new ColoredObject(crop.indexOfHarvest.Value, 1, crop.tintColor.Value)
            : new SObject(crop.indexOfHarvest.Value, 1);
    }
}
