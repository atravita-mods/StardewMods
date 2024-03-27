using HarmonyLib;

using Netcode;

namespace MarriageDialogueAdjuster;

[HarmonyPatch]
internal sealed class ModEntry : Mod
{
    private static IMonitor modMonitor = null!;
    private static IReflectionHelper reflector = null!;

    public override void Entry(IModHelper helper)
    {
        modMonitor = this.Monitor;
        reflector = this.Helper.Reflection;
        new Harmony(this.Helper.ModRegistry.ModID).PatchAll(typeof(ModEntry).Assembly);
    }

    [HarmonyPatch(typeof(MarriageDialogueReference), nameof(MarriageDialogueReference.GetDialogue))]
    private static void Prefix(MarriageDialogueReference __instance, NPC n)
    {
        NetString dialogueNet = reflector.GetField<NetString>(__instance, "_dialogueFile").GetValue();
        string dialogueFile = dialogueNet.Value;

        if (dialogueFile != "Strings\\StringsFromCSFiles")
        {
            return;
        }

        string dialogueKey = reflector.GetField<NetString>(__instance, "_dialogueKey").GetValue().Value;
        if (n.Dialogue.ContainsKey(dialogueKey))
        {
            modMonitor.Log($"Redirecting key {dialogueKey} for {n.Name}");
            dialogueNet.Value = "Characters\\Dialogue\\" + n.GetDialogueSheetName();
        }
    }

}