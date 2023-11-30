using System.Reflection;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using ScreenshotsMod.Framework.ModModels;

using StardewModdingAPI.Events;

namespace ScreenshotsMod.Framework.Screenshotter;

/// <summary>
/// The abstract class for a screenshot.
/// </summary>
internal abstract class AbstractScreenshotter : IDisposable
{
    #region delegates
    protected static Func<RenderTarget2D?> _lightMapGetter = null!;

    protected static Action<RenderTarget2D?> _lightMapSetter = null!;

    protected static Action<int, int> _allocateLightMap = null!;

    protected static Action<Game1, GameTime, RenderTarget2D> _draw = null!;
    #endregion

    private bool disposedValue;

    // event handlers
    private IGameLoopEvents gameEvents;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractScreenshotter"/> class.
    /// </summary>
    /// <param name="gameEvents">The gameloop event manager.</param>
    /// <param name="name">The name of the rule we're processing.</param>
    /// <param name="filename">The tokenized filename.</param>
    /// <param name="scale">The scale of the screenshot.</param>
    /// <param name="targetLocation">The target location.</param>
    protected AbstractScreenshotter(IGameLoopEvents gameEvents, string name, string filename, float scale, GameLocation targetLocation)
    {
        ModEntry.ModMonitor.DebugOnlyLog($"Attaching for: {name}");
        gameEvents.UpdateTicked += this.UpdateTicked;
        this.gameEvents = gameEvents;
        this.Name = name;
        this.Filename = FileNameParser.GetFilename(filename);
        this.Scale = scale;
        this.TargetLocation = targetLocation;
    }

    /// <summary>
    /// Gets the name of the rule to process.
    /// </summary>
    protected string Name { get; private set; }

    /// <summary>
    /// Gets the filename to write to.
    /// </summary>
    protected string Filename { get; private set; }

    /// <summary>
    /// Gets the scale.
    /// </summary>
    protected float Scale { get; private set; }

    /// <summary>
    /// Gets the location we're targetting with our screenshot.
    /// </summary>
    protected GameLocation TargetLocation { get; private set; }

    /// <summary>
    /// Gets whether or not this instance has been disposed.
    /// </summary>
    internal bool IsDisposed => this.disposedValue;

    /// <summary>
    /// Initializes reflection delegates.
    /// </summary>
    internal static void Init()
    {
        FieldInfo lightmap = typeof(Game1).GetCachedField("_lightmap", ReflectionCache.FlagTypes.StaticFlags);
        _lightMapGetter = lightmap.GetStaticFieldGetter<RenderTarget2D?>();
        _lightMapSetter = lightmap.GetStaticFieldSetter<RenderTarget2D?>();
        _allocateLightMap = typeof(Game1).GetCachedMethod("allocateLightmap", ReflectionCache.FlagTypes.StaticFlags).CreateDelegate<Action<int, int>>();
        _draw = typeof(Game1).GetCachedMethod("_draw", ReflectionCache.FlagTypes.InstanceFlags).CreateDelegate<Action<Game1, GameTime, RenderTarget2D>>();
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Disposing for {this.Name}.");
            this.gameEvents.UpdateTicked -= this.UpdateTicked;
            this.gameEvents = null!;
            this.Name = null!;
            this.Filename = null!;
            this.TargetLocation = null!;
            this.disposedValue = true;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="AbstractScreenshotter"/> class.
    /// </summary>
    ~AbstractScreenshotter()
    {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         this.Dispose(disposing: false);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The tick method.
    /// </summary>
    /// <param name="sender">smapi (always null).</param>
    /// <param name="args">update ticked arguments.</param>
    internal abstract void UpdateTicked(object? sender, UpdateTickedEventArgs args);

    /// <summary>
    /// Displays "screenshot taken" as a HUD element.
    /// </summary>
    protected void DisplayHud()
    {
        HUDMessage message = new(I18n.PictureTaken(this.Name), HUDMessage.screenshot_type);
        Game1.addHUDMessage(message);
        if (ModEntry.Config.AudioCue)
        {
            Game1.playSound("camera");
        }
        if (ModEntry.Config.ScreenFlash)
        {
            Game1.flashAlpha = 1f;
        }
    }

    /// <summary>
    /// Calculates the bounds for the screenshot, in pixels.
    /// </summary>
    /// <returns>The bounds, in pixels.</returns>
    protected static (int start_x, int start_y, int width, int height) CalculateBounds(GameLocation location)
    {
        int start_x = 0;
        int start_y = 0;
        int width = location.map.DisplayWidth;
        int height = location.map.DisplayHeight;
        string[] fields = location.GetMapPropertySplitBySpaces("ScreenshotRegion");
        if (fields.Length != 0)
        {
            if (!ArgUtility.TryGetInt(fields, 0, out int topLeftX, out string? error)
                || !ArgUtility.TryGetInt(fields, 1, out int topLeftY, out error)
                || !ArgUtility.TryGetInt(fields, 2, out int bottomRightX, out error)
                || !ArgUtility.TryGetInt(fields, 3, out int bottomRightY, out error))
            {
                location.LogMapPropertyError("ScreenshotRegion", fields, error);
            }
            else
            {
                start_x = topLeftX * 64;
                start_y = topLeftY * 64;
                width = ((bottomRightX + 1) * 64) - start_x;
                height = ((bottomRightY + 1) * 64) - start_y;
            }
        }

        return (start_x, start_y, width, height);
    }
}
