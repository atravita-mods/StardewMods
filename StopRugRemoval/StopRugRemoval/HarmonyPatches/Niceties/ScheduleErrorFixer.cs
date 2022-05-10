using AtraBase.Toolkit.StringHandler;
using HarmonyLib;
using StardewModdingAPI.Utilities;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// A patch to try to unfuck schedules.
/// I think this may be antisocial causing issues.
/// </summary>
[HarmonyPatch(typeof(NPC))]
internal static class ScheduleErrorFixer
{
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void Prefix(string rawData, NPC __instance)
    {
        if (__instance.currentLocation is null)
        {
            ModEntry.ModMonitor.Log($"{__instance.Name} seems to have a null current location, attempting to fix. Please inform their author! The current day is {SDate.Now()}", LogLevel.Warn);
            if (__instance.DefaultMap is not null && Game1.getLocationFromName(__instance.DefaultMap) is GameLocation location)
            {
                __instance.currentLocation = location;
                return;
            }
            else
            {
                if (rawData.SpanSplit().TryGetAtIndex(1, out SpanSplitEntry locName) && Game1.getLocationFromName(locName) is GameLocation loc)
                {
                    __instance.currentLocation = loc;
                    return;
                }
            }
            ModEntry.ModMonitor.Log($"Failed to fix current location for NPC {__instance.Name}", LogLevel.Error);
        }
    }
}