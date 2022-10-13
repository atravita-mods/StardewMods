using AtraBase.Collections;

using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static readonly string MailFlag = "atravita_LastDayLetter";
    private static IAssetName dataMail = null!;

    // denylist and allowlist
    private static IAssetName accessLists = null!;

    internal static IAssetName CropName { get; private set; } = null!;
    internal static IAssetName ObjectInfoName { get; private set; } = null!;

    internal static void Initialize(IGameContentHelper parser)
    {
        dataMail = parser.ParseAssetName("Data/mail");
        CropName = parser.ParseAssetName("Data/Crops");
        ObjectInfoName = parser.ParseAssetName("Data/ObjectInformation");
        accessLists = parser.ParseAssetName("Mods/atravita.LastDayToPlantRedux/AccessControl");
    }

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(accessLists))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<int, string>, AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataMail))
        {
            e.Edit(
            static (asset) =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                data[MailFlag] = "";
            }, AssetEditPriority.Late);
        }
    }

    /// <summary>
    /// Listens for cache invalidations and empties the relevant caches if needed.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void InvalidateCache(AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Contains(CropName))
        {
            CropAndFertilizerManager.RequestInvalidateCrops();
        }
        if (e.NamesWithoutLocale.Contains(ObjectInfoName))
        {
            CropAndFertilizerManager.RequestInvalidateFertilizers();
        }
    }
}
