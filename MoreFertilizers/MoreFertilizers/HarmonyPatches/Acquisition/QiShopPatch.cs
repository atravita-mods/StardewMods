using HarmonyLib;

using StardewValley.Menus;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Patch to put our fertilizer into Qi's shop.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class QiShopPatch
{
    [HarmonyPatch(nameof(Utility.GetQiChallengeRewardStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
    private static void Postfix(Dictionary<ISalable, int[]> __result)
    {
        if (ModEntry.EverlastingFertilizerID != -1)
        {
            SObject obj = new(ModEntry.EverlastingFertilizerID, 1);
            __result.Add(obj, new[] { 0, ShopMenu.infiniteStock, 858, 1 });
        }
    }
}