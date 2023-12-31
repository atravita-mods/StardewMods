namespace AtraCore.Framework.GameStateQueries;

using StardewValley.Delegates;

using static StardewValley.GameStateQuery;

/// <summary>
/// Handles adding a GSQ that checks for current daily luck.
/// </summary>
internal static class CurrentDailyLuck
{
    /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate"/>
    /// <remarks>Checks if the given player the specified daily luck, inclusive.</remarks>
    internal static bool DailyLuck(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGetFloat(query, 2, out float min, out error)
            || !ArgUtility.TryGetOptionalFloat(query, 3, out float max, out error, float.MaxValue))
        {
            return Helpers.ErrorResult(query, error);
        }

        return Helpers.WithPlayer(context.Player, playerKey, (Farmer target) => target.DailyLuck >= min && target.DailyLuck <= max);
    }
}