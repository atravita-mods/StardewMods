using NetEscapades.EnumGenerators;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// Seasons as flags, typically used for season constraints.
/// </summary>
[Flags]
[EnumExtensions]
public enum StardewSeasons : uint
{
    /// <summary>
    /// No season constraints.
    /// </summary>
    None = 0,

    /// <summary>
    /// Spring.
    /// </summary>
    Spring = 0b1,

    /// <summary>
    /// Summer.
    /// </summary>
    Summer = 0b10,

    /// <summary>
    /// Fall.
    /// </summary>
    Fall = 0b100,

    /// <summary>
    /// Winter.
    /// </summary>
    Winter = 0b1000,

    /// <summary>
    /// Every season.
    /// </summary>
    All = Spring | Summer | Fall | Winter,
}

/// <summary>
/// Extensions for the seasons enum.
/// </summary>
public static partial class SeasonExtensions
{
    /// <summary>
    /// Parses a list of strings into the season enum.
    /// </summary>
    /// <param name="seasonList">List of strings of seasons...</param>
    /// <returns>Stardew Seasons.</returns>
    public static StardewSeasons ParseSeasonList(this IEnumerable<string> seasonList)
    {
        StardewSeasons season = StardewSeasons.None;
        foreach (string? seasonstring in seasonList)
        {
            if (StardewSeasonsExtensions.TryParse(seasonstring.Trim(), ignoreCase: true, out StardewSeasons s))
            {
                season |= s;
            }
        }
        return season;
    }

    public static bool TryParseSeasonList(this IEnumerable<string> seasonList, out StardewSeasons seasons)
    {
        seasons = StardewSeasons.None;
        foreach (string? seasonstring in seasonList)
        {
            if (StardewSeasonsExtensions.TryParse(seasonstring.Trim(), ignoreCase: true, out StardewSeasons s))
            {
                seasons |= s;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Gets the StardewSeason enum for the current location.
    /// </summary>
    /// <param name="loc">GameLocation.</param>
    /// <returns>Season.</returns>
#warning - need to fix in Stardew 1.6.
    public static StardewSeasons GetSeasonFromGame(GameLocation? loc)
        => Utility.getSeasonNumber(Game1.GetSeasonForLocation(loc)) switch
        {
            0 => StardewSeasons.Spring,
            1 => StardewSeasons.Summer,
            2 => StardewSeasons.Fall,
            3 => StardewSeasons.Winter,
            _ => StardewSeasons.None,
        };

    /// <summary>
    /// Shifts all values in a StardewSeason enum over by one month.
    /// </summary>
    /// <param name="seasons">Initial seasons.</param>
    /// <returns>Seasons shifted by one.</returns>
    public static StardewSeasons GetNextSeason(StardewSeasons seasons)
    {
        var shifted = (uint)seasons << 1;

        if ((shifted & 0b10000) > 0)
        {
            shifted |= 0b1;
            shifted &= 0b1111;
        }
        return (StardewSeasons)shifted;
    }
}

/// <summary>
/// Weathers as flags....
/// </summary>
[Flags]
[EnumExtensions]
public enum StardewWeather : uint
{
    /// <summary>
    /// No weather contraints.
    /// </summary>
    None = 0,

    /// <summary>
    /// Sunny weather.
    /// </summary>
    Sunny = 0b1,

    /// <summary>
    /// Rain
    /// </summary>
    Rainy = 0b10,

    /// <summary>
    /// Storming.
    /// </summary>
    Stormy = 0b100,

    /// <summary>
    /// Snowing (winter only).
    /// </summary>
    Snowy = 0b1000,

    /// <summary>
    /// Windy weather, usually leaves blowing around the screen.
    /// </summary>
    Windy = 0b10000,

    /// <summary>
    /// All weathers.
    /// </summary>
    All = Sunny | Rainy | Stormy | Snowy | Windy,
}

/// <summary>
/// Extensions for the weather enum.
/// </summary>
public static partial class WeatherExtensions
{
    /// <summary>
    /// Gets a list of strings and parses them to the weatherenum.
    /// </summary>
    /// <param name="weatherList">List of strings of weathers....</param>
    /// <returns>Enum.</returns>
    public static StardewWeather ParseWeatherList(this List<string> weatherList)
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