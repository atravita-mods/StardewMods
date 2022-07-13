namespace AtraShared.Integrations.GMCMAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMSectionAttribute : Attribute
{
    internal int Order { get; init; }
    internal string Name { get; init; }

    public GMCMSectionAttribute(string name, int order)
    {
        this.Name = name;
        this.Order = order;
    }
}
