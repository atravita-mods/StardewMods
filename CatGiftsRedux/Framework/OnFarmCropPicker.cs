using AtraShared.Utils.Extensions;

using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace CatGiftsRedux.Framework;
internal static class OnFarmCropPicker
{
    internal static SObject? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Random Farm Crop");

        var farm = Game1.getFarm();

        var hoedirt = farm.terrainFeatures.Values.Where((feat) => feat is HoeDirt dirt && dirt.crop is not null && !dirt.crop.dead.Value).ToList();

        if (hoedirt.Count == 0)
        {
            return null;
        }

        var crop = ((HoeDirt)Utility.GetRandom(hoedirt, random)).crop;
        return crop.programColored.Value
            ? new ColoredObject(crop.indexOfHarvest.Value, 1, crop.tintColor.Value)
            : new SObject(crop.indexOfHarvest.Value, 1);
    }
}
