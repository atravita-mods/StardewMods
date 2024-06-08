using System.Diagnostics;

using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Holds patches against dialogue for debugging only.
/// </summary>
[HarmonyPatch(typeof(Dialogue))]
internal static class DialoguePatcher
{
    private static bool Prepare => ModEntry.ModMonitor.IsVerbose;

    [HarmonyPatch(MethodType.Constructor, new[] { typeof(NPC), typeof(string), typeof(string) })]
    private static void Postfix(Dialogue __instance, string translationKey, string dialogueText)
    {
        if (string.IsNullOrWhiteSpace(translationKey) && string.IsNullOrWhiteSpace(dialogueText))
        {
            ModEntry.ModMonitor.Log(new StackTrace().ToString());
        }
    }
}
