using AtraBase.Models.WeightedRandom;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;

using StardewModdingAPI.Events;

using StardewValley.GameData.Crops;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.Framework;

/// <summary>
/// Handles the radioactive fertilizer.
/// </summary>
internal static class RadioactiveFertilizerHandler
{
    private static readonly WeightedManager<int>?[] CropManagers = new WeightedManager<int>?[4];

    private static IAssetName crops = null!;
    private static IAssetName objects = null!;

    private static ILastDayToPlantAPI? api;

    private static Random? random;

    /// <summary>
    /// Initializes APIs and assets for the radioactive fertilizer.
    /// </summary>
    /// <param name="parser">GameContent helper.</param>
    /// <param name="registry">Mod registry.</param>
    /// <param name="translation">Translation helper.</param>
    internal static void Initialize(IGameContentHelper parser, IModRegistry registry, ITranslationHelper translation)
    {
        crops = parser.ParseAssetName("Data/Crops");
        objects = parser.ParseAssetName("Data/Objects");

        IntegrationHelper helper = new(ModEntry.ModMonitor, translation, registry);
        _ = helper.TryGetAPI("atravita.LastDayToPlantRedux", null, out api);
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(crops) || assets.Contains(objects))
        {
            for (int i = 0; i < CropManagers.Length; i++)
            {
                CropManagers[i] = null;
            }
        }
    }

    /// <summary>
    /// called at day end, handles the radioactive fertilizer.
    /// </summary>
    internal static void OnDayEnd()
    {
        if (Game1.dayOfMonth >= 28)
        {
            ModEntry.ModMonitor.Log("Too close to end of month, skipping radioactive fertilizer");
            return;
        }

        // find a farmer to do the planting with.
        Farmer bestfarmer = Game1.player;
        Profession bestProfession = bestfarmer.GetProfession();
        if (Context.IsMultiplayer)
        {
            foreach (Farmer? farmer in Game1.getOnlineFarmers())
            {
                if (bestProfession == Profession.Prestiged)
                {
                    break;
                }

                Profession profession = farmer.GetProfession();
                if (profession > bestProfession)
                {
                    bestfarmer = farmer;
                    bestProfession = profession;
                }
            }
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Using farmer {bestfarmer.Name} with profession {bestProfession}");

        Dictionary<string, CropData> cropData = DataLoader.Crops(Game1.content);

        Utility.ForEachLocation((location) =>
        {
            if (location is null)
            {
                return true;
            }

            string seasonstring = location.GetSeasonKey();
            int season = Utility.getSeasonNumber(seasonstring);

            if (season < 0 || season > 3)
            {
                ModEntry.ModMonitor.Log("Season unrecognized, skipping");
                return true;
            }

            foreach (TerrainFeature? terrain in location.terrainFeatures.Values)
            {
                if (terrain is HoeDirt dirt && dirt.fertilizer.Value == ModEntry.RadioactiveFertilizerID)
                {
                    ProcessRadioactiveFertilizer(dirt, bestfarmer, bestProfession, location, season, cropData, seasonstring);
                }
            }

            foreach (SObject? obj in location.Objects.Values)
            {
                if (obj is IndoorPot pot && pot.hoeDirt.Value is HoeDirt dirt && dirt.fertilizer.Value == ModEntry.RadioactiveFertilizerID)
                {
                    ProcessRadioactiveFertilizer(dirt, bestfarmer, bestProfession, location, season, cropData, seasonstring);
                }
            }

            return true;
        });

        random = null;
    }

    private static void ProcessRadioactiveFertilizer(HoeDirt dirt, Farmer farmer, Profession profession, GameLocation location, int season, Dictionary<int, string> cropData, string seasonstring)
    {
        if (dirt.crop is null || dirt.crop.dead.Value || dirt.crop.IsActuallyFullyGrown())
        {
            return;
        }

        random ??= RandomUtils.GetSeededRandom(9, "radioactive.fertilizer");
        if (random.OfChance(0.25))
        {
            return;
        }

        if (location.SeedsIgnoreSeasonsHere())
        {
            season = random.Next(4);
        }

        StardewSeasons seasonEnum = SeasonExtensions.GetSeasonFromIndex(season);
        CropManagers[season] ??= GeneratedWeightedList(seasonstring, cropData);

        WeightedManager<int>? manager = CropManagers[season];
        if (manager?.Count is null or 0 || !manager.GetValue(random).TryGetValue(out int crop))
        {
            return;
        }

        if (cropData.TryGetValue(crop, out string? data)
            && (location.SeedsIgnoreSeasonsHere() || HasSufficientTimeToGrow(profession, crop, data, seasonEnum)))
        {
            ModEntry.ModMonitor.Log($"Replacing plant at {dirt.Tile} with {crop}.");
            dirt.destroyCrop(false);
            dirt.plant(crop, (int)dirt.Tile.X, (int)dirt.Tile.Y, farmer, false, location);
            dirt.fertilizer.Value = null;
        }
    }

    private static Profession GetProfession(this Farmer farmer)
    {
        if (farmer.professions.Contains(Farmer.agriculturist + 100))
        {
            return Profession.Prestiged;
        }
        else if (farmer.professions.Contains(Farmer.agriculturist))
        {
            return Profession.Agriculturalist;
        }
        return Profession.None;
    }

    private static WeightedManager<int> GeneratedWeightedList(string season, Dictionary<string, string> cropData)
    {
        WeightedManager<int>? manager = new();

        HashSet<string> denylist = AssetEditor.GetRadioactiveExclusions();

        foreach ((string id, string data) in cropData)
        {
            if (id == "885" || denylist.Contains(id))
            {
                // 885 - fiber seeds.
                continue;
            }

            if (ModEntry.Config.BanRaisedSeeds && bool.TryParse(data.GetNthChunk('/', 7), out bool raised) && raised)
            {
                continue;
            }

            if (data.GetNthChunk('/', 1).Contains(season, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(data.GetNthChunk('/', 3), out int obj)
                && Game1Wrappers.ObjectData.TryGetValue(obj, out StardewValley.GameData.Objects.ObjectData objData))
            {
                ReadOnlySpan<char> name = objData.GetNthChunk('/', SObject.objectInfoNameIndex);
                if (name.Contains("Qi", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                double weight = Math.Clamp(2500.0 / price, 1.0f, 1000f);
                manager.Add(weight, id);
            }
        }

        return manager;
    }

    private static bool HasSufficientTimeToGrow(Profession profession, int cropId, string cropData, StardewSeasons season)
    {
        if (api is null)
        {
            int daysLeft = 28 - Game1.dayOfMonth;
            foreach (SpanSplitEntry days in cropData.GetNthChunk('/', 0).StreamSplit(null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(days, out int num) || num > daysLeft)
                {
                    return false;
                }
                daysLeft -= num;
            }

            return true;
        }
        else if (api.GetDays(profession, 0, cropId, season) > 28 - Game1.dayOfMonth)
        {
            return false;
        }
        return true;
    }
}
