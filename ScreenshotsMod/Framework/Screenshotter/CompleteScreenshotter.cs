// Ignore Spelling: Screenshotter

// #define TRACELOG // enables timing information.
// #define DETAIL_TIMING

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SkiaSharp;

using StardewModdingAPI.Events;

using XRectangle = xTile.Dimensions.Rectangle;

namespace ScreenshotsMod.Framework.Screenshotter;

/*****************
* This class needs to do half its work on the main thread, and half on a background thread. Roughly ->
*
* - Initialize SKCanvas happens in the constructor and on the main thread.
* - Map screenshot is taken on the main thread and SKBitmaps are queued.
* - A worker thread transfers the SKBitmaps to the canvas
* - A Task is started to write the file to disk
* - The task is polled repeatedly. When that completes, the entire class is disposed.
******************/

/// <summary>
/// The complex, skia-knitting screenshot.
/// </summary>
internal sealed class CompleteScreenshotter : AbstractScreenshotter
{
    // state constants. Grumbles.
    private const int BeforeTakingMapScreenshot = 0;
    private const int TakingMapScreenshot = 1;
    private const int TransferToSkia = 2;
    private const int WritingFile = 3;
    private const int Complete = 4;
    private const int Error = 5;

#if TRACELOG
    private readonly Stopwatch watch = new();
#endif

    private readonly int startX;
    private readonly int startY;
    private readonly int scaledWidth;
    private readonly int scaledHeight;
    private readonly int width;
    private readonly int height;

    private ConcurrentBag<(SKBitmap, SKRect)> queue = [];
    private Task? writeFileTask;

    private SKSurface surface;

    private int state;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteScreenshotter"/> class.
    /// </summary>
    /// <param name="gameEvents">The gameloop event manager.</param>
    /// <param name="name">The name of the rule we're processing.</param>
    /// <param name="filename">The tokenized filename.</param>
    /// <param name="scale">The scale of the screenshot.</param>
    /// <param name="target">The target location.</param>
    /// <remarks>Note that if there's an issue with construction, it will immediately dispose itself.</remarks>
    public CompleteScreenshotter(Farmer player, IGameLoopEvents gameEvents, string name, string filename, float scale, GameLocation target)
        : base(player, gameEvents, name, filename, scale, target)
    {
#if TRACELOG
        Stopwatch sw = Stopwatch.StartNew();
#endif

        // prepare surface
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
                failed = map_bitmap is null; // skia can be dumb.
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
                this.surface = null!;
                this.Dispose();
                this.state = Error;
                return;
            }
        }
        while (failed);

        if (map_bitmap is null)
        {
            this.surface = null!;
            this.state = Error;
            this.Dispose();
            return;
        }

        this.Scale = scale;
        this.startX = start_x;
        this.startY = start_y;
        this.scaledHeight = scaled_height;
        this.scaledWidth = scaled_width;
        this.width = width;
        this.height = height;
        this.surface = map_bitmap;
        this.state = BeforeTakingMapScreenshot;

#if TRACELOG
        ModEntry.ModMonitor.LogTimespan("setting up complete screenshot", sw);
