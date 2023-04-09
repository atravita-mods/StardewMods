using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace NPCArrows.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName arrowLocation = null!;

    private static Lazy<Texture2D> arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));

    internal static Texture2D ArrowTexture => arrowTexture.Value;

    /// <summary>
    /// Initializes asset names.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser) => arrowLocation = parser.ParseAssetName("Mods/atravita/NPCArrows/Arrow");

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(arrowLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/arrow.png", AssetLoadPriority.Exclusive);
        }
    }

    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (arrowTexture.IsValueCreated && (assets is null || assets.Contains(arrowLocation)))
        {
            arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));
        }
    }
}