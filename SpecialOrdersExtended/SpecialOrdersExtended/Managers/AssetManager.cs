using AtraBase.Collections;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

namespace SpecialOrdersExtended.Managers;

public record EmojiData(string AssetName, Point location);

/// <summary>
/// Handles asset management for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName durationOverrude = null!;
    private static IAssetName emojiOverride = null!;

    /// <summary>
    /// Initializes assets for this mod.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        durationOverrude = parser.ParseAssetName("Mods/atravita_SpecialOrdersExtended_DurationOverride");
        emojiOverride = parser.ParseAssetName("Mods/atravita_SpecialOrdersExtended_EmojiOverride");
    }

    /// <summary>
    /// Called when assets are loaded.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void OnLoadAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(durationOverrude))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low);
        }
    }

    /// <summary>
    /// Gets the duration override dictionary.
    /// </summary>
    /// <returns>The duration override dictionary.</returns>
    internal static Dictionary<string, string> GetDurationOverride()
        => Game1.content.Load<Dictionary<string, string>>(durationOverrude.BaseName);
}