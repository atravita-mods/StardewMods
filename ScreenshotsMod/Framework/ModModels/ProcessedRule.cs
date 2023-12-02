using Newtonsoft.Json;

namespace ScreenshotsMod.Framework.ModModels;

/// <summary>
/// A processed rule.
/// </summary>
/// <param name="name">The name of the rule.</param>
/// <param name="path">The path the rule corresponds to.</param>
/// <param name="scale">The scale to use.</param>
/// <param name="triggers">A list of processed triggers.</param>
internal sealed class ProcessedRule(string name, string path, float scale, bool duringEvents, ProcessedTrigger[] triggers)
{
    private bool triggered = false;

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
    /// Gets or sets a value indicating whether or not this rule should wait for an event to be over.
    /// </summary>
    [JsonProperty]
    internal bool DuringEvents => duringEvents;

    // this odd construct exists for a simple reason - it lets newtonsoft dump it.
    [JsonProperty]
    private ProcessedTrigger[] Triggers => triggers;

    internal void Reset() => this.triggered = false;

    internal bool Trigger(GameLocation current, PackedDay currentDay, int timeOfDay)
    {
        if (this.triggered)
        {
            return false;
        }
        foreach (ProcessedTrigger trigger in triggers)
        {
            if (trigger.Check(current, currentDay, timeOfDay))
            {
                this.triggered = true;
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
