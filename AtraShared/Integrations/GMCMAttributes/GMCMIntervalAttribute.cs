namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Sets the interval for GMCM.
/// </summary>
/// <param name="interval">The interval requested.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMIntervalAttribute(double interval) : Attribute
{
    /// <summary>
    /// Gets the interval to use for a GMCM menu.
    /// </summary>
    internal double Interval { get; init; } = interval;
}
