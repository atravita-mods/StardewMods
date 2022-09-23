using HarmonyLib;
using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Patches against IslandNorth to put the fertilizer into the shop.
/// </summary>
[HarmonyPatch(typeof(IslandNorth))]
internal static class IslandNorthPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(IslandNorth.getIslandMerchantTradeStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void PostfixShop(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (!Utility.hasFinishedJojaRoute() && ModEntry.DeluxeFishFoodID != -1
                && Utility.getFishCaughtPercent(Game1.player) > 0.5f)
            { // 851 - magma cap.
                __result.TryAdd(new SObject(ModEntry.DeluxeFishFoodID, 1), new[] { 0, int.MaxValue, 851, 5 });
            }
            if (ModEntry.SecretJojaFertilizerID != -1)
            { // 909 - radioactive ore
                __result.TryAdd(new SObject(ModEntry.SecretJojaFertilizerID, 1), new[] { 0, int.MaxValue, 909, Utility.hasFinishedJojaRoute() ? 1 : 5 });
            }
            if (ModEntry.MiraculousBeveragesID != -1)
            { // 253 - triple shot espresso
                __result.TryAdd(new SObject(ModEntry.MiraculousBeveragesID, 1), new[] { 0, int.MaxValue, 253, 1 });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into errors adding stock to Island North!\n\n{ex}", LogLevel.Error);
        }
    }
}