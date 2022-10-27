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

    /// <summary>
    /// The data asset for objects.
    /// </summary>
    private static IAssetName objectInfoName = null!;

    /// <summary>
    /// Gets the data asset for Data/crops.
    /// </summary>
    internal static IAssetName CropName { get; private set; } = null!;

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">the game content parser.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        dataMail = parser.ParseAssetName("Data/mail");
        CropName = parser.ParseAssetName("Data/Crops");
        objectInfoName = parser.ParseAssetName("Data/ObjectInformation");
        accessLists = parser.ParseAssetName("Mods/atravita.LastDayToPlantRedux/AccessControl");
    }

    /// <summary>
    /// Applies asset edits for this mod.
    /// </summary>
    /// <param name="e">event args.</param>
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
        if (e.NamesWithoutLocale.Contains(objectInfoName))
        {
            CropAndFertilizerManager.RequestInvalidateFertilizers();
        }
    }
}
