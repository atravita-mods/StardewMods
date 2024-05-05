namespace AtraCore.Config;

/// <summary>
/// The config model for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to show regeneration numbers.
    /// </summary>
    public bool ShowRegenNumbers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to auto color fish ponds.
    /// </summary>
    public bool AutoColorFishPonds { get; set; } = false;
}
