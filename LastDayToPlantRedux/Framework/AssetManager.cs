using AtraBase.Collections;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
internal static class AssetManager
{
    private static readonly string MailFlag = "atravita_LastDayLetter";
    private static readonly string DataMail = PathUtilities.NormalizeAssetName("Data/mail");

    // crop data.
    private static readonly string CropData = PathUtilities.NormalizeAssetName("Data/Crops");

    private static IAssetName? cropName = null;

    private static IAssetName CropName =>
        cropName ??= ModEntry.GameContentHelper.ParseAssetName(CropData);

    // data objectinfo
    private static readonly string DataObjectInfo = PathUtilities.NormalizeAssetName("Data/ObjectInformation");

    private static IAssetName? objectInfoName = null;

    private static IAssetName ObjectInfoName =>
        objectInfoName ??= ModEntry.GameContentHelper.ParseAssetName(DataObjectInfo);

    // denylist and allowlist
    private static readonly string AccessLists = PathUtilities.NormalizeAssetName("Mods/atravita.LastDayToPlantRedux/AccessControl");

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(AccessLists))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<int, string>, AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(DataMail))
        {
            e.Edit(
            static (asset) =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                data[MailFlag] = "";
            }, AssetEditPriority.Late);
        }
    }

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
