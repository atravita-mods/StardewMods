using AtraShared.Utils.Extensions;

using HarmonyLib;

namespace ExperimentalLagReduction.HarmonyPatches;

/// <summary>
/// Patches location loading to add doors everywhere.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DoorsForAll
{
    [HarmonyPatch(nameof(GameLocation.loadObjects))]
    private static void Postfix(GameLocation __instance)
    {
        if (!ModEntry.Config.AllowModAddedDoors)
        {
            return;
        }

        if (__instance.warps?.Count is null or 0)
        {
            try
            {
                __instance.updateWarps();
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"parsing warps for {__instance.NameOrUniqueName}", ex);
            }
        }

        if (__instance.doors?.Count() is 0 or null)
        {
            try
            {
                __instance.updateDoors();
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"parsing doors for {__instance.NameOrUniqueName}", ex);
            }
        }
    }
}
