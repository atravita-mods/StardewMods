using HarmonyLib;

using StardewValley.Menus;

namespace GiantCropFertilizer.HarmonyPatches;

/// <summary>
/// Patch to put our fertilizer into Qi's shop.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class UtilityShopPatcher
{
    [HarmonyPatch(nameof(Utility.GetQiChallengeRewardStock))]
    private static void Postfix(Dictionary<ISalable, int[]> __result)
    {
        if (ModEntry.GiantCropFertilizerID != -1)
        {
            SObject obj = new(ModEntry.GiantCropFertilizerID, 1);
            __result.Add(obj, new[] { 0, ShopMenu.infiniteStock, 858, 5 });
        }
    }
}