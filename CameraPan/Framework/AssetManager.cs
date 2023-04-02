using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace CameraPan.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName arrowLocation = null!;

    private static Lazy<Texture2D> arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));

    /// <summary>
    /// Gets the little arrow texture, greyscaled.
    /// </summary>
    internal static Texture2D ArrowTexture => arrowTexture.Value;

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">Game Content Helper</param>
    internal static void Initialize(IGameContentHelper parser)
        => arrowLocation = parser.ParseAssetName("Mods/atravita.CameraPan/Arrow");

    /// <summary>
    /// Loads in the arrow asset.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(arrowLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/arrow.png", AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>
    /// Listens to invalidations as necessary.
    /// </summary>
    /// <param name="assets">Assets to reset, or null to reset anyways.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets)
    {
        if (arrowTexture.IsValueCreated && (assets is null || assets.Contains(arrowLocation)))
        {
            arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));
        }
    }
}
