using System.Collections.Concurrent;

using StardewModdingAPI.Utilities;

namespace NPCArrows.Framework.NPCs;

/// <summary>
/// Gets the data to draw for a specific NPC.
/// </summary>
internal sealed class NPCData
{
    public NPCData(SObject? bestGift, int? bestGiftTaste, bool talkedTo)
    {
        this.BestGift = bestGift;
        this.BestGiftTaste = bestGiftTaste;
        this.TalkedTo = talkedTo;
    }

    /// <summary>
    /// Gets a reference to the best gift to gift to this particular NPC, or null if irrelevant/invalid.
    /// </summary>
    internal SObject? BestGift { get; private set; }

    /// <summary>
    /// Gets the gift taste for this gift.
    /// </summary>
    internal int? BestGiftTaste { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this NPC has been talked to.
    /// </summary>
    internal bool TalkedTo { get; private set; }
}

internal static class NPCDataManager
{
    private static readonly PerScreen<Dictionary<string, NPCData>> npcData = new(() => new());

    private static readonly PerScreen<Dictionary<SObject, NPCData>> giftCache = new(() => new());
}