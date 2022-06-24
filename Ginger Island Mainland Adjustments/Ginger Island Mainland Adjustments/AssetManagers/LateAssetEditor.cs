using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments.AssetManagers;

/// <summary>
/// Class to hold asset editing that has to be done **after** CP.
/// </summary>
public sealed class LateAssetEditor : IAssetEditor
{
    // We edit Pam's nine heart event to set flags to remember which path the player chose.
    private static readonly string DataEventsTrailerBig = PathUtilities.NormalizeAssetName("Data/Events/Trailer_Big");

    private LateAssetEditor()
    {
    }

    /// <summary>
    /// Gets the instance of the LateAssetEditor.
    /// </summary>
    public static LateAssetEditor Instance { get; } = new();

    /// <inheritdoc />
    [UsedImplicitly]
    public bool CanEdit<T>(IAssetInfo asset) => asset.AssetNameEquals(DataEventsTrailerBig);

    /// <inheritdoc />
    [UsedImplicitly]
    public void Edit<T>(IAssetData asset)
    {
        if (asset.AssetNameEquals(DataEventsTrailerBig))
        { // Insert mail flags into the vanilla event
            IAssetDataForDictionary<string, string>? editor = asset.AsDictionary<string, string>();
            if (editor.Data.TryGetValue("positive", out string? val))
            {
                editor.Data["positive"] = "addMailReceived atravita_GIMA_PamPositive/" + val;
            }
            foreach (string key in editor.Data.Keys)
            {
                if (key.StartsWith("503180/") && editor.Data[key] is string value)
                {
                    int lastslash = value.LastIndexOf('/');
                    if (lastslash > 0)
                    {
                        editor.Data[key] = value.Insert(lastslash, "/addMailReceived atravita_GIMA_PamInsulted");
                    }
                    break;
                }
            }
        }
    }
}