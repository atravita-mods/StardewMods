namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// Represents a possible trigger for a screenshot. This is the userland data model.
/// </summary>
public sealed class UserTrigger
{
    /// <summary>
    /// Gets or sets the internal names for maps for which this trigger should apply.
    /// </summary>
    public string[] Maps { get; set; } = ["Farm"];

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
    /// Gets or sets the start time.
    /// </summary>
    public int StartTime
    {
        get => this.startTime;
        set => this.startTime = Math.Clamp(value - (value % 10), 600, 2600);
    }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public int EndTime
    {
        get => this.endTime;
        set => this.endTime = Math.Clamp(value - (value % 10), 600, 2600);
    }
}

/// <summary>
/// Represents weather conditions that matter for screenshots.
/// </summary>
[Flags]
public enum Weather
{
    /// <summary>
    /// The weather should be considered a rainy weather.
    /// </summary>
    Rainy = 0b1,

    /// <summary>
    /// The weather should be considered a sunny weather.
    /// </summary>
    Sunny = 0b10,

    /// <summary>
    /// Either sunny or rainy weathers.
    /// </summary>
    Any = Rainy | Sunny,
}
