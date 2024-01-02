namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Attribute to set the default vector for a GMCM element.
/// </summary>
/// <param name="x">x coordinate.</param>
/// <param name="y">y coordinate.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GMCMDefaultVectorAttribute(float x, float y) : Attribute
{
    /// <summary>
    /// Gets the default X coordinate.
    /// </summary>
    internal float X { get; init; } = x;

    /// <summary>
    /// Gets the default Y coordinate.
    /// </summary>
    internal float Y { get; init; } = y;
}
