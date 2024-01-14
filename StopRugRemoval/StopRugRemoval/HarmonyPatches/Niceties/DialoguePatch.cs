using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Patch to make dialogue more sane.
/// </summary>
[HarmonyPatch(typeof(NPC))]
internal static class DialoguePatch
{
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(NPC.checkForNewCurrentDialogue))]
    private static bool Prefix(NPC __instance, ref bool __result)
    {
        if (__result)
        {
            return true;
        }

        if (__instance.CurrentDialogue.TryPeek(out Dialogue? dialogue) && !dialogue.isDialogueFinished() && dialogue.currentDialogueIndex != 0)
        {
            ModEntry.ModMonitor.VerboseLog($"NPC {__instance.Name} appears to be in mid-dialogue. Suppressing getting new dialogue until previous dialogue is finished.");
            __result = false;
            return false;
        }

        return true;
    }
}
