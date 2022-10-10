using AtraShared.Utils.Extensions;

using HarmonyLib;

namespace GingerIslandMainlandAdjustments.Niceties;

/// <summary>
/// Speeds up NPCs if they have a long way to travel to and from the resort.
/// </summary>
[HarmonyPatch(typeof(NPC))]
internal static class NPCTravelSpeedAdjuster
{
    [HarmonyPatch(nameof(NPC.checkSchedule))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static void Postfix(NPC __instance)
    {
        if (__instance?.controller is PathFindController controller
            && Game1.IsVisitingIslandToday(__instance.Name)
            && controller.pathToEndPoint.Count * 32 / 42 > 180)
        {
            Globals.ModMonitor.DebugOnlyLog($"Found npc {__instance.Name} with long travel path, speeding them up.");
            __instance.Speed = 4;
            __instance.isCharging = true;
        }
    }
}
