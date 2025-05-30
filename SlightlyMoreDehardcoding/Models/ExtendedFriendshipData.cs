using Newtonsoft.Json;

namespace SlightlyMoreDehardcoding.Models;

/// <summary>
/// A data model for additional npc data.
/// </summary>
public sealed class ExtendedFriendshipData
{
    public HeartsOverride? HeartsOverride { get; set; } = null;
}

/// <summary>
/// override max hearts.
/// </summary>
public sealed class HeartsOverride
{
    /// <summary>
    /// Maximum hearts for nondatables.
    /// </summary>
    public int NonDatable { get; set; } = -1;

    /// <summary>
    /// Maximum hearts for datables you're not dating.
    /// </summary>
    public int Datable { get; set; } = -1;

    /// <summary>
    /// Maximum hearts for datables you're dating.
    /// </summary>
    public int Dating { get; set; } = -1;

    /// <summary>
    /// Maximum hearts with spouse.
    /// </summary>
    public int Married { get; set; } = -1;

    /// <summary>
    /// Maximum hearts with someone you're divorced to. You monster.
    /// </summary>
    public int Divorced { get; set; } = -1;
}