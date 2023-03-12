using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace CritterRings.Framework;
internal static class AssetManager
{
    private static IAssetName buffTextureLocation = null!;

    private static Lazy<Texture2D> buffTex = new(() => Game1.content.Load<Texture2D>(buffTextureLocation.BaseName));

    internal static Texture2D BuffTexture => buffTex.Value;

    internal static void Initialize(IGameContentHelper parser)
    {
        buffTextureLocation = parser.ParseAssetName("Mods/atravita/CritterRings/BuffIcon");
    }

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(buffTextureLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/bunnies_fast.png", AssetLoadPriority.Exclusive);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (buffTex.IsValueCreated && (assets is null || assets.Contains(buffTextureLocation)))
        {
            buffTex = new(static () => Game1.content.Load<Texture2D>(buffTextureLocation.BaseName));
        }
    }
}
