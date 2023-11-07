using HarmonyLib;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches;

/// <summary>
/// Patches for HoeDirt.applySpeedIncreases.
/// </summary>
[HarmonyPatch(typeof(HoeDirt))]
internal static class HoeDirtSpeedIncreases
{
    [HarmonyPatch(nameof(HoeDirt.GetFertilizerSpeedBoost))]
    private static void Postfix(HoeDirt __instance, ref float __result)
    {
        if (__instance.fertilizer.Value == ModEntry.SecretJojaFertilizerID)
        {
            __result += 0.2f;
        }
        else if (__instance.fertilizer.Value == ModEntry.PaddyCropFertilizerID && __instance.hasPaddyCrop())
        {
            __result += 0.1f;
        }
    }
}