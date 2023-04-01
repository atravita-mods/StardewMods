using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    internal static Texture2D ArrowTexture => arrowTexture.Value;

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">Game Content Helper</param>
    internal static void Initialize(IGameContentHelper parser)
        => arrowLocation = parser.ParseAssetName("Mods/atravita.CameraPan/Arrow");

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(arrowLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/arrow.png", AssetLoadPriority.Exclusive);
        }
    }

    internal static void Reset(IReadOnlySet<IAssetName>? assets)
    {
        if (arrowTexture.IsValueCreated && (assets is null || assets.Contains(arrowLocation)))
        {
            arrowTexture = new(() => Game1.content.Load<Texture2D>(arrowLocation.BaseName));
        }
    }
}
