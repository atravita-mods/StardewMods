#define TRACELOG // enables timing information.

using System.Buffers;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SkiaSharp;

using StardewModdingAPI.Events;

using XRectangle = xTile.Dimensions.Rectangle;

namespace ScreenshotsMod.Framework.Screenshotter;

/// <summary>
/// The complex, skia-knitting screenshot.
/// </summary>
internal sealed class CompleteScreenshotter : AbstractScreenshotter
{
    private Task[]? tasks = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteScreenshotter"/> class.
    /// </summary>
    /// <param name="gameEvents">The gameloop event manager.</param>
    /// <param name="name">The name of the rule we're processing.</param>
    /// <param name="filename">The tokenized filename.</param>
    /// <param name="scale">The scale of the screenshot.</param>
    /// <param name="target">The target location.</param>
    public CompleteScreenshotter(IGameLoopEvents gameEvents, string name, string filename, float scale, GameLocation target)
        : base(gameEvents, name, filename, scale, target)
    {
    }

    internal override void UpdateTicked(object? sender, UpdateTickedEventArgs args)
    {
        throw new NotImplementedException();
    }

    // derived from Game1.takeMapScreenshot
    internal SKSurface? TakeScreenshot(float scale = 1f)
    {
        if (Game1.currentLocation is not GameLocation current)
        {
            return null;
        }

        (int start_x, int start_y, int width, int height) = CalculateBounds(this.TargetLocation);

        // create the surface.
        SKSurface? map_bitmap = null;
        bool failed;
        int scaled_width;
        int scaled_height;
        do
        {
            scaled_width = (int)(width * scale);
            scaled_height = (int)(height * scale);
            try
            {
                map_bitmap = SKSurface.Create(new SKImageInfo(scaled_width, scaled_height, SKColorType.Rgb888x, SKAlphaType.Opaque));
                failed = map_bitmap is null; //skia can be dumb.
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"creating bitmap of {scaled_width}x{scaled_height}", ex);
                failed = true;
            }
            if (failed)
            {
                scale -= 0.25f;
            }
            if (scale <= 0f)
            {
                return null;
            }
        }
        while (failed);

        if (map_bitmap is null)
        {
            return null;
        }

        // the game's screenshots work by rendering the map, in chunks, to a render target
        // and then stitching it all together via SkiaSharp.
        const int chunk_size = 2048;
        int scaled_chunk_size = (int)(chunk_size * scale);

        // save old state.
        XRectangle old_viewport = Game1.viewport;
        bool old_display_hud = Game1.displayHUD;
        Game1.game1.takingMapScreenshot = true;
        float old_zoom_level = Game1.options.baseZoomLevel;
        Game1.options.baseZoomLevel = 1f;
        RenderTarget2D? cached_lightmap = _lightMapGetter();
        _lightMapSetter(null);

        Color[]? buffer = null;
        RenderTarget2D? render_target = null;
        SKBitmap? skbuffer = null;

