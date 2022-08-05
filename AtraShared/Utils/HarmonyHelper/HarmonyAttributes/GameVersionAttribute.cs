namespace AtraShared.Utils.HarmonyHelper.HarmonyAttributes;

/// <summary>
/// Indicates the the following patch should only be applied to a specific game version.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GameVersionAttribute : Attribute
{
    public GameVersionAttribute(string? minVersion)
        => this.MinVersion = minVersion;

    public GameVersionAttribute(string? minVersion, string? maxVersion)
    {
        this.MinVersion = minVersion;
        this.MaxVersion = maxVersion;
    }

    internal string? MinVersion { get; init; }

    internal string? MaxVersion { get; init; }
}
