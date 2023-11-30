#define TRACELOG

namespace ScreenshotsMod;

using AtraCore.Framework.Internal;

using ScreenshotsMod.Framework;
using ScreenshotsMod.Framework.Screenshotter;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using AtraUtils = AtraShared.Utils.Utils;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// The current live screenshotters.
    /// </summary>
    private readonly PerScreen<AbstractScreenshotter?> screenshotters = new(() => null);

    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.Player.Warped += this.OnWarp;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        AbstractScreenshotter.Init();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null || !Config.KeyBind.JustPressed())
        {
            return;
        }

        this.TakeScreenshotImplict(Game1.currentLocation, "keybind", Config.KeyBindFileName, Config.KeyBindScale);
    }

    private void OnWarp(object? sender, WarpedEventArgs e)
    {
        // it's possible for this event to be raised for a "false warp".
        if (e.NewLocation is null || ReferenceEquals(e.NewLocation, e.OldLocation) || !e.IsLocalPlayer || e.NewLocation.IsTemporary)
        {
            return;
        }
    }

    private void TakeScreenshotImplict(GameLocation location, string name, string filename, float scale)
    {
        if (this.screenshotters.Value is { } prev)
        {
            if (prev.IsDisposed)
            {
                this.screenshotters.Value = null;
            }
            else
            {
                this.Monitor.Log($"Previous screenshot is still in effect.", LogLevel.Warn);
                return;
            }
        }

        xTile.Layers.Layer layer = location.Map.Layers[0];
        if (layer.LayerHeight > 32 || layer.LayerWidth > 32)
        {
            CompleteScreenshotter completeScreenshotter = new CompleteScreenshotter(this.Helper.Events.GameLoop, name, filename, scale, location);
            this.screenshotters.Value = completeScreenshotter;
        }
        else
        {
            SimplifiedScreenshotter simple = new(this.Helper.Events.GameLoop, name, filename, scale, location);
            simple.Tick();
            if (!simple.IsDisposed)
            {
                this.screenshotters.Value = simple;
            }
        }
    }
}
