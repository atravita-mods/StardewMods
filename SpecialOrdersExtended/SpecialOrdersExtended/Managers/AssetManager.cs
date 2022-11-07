using AtraBase.Collections;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

namespace SpecialOrdersExtended.Managers;

public record EmojiData(string AssetName, Point Location);

/// <summary>
/// Handles asset management for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName durationOverride = null!;

    internal static IAssetName EmojiOverride { get; private set; } = null!;

    /// <summary>
    /// Initializes assets for this mod.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        durationOverride = parser.ParseAssetName("Mods/atravita_SpecialOrdersExtended_DurationOverride");
        EmojiOverride = parser.ParseAssetName("Mods/atravita_SpecialOrdersExtended_EmojiOverride");
    }

    /// <summary>
    /// Called when assets are loaded.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void OnLoadAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(durationOverride))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low);
        }
        else if(e.NameWithoutLocale.IsEquivalentTo(EmojiOverride))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, EmojiData>, AssetLoadPriority.Low);
        }
    }

    /// <summary>
    /// Gets the duration override dictionary.
    /// </summary>
    /// <returns>The duration override dictionary.</returns>
    internal static Dictionary<string, string> GetDurationOverride()
        => Game1.content.Load<Dictionary<string, string>>(durationOverride.BaseName);
}