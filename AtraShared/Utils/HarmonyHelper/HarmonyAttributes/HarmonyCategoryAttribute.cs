namespace AtraShared.Utils.HarmonyHelper.HarmonyAttributes;

/// <summary>
/// Indicates the following patch should be categorized separately.
/// This is primarily used for unpatching.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class HarmonyCategoryAttribute: Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HarmonyCategoryAttribute"/> class.
    /// </summary>
    /// <param name="category">A string representing the category.</param>
    public HarmonyCategoryAttribute(string category)
        => this.Category = category;

    /// <summary>
    /// Gets the category this patch should be placed in.
    /// </summary>
    internal string Category { get; init; }
}