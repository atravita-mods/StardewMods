using AtraShared.Integrations.GMCMAttributes;

namespace SpecialOrdersExtended;

/// <summary>
/// Config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to suppress board updates before
    /// the board is opened.
    /// </summary>
    public bool SuppressUnnecessaryBoardUpdates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not the tag cache should be used.
    /// </summary>
    public bool UseTagCache { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not Qi orders should avoid repeating until
    /// you've seen all of them.
    /// </summary>
    public bool AvoidRepeatingQiOrders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not the other quest will be made available
    /// once all quests of the type are finished.
    /// </summary>
    [GMCMDefaultIgnore]
    public bool AllowNewQuestWhenFinished { get; set; } = true;
}