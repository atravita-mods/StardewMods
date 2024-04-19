using AtraShared.ConstantsAndEnums;

using StardewValley.Delegates;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FruitTrees;

using static StardewValley.GameStateQuery;

namespace AtraCore.Framework.GameStateQueries;

/// <summary>
/// Queries for items.
/// </summary>
internal static class IsSeedGrowingIn
{
    /// <summary>
    /// A filter query, used to check if an item is a sapling that corresponds to a fruit tree that bears fruit in a particular season.
    /// </summary>
    /// <param name="query">query.</param>
    /// <param name="context">query context.</param>
    /// <returns>true or false.</returns>
    internal static bool SaplingQuery(string[] query, GameStateQueryContext context)
    {
        if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out Item? item, out string? error))
        {
            return Helpers.ErrorResult(query, error);
        }
        if (item is not SObject sapling || !sapling.IsFruitTreeSapling())
        {
            return false;
        }

        if (query.Length < 3)
        {
            return Helpers.ErrorResult(query, "Expected at least one season");
        }

        if (!TryGetQuerySeason(query.AsSpan(2), out StardewSeasons querySeason, out error))
        {
            return Helpers.ErrorResult(query, error);
        }

        if (Game1.fruitTreeData?.TryGetValue(item.ItemId, out FruitTreeData? cropData) is not true)
        {
            return false;
        }

        foreach (Season season in cropData.Seasons)
        {
            StardewSeasons sSeason = season.ConvertFromGameSeason();
            if ((sSeason & querySeason) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A filter query, used to check if an item is a seed that corresponds to a crop that grows in a particular season.
    /// </summary>
    /// <param name="query">query.</param>
    /// <param name="context">query context.</param>
    /// <returns>true or false.</returns>
    internal static bool SeedQuery(string[] query, GameStateQueryContext context)
    {
        if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out Item? item, out string? error))
        {
            return Helpers.ErrorResult(query, error);
        }
        if (item.Category != SObject.SeedsCategory)
        {
            return false;
        }

        if (query.Length < 3)
        {
            return Helpers.ErrorResult(query, "Expected at least one season");
        }

        if (!TryGetQuerySeason(query.AsSpan(2), out StardewSeasons querySeason, out error))
        {
            return Helpers.ErrorResult(query, error);
        }

        if (Game1.cropData?.TryGetValue(item.ItemId, out CropData? cropData) is not true)
        {
            return false;
        }

        foreach (Season season in cropData.Seasons)
        {
            StardewSeasons sSeason = season.ConvertFromGameSeason();
            if ((sSeason & querySeason) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetQuerySeason(Span<string> span, out StardewSeasons season, out string? error)
    {
        season = StardewSeasons.None;
        error = null;

        foreach (string season_str in span)
        {
            ReadOnlySpan<char> season_span = season_str.AsSpan().Trim();
            if (season_span.Equals("Current", StringComparison.OrdinalIgnoreCase))
            {
                season |= Game1.season.ConvertFromGameSeason();
            }
            else if (season_span.Equals("Next", StringComparison.OrdinalIgnoreCase))
            {
                season |= Game1.season.ConvertFromGameSeason().GetNextSeason();
            }
            else if (season_span.Equals("Previous", StringComparison.OrdinalIgnoreCase))
            {
                season |= Game1.season.ConvertFromGameSeason().GetPreviousSeason();
            }
            else if (StardewSeasonsExtensions.TryParse(season_span, out StardewSeasons proposed, ignoreCase: true))
            {
                season |= proposed;
            }
            else
            {
                error = $"could not parse {season_str} as valid season";
                return false;
            }
        }

        return true;
    }
}
