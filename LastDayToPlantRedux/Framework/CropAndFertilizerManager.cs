using AtraBase.Collections;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;
using AtraShared.ConstantsAndEnums;
using AtraShared.Wrappers;

using NetEscapades.EnumGenerators;

using StardewValley.TerrainFeatures;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Handles a cache of crop and fertilizer data.
/// </summary>
internal static class CropAndFertilizerManager
{
    private enum Profession
    {
        None,
        Agriculturalist,
        Prestiged,
    }

    private static bool cropsNeedRefreshing = true;
    private static bool fertilizersNeedRefreshing = true;
    private static StardewSeasons lastLoadedSeason = StardewSeasons.None;

    // a mapping of fertilizers to a hoedirt that has them.
    private static Dictionary<int, HoeDirt> dirts = new();

    // a cache of crop data.
    private static Dictionary<int, CropEntry> crops = new();

    // a mapping of fertilizers to their localized names.
    private static Dictionary<int, string> fertilizers = new();

    // Map conditions to the number of days it takes to grow a crop.
    private static Dictionary<CropCondition, Dictionary<int, int>> daysPerCondition = new();

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "StyleCop doesn't understand records.")]
    private record CropEntry(StardewSeasons Seasons, string GrowthData);

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "StyleCop doesn't understand records.")]
    private record CropCondition(Profession Profession, int Fertilizer);

    #region processing

    internal static void Process()
    {
        if (MultiplayerManager.PrestigedAgriculturalistFarmer is not null)
        {

        }
    }

    private static void ProcessForProfession(Profession profession)
    {

    }
    #endregion

    #region loading

    /// <summary>
    /// Requests a refresh to the crops cache.
    /// </summary>
    internal static void RequestInvalidateCrops()
    {
        cropsNeedRefreshing = true;
    }

    /// <summary>
    /// Requests a refresh to the fertilizer data.
    /// </summary>
    internal static void RequestInvalidateFertilizers()
    {
        fertilizersNeedRefreshing = true;
    }

    /// <summary>
    /// Parses crop data into a more optimized format.
    /// </summary>
    /// <returns>If any values have changed..</returns>
    private static bool LoadCropData()
    {
        if (!cropsNeedRefreshing)
        {
            return false;
        }

        cropsNeedRefreshing = false;

        Dictionary<int, CropEntry> ret = new();

        Dictionary<int, string> cropData = Game1.content.Load<Dictionary<int, string>>(AssetManager.CropName.BaseName);
        foreach ((int index, string vals) in cropData)
        {
            ReadOnlySpan<char> seasons = vals.GetNthChunk('/', 1).Trim();

            StardewSeasons seasonEnum = StardewSeasons.None;
            foreach (SpanSplitEntry season in seasons.StreamSplit(null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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

            string? growthData = vals.GetNthChunk('/', 0).Trim().ToString();
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
    private static bool LoadFertilizerData()
    {
        if (!fertilizersNeedRefreshing)
        {
            return false;
        }
        fertilizersNeedRefreshing = false;

        Dictionary<int, string> ret = new();

        foreach ((int index, string vals) in Game1Wrappers.ObjectInfo)
        {
            ReadOnlySpan<char> catName = vals.GetNthChunk('/', SObject.objectInfoTypeIndex);
            int spaceIndx = catName.GetLastIndexOfWhiteSpace();
            if (spaceIndx < 0)
            {
                continue;
            }

            if (!int.TryParse(catName[spaceIndx..], out int category) ||
                (category is not SObject.fertilizerCategory))
            {
                continue;
            }

            string? name = vals.GetNthChunk('/', SObject.objectInfoDisplayNameIndex).Trim().ToString();

            ret[index] = name;
        }

        bool changed = ret.IsEquivalentTo(fertilizers);
        fertilizers = ret;
        if (changed)
        {
            PopulateFertilizerList();
        }
        return changed;
    }

    private static void PopulateFertilizerList()
    {
        dirts.Clear();

        dirts[0] = new();

        foreach (int fert in fertilizers.Keys)
        {
            HoeDirt dirt = new();
            dirt.fertilizer.Value = fert;
            dirts[fert] = dirt;
        }
    }
    #endregion
}
