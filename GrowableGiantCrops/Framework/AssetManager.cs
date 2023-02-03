using System.Buffers;

using AtraBase.Toolkit.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// Manages loading and editing assets for this mod.
/// </summary>
internal static class AssetManager
{
    /// <summary>
    /// Gets the IAssetName corresponding to the shovel's texture.
    /// </summary>
    internal static IAssetName ToolTextureName { get; private set; } = null!;

    /// <summary>
    /// The const string that starts for running JA/MGC textures through the content pipeline.
    /// </summary>
    internal const string GiantCropPrefix = "Mods/atravita/GrowableBushes/";

    private static Lazy<Texture2D> toolTex = new(() => Game1.content.Load<Texture2D>(ToolTextureName.BaseName));

    /// <summary>
    /// Gets the tool texture.
    /// </summary>
    internal static Texture2D ToolTexture => toolTex.Value;

    /// <summary>
    /// An error texture, used to fill in if a JA/MGC texture is not found.
    /// </summary>
    private static Texture2D errorTex = null!;

    /// <summary>
    /// Initializes the AssetManager.
    /// </summary>
    /// <param name="parser">GameContent helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        ToolTextureName = parser.ParseAssetName("Mods/atravita.GrowableGiantCrops/Shovel");

        var buffer = ArrayPool<Color>.Shared.Rent(48 * 48);
        try
        {
            Array.Fill(buffer, Color.MonoGameOrange);
            Texture2D tex = new(Game1.graphics.GraphicsDevice, 48, 48) { Name = GiantCropPrefix + "ErrorTex" };
            tex.SetData(0, new Rectangle(0, 0, 48, 48), buffer, 0, 48 * 48);
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
            && int.TryParse(e.NameWithoutLocale.BaseName.GetNthChunk('/',  3), out int idx))
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
