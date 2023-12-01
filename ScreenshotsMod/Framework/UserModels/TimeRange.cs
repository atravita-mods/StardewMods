namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// Represents an range in time (inclusive.)
/// </summary>
public sealed class TimeRange : IComparable<TimeRange>
{
    private int startTime = 600;
    private int endTime = 2600;

    public TimeRange() { }

    public TimeRange(int startTime, int endTime)
    {
        this.StartTime = startTime;
        this.EndTime = endTime;
    }

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

    /// <inheritdoc/>
    public int CompareTo(TimeRange? other) => this.StartTime - (other?.StartTime ?? 0);
}
