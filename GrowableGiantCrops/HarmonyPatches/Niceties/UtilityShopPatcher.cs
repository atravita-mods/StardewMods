using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework.InventoryModels;

using HarmonyLib;

using StardewValley.Menus;

namespace GrowableGiantCrops.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches against Qi's shop stock.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class UtilityShopPatcher
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Utility.GetQiChallengeRewardStock))]
    private static void PostfixQiGemShop(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            InventoryTree tree = new(TreeIndexes.Mushroom, 1, 5);
            __result.TryAdd(tree, new[] { 0, ShopMenu.infiniteStock, 858, 15 });
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding to qi's shop", ex);
        }
    }
}
