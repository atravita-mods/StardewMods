#if DEBUG
using AtraShared.Utils.Extensions;
using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties;

[HarmonyPatch(typeof(NPC))]
internal static class SayHiToPatch
{
    [HarmonyPatch(nameof(NPC.sayHiTo))]
    private static void Postfix(NPC __instance, Character c)
    {
        ModEntry.ModMonitor.DebugLog($"{__instance.Name} trying to say hi to {c.Name}", LogLevel.Alert);
    }
}
#endif