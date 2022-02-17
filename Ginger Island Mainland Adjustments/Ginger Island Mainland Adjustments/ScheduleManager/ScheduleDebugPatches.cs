using HarmonyLib;
using Microsoft.Xna.Framework;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

internal class ScheduleDebugPatches
{

    private static List<NPC> failedNPCs = new();

    public static void ApplyPatches(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), "pathfindToNextScheduleLocation"),
            finalizer: new HarmonyMethod(typeof(ScheduleDebugPatches), nameof(ScheduleDebugPatches.FinalizePathfinder))
            );
    }

    private static Exception? FinalizePathfinder(
        NPC __instance,
        string startingLocation,
        int startingX,
        int startingY,
        string endingLocation,
        int endingX,
        int endingY,
        int finalFacingDirection,
        string endBehavior,
        string endMessage,
        Exception __exception,
        ref SchedulePathDescription __result)
    {
        Globals.ModMonitor.Log($"Checking schedule point for {__instance.Name} at map {startingLocation} {startingX} {startingY}");
        if (__exception is not null)
        {
            Globals.ModMonitor.Log($"Encountered error parsing schedule for {__instance.Name}, {startingLocation} {startingX} {startingY} to {endingLocation} {endingX} {endingY}.\n\n{__exception}", LogLevel.Error);
            __result = new SchedulePathDescription(new Stack<Point>(), 2, null, null);
            failedNPCs.Add(__instance);
        }
        return null;
    }
}