// Ignore Spelling: Atra Utils

using CommunityToolkit.Diagnostics;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extension methods for adding to specific assets.
/// </summary>
public static class IAssetDataForDictionariesExtension
{
    /// <summary>
    /// Appends a context tag to the context tag asset.
    /// </summary>
    /// <param name="contextTagAsset">The context tag asset.</param>
    /// <param name="key">The key to add to.</param>
    /// <param name="items">The context tags to add.</param>
    public static void AppendContextTag(IAssetDataForDictionary<string, string> contextTagAsset, string key, string? items)
    {
        Guard.IsNotNull(contextTagAsset);
        Guard.IsNotNullOrWhiteSpace(key);
        if (string.IsNullOrWhiteSpace(items))
        {
            return;
        }

        IDictionary<string, string> data = contextTagAsset.Data;
        if (data.TryGetValue(key, out string? prev))
        {
            data[key] = prev + ", " + items;
        }
        else
        {
            data[key] = items;
        }
    }
}