        try
        {
            _allocateLightMap(chunk_size, chunk_size);
            int chunks_wide = (int)Math.Ceiling(scaled_width / (float)scaled_chunk_size);
            int chunks_high = (int)Math.Ceiling(scaled_height / (float)scaled_chunk_size);

            // hoisted buffers. Will note that the ArrayPool here is only going to be used for small scales. Still worth it.
            buffer = ArrayPool<Color>.Shared.Rent(scaled_chunk_size * scaled_chunk_size);
            skbuffer = new(scaled_chunk_size, scaled_chunk_size, SKColorType.Rgb888x, SKAlphaType.Opaque);
            render_target = new(Game1.graphics.GraphicsDevice, chunk_size, chunk_size, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            for (int dy = 0; dy < chunks_high; dy++)
            {
                for (int dx = 0; dx < chunks_wide; dx++)
                {
                    bool useSKBuffer = true;
                    int current_width = scaled_chunk_size;
                    int current_height = scaled_chunk_size;
                    int current_x = dx * scaled_chunk_size;
                    int current_y = dy * scaled_chunk_size;
                    if (current_x + scaled_chunk_size > scaled_width)
                    {
                        current_width += scaled_width - (current_x + scaled_chunk_size);
                        useSKBuffer = false;
                    }
                    if (current_y + scaled_chunk_size > scaled_height)
                    {
                        current_height += scaled_height - (current_y + scaled_chunk_size);
                        useSKBuffer = false;
                    }
                    if (current_height <= 0 || current_width <= 0)
                    {
                        continue;
                    }

                    Game1.viewport = new XRectangle(dx * chunk_size + start_x, dy * chunk_size + start_y, chunk_size, chunk_size);
                    _draw(Game1.game1, Game1.currentGameTime, render_target);

                    // if necessary, re-render to scale.
                    RenderTarget2D scaled_render_target;
                    if (current_width != chunk_size || current_height != chunk_size)
                    {
                        scaled_render_target = new(Game1.graphics.GraphicsDevice, current_width, current_height, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
                        Game1.game1.GraphicsDevice.SetRenderTarget(scaled_render_target);
                        Game1.spriteBatch.Begin(
                            sortMode: SpriteSortMode.Deferred,
                            blendState: BlendState.Opaque,
                            samplerState: SamplerState.PointClamp,
                            depthStencilState: DepthStencilState.Default,
                            rasterizerState: RasterizerState.CullNone);
                        Game1.spriteBatch.Draw(
                            texture: render_target,
                            position: Vector2.Zero,
                            sourceRectangle: render_target.Bounds,
                            color: Color.White,
                            rotation: 0f,
                            origin: Vector2.Zero,
                            scale,
                            effects: SpriteEffects.None,
                            layerDepth: 1f);
                        Game1.spriteBatch.End();
                        Game1.game1.GraphicsDevice.SetRenderTarget(null);
                    }
                    else
                    {
                        scaled_render_target = render_target;
                    }

                    // Get the data out of the scaled render buffer.
                    int pixels = current_height * current_width;
                    scaled_render_target.GetData(buffer, 0, pixels);
                    SKBitmap portion_bitmap = useSKBuffer ? skbuffer : new(current_width, current_height, SKColorType.Rgb888x, SKAlphaType.Opaque);
                    CopyToSkia(buffer, portion_bitmap, pixels);

                    if (!ReferenceEquals(skbuffer, portion_bitmap))
                    {
                        portion_bitmap.SetImmutable();
                    }

                    map_bitmap.Canvas.DrawBitmap(portion_bitmap, SKRect.Create(current_x, current_y, current_width, current_height));
                    if (!ReferenceEquals(skbuffer, portion_bitmap))
                    {
                        portion_bitmap.Dispose();
                    }
                    if (!ReferenceEquals(scaled_render_target, render_target))
                    {
                        scaled_render_target.Dispose();
                    }
                }
            }

            return map_bitmap;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("taking screenshot", ex);
            Game1.game1.GraphicsDevice.SetRenderTarget(null);
            return null;
        }
        finally
        {
            if (_lightMapGetter() is RenderTarget2D lightmap)
            {
                lightmap.Dispose();
                _lightMapSetter(null);
            }

            render_target?.Dispose();
            skbuffer?.Dispose();
            if (buffer is not null)
            {
                ArrayPool<Color>.Shared.Return(buffer);
            }

            _lightMapSetter(cached_lightmap);
            Game1.options.baseZoomLevel = old_zoom_level;
            Game1.game1.takingMapScreenshot = false;
            Game1.displayHUD = old_display_hud;
            Game1.viewport = old_viewport;
        }
    }

    /// <summary>
    /// Writes a bitmap to a file.
    /// </summary>
    /// <param name="image">The SKSurface that holds the image.</param>
    /// <param name="filename">The file to write to.</param>
    /// <remarks>This is split out because it can happen on a background thread.</remarks>
    internal static void WriteBitmap(SKSurface image, string filename)
    {
        // ensure the directory is made.
        string? directory = Path.GetDirectoryName(filename);
        Directory.CreateDirectory(directory!);

        using FileStream fs = new(filename, FileMode.OpenOrCreate);
        image.Snapshot().Encode().SaveTo(fs);
    }

    private static unsafe void CopyToSkia(Color[] buffer, SKBitmap bitmap, int pixels)
    {
        uint* ptr = (uint*)bitmap.GetPixels().ToPointer();
        fixed (Color* bufferPtr = buffer)
        {
            Buffer.MemoryCopy(bufferPtr, ptr, pixels * 4, pixels * 4);
        }
    }
}
