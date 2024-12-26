using AtraBase.Models.WeightedRandom;
using AtraBase.Toolkit.Extensions;
using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;
using MiniAtraShared.Extensions;
using StardewModdingAPI.Events;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Objects;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.Framework;

/// <summary>
/// Handles the radioactive fertilizer.
/// </summary>
internal static class RadioactiveFertilizerHandler
{
    private static readonly WeightedManager<string>?[] CropManagers = new WeightedManager<string>?[4];

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

            var season = location.GetSeason();

            if ((int)season < 0 || (int)season > 3)
            {
                ModEntry.ModMonitor.Log("Season unrecognized, skipping");
                return true;
            }

            foreach (TerrainFeature? terrain in location.terrainFeatures.Values)
            {
                if (terrain is HoeDirt dirt && dirt.fertilizer.Value == ModEntry.RadioactiveFertilizerID)
                {
                    ProcessRadioactiveFertilizer(dirt, bestfarmer, bestProfession, location, cropData, season);
                }
            }

            foreach (SObject? obj in location.Objects.Values)
            {
                if (obj is IndoorPot pot && pot.hoeDirt.Value is HoeDirt dirt && dirt.fertilizer.Value == ModEntry.RadioactiveFertilizerID)
                {
                    ProcessRadioactiveFertilizer(dirt, bestfarmer, bestProfession, location, cropData, season);
                }
            }

            return true;
        });

        random = null;
    }

    private static void ProcessRadioactiveFertilizer(HoeDirt dirt, Farmer farmer, Profession profession, GameLocation location, Dictionary<string, CropData> cropData, Season season)
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
            season = (Season)random.Next(4);
        }

        StardewSeasons seasonEnum = SeasonExtensions.ConvertFromGameSeason(season);
        CropManagers[(int)season] ??= GeneratedWeightedList(season, cropData);

        WeightedManager<string>? manager = CropManagers[(int)season];
        if (manager?.Count is null or 0 || !manager.GetValue(random).TryGetValue(out string? crop) || crop is null)
        {
            return;
        }

        if (cropData.TryGetValue(crop, out CropData? data)
            && (location.SeedsIgnoreSeasonsHere() || HasSufficientTimeToGrow(profession, crop, data, seasonEnum)))
        {
            ModEntry.ModMonitor.Log($"Replacing plant at {dirt.Tile} with {crop}.");
            dirt.destroyCrop(false);
            dirt.plant(crop, farmer, false);
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

    private static WeightedManager<string> GeneratedWeightedList(Season season, Dictionary<string, CropData> cropData)
    {
        WeightedManager<string>? manager = new();

        HashSet<string> denylist = AssetEditor.GetRadioactiveExclusions();

        foreach ((string id, CropData data) in cropData)
        {
            if (id == "885" || denylist.Contains(id))
            {
                // 885 - fiber seeds.
                continue;
            }

            if (ModEntry.Config.BanRaisedSeeds && data.IsRaised)
            {
                continue;
            }

            if (data.Seasons.Contains(season) && data.HarvestItemId is { } obj && Game1Wrappers.ObjectData.TryGetValue(obj, out ObjectData? objData))
            {
                if (objData.Name.Contains("Qi", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                double weight = Math.Clamp(2500.0 / Math.Min(objData.Price, 1), 1.0f, 1000f);
                manager.Add(weight, id);
            }
        }

        return manager;
    }

    private static bool HasSufficientTimeToGrow(Profession profession, string cropId, CropData cropData, StardewSeasons season)
    {
        if (api is null)
        {
            int daysLeft = 28 - Game1.dayOfMonth - cropData.DaysInPhase.Sum();
            return daysLeft > 0;
        }
        else if (api.GetDays(profession, null, cropId, season) > 28 - Game1.dayOfMonth)
        {
            return false;
        }
        return true;
    }
}
