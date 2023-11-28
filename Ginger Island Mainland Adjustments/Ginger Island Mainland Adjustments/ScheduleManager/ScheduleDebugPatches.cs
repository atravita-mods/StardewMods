namespace GingerIslandMainlandAdjustments.ScheduleManager;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;
using Microsoft.Xna.Framework;

using StardewValley.Pathfinding;

/// <summary>
/// Class that handles patches for debugging schedules.
/// These will only be active if DebugMode is set to true.
/// And thus: no harmony annotations.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ScheduleDebugPatches
{
    private static readonly List<NPC> FailedNPCs = [];

    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatches(Harmony harmony)
    {
        harmony.Patch(
            original: typeof(NPC).GetCachedMethod(nameof(NPC.pathfindToNextScheduleLocation), ReflectionCache.FlagTypes.InstanceFlags),
            finalizer: new HarmonyMethod(typeof(ScheduleDebugPatches), nameof(FinalizePathfinder)));
    }

    /// <summary>
    /// Nulls out the schedules for problem NPCs.
    /// </summary>
    internal static void FixNPCs()
    {
        foreach (NPC npc in FailedNPCs)
        {
            npc.ClearSchedule();
        }
        FailedNPCs.Clear();
    }

    /// <summary>
    /// Finalizer on NPC pathfindToNextScheduleLocation.
    /// </summary>
    /// <param name="__instance">NPC.</param>
    /// <param name="startingLocation">The starting map.</param>
    /// <param name="startingX">Starting X.</param>
    /// <param name="startingY">Starting Y.</param>
    /// <param name="endingLocation">Ending map.</param>
    /// <param name="endingX">Ending X.</param>
    /// <param name="endingY">Ending Y.</param>
    /// <param name="__exception">Exception raised, if any.</param>
    /// <param name="__result">The result of the function (an empty schedulePoint).</param>
    /// <returns>null to suppress the exception.</returns>
    private static Exception? FinalizePathfinder(
        NPC __instance,
        string startingLocation,
        int startingX,
        int startingY,
        string endingLocation,
        int endingX,
        int endingY,
        Exception __exception,
        ref SchedulePathDescription __result)
    {
        Globals.ModMonitor.VerboseLog($"Checking schedule point for {__instance.Name} at map {startingLocation} {startingX} {startingY}");
        if (__exception is not null)
        {
            Globals.ModMonitor.Log($"Encountered error parsing schedule for {__instance.Name}, {startingLocation} {startingX} {startingY} to {endingLocation} {endingX} {endingY}.\n\n{__exception}", LogLevel.Error);
            __result = new SchedulePathDescription(new Stack<Point>(), 2, null, null, endingLocation, new Point(endingX, endingY));
            FailedNPCs.Add(__instance);
        }
        return null;
    }
}