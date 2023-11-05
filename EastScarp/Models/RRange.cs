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

    int Min { get; set; } = 1;
    int Max { get; set; } = 1;
}