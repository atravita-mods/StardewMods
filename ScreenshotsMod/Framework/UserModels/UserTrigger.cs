using AtraShared.ConstantsAndEnums;

namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// Represents a possible trigger for a screenshot. This is the userland data model.
/// </summary>
public sealed class UserTrigger
{
    /// <summary>
    /// Gets or sets the maps for which this trigger should apply.
    /// </summary>
    public string[] Maps { get; set; } = Array.Empty<string>();

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
    public TimeRange Time { get; set; } = new();

    /// <summary>
    /// Gets or sets the weather conditions for which this trigger should apply.
    /// </summary>
    public Weather Weather { get; set; } = Weather.Any;
}

/// <summary>
/// Represents an range in time (inclusive.)
/// </summary>
public sealed class TimeRange
{
    private int startTime = 600;
    private int endTime = 2600;

    /// <summary>
    /// The start time.
    /// </summary>
    public int StartTime
    {
        get => this.startTime;
        set => this.startTime = Math.Clamp(value - value % 10, 600, 2600);
    }

    /// <summary>
    /// The end time.
    /// </summary>
    public int EndTime
    {
        get => this.endTime;
        set => this.endTime = Math.Clamp(value - value % 10, 600, 2600);
    }
}

/// <summary>
/// Represents weather conditions that matter for screenshots.
/// </summary>
public enum Weather
{
    Any,
    Rainy,
    Sunny,
}
