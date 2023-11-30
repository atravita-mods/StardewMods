#define TRACELOG // enables timing information.

using System.Buffers;
using System.Diagnostics;

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
    private int currentTask = 0;

    private SKSurface surface;

    private readonly int start_x;
    private readonly int start_y;
    private readonly int scaled_width;
    private readonly int scaled_height;

    private State state;

#if TRACELOG
    private Stopwatch watch = new();
#endif

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
                this.surface = null!;
                this.Dispose();
                this.state = State.Error;
                return;
            }
        }
        while (failed);

        if (map_bitmap is null)
        {
            this.surface = null!;
            this.state = State.Error;
            this.Dispose();
            return;
        }

        this.Scale = scale;
        this.start_x = start_x;
        this.start_y = start_y;
        this.scaled_height = scaled_height;
        this.scaled_width = scaled_width;
        this.surface = map_bitmap;
        this.state = State.BeforeTakingMapScreenshot;

#if TRACELOG
        ModEntry.ModMonitor.LogTimespan("setting up complete screenshot", sw);
#endif
    }

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
            switch (this.state)
            {
                case State.BeforeTakingMapScreenshot:
                    this.state = State.TakingMapScreenshot;
                    this.TakeScreenshot();
#if TRACELOG
                    ModEntry.ModMonitor.LogTimespan("initializing full screenshot", this.watch);
#endif
                    this.state = State.TransferToSkia;
                    break;
                case State.TransferToSkia:
                {
                    if (this.currentTask < this.tasks!.Length)
                    {
                        var task = this.tasks[this.currentTask];
                        if (task is null)
                        {
                            this.currentTask++;
                            return;
                        }
                        switch (task.Status)
                        {
                            case TaskStatus.Created:
                            {
                                ModEntry.ModMonitor.DebugOnlyLog($"Starting task {this.currentTask}", LogLevel.Info);
                                task.Start();
                                return;
                            }
                            case TaskStatus.Faulted:
                            {
                                this.state = State.Error;
                                ModEntry.ModMonitor.LogError("transfering screenshot to skia", task.Exception!);
                                this.Dispose();
                                return;
                            }
                            case TaskStatus.RanToCompletion:
                            {
                                ModEntry.ModMonitor.DebugOnlyLog($"Completed task {this.currentTask}", LogLevel.Info);
                                this.currentTask++;
                                return;
                            }
#if TRACELOG
                            case TaskStatus.Running:
                            {
                                ModEntry.ModMonitor.Log($"Awaiting task {this.currentTask}", LogLevel.Info);
                                return;
                            }
#endif
                            default:
                                return;
                        }
                    }
                    else
                    {
                        this.state = State.WritingFile;
                        this.WriteToDisk();
                        return;
                    }
                }
                case State.WritingFile:
                {
                    Task task = this.tasks![0];
                    if (!task.IsCompleted)
                    {
                        // awaiting task
                        return;
                    }
                    else if (task.IsFaulted)
                    {
                        this.state = State.Error;
                        ModEntry.ModMonitor.LogError("writing to disk", task.Exception!);
                        this.Dispose();
                        return;
                    }
                    this.state = State.Complete;
                    this.DisplayHud();
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
            this.tasks = null!;
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
        // todo - hoist scaled render target.

        try
        {
            _allocateLightMap(chunk_size, chunk_size);
            int chunks_wide = (int)Math.Ceiling(this.scaled_width / (float)scaled_chunk_size);
            int chunks_high = (int)Math.Ceiling(this.scaled_height / (float)scaled_chunk_size);

            // tasks!
            this.tasks = new Task[chunks_wide * chunks_high];

            // hoisted buffers. Will note that the ArrayPool here is only going to be used for small scales. Still worth it.
            buffer = ArrayPool<Color>.Shared.Rent(scaled_chunk_size * scaled_chunk_size);
            render_target = new(Game1.graphics.GraphicsDevice, chunk_size, chunk_size, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            for (int dy = 0; dy < chunks_high; dy++)
            {
                for (int dx = 0; dx < chunks_wide; dx++)
                {
                    int current_width = scaled_chunk_size;
                    int current_height = scaled_chunk_size;
                    int current_x = dx * scaled_chunk_size;
                    int current_y = dy * scaled_chunk_size;
                    if (current_x + scaled_chunk_size > this.scaled_width)
                    {
                        current_width += this.scaled_width - (current_x + scaled_chunk_size);
                    }
                    if (current_y + scaled_chunk_size > this.scaled_height)
                    {
                        current_height += this.scaled_height - (current_y + scaled_chunk_size);
                    }
                    if (current_height <= 0 || current_width <= 0)
                    {
                        continue;
                    }

                    Game1.viewport = new XRectangle(dx * chunk_size + this.start_x, dy * chunk_size + this.start_y, chunk_size, chunk_size);
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
                            this.Scale,
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
                    SKBitmap portion_bitmap = new(current_width, current_height, SKColorType.Rgb888x, SKAlphaType.Opaque);
                    CopyToSkia(buffer, portion_bitmap, pixels);

                    Task segment = new(() =>
                    {
#if TRACELOG
                        ModEntry.ModMonitor.DebugOnlyLog($"starting skia task for {current_x} and {current_y} with {current_width} and {current_height} on {Environment.CurrentManagedThreadId}", LogLevel.Info);
#endif
                        portion_bitmap.SetImmutable();
                        this.surface.Canvas.DrawBitmap(portion_bitmap, SKRect.Create(current_x, current_y, current_width, current_height));
                        portion_bitmap.Dispose();
                    });
                    this.tasks[dy * chunks_wide + dx] = segment;

                    if (!ReferenceEquals(scaled_render_target, render_target))
                    {
                        scaled_render_target.Dispose();
                    }
                }
            }

            return;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("taking screenshot", ex);
            Game1.game1.GraphicsDevice.SetRenderTarget(null);
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

    private void WriteToDisk()
    {
        Task task = new(() =>
        {
            // ensure the directory is made.
            string? directory = Path.GetDirectoryName(this.Filename);
            Directory.CreateDirectory(directory!);

            using FileStream fs = new(this.Filename, FileMode.OpenOrCreate);
            this.surface.Snapshot().Encode().SaveTo(fs);
        });
        task.Start();
        this.tasks = [task];
    }

    private static unsafe void CopyToSkia(Color[] buffer, SKBitmap bitmap, int pixels)
    {
        uint* ptr = (uint*)bitmap.GetPixels().ToPointer();
        fixed (Color* bufferPtr = buffer)
        {
            Buffer.MemoryCopy(bufferPtr, ptr, pixels * 4, pixels * 4);
        }
    }

    private enum State
    {
        BeforeTakingMapScreenshot,
        TakingMapScreenshot,
        TransferToSkia,
        WritingFile,
        Complete,
        Error,
    }
}
