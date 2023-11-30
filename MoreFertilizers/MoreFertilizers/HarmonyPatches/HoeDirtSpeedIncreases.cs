using HarmonyLib;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches;

/// <summary>
/// Patches for HoeDirt.applySpeedIncreases.
/// </summary>
[HarmonyPatch(typeof(HoeDirt))]
internal static class HoeDirtSpeedIncreases
{
    private static readonly Dictionary<string, (float dry, float paddy)> Values = [];

    internal static bool AddFertilizer(string unqualified, (float dry, float paddy) value)
    {
        if (Values.TryAdd(unqualified, value))
        {
            Values.TryAdd(ItemRegistry.type_object + unqualified, value);
            return true;
        }

        return false;
    }

    internal static void Clear() => Values.Clear();

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