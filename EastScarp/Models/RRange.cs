namespace EastScarp.Models;

/// <summary>
/// Represents a range, inclusive.
/// </summary>
public struct RRange
{
    public RRange()
    {
    }

    public RRange(int min, int max)
    {
        this.Min = min;
        this.Max = max;
    }

    /// <summary>
    /// The minimum value.
    /// </summary>
    int Min { get; set; } = 1;

    /// <summary>
    /// The maximum value.
    /// </summary>
    int Max { get; set; } = 1;

    internal int Get() => Random.Shared.Next(this.Min, Math.Max(this.Min, this.Max) + 1);
}