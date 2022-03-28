using StardewModdingAPI.Utilities;

namespace TrashDoesNotConsumeBait;

/// <inheritdoc />
public class AssetEditor : IAssetEditor
{
#pragma warning disable SA1310 // Field names should not contain underscore. Reviewed.
    private static readonly string FORGE_MENU_CHOICE = PathUtilities.NormalizeAssetName("Mods/atravita_ForgeMenuChoice_Tooltip_Data");
    private static readonly string SECRET_NOTE_LOCATION = PathUtilities.NormalizeAssetName("Data/SecretNotes");
#pragma warning restore SA1310 // Field names should not contain underscore

    private AssetEditor()
    {
    }

    /// <summary>
    /// Gets the instance of the asset editor for this mod.
    /// </summary>
    public static AssetEditor Instance { get; } = new();

    /// <inheritdoc/>
    public bool CanEdit<T>(IAssetInfo asset)
        => asset.AssetNameEquals(FORGE_MENU_CHOICE) || asset.AssetNameEquals(SECRET_NOTE_LOCATION);

    /// <inheritdoc/>
    public void Edit<T>(IAssetData asset)
    {
        if (asset.AssetNameEquals(FORGE_MENU_CHOICE))
        {
            IAssetDataForDictionary<string, string> data = asset.AsDictionary<string, string>();
            data.Data["Preserving"] = data.Data["Preserving"].Replace("50", ((1 - ModEntry.Config.ConsumeChancePreserving) * 100).ToString());
        }
        else
        {
            IAssetDataForDictionary<int, string> data = asset.AsDictionary<int, string>();
            data.Data[1008] = data.Data[1008].Replace("50", ((1 - ModEntry.Config.ConsumeChancePreserving) * 100).ToString());
        }
    }

    /// <summary>
    /// Asks SMAPI to invalidate the assets.
    /// </summary>
    internal static void Invalidate()
    {
        ModEntry.ContentHelper.InvalidateCache(SECRET_NOTE_LOCATION);
        ModEntry.ContentHelper.InvalidateCache(FORGE_MENU_CHOICE);
    }
}