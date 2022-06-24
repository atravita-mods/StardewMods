namespace AtraShared.ConstantsAndEnums;

#pragma warning disable SA1602 // Enumeration items should be documented. Should be obvious enough

/// <summary>
/// Seasons as flags.
/// </summary>
[Flags]
public enum StardewSeasons : uint
{
    None = 0,
    Spring = 0b1,
    Summer = 0b10,
    Fall = 0b100,
    Winter = 0b1000,
    All = Spring | Summer | Fall | Winter,
}

/// <summary>
/// Extensions for the seasons enum.
/// </summary>
internal static class SeasonExtensions
{
    /// <summary>
    /// Parses a list of strings into the season enum.
    /// </summary>
    /// <param name="seasonList">List of strings of seasons...</param>
    /// <returns>Stardew Seasons.</returns>
    internal static StardewSeasons ParseSeasonList(this List<string> seasonList)
    {
        StardewSeasons season = StardewSeasons.None;
        foreach (string? seasonstring in seasonList)
        {
            if (Enum.TryParse(seasonstring, ignoreCase: true, out StardewSeasons s))
            {
                season |= s;
            }
        }
        return season;
    }

    /// <summary>
    /// Gets the StardewSeason enum for the current location.
    /// </summary>
    /// <param name="loc">GameLocation.</param>
    /// <returns>Season.</returns>
#warning - need to fix in Stardew 1.6.
    internal static StardewSeasons GetSeasonFromGame(GameLocation? loc)
        => Utility.getSeasonNumber(Game1.GetSeasonForLocation(loc)) switch
        {
            0 => StardewSeasons.Spring,
            1 => StardewSeasons.Summer,
            2 => StardewSeasons.Fall,
            3 => StardewSeasons.Winter,
            _ => StardewSeasons.None,
        };
}

/// <summary>
/// Weathers as flags....
/// </summary>
[Flags]
public enum StardewWeather : uint
{
    None = 0,
    Sunny = 0b1,
    Rainy = 0b10,
    Stormy = 0b100,
    Snowy = 0b1000,
    Windy = 0b10000,
    All = Sunny | Rainy | Stormy | Snowy | Windy,
}
#pragma warning restore SA1602 // Enumeration items should be documented

/// <summary>
/// Extensions for the weather enum.
/// </summary>
internal static class WeatherExtensions
{
    /// <summary>
    /// Gets a list of strings and parses them to the weatherenum.
    /// </summary>
    /// <param name="weatherList">List of strings of weathers....</param>
    /// <returns>Enum.</returns>
    internal static StardewWeather ParseWeatherList(this List<string> weatherList)
    {
        StardewWeather weather = StardewWeather.None;
        foreach (string? weatherstring in weatherList)
        {
            if (Enum.TryParse(weatherstring, ignoreCase: true, out StardewWeather w))
            {
                weather |= w;
            }
        }
        return weather;
    }
}