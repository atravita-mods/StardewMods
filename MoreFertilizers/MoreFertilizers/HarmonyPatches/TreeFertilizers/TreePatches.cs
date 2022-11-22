using AtraShared.Utils.Extensions;
using HarmonyLib;
using MoreFertilizers.Framework;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.TreeFertilizers;

/// <summary>
/// Patches against Tree.
/// </summary>
[HarmonyPatch(typeof(Tree))]
internal static class TreePatches
{
    [HarmonyPatch(nameof(Tree.fertilize))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "HarmonyConvention")]
    private static void Postfix(Tree __instance, bool __result)
    {
        if (__result)
        {
            __instance.modData?.SetBool(CanPlaceHandler.TreeFertilizer, true);
        }
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(nameof(Tree.UpdateTapperProduct))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "HarmonyConvention")]
    private static void PostfixUpdate(Tree __instance, SObject tapper_instance)
    {
        if (__instance.modData?.GetBool(CanPlaceHandler.TreeTapperFertilizer) == true)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Reducing tapper time of tree at {__instance.currentTileLocation}.");
            tapper_instance.MinutesUntilReady = Math.Max((int)(tapper_instance.MinutesUntilReady * 0.75), 0);
        }
    }
}