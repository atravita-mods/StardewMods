using AtraBase.Models.WeightedRandom;

using AtraShared.ConstantsAndEnums;

using BetterIntegratedModItems.DataModels;

namespace BetterIntegratedModItems.Framework;
internal static class SeasonalFishHandler
{
    private static WeightedManager<int>? manager = new();
    private static StardewSeasons lastLoadedSeason = StardewSeasons.None;

    internal static void Initialize(ModEntry mod)
    {
        mod.OnLocationSeen += Reset;
    }

    private static bool Load()
    {
        if (!StardewSeasonsExtensions.TryParse(Game1.currentSeason, value: out StardewSeasons currentSeason, ignoreCase: true))
        {
            ModEntry.ModMonitor.Log($"Failed to parse {Game1.currentSeason} as a season, skipping.", LogLevel.Warn);
            return false;
        }

        if (currentSeason == lastLoadedSeason)
        {
            return false;
        }
        lastLoadedSeason = currentSeason;

        return true;
    }

    private static void Reset(object? sender, LocationSeenEventArgs e) => manager = null;
}
