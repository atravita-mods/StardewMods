#define TRACELOG

using System.Diagnostics;

using AtraShared.Utils.Extensions;

using HarmonyLib;

namespace ExperimentalLagReduction.HarmonyPatches.Diagnostics;

[HarmonyPatch(typeof(NPC))]
internal static class ChooseAppearance
{
    private static bool Prepare => ModEntry.ModMonitor.IsVerbose;

    [HarmonyPatch(nameof(NPC.ChooseAppearance))]
    private static void Prefix(NPC __instance, out Stopwatch __state)
    {
        ModEntry.ModMonitor.Log($"ChooseAppearance called for {__instance.Name} at {__instance.currentLocation.NameOrUniqueName}", LogLevel.Info);
        __state = Stopwatch.StartNew();
    }

    [HarmonyPatch(nameof(NPC.ChooseAppearance))]
    private static void Postfix(NPC __instance, Stopwatch __state)
    {
        ModEntry.ModMonitor.LogTimespan($"checking appearance for {__instance.Name}", __state);
    }
}
