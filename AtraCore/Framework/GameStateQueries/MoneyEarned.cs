namespace AtraCore.Framework.GameStateQueries;

using StardewValley.Delegates;

using static StardewValley.GameStateQuery;

/// <summary>
/// Handles adding a GSQ that checks for money earned.
/// </summary>
internal static class MoneyEarned
{
    /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate"/>
    /// <remarks>Checks if the given player has the specified amount of money earned, inclusive.</remarks>
    internal static bool CheckMoneyEarned(string[] query, GameStateQueryContext context)
    {
        uint max = uint.MaxValue;
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGet(query, 2, out string? minS, out error) || !TryParseUInt(minS, out uint min, out error)
            || !ArgUtility.TryGetOptional(query, 3, out string? maxS, out error, null)
            || (maxS is not null && !TryParseUInt(maxS, out max, out error)))
        {
            return Helpers.ErrorResult(query, error);
        }

        return Helpers.WithPlayer(context.Player, playerKey, (Farmer target) => target.totalMoneyEarned >= min && target.totalMoneyEarned <= max);
    }

    private static bool TryParseUInt(string str, out uint value, out string error)
    {
        if (uint.TryParse(str, out value))
        {
            error = string.Empty;
            return true;
        }

        value = 0;
        error = $"value '{str}', which can't be parsed as uint";
        return false;
    }
}