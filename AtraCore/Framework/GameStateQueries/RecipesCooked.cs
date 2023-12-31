namespace AtraCore.Framework.GameStateQueries;

using StardewValley.Delegates;

using static StardewValley.GameStateQuery;

/// <summary>
/// Handles adding a GSQ that checks for recipes cooked.
/// </summary>
internal static class RecipesCooked
{
    /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate"/>
    /// <remarks>Checks if the given player has the specified percentage of recipes cooked, inclusive.</remarks>
    internal static bool RecipesCookedPercent(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGetFloat(query, 2, out float min, out error)
            || !ArgUtility.TryGetOptionalFloat(query, 3, out float max, out error, float.MaxValue))
        {
            return Helpers.ErrorResult(query, error);
        }

        return Helpers.WithPlayer(
            context.Player,
            playerKey,
            (Farmer target) =>
                {
                    float recipesCooked = Utility.getCookedRecipesPercent(target);
                    return recipesCooked >= min && recipesCooked <= max;
                });
    }
}