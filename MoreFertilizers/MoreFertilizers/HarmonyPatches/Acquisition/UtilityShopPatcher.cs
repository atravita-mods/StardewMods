using HarmonyLib;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Holds patches against Utility for shops.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class UtilityShopPatcher
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.getShopStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    private static void PostfixGetShopStock(bool Pierres, ref List<Item> __result)
    {
        if (Pierres)
        {
            try
            {
                if (ModEntry.LuckyFertilizerID != -1
                    && !(Game1.year == 1 && Game1.currentSeason.Equals("spring", StringComparison.OrdinalIgnoreCase))
                    && Game1.player.team.AverageDailyLuck() > 0.07)
                {
                    __result.Add(new SObject(ModEntry.LuckyFertilizerID, 15, isRecipe: false, price: Game1.year == 1 ? 100 : 150));
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed in adding to Pierre's stock!{ex}", LogLevel.Error);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.getQiShopStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    private static void PostfixGetCasinoShop(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (ModEntry.LuckyFertilizerID != -1 && Game1.player.team.AverageDailyLuck() > 0.05)
            {
                __result.Add(new SObject(ModEntry.LuckyFertilizerID, 1), new[] { 300, int.MaxValue });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in adding to casino's stock!{ex}", LogLevel.Error);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.getJojaStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    private static void PostfixJojaStock(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (ModEntry.SecretJojaFertilizerID != -1 && Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja")
                && Game1.player.stats.IndividualMoneyEarned > 1_000_000 && Game1.random.NextDouble() < 0.15)
            {
                __result.Add(new SObject(ModEntry.SecretJojaFertilizerID, 1), new[] { 150, 20 });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while trying to add Secret Joja Fertilizer to JojaMart.\n\n{ex}", LogLevel.Error);
        }
    }
}