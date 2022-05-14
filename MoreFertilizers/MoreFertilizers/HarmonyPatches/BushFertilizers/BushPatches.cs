using AtraShared.Utils.Extensions;
using HarmonyLib;
using MoreFertilizers.Framework;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.BushFertilizers;

/// <summary>
/// Patches against bushes.
/// </summary>
[HarmonyPatch(typeof(Bush))]
internal static class BushPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Bush.getAge))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
    private static void PostfixGetAge(Bush __instance, ref int __result)
    {
        if (__instance.modData?.GetBool(CanPlaceHandler.RapidBush) == true)
        {
            __result = (int)(__result * 1.2);
        }
    }
}