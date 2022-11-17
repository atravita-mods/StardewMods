using HarmonyLib;
using StardewValley.Locations;
using StardewValley.Menus;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Postfix to add things to Krobus's shop.
/// </summary>
[HarmonyPatch(typeof(Sewer))]
internal static class KrobusShopStockPostfix
{
    [HarmonyPatch(nameof(Sewer.getShadowShopStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static void Postfix(ref Dictionary<ISalable, int[]> __result)
    {
        if (ModEntry.PaddyCropFertilizerID != -1)
        {
            __result.TryAdd(new SObject(ModEntry.PaddyCropFertilizerID, 1), new[] { 40, ShopMenu.infiniteStock });
        }
        if (ModEntry.WisdomFertilizerID != -1 && Game1.currentSeason is "spring" or "fall")
        {
            __result.TryAdd(new SObject(ModEntry.WisdomFertilizerID, 1), new[] { 80, ShopMenu.infiniteStock });
        }
        if (ModEntry.MiraculousBeveragesID != -1 && Game1.year > 2 && Utility.getCookedRecipesPercent() > 0.5f)
        {
            __result.TryAdd(new SObject(ModEntry.MiraculousBeveragesID, 1), new[] { 250, ShopMenu.infiniteStock });
        }
    }
}