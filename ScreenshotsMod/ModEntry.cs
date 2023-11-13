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

        helper.ConsoleCommands.Add(
            "av.screenshot",
            "Takes a screenshot of the current map",
            (_,_) =>
            {
                SKSurface? surface = this.screenshotter.TakeScreenshot();
                if (surface is not null)
                    Task.Run(() =>
                    {
                        Screenshotter.WriteBitmap(surface, Path.Combine(Game1.game1.GetScreenshotFolder(), "test-screenshot.png"));
                        surface.Dispose();
                    });
            });

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
        this.Helper.Events.GameLoop.UpdateTicked += this.ScreenshotQueueHandler;
    }

    private void ScreenshotQueueHandler(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.currentLocation is null)
        {
            return;
        }

        this.TakeScreenShotImpl(Path.Combine(Game1.game1.GetScreenshotFolder(), $"test-screenshot-{Game1.currentLocation.NameOrUniqueName}.png"), 1f);

        this.Helper.Events.GameLoop.UpdateTicked -= this.ScreenshotQueueHandler;
    }

    private void TakeScreenShotImpl(string filename, float scale = 1f)
    {
#if DEBUG
        Stopwatch stopwatch = Stopwatch.StartNew();
#endif
        SKSurface? surface = this.screenshotter.TakeScreenshot(scale);
        this.Monitor.LogTimespan("preparing screenshot", stopwatch);
        if (surface is not null)
        {
            Task.Run(() =>
            {
                Screenshotter.WriteBitmap(surface,  filename);
                surface.Dispose();
            });
        }

#if DEBUG
        this.Monitor.LogTimespan("taking screenshot", stopwatch);
#endif
    }
}
