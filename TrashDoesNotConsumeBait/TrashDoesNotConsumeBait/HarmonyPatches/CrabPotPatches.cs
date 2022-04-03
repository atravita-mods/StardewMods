using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewValley.Objects;

namespace TrashDoesNotConsumeBait.HarmonyPatches;

[HarmonyPatch(typeof(CrabPot))]
internal static class CrabPotPatches
{
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(CrabPot.checkForAction))]
    private static void Prefix(CrabPot __instance, bool justCheckingForActivity, out SObject? __state)
    {
        if (!justCheckingForActivity && __instance.heldObject.Value?.IsTrashItem() == true)
        {
            __state = __instance.bait.Value;
        }
        else
        {
            __state = null;
        }
    }

    [HarmonyPatch(nameof(CrabPot.checkForAction))]
    private static void Postfix(CrabPot __instance, bool justCheckingForActivity, SObject? __state)
    {
        ModEntry.ModMonitor.Log(__state?.DisplayName, LogLevel.Debug);
        if (!justCheckingForActivity && __state is not null && __instance.bait.Value is null)
        {
            __instance.bait.Value = __state;
        }
    }
}