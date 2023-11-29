namespace AtraCore.Framework.Models;

/// <summary>
/// Data model to allow overriding category names.
/// </summary>
public sealed class CategoryExtension
{
    /// <summary>
    /// Gets or sets tokenized string for the category name, or null to not set.
    /// </summary>
    public string? CategoryNameOverride { get; set; }

    /// <summary>
    /// Gets or sets string that represents the color. See <see cref="Utility.StringToColor(string)"/>.
    /// </summary>
    public string? CategoryColorOverride { get; set; }
}
