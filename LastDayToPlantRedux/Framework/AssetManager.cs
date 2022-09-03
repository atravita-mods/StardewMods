using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace LastDayToPlantRedux.Framework;
internal static class AssetManager
{
    private static readonly string MailFlag = "atravita_LastDayLetter";

    private static readonly string DataMail = PathUtilities.NormalizeAssetName("Data/mail");

    // crop data.
    private static readonly string CropData = PathUtilities.NormalizeAssetName("Data/Crops");

    private static IAssetName? cropName = null;

    private static IAssetName CropName =>
        cropName ??= ModEntry.GameContentHelper.ParseAssetName(CropData);


    internal static void ApplyEdit(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(DataMail))
        {
            // apply edits?
        }
    }

    internal static void InvalidateCache(AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Contains(CropName))
        {
            // invalidate cache.
        }
    }
}
