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
internal static class ColorHandler
{
    private static readonly Regex ColorCode = new(
        @"^#(?<R>[0-9a-fA-F]{2})(?<G>[0-9a-fA-F]{2})(?<B>[0-9a-fA-F]{2})(?<A>[0-9a-fA-F]{2})?$",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    /// <summary>
    /// Tries to parse a user string to an XNAcolor.
    /// </summary>
    /// <param name="colorname">user string.</param>
    /// <param name="color">XNAcolor.</param>
    /// <returns>True if successful, false otherwise.</returns>
    internal static bool TryParseColor(string colorname, out XNAColor color)
    {
        colorname = colorname.Trim();

        // Try to see if it's a valid KnownColor enum?
        if (Enum.TryParse(colorname, ignoreCase: true, out KnownColor result))
        {
            color = Color.FromKnownColor(result).ToXNAColor();
            return true;
        }

        // Process as HTML color code?
        if (ColorCode.Match(colorname) is Match match && match.Success)
        {
            Dictionary<string, int> matchdict = match.MatchGroupsToDictionary((name) => name, (value) => int.Parse(value, NumberStyles.HexNumber), namedOnly: true);
            color = matchdict.ContainsKey("A")
                ? new(matchdict["R"], matchdict["G"], matchdict["B"], matchdict["A"])
                : new(matchdict["R"], matchdict["G"], matchdict["B"]);
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
            if (byte.TryParse(splits[i], out byte parsed) && parsed <= byte.MaxValue)
            {
                vals[i] = parsed;
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
    internal static Color ToSystemColor(this XNAColor color)
        => Color.FromArgb(color.R, color.G, color.B, color.A);

    /// <summary>
    /// Converts a System color to an XNA color.
    /// </summary>
    /// <param name="color">System color.</param>
    /// <returns>XNA color.</returns>
    internal static XNAColor ToXNAColor(this Color color)
        => new(color.R, color.G, color.B, color.A);

    /// <summary>
    /// Converts an XNA color to a hex string.
    /// </summary>
    /// <param name="color">XNA color.</param>
    /// <returns>Hex string.</returns>
    internal static string ToHexString(this XNAColor color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    /// <summary>
    /// Converts a System color to a hex string.
    /// </summary>
    /// <param name="color">System color.</param>
    /// <returns>Hex string.</returns>
    internal static string ToHexString(this Color color)
    => $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
}

#endif