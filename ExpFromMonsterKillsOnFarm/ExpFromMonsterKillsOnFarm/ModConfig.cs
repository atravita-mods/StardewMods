namespace ExpFromMonsterKillsOnFarm;

/// <summary>
/// The configuration class for this mod.
/// </summary>
internal class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether to grant monster kill xp on the farm.
    /// </summary>
    public bool GainExp { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether billboard quests will be updated by farm kills.
    /// </summary>
    public bool QuestCompletion { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether special order objects will be updated by farm kills.
    /// </summary>
    public bool SpecialOrderCompletion { get; set; } = true;
}