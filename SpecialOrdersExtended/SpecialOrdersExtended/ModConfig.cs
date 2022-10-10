using AtraShared.Integrations.GMCMAttributes;

namespace SpecialOrdersExtended;

/// <summary>
/// Config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether to be verbose or not.
    /// </summary>
    /// <remarks>Use this setting for anything that would be useful for other mod authors.</remarks>
    [GMCMDefaultIgnore]
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to surpress board updates before
    /// the board is opened.
    /// </summary>
    public bool SurpressUnnecessaryBoardUpdates { get; set; } = true;
}