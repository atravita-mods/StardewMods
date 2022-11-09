using System.Collections.ObjectModel;
using System.Text;

using AtraBase.Collections;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraShared.Caching;
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
    private static readonly TickCache<bool> HasStocklist = new(() => Game1.player.hasOrWillReceiveMail("PierreStocklist"));

    // Map conditions to the number of days it takes to grow a crop.
    private static readonly Dictionary<CropCondition, Dictionary<int, int>> DaysPerCondition = new();

    /// <summary>
    /// a mapping of fertilizers to a hoedirt that has them.
    /// </summary>
    private static readonly Dictionary<int, DummyHoeDirt> Dirts = new();

    /// <summary>
    /// a cache of crop data.
    /// </summary>
    private static Dictionary<int, CropEntry> crops = new();

    /// <summary>
    /// a mapping of fertilizers to their localized names.
    /// </summary>
    private static Dictionary<int, string> fertilizers = new();

    private static bool cropsNeedRefreshing = true;
    private static bool fertilizersNeedRefreshing = true;
    private static StardewSeasons lastLoadedSeason = StardewSeasons.None;
    private static bool hadStocklistLastCheck = false;
    private static bool requiresReset = true;

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "StyleCop doesn't understand records.")]
    private record CropEntry(StardewSeasons Seasons, string GrowthData);

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "StyleCop doesn't understand records.")]
    private record CropCondition(Profession Profession, int Fertilizer);

    #region API

    internal static int? GetDays(Profession profession, int fertilizer, int crop)
    {
        // check with specific fertilizer.
        CropCondition? key = new(profession, fertilizer);
        if (DaysPerCondition.TryGetValue(key, out Dictionary<int, int>? daysDict)
            && daysDict.TryGetValue(crop, out var days))
        {
            return days;
        }

        // check with no fertilizer.
        key = new(profession, 0);
        if (DaysPerCondition.TryGetValue(key, out daysDict)
            && daysDict.TryGetValue(crop, out days))
        {
            return days;
        }
        return null;
    }

    internal static IReadOnlyDictionary<int, int>? GetAll(Profession profession, int fertilizer)
    {
        CropCondition? key = new(profession, fertilizer);
        if (DaysPerCondition.TryGetValue(key, out var daysDict))
        {
            return new ReadOnlyDictionary<int, int>(daysDict);
        }
        return null;
    }
    #endregion

    #region processing

    internal static void RequestReset() => requiresReset = true;

    internal static (string message, bool showplayer) GenerateMessageString()
    {
        ModEntry.ModMonitor.Log($"Processing for day {Game1.dayOfMonth}");
        MultiplayerManager.UpdateOnDayStart();
        Process();

        int daysRemaining = 28 - Game1.dayOfMonth;

        if (daysRemaining < 0)
        {
            ModEntry.ModMonitor.Log($"Day of Month {Game1.dayOfMonth} seems odd, do you have a mod adjusting that?", LogLevel.Warn);
            return (string.Empty, false);
        }

        StringBuilder? sb = StringBuilderCache.Acquire();
        sb.Append(I18n.Intro())
           .Append("^^");
        bool hasCrops = false;

        foreach ((CropCondition condition, Dictionary<int, int> cropvalues) in DaysPerCondition)
        {
            if (cropvalues.Count == 0)
            {
                continue;
            }

            foreach ((int index, int days) in cropvalues)
            {
                if (days != daysRemaining)
                {
                    continue;
                }

                if (!Game1Wrappers.ObjectInfo.TryGetValue(index, out string? data))
                {
                    continue;
                }

                ReadOnlySpan<char> name = data.GetNthChunk('/', SObject.objectInfoDisplayNameIndex);

                if (name.Length == 0)
                {
                    continue;
                }
                hasCrops = true;

                sb.Append(I18n.CropInfo(name.ToString(), days));
                if (condition.Fertilizer != 0)
                {
                    sb.Append(I18n.CropInfo_Fertilizer(fertilizers[condition.Fertilizer]));
                }
                switch (condition.Profession)
                {
                    case Profession.Agriculturalist:
                        sb.Append(I18n.CropInfo_Agriculturalist());
                        break;
                    case Profession.Prestiged:
                        sb.Append(I18n.CropInfo_Prestiged());
                        break;
                    default:
                        break;
                }
                sb.Append('^');
            }
        }

        if (!hasCrops)
        {
            sb.Clear();
            StringBuilderCache.Release(sb);
            return (I18n.None(), false);
        }
        else
        {
            sb.Append("[#]")
                .Append(I18n.CropInfo_Title());
            return (StringBuilderCache.GetStringAndRelease(sb), true);
        }
    }

    private static void Process()
    {
        if (!StardewSeasonsExtensions.TryParse(Game1.currentSeason, ignoreCase: true, out StardewSeasons currentSeason))
        {
            ModEntry.ModMonitor.Log($"Could not parse season {Game1.currentSeason}?", LogLevel.Error);
            return;
        }

        // if there is a season change, or if our backing data has changed, dump the cache.
        // first pipe is single intentionally, don't want shortcutting there.
        if (LoadCropData() | LoadFertilizerData()
            || requiresReset || currentSeason != lastLoadedSeason || hadStocklistLastCheck != HasStocklist.GetValue())
        {
            DaysPerCondition.Clear();
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
                goto SUCCESS;
            }
        }

        if (MultiplayerManager.AgriculturalistFarmer?.TryGetTarget(out Farmer? agriculturalist) == true)
        {
            ProcessForProfession(Profession.Agriculturalist, currentCrops, agriculturalist);

            if (!Context.IsMultiplayer)
            {
                goto SUCCESS;
            }
        }

        if (MultiplayerManager.NormalFarmer?.TryGetTarget(out Farmer? normal) == true)
        {
            ProcessForProfession(Profession.None, currentCrops, normal);
            goto SUCCESS;
        }

        ModEntry.ModMonitor.Log($"No available farmers for data analysis, how did this happen?", LogLevel.Warn);
        return;

