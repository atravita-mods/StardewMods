// Ignore Spelling: Screenshotter Impl

#define TRACELOG // enables timing information.
#define DETAIL_TIMING

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

using AtraBase.Toolkit.StringHandler;

using AtraShared.Utils.Extensions;

using CommunityToolkit.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.OpenGL;

using SkiaSharp;

using StardewModdingAPI.Events;

using StardewValley.BellsAndWhistles;

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
    // state constants. Grumbles. I would use an enum if C# would let me, but cmpexchange and the volatiles don't make it easy.

    /// <summary>
    /// Initial state, set in constructor.
    /// </summary>
    private const int BeforeTakingMapScreenshot = 0; // initial state

    /// <summary>
    /// Set when the map screenshot starts.
    /// </summary>
    private const int TakingMapScreenshot = 1;

    /// <summary>
    /// Set when the map screenshot finishes, waiting for the queue to finish up.
    /// </summary>
    private const int TransferToSkia = 2;

    /// <summary>
    /// Set by the handle-skia queue after is fully done.
    /// </summary>
    private const int WritingFile = 3;

    /// <summary>
    /// Set when the write task is finished.
    /// </summary>
    private const int Complete = 4;

    /// <summary>
    /// Can be set by literally anyone if something goes wrong.
    /// </summary>
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
    /// <param name="player">The relevant player.</param>
    /// <param name="gameEvents">The gameloop event manager.</param>
    /// <param name="name">The name of the rule we're processing.</param>
    /// <param name="tokenizedFileName">The tokenized filename.</param>
    /// <param name="scale">The scale of the screenshot.</param>
    /// <param name="duringEvent">Whether to fire during an event (true) or wait until the event is over (false).</param>
    /// <param name="target">The target location.</param>
    /// <remarks>Note that if there's an issue with construction, it will immediately dispose itself.</remarks>
    public CompleteScreenshotter(Farmer player, IGameLoopEvents gameEvents, string name, string tokenizedFileName, float scale, bool duringEvent, GameLocation target)
        : base(player, gameEvents, name, tokenizedFileName, scale, duringEvent, target)
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
    protected override void TickImpl()
    {
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

                    this.DisplayHud();
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

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            this.surface?.Dispose();
            this.surface = null!;
            if (this.queue is { } q)
            {
                foreach ((SKBitmap bitmap, SKRect _) in q)
                {
                    bitmap.Dispose();
                }
            }

            this.queue = null!;
            this.writeFileTask = null!;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Takes the screenshot.
    /// This process needs to happen on the UI thread.
    /// </summary>
    private void TakeScreenshot()
    {
        Threading.EnsureUIThread();

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

        RenderTarget2D? render_target = null;
        RenderTarget2D? scaled_render_target = null;
        try
        {
            _allocateLightMap(chunk_size, chunk_size);
            int chunks_wide = (int)Math.Ceiling(this.scaledWidth / (float)scaled_chunk_size);
            int chunks_high = (int)Math.Ceiling(this.scaledHeight / (float)scaled_chunk_size);

            render_target = new(
                Game1.graphics.GraphicsDevice,
                Math.Min(this.width, chunk_size),
                Math.Min(this.height, chunk_size),
                mipMap: false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.DiscardContents);

            // okay, safe to start the skia transfers now.
            new Thread(this.HandleSkiaTransfers).Start();

            for (int dy = 0; dy < chunks_high; dy++)
            {
                for (int dx = 0; dx < chunks_wide; dx++)
                {
                    if (Volatile.Read(ref this.state) == Error)
                    {
                        return;
                    }

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

                    ModEntry.ModMonitor.TraceOnlyLog($"Begin render for {current_x}x{current_y}");

                    XRectangle window = new((dx * chunk_size) + this.startX, (dy * chunk_size) + this.startY, chunk_size, chunk_size);
                    Game1.viewport = window;
                    _draw(Game1.game1, Game1.currentGameTime, render_target);

                    RenderTarget2D target;

                    bool dispose_target = false;

                    // if necessary, re-render to scale. We'll re-render to a correct sized texture because it's much faster to pull the image info from.

                    // no rescaling needed.
                    if (current_height == render_target.Height && current_width == render_target.Width)
                    {
                        target = render_target;
                    }
                    else
                    {
                        if (current_height != scaled_chunk_size || current_width != scaled_chunk_size)
                        {
                            target = new(
                                Game1.graphics.GraphicsDevice,
                                current_width,
                                current_height,
                                mipMap: false,
                                SurfaceFormat.Color,
                                DepthFormat.None,
                                0,
                                RenderTargetUsage.DiscardContents);
                            dispose_target = true;
                        }
                        else
                        {
                            scaled_render_target ??= new(
                                Game1.graphics.GraphicsDevice,
                                scaled_chunk_size,
                                scaled_chunk_size,
                                mipMap: false,
                                SurfaceFormat.Color,
                                DepthFormat.None,
                                0,
                                RenderTargetUsage.DiscardContents);
                            target = scaled_render_target;
                        }

                        Game1.game1.GraphicsDevice.SetRenderTarget(target);
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
                    }

#if DETAIL_TIMING
                    ModEntry.ModMonitor.LogTimespan($"rendering for {current_x}x{current_y}", render);
#endif

#if DETAIL_TIMING
                    Stopwatch getData = Stopwatch.StartNew();
#endif

                    SKBitmap portion_bitmap = new(current_width, current_height, SKColorType.Rgb888x, SKAlphaType.Opaque);
                    CopyTextureToSkia(target, portion_bitmap);

                    if (dispose_target)
                    {
                        target.Dispose();
                    }

#if DETAIL_TIMING
                    ModEntry.ModMonitor.LogTimespan($"get data for {current_x}x{current_y}", getData);
#endif
                    portion_bitmap.SetImmutable();
                    this.queue.Add((portion_bitmap, SKRect.Create(current_x, current_y, current_width, current_height)));

                    // sneakily advancing the screen fade to make things "appear" to go faster.
                    if (Game1.globalFade)
                    {
                        ScreenFade? fade = _fade();
                        if (fade?.fadeIn == true)
                        {
                            fade.fadeToBlackAlpha = Math.Max(0f, fade.fadeToBlackAlpha - fade.globalFadeSpeed);
                        }
                    }
                }
            }

            // if this was changed on us, the screenshot was unsuccessful.
            if (Interlocked.CompareExchange(ref this.state, TransferToSkia, TakingMapScreenshot) == TakingMapScreenshot)
            {
                DisplayEffects();
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

            _lightMapSetter(cached_lightmap);
            Game1.options.baseZoomLevel = old_zoom_level;
            Game1.game1.takingMapScreenshot = false;
            Game1.displayHUD = old_display_hud;
            Game1.viewport = old_viewport;
        }
    }

    /// <summary>
    /// Currently, this is started when the screenshot method finishes setup.
    /// Will call the screenshot write to file when it's done.
    /// </summary>
    private void HandleSkiaTransfers()
    {
        try
        {
            Thread.Sleep(50);

            while (Volatile.Read(ref this.state) == BeforeTakingMapScreenshot)
            {
                ModEntry.ModMonitor.TraceOnlyLog($"waiting for screenshot to start");
                Thread.Sleep(100);
            }

#if TRACELOG
            Stopwatch watch = new();
#endif

            while (true)
            {
                // something has gone wrong somewhere...
                switch (Volatile.Read(ref this.state))
                {
                    case Error:
                        return;
                    case Complete:
                    {
                        ThrowHelper.ThrowInvalidOperationException();
                        return;
                    }
                }

#if TRACELOG
                watch.Start();
#endif

                ModEntry.ModMonitor.TraceOnlyLog($"Attempting to grab work.");
                while (this.queue?.TryTake(out (SKBitmap bitmap, SKRect rect) pair) == true)
                {
                    SKBitmap bitmap = pair.bitmap;
                    SKRect rect = pair.rect;

                    ModEntry.ModMonitor.TraceOnlyLog($"Processing for {rect.Top}x{rect.Left} (size {rect.Height}x{rect.Width}).", LogLevel.Debug);
                    this.surface.Canvas.DrawBitmap(bitmap, rect);
                    bitmap.Dispose();
                }

#if TRACELOG
                watch.Stop();
#endif

                // no more to process.
                if (Volatile.Read(ref this.state) == TransferToSkia && this.queue?.IsEmpty != false)
                {
                    break;
                }

                ModEntry.ModMonitor.TraceOnlyLog($"snoozing");
                Thread.Sleep(100);
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
            return;
        }

        this.WriteToDisk();
    }

    private void WriteToDisk()
    {
        Volatile.Write(ref this.state, WritingFile);
        this.writeFileTask = new(() =>
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Start write to disk for {this.Name}.", LogLevel.Debug);
            string filename = this.Filename;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    // ensure the directory is made.
                    string? directory = Path.GetDirectoryName(this.Filename);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using FileStream fs = new(this.Filename, FileMode.OpenOrCreate);
                    this.surface.Snapshot().Encode().SaveTo(fs);
                    break;
                }
                catch (IOException ex)
                {
                    if (i == 0)
                    {
                        filename = SanitizeFilename(this.Filename);
                    }
                    else
                    {
                        ModEntry.ModMonitor.LogError($"writing to disk", ex);
                    }
                }
            }
        });
        this.writeFileTask.Start();
    }

    /// <summary>
    /// Given a texture the same size as an SKBitmap, transfers the data out of the texture into the bitmap.
    /// </summary>
    /// <param name="texture">The texture.</param>
    /// <param name="bitmap">The bitmap.</param>
    private static void CopyTextureToSkia(RenderTarget2D texture, SKBitmap bitmap)
    {
        const int colorSize = sizeof(uint); // Color is secretly a uint.

        // initial checks.
        Threading.EnsureUIThread();

        // ensure a valid level.
        Guard.IsGreaterThan(texture.LevelCount, 0);

        // ensure my texture is the same size as the bitmap
        Guard.IsEqualTo(texture.Height, bitmap.Height);
        Guard.IsEqualTo(texture.Width, bitmap.Width);
        Guard.IsEqualTo(texture.Format.GetSize(), colorSize);
        Guard.IsNotEqualTo((int)texture.glFormat, (int)PixelFormat.CompressedTextureFormats);

        GL.BindTexture(TextureTarget.Texture2D, texture.glTexture);
        GL.PixelStore(PixelStoreParameter.PackAlignment, colorSize);

        GL.GetTexImageInternal(TextureTarget.Texture2D, 0, texture.glFormat, texture.glType, bitmap.GetPixels());
    }

    private static readonly Lazy<Regex> _fileSanitationRegex = new(
        static () => new Regex(@"^[A-Z]:|^\\\\[.?]\\|^\\\\[a-zA-Z0-9]+\\", RegexOptions.CultureInvariant | RegexOptions.Compiled)
        );

    private static string SanitizeFilename(string originalFilename)
    {
        // sanitize output, hopefully
        Span<char> buffer = stackalloc char[Math.Min(256, originalFilename.Length)];
        ValueStringBuilder builder = new(buffer);

        ReadOnlySpan<char> original = originalFilename;

        Regex regex = _fileSanitationRegex.Value;

        if (regex.Match(originalFilename) is Match match && match.Success)
        {
            builder.Append(match.ValueSpan);
            original = original[match.ValueSpan.Length..];
        }

        foreach (SpanSplitEntry item in original.StreamSplit(Path.GetInvalidFileNameChars()))
        {
            builder.Append(item.Word);
            builder.Append(item.Separator switch
            {
                "" => string.Empty,
                ":" or "/" or "\\" => item.Separator,
                _ => "_"
            });
        }

        return builder.ToString();
    }
}
