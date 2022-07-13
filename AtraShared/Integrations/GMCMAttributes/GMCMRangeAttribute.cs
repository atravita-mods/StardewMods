namespace AtraShared.Integrations.GMCMAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMRangeAttribute : Attribute
{
    internal double min { get; init; }

    internal double max { get; init; }

    public GMCMRangeAttribute(double min, double max)
    {
        this.min = min;
        this.max = max;
    }
}