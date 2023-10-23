using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Menus;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Holds patches against Utility for shops.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class UtilityShopPatcher
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.getShopStock))]
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
                ModEntry.ModMonitor.LogError("adding to Pierre's stock", ex);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.getQiShopStock))]
    private static void PostfixGetCasinoShop(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (ModEntry.LuckyFertilizerID != -1 && Game1.player.team.AverageDailyLuck() > 0.05)
            {
                __result.Add(new SObject(ModEntry.LuckyFertilizerID, 1), new[] { 500, ShopMenu.infiniteStock });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding to casino's stock", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.getJojaStock))]
    private static void PostfixJojaStock(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (ModEntry.SecretJojaFertilizerID != -1
                && Game1.player.stats.IndividualMoneyEarned > 1_000_000 && Singletons.Random.OfChance(0.15)
                && Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja"))
            {
                __result.Add(new SObject(ModEntry.SecretJojaFertilizerID, 1), new[] { 500, 2 });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding Secret Joja Fertilizer to JojaMart", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.GetQiChallengeRewardStock))]
    private static void PostfixQiGemShop(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (ModEntry.EverlastingFertilizerID != -1)
            {
                SObject obj = new(ModEntry.EverlastingFertilizerID, 1);
                __result.Add(obj, new[] { 0, ShopMenu.infiniteStock, 858, 1 });
            }
            if (ModEntry.EverlastingFruitTreeFertilizerID != -1)
            {
                SObject obj = new(ModEntry.EverlastingFruitTreeFertilizerID, 1);
                __result.Add(obj, new[] { 0, ShopMenu.infiniteStock, 858, 1 });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding to qi's shop", ex);
        }
    }
}