SUCCESS:
        requiresReset = false;
        hadStocklistLastCheck = HasStocklist.GetValue();
        lastLoadedSeason = currentSeason;
        InventoryWatcher.Reset();
    }

    private static void ProcessForProfession(Profession profession, List<int> currentCrops, Farmer? farmer = null)
    {
        if (farmer is null)
        {
            ModEntry.ModMonitor.Log($"Could not find farmer for {profession}, continuing.");
            return;
        }

        if (!DaysPerCondition.TryGetValue(new CropCondition(profession, 0), out Dictionary<int, int>? unfertilized))
        {
            unfertilized = new();
            DaysPerCondition[new CropCondition(profession, 0)] = unfertilized;
        }

        // set up unfertilized.
        foreach (int crop in currentCrops)
        {
            if (!unfertilized.ContainsKey(crop))
            {
                Crop c = new(crop, 0, 0);
                DummyHoeDirt? dirt = Dirts[0];
                dirt.crop = c;
                dirt.nearWaterForPaddy.Value = c.isPaddyCrop() ? 1 : 0;
                int? days = dirt.CalculateTimings(farmer);
                if (days is not null)
                {
                    unfertilized[crop] = days.Value;
                    ModEntry.ModMonitor.DebugOnlyLog($"{crop} takes {days.Value} days");
                }
            }
        }

        foreach (int fertilizer in fertilizers.Keys)
        {
            CropCondition condition = new(profession, fertilizer);

            if (!DaysPerCondition.TryGetValue(condition, out Dictionary<int, int>? dict))
            {
                dict = new();
                DaysPerCondition[condition] = dict;
            }
            else if (!InventoryWatcher.HasSeedChanges)
            {
                // if we've processed this fertilizer before and
                // don't have new seed changes, we can skip this round.
                continue;
            }

            foreach (int crop in currentCrops)
            {
                if (!dict.ContainsKey(crop))
                {
                    Crop c = new(crop, 0, 0);
                    DummyHoeDirt? dirt = Dirts[fertilizer];
                    dirt.crop = c;
                    dirt.nearWaterForPaddy.Value = c.isPaddyCrop() ? 1 : 0;
                    int? days = dirt.CalculateTimings(farmer);

                    // only save when there's a difference from unfertilized.
                    // most fertilizers don't change the time to grow.
                    if (days is not null && unfertilized[crop] != days)
                    {
                        dict[crop] = days.Value;
                        ModEntry.ModMonitor.DebugOnlyLog($"{crop} takes {days.Value} days for fertilizer {fertilizer}.");
                    }
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

        if (!Game1Wrappers.ObjectInfo.TryGetValue(crop, out string? data))
        {
            return false;
        }

        switch (ModEntry.Config.CropsToDisplay)
        {
            case CropOptions.All:
                return true;
            case CropOptions.Purchaseable:
            {
                if (HasStocklist.GetValue())
                {
                    goto case CropOptions.All;
                }

                if (crop < 3000)
                {
                    return Game1.year > 1 || !(crop is 476 or 485 or 489); // the year2 seeds.
                }
                string? name = data.GetNthChunk('/', 0).ToString();
                if (JsonAssetsShims.IsAvailableSeed(name))
                {
                    return true;
                }

                goto case CropOptions.Seen;
            }
            case CropOptions.Seen:
            {
                string? name = data.GetNthChunk('/', 0).ToString();
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

            if (!int.TryParse(catName[(spaceIndx + 1)..], out int category) ||
                (category is not SObject.fertilizerCategory))
            {
                continue;
            }

            dirt.fertilizer.Value = HoeDirt.noFertilizer;
            if (!dirt.plant(index, 0, 0, Game1.player, true, Game1.getFarm()))
            {
                continue;
            }

            string? name = vals.GetNthChunk('/', SObject.objectInfoNameIndex).Trim().ToString();
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
        Dirts.Clear();

        Dirts[0] = new(0);

        foreach (int fert in fertilizers.Keys)
        {
            Dirts[fert] = new(fert);
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Set up {Dirts.Count} hoedirts");
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
        return InventoryWatcher.Model?.Fertilizers?.Contains(name) != false;
    }
    #endregion
}
