namespace ExpFromMonsterKillsOnFarm;

/// <summary>
/// The configuration class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether to grant full monster kill xp on the farm.
    /// </summary>
    public bool GainExp { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether all billboard quests will be updated by farm kills.
    /// </summary>
    public bool QuestCompletion { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether all special order objectives will be updated by farm kills.
    /// </summary>
    public bool SpecialOrderCompletion { get; set; } = true;
}