using AtraBase.Collections;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;
using AtraShared.ConstantsAndEnums;
using AtraShared.Wrappers;

using StardewValley.TerrainFeatures;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Handles a cache of crop and fertilizer data.
/// </summary>
internal static class CropAndFertilizerManager
{
    private static bool cropsNeedRefreshing = true;
    private static bool fertilizersNeedRefreshing = true;

    // a mapping of fertilizers to a hoedirt that has them.
    private static Dictionary<int, HoeDirt> dirts = new();

    // a cache of crop data.
    private static Dictionary<int, CropEntry> crops = new();

    // a mapping of fertilizers to their localized names.
    private static Dictionary<int, string> fertilizers = new();

    private record CropEntry(StardewSeasons Seasons, string GrowthData);


#region loading

    internal static void RequestInvalidateCrops()
    {
        cropsNeedRefreshing = true;
    }

    internal static void RequestInvalidateFertilizers()
    {
        fertilizersNeedRefreshing = true;
    }

    /// <summary>
    /// Parses crop data into a more optimized format.
    /// </summary>
    /// <returns>If any values have changed..</returns>
    internal static bool LoadCropData()
    {
        if (!cropsNeedRefreshing)
        {
            return false;
        }

        cropsNeedRefreshing = false;

        Dictionary<int, CropEntry> ret = new();

        Dictionary<int, string> cropData = Game1.content.Load<Dictionary<int, string>>("Data\\Crops");
        foreach (var (index, vals) in cropData)
        {
            var seasons = vals.GetNthChunk('/', 1).Trim();

            StardewSeasons seasonEnum = StardewSeasons.None;
            foreach (var season in seasons.StreamSplit(null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (StardewSeasonsExtensions.TryParse(season, ignoreCase: true, out StardewSeasons s))
                {
                    seasonEnum |= s;
                }
                else
                {
                    ModEntry.ModMonitor.Log($"Crop {index} - {vals} seems to have unparseable season data, skipping", LogLevel.Warn);
                    goto breakcontinue;
                }
            }

            var growthData = vals.GetNthChunk('/', 0).Trim().ToString();
            ret[index] = new CropEntry(seasonEnum, growthData);
breakcontinue:
            ;
        }

        bool changed = !ret.IsEquivalentTo(crops);
        crops = ret;
        return changed;
    }

    /// <summary>
    /// Loads a list of fertilizers.
    /// </summary>
    /// <returns>If anything changed.</returns>
    internal static bool LoadFertilizerData()
    {
        if (!fertilizersNeedRefreshing)
        {
            return false;
        }
        fertilizersNeedRefreshing = false;

        Dictionary<int, string> ret = new();

        var data = Game1Wrappers.ObjectInfo;

        foreach (var (index, vals) in data)
        {
            var catName = vals.GetNthChunk('/', SObject.objectInfoTypeIndex);
            var spaceIndx = catName.GetLastIndexOfWhiteSpace();
            if (spaceIndx < 0)
            {
                continue;
            }

            int category;
            if (!int.TryParse(catName.Slice(spaceIndx), out category) ||
                (category is not SObject.fertilizerCategory))
            {
                continue;
            }

            var name = vals.GetNthChunk('/', SObject.objectInfoDisplayNameIndex).Trim().ToString();

            ret[index] = name;
        }

        bool changed = ret.IsEquivalentTo(fertilizers);
        fertilizers = ret;
        return changed;
    }
#endregion
}
