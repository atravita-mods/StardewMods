using AtraBase.Models.WeightedRandom;

using AtraShared.ConstantsAndEnums;

using BetterIntegratedModItems.DataModels;

namespace BetterIntegratedModItems.Framework;
internal static class SeasonalFishHandler
{
    private static WeightedManager<int>? Manager = new();
    private static StardewSeasons LastLoadedSeason = StardewSeasons.None;

    internal static void Initialize(ModEntry mod)
    {
        mod.OnLocationSeen += Mod_OnLocationSeen;
    }

    private static bool Load()
    {
        if (!StardewSeasonsExtensions.TryParse(Game1.currentSeason, value: out StardewSeasons currentSeason, ignoreCase: true))
        {
            ModEntry.ModMonitor.Log($"Failed to parse {Game1.currentSeason} as a season, skipping.", LogLevel.Warn);
            return false;
        }

        return true;
    }

    private static void Mod_OnLocationSeen(object? sender, LocationSeenEventArgs e) => Manager = null;
}
