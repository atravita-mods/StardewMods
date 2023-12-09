namespace EastScarp.Models;

/// <summary>
/// A color to apply to the water.
/// </summary>
public sealed class WaterColor : BaseEntry
{
    /// <summary>
    /// Gets or sets the color to set the water.
    /// </summary>
    public string Color { get; set; } = string.Empty;
}
