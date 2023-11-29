namespace NPCArrows.Framework;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName arrowLocation = null!;

    private static Lazy<Texture2D> arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));

    /// <summary>
    /// Gets the little arrow texture, greyscale.
    /// </summary>
    internal static Texture2D ArrowTexture => arrowTexture.Value;

    /// <summary>
    /// Initializes asset names.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser) => arrowLocation = parser.ParseAssetName("Mods/atravita/NPCArrows/Arrow");

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(arrowLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/arrow.png", AssetLoadPriority.Exclusive);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (arrowTexture.IsValueCreated && (assets is null || assets.Contains(arrowLocation)))
        {
            arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));
        }
    }
}