using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace GiantCropFertilizer;

/// <summary>
/// Handles asset editing for this mod.
/// </summary>
internal static class AssetEditor
{
    private static readonly string OBJECTDATA = PathUtilities.NormalizeAssetName("Data/ObjectInformation");

    /// <summary>
    /// Called when assets are requested.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void HandleAssetRequested(AssetRequestedEventArgs e)
    {
        if (ModEntry.GiantCropFertilizerID != -1 && e.NameWithoutLocale.IsEquivalentTo(OBJECTDATA))
        {
            e.Edit(EditAsset, AssetEditPriority.Late);
        }
    }

    private static void EditAsset(IAssetData asset)
    {
        IAssetDataForDictionary<int, string>? editor = asset.AsDictionary<int, string>();
        if (ModEntry.GiantCropFertilizerID != -1 && editor.Data.TryGetValue(ModEntry.GiantCropFertilizerID, out string? val))
        {
            editor.Data[ModEntry.GiantCropFertilizerID] = val.Replace("Basic -20", "Basic -19");
            ModEntry.ModMonitor.Log($"Successfully edited {ModEntry.GiantCropFertilizerID}");
        }
        else
        {
            ModEntry.ModMonitor.Log($"Could not find {ModEntry.GiantCropFertilizerID} in ObjectInformation to edit! This mod may not function properly.", LogLevel.Error);
        }
    }
}