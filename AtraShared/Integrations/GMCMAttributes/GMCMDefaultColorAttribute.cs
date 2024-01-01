namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Attribute to set the default color for a GMCM element.
/// </summary>
/// <param name="r">red component.</param>
/// <param name="g">green component.</param>
/// <param name="b">blue component.</param>
/// <param name="a">alpha.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMDefaultColorAttribute(byte r, byte g, byte b, byte a) : Attribute
{
    /// <summary>
    /// Gets the red component.
    /// </summary>
    internal byte R { get; init; } = r;

    /// <summary>
    /// Gets the green component.
    /// </summary>
    internal byte G { get; init; } = g;

    /// <summary>
    /// Gets the blue component.
    /// </summary>
    internal byte B { get; init; } = b;

    /// <summary>
    /// Gets the alpha.
    /// </summary>
    internal byte A { get; init; } = a;
}
