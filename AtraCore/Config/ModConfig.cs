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

    public bool AutoColorFishPonds { get; set; } = false;
}
