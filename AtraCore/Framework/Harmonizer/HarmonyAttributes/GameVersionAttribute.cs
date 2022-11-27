namespace AtraCore.Framework.Harmonizer.HarmonyAttributes;

/// <summary>
/// Indicates the following patch should only be applied to a specific game version.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GameVersionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersionAttribute"/> class.
    /// </summary>
    /// <param name="minVersion">Minimum version this patch should apply to, inclusive..</param>
    public GameVersionAttribute(string? minVersion)
        => this.MinVersion = minVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersionAttribute"/> class.
    /// </summary>
    /// <param name="minVersion">Minimum version this patch should apply to, inclusive.</param>
    /// <param name="maxVersion">Maximum version this patch should apply to, inclusive.</param>
    public GameVersionAttribute(string? minVersion, string? maxVersion)
    {
        this.MinVersion = minVersion;
        this.MaxVersion = maxVersion;
    }

    /// <summary>
    /// Gets the minimum game version this patch should apply to, inclusive.
    /// </summary>
    internal string? MinVersion { get; init; }

    /// <summary>
    /// Gets the maximum game version this patch should apply to, inclusive.
    /// </summary>
    internal string? MaxVersion { get; init; }
}
