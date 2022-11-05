using AtraBase.Collections;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.Shims;
using AtraShared.Wrappers;

using StardewValley.TerrainFeatures;

namespace LastDayToPlantRedux.Framework;

// fertilizers are filtered while loading, while seeds (which I expect more change for) are filtered while calculating.

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
    private static Dictionary<int, DummyHoeDirt> dirts = new();

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
        if (!StardewSeasonsExtensions.TryParse(Game1.currentSeason, ignoreCase: true, out StardewSeasons currentSeason))
        {
            ModEntry.ModMonitor.Log($"Could not parse season {Game1.currentSeason}, what?");
            return;
        }

        // if there is a season change, or if our backing data has changed, dump the cache.
        if (currentSeason != lastLoadedSeason | LoadCropData() | LoadFertilizerData())
        {
            daysPerCondition.Clear();
        }

        StardewSeasons nextSeason = currentSeason.GetNextSeason();
        ModEntry.ModMonitor.DebugOnlyLog($"Checking for {currentSeason} - next is {nextSeason}", LogLevel.Info);

        // load relevant crops.
        List<int>? currentCrops = crops.Where(c => c.Value.Seasons.HasFlag(currentSeason) && !c.Value.Seasons.HasFlag(nextSeason) && FilterCropsToUserConfig(c.Key))
                                .Select(c => c.Key)
                                .ToList();
        ModEntry.ModMonitor.DebugOnlyLog($"Found {currentCrops.Count} relevant crops");

        if (MultiplayerManager.PrestigedAgriculturalistFarmer?.TryGetTarget(out Farmer? prestiged) == true)
        {
            ProcessForProfession(Profession.Prestiged, currentCrops, prestiged);

            if (!Context.IsMultiplayer)
            {
                return;
            }
        }

        if (MultiplayerManager.AgriculturalistFarmer?.TryGetTarget(out Farmer? agriculturalist) == true)
        {
            ProcessForProfession(Profession.Agriculturalist, currentCrops, agriculturalist);

            if (!Context.IsMultiplayer)
            {
                return;
            }
        }

        if (MultiplayerManager.NormalFarmer?.TryGetTarget(out Farmer? normal) == true)
        {
            ProcessForProfession(Profession.None, currentCrops, normal);
            return;
        }

        ModEntry.ModMonitor.Log($"No available farmers for data analysis, how did this happen?");
    }

    private static void ProcessForProfession(Profession profession, List<int> currentCrops, Farmer? farmer = null)
    {
        if (farmer is null)
        {
            ModEntry.ModMonitor.Log($"Could not find farmer for {profession}, continuing.");
            return;
        }

        if (!daysPerCondition.TryGetValue(new CropCondition(profession, 0), out Dictionary<int, int>? unfertilized))
        {
            unfertilized = new();
            daysPerCondition[new CropCondition(profession, 0)] = unfertilized;
        }

        foreach (int crop in currentCrops)
        {
            if (!unfertilized.ContainsKey(crop))
            {
                Crop c = new(crop, 0, 0);
                DummyHoeDirt? dirt = dirts[0];
                dirt.nearWaterForPaddy.Value = c.isPaddyCrop() ? 1 : 0;
                dirt.crop = c;
                int? days = dirt.CalculateTimings(farmer);
                if (days is not null)
                {
                    unfertilized[crop] = days.Value;
                    ModEntry.ModMonitor.DebugOnlyLog($"{crop} takes {days.Value} days");
                }
            }
        }

        foreach (var fertilizer in fertilizers.Keys)
        {
            CropCondition condition = new(profession, fertilizer);

            if (!daysPerCondition.TryGetValue(condition, out var dict))
            {
                dict = new();
                daysPerCondition[condition] = dict;
            }
            else if (!InventoryWatcher.HasSeedChanges)
            {
                continue;
            }

#warning - finish this?
            foreach (int crop in currentCrops)
            {
                if (!dict.ContainsKey(crop))
                {

                }
            }
        }

    }

    private static bool FilterCropsToUserConfig(int crop)
    {
        // mixed seeds.
        if (crop == 770)
        {
            return false;
        }

        if (ModEntry.Config.GetAllowedSeeds().Contains(crop)
            || AssetManager.AllowedSeeds.Contains(crop))
        {
            return true;
        }

        if (AssetManager.DeniedSeeds.Contains(crop) || SObject.isWildTreeSeed(crop))
        {
            return false;
        }

        if (!Game1Wrappers.ObjectInfo.TryGetValue(crop, out var data))
        {
            return false;
        }

        switch (ModEntry.Config.CropsToDisplay)
        {
            case CropOptions.All:
                return true;
            case CropOptions.Purchaseable:
            {
                var name = data.GetNthChunk('/', 0).ToString();
                if (JsonAssetsShims.IsAvailableSeed(name))
                {
                    return true;
                }

                goto case CropOptions.Seen;
            }
            case CropOptions.Seen:
            {
                var name = data.GetNthChunk('/', 0).ToString();
                return InventoryWatcher.Model?.Seeds?.Contains(name) != false;
            }
            default:
                return true;
        }
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

        DummyHoeDirt dirt = new(0);

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

            dirt.fertilizer.Value = HoeDirt.noFertilizer;
            if (!dirt.plant(index, 0, 0, Game1.player, true, Game1.getFarm()))
            {
                continue;
            }

            var name = vals.GetNthChunk('/', SObject.objectInfoDisplayNameIndex).Trim().ToString();
            if (!IsAllowedFertilizer(index, name))
            {
                continue;
            }

            ret[index] = name;
        }

        bool changed = !ret.IsEquivalentTo(fertilizers);
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

        dirts[0] = new(0);

        foreach (int fert in fertilizers.Keys)
        {
            dirts[fert] = new(fert);
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Set up {dirts.Count} hoedirts");
    }

    private static bool IsAllowedFertilizer(int id, string name)
    {
        if (ModEntry.Config.GetAllowedFertilizers().Contains(id)
            || AssetManager.AllowedFertilizers.Contains(id))
        {
            return true;
        }
        if (name.Equals("Tree Fertilizer", StringComparison.OrdinalIgnoreCase)
            || AssetManager.DeniedFertilizers.Contains(id))
        {
            return false;
        }
        if (ModEntry.Config.FertilizersToDisplay == FertilizerOptions.All)
        {
            return true;
        }
        return InventoryWatcher.Model?.Seeds?.Contains(name) != false;
    }
    #endregion
}
