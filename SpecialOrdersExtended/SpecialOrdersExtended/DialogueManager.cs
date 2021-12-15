using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;

using StardewValley;

namespace SpecialOrdersExtended
{

    internal class DialogueManager
    {
        public static void PostfixCheckDialogue(ref bool __result, ref NPC __instance, int __0, bool __1)
        {
            try
            {
                if (__result) { return; } //have already found a New Current Dialogue
                foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                {

                    string baseKey = ((__1) ? specialOrder.questKey.Value : Game1.currentSeason + specialOrder.questKey.Value);
                    switch (specialOrder.questState.Value)
                    {
                        case SpecialOrder.QuestState.InProgress:
                            baseKey += "_InProgress";
                            break;
                        case SpecialOrder.QuestState.Failed:
                            baseKey += "_Failed";
                            break;
                        case SpecialOrder.QuestState.Complete:
                            baseKey += "_Completed";
                            break;
                        default:
                            throw new UnexpectedEnumValueException<SpecialOrder.QuestState>(specialOrder.questState.Value);
                    }

                    string dialogueKey = $"{baseKey}_{Game1.shortDayDisplayNameFromDayOfSeason(Game1.dayOfMonth)}";
                    if (__instance.Dialogue.ContainsKey(dialogueKey))
                    {
                        if (Game1.player.mailReceived.Contains($"{__instance.Name}_{dialogueKey}")) { continue; }
                        Game1.player.mailReceived.Add($"{__instance.Name}_{dialogueKey}");
                        __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[dialogueKey], __instance) { removeOnNextMove = true });
                        ModEntry.ModMonitor.Log($"Found key {dialogueKey}", LogLevel.Trace);
                        __result = true;
                        return;
                    }

                    for (int heartLevel = 14; heartLevel > 0; heartLevel -= 2)
                    {
                        dialogueKey = $"{baseKey}{heartLevel}";
                        if (__0 > heartLevel && __instance.Dialogue.ContainsKey(dialogueKey))
                        {
                            if (Game1.player.mailReceived.Contains($"{__instance.Name}_{dialogueKey}")) { continue; }
                            Game1.player.mailReceived.Add($"{__instance.Name}_{dialogueKey}");
                            __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[dialogueKey], __instance) { removeOnNextMove = true });
                            ModEntry.ModMonitor.Log($"Found key {dialogueKey}", LogLevel.Trace);
                            __result = true;
                            return;
                        }
                    }

                    if (__instance.Dialogue.ContainsKey(baseKey))
                    {
                        if (Game1.player.mailReceived.Contains($"{__instance.Name}_{baseKey}")) { continue; }
                        Game1.player.mailReceived.Add($"{__instance.Name}_{baseKey}");
                        __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[baseKey], __instance) { removeOnNextMove = true });
                        ModEntry.ModMonitor.Log($"Found key {baseKey}", LogLevel.Trace);
                        __result = true;
                        return;
                    }

                    ModEntry.ModMonitor.Log($"Did not find dialogue key for special order {baseKey} for NPC {__instance.Name}", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed in checking for Special Order dialogue for NPC {__instance.Name}\n{ex}", LogLevel.Error);
            }
        }
    }

    internal class DialogueLog
    {
        string key;
        List<string> charactersList = new();

        public DialogueLog(string key)
        {
            this.key = key;
        }
    }

}
