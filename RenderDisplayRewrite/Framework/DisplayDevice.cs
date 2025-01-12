using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;
using xTile.Tiles;

using Rectangle = Microsoft.Xna.Framework.Rectangle;
using XRectangle = xTile.Dimensions.Rectangle;

namespace RenderDisplayRewrite.Framework;

/// <summary>
/// A display device manager.
/// </summary>
/// <param name="content">The content manager.</param>
/// <param name="graphics">The graphics device.</param>
/// <param name="monitor">The logger for this class.</param>
internal sealed class DisplayDevice(ContentManager content, GraphicsDevice graphics, IMonitor monitor)
    : IDisplayDevice
{
    private readonly Dictionary<TileSheet, Texture2D> cache = [];

    private SpriteBatch? batch;

    /// <summary>
    /// Gets or sets the modulation color.
    /// </summary>
    internal Color ModulationColor { get; set; } = Color.White;

    /// <inheritdoc/>
    public void BeginScene(SpriteBatch b) => this.batch = b;

    /// <inheritdoc/>
    public void DisposeTileSheet(TileSheet tileSheet) => this.cache.Remove(tileSheet);

    /// <inheritdoc/>
    public void DrawTile(Tile tile, Location location, float layerDepth)
    {
        if (tile is null)
        {
            return;
        }

        if (!this.cache.TryGetValue(tile.TileSheet, out Texture2D? texture))
        {
            texture = this.TryLoadTilesheetTexture(tile.TileSheet);
        }

        if (texture is null || texture.IsDisposed)
        {
            return;
        }

        Rectangle sourceRect = tile.TileSheet.GetTileImageBounds(tile.TileIndex).AsXNARectangle();
        float rotation = GetRotation(tile);
        SpriteEffects effects = GetEffects(tile);

        Vector2 midPoint = new (x: sourceRect.Height / 2, y: sourceRect.Width / 2);
        Vector2 drawPos = new (x: location.X + (midPoint.X * Layer.zoom), y: location.Y + (midPoint.Y * Layer.zoom));
        this.batch?.Draw(
            texture,
            position: drawPos,
            sourceRectangle: sourceRect,
            color: this.ModulationColor,
            rotation: rotation,
            origin: midPoint,
            scale: Layer.zoom,
            effects: effects,
            layerDepth: layerDepth);
    }

    /// <inheritdoc/>
    public void EndScene()
    {
    }

    /// <inheritdoc/>
    public void LoadTileSheet(TileSheet tileSheet)
    {
        if (tileSheet is null)
        {
            return;
        }
        this.TryLoadTilesheetTexture(tileSheet);
    }

    /// <inheritdoc cref="XnaDisplayDevice.SetClippingRegion(XRectangle)"/>
    public void SetClippingRegion(XRectangle clippingRegion)
    {
        int nMaxWidth = graphics.PresentationParameters.BackBufferWidth;
        int nMaxHeight = graphics.PresentationParameters.BackBufferHeight;

        int nClipLeft = Math.Clamp(clippingRegion.X, 0, nMaxWidth);
        int nClipTop = Math.Clamp(clippingRegion.Y, 0, nMaxHeight);

        int nClipRight = Math.Clamp(clippingRegion.X + clippingRegion.Width, 0, nMaxWidth);
        int nClipBottom = Math.Clamp(clippingRegion.Y + clippingRegion.Height, 0, nMaxHeight);

        graphics.Viewport = new Viewport(nClipLeft, nClipTop, nClipRight - nClipLeft, nClipBottom - nClipTop);
    }

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    internal void ClearCache() => this.cache.Clear();

    private static float GetRotation(Tile tile) => tile is RotTile rot ? rot.Rotation : RotTile.ParseRotation(tile);
    private static SpriteEffects GetEffects(Tile tile) => tile is RotTile rot ? rot.Effects : RotTile.ParseEffects(tile);

    internal Texture2D? TryLoadTilesheetTexture(TileSheet tileSheet)
    {
        string source = tileSheet.ImageSource;
        try
        {
            if (DisplayDeviceManager.IsFailure(source))
            {
                return null;
            }

            Texture2D texture = content.Load<Texture2D>(source);
            this.cache[tileSheet] = texture;
            return texture;
        }
        catch (ContentLoadException ex)
        {
            monitor.Log($"Could not load {source}, see log for details.", LogLevel.Error);
            monitor.Log(ex.ToString());
            DisplayDeviceManager.SetFailure(source);
            return null;
        }
    }
}

file static class Extensions
{
    internal static Rectangle AsXNARectangle(this XRectangle rect)
        => new (rect.X, rect.Y, rect.Width, rect.Height);
}