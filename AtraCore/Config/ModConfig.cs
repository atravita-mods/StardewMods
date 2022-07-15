using AtraShared.Integrations.GMCMAttributes;

namespace AtraCore.Config;

/// <summary>
/// The config model for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether more verbose printing should happen.
    /// </summary>
    [GMCMDefaultIgnore]
    public bool Verbose { get; set; } = false;
}