#endif
    }

    /// <inheritdoc />
    internal override void UpdateTicked(object? sender, UpdateTickedEventArgs args) => this.Tick();

    internal void Tick()
    {
        if (!ReferenceEquals(this.Player, Game1.player) || !ReferenceEquals(Game1.currentLocation, this.TargetLocation) || Game1.game1.takingMapScreenshot)
        {
            return;
        }
#if TRACELOG
        this.watch.Start();
        try
        {
#endif
            int state = Volatile.Read(ref this.state);
            switch (state)
            {
                case BeforeTakingMapScreenshot:
                    Volatile.Write(ref this.state, TakingMapScreenshot);
                    this.TakeScreenshot();
#if TRACELOG
                    ModEntry.ModMonitor.LogTimespan("initializing full screenshot", this.watch);
#endif
                    Volatile.Write(ref this.state, TransferToSkia);
                    Thread t = new(this.HandleSkiaTransfers); // will start the WritingFile task when it's done.
                    t.Start();
                    this.DisplayHud();
                    break;
                case WritingFile:
                {
                    if (this.writeFileTask is not Task task)
                    {
                        return;
                    }

                    if (!task.IsCompleted)
                    {
                        // awaiting task
                        return;
                    }
                    else if (task.IsFaulted)
                    {
                        Volatile.Write(ref this.state, Error);
                        ModEntry.ModMonitor.LogError("writing to disk", task.Exception!);
                        this.Dispose();
                        return;
                    }
                    Volatile.Write(ref this.state, Complete);
#if TRACELOG
                    ModEntry.ModMonitor.LogTimespan("taking full screenshot", this.watch);
#endif
                    this.Dispose();
                    return;
                }
            }
#if TRACELOG
        }
        finally
        {
            this.watch.Stop();
        }
#endif
    }

    protected override void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            this.surface?.Dispose();
            this.surface = null!;
            this.queue = null!;
            this.writeFileTask = null!;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Takes the screenshot.
    /// As of this point, we are fully sync still.
    /// </summary>
    private void TakeScreenshot()
    {
        // the game's screenshots work by rendering the map, in chunks, to a render target
        // and then stitching it all together via SkiaSharp.
        // derived from Game1.takeMapScreenshot
        const int chunk_size = 2048;
        int scaled_chunk_size = (int)(chunk_size * this.Scale);

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
        RenderTarget2D? scaled_render_target = null;

        bool needs_scaling = this.Scale != 1f;

        try
        {
            _allocateLightMap(chunk_size, chunk_size);
            int chunks_wide = (int)Math.Ceiling(this.scaledWidth / (float)scaled_chunk_size);
            int chunks_high = (int)Math.Ceiling(this.scaledHeight / (float)scaled_chunk_size);

            // hoisted buffers. Will note that the ArrayPool here is only going to be used for small scales. Still worth it.
            buffer = ArrayPool<Color>.Shared.Rent(scaled_chunk_size * scaled_chunk_size);
            render_target = new(Game1.graphics.GraphicsDevice, chunk_size, chunk_size, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            if (needs_scaling)
            {
                ModEntry.ModMonitor.TraceOnlyLog($"Scaling requested, creating scaled render target", LogLevel.Info);
                scaled_render_target = new(Game1.graphics.GraphicsDevice, scaled_chunk_size, scaled_chunk_size, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }

            for (int dy = 0; dy < chunks_high; dy++)
            {
                for (int dx = 0; dx < chunks_wide; dx++)
                {
                    int current_x = dx * scaled_chunk_size;
                    int current_y = dy * scaled_chunk_size;

                    int current_width = Math.Min(scaled_chunk_size, this.scaledWidth - current_x);
                    int current_height = Math.Min(scaled_chunk_size, this.scaledHeight - current_y);
                    if (current_height <= 0 || current_width <= 0)
                    {
                        continue;
                    }

#if DETAIL_TIMING
                    Stopwatch render = Stopwatch.StartNew();
#endif

                    XRectangle window = new((dx * chunk_size) + this.startX, (dy * chunk_size) + this.startY, chunk_size, chunk_size);
                    Game1.viewport = window;
                    _draw(Game1.game1, Game1.currentGameTime, render_target);

                    RenderTarget2D target;

                    // if necessary, re-render to scale.
                    if (needs_scaling)
                    {
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
                            sourceRectangle: new(0, 0, Math.Min(chunk_size, this.width - window.X), Math.Min(chunk_size, this.height - window.Y)),
                            color: Color.White,
                            rotation: 0f,
                            origin: Vector2.Zero,
                            this.Scale,
                            effects: SpriteEffects.None,
                            layerDepth: 1f);
                        Game1.spriteBatch.End();
                        Game1.game1.GraphicsDevice.SetRenderTarget(null);
                        target = scaled_render_target!;
                    }
                    else
                    {
                        target = render_target;
                    }

#if DETAIL_TIMING
                    ModEntry.ModMonitor.LogTimespan("rendering", render);
#endif

#if DETAIL_TIMING
                    Stopwatch getData = Stopwatch.StartNew();
#endif

                    // Get the data out of the scaled render buffer.
                    int pixels = current_height * current_width;
                    target.GetData(0, new Rectangle(0, 0, current_width, current_height), buffer, 0, pixels);

#if DETAIL_TIMING
                    ModEntry.ModMonitor.LogTimespan("get data", getData);
#endif

#if DETAIL_TIMING
                    Stopwatch watch = Stopwatch.StartNew();
#endif

                    SKBitmap portion_bitmap = new(current_width, current_height, SKColorType.Rgb888x, SKAlphaType.Opaque);
                    CopyToSkia(buffer, portion_bitmap, pixels);

#if DETAIL_TIMING
                    ModEntry.ModMonitor.LogTimespan("creating and transfer to skia bitmap", watch);
#endif

                    portion_bitmap.SetImmutable();
                    this.queue.Add((portion_bitmap, SKRect.Create(current_x, current_y, current_width, current_height)));
                }
            }

            return;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("taking screenshot", ex);
            Game1.game1.GraphicsDevice.SetRenderTarget(null);
            Volatile.Write(ref this.state, Error);
            this.Dispose();
        }
        finally
        {
            if (_lightMapGetter() is RenderTarget2D lightmap)
            {
                lightmap.Dispose();
                _lightMapSetter(null);
            }

            render_target?.Dispose();
            scaled_render_target?.Dispose();

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

    private void HandleSkiaTransfers()
    {
        try
        {
            while (Volatile.Read(ref this.state) != TransferToSkia)
            {
#if TRACELOG
                ModEntry.ModMonitor.Log($"waiting for Transfer to start");
#endif
                Thread.Sleep(100);
            }

#if TRACELOG
            Stopwatch watch = Stopwatch.StartNew();
#endif

            // all my work is queued. Let's start processing.
            while (this.queue.TryTake(out (SKBitmap bitmap, SKRect rect) pair))
            {
                SKBitmap bitmap = pair.bitmap;
                SKRect rect = pair.rect;

                ModEntry.ModMonitor.TraceOnlyLog($"Processing for {rect.Top}x{rect.Left} (size {rect.Height}x{rect.Width}).", LogLevel.Debug);
                this.surface.Canvas.DrawBitmap(bitmap, rect);
                bitmap.Dispose();
            }

#if TRACELOG
            ModEntry.ModMonitor.LogTimespan("transferring to skia", watch);
#endif
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("transferring to skia", ex);
            Volatile.Write(ref this.state, Error);
            this.Dispose();
        }

        Volatile.Write(ref this.state, WritingFile);
        this.WriteToDisk();
    }

    private void WriteToDisk()
    {
        this.writeFileTask = new(() =>
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Start write to disk for {this.Name}.", LogLevel.Debug);

            // ensure the directory is made.
            string? directory = Path.GetDirectoryName(this.Filename);
            Directory.CreateDirectory(directory!);

            using FileStream fs = new(this.Filename, FileMode.OpenOrCreate);
            this.surface.Snapshot().Encode().SaveTo(fs);
        });
        this.writeFileTask.Start();
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
