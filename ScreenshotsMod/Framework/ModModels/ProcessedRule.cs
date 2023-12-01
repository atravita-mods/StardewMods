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
    internal string Name => name;

    /// <summary>
    /// Gets the scale this screenshot should be at.
    /// </summary>
    internal float Scale => scale;

    /// <summary>
    /// Gets the tokenized path this screenshot should be saved at.
    /// </summary>
    internal string Path => path;

    /// <summary>
    /// Gets or sets a value indicating whether or not this rule should wait for an event to be over.
    /// </summary>
    internal bool DuringEvents => duringEvents;

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
}
