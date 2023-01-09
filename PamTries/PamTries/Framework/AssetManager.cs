using AtraBase.Collections;
using StardewModdingAPI.Events;

namespace PamTries.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName joja = null!;

    internal static void Initialize(IGameContentHelper parser)
        => joja = parser.ParseAssetName("Data/Events/JojaMart");

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(joja))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low);
        }
    }
}
