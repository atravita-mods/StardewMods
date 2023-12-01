namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// Represents a possible trigger for a screenshot. This is the userland data model.
/// </summary>
public sealed class UserTrigger
{
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
}