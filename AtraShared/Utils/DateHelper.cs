using StardewModdingAPI.Utilities;

namespace AtraShared.Utils;

/// <summary>
/// Helper methods for Stardew dates.
/// </summary>
public static class DateHelper
{
    #region WinterStar

    /// <summary>
    /// Whether or not the Winter Star letter is valid on this date.
    /// </summary>
    /// <param name="date">The WorldDate.</param>
    /// <returns>True if the Winter Star letter is valid.</returns>
    public static bool IsWinterStarLetterValid(WorldDate date)
        => date.SeasonIndex == 3 && date.DayOfMonth >= 18 && date.DayOfMonth <= 25;

    /// <summary>
    /// Whether or not the Winter Star letter is valid on this date.
    /// </summary>
    /// <param name="date">The SDate.</param>
    /// <returns>True if the Winter Star letter is valid.</returns>
    public static bool IsWinterStarLetterValid(SDate date)
        => date.SeasonIndex == 3 && date.Day >= 18 && date.Day <= 25;
    /// <summary>
    /// Whether or not the Winter Star letter is currently valid.
    /// </summary>
    /// <returns>True if the Winter Star letter is valid.</returns>
    public static bool IsWinterStarLetterValidToday()
        => Game1.IsWinter && Game1.dayOfMonth >= 18 && Game1.dayOfMonth <= 25;
#endregion
}
#pragma warning restore SA1124 // Do not use regions