namespace GingerIslandMainlandAdjustments.Niceties;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Pathfinding;

/// <summary>
/// Speeds up NPCs if they have a long way to travel to and from the resort.
/// </summary>
/// <remarks>using about six hours as the cutoff for now.</remarks>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class NPCTravelSpeedAdjuster
{
    [HarmonyPatch(nameof(NPC.checkSchedule))]
    private static void Postfix(NPC __instance)
    {
        try
        {
            if (__instance?.controller is PathFindController controller
                && Game1.IsVisitingIslandToday(__instance.Name)
                && controller.pathToEndPoint.Count * 32 / 42 > 340)
            {
                Globals.ModMonitor.DebugOnlyLog($"Found npc {__instance.Name} with long travel path, speeding them up.");
                __instance.Speed = 4;
                __instance.isCharging = true;
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError("speeding up NPCs", ex);
        }
    }
}
