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
    /// <param name="rectangle">rectangle.</param>
    /// <param name="location">map to clamp to.</param>
    /// <returns>clamped rectangle.</returns>
    internal static Rectangle ClampMap(this Rectangle rectangle, GameLocation location)
    {
        if (location?.Map?.GetLayer("Back") is not { } layer)
        {
            ModEntry.ModMonitor.LogOnce($"{location?.NameOrUniqueName ?? "Unknown Location"} appears to be missing 'back' layer.", LogLevel.Warn);
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
                Width = Math.Clamp(rectangle.Width, 0, layer.LayerHeight - rectangle.X),
                Height = Math.Clamp(rectangle.Height, 0,  layer.LayerHeight - rectangle.Y),
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

    /// <summary>
    /// Tries to split once by a deliminator.
    /// </summary>
    /// <param name="str">Text to split.</param>
    /// <param name="deliminator">Deliminator to split by.</param>
    /// <param name="first">The part that precedes the deliminator, or the whole text if not found.</param>
    /// <param name="second">The part that is after the deliminator.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [Pure]
    internal static bool TrySplitOnce(this ReadOnlySpan<char> str, char deliminator, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
    {
        int idx = str.IndexOf(deliminator);

        if (idx < 0)
        {
            first = str;
            second = [];
            return false;
        }

        first = str[..idx];
        second = str[(idx + 1)..];
        return true;
    }
}