#define TRACELOG

namespace ScreenshotsMod;

using System.Diagnostics;

using AtraCore.Framework.Internal;

using AtraShared.Utils.Extensions;

using ScreenshotsMod.Framework;

using SkiaSharp;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    private Screenshotter screenshotter = null!;

    private ModConfig config = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        this.screenshotter = new();
        this.config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.Player.Warped += this.OnWarp;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null || !this.config.KeyBind.JustPressed())
        {
            return;
        }

        this.TakeScreenShotImpl(FileNameParser.GetFilename(this.config.KeyBindFileName), this.config.KeyBindScale);
    }

    private void OnWarp(object? sender, WarpedEventArgs e)
    {
        // it's possible for this event to be raised for a "false warp".
        if (e.NewLocation is null || ReferenceEquals(e.NewLocation, e.OldLocation) || !e.IsLocalPlayer || e.NewLocation.IsTemporary)
        {
            return;
        }

        this.Helper.Events.GameLoop.UpdateTicked += this.ScreenshotQueueHandler;
    }

    private void ScreenshotQueueHandler(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.currentLocation is not { } current)
        {
            return;
        }

#if TRACELOG
        Stopwatch stopwatch = Stopwatch.StartNew();
#endif

        var width = current.map.DisplayWidth;
        var height = current.map.DisplayHeight;

        if (width > 32 || height > 32)
        {
            this.TakeScreenShotImpl(Path.Combine(Game1.game1.GetScreenshotFolder(), $"test-screenshot-{Game1.currentLocation.NameOrUniqueName}.png"), 1f);
        }
        else
        {
            this.Monitor.DebugOnlyLog($"simplified screenshot - {width}x{height}");
            this.screenshotter.TakeScreenshotSimplifed(Path.Combine(Game1.game1.GetScreenshotFolder(), $"test-screenshot-{Game1.currentLocation.NameOrUniqueName}.png"), 1f);
        }
        this.Helper.Events.GameLoop.UpdateTicked -= this.ScreenshotQueueHandler;

#if TRACELOG
        this.Monitor.LogTimespan("taking screenshot", stopwatch);
#endif
    }

    private void TakeScreenShotImpl(string filename, float scale = 1f)
    {

        SKSurface? surface = this.screenshotter.TakeScreenshot(scale);
        if (surface is not null)
        {
            Task.Run(() =>
            {
                Screenshotter.WriteBitmap(surface, filename);
                surface.Dispose();
            });
        }
    }
}
