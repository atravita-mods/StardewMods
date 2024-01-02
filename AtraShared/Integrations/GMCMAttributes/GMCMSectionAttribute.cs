namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Assigns a property to a GMCM section.
/// </summary>
/// <param name="name">The (internal) name of the section.</param>
/// <param name="order">The ordering the section should be in.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMSectionAttribute(string name, int order) : Attribute
{
    /// <summary>
    /// Gets the ordering.
    /// </summary>
    internal int Order { get; init; } = order;

    /// <summary>
    /// Gets the internal name for the section.
    /// </summary>
    internal string Name { get; init; } = name;
}
