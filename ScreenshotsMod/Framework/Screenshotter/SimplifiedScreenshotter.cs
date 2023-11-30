#define TRACELOG

namespace ScreenshotsMod.Framework.Screenshotter;

using System.Diagnostics;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using ScreenshotsMod.Framework.ModModels;

using StardewModdingAPI.Events;

using XRectangle = xTile.Dimensions.Rectangle;

/// <summary>
/// Handles taking screenshots. Optimized for small maps, avoids skiasharp.
/// </summary>
internal sealed class SimplifiedScreenshotter : AbstractScreenshotter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimplifiedScreenshotter"/> class.
    /// </summary>
    /// <param name="gameEvents">The gameloop event manager.</param>
    /// <param name="name">The name of the rule we're processing.</param>
    /// <param name="filename">The tokenized filename.</param>
    /// <param name="scale">The scale of the screenshot.</param>
    /// <param name="target">The target location.</param>
    public SimplifiedScreenshotter(IGameLoopEvents gameEvents, string name, string filename, float scale, GameLocation target)
        : base(gameEvents, name, filename, scale, target)
    {
    }

    /// <inheritdoc />
    internal override void UpdateTicked(object? sender, UpdateTickedEventArgs args) => this.Tick();

    /// <summary>
    /// Does a single tick.
    /// </summary>
    internal void Tick()
    {
        if (!ReferenceEquals(Game1.currentLocation, this.TargetLocation) || Game1.game1.takingMapScreenshot)
        {
            return;
        }

        // this one is fully sync.
        ModEntry.ModMonitor.DebugOnlyLog($"Taking simplified screenshot for {this.TargetLocation.NameOrUniqueName} - {this.Name}.");

#if TRACELOG
        Stopwatch sw = Stopwatch.StartNew();
#endif
        this.TakeScreenshotSimplifed();

#if TRACELOG
        ModEntry.ModMonitor.LogTimespan("taking simplified screenshot", sw);
#endif
        this.DisplayHud();
        ModEntry.ModMonitor.Log($"Took screenshot for rule {this.Name}, saved to {this.Filename}.");
        this.Dispose();
    }

    /// <summary>
    /// A simplified screenshot method, meant for cases where the map is smaller than 32*32. Avoids using Skia.
    /// </summary>
    private void TakeScreenshotSimplifed()
    {
        float scale = this.Scale;
        string filename = this.Filename;
        (int start_x, int start_y, int width, int height) = CalculateBounds(this.TargetLocation);

        int scaled_width = (int)(width * scale);
        int scaled_height = (int)(height * scale);

        // save old state.
        XRectangle old_viewport = Game1.viewport;
        bool old_display_hud = Game1.displayHUD;
        Game1.game1.takingMapScreenshot = true;
        float old_zoom_level = Game1.options.baseZoomLevel;
        Game1.options.baseZoomLevel = 1f;
        RenderTarget2D? cached_lightmap = _lightMapGetter();
        _lightMapSetter(null);

        try
        {
            _allocateLightMap(width, height);
            RenderTarget2D render_target = new(Game1.graphics.GraphicsDevice, width, height, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            Game1.viewport = new XRectangle(0, 0, width, height);
            _draw(Game1.game1, Game1.currentGameTime, render_target);

            // if necessary, re-render to scale.
            RenderTarget2D scaled_render_target;
            if (scaled_height != height || scaled_width != width)
            {
                scaled_render_target = new(Game1.graphics.GraphicsDevice, scaled_width, scaled_height, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
                Game1.game1.GraphicsDevice.SetRenderTarget(scaled_render_target);
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
                Color color = Color.White;
                Game1.spriteBatch.Draw(render_target, Vector2.Zero, render_target.Bounds, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                Game1.spriteBatch.End();
                Game1.game1.GraphicsDevice.SetRenderTarget(null);
            }
            else
            {
                scaled_render_target = render_target;
            }

            string? directory = Path.GetDirectoryName(filename);
            Directory.CreateDirectory(directory!);

            using FileStream fs = new(filename, FileMode.OpenOrCreate);

            scaled_render_target.SaveAsPng(fs, scaled_width, scaled_height);

            if (!ReferenceEquals(scaled_render_target, render_target))
            {
                scaled_render_target.Dispose();
            }
            render_target.Dispose();
            return;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("taking simplified screenshot", ex);
            Game1.game1.GraphicsDevice.SetRenderTarget(null);
            return;
        }
        finally
        {
            if (_lightMapGetter() is RenderTarget2D lightmap)
            {
                lightmap.Dispose();
                _lightMapSetter(null);
            }

            _lightMapSetter(cached_lightmap);
            Game1.options.baseZoomLevel = old_zoom_level;
            Game1.game1.takingMapScreenshot = false;
            Game1.displayHUD = old_display_hud;
            Game1.viewport = old_viewport;
        }
    }
}
