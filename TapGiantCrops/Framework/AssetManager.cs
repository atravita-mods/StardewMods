using AtraBase.Collections;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace TapGiantCrops.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static readonly string ASSETPATH = PathUtilities.NormalizeAssetName("Mods/atravita.TapGiantCrops/TappedObjectOverride");

    private static Lazy<Dictionary<int, string>> overrides = new(() => Game1.content.Load<Dictionary<int, string>>(ASSETPATH));

    internal static void Load(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(ASSETPATH))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<int, string>, AssetLoadPriority.Exclusive);
        }
    }
}
