using NetEscapades.EnumGenerators;

using StardewModdingAPI.Utilities;

using StardewValley.Delegates;

using static StardewValley.GameStateQuery;

namespace AtraCore.Framework.GameStateQueries;
internal static class NPCQueries
{
    /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate"/>
    /// <remarks>Checks if the given player has talked to a specific NPC.</remarks>
    internal static bool WasTalkedTo(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGet(query, 2, out string? npcKey, out error))
        {
            return Helpers.ErrorResult(query, error);
        }

        return Helpers.WithPlayer(
            context.Player,
            playerKey,
            (Farmer target) => target.friendshipData.TryGetValue(npcKey, out Friendship? friendship) && friendship.TalkedToToday);
    }

    internal static bool WasLastGifted(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGet(query, 2, out string? npcKey, out error)
            || !ArgUtility.TryGetInt(query, 3, out int min, out error)
            || !ArgUtility.TryGetOptionalInt(query, 4, out int max, out error, defaultValue: int.MaxValue))
        {
            return Helpers.ErrorResult(query, error);
        }

        int date = SDate.Now().DaysSinceStart;

        if (min <= 0)
        {
            min = date + min;
        }

        if (max <= 0)
        {
            max = date + max;
        }

        if (min > max)
        {
            ModEntry.ModMonitor.VerboseLog($"Query: {string.Join(' ', query)} corresponds to a min that's bigger than the max: {min}-{max}");
            return false;
        }

        return Helpers.WithPlayer(
            context.Player,
            playerKey,
            (Farmer target) =>
            {
                if (target.friendshipData.TryGetValue(npcKey, out Friendship? friendship))
                {
                    int days = friendship.LastGiftDate.TotalDays;
                    return days >= min && days <= max;
                }
                return false;
            });
    }

    internal static bool DaysSinceMarriage(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGet(query, 2, out string? npcKey, out error)
            || !ArgUtility.TryGetInt(query, 3, out int min, out error)
            || !ArgUtility.TryGetOptionalInt(query, 4, out int max, out error, defaultValue: int.MaxValue))
        {
            return Helpers.ErrorResult(query, error);
        }

        return Helpers.WithPlayer(
            context.Player,
            playerKey,
            (Farmer target) => target.friendshipData.TryGetValue(npcKey, out Friendship? friendship)
                && friendship.WeddingDate is not null && friendship.WeddingDate.TotalDays <= Game1.Date.TotalDays
                && friendship.DaysMarried >= min && friendship.DaysMarried <= max);
    }

    internal static bool IsAnniversaryOfMarriage(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGet(query, 2, out string? npcKey, out error)
            || !ArgUtility.TryGet(query, 3, out string? modulusKey, out error)
            || !ArgUtility.TryGetOptionalInt(query, 4, out int offset, out error))
        {
            return Helpers.ErrorResult(query, error);
        }

        if (!TimeFrameExtensions.TryParse(modulusKey, out TimeFrame timeFrame, ignoreCase: true))
        {
            return Helpers.ErrorResult(query, $"could not parse '{modulusKey}' as a valid time frame");
        }

        return Helpers.WithPlayer(
            context.Player,
            playerKey,
            (Farmer target) => target.friendshipData.TryGetValue(npcKey, out Friendship? friendship)
                && friendship.DaysMarried != 0 && (friendship.DaysMarried % (int)timeFrame == offset));
    }
}

[EnumExtensions]
public enum TimeFrame
{
    Day = 1,
    Week = 7,
    Month = 28,
    Year = 28 * 4,
}