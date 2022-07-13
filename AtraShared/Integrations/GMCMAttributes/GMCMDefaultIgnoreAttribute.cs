namespace AtraShared.Integrations.GMCMAttributes;

/// <summary>
/// Causes the default GMCM generator to ignore this field or property.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GMCMDefaultIgnoreAttribute : Attribute
{
}
