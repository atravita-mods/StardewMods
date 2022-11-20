using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;

namespace MoreFertilizers.Framework;
internal static class RadioactiveFertilizerHandler
{
    private static IAssetName crops = null!;
    private static IAssetName objects = null!;

    private static ILastDayToPlantAPI? api;

    internal static void Initialize(IGameContentHelper parser, IModRegistry registry, ITranslationHelper translation)
    {
        crops = parser.ParseAssetName("Data/Crops");
        objects = parser.ParseAssetName("Data/ObjectInformation");

        IntegrationHelper helper = new(ModEntry.ModMonitor, translation, registry);
        _ = helper.TryGetAPI("atravita.LastDayToPlantRedux", null, out api);
    }

    internal static void Reset(IReadOnlySet<IAssetName> assets)
    {

    }
}
