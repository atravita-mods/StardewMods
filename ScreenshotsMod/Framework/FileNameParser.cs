namespace ScreenshotsMod.Framework;

using System.Text.RegularExpressions;

using StardewModdingAPI.Utilities;

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
    /// <returns>Filename (sanitized) (hopefully).</returns>
    internal static string GetFilename(string tokenized)
        => string.Join(
            '_',
            _parser.Replace(tokenized, MatchEvaluator).Split(Path.GetInvalidPathChars()));

    private static string MatchEvaluator(Match match)
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

        GameLocation currentLocation = Game1.currentLocation;
        return loweredToken switch
        {
            "default" => Game1.game1.GetScreenshotFolder(false),
            "location" => currentLocation.NameOrUniqueName,
            "save" => $"{Game1.player.farmName.Value}_{Game1.uniqueIDForThisGame}",
            "farm" => Game1.player.farmName.Value,
            "name" => Game1.player.Name,
            "date" => $"{Game1.year:D2}_{Game1.seasonIndex + 1:D2}_{Game1.dayOfMonth:D2}", // year_month_day for sorting
            "weather" => currentLocation.GetWeather().Weather,
            "time" => $"{Game1.timeOfDay:D4}",
            "timestamp" => $"{DateTime.Now:yyyy.MM.dd HH-mm-ss}",
            _ => match.Value,
        };
    }
}
