namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Defines the range to use with GMCM.
/// </summary>
/// <param name="min">minimum value.</param>
/// <param name="max">maximum value.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMRangeAttribute(double min, double max) : Attribute
{
    /// <summary>
    /// Gets the min value.
    /// </summary>
    internal double Min { get; init; } = min;

    /// <summary>
    /// Gets the max value.
    /// </summary>
    internal double Max { get; init; } = max;
}