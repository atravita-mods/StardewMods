// Ignore Spelling: Cooldown

using ScreenshotsMod.Framework.ModModels;

namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// Represents a possible trigger for a screenshot. This is the userland data model.
/// </summary>
public sealed class UserTrigger
{
    private uint cooldown = 1;

    /// <summary>
    /// Gets or sets a value indicating how many days should pass before this rule applies again.
    /// </summary>
    public uint Cooldown
    {
        get => this.cooldown;
        set => this.cooldown = Math.Max(value, 1);
    }

    /// <summary>
    /// Gets or sets the season for which this trigger should apply.
    /// </summary>
    public string[] Seasons { get; set; } = ["Any"];

    /// <summary>
    /// Gets or sets the days for which this trigger should apply.
    /// </summary>
    public string[] Days { get; set; } = ["Any"];

    /// <summary>
    /// Gets or sets the times for which this trigger should apply.
    /// </summary>
    public TimeRange[] Time { get; set; } = [new()];

    /// <summary>
    /// Gets or sets the weather conditions for which this trigger should apply.
    /// </summary>
    public Weather Weather { get; set; } = Weather.Any;

    /// <summary>
    /// Gets or sets a <see cref="GameStateQuery"/> that controls this trigger.
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Processes a trigger.
    /// </summary>
    /// <param name="rule">The rule associated with this trigger.</param>
    /// <returns>A processed trigger.</returns>
    internal ProcessedTrigger? Process(string rule)
    {
        if (this.Time.Length == 0)
        {
            ModEntry.ModMonitor.Log($"Trigger for rule {rule} has no valid times, skipping.", LogLevel.Warn);
            return null;
        }

        TimeRange[] times = TimeRange.FoldTimes(this.Time);

        PackedDay? packed = PackedDay.Parse(this.Seasons, this.Days, out string? error);
        if (packed is null)
        {
            ModEntry.ModMonitor.Log($"Trigger for rule {rule} has invalid times: {error}, skipping", LogLevel.Warn);
            return null;
        }

        return new(packed.Value, times, this.Weather, this.Cooldown, this.Condition);
    }
}