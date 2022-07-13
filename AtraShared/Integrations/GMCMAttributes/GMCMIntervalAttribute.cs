namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Sets the interval for GMCM.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMIntervalAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GMCMIntervalAttribute"/> class.
    /// </summary>
    /// <param name="interval">The interval requested.</param>
    public GMCMIntervalAttribute(double interval) => this.interval = interval;

    internal double interval { get; init; }
}
