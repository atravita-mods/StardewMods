using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace DresserMiniMenu.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManger
{
    private static IAssetName iconName = null!;

    private static Lazy<Texture2D> _icons = new(() => Game1.temporaryContent.Load<Texture2D>(iconName.BaseName));

    /// <summary>
    /// Gets the texture used for our icons.
    /// </summary>
    internal static Texture2D Icons => _icons.Value;

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
        => iconName = parser.ParseAssetName("Mods/atravita/DresserMiniMenu/Icons");

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(iconName))
        {
            e.LoadFromModFile<Texture2D>("assets/icons.png", AssetLoadPriority.Exclusive);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (_icons.IsValueCreated && (assets is null || assets.Contains(iconName)))
        {
            _icons = new(() => Game1.temporaryContent.Load<Texture2D>(iconName.BaseName)); ;
        }
    }
}
