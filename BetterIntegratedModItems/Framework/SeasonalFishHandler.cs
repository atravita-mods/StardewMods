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

    private static void Mod_OnLocationSeen(object? sender, LocationSeenEventArgs e) => Manager = null;
}
