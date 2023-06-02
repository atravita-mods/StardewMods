using HarmonyLib;

namespace GingerIslandMainlandAdjustments.Niceties;

/// <summary>
/// A patch to skip actually scheduling if NPCs lack schedules.
/// It's a little faster.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
internal static class NPCSchedulePatches
{
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(NPC.getSchedule))]
    private static bool Prefix(NPC __instance, ref Dictionary<int, SchedulePathDescription>? __result)
    {
        try
        {
            if (__instance.getMasterScheduleRawData()?.Count is null or 0)
            {
                __instance.followSchedule = false;
                __result = null;
                return false;
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Failed while skipping past NPCs that don't actually have schedules", LogLevel.Error);
            Globals.ModMonitor.Log(ex.ToString());
        }
        return true;
    }
}
