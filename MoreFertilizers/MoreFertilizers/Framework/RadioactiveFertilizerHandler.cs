using AtraBase.Models.WeightedRandom;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils;

using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.Framework;
internal static class RadioactiveFertilizerHandler
{
    private static IAssetName crops = null!;
    private static IAssetName objects = null!;

    private static ILastDayToPlantAPI? api;

    private static readonly WeightedManager<(int, StardewSeasons)>?[] cropManager = new WeightedManager<(int, StardewSeasons)>?[4];

    internal static void Initialize(IGameContentHelper parser, IModRegistry registry, ITranslationHelper translation)
    {
        crops = parser.ParseAssetName("Data/Crops");
        objects = parser.ParseAssetName("Data/ObjectInformation");

        IntegrationHelper helper = new(ModEntry.ModMonitor, translation, registry);
        _ = helper.TryGetAPI("atravita.LastDayToPlantRedux", null, out api);
    }

    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(crops) || assets.Contains(objects))
        {
            for (int i = 0; i < cropManager.Length; i++)
            {
                cropManager[i] = null;
            }
        }
    }

    internal static void OnDayEnd()
    {
        List<HoeDirt> dirts = new();

        Utility.ForAllLocations((location) =>
        {
            foreach (var terrain in location.terrainFeatures.Values)
            {
                if (terrain is HoeDirt dirt && dirt.fertilizer.Value == ModEntry.RadioactiveFertilizerID)
                {
                    dirts.Add(dirt);
                }
            }

            foreach (var obj in location.Objects.Values)
            {
                if (obj is IndoorPot pot && pot.hoeDirt.Value is HoeDirt dirt && dirt.fertilizer.Value == ModEntry.RadioactiveFertilizerID)
                {
                    dirts.Add(dirt);
                }
            }
        });

        if (dirts.Count == 0)
        {
            ModEntry.ModMonitor.Log($"No radioactive dirt found.");
        }

        Random random = RandomUtils.GetSeededRandom(9, (int)Game1.uniqueIDForThisGame);
    }

    private static WeightedManager<int> GeneratedWeightedList()
    {
        var cropData = Game1.content.Load<Dictionary<int, string>>(crops.BaseName);

        foreach (var)
    }
}
