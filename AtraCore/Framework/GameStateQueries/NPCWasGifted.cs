using AtraCore.Framework.Caches;

using StardewValley.Delegates;

using static StardewValley.GameStateQuery;

namespace AtraCore.Framework.GameStateQueries;
internal static class NPCWasGifted
{
    /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate"/>
    /// <remarks>Checks if the given player has caught the specified percentage of total fish, inclusive.</remarks>
    internal static bool Query(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGet(query, 2, out string? npcKey, out error))
        {
            return Helpers.ErrorResult(query, error);
        }

        if (query.Length < 4)
        {
            return Helpers.ErrorResult(query, "expected at least one item");
        }

        NPC? npc = null;
        npc = NPCCache.GetByVillagerName(npcKey);

        if (npc is null)
        {
            return Helpers.ErrorResult(query, $"could not find npc by name '{npcKey}'");
        }

        ArraySegment<string> gifts = new ArraySegment<string>(query, 3, query.Length - 3);

        return Helpers.WithPlayer(
            context.Player,
            playerKey,
            (Farmer target) => gifts.Any(g => target.hasItemBeenGifted(npc, g)));
    }
}
