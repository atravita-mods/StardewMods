using HarmonyLib;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

#warning - check for multiplayer compat here.

/// <summary>
/// Applies patches against the traveling merchant.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class TravelingMerchantPatcher
{
    [HarmonyPatch("generateLocalTravelingMerchantStock")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void Postfix(Dictionary<ISalable, int[]> __result, int seed)
    {
        Random random = new(seed);
        if (ModEntry.SecretJojaFertilizerID != -1 && Game1.player.DailyLuck > 0.5 && random.NextDouble() < 0.01)
        {
            __result.Add(new SObject(ModEntry.SecretJojaFertilizerID, 1), new[] { (int)(2000 * Game1.player.difficultyModifier), 5 });
        }

        if (ModEntry.BountifulBushID != -1 && Game1.currentSeason is "spring" or "fall")
        {
            __result.Add(new SObject(ModEntry.BountifulBushID, 1), new[] { 200, 5 });
        }
        else if (ModEntry.WisdomFertilizerID != -1)
        {
            __result.Add(new SObject(ModEntry.WisdomFertilizerID, 1), new[] { 100, 5 });
        }
    }
}
