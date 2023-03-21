using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace GrowableBushes.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    /// <summary>
    /// Gets the asset path of the bush shop texture.
    /// </summary>
    internal static IAssetName BushShopTexture { get; private set; } = null!;

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">Game content parser.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        BushShopTexture = parser.ParseAssetName("Mods/atravita/GrowableBushes/BushTAS");
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Load(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(BushShopTexture))
        {
            e.LoadFromModFile<Texture2D>("assets/shop.png", AssetLoadPriority.Exclusive);
        }
    }
}
