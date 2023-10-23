using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

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
    internal static void ApplyDGAPatch(Harmony harmony)
    {
#warning - do the DGA patch here too. Probably will need seperate patches.
    }

    [HarmonyPatch(nameof(FruitTree.shake))]
    private static void Prefix(FruitTree __instance, out int __state)
    {
        __state = __instance.fruitsOnTree.Value;
    }

    [HarmonyPatch(nameof(FruitTree.shake))]
    private static void Postfix(FruitTree __instance, int __state, Vector2 tileLocation)
    {
        if (__instance.struckByLightningCountdown.Value > 0 || __state == 0 || __instance.fruitsOnTree.Value != 0
            || __instance.modData?.GetBool(CanPlaceHandler.MiraculousBeverages) != true)
        {
            return;
        }

        do
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Checking for beverages: state {__state}.");

            if (MiraculousFertilizerHandler.GetBeverage(__instance.indexOfFruit.Value) is SObject output)
            {
                Game1.createItemDebris(output, tileLocation * 64f, -1);
            }
        }
        while (--__state > 0);
    }
}
