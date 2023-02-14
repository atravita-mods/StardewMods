using System.Buffers;

using AtraBase.Toolkit.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace GrowableGiantCrops.Framework.Assets;

/// <summary>
/// Manages loading and editing assets for this mod.
/// </summary>
internal static class AssetManager
{
    /// <summary>
    /// The const string that starts for running JA/MGC textures through the content pipeline.
    /// </summary>
    internal const string GiantCropPrefix = "Mods/atravita/GrowableBushes/";

    private static IAssetName fruitTreeData = null!;

    /// <summary>
    /// An error texture, used to fill in if a JA/MGC texture is not found.
    /// </summary>
    private static Texture2D errorTex = null!;

    private static Lazy<Texture2D> toolTex = new(() => Game1.content.Load<Texture2D>(ToolTextureName!.BaseName));

    /// <summary>
    /// Gets the tool texture.
    /// </summary>
    internal static Texture2D ToolTexture => toolTex.Value;

    /// <summary>
    /// Gets the IAssetName corresponding to the shovel's texture.
    /// </summary>
    internal static IAssetName ToolTextureName { get; private set; } = null!;

    /// <summary>
    /// Initializes the AssetManager.
    /// </summary>
    /// <param name="parser">GameContent helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        ToolTextureName = parser.ParseAssetName("Mods/atravita.GrowableGiantCrops/Shovel");
        fruitTreeData = parser.ParseAssetName(@"Data\fruitTrees");

        const int TEX_WIDTH = 48;
        const int TEX_HEIGHT = 64;
        Color[] buffer = ArrayPool<Color>.Shared.Rent(TEX_WIDTH * TEX_HEIGHT);
        try
        {
            Array.Fill(buffer, Color.MonoGameOrange, 0, TEX_WIDTH * TEX_HEIGHT);
            Texture2D tex = new(Game1.graphics.GraphicsDevice, TEX_WIDTH, TEX_HEIGHT) { Name = GiantCropPrefix + "ErrorTex" };
            tex.SetData(0, new Rectangle(0, 0, TEX_WIDTH, TEX_HEIGHT), buffer, 0, TEX_WIDTH * TEX_HEIGHT);
            errorTex = tex;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while creating error tex:\n\n{ex}", LogLevel.Error);
        }
        finally
        {
            ArrayPool<Color>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested" />
    internal static void OnAssetRequested(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(ToolTextureName))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/shovel.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.StartsWith(GiantCropPrefix, false, false)
            && int.TryParse(e.NameWithoutLocale.BaseName.GetNthChunk('/', 3), out int idx))
        {
            if (ModEntry.JaAPI?.TryGetGiantCropSprite(idx, out Lazy<Texture2D>? lazy) == true)
            {
                e.LoadFrom(() => lazy.Value, AssetLoadPriority.Exclusive);
            }
            else if (ModEntry.MoreGiantCropsAPI?.GetTexture(idx) is Texture2D tex)
            {
                e.LoadFrom(() => tex, AssetLoadPriority.Exclusive);
            }
            else
            {
                e.LoadFrom(() => errorTex, AssetLoadPriority.Exclusive);
            }
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated" />
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if ((assets is null || assets.Contains(ToolTextureName)) && toolTex.IsValueCreated)
        {
            toolTex = new(() => Game1.content.Load<Texture2D>(ToolTextureName.BaseName));
        }
    }
}
