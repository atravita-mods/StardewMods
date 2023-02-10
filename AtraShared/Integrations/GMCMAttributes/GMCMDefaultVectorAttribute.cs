namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Attribute to set the default vector for a GMCM element.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GMCMDefaultVectorAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GMCMDefaultVectorAttribute"/> class.
    /// </summary>
    /// <param name="x">x coordinate.</param>
    /// <param name="y">y coordinate.</param>
    public GMCMDefaultVectorAttribute(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }

    /// <summary>
    /// Gets the default X coordinate.
    /// </summary>
    internal float X { get; init; }

    /// <summary>
    /// Gets the default Y coordinate.
    /// </summary>
    internal float Y { get; init; }
}
