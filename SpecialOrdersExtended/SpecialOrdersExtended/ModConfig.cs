namespace SpecialOrdersExtended;

/// <summary>
/// Config class for this mod.
/// </summary>
internal class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether to be verbose or not.
    /// </summary>
    /// <remarks>Use this setting for anything that would be useful for other mod authors.</remarks>
    internal bool Verbose { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to surpress board updates before
    /// the board is opened.
    /// </summary>
    internal bool SurpressUnnecessaryBoardUpdates { get; set; } = true;
}