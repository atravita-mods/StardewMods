using AtraShared.Utils.Extensions;

using StardewModdingAPI.Events;

namespace TrashDoesNotConsumeBait;

/// <summary>
/// Handles asset editing for this mod.
/// </summary>
public static class AssetEditor
{
    private static IAssetName forgeMenuChoice = null!;
    private static IAssetName secretNoteLocation = null!;

    /// <summary>
    /// Initializes the asset editor.
    /// </summary>
    /// <param name="parser">game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        forgeMenuChoice = parser.ParseAssetName("Mods/atravita_ForgeMenuChoice_Tooltip_Data");
        secretNoteLocation = parser.ParseAssetName("Data/SecretNotes");
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    /// <remarks>
    /// Edits the secret note and ForgeMenuChoice's tool-tips to match.
    /// </remarks>
    internal static void EditAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(forgeMenuChoice))
        {
            e.Edit(EditForgeMenu);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(secretNoteLocation))
        {
            e.Edit(EditSecretNote);
        }
    }

    /// <summary>
    /// Asks SMAPI to invalidate the assets.
    /// </summary>
    internal static void Invalidate()
    {
        ModEntry.GameContentHelper.InvalidateCacheAndLocalized(secretNoteLocation.BaseName);
        ModEntry.GameContentHelper.InvalidateCacheAndLocalized(forgeMenuChoice.BaseName);
    }

    private static void EditForgeMenu(IAssetData editor)
    {
        IAssetDataForDictionary<string, string> data = editor.AsDictionary<string, string>();
        if (data.Data.TryGetValue("Preserving", out string? val))
        {
            data.Data["Preserving"] = val.Replace("50", ((1 - ModEntry.Config.ConsumeChancePreserving) * 100).ToString());
        }
        else
        {
            ModEntry.ModMonitor.Log("ForgeMenuChoice's Preserving key not found....", LogLevel.Debug);
        }
    }

    private static void EditSecretNote(IAssetData editor)
    {
        IAssetDataForDictionary<int, string> data = editor.AsDictionary<int, string>();
        if (data.Data.TryGetValue(1008, out string? val))
        {
            data.Data[1008] = val.Replace("50", ((1 - ModEntry.Config.ConsumeChancePreserving) * 100).ToString());
        }
        else
        {
            ModEntry.ModMonitor.Log("Data for secret note 1008 not found?", LogLevel.Debug);
        }
    }
}