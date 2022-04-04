#if DEBUG
using AtraShared.Utils.Extensions;
using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Patches for SayHiTo...
/// </summary>
[HarmonyPatch(typeof(NPC))]
internal static class SayHiToPatch
{
    [HarmonyPatch(nameof(NPC.sayHiTo))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void Postfix(NPC __instance, Character c)
    {
        ModEntry.ModMonitor.DebugLog($"{__instance.Name} trying to say hi to {c.Name}", LogLevel.Alert);
    }
}
#endif