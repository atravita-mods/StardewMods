using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace ShopTabs.Framework;
internal static class AssetEditor
{
    private static IAssetName emptyTabLocation = null!;
    private static Lazy<Texture2D> emptyTab = new(static () => Game1.content.Load<Texture2D>(emptyTabLocation.BaseName));

    /// <summary>
    /// Gets the empty tab texture.
    /// </summary>
    internal static Texture2D EmptyTab => emptyTab.Value;

    /// <summary>
    /// Initializes assets for this mod.
    /// </summary>
    /// <param name="parser">GameContentHelper.</param>
    internal static void Init(IGameContentHelper parser) => emptyTabLocation = parser.ParseAssetName("Mods/atravita.ShopTabs/emptyTab");

    /// <summary>
    /// Refreshes lazies.
    /// </summary>
    /// <param name="assets">IReadOnlySet of assets to refresh.</param>
    internal static void Refresh(IReadOnlySet<IAssetName>? assets = null)
    {
        if (emptyTab.IsValueCreated && (assets is null || assets.Contains(emptyTabLocation)))
        {
            emptyTab = new(static () => Game1.content.Load<Texture2D>(emptyTabLocation.BaseName));
        }
    }

    internal static void Edit(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(emptyTabLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/emptytab.png", AssetLoadPriority.Exclusive);
        }
    }
}
