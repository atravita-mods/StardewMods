#if COLORS

using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;
using XNAColor = Microsoft.Xna.Framework.Color;

namespace AtraShared.Utils;

/// <summary>
/// Helper functions to deal with parsing colors from user input.
/// </summary>
public static class ColorHandler
{
    /// <summary>
    /// Tries to parse a user string to an XNAcolor.
    /// </summary>
    /// <param name="colorname">user string.</param>
    /// <param name="color">XNAcolor.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool TryParseColor(string colorname, out XNAColor color)
    {
        // Enum.TryParse doesn't accept a ReadOnlySpan<char> until NET 6.
        colorname = colorname.Trim();

        // Try to see if it's a valid KnownColor enum?
        if (Enum.TryParse(colorname, ignoreCase: true, out KnownColor result))
        {
            color = Color.FromKnownColor(result).ToXNAColor();
            return true;
        }

        // Process as HTML color code?
        if (colorname.StartsWith('#'))
        {
            int len;
            switch (colorname.Length)
            {
                case 4:
                case 5:
                    len = 1;
                    break;
                case 7:
                case 9:
                    len = 2;
                    break;
                default:
                    goto ColorParseFail;
            }
            ReadOnlySpan<char> span = colorname.AsSpan();
            byte[] array = { 255, 255, 255, 255 };
            for (int i = 0; i < 4; i++)
            {
                if (i == 3 && colorname.Length is 4 or 7)
                {
                    break;
                }

                // 1 <- the offset for the # at the start
                if (byte.TryParse(span.Slice((i * len) + 1, len), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte parsed))
                {
                    array[i] = len == 2 ? parsed : (byte)(parsed * 0x11);
                }
                else
                {
                    goto ColorParseFail;
                }
            }
            color = new(array[0], array[1], array[2], array[3]);
            return true;
        }

        // Try to split and process it that way?
        SpanSplit splits = colorname.SpanSplit(new[] { '/', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!splits.TryGetAtIndex(2, out _))
        {
            goto ColorParseFail;
        }

        byte[] vals = new byte[splits.TryGetAtIndex(3, out _) ? 4 : 3];
        for (int i = 0; i < vals.Length; i++)
        {
            ReadOnlySpan<char> split = splits[i].Word.Trim();
            bool percent = false;
            if (split.EndsWith("%"))
            {
                split = split[0..^1];
                percent = true;
            }
            if (byte.TryParse(split, out byte parsed))
            {
                vals[i] = percent ? (byte)(parsed * 2.55) : parsed;
            }
            else
            {
                goto ColorParseFail;
            }
        }
        color = vals.Length >= 4
            ? new(vals[0], vals[1], vals[2], vals[3])
            : new(vals[0], vals[1], vals[2]);
        return true;

ColorParseFail:
        color = XNAColor.White;
        return false;
    }

    /// <summary>
    /// Converts an XNA color to a system color.
    /// </summary>
    /// <param name="color">XNA color.</param>
    /// <returns>System color.</returns>
    public static Color ToSystemColor(this XNAColor color)
        => Color.FromArgb(color.R, color.G, color.B, color.A);

    /// <summary>
    /// Converts a System color to an XNA color.
    /// </summary>
    /// <param name="color">System color.</param>
    /// <returns>XNA color.</returns>
    public static XNAColor ToXNAColor(this Color color)
        => new(color.R, color.G, color.B, color.A);

    /// <summary>
    /// Converts an XNA color to a hex string.
    /// </summary>
    /// <param name="color">XNA color.</param>
    /// <returns>Hex string.</returns>
    public static string ToHexString(this XNAColor color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    /// <summary>
    /// Converts a System color to a hex string.
    /// </summary>
    /// <param name="color">System color.</param>
    /// <returns>Hex string.</returns>
    public static string ToHexString(this Color color)
    => $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
}

#endif