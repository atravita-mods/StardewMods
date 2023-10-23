namespace AtraShared.Utils.Extensions;

/// <summary>
/// Additional methods for Buffs.
/// </summary>
public static class BuffExtensions
{
    /// <summary>
    /// Converts the minutesDuration of a Stardew buff to the actual time.
    /// </summary>
    /// <param name="minutesDuration">The minutesDuration from the data.</param>
    /// <returns>A timespan corresponding to the actual time.</returns>
    public static TimeSpan ActualTime(int minutesDuration)
        => TimeSpan.FromMilliseconds((minutesDuration / 10) * 7_000);
}