using AtraBase.Collections;
using StardewModdingAPI.Events;

namespace PamTries.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/JojaMart"))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low);
        }
    }
}
