using AtraBase.Collections;

using StardewModdingAPI.Events;

namespace SpecialOrdersExtended.Managers;

/// <summary>
/// Handles asset management for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName assetLocation = null!;

    /// <summary>
    /// Initializes assets for this mod.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
        => assetLocation = parser.ParseAssetName("Mods/atravita_SpecialOrdersExtended_DurationOverride");

    /// <summary>
    /// Called when assets are loaded.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void OnLoadAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(assetLocation))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low);
        }
    }

    /// <summary>
    /// Gets the duration override dictionary.
    /// </summary>
    /// <returns>The duration override dictionary.</returns>
    internal static Dictionary<string, string> GetDurationOverride()
        => Game1.content.Load<Dictionary<string, string>>(assetLocation.BaseName);
}