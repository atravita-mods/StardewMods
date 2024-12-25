using System.Globalization;

using CommunityToolkit.Diagnostics;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Utilities;

namespace AtraShared.Utils;

/// <summary>
/// Utility methods.
/// </summary>
public static class Utils
{

    /// <summary>
    /// Yields all tiles around a specific tile.
    /// </summary>
    /// <param name="tile">Vector2 location of tile.</param>
    /// <param name="radius">A radius to search in.</param>
    /// <returns>All tiles within radius.</returns>
    /// <remarks>This actually returns a square, not a circle.</remarks>
    public static IEnumerable<Point> YieldSurroundingTiles(Vector2 tile, int radius = 1)
    {
        int x = (int)tile.X;
        int y = (int)tile.Y;
        for (int xdiff = -radius; xdiff <= radius; xdiff++)
        {
            for (int ydiff = -radius; ydiff <= radius; ydiff++)
            {
                yield return new Point(x + xdiff, y + ydiff);
            }
        }
    }

    /// <summary>
    /// Yields an iterator over all tiles on a location.
    /// </summary>
    /// <param name="location">Location to check.</param>
    /// <returns>IEnumerable of all tiles.</returns>
    public static IEnumerable<Vector2> YieldAllTiles(GameLocation location)
    {
        for (int x = 0; x < location.Map.Layers[0].LayerWidth; x++)
        {
            for (int y = 0; y < location.Map.Layers[0].LayerHeight; y++)
            {
                yield return new Vector2(x, y);
            }
        }
    }

    /// <summary>
    /// Sort strings, taking into account CultureInfo of currently selected language.
    /// </summary>
    /// <param name="enumerable">IEnumerable of strings to sort.</param>
    /// <returns>A sorted list of strings.</returns>
    public static List<string> ContextSort(IEnumerable<string> enumerable)
    {
        List<string> outputlist = enumerable.ToList();
        outputlist.Sort(GetCurrentLanguageComparer(ignoreCase: true));
        return outputlist;
    }

    /// <summary>
    /// Sorts strings (in place), taking into account the CultureInfo of the currently selected language.
    /// </summary>
    /// <param name="list">List of strings.</param>
    /// <returns>A sorted list of strings.</returns>
    public static List<string> ContextSort(List<string> list)
    {
        list.Sort(GetCurrentLanguageComparer(ignoreCase: true));
        return list;
    }

    /// <summary>
    /// Returns a StringComparer for the current language the player is using.
    /// </summary>
    /// <param name="ignoreCase">Whether or not to ignore case.</param>
    /// <returns>A string comparer.</returns>
    public static StringComparer GetCurrentLanguageComparer(bool ignoreCase = false)
        => StringComparer.Create(GetCurrentCulture(), ignoreCase);

    /// <summary>
    /// Tries to get a CultureInfo corresponding to the player's current culture. Falls back to the thread
    /// culture.
    /// </summary>
    /// <returns>CultureInfo.</returns>
    public static CultureInfo GetCurrentCulture()
    {
        try
        {
            return new CultureInfo(LocalizedContentManager.LanguageCodeString(Game1.content.GetCurrentLanguage()));
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentCulture;
        }
    }

    /// <summary>
    /// Gets all birthday NPCs.
    /// </summary>
    /// <param name="day">Current date.</param>
    /// <returns>IEnumerable of birthday npcs.</returns>
    public static IEnumerable<NPC> GetBirthdayNPCs(SDate day)
    {
        foreach (NPC npc in NPCHelpers.GetNPCs())
        {
            if (npc is not null && npc.Birthday_Season == day.SeasonKey && npc.Birthday_Day == day.Day)
            {
                yield return npc;
            }
        }
    }

    /// <summary>
    /// Gets the next day, given the specific season and day.
    /// </summary>
    /// <param name="season">Season as string.</param>
    /// <param name="day">Day as int.</param>
    /// <returns>ValueTuple containing the next day.</returns>
    public static (string season, int day) GetTomorrow(string season, int day)
    {
        if (day == 28)
        {
            return season switch
            {
                "spring" => ("summer", 1),
                "summer" => ("fall", 1),
                "fall" => ("winter", 1),
                "winter" => ("spring", 1),
                _ => ThrowHelper.ThrowArgumentException<(string, int)>($"Unexpected season {season}!"),
            };
        }
        else
        {
            return (season, day + 1);
        }
    }

    /// <summary>
    /// Gets the next day, given the specific season and day.
    /// </summary>
    /// <param name="season">Season as enum.</param>
    /// <param name="day">Day as int.</param>
    /// <returns>ValueTuple containing the next day.</returns>
    public static (Season season, int day) GetTomorrow(Season season, int day)
    {
        if (season < Season.Spring || season > Season.Winter)
        {
            return ThrowHelper.ThrowArgumentException<(Season, int)>($"Unexpected season {season}!");
        }
        if (day == 28)
        {
            Season nextSeason = season + 1;
            if (nextSeason > Season.Winter)
            {
                nextSeason = Season.Spring;
            }
            return (nextSeason, 1);
        }
        else
        {
            return (season, day + 1);
        }
    }
}