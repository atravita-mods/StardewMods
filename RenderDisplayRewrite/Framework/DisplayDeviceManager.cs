using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace RenderDisplayRewrite.Framework;

/// <summary>
/// Checks failed loads and refuses to load them.
/// </summary>
internal static class DisplayDeviceManager
{
    private static readonly HashSet<string> Failed = [];

    /// <summary>
    /// Marks an asset as one that has failed to load.
    /// </summary>
    /// <param name="asset">the asset in question.</param>
    internal static void SetFailure(string asset) => Failed.Add(PathUtilities.NormalizeAssetName(asset));

    /// <summary>
    /// Checks to see if an asset has failed to load in the past.
    /// </summary>
    /// <param name="asset">The asset to check.</param>
    /// <returns>If the asset has failed.</returns>
    internal static bool IsFailure(string asset) => Failed.Contains(PathUtilities.NormalizeAssetName(asset));

    /// <summary>
    /// Removes failed assets when they are invalidated.
    /// </summary>
    /// <param name="assets">Assets to remove.</param>
    internal static void Invalidate(IReadOnlySet<IAssetName> assets)
    {
        foreach (IAssetName asset in assets)
        {
            Failed.Remove(asset.Name);
        }
    }

    /// <summary>
    /// Removes failed assets when they are readied.
    /// </summary>
    /// <param name="e">Assets to remove.</param>
    internal static void Ready(AssetReadyEventArgs e) => Failed.Remove(e.Name.Name);
}
