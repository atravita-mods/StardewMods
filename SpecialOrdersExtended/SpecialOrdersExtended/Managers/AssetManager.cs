using AtraBase.Collections;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace SpecialOrdersExtended.Managers;

internal static class AssetManager
{
    private static readonly string AssetLocation = PathUtilities.NormalizeAssetName("Mods/atravita_SpecialOrdersExtended_DurationOverride");

    internal static void OnLoadAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(AssetLocation))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, int>, AssetLoadPriority.Low);
        }
    }

    /// <summary>
    /// Gets the duration override dictionary.
    /// </summary>
    /// <returns>The duration override dictionary.</returns>
    internal static Dictionary<string, int> GetDurationOverride()
        => Game1.content.Load<Dictionary<string, int>>(AssetLocation);
}