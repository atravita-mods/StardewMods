namespace EastScarp;

using System.Diagnostics;

using Microsoft.Xna.Framework;

/// <summary>
/// The extensions for this mod.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Given a Rectangle area, clamps it to the current map.
    /// </summary>
    /// <param name="rectangle"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    internal static Rectangle ClampMap(this Rectangle rectangle, GameLocation location)
    {
        if (location?.Map?.GetLayer("Back") is not { } layer)
        {
            ModEntry.ModMonitor.LogOnce($"{location?.NameOrUniqueName ?? "Unknown Location"} appears to be missing 'back' layer");
            return Rectangle.Empty;
        }
        else
        {
            if (rectangle.Width <= 0)
            {
                rectangle.Width = layer.LayerWidth - rectangle.X;
            }
            if (rectangle.Height <= 0)
            {
                rectangle.Height = layer.LayerHeight - rectangle.Y;
            }

            return new Rectangle()
            {
                X = Math.Clamp(rectangle.X, 0, layer.LayerWidth),
                Y = Math.Clamp(rectangle.Y, 0, layer.LayerHeight),
                Width = Math.Clamp(0, rectangle.Width, layer.LayerHeight - rectangle.X),
                Height = Math.Clamp(0, rectangle.Height, layer.LayerHeight - rectangle.Y),
            };
        }
    }

    /// <summary>
    /// Logs an exception.
    /// </summary>
    /// <param name="monitor">Logging instance to use.</param>
    /// <param name="action">The current actions being taken when the exception happened.</param>
    /// <param name="ex">The exception.</param>
    [DebuggerHidden]
    internal static void LogError(this IMonitor monitor, string action, Exception ex)
    {
        monitor.Log($"Mod failed while {action}, see log for details.", LogLevel.Error);
        monitor.Log(ex.ToString());
    }
}