using AtraShared.Utils.Extensions;

using GrowableBushes.Framework;

using HarmonyLib;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.HarmonyPatches;

[HarmonyPatch(typeof(Bush))]
internal static class BushDestroyablePatches
{
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(nameof(Bush.isDestroyable))]
    private static void Postfix(Bush __instance, ref bool __result)
    {
        if (!__result)
        {
            try
            {
                if (__instance.modData?.GetBool(InventoryBush.BushModData) == true)
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed while attempting to override bush destroyability\n\n{ex}", LogLevel.Error);
            }
        }
    }
}
