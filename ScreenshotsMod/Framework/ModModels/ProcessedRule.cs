using Newtonsoft.Json;

namespace ScreenshotsMod.Framework.ModModels;

/// <summary>
/// A processed rule.
/// </summary>
/// <param name="name">The name of the rule.</param>
/// <param name="path">The path the rule corresponds to.</param>
/// <param name="scale">The scale to use.</param>
/// <param name="duringEvents">Whether or not this rule should wait for events to be over.</param>
/// <param name="triggers">A list of processed triggers.</param>
internal sealed class ProcessedRule(string name, string path, float scale, bool duringEvents, ProcessedTrigger[] triggers)
{
    /// <summary>
    /// Gets the name of the rule.
    /// </summary>
    [JsonProperty]
    internal string Name => name;

    /// <summary>
    /// Gets the scale this screenshot should be at.
    /// </summary>
    [JsonProperty]
    internal float Scale => scale;

    /// <summary>
    /// Gets the tokenized path this screenshot should be saved at.
    /// </summary>
    [JsonProperty]
    internal string Path => path;

    /// <summary>
    /// Gets a value indicating whether or not this rule should wait for an event to be over.
    /// </summary>
    [JsonProperty]
    internal bool DuringEvents => duringEvents;

    // this odd construct exists for a simple reason - it lets newtonsoft dump it.
    [JsonProperty]
    private ProcessedTrigger[] Triggers => triggers;

    /// <summary>
    /// Checks to see if a screenshot can be triggered for the current map.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <param name="farmer">The farmer to check for.</param>
    /// <param name="currentDay">A <see cref="PackedDay"/> that represents the current day.</param>
    /// <param name="timeOfDay">The current time of day.</param>
    /// <param name="daysSinceTriggered">The number of days since a screenshot was triggered on this map.</param>
    /// <returns>True if it can be triggered, false otherwise.</returns>
    internal bool CanTrigger(GameLocation location, Farmer farmer, PackedDay currentDay, int timeOfDay, uint daysSinceTriggered)
    {
        if (daysSinceTriggered == 0u)
        {
            return false;
        }

        foreach (ProcessedTrigger trigger in triggers)
        {
            if (trigger.Check(location, farmer, currentDay, timeOfDay, daysSinceTriggered))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets a copy of this processed rule.
    /// </summary>
    /// <returns>A copy of the rule.</returns>
    internal ProcessedRule Clone() => new(name, path, scale, duringEvents, triggers);
}
