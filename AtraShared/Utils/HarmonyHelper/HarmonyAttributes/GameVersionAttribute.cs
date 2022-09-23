namespace AtraShared.Utils.HarmonyHelper.HarmonyAttributes;

/// <summary>
/// Indicates the the following patch should only be applied to a specific game version.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GameVersionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersionAttribute"/> class.
    /// </summary>
    /// <param name="minVersion">Minimum version this patch should apply to.</param>
    public GameVersionAttribute(string? minVersion)
        => this.MinVersion = minVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersionAttribute"/> class.
    /// </summary>
    /// <param name="minVersion">Minimum version this patch should apply to.</param>
    /// <param name="maxVersion">Maximum version this patch should apply to.</param>
    public GameVersionAttribute(string? minVersion, string? maxVersion)
    {
        this.MinVersion = minVersion;
        this.MaxVersion = maxVersion;
    }

    internal string? MinVersion { get; init; }

    internal string? MaxVersion { get; init; }
}
