namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Sets the format string for a numeric GMCM option.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GMCMFormatAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GMCMFormatAttribute"/> class.
    /// </summary>
    /// <param name="formatString">format string.</param>
    public GMCMFormatAttribute(string formatString)
        => this.FormatString = formatString;

    /// <summary>
    /// Gets the c-style format string.
    /// </summary>
    internal string FormatString { get; init; }
}
