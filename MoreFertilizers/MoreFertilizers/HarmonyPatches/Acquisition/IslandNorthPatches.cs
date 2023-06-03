using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Locations;
using StardewValley.Menus;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Patches against IslandNorth to put the fertilizer into the shop.
/// </summary>
[HarmonyPatch(typeof(IslandNorth))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class IslandNorthPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(IslandNorth.getIslandMerchantTradeStock))]
    private static void PostfixShop(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (!Utility.hasFinishedJojaRoute() && ModEntry.DeluxeFishFoodID != -1
                && Utility.getFishCaughtPercent(Game1.player) > 0.5f)
            { // 851 - magma cap.
                __result.TryAdd(new SObject(ModEntry.DeluxeFishFoodID, 1), new[] { 0, ShopMenu.infiniteStock, 851, 5 });
            }
            if (ModEntry.SecretJojaFertilizerID != -1 && Game1.player.DailyLuck > 0.05)
            { // 909 - radioactive ore
                __result.TryAdd(new SObject(ModEntry.SecretJojaFertilizerID, 1), new[] { 0, ShopMenu.infiniteStock, 909, Utility.hasFinishedJojaRoute() ? 2 : 4 });
            }
            if (ModEntry.MiraculousBeveragesID != -1)
            { // 253 - triple shot espresso
                __result.TryAdd(new SObject(ModEntry.MiraculousBeveragesID, 1), new[] { 0, ShopMenu.infiniteStock, 253, 1 });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding stock to Island North", ex);
        }
    }
}