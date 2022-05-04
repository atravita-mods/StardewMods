using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace StopRugRemoval;

/// <summary>
/// Handles editing assets.
/// </summary>
internal static class AssetEditor
{
#pragma warning disable SA1310 // Field names should not contain underscore. Reviewed.
    private static readonly string SALOON_EVENTS = PathUtilities.NormalizeAssetName("Data/Events/Saloon");
#pragma warning restore SA1310 // Field names should not contain underscore

    /// <summary>
    /// Applies edits.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void Edit(AssetRequestedEventArgs e, IModRegistry registry)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(SALOON_EVENTS) && !registry.IsLoaded("violetlizabet.CP.NoAlcohol"))
        {
            e.Edit(EditSaloon, AssetEditPriority.Late);
        }
    }

    private static void EditSaloon(IAssetData asset)
    {
        IAssetDataForDictionary<string, string>? editor = asset.AsDictionary<string, string>();

        if (editor.Data.ContainsKey("atravita_elliott_nodrink"))
        {// event has been edited already?
            return;
        }
        foreach ((string key, string value) in editor.Data)
        {
            if (key.StartsWith("40", StringComparison.OrdinalIgnoreCase))
            {
                int index = value.IndexOf("speak Elliott");
                int second = value.IndexOf("speak Elliott", index + 1);
                int nextslash = value.IndexOf('/', second);
                if (nextslash > -1)
                {
                    string initial = value[..nextslash] + $"/question fork1 \"#{I18n.Drink()}#{I18n.Nondrink()}/fork atravita_elliott_nodrink/";
                    string remainder = value[(nextslash + 1)..];

                    editor.Data["atravita_elliott_nodrink"] = remainder.Replace("346", "350");
                    editor.Data[key] = initial + remainder;
                }
                return;
            }
        }
    }
}