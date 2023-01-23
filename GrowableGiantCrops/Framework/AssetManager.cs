using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace GrowableGiantCrops.Framework;
internal static class AssetManager
{
    internal static IAssetName ToolTextureName { get; private set; } = null!;

    private static Lazy<Texture2D> toolTex = new(() => Game1.content.Load<Texture2D>(ToolTextureName.BaseName));

    internal static Texture2D ToolTexture => toolTex.Value;

    internal static void Initialize(IGameContentHelper parser)
    {
        ToolTextureName = parser.ParseAssetName("Mods/atravita.GrowableGiantCrops/Shovel");
    }

    internal static void OnAssetRequested(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(ToolTextureName))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/shovel.png", AssetLoadPriority.Exclusive);
        }
    }
}
