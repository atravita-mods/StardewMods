using AtraShared.Integrations.GMCMAttributes;

namespace SleepInWedding;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets when the wedding should begin.
    /// </summary>
    [GMCMInterval(10)]
    [GMCMRange(600, 2600)]
    public int WeddingTime { get; set; } = 800;

    /// <summary>
    /// Gets or sets a value indicating whether or not we should try to
    /// set the wedding on save load.
    /// </summary>
    public bool TryRecoverWedding { get; set; } = true;
}
