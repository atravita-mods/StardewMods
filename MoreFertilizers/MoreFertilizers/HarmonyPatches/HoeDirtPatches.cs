using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches;

/// <summary>
/// Patches against HoeDirt.
/// </summary>
[HarmonyPatch(typeof(HoeDirt))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class HoeDirtPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(HoeDirt.canPlantThisSeedHere))]
    private static bool PrefixCanBePlanted(int objectIndex, bool isFertilizer, ref bool __result)
    {
        if (isFertilizer && ModEntry.SpecialFertilizerIDs.Contains(objectIndex))
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HoeDirt.plant))]
    private static bool PrefixPlant(int index, bool isFertilizer, ref bool __result)
    {
        if (isFertilizer && ModEntry.SpecialFertilizerIDs.Contains(index))
        {
            __result = false;
            return false;
        }
        return true;
    }
}