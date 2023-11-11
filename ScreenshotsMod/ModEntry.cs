#define TRACELOG

using System.Diagnostics;


using AtraCore.Framework.Internal;

using AtraShared.Utils.Extensions;

using ScreenshotsMod.Framework;

using XRectangle = xTile.Dimensions.Rectangle;

namespace ScreenshotsMod;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{

    private Screenshotter screenshoter = null!;

    // avoid repeatedly allocating arrays.

    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        this.screenshoter = new();

        helper.ConsoleCommands.Add(
            "av.screenshot",
            "Takes a screenshot of the current map",
            (_,_) =>
            {
                var surface = this.screenshoter.TakeScreenshot();
                if (surface is not null)
                    Task.Run(() =>
                    {
                        Screenshotter.WriteBitmap(surface, Path.Combine(Game1.game1.GetScreenshotFolder(), "test-screenshot.png"));
                        surface.Dispose();
                    });
            });
    }
}
