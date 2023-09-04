using AtraBase.Toolkit;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.GameData.Shops;

namespace CameraPan.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = StyleCopErrorConsts.AccessorsNearFields)]
internal static class AssetManager
{
    private static IAssetName arrowLocation = null!;
    private static IAssetName dartsLocation = null!;
    private static IAssetName shopsLocation = null!;

    private static Lazy<Texture2D> arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));

    /// <summary>
    /// Gets the little arrow texture, greyscaled.
    /// </summary>
    internal static Texture2D ArrowTexture => arrowTexture.Value;

    private static Lazy<Texture2D> dartsTexture = new(() => Game1.temporaryContent.Load<Texture2D>(dartsLocation.BaseName));

    /// <summary>
    /// Gets the texture of the darts.
    /// </summary>
    internal static Texture2D DartsTexture => dartsTexture.Value;

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">Game Content Helper</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        arrowLocation = parser.ParseAssetName("Mods/atravita.CameraPan/Arrow");
        dartsLocation = parser.ParseAssetName("Minigames/Darts");
        shopsLocation = parser.ParseAssetName("Data/Shops");
    }

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
        else if (e.NameWithoutLocale.IsEquivalentTo(shopsLocation))
        {
            e.Edit(static (asset) =>
            {
                if (!asset.AsDictionary<string, ShopData>().Data.TryGetValue("Carpenter", out ShopData? shop))
                {
                    ModEntry.ModMonitor.Log($"Could not find Robin's shop to edit.", LogLevel.Warn);
                    return;
                }

                shop.Items.Add(new()
                {
                    ItemId = ModEntry.CAMERA_ID,
                    Price = 2_000,
                });
            });
        }
    }

    /// <summary>
    /// Listens to invalidations as necessary.
    /// </summary>
    /// <param name="assets">Assets to reset, or null to reset anyways.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (arrowTexture.IsValueCreated && (assets is null || assets.Contains(arrowLocation)))
        {
            arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));
        }

        if (dartsTexture.IsValueCreated && (assets is null || assets.Contains(dartsLocation)))
        {
            dartsTexture = new(() => Game1.temporaryContent.Load<Texture2D>(dartsLocation.BaseName));
        }
    }
}
