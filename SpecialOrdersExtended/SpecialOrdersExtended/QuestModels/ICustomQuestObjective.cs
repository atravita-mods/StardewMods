namespace SpecialOrdersExtended.QuestModels;

/// <summary>
/// An interface that marks custom quest objectives.
/// </summary>
public interface ICustomQuestObjective
{
    /// <summary>
    /// The other this belongs to.
    /// </summary>
    public string Order { get; set; }
}

/// <summary>
/// An interface that marks custom quest rewards.
/// </summary>
public interface ICustomQuestReward
{
    /// <summary>
    /// The order this belongs to.
    /// </summary>
    public string Order { get; set; }
}
