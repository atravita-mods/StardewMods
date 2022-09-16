namespace SpecialOrdersExtended.QuestModels;

/// <summary>
/// An interface that marks custom quest objectives.
/// </summary>
public interface ICustomQuestObjective
{
    /// <summary>
    /// The order this belongs to.
    /// </summary>
    public string Order { get; set; }

    /// <summary>
    /// The position of the order in the list.
    /// This is used to disambiguate multiple of the same order.
    /// </summary>
    public int Position { get; set; }
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

    /// <summary>
    /// The position of the order in the list.
    /// This is used to disambiguate multiple of the same order.
    /// </summary>
    public int Position { get; set; }
}
