namespace GingerIslandMainlandAdjustments.Niceties;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

/// <summary>
/// A patch to skip actually scheduling if NPCs lack schedules.
/// It's a little faster.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class NPCSchedulePatches
{
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(NPC.TryLoadSchedule), new Type[] { })]
    private static bool Prefix(NPC __instance, ref bool __result)
    {
        try
        {
            if (__instance.getMasterScheduleRawData()?.Count is null or 0)
            {
                __instance.ClearSchedule();
                __result = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError("skipping past NPCs that don't actually have schedules", ex);
        }
        return true;
    }
}
