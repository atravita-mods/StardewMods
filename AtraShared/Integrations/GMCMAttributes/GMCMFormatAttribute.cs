namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Sets the format string for a numeric GMCM option.
/// </summary>
/// <param name="formatString">format string.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GMCMFormatAttribute(string formatString) : Attribute
{
    /// <summary>
    /// Gets the c-style format string.
    /// </summary>
    internal string FormatString { get; init; } = formatString;
}
