using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch(typeof(Event))]
internal static class EventPreconditionsLogging
{
    private static bool Prepare() => ModEntry.ModMonitor.IsVerbose;

    [HarmonyPatch(nameof(Event.CheckPrecondition), new[] { typeof(GameLocation), typeof(string), typeof(string)})]
    private static void Postfix(string eventId, string precondition, bool __result)
    {
        ModEntry.ModMonitor.Log($"Checking precondition {precondition} for event {eventId} was {__result}", LogLevel.Info);
    }
}
