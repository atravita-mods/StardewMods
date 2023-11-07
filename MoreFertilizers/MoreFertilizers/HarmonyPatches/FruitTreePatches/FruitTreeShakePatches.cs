using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using MoreFertilizers.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.FruitTreePatches;

/// <summary>
/// Applies patches against shaking fruit trees.
/// We do this to get the beverage for the Miraculous Beverages fertilizer.
/// </summary>
[HarmonyPatch(typeof(FruitTree))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class FruitTreeShakePatches
{
    [HarmonyPatch(nameof(FruitTree.shake))]
    private static void Prefix(FruitTree __instance)
    {
        if (__instance.struckByLightningCountdown.Value > 0 || __instance.fruit.Count == 0 
            || __instance.modData?.GetBool(CanPlaceHandler.MiraculousBeverages) != true)
        {
            return;
        }

        int count = __instance.fruit.Count;
        for (int i = count; i >= 0; i++)
        {
            SObject? obj = __instance.fruit[i] as SObject;
            if (obj is not null && MiraculousFertilizerHandler.GetBeverage(obj) is SObject output)
            {
                __instance.fruit.Add(output);
            }
        }
    }
}
