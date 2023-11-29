namespace ScreenshotsMod.Framework.ModModels;

/// <summary>
/// A struct that represents a packed format of a day/season constraint.
/// </summary>
internal readonly record struct PackedDay
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackedDay"/> class.
    /// Gets the packed day representation of the current day.
    /// </summary>
    public PackedDay()
        => this.value = (1u << (Math.Clamp(Game1.seasonIndex, 0, 3) + 28)) | (1u << ((Game1.dayOfMonth % 28) - 1));

    /// <summary>
    /// Initializes a new instance of the <see cref="PackedDay"/> class.
    /// Gets the packed day representation of the given value.
    /// </summary>
    /// <param name="value">the internal value representation.</param>
    public PackedDay(uint value)
        => this.value = value;

    private readonly uint value;

    /// <summary>
    /// Checks to see if the current day is allowed by this value.
    /// </summary>
    /// <param name="current">The current day.</param>
    /// <returns>True if allowed, false otherwise.</returns>
    internal bool Check(PackedDay current)
        => (current.value & this.value) == current.value;
}
