namespace ScreenshotsMod.Framework;

using System.IO;
using System.Text.RegularExpressions;

using StardewModdingAPI.Utilities;

using StardewValley;

/// <summary>
/// Parses tokens out of the filename.
/// </summary>
internal static class FileNameParser
{
    /// <summary>
    /// The default filename.
    /// </summary>
    internal const string DEFAULT_FILENAME = @"{{Default}}/{{Save}}/{{Location}}/{{Date}}.png";

    [RegexPattern]
    private static readonly Regex _parser = new(@"{{([a-zA-Z]+)}}", RegexOptions.Compiled, TimeSpan.FromSeconds(20));

    /// <summary>
    /// Sanitizes a given path.
    /// </summary>
    /// <param name="value">Path to sanitize.</param>
    /// <returns>Sanitized (hopefully) path.</returns>
    internal static string SanitizePath(string value)
    {
        string proposed = PathUtilities.NormalizePath(value);
        string ext = Path.GetExtension(proposed);
        if (!ext.Equals(".png", Constants.TargetPlatform is GamePlatform.Linux or GamePlatform.Android ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            proposed += ".png";
        }

        return proposed;
    }

    /// <summary>
    /// Gets the filename associated with a tokenized string.
    /// </summary>
    /// <param name="tokenized">Tokenized string.</param>
    /// <param name="currentLocation">The location to use.</param>
    /// <param name="ruleName">The name of the rule.</param>
    /// <returns>Filename (sanitized) (hopefully).</returns>
    internal static string GetFilename(string tokenized, GameLocation currentLocation, string ruleName)
    {
        // we must pass in the currentLocation because occasionally Game1.currentLocation is null in multiplayer when farmhands warp.
        var ret = string.Join(
                '_',
                _parser.Replace(tokenized, MatchEvaluator).Split(Path.GetInvalidPathChars()));

        if (Constants.TargetPlatform != GamePlatform.Windows)
        {
            // handle the unix `~` home.
            // took a look at the SMAPI installer for this: https://github.com/Pathoschild/SMAPI/blob/5919337236650c6a0d7755863d35b2923a94775c/src/SMAPI.Installer/InteractiveInstaller.cs#L776C21-L777C66
            // Thanks, Pathos!
            if (ret.StartsWith("~/"))
            {
                string? home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
                if (home is not null)
                {
                    ret = Path.Combine(home, ret[2..]);
                }
            }
        }

        // anything like this for Windows?

        return ret;

        string MatchEvaluator(Match match)
        {
            ReadOnlySpan<char> token = match.Groups[1].ValueSpan.Trim();

            if (token.Length > 256)
            {
                ModEntry.ModMonitor.LogOnce($"Unrecognized token {token}", LogLevel.Warn);
                return match.Value;
            }

            // SAFETY: length was checked earlier, caps to 256
            Span<char> loweredToken = stackalloc char[token.Length + 10];
            int copiedCount = token.ToLowerInvariant(loweredToken);
            if (copiedCount < 0)
            {
                ModEntry.ModMonitor.LogOnce($"Unable to lowercase token {token}", LogLevel.Warn);
                return match.Value;
            }

            loweredToken = loweredToken[..copiedCount];

            return loweredToken switch
            {
                "context" => currentLocation.GetLocationContextId(),
                "date" => $"{Game1.year:D2}_{Game1.seasonIndex + 1:D2}_{Game1.dayOfMonth:D2}", // year_month_day for sorting
                "default" => Game1.game1.GetScreenshotFolder(false),
                "farm" => Game1.player.farmName.Value,
                "location" => currentLocation.NameOrUniqueName,
                "name" => Game1.player.Name,
                "rule" => ruleName,
                "save" => $"{Game1.player.farmName.Value}_{Game1.uniqueIDForThisGame}",
                "time" => $"{Game1.timeOfDay:D4}",
                "timestamp" => $"{DateTime.Now:yyyy.MM.dd HH-mm-ss}",
                "weather" => currentLocation.GetWeather().Weather,
                _ => match.Value,
            };
        }
    }
}
