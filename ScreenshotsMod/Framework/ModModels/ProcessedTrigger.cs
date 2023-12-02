using ScreenshotsMod.Framework.UserModels;

namespace ScreenshotsMod.Framework.ModModels;

/// <summary>
/// Represents a processed trigger condition.
/// </summary>
/// <param name="Day">The packed day.</param>
/// <param name="Times">The allowed times.</param>
/// <param name="Weather">The weather allowed.</param>
/// <param name="Cooldown">The number of days that need to pass since a screenshot has been taken on this map.</param>
internal readonly record struct ProcessedTrigger(PackedDay Day, TimeRange[] Times, Weather Weather, uint Cooldown, string? Condition)
{
    /// <summary>
    /// Checks to see if this processed trigger is valid.
    /// </summary>
    /// <param name="current">The current location.</param>
    /// <param name="farmer">The farmer to check for.</param>
    /// <param name="currentDay">The (packed) current day.</param>
    /// <param name="timeOfDay">The current time of day.</param>
    /// <param name="daysSinceLastTrigger">The number of days since a screenshot was last triggered on this map.</param>
    /// <returns>Whether or not this is valid.</returns>
    internal bool Check(GameLocation current, Farmer farmer, PackedDay currentDay, int timeOfDay, uint daysSinceLastTrigger)
    {
        if (daysSinceLastTrigger < this.Cooldown)
        {
            return false;
        }

        // check weather.
        switch (this.Weather)
        {
            case Weather.Sunny when current.IsRainingHere():
                return false;
            case Weather.Rainy when !current.IsRainingHere():
                return false;
        }

        // check to see if my day is valid. The first four bits are my season, other twenty eight are the days.
        if (!this.Day.Check(currentDay))
        {
            return false;
        }

        // check game state query, if given
        if (!GameStateQuery.CheckConditions(this.Condition, current, farmer, random: Random.Shared))
        {
            return false;
        }

        // check to see if I'm in a valid time range
        foreach (TimeRange range in this.Times)
        {
            if (range.StartTime <= timeOfDay && range.EndTime >= timeOfDay)
            {
                return true;
            }
        }
        return false;
    }
}
